using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

namespace Skote.Vfx.Sdf
{
    /// <summary>
    /// Manages multiple animators, allowing synchronized control of speed, triggers, and bools.
    /// Also manages SDF switching for VFX graphs.
    /// </summary>
    public class AnimatorManager : MonoBehaviour
    {
        [SerializeField] private List<Animator> animators = new List<Animator>();

        [Header("Speed Settings")]
        [SerializeField] private float _minSpeed = 0f;
        [SerializeField] private float _maxSpeed = 2f;

        [Header("SDF Switching")]
        [SerializeField] private List<SDFSwitchGroup> sdfGroups = new List<SDFSwitchGroup>();

        private float _currentSpeed = 1f;
        private bool _isPaused;
        private float _speedBeforePause;
        private Dictionary<string, SDFSwitchGroup> _sdfGroupLookup;

        public float CurrentSpeed => _currentSpeed;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            InitializeSDFGroups();
        }

        private void InitializeSDFGroups()
        {
            _sdfGroupLookup = new Dictionary<string, SDFSwitchGroup>();

            foreach (var group in sdfGroups)
            {
                if (string.IsNullOrEmpty(group.groupName)) continue;

                group.ResolveSDF();
                _sdfGroupLookup[group.groupName.ToLower()] = group;

                // Set initial SDF
                if (group.ResolvedSDFTextures.Count > 0)
                {
                    group.ApplyCurrentSDF();
                }
            }
        }

        public void AddAnimator(Animator animator)
        {
            if (animator != null && !animators.Contains(animator))
            {
                animators.Add(animator);
            }
        }

        public void RemoveAnimator(Animator animator)
        {
            animators.Remove(animator);
        }

        public void ClearAllAnimators()
        {
            animators.Clear();
        }

        /// <summary>
        /// Set speed on all animators (0-1 normalized input mapped to minSpeed-maxSpeed).
        /// </summary>
        public void SetSpeed(float normalizedValue)
        {
            if (_isPaused) return;

            _currentSpeed = Mathf.Lerp(_minSpeed, _maxSpeed, Mathf.Clamp01(normalizedValue));
            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    animator.speed = _currentSpeed;
                }
            }
        }

        /// <summary>
        /// Set speed as absolute value on all animators.
        /// </summary>
        public void SetSpeedAbsolute(float speed)
        {
            if (_isPaused) return;

            _currentSpeed = Mathf.Clamp(speed, _minSpeed, _maxSpeed);
            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    animator.speed = _currentSpeed;
                }
            }
        }

        /// <summary>
        /// Send a trigger to all animators.
        /// </summary>
        public void SendTrigger(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) return;

            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    animator.SetTrigger(triggerName);
                }
            }
        }

        /// <summary>
        /// Set a bool parameter on all animators.
        /// </summary>
        public void SetBool(string paramName, bool value)
        {
            if (string.IsNullOrEmpty(paramName)) return;

            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    animator.SetBool(paramName, value);
                }
            }
        }

        /// <summary>
        /// Set a float parameter on all animators.
        /// </summary>
        public void SetFloat(string paramName, float value)
        {
            if (string.IsNullOrEmpty(paramName)) return;

            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    animator.SetFloat(paramName, value);
                }
            }
        }

        /// <summary>
        /// Set an int parameter on all animators.
        /// </summary>
        public void SetInt(string paramName, int value)
        {
            if (string.IsNullOrEmpty(paramName)) return;

            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    animator.SetInteger(paramName, value);
                }
            }
        }

        /// <summary>
        /// Toggle pause state on all animators.
        /// </summary>
        public void TogglePause()
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// Pause all animators.
        /// </summary>
        public void Pause()
        {
            if (_isPaused) return;

            _speedBeforePause = _currentSpeed;
            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    animator.speed = 0f;
                }
            }
            _isPaused = true;
        }

        /// <summary>
        /// Resume all animators at the speed before pause.
        /// </summary>
        public void Resume()
        {
            if (!_isPaused) return;

            _currentSpeed = _speedBeforePause;
            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    animator.speed = _currentSpeed;
                }
            }
            _isPaused = false;
        }

        #region SDF Switching

        /// <summary>
        /// Switch to the next SDF in the specified group.
        /// </summary>
        public void NextSDF(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return;
            if (_sdfGroupLookup == null) InitializeSDFGroups();

            if (_sdfGroupLookup.TryGetValue(groupName.ToLower(), out var group))
            {
                group.Next();
            }
            else
            {
                Debug.LogWarning($"[AnimatorManager] SDF group '{groupName}' not found");
            }
        }

        /// <summary>
        /// Switch to the previous SDF in the specified group.
        /// </summary>
        public void PrevSDF(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return;
            if (_sdfGroupLookup == null) InitializeSDFGroups();

            if (_sdfGroupLookup.TryGetValue(groupName.ToLower(), out var group))
            {
                group.Prev();
            }
            else
            {
                Debug.LogWarning($"[AnimatorManager] SDF group '{groupName}' not found");
            }
        }

        /// <summary>
        /// Set a specific SDF by index in the specified group.
        /// </summary>
        public void SetSDF(string groupName, int index)
        {
            if (string.IsNullOrEmpty(groupName)) return;
            if (_sdfGroupLookup == null) InitializeSDFGroups();

            if (_sdfGroupLookup.TryGetValue(groupName.ToLower(), out var group))
            {
                group.SetIndex(index);
            }
            else
            {
                Debug.LogWarning($"[AnimatorManager] SDF group '{groupName}' not found");
            }
        }

        /// <summary>
        /// Process an SDF action string like "NextMyGroup" or "PrevMyGroup".
        /// Returns true if the action was handled.
        /// </summary>
        public bool ProcessSDFAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return false;

            string actionLower = action.ToLower();

            if (actionLower.StartsWith("next"))
            {
                string groupName = action.Substring(4);
                NextSDF(groupName);
                return true;
            }
            else if (actionLower.StartsWith("prev"))
            {
                string groupName = action.Substring(4);
                PrevSDF(groupName);
                return true;
            }

            return false;
        }

        #endregion
    }

    [System.Serializable]
    public class SDFSwitchGroup
    {
        [Tooltip("Name of this group (used in actions like 'NextMyGroup' or 'PrevMyGroup')")]
        public string groupName;

        [Tooltip("The VFX to switch SDF on")]
        public VisualEffect targetVFX;

        [Tooltip("SDF property name in the VFX Graph")]
        public string sdfPropertyName = "SDF";

        [Tooltip("GameObjects containing SDFTexture components")]
        public List<GameObject> sdfSources = new List<GameObject>();

        [Tooltip("Loop when reaching the end")]
        public bool loop = true;

        // Runtime
        [System.NonSerialized] public List<SDFTexture> ResolvedSDFTextures = new List<SDFTexture>();
        [System.NonSerialized] public int currentIndex = 0;

        public void ResolveSDF()
        {
            ResolvedSDFTextures.Clear();

            foreach (var go in sdfSources)
            {
                if (go == null) continue;

                var sdfTex = go.GetComponent<SDFTexture>();
                if (sdfTex == null)
                {
                    sdfTex = go.GetComponentInChildren<SDFTexture>();
                }

                if (sdfTex != null)
                {
                    ResolvedSDFTextures.Add(sdfTex);
                }
            }
        }

        public void Next()
        {
            if (ResolvedSDFTextures.Count == 0) return;

            currentIndex++;
            if (currentIndex >= ResolvedSDFTextures.Count)
            {
                currentIndex = loop ? 0 : ResolvedSDFTextures.Count - 1;
            }

            ApplyCurrentSDF();
        }

        public void Prev()
        {
            if (ResolvedSDFTextures.Count == 0) return;

            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = loop ? ResolvedSDFTextures.Count - 1 : 0;
            }

            ApplyCurrentSDF();
        }

        public void SetIndex(int index)
        {
            if (ResolvedSDFTextures.Count == 0) return;

            currentIndex = Mathf.Clamp(index, 0, ResolvedSDFTextures.Count - 1);
            ApplyCurrentSDF();
        }

        public void ApplyCurrentSDF()
        {
            if (targetVFX == null || ResolvedSDFTextures.Count == 0) return;
            if (currentIndex < 0 || currentIndex >= ResolvedSDFTextures.Count) return;

            var sdfTexture = ResolvedSDFTextures[currentIndex];
            if (sdfTexture != null && sdfTexture.sdf != null)
            {
                targetVFX.SetTexture(sdfPropertyName, sdfTexture.sdf);
                Debug.Log($"[AnimatorManager] SDF '{groupName}' switched to index {currentIndex}: {sdfSources[currentIndex]?.name}");
            }
        }
    }
}
