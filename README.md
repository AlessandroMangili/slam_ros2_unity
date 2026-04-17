# SLAM ROS2 Unity — Search & Rescue Teleoperation and Autonomous Navigation System

> A Unity + ROS 2 project developed for the Virtual Reality course of my Master's in Robotics Engineering.
> The system simulates a search-and-rescue mission inside a burning building, where an operator can either teleoperate a ground robot or switch to autonomous navigation directly from the XR HUD.

https://github.com/user-attachments/assets/29792f46-3e6c-4c65-98ff-3afa90b438d4

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

The operator can drive the robot manually or hand off control to Nav2 at any time using a toggle in the HUD — without interrupting the mission or restarting the navigation stack.

| <img width="1088" height="572" alt="navigation" src="https://github.com/user-attachments/assets/6c4f8776-5f63-4d20-8b07-4791f30d692a" /> |
|:---:|
| *Mission A active: the green path computed by Nav2 is displayed in real time overlaid on the scene* |

## System Architecture

```
┌─────────────────────────────────────┐        ┌──────────────────────────────────┐
│         Unity (Frontend)            │        │         ROS 2 (Backend)          │
│                                     │        │                                  │
│  MissionMenuManager (mission select)│        │  ros_tcp_endpoint                │
│  PathArrowGuide     (static arrows) │        │  slam_toolbox    (mapping)       │
│  ROSPathRequester   (goal publisher)│        │  nav2_bringup    (path planning) │
│  ROSPathVisualizer  (line renderer) │        │  path_bridge.py  (bridge)        │
│  ROSNavigator       (nav2 goal)     │◄──────►│  /unity/path_goal topic          │
│  RadarDisplay       (radar display) │        │  /unity/path_result topic        │
│  CameraBlending     (front/rear)    │        │  /unity/nav_goal topic           │
│  OdometryPublisher  (odom publish)  │        │  /unity/cancel_nav topic         │
│  RobotMapMarker     (map overlay)   │        │  /odom, /map, /scan topics       │
│  ROSLatencyMonitor  (RTT display)   │        │                                  │
└─────────────────────────────────────┘        └──────────────────────────────────┘
```

The bridge between Unity and ROS 2 is handled by a custom Python node (`path_bridge.py`). It receives goal poses from Unity, communicates with Nav2, and sends back either the computed path or navigation state updates depending on the selected mode. It also handles latency measurement via a ping/pong echo mechanism.

## Robot & Environment

### Robot — TurtleBot3 Waffle

The robot model used is the **TurtleBot3 Waffle**, imported into Unity as a prefab. It is equipped with:

- 4 cameras (front, rear, left, right)
- 2 wheels and 2 castors
- A simulated 360° LiDAR
- Odometry publisher

### Environment — Warehouse

The scene is a warehouse interior featuring:

- Floors, walls, shelves, barrels, and crates — all with **MeshCollider** components for physically accurate LiDAR ray interaction
- Three mission goal positions (Exit, Shelves, Barrels)
- Static arrow guides placed along pre-authored waypoint paths

| <img width="1088" height="572" alt="menu" src="https://github.com/user-attachments/assets/10323e07-2197-40c1-bb99-9242aca92163" /> |
|:---:|
| *Mission selection menu shown to the operator at startup* |

## Navigation Modes

### Teleoperation Mode

In teleoperation mode, the operator drives the robot manually using keyboard input or controller commands published to `/cmd_vel`.

At the same time, the system continuously computes and visualizes the optimal path to the selected mission goal using Nav2's `ComputePathToPose`. This gives the operator live navigation feedback while preserving full manual control.

### Autonomous Mode

A toggle in the HUD enables autonomous navigation. When activated:

1. Unity publishes the mission goal as a `PoseStamped` to `/unity/nav_goal`.
2. `path_bridge.py` forwards it to the Nav2 `NavigateToPose` action server.
3. Nav2 takes control of the robot's motion and drives it autonomously to the goal.
4. The path is replanned continuously as the SLAM map evolves.

The operator can switch back to teleoperation at any time by toggling the HUD switch. This immediately sends a cancel signal to the bridge, which aborts the active `NavigateToPose` goal and restores manual control without restarting any node.

The toggle can also be flipped **mid-mission**: if autonomous mode is enabled after a mission is already active, navigation starts from the robot's current position without requiring a mission reselection.

To avoid interference between the visual path (`ComputePathToPose`) and the active navigation goal (`NavigateToPose`), the two use **separate ROS topics**: `/unity/path_goal_visual` for the visual path in autonomous mode, and `/unity/path_goal` in teleoperation mode.

### Shared Behavior in Both Modes

In both modes, the operator always sees:

- The live SLAM map with the robot's position and orientation overlaid
- The current goal distance in metres
- The mission selection state
- The computed route rendered on the scene floor
- The current RTT communication latency

## ROS 2 Integration

Communication between Unity and ROS 2 is handled via **ROS-TCP-Connector** (`com.unity.robotics.ros-tcp-connector`), which establishes a TCP bridge to a running `ros_tcp_endpoint` node on the ROS 2 machine.

### Key topics

| Topic | Direction | Type | Description |
|---|---|---|---|
| `/cmd_vel` | ROS → Unity | `Twist` | Teleoperation or Nav2 velocity commands |
| `/odom` | Unity → ROS | `Odometry` | Robot pose published from Unity physics |
| `/tf` | Unity → ROS | `TFMessage` | `odom → base_link` transform |
| `/scan` | Unity → ROS | `LaserScan` | 2D LiDAR slice for Nav2 costmaps |
| `/point_cloud` | Unity → ROS | `PointCloud2` | Full 3D LiDAR point cloud |
| `/map` | ROS → Unity | `OccupancyGrid` | SLAM map for minimap display |
| `/unity/path_goal` | Unity → ROS | `PoseStamped` | Goal for visual path (teleop mode) |
| `/unity/path_goal_visual` | Unity → ROS | `PoseStamped` | Goal for visual path (autonomous mode) |
| `/unity/path_result` | ROS → Unity | `Path` | Computed path from Nav2 |
| `/unity/nav_goal` | Unity → ROS | `PoseStamped` | Autonomous navigation goal |
| `/unity/cancel_nav` | Unity → ROS | `Bool` | Cancels the active Nav2 goal |
| `/unity/ping` | Unity → ROS | `Header` | Latency measurement ping |
| `/unity/pong` | ROS → Unity | `Header` | Latency measurement pong (echo) |

## Sensor Stack

### Cameras

Four cameras surround the robot for complete situational awareness. In the HUD:

- **Front/rear cameras** alternate using alpha blending based on the current movement direction — the front camera is shown when moving forward, the rear when reversing.
- **Left and right cameras** are shown in fixed side panels.

### LiDAR

A 360° LiDAR scans the environment via raycasting and publishes on both `/scan` and `/point_cloud`. The data feeds:

1. **SLAM Toolbox** — for real-time map construction
2. **Nav2 costmaps** — for obstacle-aware path planning
3. **Radar HUD** — for proximity visualization

## Navigation & Path Planning

### SLAM — `slam_toolbox`

The `online_async` mode of slam_toolbox is used to build the occupancy map incrementally as the robot explores. The operator starts with zero knowledge of the building interior; the map is constructed entirely from LiDAR data during the mission.

### Path Planning — Nav2

When the operator selects a mission, Unity publishes the goal pose to the appropriate topic based on the active mode. The `path_bridge.py` node intercepts the request and interacts with Nav2 accordingly.

```text
Operator selects mission
        │
        ▼
Unity publishes PoseStamped
        │
        ├── Teleop mode ──► /unity/path_goal
        │                        │
        │                        ▼
        │               ComputePathToPose (visual only)
        │                        │
        │                        ▼
        │               path_bridge.py → /unity/path_result
        │                        │
        │                        ▼
        │               LineRenderer draws path on scene floor
        │
        └── Autonomous mode ──► /unity/nav_goal
                                     │
                                     ▼
                            NavigateToPose (robot moves)
                                     │
                                     ├── /unity/path_goal_visual
                                     │   → ComputePathToPose (visual only,
                                     │     separate from nav goal)
                                     │
                                     ▼
                            Nav2 drives robot, replans on map updates
                                     │
                                     ▼
                            on success → Unity shows "Robot has reached the goal!"
                            on cancel  → robot stops, teleop restored
```

> **Note:** In autonomous mode, the visual path and the navigation goal use separate topics so that `ComputePathToPose` requests do not interfere with the active `NavigateToPose` action.

### Static Arrow Guides

In addition to the live Nav2 path, a set of **static green arrows** is pre-placed along authored waypoints for each mission. These provide a stable reference guide independent of the map state, and are always visible once a mission is selected.

| <img width="1088" height="572" alt="goal" src="https://github.com/user-attachments/assets/3a830ff9-2507-4314-adb3-99a9d79b8308" /> |
|:---:|
| *"Robot has reached the goal!" message displayed when the robot arrives at the mission destination* |

## HUD & User Interface

The operator's HUD (displayed in the XR headset) includes:

### Radar (top-left)
A circular radar display showing obstacles detected by LiDAR in real time. Points are color-coded by proximity: **red → yellow** as distance increases. Point size also scales with proximity for immediate visual feedback.

### Camera panels (top-center)
Three camera feeds displayed simultaneously:
- **Center**: front/rear blend (switches automatically based on movement direction)
- **Left and right**: side cameras

### Minimap (top-right)
The occupancy map built by SLAM Toolbox, updated in real time via the `/map` topic. The **robot's current position and heading** are overlaid on the map as a red dot with an orange directional arrow, projected from the Unity world transform into map UV space at every frame.

### Mission panel (bottom-right)
- Current mission name and goal distance in metres
- Autonomous navigation toggle (ON/OFF)
- Button to open the mission selection menu

### Path visualization (in-scene)
- **Static green arrows** along pre-authored waypoints
- **Dynamic green line** (LineRenderer) showing the Nav2-computed path, refreshed every ~1.5 s as the robot moves

## Communication Latency

A dedicated latency monitor measures the **Round-Trip Time (RTT)** between Unity and ROS 2 using a ping/pong mechanism:

1. Unity publishes a `HeaderMsg` on `/unity/ping` every 500 ms.
2. `path_bridge.py` echoes it immediately on `/unity/pong`.
3. Unity measures the elapsed time with `System.Diagnostics.Stopwatch` (hardware performance counter, sub-microsecond resolution).

The RTT and estimated one-way latency (`RTT / 2`) are displayed live in the HUD. The measurement is independent of clock synchronisation between the two machines, making it reliable on any network setup. An Exponential Moving Average smooths out occasional spikes.

## Setup & Launch

### Prerequisites

- ROS 2 Humble with `slam_toolbox`, `nav2_bringup`, `ros_tcp_endpoint`, `teleop_twist_keyboard` installed
- Unity 6 with `com.unity.robotics.ros-tcp-connector` package
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

### 5. Operate the robot

**Teleoperation:** switch to the **TELEOPERATION** terminal and use the keyboard:

```
u i o
j k l
m , .
```

**Autonomous navigation:** select a mission from the Unity HUD and enable the **Auto Navigation** toggle. The robot will navigate to the goal independently. Toggle it off at any time to resume manual control.

## Future Work

- **Multi-robot support**: extend the system to coordinate multiple robots exploring different sections of the building simultaneously.
- **Victim detection**: integrate an object detection model on the camera feeds to highlight potential survivors on the map.
- **VR headset integration**: deploy the HUD on a physical XR headset for immersive first-person teleoperation.
