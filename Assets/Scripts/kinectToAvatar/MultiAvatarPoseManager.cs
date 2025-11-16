using UnityEngine;
using K4AdotNet.Samples.Unity;

/// <summary>
/// Example script demonstrating how to manage multiple avatars with different static poses
/// </summary>
public class MultiAvatarPoseManager : MonoBehaviour
{
    [Header("Avatar References")]
    [Tooltip("Array of avatar GameObjects that will display different poses")]
    public GameObject[] avatars;

    [Header("Pose Files")]
    [Tooltip("Array of JSON file names (must be in Assets/Poses/ folder)")]
    public string[] poseFiles;

    [Header("Settings")]
    [Tooltip("Automatically assign poses to avatars on Start")]
    public bool autoAssignOnStart = true;

    void Start()
    {
        if (autoAssignOnStart)
        {
            AssignPosesToAvatars();
        }
    }

    /// <summary>
    /// Automatically assign poses to avatars based on the poseFiles array
    /// </summary>
    public void AssignPosesToAvatars()
    {
        if (avatars == null || avatars.Length == 0)
        {
            Debug.LogWarning("[MultiAvatarPoseManager] No avatars assigned!");
            return;
        }

        if (poseFiles == null || poseFiles.Length == 0)
        {
            Debug.LogWarning("[MultiAvatarPoseManager] No pose files specified!");
            return;
        }

        for (int i = 0; i < avatars.Length; i++)
        {
            if (avatars[i] == null)
            {
                Debug.LogWarning($"[MultiAvatarPoseManager] Avatar at index {i} is null!");
                continue;
            }

            // Get or add required components
            var animator = avatars[i].GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError($"[MultiAvatarPoseManager] Avatar '{avatars[i].name}' is missing Animator component!");
                continue;
            }

            var characterAnimator = avatars[i].GetComponent<CharacterAnimator>();
            if (characterAnimator == null)
            {
                characterAnimator = avatars[i].AddComponent<CharacterAnimator>();
                Debug.Log($"[MultiAvatarPoseManager] Added CharacterAnimator to '{avatars[i].name}'");
            }

            var jsonProvider = avatars[i].GetComponent<SkeletonProviderFromJson>();
            if (jsonProvider == null)
            {
                jsonProvider = avatars[i].AddComponent<SkeletonProviderFromJson>();
                Debug.Log($"[MultiAvatarPoseManager] Added SkeletonProviderFromJson to '{avatars[i].name}'");
            }

            // Assign pose file (cycle through if more avatars than pose files)
            int poseIndex = i % poseFiles.Length;
            jsonProvider.jsonFileName = poseFiles[poseIndex];
            jsonProvider.loadOnStart = false; // We'll load manually

            Debug.Log($"[MultiAvatarPoseManager] Avatar '{avatars[i].name}' assigned pose '{poseFiles[poseIndex]}'");
        }

        // Load all poses
        LoadAllPoses();
    }

    /// <summary>
    /// Load poses for all avatars
    /// </summary>
    public void LoadAllPoses()
    {
        foreach (var avatar in avatars)
        {
            if (avatar != null)
            {
                var provider = avatar.GetComponent<SkeletonProviderFromJson>();
                if (provider != null)
                {
                    provider.LoadPoseFromJson();
                }
            }
        }
        Debug.Log("[MultiAvatarPoseManager] All poses loaded!");
    }

    /// <summary>
    /// Load a specific pose for a specific avatar
    /// </summary>
    public void LoadPoseForAvatar(int avatarIndex, string poseFileName)
    {
        if (avatarIndex < 0 || avatarIndex >= avatars.Length)
        {
            Debug.LogError($"[MultiAvatarPoseManager] Invalid avatar index: {avatarIndex}");
            return;
        }

        var avatar = avatars[avatarIndex];
        if (avatar == null)
        {
            Debug.LogError($"[MultiAvatarPoseManager] Avatar at index {avatarIndex} is null!");
            return;
        }

        var provider = avatar.GetComponent<SkeletonProviderFromJson>();
        if (provider != null)
        {
            provider.LoadPoseFromJson(poseFileName);
            Debug.Log($"[MultiAvatarPoseManager] Loaded pose '{poseFileName}' for avatar '{avatar.name}'");
        }
        else
        {
            Debug.LogError($"[MultiAvatarPoseManager] Avatar '{avatar.name}' doesn't have SkeletonProviderFromJson!");
        }
    }

    /// <summary>
    /// Clear pose for a specific avatar
    /// </summary>
    public void ClearAvatarPose(int avatarIndex)
    {
        if (avatarIndex < 0 || avatarIndex >= avatars.Length)
        {
            Debug.LogError($"[MultiAvatarPoseManager] Invalid avatar index: {avatarIndex}");
            return;
        }

        var avatar = avatars[avatarIndex];
        if (avatar != null)
        {
            var provider = avatar.GetComponent<SkeletonProviderFromJson>();
            if (provider != null)
            {
                provider.ClearPose();
            }
        }
    }

    /// <summary>
    /// Clear all avatar poses
    /// </summary>
    public void ClearAllPoses()
    {
        foreach (var avatar in avatars)
        {
            if (avatar != null)
            {
                var provider = avatar.GetComponent<SkeletonProviderFromJson>();
                if (provider != null)
                {
                    provider.ClearPose();
                }
            }
        }
        Debug.Log("[MultiAvatarPoseManager] All poses cleared!");
    }

    /// <summary>
    /// Cycle through different poses for a specific avatar
    /// </summary>
    public void CyclePose(int avatarIndex)
    {
        if (avatarIndex < 0 || avatarIndex >= avatars.Length)
        {
            Debug.LogError($"[MultiAvatarPoseManager] Invalid avatar index: {avatarIndex}");
            return;
        }

        if (poseFiles == null || poseFiles.Length == 0)
        {
            Debug.LogError("[MultiAvatarPoseManager] No pose files available!");
            return;
        }

        var avatar = avatars[avatarIndex];
        if (avatar == null) return;

        var provider = avatar.GetComponent<SkeletonProviderFromJson>();
        if (provider != null)
        {
            // Find current pose index
            int currentIndex = System.Array.IndexOf(poseFiles, provider.jsonFileName);
            int nextIndex = (currentIndex + 1) % poseFiles.Length;

            provider.LoadPoseFromJson(poseFiles[nextIndex]);
            Debug.Log($"[MultiAvatarPoseManager] Avatar '{avatar.name}' cycled to pose '{poseFiles[nextIndex]}'");
        }
    }
}
