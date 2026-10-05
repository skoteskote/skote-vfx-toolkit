#if SKOTE_MESH_SIMPLIFIER
using UnityEngine;
using UnityMeshSimplifier;

namespace Skote.Vfx.Sdf
{
    /// <summary>
    /// Simplifies a SkinnedMeshRenderer's mesh at runtime for cheaper SDF generation.
    /// Preserves bone weights and bindings.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before MeshToSDF
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class SDFMeshSimplifier : MonoBehaviour
    {
        [Header("Simplification Settings")]
        [Tooltip("Target quality (0-1). 0.1 = 10% of original triangles")]
        [Range(0.01f, 1f)]
        [SerializeField] private float quality = 0.1f;

        [Tooltip("Preserve bone weights during simplification")]
        [SerializeField] private bool preserveBoneWeights = true;

        [Header("Debug")]
        [SerializeField] private bool logStats = true;

        private SkinnedMeshRenderer _skinnedMeshRenderer;
        private Mesh _originalMesh;
        private Mesh _simplifiedMesh;

        private void Awake()
        {
            _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            _originalMesh = _skinnedMeshRenderer.sharedMesh;

            if (_originalMesh == null)
            {
                Debug.LogError("[SDFMeshSimplifier] No mesh assigned to SkinnedMeshRenderer", this);
                return;
            }

            SimplifyMesh();
        }

        private void SimplifyMesh()
        {
            var simplifier = new MeshSimplifier(_originalMesh);

            // Configure simplification options - relaxed for SDF use case
            var options = new SimplificationOptions
            {
                PreserveBorderEdges = false,        // Don't need for SDF
                PreserveUVSeamEdges = false,        // Don't need for SDF
                PreserveUVFoldoverEdges = false,    // Don't need for SDF
                PreserveSurfaceCurvature = false,   // Allow aggressive simplification
                EnableSmartLink = true,
                VertexLinkDistance = 0.0001f,       // Merge very close vertices
                MaxIterationCount = 100,            // More iterations for better results
                Agressiveness = 7.0                 // Higher = more aggressive
            };
            simplifier.SimplificationOptions = options;

            if (logStats)
            {
                Debug.Log($"[SDFMeshSimplifier] Simplifying {_originalMesh.name} with quality {quality}");
            }

            // Simplify to target quality
            simplifier.SimplifyMesh(quality);

            // Get the simplified mesh
            _simplifiedMesh = simplifier.ToMesh();
            _simplifiedMesh.name = $"{_originalMesh.name}_Simplified";

            // Copy bone data from original mesh
            if (preserveBoneWeights)
            {
                _simplifiedMesh.bindposes = _originalMesh.bindposes;
            }

            // Assign to the renderer
            _skinnedMeshRenderer.sharedMesh = _simplifiedMesh;

            if (logStats)
            {
                int originalTris = _originalMesh.triangles.Length / 3;
                int simplifiedTris = _simplifiedMesh.triangles.Length / 3;
                float reduction = (1f - (float)simplifiedTris / originalTris) * 100f;
                Debug.Log($"[SDFMeshSimplifier] {_originalMesh.name}: {originalTris} -> {simplifiedTris} tris ({reduction:F1}% reduction) [target quality: {quality}]", this);
            }
        }

        private void Start()
        {
            // Verify after all Awake/OnEnable calls
            if (logStats && _skinnedMeshRenderer != null)
            {
                var currentMesh = _skinnedMeshRenderer.sharedMesh;
                int currentTris = currentMesh != null ? currentMesh.triangles.Length / 3 : 0;
                Debug.Log($"[SDFMeshSimplifier] Verification in Start - sharedMesh: {currentMesh?.name}, tris: {currentTris}", this);
            }
        }

        private void OnDestroy()
        {
            // Clean up the runtime-created mesh
            if (_simplifiedMesh != null)
            {
                Destroy(_simplifiedMesh);
            }
        }

        /// <summary>
        /// Restore the original mesh (useful for editor testing)
        /// </summary>
        public void RestoreOriginalMesh()
        {
            if (_skinnedMeshRenderer != null && _originalMesh != null)
            {
                _skinnedMeshRenderer.sharedMesh = _originalMesh;
            }
        }
    }
}
#endif
