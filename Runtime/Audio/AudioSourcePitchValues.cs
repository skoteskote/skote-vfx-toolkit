using UnityEngine;
using System.Collections.Generic;
using Skote.Vfx.Audio.PitchDetection;

namespace Skote.Vfx.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourcePitchValues : MonoBehaviour
    {
        private AudioSource audioSource;

        [Header("Pitch Settings")]
        public int sampleRate = 44100;
        public float pitchLow = 0;
        public float pitchLowThreshold = 10f;
        public float pitchHigh = 500;
        public float pitchHighThreshold = 5f;
        public float pitchMin = 0;
        public float pitchMax = 50;
        public float pitchWindowSize = 0.1f;

        // Introducing window size for chunk processing
        private int WINDOW_SIZE = 0;
        private float[] data;

        private RAPTPitchDetector Detector;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            WINDOW_SIZE = (int)(pitchWindowSize * sampleRate);
            data = new float[WINDOW_SIZE];
            Detector = new RAPTPitchDetector(sampleRate, 50f, 800f);
        }

        public float GetCurrentPitchValue()
        {
            if (audioSource == null || audioSource.clip == null) return 0;

            data = new float[audioSource.clip.samples];
            int position = audioSource.timeSamples;
            audioSource.clip.GetData(data, position);


            if (data.Length < WINDOW_SIZE) return 0;

            float newDB = 0f;
            int dataLength = data.Length;
            List<float> pitchValues = Detector.getPitch(data, 0, ref dataLength, ref newDB, sampleRate, true, !audioSource.isPlaying);

            if (pitchValues == null || pitchValues.Count == 0)
            {
                Debug.Log("No pitch values found");
                return 0;
            }


            float accumulatedPitch = 0;
            foreach (var pitchVal in pitchValues)
            {
                accumulatedPitch += pitchVal;
            }
            float pitch = accumulatedPitch / pitchValues.Count;

            pitch = Mathf.Clamp(pitch, pitchLow + pitchLowThreshold, pitchHigh - pitchHighThreshold);
            float mappedPitch = Map(pitch, pitchLow, pitchHigh, pitchMin, pitchMax);
            return Mathf.Clamp(mappedPitch, pitchMin, pitchMax);
        }

        private float Map(float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

    }
}
