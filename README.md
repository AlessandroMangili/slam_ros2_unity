# SLAM ROS2 Unity — Search & Rescue Teleoperation System

> A Unity + ROS 2 project developed for the Virtual Reality course, simulating a search and rescue operation inside a burning building using a teleoperated mobile robot.


https://github.com/user-attachments/assets/676c7c5f-774f-4a78-8edb-0c1997646d2d


## Table of Contents

- [Overview](#overview)
- [System Architecture](#system-architecture)
- [Robot & Environment](#robot--environment)
- [ROS 2 Integration](#ros-2-integration)
- [Sensor Stack](#sensor-stack)
- [Navigation & Path Planning](#navigation--path-planning)
- [HUD & User Interface](#hud--user-interface)
- [Communication Latency](#communication-latency)
- [Setup & Launch](#setup--launch)
- [Future Work](#future-work)

## Overview

This project simulates a **first-responder teleoperation scenario**: an operator controls a ground robot navigating the interior of a warehouse on fire. The operator has no prior knowledge of the building's internal layout. Using onboard cameras, LiDAR, and ROS 2-based SLAM, the robot reconstructs the environment in real time and displays the computed path to reach the selected mission goal.

The system is built on the **Unity game engine** as the simulation and visualization frontend, with **ROS 2 (Humble)** handling all robotics computation on the backend. Communication between the two is managed via the **ROS-TCP-Connector** package.

| <img width="1088" height="572" alt="Screenshot from 2026-04-10 11-35-21" src="https://github.com/user-attachments/assets/20662602-8cbb-4cc7-a4d4-9f2fc6ad8933" /> |
|:---:|
| *Mission A active: the green path computed by Nav2 is displayed in real time overlaid on the scene* |

## System Architecture

```
┌─────────────────────────────────────┐        ┌──────────────────────────────────┐
│         Unity (Frontend)            │        │         ROS 2 (Backend)          │
│                                     │        │                                  │
│  MissionMenuManager (mission choose)│        │  ros_tcp_endpoint                │
│  PathArrowGuide     (static arrows) │        │  slam_toolbox    (mapping)       │
│  ROSPathRequester   (goal publisher)│◄──────►│  nav2_bringup    (path planning) │
│  ROSPathVisualizer  (line renderer) │        │  path_bridge.py  (bridge for nav)│
│  RadarDisplay       (radar display) │        │  /unity/path_goal topic          │
│  CameraBlending     (front and back)│        │  /unity/path_result topic        │
│  OdometryPublisher  (odom publish)  │        │  /odom, /map, /plan topics       │
└─────────────────────────────────────┘        └──────────────────────────────────┘
```

The bridge between Unity and ROS 2 is a custom Python node (`path_bridge.py`) that subscribes to the goal pose published by Unity, calls the Nav2 `ComputePathToPose` action server, and republishes the resulting path back to Unity on a simple topic.

## Robot & Environment

### Robot — TurtleBot3 Waffle

The robot model used is the **TurtleBot3 Waffle**, imported into Unity as a prefab. It is equipped with:

- 4 cameras (front, rear, left, right)
- 2 wheels
- 2 castors
- A simulated LiDAR (360°)
- Odometry publisher

### Environment — Warehouse

The scene is a warehouse interior featuring:

- Floors, walls, shelves, barrels, and crates — all with **MeshCollider** components for physically accurate LiDAR ray interaction
- Three mission goal positions (Exit, Shelves, Barrels)
- Static arrow guides placed along pre-authored waypoint paths

| <img width="1088" height="572" alt="Screenshot from 2026-04-10 11-04-23" src="https://github.com/user-attachments/assets/e38cd154-636f-4ab5-a0c7-a0fecc355fa4" /> |
|:---:|
| *Mission selection menu shown to the operator at startup* |

## ROS 2 Integration

Communication between Unity and ROS 2 is handled via **ROS-TCP-Connector** (`com.unity.robotics.ros-tcp-connector`), which establishes a WebSocket bridge to a running `ros_tcp_endpoint` node on the ROS 2 machine.

### Key topics

| Topic | Direction | Type | Description |
|---|---|---|---|
| `/cmd_vel` | ROS&nbsp;→&nbsp;Unity | `Twist` | Teleoperation velocity commands |
| `/odom` | Unity&nbsp;→&nbsp;ROS | `Odometry` | Robot pose published from Unity |
| `/scan` | Unity&nbsp;→&nbsp;ROS | `LaserScan` | LiDAR scan data |
| `/camera_front`, `/camera_rear`, `/camera_left`, `/camera_right` | Unity → ROS | `Image` | Camera feeds |
| `/point_cloud` | Unity&nbsp;→&nbsp;ROS | `PointCloud2` | LiDAR point cloud |
| `/map` | ROS&nbsp;→&nbsp;Unity | `OccupancyGrid` | SLAM map for minimap display |
| `/unity/path_goal` | Unity&nbsp;→&nbsp;ROS | `PoseStamped` | Goal pose for path computation |
| `/unity/path_result` | ROS&nbsp;→&nbsp;Unity | `Path` | Computed path from Nav2 |

## Sensor Stack

### Cameras

Four cameras surround the robot for complete situational awareness. In the HUD:

- **Front/rear cameras** alternate using alpha blending based on the current movement command — the front camera is shown when moving forward, the rear when reversing.
- **Left and right cameras** are shown in fixed side panels.

### LiDAR

A 360° LiDAR scans the environment and publishes on `/point_cloud`. The data feeds:

1. **SLAM Toolbox** — for real-time map construction
2. **Nav2 costmaps** — for obstacle-aware path planning
3. **Radar HUD** — for proximity visualization

## Navigation & Path Planning

### SLAM — `slam_toolbox`

The `online_async` mode of slam_toolbox is used to build the occupancy map incrementally as the robot explores. The operator starts with zero knowledge of the building interior; the map is constructed entirely from LiDAR data during the mission.

### Path Planning — Nav2 `ComputePathToPose`

When the operator selects a mission, Unity publishes the goal pose (last waypoint of the selected mission) to `/unity/path_goal`. The `path_bridge.py` node intercepts this, calls the Nav2 `ComputePathToPose` action server, and publishes the resulting path to `/unity/path_result`.

Unity subscribes to `/unity/path_result` and renders the path as a **green LineRenderer** on the floor of the scene, updated every ~1.5 seconds as the robot moves.

```
Operator selects mission
        │
        ▼
Unity publishes PoseStamped → /unity/path_goal
        │
        ▼
path_bridge.py calls ComputePathToPose action
        │
        ▼
Nav2 computes path using LiDAR costmap + SLAM map
        │
        ▼
path_bridge.py publishes nav_msgs/Path → /unity/path_result
        │
        ▼
Unity LineRenderer draws path on scene floor (live update)
```

> **Note:** Since this is a teleoperation system, autonomous navigation is not enabled. The robot is always driven manually by the operator; Nav2 is used only for path *visualization*, not execution.

### Static Arrow Guides

In addition to the live Nav2 path, a set of **static green arrows** is pre-placed along authored waypoints for each mission. These provide a stable reference guide independent of the map state, and are always visible once a mission is selected.

| <img width="1088" height="572" alt="Screenshot from 2026-04-10 10-57-18" src="https://github.com/user-attachments/assets/e4620862-9316-45e4-b55d-c23c24c3c7b9" /> |
|:---:|
| *"Robot has reached the goal!" message displayed when the robot arrives at the mission destination* |

## HUD & User Interface

The operator's HUD (displayed in the XR headset) includes:

### Radar (top-left)
A circular radar display showing obstacles detected by LiDAR in real time. Points are color-coded by proximity: **green → yellow → orange → red** as distance decreases. Point size also scales with proximity for immediate visual feedback.

### Camera panels (top-center)
Three camera feeds displayed simultaneously:
- **Center**: front/rear blend (switches automatically with movement direction)
- **Left and right**: side cameras

### Minimap (top-right)
The occupancy map built by SLAM Toolbox, updated in real time via the `/map` topic.

### Mission panel (bottom-right)
- Current mission name and goal distance in meters
- Button to open the mission selection menu

### Path visualization (in-scene)
- **Static green arrows** along pre-authored waypoints
- **Dynamic green line** (LineRenderer) showing the Nav2-computed path, refreshed every 1.5 s

## Communication Latency

A latency measurement script was implemented to measure the round-trip delay between Unity issuing a command and the robot responding. This allows the operator to monitor control lag and account for it during teleoperation in high-stakes scenarios.

## Setup & Launch

### Prerequisites

- ROS 2 Humble
- `slam_toolbox`, `nav2_bringup`, `ros_tcp_endpoint` installed
- Unity 2022.x with `com.unity.robotics.ros-tcp-connector` package

### Launch order

```bash
# 1. Start SLAM + Nav2
ros2 launch ros2_navigation slam_navigation.launch.py use_sim_time:=false

# 2. Start ROS-TCP bridge
ros2 run ros_tcp_endpoint default_server_endpoint \
  --ros-args -p ROS_IP:=<ROS_PC_IP><img width="1088" height="572" alt="Screenshot from 2026-04-10 11-04-23" src="https://github.com/user-attachments/assets/4c8f9c4a-4b3b-418f-aebc-6d7148fd5352" />


# 3. Start path bridge
python3 path_bridge.py

# 4. Press Play in Unity
```

### Teleoperation

```bash
# Control the robot from keyboard
ros2 run teleop_twist_keyboard teleop_twist_keyboard
```

## Future Work

- **Autonomous navigation mode**: a toggle in the HUD would allow the operator to hand off control to Nav2, enabling fully autonomous goal-reaching using the `NavigateToPose` action. The operator could re-enable manual control at any time via a flag.
- **Multi-robot support**: extend the system to coordinate multiple robots exploring different sections of the building simultaneously.
- **Victim detection**: integrate an object detection model on the camera feeds to highlight potential survivors on the map.
