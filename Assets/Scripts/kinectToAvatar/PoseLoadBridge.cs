using UnityEngine;
using Physical.Therapy.UI;

namespace K4AdotNet.Samples.Unity
{
    /// <summary>
    /// Bridge component that connects the UI layer (Physical.Therapy.UI namespace)
    /// to the skeleton/pose loading layer (K4AdotNet.Samples.Unity namespace).
    ///
    /// This component listens to events from PoseSearchController and loads poses
    /// using SkeletonProviderFromJson, keeping the UI completely decoupled from
    /// the skeleton system.
    /// </summary>
    public class PoseLoadBridge : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The PoseSearchController from the UI layer")]
        public PoseSearchController poseSearchController;

        [Tooltip("The SkeletonProviderFromJson that loads pose data")]
        public SkeletonProviderFromJson skeletonProvider;

        [Tooltip("The ExerciseManager for pose verification and avatar control")]
        public ExerciseManager exerciseManager;

        [Tooltip("The PoseService that provides pose metadata")]
        public PoseService poseService;

        [Header("Settings")]
        [Tooltip("Automatically find references if not assigned")]
        public bool autoFindReferences = true;

        private void Start()
        {
            if (autoFindReferences)
            {
                if (poseSearchController == null)
                    poseSearchController = FindObjectOfType<PoseSearchController>();

                if (skeletonProvider == null)
                    skeletonProvider = FindObjectOfType<SkeletonProviderFromJson>();

                if (exerciseManager == null)
                    exerciseManager = FindObjectOfType<ExerciseManager>();

                if (poseService == null)
                    poseService = FindObjectOfType<PoseService>();
            }

            // Subscribe to UI events
            if (poseSearchController != null)
            {
                poseSearchController.OnPoseSelected += HandlePoseSelected;
                poseSearchController.OnPlayPoseEvent += OnPlayPose;
                poseSearchController.OnPausePoseEvent += OnPausePose;
                Debug.Log("[PoseLoadBridge] Subscribed to PoseSearchController events");
            }
            else
            {
                Debug.LogWarning("[PoseLoadBridge] No PoseSearchController found!");
            }

            if (skeletonProvider == null)
            {
                Debug.LogWarning("[PoseLoadBridge] No SkeletonProviderFromJson found!");
            }

            if (exerciseManager == null)
            {
                Debug.LogWarning("[PoseLoadBridge] No ExerciseManager found!");
            }

            if (poseService == null)
            {
                Debug.LogWarning("[PoseLoadBridge] No PoseService found!");
            }

            // Initialize: disable player avatar tracking on start
            SetPlayerAvatarTracking(false);
        }

        private void OnDestroy()
        {
            if (poseSearchController != null)
            {
                poseSearchController.OnPoseSelected -= HandlePoseSelected;
                poseSearchController.OnPlayPoseEvent -= OnPlayPose;
                poseSearchController.OnPausePoseEvent -= OnPausePose;
            }
        }

        /// <summary>
        /// Handles the pose selection event from the UI layer.
        /// Loads the selected pose using SkeletonProviderFromJson.
        /// If poseName starts with "CLEAR:", clears the pose only if it matches the currently loaded pose.
        /// </summary>
        private void HandlePoseSelected(string poseName)
        {
            if (poseName.StartsWith("CLEAR:"))
            {
                // Extract the pose name being cleared
                string poseBeingCleared = poseName.Substring(6); // Remove "CLEAR:" prefix
                string fileNameBeingCleared = PoseService.GetPoseFileName(poseBeingCleared);

                // Only clear if this is the currently loaded pose
                if (skeletonProvider != null && skeletonProvider.CurrentPoseFileName == fileNameBeingCleared)
                {
                    Debug.Log($"[PoseLoadBridge] Clearing pose: {poseBeingCleared} (matches current: {skeletonProvider.CurrentPoseFileName})");
                    ClearPose();
                }
                else
                {
                    Debug.Log($"[PoseLoadBridge] Ignoring clear for '{poseBeingCleared}' - not the current pose (current: {skeletonProvider?.CurrentPoseFileName})");
                }
                return;
            }

            Debug.Log($"[PoseLoadBridge] Loading pose: {poseName}");

            if (skeletonProvider != null)
            {
                string fileName = PoseService.GetPoseFileName(poseName);
                skeletonProvider.LoadPoseFromJson(fileName);
                exerciseManager.SetTargetSkeletonProvider(skeletonProvider);
                exerciseManager.SetShowPoseSkeleton(true);

                // Apply Y rotation to both skeleton GameObjects based on pose metadata
                if (poseService != null)
                {
                    PoseMetadata metadata = poseService.GetPoseByName(poseName);
                    if (metadata != null)
                    {
                        ApplyRotationToSkeletons(metadata.ApplyYRotation180);
                        Debug.Log($"[PoseLoadBridge] Applied rotation = {metadata.ApplyYRotation180} for pose: {poseName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PoseLoadBridge] Could not find metadata for pose: {poseName}");
                        ApplyRotationToSkeletons(false);
                    }
                }
            }
            else
            {
                Debug.LogError("[PoseLoadBridge] Cannot load pose - SkeletonProviderFromJson is null!");
            }
        }

        /// <summary>
        /// Manually load a pose by name.
        /// Can be called from other scripts or Unity Events.
        /// </summary>
        public void LoadPose(string poseName)
        {
            HandlePoseSelected(poseName);
        }

        /// <summary>
        /// Clear the current pose.
        /// </summary>
        public void ClearPose()
        {
            if (skeletonProvider != null)
            {
                skeletonProvider.ClearPose();
            }

            if (exerciseManager != null)
            {
                exerciseManager.SetShowPoseSkeleton(false);
            }

            // Reset rotation to default (0 degrees)
            ApplyRotationToSkeletons(false);

            // Also disable player avatar tracking and reset verification
            SetPlayerAvatarTracking(false);
        }

        /// <summary>
        /// Apply Y-axis 180-degree rotation to the target pose skeleton.
        /// </summary>
        private void ApplyRotationToSkeletons(bool applyRotation)
        {
            float yRotation = applyRotation ? 180f : 0f;

            // Rotate the target pose skeleton provider
            if (skeletonProvider != null)
            {
                skeletonProvider.transform.localRotation = UnityEngine.Quaternion.Euler(0, yRotation, 0);
                Debug.Log($"[PoseLoadBridge] Set target skeleton rotation Y={yRotation}");
            }
        }

        /// <summary>
        /// Enable or disable player avatar tracking for pose comparison.
        /// </summary>
        public void SetPlayerAvatarTracking(bool enabled)
        {
            Debug.Log($"[PoseLoadBridge] Setting player avatar tracking: {enabled}");

            if (exerciseManager != null)
            {
                exerciseManager.SetShowAvatarSkeleton(enabled);
            }
        }

        /// <summary>
        /// Start playing the pose - enables player avatar tracking.
        /// </summary>
        public void OnPlayPose()
        {
            Debug.Log("[PoseLoadBridge] Play pose - enabling player avatar tracking");
            SetPlayerAvatarTracking(true);
        }

        /// <summary>
        /// Pause the pose tracking - disables player avatar tracking.
        /// </summary>
        public void OnPausePose()
        {
            Debug.Log("[PoseLoadBridge] Pause pose - disabling player avatar tracking");
            SetPlayerAvatarTracking(false);
        }
    }
}
