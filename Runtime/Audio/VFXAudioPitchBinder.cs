using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Skote.Vfx.Audio
{
    [AddComponentMenu("VFX/Property Binders/Audio/Pitch Binder")]
    [VFXBinder("Audio/Pitch")]
    public sealed class VFXAudioPitchBinder : VFXBinderBase
    {
        [SerializeField] AudioSourcePitchValues _source;

        public string PitchProperty
        {
            get => (string)_pitchProperty;
            set => _pitchProperty = value;
        }

        [VFXPropertyBinding("System.Single"), SerializeField]
        ExposedProperty _pitchProperty = "Pitch";

        public override bool IsValid(VisualEffect component)
            => _source != null && component.HasFloat(_pitchProperty);

        public override void UpdateBinding(VisualEffect component)
        {
            float pitch = _source.GetCurrentPitchValue();
            component.SetFloat(_pitchProperty, pitch);
        }

        public override string ToString()
            => $"Audio Pitch : {_pitchProperty}";
    }
}
