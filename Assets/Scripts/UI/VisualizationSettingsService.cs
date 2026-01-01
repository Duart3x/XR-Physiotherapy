using UnityEngine;

namespace Physical.Therapy.UI
{
    /// <summary>
    /// Configuration for skeleton visualization including position and avatar visibility.
    /// </summary>
    [System.Serializable]
    public class VisualizationConfiguration
    {
        [Tooltip("Position for the target skeleton/avatar.")]
        public Vector3 targetPosition;

        [Tooltip("Position for the player skeleton/avatar.")]
        public Vector3 playerPosition;

        [Tooltip("Hide the target avatar (only show skeleton).")]
        public bool hideTargetAvatar;

        [Tooltip("Hide the player avatar (only show skeleton).")]
        public bool hidePlayerAvatar;

        public VisualizationConfiguration(Vector3 targetPos, Vector3 playerPos, bool hideTarget, bool hidePlayer)
        {
            targetPosition = targetPos;
            playerPosition = playerPos;
            hideTargetAvatar = hideTarget;
            hidePlayerAvatar = hidePlayer;
        }
    }

    /// <summary>
    /// Service for managing visualization configurations with position and avatar visibility.
    /// </summary>
    public class VisualizationSettingsService : MonoBehaviour
    {
        [Header("Transform References")]
        [Tooltip("Target skeleton/avatar root transform.")]
        public Transform targetTransform;

        [Tooltip("Player skeleton/avatar root transform.")]
        public Transform playerTransform;

        [Header("Avatar References")]
        [Tooltip("Target avatar GameObject to show/hide.")]
        public GameObject targetAvatar;

        [Tooltip("Player avatar GameObject to show/hide.")]
        public GameObject playerAvatar;

        [Header("Visualization Configurations")]
        [Tooltip("Configuration 1")]
        public VisualizationConfiguration configuration1;

        [Tooltip("Configuration 2")]
        public VisualizationConfiguration configuration2;

        [Tooltip("Configuration 3")]
        public VisualizationConfiguration configuration3;

        [Header("Active Configuration")]
        [Tooltip("Which configuration to apply on start (1, 2, or 3).")]
        [Range(1, 3)]
        public int activeConfiguration = 1;

        private void Start()
        {
            // Initialize default configurations
            if (configuration1 == null)
                configuration1 = new VisualizationConfiguration(new Vector3(0, 0, 3), new Vector3(0, 0, 2), false, false);

            if (configuration2 == null)
                configuration2 = new VisualizationConfiguration(new Vector3(-1.8f, 0, 3), new Vector3(1.8f, 0, 3), false, false);

            if (configuration3 == null)
                configuration3 = new VisualizationConfiguration(new Vector3(2, 0, 2), new Vector3(4, 0, 2), false, false);

            // Apply the active configuration
            ApplyActiveConfiguration();
        }

        /// <summary>
        /// Applies the currently selected configuration.
        /// </summary>
        public void ApplyActiveConfiguration()
        {
            VisualizationConfiguration config = activeConfiguration switch
            {
                1 => configuration1,
                2 => configuration2,
                3 => configuration3,
                _ => configuration1
            };

            ApplyConfiguration(config);
        }

        /// <summary>
        /// Applies a specific configuration by index (1, 2, or 3).
        /// </summary>
        public void ApplyConfigurationByIndex(int configIndex)
        {
            Debug.Log($"[VisualizationSettingsService] Applying configuration index (selected + 1): {configIndex}");

            if (configIndex < 1 || configIndex > 3)
            {
                Debug.LogWarning($"[VisualizationSettingsService] Invalid configuration index: {configIndex}. Must be 1, 2, or 3.");
                return;
            }

            activeConfiguration = configIndex;
            ApplyActiveConfiguration();
        }

        /// <summary>
        /// Applies a visualization configuration (position and avatar visibility).
        /// </summary>
        public void ApplyConfiguration(VisualizationConfiguration config)
        {
            if (config == null)
            {
                Debug.LogError("[VisualizationSettingsService] Configuration is null.");
                return;
            }

            // Apply positions
            if (targetTransform != null)
            {
                targetTransform.position = config.targetPosition;
                Debug.Log($"[VisualizationSettingsService] Applied target position: {config.targetPosition}");
            }
            else
            {
                Debug.LogWarning("[VisualizationSettingsService] Target transform is not assigned.");
            }

            if (playerTransform != null)
            {
                playerTransform.position = config.playerPosition;
                Debug.Log($"[VisualizationSettingsService] Applied player position: {config.playerPosition}");
            }
            else
            {
                Debug.LogWarning("[VisualizationSettingsService] Player transform is not assigned.");
            }

            // Apply avatar visibility
            if (targetAvatar != null)
            {
                targetAvatar.SetActive(!config.hideTargetAvatar);
                Debug.Log($"[VisualizationSettingsService] Target avatar visible: {!config.hideTargetAvatar}");
            }
            else
            {
                Debug.LogWarning("[VisualizationSettingsService] Target avatar is not assigned.");
            }

            if (playerAvatar != null)
            {
                playerAvatar.SetActive(!config.hidePlayerAvatar);
                Debug.Log($"[VisualizationSettingsService] Player avatar visible: {!config.hidePlayerAvatar}");
            }
            else
            {
                Debug.LogWarning("[VisualizationSettingsService] Player avatar is not assigned.");
            }
        }

        public void OnChangeConfiguration(int configIndex)
        {
            Debug.Log("[VisualizationSettingsService] OnChangeConfiguration called with index: " + configIndex);
            ApplyConfigurationByIndex(configIndex + 1);
        }
    }

}
