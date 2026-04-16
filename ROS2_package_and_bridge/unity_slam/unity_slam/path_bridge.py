import rclpy
from rclpy.node import Node
from rclpy.action import ActionClient
from geometry_msgs.msg import PoseStamped
from nav_msgs.msg import Path
from nav2_msgs.action import ComputePathToPose, NavigateToPose
from std_msgs.msg import Bool, Header


class PathBridge(Node):
    """
    ROS2 bridge between Unity and Nav2.

    Handles three responsibilities:
      1. Latency measurement  — echoes /unity/ping back on /unity/pong.
      2. Visual path          — receives a goal from Unity, calls Nav2
                                ComputePathToPose, and publishes the result
                                back to Unity for rendering.
      3. Autonomous navigation — receives a goal from Unity and sends it to
                                 Nav2 NavigateToPose, tracking the full
                                 action lifecycle until completion or cancel.
    """

    def __init__(self):
        super().__init__('unity_path_bridge')

        # ── Action clients ────────────────────────────────────────────────────
        self._compute_client = ActionClient(self, ComputePathToPose, 'compute_path_to_pose')
        self._nav_client     = ActionClient(self, NavigateToPose,    'navigate_to_pose')

        # ── Publishers ────────────────────────────────────────────────────────
        self._path_pub = self.create_publisher(Path,   '/unity/path_result', 10)
        self._pong_pub = self.create_publisher(Header, '/unity/pong',        10)

        # ── Subscriptions ─────────────────────────────────────────────────────
        self.create_subscription(PoseStamped, '/unity/path_goal',        self.on_path_goal, 10)
        self.create_subscription(PoseStamped, '/unity/path_goal_visual', self.on_path_goal, 10)
        self.create_subscription(PoseStamped, '/unity/nav_goal',         self.on_nav_goal,  10)
        self.create_subscription(Bool,        '/unity/cancel_nav',       self.on_cancel,    10)
        self.create_subscription(Header,      '/unity/ping',             self.on_ping,      10)

        # ── Internal state ────────────────────────────────────────────────────
        self._compute_busy = False   # prevents overlapping ComputePathToPose calls
        self._nav_handle   = None    # handle of the active NavigateToPose goal
        self._nav_goal_id  = 0       # incremented on each new nav goal
        self._active_id    = -1      # ID of the goal currently being executed

        self.get_logger().info('PathBridge ready.')
        self.get_logger().info('  Listening on: /unity/path_goal, /unity/path_goal_visual,')
        self.get_logger().info('                /unity/nav_goal, /unity/cancel_nav, /unity/ping')
        self.get_logger().info('  Publishing on: /unity/path_result, /unity/pong')

    # ──────────────────────────────────────────────────────────────────────────
    # 1. Latency measurement — instant echo
    # ──────────────────────────────────────────────────────────────────────────

    def on_ping(self, msg: Header):
        """Echo the ping message back to Unity immediately for RTT measurement."""
        self._pong_pub.publish(msg)

    # ──────────────────────────────────────────────────────────────────────────
    # 2. Visual path — ComputePathToPose → /unity/path_result
    #
    #  Unity sends goal  →  [on_path_goal]
    #    Nav2 ComputePathToPose requested  →  [on_compute_accepted]
    #      Path received from Nav2  →  [on_compute_result]
    #        Published to Unity on /unity/path_result
    # ──────────────────────────────────────────────────────────────────────────

    def on_path_goal(self, msg: PoseStamped):
        """
        Receives a visual path goal from Unity and requests a path from Nav2.
        Drops the request if a previous computation is still in progress.
        """
        if self._compute_busy:
            self.get_logger().debug('[visual path] Previous computation still in progress, skipping.')
            return

        self._compute_busy = True
        self.get_logger().info(
            f'[visual path] Goal received: ({msg.pose.position.x:.2f}, {msg.pose.position.y:.2f}). '
            f'Requesting ComputePathToPose...')

        if not self._compute_client.wait_for_server(timeout_sec=1.0):
            self.get_logger().warn('[visual path] ComputePathToPose action server not available.')
            self._compute_busy = False
            return

        goal            = ComputePathToPose.Goal()
        goal.goal       = msg
        goal.use_start  = False
        goal.planner_id = ''

        future = self._compute_client.send_goal_async(goal)
        future.add_done_callback(self.on_compute_accepted)

    def on_compute_accepted(self, future):
        """Called when Nav2 accepts or rejects the ComputePathToPose goal."""
        handle = future.result()

        if not handle.accepted:
            self.get_logger().warn('[visual path] ComputePathToPose goal rejected by Nav2.')
            self._compute_busy = False
            return

        self.get_logger().info('[visual path] Goal accepted by Nav2, waiting for path result...')
        handle.get_result_async().add_done_callback(self.on_compute_result)

    def on_compute_result(self, future):
        """Called when Nav2 returns the computed path. Publishes it to Unity."""
        result = future.result().result

        if result.path.poses:
            self._path_pub.publish(result.path)
            self.get_logger().info(
                f'[visual path] Path computed: {len(result.path.poses)} poses. '
                f'Published to /unity/path_result.')
        else:
            self.get_logger().warn('[visual path] Nav2 returned an empty path.')

        self._compute_busy = False

    # ──────────────────────────────────────────────────────────────────────────
    # 3. Autonomous navigation — NavigateToPose full lifecycle
    #
    #  Unity sends goal  →  [on_nav_goal]
    #    Nav2 NavigateToPose requested  →  [on_nav_accepted]
    #      Robot navigating...
    #        Goal reached / cancelled / failed  →  [on_nav_result]
    #
    #  Unity sends cancel  →  [on_cancel]
    #    Active goal aborted via cancel_goal_async()
    # ──────────────────────────────────────────────────────────────────────────

    def on_nav_goal(self, msg: PoseStamped):
        """
        Receives an autonomous navigation goal from Unity.
        Cancels any previously active goal before sending the new one.
        Each goal is assigned a unique incremental ID to prevent stale callbacks
        from affecting the current navigation.
        """
        self._nav_goal_id += 1
        my_id = self._nav_goal_id

        if self._nav_handle is not None:
            self.get_logger().info(
                f'[nav] New goal (id={my_id}) received while navigating — cancelling previous goal.')
            self._nav_handle.cancel_goal_async()
            self._nav_handle = None

        self.get_logger().info(
            f'[nav] Goal (id={my_id}): ({msg.pose.position.x:.2f}, {msg.pose.position.y:.2f}). '
            f'Contacting NavigateToPose action server...')

        if not self._nav_client.wait_for_server(timeout_sec=2.0):
            self.get_logger().warn('[nav] NavigateToPose action server not available.')
            return

        goal      = NavigateToPose.Goal()
        goal.pose = msg

        future = self._nav_client.send_goal_async(goal)
        future.add_done_callback(lambda f, id=my_id: self.on_nav_accepted(f, id))

    def on_nav_accepted(self, future, goal_id):
        """
        Called when Nav2 accepts or rejects the NavigateToPose goal.
        If a newer goal has already been sent, the accepted goal is immediately cancelled.
        """
        handle = future.result()

        if not handle.accepted:
            self.get_logger().warn(f'[nav] Goal (id={goal_id}) rejected by Nav2.')
            return

        if goal_id == self._nav_goal_id:
            # This is still the most recent goal — make it active
            self._nav_handle = handle
            self._active_id  = goal_id
            self.get_logger().info(
                f'[nav] Goal (id={goal_id}) accepted. Robot is now navigating...')
        else:
            # A newer goal was already sent — discard this one
            self.get_logger().info(
                f'[nav] Goal (id={goal_id}) accepted but superseded by a newer goal. Cancelling.')
            handle.cancel_goal_async()
            return

        handle.get_result_async().add_done_callback(
            lambda f, id=goal_id: self.on_nav_result(f, id))

    def on_nav_result(self, future, goal_id):
        """
        Called when the NavigateToPose action finishes (success, cancel, or failure).
        Results from superseded goals are silently ignored.

        Nav2 status codes:
          4 = SUCCEEDED   robot reached the goal
          5 = CANCELED    goal was cancelled (by Unity or a new goal)
          6 = ABORTED     Nav2 aborted due to an internal error
        """
        if goal_id != self._active_id:
            self.get_logger().debug(
                f'[nav] Result for goal (id={goal_id}) ignored — no longer the active goal.')
            return

        self._nav_handle = None
        self._active_id  = -1
        status = future.result().status

        if status == 4:
            self.get_logger().info(f'[nav] Goal (id={goal_id}) SUCCEEDED — robot reached the goal.')
        elif status == 5:
            self.get_logger().info(f'[nav] Goal (id={goal_id}) CANCELED.')
        elif status == 6:
            self.get_logger().error(
                f'[nav] Goal (id={goal_id}) ABORTED by Nav2 (e.g. stuck, planner failure).')
        else:
            self.get_logger().warn(f'[nav] Goal (id={goal_id}) ended with unknown status: {status}.')

    # ──────────────────────────────────────────────────────────────────────────
    # Cancel
    # ──────────────────────────────────────────────────────────────────────────

    def on_cancel(self, msg: Bool):
        """Cancels the active NavigateToPose goal on request from Unity."""
        if not msg.data:
            return

        if self._nav_handle is None:
            self.get_logger().warn('[nav] Cancel received but no navigation is currently active.')
            return

        self.get_logger().info('[nav] Cancel requested by Unity — aborting active goal.')
        self._active_id = -1   # invalidate immediately so on_nav_result ignores the callback
        self._nav_handle.cancel_goal_async()
        self._nav_handle = None


def main():
    rclpy.init()
    node = PathBridge()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == '__main__':
    main()
