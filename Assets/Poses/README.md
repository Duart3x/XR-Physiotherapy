# Static Pose Display System

## Overview
This system allows you to display multiple avatars with different static poses loaded from JSON files, without affecting the existing live tracking system.

## Components

### SkeletonProviderFromJson
- Loads skeleton pose data from JSON files in the `Assets/Poses/` folder
- Triggers once on Start to display a static pose
- Can load different poses for different avatars
- Works independently of the live tracking `SkeletonProvider`

### CharacterAnimator (Updated)
- Now supports **both** live tracking and static pose display
- Automatically detects which provider is attached to the GameObject
- No changes needed to existing live tracking setup

## Setup Instructions

### For Static Pose Display

1. **Create/Prepare Your Avatar GameObject**
   - Add an `Animator` component (required)
   - Make sure the avatar has a humanoid rig

2. **Add Required Components**
   ```
   Avatar GameObject
   ├── Animator (Unity built-in)
   ├── CharacterAnimator (existing script)
   └── SkeletonProviderFromJson (new script)
   ```

3. **Configure SkeletonProviderFromJson**
   - In the Inspector, set `Json File Name` to your pose file (e.g., `frontal_lunge_arms_up.json`)
   - Check `Load On Start` if you want the pose to load automatically
   - The file should be in `Assets/Poses/` folder

4. **Place Pose Files**
   - Put your JSON pose files in `Assets/Poses/`
   - Files should follow the Azure Kinect skeleton format

### For Multiple Avatars with Different Poses

#### Scenario 1: Multiple Static Pose Avatars
```
Avatar1 (frontal_lunge)
├── Animator
├── CharacterAnimator
└── SkeletonProviderFromJson
    └── jsonFileName = "frontal_lunge_arms_up.json"

Avatar2 (t_pose)
├── Animator
├── CharacterAnimator
└── SkeletonProviderFromJson
    └── jsonFileName = "t_pose.json"

Avatar3 (squat)
├── Animator
├── CharacterAnimator
└── SkeletonProviderFromJson
    └── jsonFileName = "squat_pose.json"
```

#### Scenario 2: Live Tracking + Static Poses
```
LiveAvatar
├── Animator
├── CharacterAnimator
└── SkeletonProvider (live Kinect tracking)

StaticPoseAvatar
├── Animator
├── CharacterAnimator
└── SkeletonProviderFromJson (static pose from file)
```

## Usage Examples

### Basic Setup (Single Avatar)
1. Drag your avatar into the scene
2. Add `CharacterAnimator` and `SkeletonProviderFromJson` components
3. Set the JSON file name in the Inspector
4. Press Play - the pose loads automatically!

### Advanced Setup (Multiple Avatars)
1. Create an empty GameObject in your scene (name it "PoseManager")
2. Add the `MultiAvatarPoseManager` component to it
3. Drag your avatars into the `Avatars` array
4. Add pose file names to the `Pose Files` array (e.g., "frontal_lunge_arms_up.json")
5. Check `Auto Assign On Start`
6. Press Play - all avatars load their assigned poses!

### Using the MultiAvatarPoseManager
The `MultiAvatarPoseManager` script makes it easy to manage multiple avatars:

```csharp
// Example: Set up in Unity Inspector
public MultiAvatarPoseManager poseManager;

void Start()
{
    // Avatars and pose files assigned in Inspector
    // Auto-assignment happens on Start if enabled
}

// Or control manually:
void Example()
{
    // Load all poses
    poseManager.LoadAllPoses();
    
    // Load specific pose for specific avatar
    poseManager.LoadPoseForAvatar(0, "squat_pose.json");
    
    // Cycle through poses for avatar 0
    poseManager.CyclePose(0);
    
    // Clear all poses
    poseManager.ClearAllPoses();
}
```

### Runtime Pose Loading
```csharp
// Get reference to the provider
var provider = GetComponent<SkeletonProviderFromJson>();

// Load a different pose
provider.LoadPoseFromJson("different_pose.json");

// Clear the current pose
provider.ClearPose();

// Check if a pose is loaded
if (provider.IsPoseLoaded)
{
    Debug.Log("Pose is active!");
}
```

### Switching Poses at Runtime
```csharp
public class PoseSwitcher : MonoBehaviour
{
    private SkeletonProviderFromJson provider;
    public string[] poseFiles = new string[] 
    {
        "frontal_lunge_arms_up.json",
        "t_pose.json",
        "squat_pose.json"
    };
    
    void Start()
    {
        provider = GetComponent<SkeletonProviderFromJson>();
    }
    
    public void LoadPose(int index)
    {
        if (index >= 0 && index < poseFiles.Length)
        {
            provider.LoadPoseFromJson(poseFiles[index]);
        }
    }
}
```

## JSON File Format
Pose files should be in the Azure Kinect skeleton format:
```json
{
  "body_id": 1,
  "timestamp": 123456789,
  "joints": [
    {
      "joint_name": "PELVIS",
      "position": { "x": 0.0, "y": 0.0, "z": 0.0 },
      "orientation": { "w": 1.0, "x": 0.0, "y": 0.0, "z": 0.0 },
      "confidence_level": 2
    },
    ...
  ]
}
```
