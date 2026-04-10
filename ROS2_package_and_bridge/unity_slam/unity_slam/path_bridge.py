import rclpy
from rclpy.node import Node
from rclpy.action import ActionClient
from geometry_msgs.msg import PoseStamped
from nav_msgs.msg import Path
from nav2_msgs.action import ComputePathToPose

class PathBridge(Node):
    def __init__(self):
        super().__init__('unity_path_bridge')

        self._action_client = ActionClient(
            self, ComputePathToPose, 'compute_path_to_pose')

        self._path_pub = self.create_publisher(
            Path, '/unity/path_result', 10)

        self.create_subscription(
            PoseStamped, '/unity/path_goal', self.on_goal, 10)

        self._busy = False
        self.get_logger().info('PathBridge avviato, in attesa di goal da Unity...')

    def on_goal(self, msg: PoseStamped):
        if self._busy:
            return  # scarta se la richiesta precedente è ancora in corso

        if not self._action_client.wait_for_server(timeout_sec=1.0):
            self.get_logger().warn('Action server compute_path_to_pose non disponibile!')
            return

        self._busy = True
        goal = ComputePathToPose.Goal()
        goal.goal      = msg
        goal.use_start = False
        goal.planner_id = ''

        self.get_logger().info('Invio goal a Nav2...')
        future = self._action_client.send_goal_async(goal)
        future.add_done_callback(self.on_goal_accepted)

    def on_goal_accepted(self, future):
        handle = future.result()
        if not handle.accepted:
            self.get_logger().warn('Goal rifiutato da Nav2.')
            self._busy = False
            return
        handle.get_result_async().add_done_callback(self.on_result)

    def on_result(self, future):
        result = future.result().result
        if result.path.poses:
            self.get_logger().info(
                f'Path ricevuto: {len(result.path.poses)} pose → pubblico su /unity/path_result')
            self._path_pub.publish(result.path)
        else:
            self.get_logger().warn('Path vuoto restituito da Nav2.')
        self._busy = False

def main():
    rclpy.init()
    node = PathBridge()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()

if __name__ == '__main__':
    main()
