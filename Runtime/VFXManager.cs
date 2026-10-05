using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;

namespace Skote.Vfx
{
    public class VFXManager : MonoBehaviour
    {
        [Header("VFX Groups (Kill/Revive)")]
        [Tooltip("Groups of VFX that can be killed or revived together. Reference root GameObjects - VFX components are found automatically in children.")]
        [SerializeField] private List<VFXGroup> vfxGroups = new List<VFXGroup>();
        [SerializeField] private string rateMultiplierProperty = "RateMultiplier";
        [SerializeField] private string lifeRangeProperty = "LifeRange";
        [Tooltip("Fallback lifetime if VFX doesn't have LifeRange property")]
        [SerializeField] private float fallbackMaxLifetime = 5f;

        [Header("Additional VFX (Optional)")]
        [Tooltip("Additional VFX not in any group but should receive property changes")]
        [SerializeField] private List<VisualEffect> additionalVFX = new List<VisualEffect>();

        [Header("Float Properties")]
        [SerializeField] private List<VFXFloatProperty> floatProperties = new List<VFXFloatProperty>();

        [Header("Vector2 Properties")]
        [SerializeField] private List<VFXVector2Property> vector2Properties = new List<VFXVector2Property>();

        private Dictionary<string, VFXFloatProperty> _floatLookup;
        private Dictionary<string, VFXVector2Property> _vector2Lookup;
        private Dictionary<string, VFXGroup> _groupLookup;
        private Dictionary<string, Coroutine> _killCoroutines = new Dictionary<string, Coroutine>();
        private List<VisualEffect> _allVFX = new List<VisualEffect>();

        private void Awake()
        {
            ResolveAllVFX();
            BuildLookups();
        }

        private void ResolveAllVFX()
        {
            _allVFX.Clear();

            // Resolve VFX components from GameObjects in each group
            foreach (var group in vfxGroups)
            {
                group.ResolveVFX();

                // Add to combined list (avoid duplicates)
                foreach (var vfx in group.ResolvedVFX)
                {
                    if (vfx != null && !_allVFX.Contains(vfx))
                    {
                        _allVFX.Add(vfx);
                    }
                }
            }

            // Add any additional VFX not in groups
            foreach (var vfx in additionalVFX)
            {
                if (vfx != null && !_allVFX.Contains(vfx))
                {
                    _allVFX.Add(vfx);
                }
            }

            Debug.Log($"[VFXManager] Resolved {_allVFX.Count} total VFX from {vfxGroups.Count} groups + {additionalVFX.Count} additional");
        }

        private void BuildLookups()
        {
            _floatLookup = new Dictionary<string, VFXFloatProperty>();
            foreach (var prop in floatProperties)
            {
                if (!string.IsNullOrEmpty(prop.propertyName))
                {
                    _floatLookup[prop.propertyName] = prop;
                }
            }

            _vector2Lookup = new Dictionary<string, VFXVector2Property>();
            foreach (var prop in vector2Properties)
            {
                if (!string.IsNullOrEmpty(prop.propertyName))
                {
                    _vector2Lookup[prop.propertyName] = prop;
                }
            }

            _groupLookup = new Dictionary<string, VFXGroup>();
            foreach (var group in vfxGroups)
            {
                if (!string.IsNullOrEmpty(group.groupName))
                {
                    _groupLookup[group.groupName.ToLower()] = group;
                }
            }
        }

        public void AddVFX(VisualEffect vfx)
        {
            if (vfx != null && !_allVFX.Contains(vfx))
            {
                _allVFX.Add(vfx);
            }
        }

        public void RemoveVFX(VisualEffect vfx)
        {
            _allVFX.Remove(vfx);
        }

        /// <summary>
        /// Set a property by name on ALL VFX. Automatically detects if it's a float or Vector2 property.
        /// </summary>
        public void SetProperty(string propertyName, float t)
        {
            if (string.IsNullOrEmpty(propertyName)) return;
            if (_floatLookup == null || _vector2Lookup == null) BuildLookups();

            t = Mathf.Clamp01(t);

            // Check float properties first
            if (_floatLookup.TryGetValue(propertyName, out var floatProp))
            {
                float value = Mathf.Lerp(floatProp.min, floatProp.max, t);
                foreach (var vfx in _allVFX)
                {
                    if (vfx != null && vfx.HasFloat(propertyName))
                    {
                        vfx.SetFloat(propertyName, value);
                    }
                }
                return;
            }

            // Check Vector2 properties
            if (_vector2Lookup.TryGetValue(propertyName, out var vec2Prop))
            {
                Vector2 value = Vector2.Lerp(vec2Prop.min, vec2Prop.max, t);
                foreach (var vfx in _allVFX)
                {
                    if (vfx != null && vfx.HasVector2(propertyName))
                    {
                        vfx.SetVector2(propertyName, value);
                    }
                }
            }
        }

        /// <summary>
        /// Kill a VFX group by setting RateMultiplier to 0, then disabling after particles die out.
        /// </summary>
        public void KillGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return;
            if (_groupLookup == null) BuildLookups();

            string key = groupName.ToLower();

            if (_groupLookup.TryGetValue(key, out var group))
            {
                // Cancel any existing kill coroutine for this group
                if (_killCoroutines.TryGetValue(key, out var existingCoroutine) && existingCoroutine != null)
                {
                    StopCoroutine(existingCoroutine);
                }

                // Find max lifetime across all VFX in group
                float maxLifetime = 0f;
                foreach (var vfx in group.ResolvedVFX)
                {
                    if (vfx != null)
                    {
                        // Stop spawning immediately
                        if (vfx.HasFloat(rateMultiplierProperty))
                        {
                            vfx.SetFloat(rateMultiplierProperty, 0f);
                        }

                        // Get max lifetime from LifeRange.y
                        if (vfx.HasVector2(lifeRangeProperty))
                        {
                            Vector2 lifeRange = vfx.GetVector2(lifeRangeProperty);
                            maxLifetime = Mathf.Max(maxLifetime, lifeRange.y);
                        }
                    }
                }

                // Use fallback if no LifeRange found
                if (maxLifetime <= 0f)
                {
                    maxLifetime = fallbackMaxLifetime;
                }

                // Start coroutine to disable after particles die
                _killCoroutines[key] = StartCoroutine(DisableAfterDelay(group, key, maxLifetime));
            }
            else
            {
                Debug.LogWarning($"[VFXManager] Group '{groupName}' not found");
            }
        }

        private IEnumerator DisableAfterDelay(VFXGroup group, string groupKey, float delay)
        {
            yield return new WaitForSeconds(delay);

            foreach (var vfx in group.ResolvedVFX)
            {
                if (vfx != null)
                {
                    vfx.enabled = false;
                }
            }

            _killCoroutines.Remove(groupKey);
        }

        /// <summary>
        /// Revive a VFX group by enabling and setting RateMultiplier to 1.
        /// </summary>
        public void ReviveGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return;
            if (_groupLookup == null) BuildLookups();

            string key = groupName.ToLower();

            // Cancel any pending kill coroutine
            if (_killCoroutines.TryGetValue(key, out var existingCoroutine) && existingCoroutine != null)
            {
                StopCoroutine(existingCoroutine);
                _killCoroutines.Remove(key);
            }

            if (_groupLookup.TryGetValue(key, out var group))
            {
                foreach (var vfx in group.ResolvedVFX)
                {
                    if (vfx != null)
                    {
                        // Re-enable first
                        vfx.enabled = true;

                        // Then set rate
                        if (vfx.HasFloat(rateMultiplierProperty))
                        {
                            vfx.SetFloat(rateMultiplierProperty, 1f);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[VFXManager] Group '{groupName}' not found");
            }
        }

        /// <summary>
        /// Toggle a bool property on all VFX.
        /// </summary>
        public void ToggleBool(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            foreach (var vfx in _allVFX)
            {
                if (vfx != null && vfx.HasBool(propertyName))
                {
                    bool current = vfx.GetBool(propertyName);
                    vfx.SetBool(propertyName, !current);
                }
            }
        }

        /// <summary>
        /// Process a group action string like "KillA" or "ReviveB".
        /// Returns true if the action was handled.
        /// </summary>
        public bool ProcessGroupAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return false;

            string actionLower = action.ToLower();

            if (actionLower.StartsWith("kill"))
            {
                string groupName = action.Substring(4);
                KillGroup(groupName);
                return true;
            }
            else if (actionLower.StartsWith("revive"))
            {
                string groupName = action.Substring(6);
                ReviveGroup(groupName);
                return true;
            }

            return false;
        }
    }

    [System.Serializable]
    public class VFXFloatProperty
    {
        [Tooltip("Property name in the VFX Graph (must match exactly)")]
        public string propertyName;

        [Tooltip("Value when knob is at 0")]
        public float min;

        [Tooltip("Value when knob is at 1")]
        public float max = 1f;
    }

    [System.Serializable]
    public class VFXGroup
    {
        [Tooltip("Name of this group (used in actions like 'KillA' or 'ReviveB')")]
        public string groupName;

        [Tooltip("Root GameObjects containing VFX (VFX component found in self or children)")]
        public List<GameObject> gameObjects = new List<GameObject>();

        // Resolved at runtime
        [System.NonSerialized]
        public List<VisualEffect> ResolvedVFX = new List<VisualEffect>();

        public void ResolveVFX()
        {
            ResolvedVFX.Clear();

            foreach (var go in gameObjects)
            {
                if (go == null) continue;

                // Try to get VFX on the object itself first
                var vfx = go.GetComponent<VisualEffect>();
                if (vfx != null)
                {
                    ResolvedVFX.Add(vfx);
                }
                else
                {
                    // Otherwise search in children
                    vfx = go.GetComponentInChildren<VisualEffect>();
                    if (vfx != null)
                    {
                        ResolvedVFX.Add(vfx);
                    }
                }
            }
        }
    }
}
