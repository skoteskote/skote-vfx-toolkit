using UnityEngine;

namespace Skote.Vfx.Audio
{
    /// <summary>
    /// Owns a single Microphone.Start per device and shares its AudioClip to any readers.
    /// Put exactly one of these in the scene (or mark it DontDestroyOnLoad).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class SharedMicrophone : MonoBehaviour
    {
        [Header("Microphone")]
        [Tooltip("Null/empty = default device")]
        public string deviceName = null;
        public int sampleRate = 44100;
        [Range(1, 10)] public int audioClipLengthSeconds = 1;
        public bool autoStart = true;

        [Header("Debug")]
        public bool showDebugInfo = false;

        public AudioClip Clip { get; private set; }
        public int SampleRate => sampleRate;
        public string Device => deviceName;
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            if (autoStart) EnsureStarted();
        }

        private void OnDestroy()
        {
            StopMic();
        }

        public void EnsureStarted()
        {
            if (IsRunning && Clip != null)
            {
                Debug.Log("SharedMicrophone: already running.");
                return;
            }


            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("SharedMicrophone: no devices.");
                return;
            }

            Clip = Microphone.Start(deviceName, true, Mathf.Max(1, audioClipLengthSeconds), sampleRate);
            IsRunning = Clip != null;


            if (IsRunning)
                Debug.Log($"SharedMicrophone: started '{(deviceName ?? "Default")}' @ {sampleRate}Hz, {audioClipLengthSeconds}s.");
            else
                Debug.LogError("SharedMicrophone: failed to start.");
        }

        public void StopMic()
        {
            if (!IsRunning) return;
            Microphone.End(deviceName);
            IsRunning = false;
            Clip = null;
            if (showDebugInfo) Debug.Log("SharedMicrophone: stopped.");
        }

        public int GetWritePosition()
        {
            return IsRunning ? Microphone.GetPosition(deviceName) : -1;
        }
    }
}
