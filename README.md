# SLAM ROS2 Unity —  Search & Rescue Teleoperation and Autonomous Navigation System

> A Unity + ROS 2 project developed for the Virtual Reality course of my Master's in Robotics Engineering.
> The system simulates a search-and-rescue mission inside a burning building, where an operator can either teleoperate a ground robot or switch to autonomous navigation directly from the XR HUD.

https://github.com/user-attachments/assets/499b664c-44c8-46c0-ac39-f7f07ecb1f09

## Table of Contents
- [Overview](#overview)
- [System Architecture](#system-architecture)
- [Robot & Environment](#robot--environment)
- [Navigation Modes](#navigation-modes)
- [ROS 2 Integration](#ros-2-integration)
- [Sensor Stack](#sensor-stack)
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

The bridge between Unity and ROS 2 is handled by a custom Python node (`path_bridge.py`). It receives goal poses from Unity, communicates with Nav2, and sends back either the computed path or navigation state updates depending on the selected mode.

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

## Navigation Modes

### Teleoperation Mode

In teleoperation mode, the operator drives the robot manually using keyboard input or controller commands published to `/cmd_vel`.

At the same time, the system continuously computes and visualizes the optimal path to the selected mission goal using Nav2’s `ComputePathToPose`. This gives the operator live navigation feedback while preserving full manual control.

### Autonomous Mode

A toggle in the HUD enables autonomous navigation. When this mode is activated, the robot switches from manual driving to Nav2-based autonomy and navigates independently to the selected goal using `NavigateToPose`.

The navigation is dynamically replanned in real time as the SLAM map evolves. The operator can take back control at any time by switching the toggle back to teleoperation mode.

### Shared Behavior in Both Modes

In both modes, the operator always sees:

* the live SLAM map,
* the current goal distance,
* the mission selection state,
* and the computed route in the scene.

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

### Path Planning — Nav2 `ComputePathToPose` and `NavigateToPose`

When the operator selects a mission, Unity publishes the goal pose (last waypoint of the selected mission) to `/unity/path_goal`. The `path_bridge.py` node intercepts this request and interacts with Nav2 depending on the selected operation mode.

In **teleoperation mode**, the bridge calls the Nav2 `ComputePathToPose` action server and publishes the resulting path to `/unity/path_result`.

In **autonomous mode**, the same goal is forwarded to the Nav2 `NavigateToPose` action server, allowing the robot to move independently toward the selected mission target while continuously replanning based on the evolving SLAM map.

Unity subscribes to `/unity/path_result` and renders the path as a **green LineRenderer** on the floor of the scene, updated every ~1.5 seconds as the robot moves.

```text
Operator selects mission
        │
        ▼
Unity publishes PoseStamped → /unity/path_goal
        │
        ▼
path_bridge.py checks operation mode
        │
        ├── Teleop mode ──► ComputePathToPose
        │                  │
        │                  ▼
        │          Nav2 computes path using LiDAR costmap + SLAM map
        │                  │
        │                  ▼
        │          path_bridge.py publishes nav_msgs/Path → /unity/path_result
        │                  │
        │                  ▼
        │          Unity LineRenderer draws path on scene floor
        │
        └── Autonomous mode ─► NavigateToPose
                           │
                           ▼
                   Nav2 autonomously drives the robot to the goal
                           │
                           ▼
                   Robot replans in real time as the map updates
```

> **Note:** In teleoperation mode, the operator drives the robot manually and Nav2 is used for path visualization only. In autonomous mode, Nav2 takes control of motion through `NavigateToPose`, and manual control can be restored at any time through the HUD toggle.

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

- ROS 2 Humble with `slam_toolbox`, `nav2_bringup`, `ros_tcp_endpoint`, `teleop_twist_keyboard` installed
- Unity 2022.x with `com.unity.robotics.ros-tcp-connector` package
- All ROS 2 packages placed inside a colcon workspace

### 1. Build the ROS 2 workspace

```bash
cd <your_ws>
colcon build
source install/setup.bash
```

### 2. Open Unity

Open the project and load **Scene 2**. Do not press Play yet.

### 3. Launch everything from ROS 2

A single launch file starts all required nodes and opens dedicated terminals automatically:

```bash
ros2 launch unity_slam start.launch.py
```

This will open the following terminals in sequence:

| Terminal | Content | Delay |
|:---|:---|:---:|
| Main process | Static TF publisher + ROS-TCP endpoint | 0s |
| MAPPING | `slam_toolbox` online async mapping | 0s |
| NAVIGATION | Nav2 navigation stack | 5s |
| PATH BRIDGE | `path_bridge` node (Unity ↔ Nav2) | 10s |
| TELEOPERATION | `teleop_twist_keyboard` | 12s |

### 4. Press Play in Unity

Once all terminals are running and Nav2 is ready, press **Play** in the Unity Editor. The ROS-TCP connection will be established automatically.

### 5. Teleoperate the robot

Switch to the **TELEOPERATION** terminal and use the keyboard to drive the robot:

```
u i o
j k l
m , .
```

Select a mission from the Unity HUD — the Nav2 path will appear in the scene in real time.

## Future Work

- **Multi-robot support**: extend the system to coordinate multiple robots exploring different sections of the building simultaneously.
- **Victim detection**: integrate an object detection model on the camera feeds to highlight potential survivors on the map.


