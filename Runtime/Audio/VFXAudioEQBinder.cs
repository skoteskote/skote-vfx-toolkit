using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Skote.Vfx.Audio
{
    [AddComponentMenu("VFX/Property Binders/Audio/EQ Binder")]
    [VFXBinder("Audio/EQ")]
    public sealed class VFXAudioEQBinder : VFXBinderBase
    {
        [SerializeField] AudioSourceDecibelValues _source;

        [System.Serializable]
        public class Band
        {
            public string propertyName = "Bass";

            [Header("Frequency Range (Hz)")]
            [Tooltip("Low cutoff frequency. Use 0 for no high-pass.")]
            public float lowCutoff = 20f;
            [Tooltip("High cutoff frequency. Use 0 or very high for no low-pass.")]
            public float highCutoff = 150f;

            [Header("dB Remap")]
            [Tooltip("Minimum expected dB value (silence is around -80)")]
            public float minDB = -60f;
            [Tooltip("Maximum expected dB value (loud is around 0)")]
            public float maxDB = 0f;

            [Header("Output Remap")]
            [Tooltip("Output value when at minDB")]
            public float outputMin = 0f;
            [Tooltip("Output value when at maxDB")]
            public float outputMax = 1f;

            [HideInInspector] public ExposedProperty exposedProperty;
        }

        public Band[] bands = new Band[]
        {
            new Band { propertyName = "Bass",  lowCutoff = 20,   highCutoff = 150 },
            new Band { propertyName = "Mids",  lowCutoff = 150,  highCutoff = 2000 },
            new Band { propertyName = "Highs", lowCutoff = 2000, highCutoff = 20000 },
        };

        private void OnValidate()
        {
            SyncExposedProperties();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SyncExposedProperties();
        }

        private void SyncExposedProperties()
        {
            if (bands == null) return;
            foreach (var band in bands)
                band.exposedProperty = band.propertyName;
        }

        public override bool IsValid(VisualEffect component)
        {
            if (_source == null || bands == null) return false;
            foreach (var band in bands)
            {
                if (string.IsNullOrEmpty(band.propertyName)) return false;
                band.exposedProperty = band.propertyName;
                if (!component.HasFloat(band.exposedProperty)) return false;
            }
            return true;
        }

        public override void UpdateBinding(VisualEffect component)
        {
            foreach (var band in bands)
            {
                float db = _source.GetCurrentDBValueFiltered(band.lowCutoff, band.highCutoff);
                float normalized = Mathf.InverseLerp(band.minDB, band.maxDB, db);
                float output = Mathf.Lerp(band.outputMin, band.outputMax, normalized);
                component.SetFloat(band.exposedProperty, output);
            }
        }

        public override string ToString()
            => $"Audio EQ : {bands?.Length ?? 0} bands";
    }
}
