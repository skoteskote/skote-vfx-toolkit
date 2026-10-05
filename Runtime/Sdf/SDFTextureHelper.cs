using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Experimental.Rendering;

namespace Skote.Vfx.Sdf
{
    /// <summary>
    /// Creates a RenderTexture at runtime for SDFTexture and assigns it to both
    /// the SDFTexture component and a VFX Graph.
    /// </summary>
    [RequireComponent(typeof(SDFTexture))]
    public class SDFTextureHelper : MonoBehaviour
    {
        [Header("Render Texture Settings")]
        [SerializeField] private int size = 32;

        [Header("VFX Assignment")]
        [SerializeField] private VisualEffect targetVFX;
        [SerializeField] private string vfxPropertyName = "SDF";

        private RenderTexture _sdfTexture;
        private SDFTexture _sdfComponent;

        private void Awake()
        {
            _sdfComponent = GetComponent<SDFTexture>();

            CreateRenderTexture();
            AssignToSDFTexture();
            AssignToVFX();
        }

        private void CreateRenderTexture()
        {
            _sdfTexture = new RenderTexture(size, size, 0, RenderTextureFormat.RHalf)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                volumeDepth = size,
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0,
                useMipMap = false,
                autoGenerateMips = false
            };
            _sdfTexture.Create();
        }

        private void AssignToSDFTexture()
        {
            if (_sdfComponent != null)
            {
                _sdfComponent.sdf = _sdfTexture;
            }
        }

        private void AssignToVFX()
        {
            if (targetVFX != null && !string.IsNullOrEmpty(vfxPropertyName))
            {
                targetVFX.SetTexture(vfxPropertyName, _sdfTexture);
            }
        }

        private void OnDestroy()
        {
            if (_sdfTexture != null)
            {
                _sdfTexture.Release();
                Destroy(_sdfTexture);
            }
        }
    }
}
