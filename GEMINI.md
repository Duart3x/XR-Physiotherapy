## Project Overview

This is a Unity project that demonstrates how to use the Azure Kinect DK to capture body tracking data and animate a 3D character in real-time. The project is set up to work with the Azure Kinect Body Tracking SDK and includes scripts for capturing, processing, and rendering the skeleton data.

The core of the project lies in the `Assets/Scripts/kinectToAvatar` directory, which contains the C# scripts responsible for the following:

*   **Data Capture:** The `CaptureManager.cs` script is responsible for initializing the Kinect sensor and capturing the body tracking data.
*   **Skeleton Processing:** The `SkeletonProvider.cs` script processes the captured data to extract the skeleton information for each detected body.
*   **Character Animation:** The `CharacterAnimator.cs` script takes the skeleton data and applies it to a 3D character model, animating it in real-time.
*   **Exercise Manager:** The `ExerciseManager.cs` script compares the live user skeleton with a static target pose and visualizes the difference using arrows, guiding the user to match the pose.

The project also includes a sample scene `KinectAvatarScene.unity` that is set up with a character and the necessary scripts to get started.

## Building and Running

To build and run this project, you will need to have the following installed:

*   Unity Hub
*   Unity Editor (version 2020.3 or later)
*   Azure Kinect SDK
*   Azure Kinect Body Tracking SDK

Before opening the project in Unity for the first time, you must run the `prepare.cmd` script located in the root of the project. This script will copy the necessary files from the Azure Kinect Body Tracking SDK to the project's `Assets/Plugins/K4AdotNet` directory.

```bash
prepare.cmd
```

Once the preparation script has been run, you can open the project in the Unity Editor. The main scene to open is `Assets/Scenes/KinectAvatarScene.unity`.

To run the project, simply press the "Play" button in the Unity Editor. The application will attempt to connect to the Azure Kinect sensor and, if successful, will start tracking and animating the character on the screen.

## Development Conventions

The project follows the standard C# and Unity development conventions. All scripts are located in the `Assets/Scripts` directory and are organized into subdirectories based on their functionality.

The code is well-commented, and the class and method names are self-explanatory. The project also includes a `README.md` file with basic instructions on how to get started.

There are no specific testing or contribution guidelines outlined in the project. However, the code is structured in a way that makes it easy to extend and modify. For example, you can easily create your own character and animate it by creating a new prefab and adding the `CharacterAnimator.cs` script to it.
