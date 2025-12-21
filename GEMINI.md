## Project Overview

This is a Unity project (XR-Physiotherapy) that demonstrates how to use body tracking data (originally from Azure Kinect, now via TCP socket) to animate a 3D character in real-time and provide feedback on rehabilitation exercises. The project is set up to work with the K4AdotNet library and includes scripts for processing and rendering skeleton data.

The core of the project lies in the `Assets/Scripts` directory, organized as follows:

*   **Data Receiver (`SkeletonProvider.cs`):**  
    Modified to act as a TCP Server. It receives JSON-formatted skeleton data from an external client (e.g., a Kinect sensor app), parses it, and fires events when the skeleton is updated. It no longer directly manages the Kinect hardware.
    
*   **Skeleton Visualization (`SkeletonRenderer.cs`):**  
    Responsible for rendering the raw wireframe skeleton using Unity primitives (Spheres for joints, Cylinders for bones).
    *   **Fixes:** Implements a shader fallback (`Sprites/Default` or `URP/Unlit`) to prevent "purple" rendering in builds. 
    *   **Positioning:** Anchors the skeleton to the Pelvis (local 0,0,0) to keep it superimposed "directly above" the avatar, preventing it from moving around the world space as the user walks.
    *   **API:** Provides public methods (`SetJointColor`, `SetBoneColor`) for external scripts to highlight specific body parts.

*   **Character Animation (`CharacterAnimator.cs`):**  
    Takes the skeleton data and applies it to a standard Unity Humanoid avatar (like the "Robot Kyle" model), animating it in real-time. It maps Kinect joint data to Unity's HumanBodyBones.

*   **Exercise Logic (`ExerciseManager.cs`):**  
    Compares the live user skeleton (`SkeletonProvider`) with a recorded static target pose (`SkeletonProviderFromJson`).
    *   **Feedback:** Monitors specific joints (Wrists, Elbows, Knees, Ankles). If the user's joint position deviates from the target beyond a threshold, the corresponding joint and bone on the `SkeletonRenderer` are colored **Red**. If correct, they turn **Green**.

## Building and Running

To build and run this project, you will need:

*   Unity Hub
*   Unity Editor (Version compatible with the project, likely 2020.3+)
*   Azure Kinect SDK / Body Tracking SDK (dependencies managed via `prepare.cmd` for K4AdotNet)

**Setup:**
1.  Run `prepare.cmd` in the root directory to copy necessary K4AdotNet plugins.
2.  Open `Assets/Scenes/ArPassthroughScene.unity` (or `KinectAvatarScene.unity`).
3.  Ensure an external client is sending skeleton JSON data to the local IP on Port `8888`.

## Development Conventions

*   **Scripts:** Located in `Assets/Scripts`.
*   **Rendering:** The project uses the Universal Render Pipeline (URP).
*   **Networking:** Skeleton data is received via standard .NET Sockets (TCP).
*   **Coordinates:** Kinect coordinate systems are converted to Unity's coordinate system within the provider and renderer scripts (handling axis flips and scaling from millimeters to meters).

## Recent Changes & Technical Notes

*   **10-Dec-2025:**
    *   **SkeletonRenderer Update:** Fixed an issue where the procedural skeleton used the default material (causing pink/purple errors in builds). It now explicitly assigns a standard shader.
    *   **Skeleton Alignment:** Modified `SkeletonRenderer` to remove absolute world positioning. It now subtracts the Pelvis offset, effectively locking the visualization to the Avatar's root for easier comparison.
    *   **Visual Feedback:** Refactored `ExerciseManager` to remove the old "LineRenderer Arrow" system. Feedback is now provided directly on the skeleton wireframe by changing material colors (Red/Green) based on pose accuracy.

*   **11-Dec-2025:**
    *   **Architecture:** Introduced `ISkeletonProvider` interface to standardize skeleton data access. Updated `SkeletonProvider` (Live) and `SkeletonProviderFromJson` (Static) to implement this interface.
    *   **SkeletonRenderer Enhancements:** 
        *   Decoupled from concrete provider classes; now depends on `ISkeletonProvider`.
        *   Added `skeletonColor` field to allow inspector-based color customization (e.g., light blue for static target poses).
        *   Added `yRotation180` and `offset` fields to allow manual adjustment of the visualization's orientation and position relative to the avatar.
    *   **Scene Configuration:** Updated `ArPassthroughScene` to include a `SkeletonRenderer` for the static target pose (light blue, 180° rotated, slightly offset).
