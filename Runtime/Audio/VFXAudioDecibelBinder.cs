using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Skote.Vfx.Audio
{
    [AddComponentMenu("VFX/Property Binders/Audio/Decibel Binder")]
    [VFXBinder("Audio/Decibel")]
    public sealed class VFXAudioDecibelBinder : VFXBinderBase
    {
        [SerializeField] AudioSourceDecibelValues _source;

        public string DecibelProperty
        {
            get => (string)_decibelProperty;
            set => _decibelProperty = value;
        }

        [VFXPropertyBinding("System.Single"), SerializeField]
        ExposedProperty _decibelProperty = "Decibel";

        [Header("Value Remapping")]
        [Tooltip("Minimum expected dB value (silence is around -80)")]
        public float minDB = -60f;
        [Tooltip("Maximum expected dB value (loud is around 0)")]
        public float maxDB = 0f;
        [Tooltip("Output value when at minDB")]
        public float outputMin = 0f;
        [Tooltip("Output value when at maxDB")]
        public float outputMax = 1f;

        public override bool IsValid(VisualEffect component)
            => _source != null && component.HasFloat(_decibelProperty);

        public override void UpdateBinding(VisualEffect component)
        {
            float db = _source.GetCurrentDBValue();

            // Clamp and remap from dB range to output range
            float normalized = Mathf.InverseLerp(minDB, maxDB, db);
            float output = Mathf.Lerp(outputMin, outputMax, normalized);

            component.SetFloat(_decibelProperty, output);
        }

        public override string ToString()
            => $"Audio Decibel : {_decibelProperty}";
    }
}
