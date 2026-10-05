using UnityEngine;
using System.Collections.Generic;

namespace Skote.Vfx.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceDecibelValues : MonoBehaviour
    {
        public float samplesToCalculate = 0.001f;
        public int smoothingBufferSize = 10;

        [Tooltip("Force AudioSource playback to follow game time. Enable for offline/slow rendering so audio stays in sync with frames.")]
        public bool useGameTime = false;

        private AudioSource audioSource;
        private float gameTimePosition;
        private Queue<float> recentDBValues = new Queue<float>();
        private Dictionary<string, Queue<float>> filteredDBBuffers = new Dictionary<string, Queue<float>>();
        private Dictionary<string, BiquadState> filterStates = new Dictionary<string, BiquadState>();

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (useGameTime && audioSource.isPlaying)
            {
                gameTimePosition += Time.deltaTime;
                audioSource.time = Mathf.Min(gameTimePosition, audioSource.clip.length - 0.01f);
            }
        }

        public float GetCurrentDBValue()
        {
            if (audioSource.clip == null) return 0;

            float[] data = GetCurrentSamples(out int samples);

            float dBValue = ComputeRMS_DB(data, samples);

            recentDBValues.Enqueue(dBValue);
            while (recentDBValues.Count > smoothingBufferSize)
                recentDBValues.Dequeue();

            return AverageQueue(recentDBValues);
        }

        /// <summary>
        /// Returns the current dB value filtered to a frequency band.
        /// Use lowCutoff=0 for low-pass only, highCutoff=0 (or >= Nyquist) for high-pass only.
        /// Examples: Bass (20,150), Mids (150,2000), Highs (2000,20000)
        /// </summary>
        public float GetCurrentDBValueFiltered(float lowCutoff, float highCutoff)
        {
            if (audioSource.clip == null) return 0;

            float sampleRate = audioSource.clip.frequency;
            float[] data = GetCurrentSamples(out int samples);

            string key = $"{lowCutoff}_{highCutoff}";

            if (!filterStates.ContainsKey(key))
                filterStates[key] = new BiquadState();
            if (!filteredDBBuffers.ContainsKey(key))
                filteredDBBuffers[key] = new Queue<float>();

            BiquadState state = filterStates[key];
            float[] filtered = new float[samples];
            System.Array.Copy(data, filtered, samples);

            // Apply high-pass if lowCutoff > 0 (removes everything below)
            if (lowCutoff > 0)
            {
                BiquadCoeffs hp = BiquadCoeffs.HighPass(lowCutoff, sampleRate, 0.707f);
                ApplyBiquad(filtered, hp, ref state.hp_x1, ref state.hp_x2, ref state.hp_y1, ref state.hp_y2);
            }

            // Apply low-pass if highCutoff > 0 and below Nyquist (removes everything above)
            if (highCutoff > 0 && highCutoff < sampleRate * 0.5f)
            {
                BiquadCoeffs lp = BiquadCoeffs.LowPass(highCutoff, sampleRate, 0.707f);
                ApplyBiquad(filtered, lp, ref state.lp_x1, ref state.lp_x2, ref state.lp_y1, ref state.lp_y2);
            }

            float dBValue = ComputeRMS_DB(filtered, samples);

            Queue<float> buffer = filteredDBBuffers[key];
            buffer.Enqueue(dBValue);
            while (buffer.Count > smoothingBufferSize)
                buffer.Dequeue();

            return AverageQueue(buffer);
        }

        private float[] GetCurrentSamples(out int sampleCount)
        {
            sampleCount = Mathf.RoundToInt(samplesToCalculate * audioSource.clip.frequency);
            float time = useGameTime ? gameTimePosition : audioSource.time;
            int currentSample = Mathf.RoundToInt(time * audioSource.clip.frequency);
            int startSample = Mathf.Max(currentSample - sampleCount, 0);
            float[] data = new float[sampleCount];
            audioSource.clip.GetData(data, startSample);
            return data;
        }

        private static float ComputeRMS_DB(float[] data, int samples)
        {
            float sumOfSquares = 0;
            for (int i = 0; i < samples; i++)
                sumOfSquares += data[i] * data[i];
            float rms = Mathf.Sqrt(sumOfSquares / samples);
            return 20f * Mathf.Log10(rms);
        }

        private static float AverageQueue(Queue<float> queue)
        {
            float sum = 0;
            foreach (float val in queue)
                sum += val;
            return sum / queue.Count;
        }

        private static void ApplyBiquad(float[] data, BiquadCoeffs c,
            ref float x1, ref float x2, ref float y1, ref float y2)
        {
            for (int i = 0; i < data.Length; i++)
            {
                float x0 = data[i];
                float y0 = c.b0 * x0 + c.b1 * x1 + c.b2 * x2 - c.a1 * y1 - c.a2 * y2;
                x2 = x1; x1 = x0;
                y2 = y1; y1 = y0;
                data[i] = y0;
            }
        }

        private struct BiquadCoeffs
        {
            public float b0, b1, b2, a1, a2;

            public static BiquadCoeffs LowPass(float freq, float sampleRate, float Q)
            {
                float w0 = 2f * Mathf.PI * freq / sampleRate;
                float alpha = Mathf.Sin(w0) / (2f * Q);
                float cosw0 = Mathf.Cos(w0);
                float a0 = 1f + alpha;
                return new BiquadCoeffs
                {
                    b0 = ((1f - cosw0) / 2f) / a0,
                    b1 = (1f - cosw0) / a0,
                    b2 = ((1f - cosw0) / 2f) / a0,
                    a1 = (-2f * cosw0) / a0,
                    a2 = (1f - alpha) / a0,
                };
            }

            public static BiquadCoeffs HighPass(float freq, float sampleRate, float Q)
            {
                float w0 = 2f * Mathf.PI * freq / sampleRate;
                float alpha = Mathf.Sin(w0) / (2f * Q);
                float cosw0 = Mathf.Cos(w0);
                float a0 = 1f + alpha;
                return new BiquadCoeffs
                {
                    b0 = ((1f + cosw0) / 2f) / a0,
                    b1 = (-(1f + cosw0)) / a0,
                    b2 = ((1f + cosw0) / 2f) / a0,
                    a1 = (-2f * cosw0) / a0,
                    a2 = (1f - alpha) / a0,
                };
            }
        }

        private class BiquadState
        {
            public float hp_x1, hp_x2, hp_y1, hp_y2;
            public float lp_x1, lp_x2, lp_y1, lp_y2;
        }
    }
}
