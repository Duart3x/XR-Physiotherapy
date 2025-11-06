using System;
using UnityEngine;

public class ClockMecanism : MonoBehaviour
{
    [Header("Clock Hands")]
    [Tooltip("Transform for the hour hand (short hand)")]
    public Transform hourHand;

    [Tooltip("Transform for the minute hand (medium hand)")]
    public Transform minuteHand;

    [Tooltip("Transform for the second hand (long hand)")]
    public Transform secondHand;

    [Header("Clock Settings")]
    [Tooltip("Use real local time from the user's system")]
    public bool useRealTime = true;

    [Tooltip("If not using real time, set a custom time for your VR world")]
    public int customHour = 10;

    [Tooltip("If not using real time, set custom minutes")]
    public int customMinute = 10;

    [Tooltip("If not using real time, set custom seconds")]
    public int customSecond = 0;

    [Tooltip("Speed multiplier for time passage in VR world (only if not using real time)")]
    public float timeSpeed = 1f;

    [Header("Hand Rotation Axis")]
    [Tooltip("The local axis around which hands rotate (typically forward for wall clocks)")]
    public Vector3 rotationAxis = Vector3.right;

    [Tooltip("Reverse rotation direction if needed")]
    public bool reverseRotation = false;

    [Tooltip("Smooth the second hand movement instead of ticking")]
    public bool smoothSecondHand = true;

    private float virtualTime; // Time in seconds for custom time mode

    void Start()
    {
        // Initialize virtual time if using custom time
        if (!useRealTime)
        {
            virtualTime = customHour * 3600f + customMinute * 60f + customSecond;
        }

        // Initial update to set correct positions
        UpdateClockHands();
    }

    void Update()
    {
        UpdateClockHands();
    }

    void UpdateClockHands()
    {
        float hours, minutes, seconds;

        if (useRealTime)
        {
            // Get current system time
            DateTime now = DateTime.Now;
            hours = now.Hour % 12; // Convert to 12-hour format
            minutes = now.Minute;
            seconds = now.Second + now.Millisecond / 1000f; // Include milliseconds for smooth movement
        }
        else
        {
            // Update virtual time
            virtualTime += Time.deltaTime * timeSpeed;

            // Convert virtual time to hours, minutes, seconds
            float totalSeconds = virtualTime % 86400; // Wrap at 24 hours
            hours = (totalSeconds / 3600f) % 12; // 12-hour format
            minutes = (totalSeconds % 3600f) / 60f;
            seconds = totalSeconds % 60f;
        }

        // Calculate rotation angles
        // Clock hands rotate clockwise, which is negative rotation around forward axis
        float secondAngle = smoothSecondHand ? (seconds / 60f) * 360f : Mathf.Floor(seconds) * 6f;
        float minuteAngle = ((minutes + seconds / 60f) / 60f) * 360f;
        float hourAngle = ((hours + minutes / 60f) / 12f) * 360f;

        // Apply direction multiplier
        float direction = reverseRotation ? 1f : -1f;
        secondAngle *= direction;
        minuteAngle *= direction;
        hourAngle *= direction;

        // Apply rotations
        // set the Z axis to -90 for all hands to face forward
        if (secondHand != null)
        {
            secondHand.localRotation = Quaternion.AngleAxis(secondAngle, rotationAxis);
            Vector3 zRotation = secondHand.localEulerAngles;
            zRotation.z = -90f;
            secondHand.localEulerAngles = zRotation;
        }

        if (minuteHand != null)
        {
            minuteHand.localRotation = Quaternion.AngleAxis(minuteAngle, rotationAxis);
            Vector3 zRotation = minuteHand.localEulerAngles;
            zRotation.z = -90f;
            minuteHand.localEulerAngles = zRotation;
        }

        if (hourHand != null)
        {
            hourHand.localRotation = Quaternion.AngleAxis(hourAngle, rotationAxis);
            Vector3 zRotation = hourHand.localEulerAngles;
            zRotation.z = -90f;
            hourHand.localEulerAngles = zRotation;
        }   
    }

    // Helper method to set custom time at runtime
    public void SetCustomTime(int hour, int minute, int second)
    {
        customHour = hour;
        customMinute = minute;
        customSecond = second;
        virtualTime = hour * 3600f + minute * 60f + second;
    }

    // Helper method to toggle between real and custom time
    public void ToggleTimeMode()
    {
        useRealTime = !useRealTime;
        if (!useRealTime)
        {
            virtualTime = customHour * 3600f + customMinute * 60f + customSecond;
        }
    }
}
