# Human Motion Video to Unity 2D Character Animation

A three-stage pipeline that converts human motion video into Unity 2D character animation through pose estimation, motion stabilization, and character rig mapping.

This repository contains the Unity mapping and character-driving module developed as part of a three-person university project.

## Results

The following animations demonstrate the Unity output using two different 2D characters.

### Character Animated in Place

| Bee Character | Human Character |
|:---:|:---:|
| ![Bee character animated in place](docs/Bee_after_smooth.gif) | ![Human character animated in place](docs/Human_after_smooth.gif) |

### Character Following the Skeleton Movement

| Bee Character | Human Character |
|:---:|:---:|
| ![Bee character following the skeleton movement](docs/Move_Bee_after_smooth.gif) | ![Human character following the skeleton movement](docs/Move_Human_after_smooth.gif) |

## Project Presentation

[▶ Watch the project presentation](docs/project-video.mp4)

The presentation explains the complete project pipeline, including pose estimation, motion stabilization, and Unity 2D character driving.

## System Pipeline

```text
Human Motion Video
        ↓
Pose Estimation
        ↓
Joint Coordinate Sequence
        ↓
Motion Stabilization
        ↓
Processed JSON Pose Data
        ↓
Unity 2D Skeleton
        ↓
Character Rig Mapping
        ↓
Unity 2D Character Animation