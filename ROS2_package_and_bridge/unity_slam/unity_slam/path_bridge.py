import rclpy
from rclpy.node import Node
from rclpy.action import ActionClient
from geometry_msgs.msg import PoseStamped
from nav_msgs.msg import Path
from nav2_msgs.action import ComputePathToPose, NavigateToPose
from std_msgs.msg import Bool, Header          # ← aggiunto Header


class PathBridge(Node):
    def __init__(self):
        super().__init__('unity_path_bridge')

        # ── Action clients ────────────────────────────────────────────────────
        self._compute_client = ActionClient(
            self, ComputePathToPose, 'compute_path_to_pose')
        self._nav_client = ActionClient(
            self, NavigateToPose, 'navigate_to_pose')

        # ── Publishers ────────────────────────────────────────────────────────
        self._path_pub = self.create_publisher(Path,   '/unity/path_result', 10)
        self._pong_pub = self.create_publisher(Header, '/unity/pong',        10)  # ← nuovo

        # ── Subscriptions ─────────────────────────────────────────────────────
        self.create_subscription(
            PoseStamped, '/unity/path_goal',
            self.on_path_goal, 10)
        self.create_subscription(
            PoseStamped, '/unity/path_goal_visual',
            self.on_path_goal, 10)
        self.create_subscription(
            PoseStamped, '/unity/nav_goal',
            self.on_nav_goal, 10)
        self.create_subscription(
            Bool, '/unity/cancel_nav',
            self.on_cancel, 10)
        self.create_subscription(                      # ← nuovo
            Header, '/unity/ping',
            self.on_ping, 10)

        # ── Stato interno ─────────────────────────────────────────────────────
        self._compute_busy = False
        self._nav_handle   = None
        self._nav_goal_id  = 0
        self._active_id    = -1

        self.get_logger().info('PathBridge avviato.')

    # ──────────────────────────────────────────────────────────────────────────
    # Ping / Pong  (latency measurement)
    # Eco immediato: ri-pubblica il messaggio intatto su /unity/pong.
    # Unity confronta il timestamp originale con l'ora corrente → RTT.
    # ──────────────────────────────────────────────────────────────────────────

    def on_ping(self, msg: Header):
        self._pong_pub.publish(msg)

    # ──────────────────────────────────────────────────────────────────────────
    # Path visivo
    # ──────────────────────────────────────────────────────────────────────────

    def on_path_goal(self, msg: PoseStamped):
        if self._compute_busy:
            return
        self._compute_busy = True

        if not self._compute_client.wait_for_server(timeout_sec=1.0):
            self.get_logger().warn('compute_path_to_pose non disponibile')
            self._compute_busy = False
            return

        goal = ComputePathToPose.Goal()
        goal.goal       = msg
        goal.use_start  = False
        goal.planner_id = ''

        future = self._compute_client.send_goal_async(goal)
        future.add_done_callback(self.on_compute_accepted)

    def on_compute_accepted(self, future):
        handle = future.result()
        if not handle.accepted:
            self._compute_busy = False
            return
        handle.get_result_async().add_done_callback(self.on_compute_result)

    def on_compute_result(self, future):
        result = future.result().result
        if result.path.poses:
            self._path_pub.publish(result.path)
            self.get_logger().info(f'Path visivo: {len(result.path.poses)} pose')
        else:
            self.get_logger().warn('Path visivo vuoto.')
        self._compute_busy = False

    # ──────────────────────────────────────────────────────────────────────────
    # Navigazione autonoma
    # ──────────────────────────────────────────────────────────────────────────

    def on_nav_goal(self, msg: PoseStamped):
        self._nav_goal_id += 1
        my_id = self._nav_goal_id

        if self._nav_handle is not None:
            self.get_logger().info(
                f'Nuovo goal (id={my_id}): cancello navigazione precedente...')
            self._nav_handle.cancel_goal_async()
            self._nav_handle = None

        if not self._nav_client.wait_for_server(timeout_sec=2.0):
            self.get_logger().warn('navigate_to_pose non disponibile')
            return

        goal = NavigateToPose.Goal()
        goal.pose = msg

        self.get_logger().info(
            f'Navigazione (id={my_id}) verso '
            f'({msg.pose.position.x:.2f},{msg.pose.position.y:.2f})')

        future = self._nav_client.send_goal_async(goal)
        future.add_done_callback(lambda f, id=my_id: self.on_nav_accepted(f, id))

    def on_nav_accepted(self, future, goal_id):
        handle = future.result()
        if not handle.accepted:
            self.get_logger().warn(f'Goal (id={goal_id}) rifiutato.')
            return

        if goal_id == self._nav_goal_id:
            self._nav_handle = handle
            self._active_id  = goal_id
            self.get_logger().info(f'Goal (id={goal_id}) accettato e attivo.')
        else:
            self.get_logger().info(f'Goal (id={goal_id}) superato, cancello.')
            handle.cancel_goal_async()
            return

        handle.get_result_async().add_done_callback(
            lambda f, id=goal_id: self.on_nav_result(f, id))

    def on_nav_result(self, future, goal_id):
        if goal_id != self._active_id:
            self.get_logger().info(
                f'Risultato goal (id={goal_id}) ignorato — non più attivo.')
            return

        self._nav_handle = None
        self._active_id  = -1
        status = future.result().status

        if status == 4:
            self.get_logger().info(f'Navigazione (id={goal_id}) completata.')
        elif status == 5:
            self.get_logger().info(f'Navigazione (id={goal_id}) cancellata.')
        else:
            self.get_logger().warn(
                f'Navigazione (id={goal_id}) terminata con status: {status}')

    # ──────────────────────────────────────────────────────────────────────────
    # Cancel
    # ──────────────────────────────────────────────────────────────────────────

    def on_cancel(self, msg: Bool):
        if not msg.data:
            return

        if self._nav_handle is None:
            self.get_logger().warn('Nessuna navigazione attiva da cancellare.')
            return

        self.get_logger().info('Cancellazione navigazione...')
        self._active_id = -1
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
