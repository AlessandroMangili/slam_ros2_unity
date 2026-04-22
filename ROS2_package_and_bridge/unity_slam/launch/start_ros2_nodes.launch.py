import os
import subprocess
from launch import LaunchDescription
from launch.actions import ExecuteProcess, TimerAction
from launch_ros.actions import Node
from launch.actions import IncludeLaunchDescription
from launch.launch_description_sources import PythonLaunchDescriptionSource
from ament_index_python.packages import get_package_share_directory


def generate_launch_description():

    nav2_pkg = get_package_share_directory('ros2_navigation')

    # 1. Static transform publisher: base_link → base_scan
    static_tf = Node(
        package='tf2_ros',
        executable='static_transform_publisher',
        name='static_tf_base_scan',
        arguments=['0', '0', '0.1', '0', '0', '0', 'base_link', 'base_scan'],
        output='screen'
    )

    # 2. ROS TCP Endpoint
    ros_tcp = Node(
        package='ros_tcp_endpoint',
        executable='default_server_endpoint',
        name='ros_tcp_endpoint',
        parameters=[{
            'ROS_IP': '127.0.0.1',
            'ROS_TCP_PORT': 10000
        }],
        output='screen'
    )

    # 3. Mapping launch — in a new gnome-terminal tab
    mapping_terminal = ExecuteProcess(
        cmd=[
            'gnome-terminal', '--tab',
            '--title=MAPPING',
            '--',
            'bash', '-c',
            f'ros2 launch ros2_navigation mapping.launch.py; exec bash'
        ],
        output='screen'
    )

    # 4. Navigation launch — in a new gnome-terminal tab (delayed to let mapping start)
    navigation_terminal = TimerAction(
        period=5.0,
        actions=[
            ExecuteProcess(
                cmd=[
                    'gnome-terminal', '--tab',
                    '--title=NAVIGATION',
                    '--',
                    'bash', '-c',
                    f'ros2 launch ros2_navigation navigation.launch.py; exec bash'
                ],
                output='screen'
            )
        ]
    )

    # 5. Path bridge — in a new gnome-terminal tab (delayed after navigation)
    path_bridge_terminal = TimerAction(
        period=10.0,
        actions=[
            ExecuteProcess(
                cmd=[
                    'gnome-terminal', '--tab',
                    '--title=PATH BRIDGE',
                    '--',
                    'bash', '-c',
                    'ros2 run unity_slam path_bridge; exec bash'
                ],
                output='screen'
            )
        ]
    )

    # 6. Teleoperation — in a new gnome-terminal tab (delayed after bridge)
    teleop_terminal = TimerAction(
        period=12.0,
        actions=[
            ExecuteProcess(
                cmd=[
                    'gnome-terminal', '--tab',
                    '--title=TELEOPERATION',
                    '--',
                    'bash', '-c',
                    'ros2 run teleop_twist_keyboard teleop_twist_keyboard; exec bash'
                ],
                output='screen'
            )
        ]
    )

    return LaunchDescription([
        static_tf,
        ros_tcp,
        mapping_terminal,
        navigation_terminal,
        path_bridge_terminal,
        teleop_terminal,
    ])
