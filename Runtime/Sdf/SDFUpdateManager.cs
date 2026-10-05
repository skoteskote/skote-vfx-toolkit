using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Skote.Vfx.Sdf
{
    /// <summary>
    /// Manages staggered SDF updates across multiple MeshToSDF components.
    /// Instead of updating all SDFs every frame, updates N per frame in rotation.
    /// </summary>
    public class SDFUpdateManager : MonoBehaviour
    {
        [Header("Update Settings")]
        [Tooltip("How many SDFs to update per frame")]
        [SerializeField] private int updatesPerFrame = 2;

        [Tooltip("Automatically find and register all MeshToSDF components on Start")]
        [SerializeField] private bool autoRegisterOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool logUpdates = false;

        private List<MeshToSDF> _registeredSDFs = new List<MeshToSDF>();
        private int _currentIndex = 0;
        private CommandBuffer _commandBuffer;

        private static SDFUpdateManager _instance;
        public static SDFUpdateManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[SDFUpdateManager] Multiple instances detected, destroying duplicate");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _commandBuffer = new CommandBuffer { name = "SDFUpdateManager" };
        }

        private void Start()
        {
            if (autoRegisterOnStart)
            {
                AutoRegisterAll();
            }
        }

        private void AutoRegisterAll()
        {
            var allSDFs = FindObjectsByType<MeshToSDF>(FindObjectsSortMode.None);
            foreach (var sdf in allSDFs)
            {
                Register(sdf);
            }

            if (logUpdates)
            {
                Debug.Log($"[SDFUpdateManager] Auto-registered {_registeredSDFs.Count} MeshToSDF components");
            }
        }

        /// <summary>
        /// Register a MeshToSDF for managed updates. Sets it to Explicit mode.
        /// </summary>
        public void Register(MeshToSDF sdf)
        {
            if (sdf == null || _registeredSDFs.Contains(sdf)) return;

            sdf.updateMode = MeshToSDF.UpdateMode.Explicit;
            _registeredSDFs.Add(sdf);
        }

        /// <summary>
        /// Unregister a MeshToSDF from managed updates.
        /// </summary>
        public void Unregister(MeshToSDF sdf)
        {
            _registeredSDFs.Remove(sdf);
        }

        private void LateUpdate()
        {
            if (_registeredSDFs.Count == 0) return;

            // Clean up any destroyed references
            _registeredSDFs.RemoveAll(s => s == null);
            if (_registeredSDFs.Count == 0) return;

            _commandBuffer.Clear();

            int updatedCount = 0;
            int startIndex = _currentIndex;

            // Update N SDFs starting from current index
            for (int i = 0; i < updatesPerFrame && i < _registeredSDFs.Count; i++)
            {
                int index = (_currentIndex + i) % _registeredSDFs.Count;
                var sdf = _registeredSDFs[index];

                if (sdf != null && sdf.isActiveAndEnabled)
                {
                    sdf.UpdateSDF(_commandBuffer);
                    updatedCount++;
                }
            }

            // Advance index for next frame
            _currentIndex = (_currentIndex + updatesPerFrame) % _registeredSDFs.Count;

            // Execute all SDF updates in one batch
            if (updatedCount > 0)
            {
                Graphics.ExecuteCommandBuffer(_commandBuffer);

                if (logUpdates)
                {
                    Debug.Log($"[SDFUpdateManager] Updated {updatedCount} SDFs (indices {startIndex}-{(_currentIndex - 1 + _registeredSDFs.Count) % _registeredSDFs.Count})");
                }
            }
        }

        private void OnDestroy()
        {
            if (_commandBuffer != null)
            {
                _commandBuffer.Release();
                _commandBuffer = null;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Force update all SDFs immediately (useful for initial setup or teleports)
        /// </summary>
        public void ForceUpdateAll()
        {
            if (_registeredSDFs.Count == 0) return;

            _commandBuffer.Clear();

            foreach (var sdf in _registeredSDFs)
            {
                if (sdf != null && sdf.isActiveAndEnabled)
                {
                    sdf.UpdateSDF(_commandBuffer);
                }
            }

            Graphics.ExecuteCommandBuffer(_commandBuffer);

            if (logUpdates)
            {
                Debug.Log($"[SDFUpdateManager] Force updated all {_registeredSDFs.Count} SDFs");
            }
        }

        /// <summary>
        /// Get current stats
        /// </summary>
        public (int registered, int updatesPerFrame, float updateFrequency) GetStats()
        {
            float freq = _registeredSDFs.Count > 0
                ? (float)updatesPerFrame / _registeredSDFs.Count * Application.targetFrameRate
                : 0;
            return (_registeredSDFs.Count, updatesPerFrame, freq);
        }
    }
}
