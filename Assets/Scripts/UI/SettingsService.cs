using TMPro;
using UnityEngine;

namespace Physical.Therapy.UI
{
    /// <summary>
    /// Settings service for managing various pose visualization configurations.
    /// </summary>
    public class SettingsService : MonoBehaviour
    {

        [Header("Visualization Settings")]
        [Tooltip("First configuration of pose visualization.")]
        public TMP_InputField coordinatesVisualization1InputField;

        [Tooltip("Second configuration of pose visualization.")]
        public TMP_InputField coordinatesVisualization2InputField;

        [Tooltip("Third configuration of pose visualization.")]
        public TMP_InputField coordinatesVisualization3InputField;

        [Header("Skeleton References")]
        [Tooltip("Target skeleton/avatar root transform (pose to match).")]
        public Transform targetTransform;

        [Tooltip("Player skeleton/avatar root transform (live tracking).")]
        public Transform playerTransform;

        [Header("Configuration Selection")]
        [Tooltip("Which visualization configuration to apply (1, 2, or 3).")]
        [Range(1, 3)]
        public int activeConfiguration = 1;

        private void Start()
        {
            // Set default values for each visualization configuration
            // Format: x,y,z\x,y,z (target coordinates\player coordinates)
            coordinatesVisualization1InputField.text = "0,0,3\\0,0,2";
            coordinatesVisualization2InputField.text = "-1.8,0,3\\1.8,0,3";
            coordinatesVisualization3InputField.text = "2,0,2\\4,0,2";

            // Apply the active configuration
            ApplyActiveConfiguration();
        }

        /// <summary>
        /// Applies the currently selected visualization configuration.
        /// </summary>
        public void ApplyActiveConfiguration()
        {
            string configText = activeConfiguration switch
            {
                1 => coordinatesVisualization1InputField.text,
                2 => coordinatesVisualization2InputField.text,
                3 => coordinatesVisualization3InputField.text,
                _ => coordinatesVisualization1InputField.text
            };

            ApplyCoordinates(configText);
        }

        /// <summary>
        /// Applies a specific configuration by index (1, 2, or 3).
        /// </summary>
        public void ApplyConfiguration(int configIndex)
        {
            Debug.Log("[SettingsService] Applying configuration index: " + configIndex);
            if (configIndex < 1 || configIndex > 3)
            {
                Debug.LogWarning($"[SettingsService] Invalid configuration index: {configIndex}. Must be 1, 2, or 3.");
                return;
            }

            activeConfiguration = configIndex;
            ApplyActiveConfiguration();
        }

        /// <summary>
        /// Parses coordinate text and applies positions to target and player transforms.
        /// This moves both the skeleton renderer and any avatar attached to the transform.
        /// Expected format: "x,y,z\\x,y,z" where first is target, second is player.
        /// </summary>
        private void ApplyCoordinates(string coordinateText)
        {
            if (string.IsNullOrWhiteSpace(coordinateText))
            {
                Debug.LogWarning("[SettingsService] Coordinate text is empty.");
                return;
            }

            // Split by '\' separator
            string[] parts = coordinateText.Split('\\');
            if (parts.Length != 2)
            {
                Debug.LogError($"[SettingsService] Invalid coordinate format: '{coordinateText}'. Expected format: 'x,y,z\\x,y,z'");
                return;
            }

            // Parse target coordinates (first part)
            Vector3 targetPosition = ParseVector3(parts[0].Trim());

            // Parse player coordinates (second part)
            Vector3 playerPosition = ParseVector3(parts[1].Trim());

            // Apply positions to transforms (moves both skeleton and avatar)
            if (targetTransform != null)
            {
                targetTransform.position = targetPosition;
                Debug.Log($"[SettingsService] Applied target transform position: {targetPosition}");
            }
            else
            {
                Debug.LogWarning("[SettingsService] Target transform is not assigned.");
            }

            if (playerTransform != null)
            {
                playerTransform.position = playerPosition;
                Debug.Log($"[SettingsService] Applied player transform position: {playerPosition}");
            }
            else
            {
                Debug.LogWarning("[SettingsService] Player transform is not assigned.");
            }
        }

        /// <summary>
        /// Parses a string in format "x,y,z" to Vector3.
        /// </summary>
        private Vector3 ParseVector3(string vectorText)
        {
            // Remove whitespace
            vectorText = vectorText.Trim();

            // Split by comma
            string[] components = vectorText.Split(',');
            if (components.Length != 3)
            {
                Debug.LogError($"[SettingsService] Invalid vector format: '{vectorText}'. Expected format: 'x,y,z'");
                return Vector3.zero;
            }

            // Parse each component
            if (float.TryParse(components[0].Trim(), out float x) &&
                float.TryParse(components[1].Trim(), out float y) &&
                float.TryParse(components[2].Trim(), out float z))
            {
                return new Vector3(x, y, z);
            }
            else
            {
                Debug.LogError($"[SettingsService] Failed to parse vector components from: '{vectorText}'");
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Updates coordinates from input field and applies them.
        /// Call this when input field values change.
        /// </summary>
        public void OnCoordinatesChanged(int configIndex)
        {
            Debug.Log("[SettingsService] Coordinates changed for configuration index: " + configIndex);
            ApplyConfiguration(configIndex);
        }
    }

}