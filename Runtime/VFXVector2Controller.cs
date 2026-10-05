using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

namespace Skote.Vfx
{
    public class VFXVector2Controller : MonoBehaviour
    {
        [SerializeField] private VisualEffect targetVFX;

        [SerializeField] private List<VFXVector2Property> properties = new List<VFXVector2Property>();

        private Dictionary<string, VFXVector2Property> _propertyLookup;

        private void Reset()
        {
            targetVFX = GetComponent<VisualEffect>();
        }

        private void Awake()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _propertyLookup = new Dictionary<string, VFXVector2Property>();
            foreach (var prop in properties)
            {
                if (!string.IsNullOrEmpty(prop.propertyName))
                {
                    _propertyLookup[prop.propertyName] = prop;
                }
            }
        }

        public void SetProperty(string propertyName, float t)
        {
            if (targetVFX == null || string.IsNullOrEmpty(propertyName)) return;

            // Rebuild lookup if needed (e.g., called before Awake or after list changed)
            if (_propertyLookup == null) BuildLookup();

            if (_propertyLookup.TryGetValue(propertyName, out var prop))
            {
                t = Mathf.Clamp01(t);
                Vector2 value = Vector2.Lerp(prop.min, prop.max, t);
                targetVFX.SetVector2(propertyName, value);
            }
        }
    }

    [System.Serializable]
    public class VFXVector2Property
    {
        [Tooltip("Property name in the VFX Graph (must match exactly)")]
        public string propertyName;

        [Tooltip("Value when knob is at 0")]
        public Vector2 min;

        [Tooltip("Value when knob is at 1")]
        public Vector2 max;
    }
}
