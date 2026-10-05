using UnityEngine;
using System.Collections.Generic;

namespace Skote.Vfx.Audio
{
    public class MicrophoneDecibelValues : MonoBehaviour
    {
        [Header("Analysis Settings")]
        public int sampleDataLength = 1024; // Power of 2 for efficiency
        public int smoothingBufferSize = 10; // Number of previous dB values to average
        public float referenceValue = 0.1f; // RMS value reference
        [Range(0, -160)]
        public float minDB = -80f; // Minimum dB value to avoid -Infinity when silent

        [Header("Debug")]
        public bool showDebugInfo = false;

        // Private variables
        private SharedMicrophone sharedMic;
        private float[] sampleData;
        private int lastSamplePosition = 0;
        private Queue<float> recentDBValues = new Queue<float>();
        private float currentDB = -80f;

        // Public accessor for the current dB value
        public float CurrentDB => currentDB;

        private void Start()
        {
            sampleData = new float[sampleDataLength];
            StartMicrophone();
        }

        private void Update()
        {
            if (sharedMic != null && sharedMic.IsRunning)
                AnalyzeMicrophoneData();
        }

        /// <summary>
        /// Starts recording from the microphone using SharedMicrophone
        /// </summary>
        public void StartMicrophone()
        {
            if (sharedMic == null)
            {
                sharedMic = FindAnyObjectByType<SharedMicrophone>();
            }
            if (sharedMic == null)
            {
                sharedMic = gameObject.AddComponent<SharedMicrophone>();
            }
            sharedMic.EnsureStarted();
            lastSamplePosition = 0;

            if (showDebugInfo)
                Debug.Log($"MicrophoneDecibelValues: using SharedMicrophone");
        }

        /// <summary>
        /// Analyzes the current microphone data and calculates the dB value
        /// </summary>
        private void AnalyzeMicrophoneData()
        {
            if (sharedMic == null || sharedMic.Clip == null) return;

            // Get the current position of the recording
            int currentPosition = sharedMic.GetWritePosition();

            // If position hasn't changed, no need to process
            if (currentPosition == lastSamplePosition)
                return;

            // Calculate how many samples we need to read
            int samplesToRead = 0;

            if (currentPosition < lastSamplePosition)
            {
                // Wrapped around the end of the buffer
                samplesToRead = (sharedMic.Clip.samples - lastSamplePosition) + currentPosition;
            }
            else
            {
                samplesToRead = currentPosition - lastSamplePosition;
            }

            // Only process if we have enough samples
            if (samplesToRead >= sampleDataLength)
            {
                // Get data from the right position
                sharedMic.Clip.GetData(sampleData, lastSamplePosition % sharedMic.Clip.samples);

                // Calculate RMS
                float sum = 0;
                for (int i = 0; i < sampleDataLength; i++)
                {
                    sum += sampleData[i] * sampleData[i];
                }
                float rmsValue = Mathf.Sqrt(sum / sampleDataLength);

                // Calculate dB
                float dbValue = 20 * Mathf.Log10(rmsValue / referenceValue);

                // Set minimum threshold to avoid -infinity
                dbValue = Mathf.Max(dbValue, minDB);

                // Update last position to where we read up to
                lastSamplePosition = (lastSamplePosition + sampleDataLength) % sharedMic.Clip.samples;

                // Add to smoothing buffer
                recentDBValues.Enqueue(dbValue);
                while (recentDBValues.Count > smoothingBufferSize)
                {
                    recentDBValues.Dequeue();
                }

                // Calculate smoothed dB value
                float sumDB = 0;
                foreach (float db in recentDBValues)
                {
                    sumDB += db;
                }
                currentDB = sumDB / recentDBValues.Count;

                if (showDebugInfo && Time.frameCount % 30 == 0)
                {
                    Debug.Log($"Microphone dB: {currentDB:F2}");
                }
            }
        }

        /// <summary>
        /// Normalizes the dB value to a 0-1 range based on the provided min/max range
        /// </summary>
        /// <param name="minThreshold">The minimum dB threshold (typically negative, e.g. -60)</param>
        /// <param name="maxThreshold">The maximum dB threshold (typically near 0, e.g. -10)</param>
        /// <returns>Normalized value between 0 and 1</returns>
        public float GetNormalizedDB(float minThreshold = -60f, float maxThreshold = -10f)
        {
            return Mathf.Clamp01((currentDB - minThreshold) / (maxThreshold - minThreshold));
        }
    }
}