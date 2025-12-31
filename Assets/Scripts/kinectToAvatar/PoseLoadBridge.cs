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
            }

            // Subscribe to UI events
            if (poseSearchController != null)
            {
                poseSearchController.OnPoseSelected += HandlePoseSelected;
                Debug.Log("[PoseLoadBridge] Subscribed to PoseSearchController.OnPoseSelected");
            }
            else
            {
                Debug.LogWarning("[PoseLoadBridge] No PoseSearchController found!");
            }

            if (skeletonProvider == null)
            {
                Debug.LogWarning("[PoseLoadBridge] No SkeletonProviderFromJson found!");
            }
        }

        private void OnDestroy()
        {
            if (poseSearchController != null)
            {
                poseSearchController.OnPoseSelected -= HandlePoseSelected;
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
        }
    }
}
