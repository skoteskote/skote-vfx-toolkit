using UnityEngine;
using System.Collections.Generic;
using Skote.Vfx.Audio.PitchDetection;

namespace Skote.Vfx.Audio
{
    /// <summary>
    /// Direct microphone pitch using the same logic as AudioSourcePitchValues.
    /// Avoids the AudioSource routing delay. Debug output optional.
    /// </summary>
    public class MicrophonePitchValues : MonoBehaviour
    {
        private RAPTPitchDetector pitchDetector;

        [Header("Pitch Settings")]
        public int sampleRate = 44100;
        public float pitchLow = 0;
        public float pitchLowThreshold = 10f;
        public float pitchHigh = 500;
        public float pitchHighThreshold = 5f;
        public float pitchMin = 0;
        public float pitchMax = 50;
        public float pitchWindowSize = 0.1f;

        [Header("Debug")]
        public bool logPitch = false;
        public bool logPitchOnUpdate = false;

        private RAPTPitchDetector Detector => pitchDetector ??= new RAPTPitchDetector(sampleRate, 50f, 800f);

        // runtime
        private SharedMicrophone sharedMic;

        private void Start()
        {
            StartMicrophone();
        }

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
        }

        void Update()
        {
            if (logPitchOnUpdate)
            {
                float pitch = GetCurrentPitchValue();
                Debug.Log($"Mic pitch: {pitch:F2}");
            }
        }

        /// <summary>
        /// Returns mapped pitch value following the same algorithmic steps as AudioSourcePitchValues.
        /// </summary>
        public float GetCurrentPitchValue()
        {
            if (sharedMic == null || sharedMic.Clip == null) return 0;

            // Mirror the AudioSource approach: allocate to clip length, read starting at current write head.
            // This keeps behavior identical to the working AudioSourcePitchValues.
            int position = sharedMic.GetWritePosition();
            if (position < 0) return 0;

            float[] data = new float[sharedMic.Clip.samples];
            sharedMic.Clip.GetData(data, position);

            float newDB = 0f;
            int dataLength = data.Length;

            // Keep flags consistent with your working script: detectInMono=true, "playing" inverted in your usage.
            // For mic capture, pass false for "playing/last" to keep streaming behavior.
            List<float> pitchValues = Detector.getPitch(data, 0, ref dataLength, ref newDB, sampleRate, true, false);

            if (pitchValues == null || pitchValues.Count == 0)
            {
                if (logPitch) Debug.Log("MicrophonePitchValues: no pitch values found");
                return 0;
            }

            float accumulatedPitch = 0f;
            foreach (var pitchVal in pitchValues)
            {
                accumulatedPitch += pitchVal;
            }
            float pitch = accumulatedPitch / pitchValues.Count;

            // Same clamp + map as AudioSourcePitchValues
            pitch = Mathf.Clamp(pitch, pitchLow + pitchLowThreshold, pitchHigh - pitchHighThreshold);
            float mappedPitch = Map(pitch, pitchLow, pitchHigh, pitchMin, pitchMax);
            float result = Mathf.Clamp(mappedPitch, pitchMin, pitchMax);

            if (logPitch)
                Debug.Log($"Mic pitch: {pitch:F1} Hz → {result:F2}");

            return result;
        }

        private static float Map(float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }
    }
}
