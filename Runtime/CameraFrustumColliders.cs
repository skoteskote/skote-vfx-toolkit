using UnityEngine;
using UnityEngine.VFX;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Skote.Vfx
{
    /// <summary>
    /// Computes 6 plane positions/normals aligned to a camera frustum (Top/Bottom/Left/Right + custom Near/Far),
    /// and pushes them into one or more VFX Graphs as Vector3 parameters:
    ///   {Top|Bottom|Left|Right|Near|Far}PlanePosition
    ///   {Top|Bottom|Left|Right|Near|Far}PlaneNormal
    ///
    /// Debug is drawn via Gizmos (no generated scene objects).
    /// </summary>
    [ExecuteAlways]
    public sealed class CameraFrustumColliders : MonoBehaviour
    {
        [Header("Camera")]
        public Camera targetCamera;

        [Header("Debug (Gizmos)")]
        public bool showDebugVisuals = true;
        public bool drawOnlyWhenSelected = false;
        [Tooltip("Half-size of the debug plane quad drawn in the Scene view.")]
        public float debugPlaneHalfSize = 5f;
        [Tooltip("Length of the normal line drawn from each plane position.")]
        public float debugNormalLength = 2f;

        [Header("Plane Settings")]
        [Tooltip("Distance to move side planes inward along their normal.")]
        public float marginDistance = 0.1f;

        [Tooltip("Custom near plane distance from camera (for collision plane data, not camera clip plane).")]
        public float nearPlaneDistance = 0.5f;

        [Tooltip("Custom far plane distance from camera (for collision plane data, not camera clip plane).")]
        public float farPlaneDistance = 10f;

        [Header("VFX Graph Reference")]
        public VisualEffect[] vfxGraphs;

        [System.Serializable]
        public sealed class PlaneData
        {
            public Vector3 normal;
            public Vector3 position;
        }

        // Top, Bottom, Left, Right, Near, Far
        [SerializeField] private PlaneData[] planeData = new PlaneData[6];

        private static readonly string[] PlaneNames = { "Top", "Bottom", "Left", "Right", "Near", "Far" };

    #if UNITY_EDITOR
        private bool _editorHooked;
    #endif

        private void Reset()
        {
            // Convenient default when you add the component.
            if (!targetCamera) targetCamera = Camera.main;

            if ((vfxGraphs == null || vfxGraphs.Length == 0) && TryGetComponent(out VisualEffect vfx))
                vfxGraphs = new[] { vfx };

            EnsurePlaneData();
            RecomputeAndPush();
        }

        private void OnEnable()
        {
            if (!targetCamera) targetCamera = Camera.main;

            EnsurePlaneData();

            // If this script previously spawned "DebugVisuals" children (old version),
            // remove them so nothing stays stuck in the scene.
            CleanupLegacyDebugChildren();

            RecomputeAndPush();

    #if UNITY_EDITOR
            HookEditorUpdate();
    #endif
        }

        private void OnDisable()
        {
    #if UNITY_EDITOR
            UnhookEditorUpdate();
    #endif
        }

        private void OnValidate()
        {
            // Clamp obviously invalid values.
            if (nearPlaneDistance < 0f) nearPlaneDistance = 0f;
            if (farPlaneDistance < 0f) farPlaneDistance = 0f;
            if (debugPlaneHalfSize < 0.01f) debugPlaneHalfSize = 0.01f;
            if (debugNormalLength < 0f) debugNormalLength = 0f;

            EnsurePlaneData();
            RecomputeAndPush();
        }

        private void Update()
        {
            // Runtime: keep it live.
            if (Application.isPlaying)
                RecomputeAndPush();
        }

        private void EnsurePlaneData()
        {
            if (planeData == null || planeData.Length != 6)
                planeData = new PlaneData[6];

            for (int i = 0; i < 6; i++)
            {
                if (planeData[i] == null)
                    planeData[i] = new PlaneData();
            }
        }

        private void CleanupLegacyDebugChildren()
        {
            // Old script created a child named "DebugVisuals". Remove it deterministically.
            var legacy = transform.Find("DebugVisuals");
            if (!legacy) return;

    #if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(legacy.gameObject);
            else
                Destroy(legacy.gameObject);
    #else
            Destroy(legacy.gameObject);
    #endif
        }

        private void RecomputeAndPush()
        {
            if (!targetCamera)
                return;

            UpdateFrustumPlanes();
            UpdateVFXGraphs();
        }

        private void UpdateFrustumPlanes()
        {
            var t = targetCamera.transform;

            // Use camera clip planes to compute the frustum sides.
            float near = targetCamera.nearClipPlane;
            float far = targetCamera.farClipPlane;
            float aspect = targetCamera.aspect;

            float fovHalfRad = targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float tanFov = Mathf.Tan(fovHalfRad);

            float nearHeight = 2f * tanFov * near;
            float nearWidth = nearHeight * aspect;

            float farHeight = 2f * tanFov * far;
            float farWidth = farHeight * aspect;

            Vector3 camForward = t.forward;
            Vector3 camRight = t.right;
            Vector3 camUp = t.up;
            Vector3 camPos = t.position;

            Vector3 nearCenter = camPos + camForward * near;
            Vector3 farCenter = camPos + camForward * far;

            Vector3 nearTopLeft     = nearCenter + (camUp * nearHeight * 0.5f) - (camRight * nearWidth * 0.5f);
            Vector3 nearTopRight    = nearCenter + (camUp * nearHeight * 0.5f) + (camRight * nearWidth * 0.5f);
            Vector3 nearBottomLeft  = nearCenter - (camUp * nearHeight * 0.5f) - (camRight * nearWidth * 0.5f);
            Vector3 nearBottomRight = nearCenter - (camUp * nearHeight * 0.5f) + (camRight * nearWidth * 0.5f);

            Vector3 farTopLeft      = farCenter + (camUp * farHeight * 0.5f) - (camRight * farWidth * 0.5f);
            Vector3 farTopRight     = farCenter + (camUp * farHeight * 0.5f) + (camRight * farWidth * 0.5f);
            Vector3 farBottomLeft   = farCenter - (camUp * farHeight * 0.5f) - (camRight * farWidth * 0.5f);
            Vector3 farBottomRight  = farCenter - (camUp * farHeight * 0.5f) + (camRight * farWidth * 0.5f);

            // Top
            Vector3 topCenter = (nearTopLeft + nearTopRight + farTopLeft + farTopRight) * 0.25f;
            Vector3 topNormal = Vector3.Cross(farTopRight - nearTopRight, nearTopLeft - nearTopRight).normalized;
            SetPlaneData(0, topCenter + topNormal * marginDistance, topNormal);

            // Bottom
            Vector3 bottomCenter = (nearBottomLeft + nearBottomRight + farBottomLeft + farBottomRight) * 0.25f;
            Vector3 bottomNormal = Vector3.Cross(nearBottomLeft - nearBottomRight, farBottomRight - nearBottomRight).normalized;
            SetPlaneData(1, bottomCenter + bottomNormal * marginDistance, bottomNormal);

            // Left
            Vector3 leftCenter = (nearTopLeft + nearBottomLeft + farTopLeft + farBottomLeft) * 0.25f;
            Vector3 leftNormal = Vector3.Cross(farTopLeft - nearTopLeft, nearBottomLeft - nearTopLeft).normalized;
            SetPlaneData(2, leftCenter + leftNormal * marginDistance, leftNormal);

            // Right
            Vector3 rightCenter = (nearTopRight + nearBottomRight + farTopRight + farBottomRight) * 0.25f;
            Vector3 rightNormal = Vector3.Cross(nearBottomRight - nearTopRight, farTopRight - nearTopRight).normalized;
            SetPlaneData(3, rightCenter + rightNormal * marginDistance, rightNormal);

            // Custom Near/Far (these are not tied to camera clip planes)
            Vector3 customNearCenter = camPos + camForward * nearPlaneDistance;
            Vector3 nearNormal = camForward;      // points into the frustum
            SetPlaneData(4, customNearCenter, nearNormal);

            Vector3 customFarCenter = camPos + camForward * farPlaneDistance;
            Vector3 farNormal = -camForward;      // points back toward the camera (into the frustum)
            SetPlaneData(5, customFarCenter, farNormal);
        }

        private void SetPlaneData(int index, Vector3 position, Vector3 normal)
        {
            EnsurePlaneData();
            planeData[index].position = position;
            planeData[index].normal = normal.normalized;
        }

        private void UpdateVFXGraphs()
        {
            if (vfxGraphs == null) return;

            for (int i = 0; i < vfxGraphs.Length; i++)
            {
                var vfx = vfxGraphs[i];
                if (!vfx) continue;

                for (int p = 0; p < 6; p++)
                {
                    var pd = planeData[p];
                    if (pd == null) continue;

                    vfx.SetVector3($"{PlaneNames[p]}PlanePosition", pd.position);
                    vfx.SetVector3($"{PlaneNames[p]}PlaneNormal", pd.normal);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (drawOnlyWhenSelected) return;
            DrawGizmosInternal();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawOnlyWhenSelected) return;
            DrawGizmosInternal();
        }

        private void DrawGizmosInternal()
        {
            if (!showDebugVisuals || !targetCamera)
                return;

            EnsurePlaneData();
            RecomputeAndPush(); // ensures gizmos show correct data in edit mode even if Update isn't ticking

            // Frustum edges (camera near/far clip planes)
            DrawCameraFrustumWire(targetCamera);

            // Planes + normals
            // Colors mirror the old debug objects (Top/Bottom/Left/Right/Near/Far).
            Color[] colors =
            {
                new Color(1, 0, 0, 0.15f),   // Top
                new Color(0, 1, 0, 0.15f),   // Bottom
                new Color(0, 0, 1, 0.15f),   // Left
                new Color(1, 1, 0, 0.15f),   // Right
                new Color(1, 0, 1, 0.15f),   // Near
                new Color(0, 1, 1, 0.15f)    // Far
            };

            for (int i = 0; i < 6; i++)
            {
                var pd = planeData[i];
                if (pd == null) continue;

                DrawPlaneGizmo(pd.position, pd.normal, colors[i]);
            }
        }

        private void DrawPlaneGizmo(Vector3 position, Vector3 normal, Color color)
        {
            // Plane visual as a filled quad (DrawCube with near-zero thickness) + outline.
            Quaternion rot = (normal.sqrMagnitude > 1e-6f)
                ? Quaternion.LookRotation(normal)
                : Quaternion.identity;

            float size = debugPlaneHalfSize * 2f;
            Vector3 cubeSize = new Vector3(size, size, 0.001f);

            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(position, rot, Vector3.one);

            Gizmos.color = color;
            Gizmos.DrawCube(Vector3.zero, cubeSize);

            Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, cubeSize);

            Gizmos.matrix = old;

            // Normal line
            Gizmos.color = Color.white;
            Gizmos.DrawLine(position, position + normal.normalized * debugNormalLength);
        }

        private void DrawCameraFrustumWire(Camera cam)
        {
            Gizmos.color = Color.white;

            float near = cam.nearClipPlane;
            float far = cam.farClipPlane;
            float aspect = cam.aspect;

            float fovHalfRad = cam.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float tanFov = Mathf.Tan(fovHalfRad);

            float nearHeight = 2f * tanFov * near;
            float nearWidth = nearHeight * aspect;

            float farHeight = 2f * tanFov * far;
            float farWidth = farHeight * aspect;

            Transform t = cam.transform;
            Vector3 forward = t.forward;
            Vector3 right = t.right;
            Vector3 up = t.up;
            Vector3 pos = t.position;

            Vector3 nearCenter = pos + forward * near;
            Vector3 farCenter = pos + forward * far;

            Vector3 ntl = nearCenter + (up * nearHeight * 0.5f) - (right * nearWidth * 0.5f);
            Vector3 ntr = nearCenter + (up * nearHeight * 0.5f) + (right * nearWidth * 0.5f);
            Vector3 nbl = nearCenter - (up * nearHeight * 0.5f) - (right * nearWidth * 0.5f);
            Vector3 nbr = nearCenter - (up * nearHeight * 0.5f) + (right * nearWidth * 0.5f);

            Vector3 ftl = farCenter + (up * farHeight * 0.5f) - (right * farWidth * 0.5f);
            Vector3 ftr = farCenter + (up * farHeight * 0.5f) + (right * farWidth * 0.5f);
            Vector3 fbl = farCenter - (up * farHeight * 0.5f) - (right * farWidth * 0.5f);
            Vector3 fbr = farCenter - (up * farHeight * 0.5f) + (right * farWidth * 0.5f);

            // Near
            Gizmos.DrawLine(ntl, ntr);
            Gizmos.DrawLine(ntr, nbr);
            Gizmos.DrawLine(nbr, nbl);
            Gizmos.DrawLine(nbl, ntl);

            // Far
            Gizmos.DrawLine(ftl, ftr);
            Gizmos.DrawLine(ftr, fbr);
            Gizmos.DrawLine(fbr, fbl);
            Gizmos.DrawLine(fbl, ftl);

            // Sides
            Gizmos.DrawLine(ntl, ftl);
            Gizmos.DrawLine(ntr, ftr);
            Gizmos.DrawLine(nbl, fbl);
            Gizmos.DrawLine(nbr, fbr);
        }

    #if UNITY_EDITOR
        private void HookEditorUpdate()
        {
            if (_editorHooked) return;
            _editorHooked = true;
            EditorApplication.update += EditorTick;
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private void UnhookEditorUpdate()
        {
            if (!_editorHooked) return;
            _editorHooked = false;
            EditorApplication.update -= EditorTick;
            SceneView.duringSceneGui -= DuringSceneGui;
        }

        private void EditorTick()
        {
            // Keep VFX parameters live in edit mode without relying on Update().
            if (Application.isPlaying) return;

            // Avoid burning CPU when the object is inactive.
            if (!isActiveAndEnabled) return;

            RecomputeAndPush();
        }

        private void DuringSceneGui(SceneView _)
        {
            // Force Scene view repaint so gizmos reliably show updates when camera moves.
            if (!showDebugVisuals) return;
            if (Application.isPlaying) return;

            // Unity throttles repaints; this keeps it responsive while editing.
            SceneView.RepaintAll();
        }
    #endif
    }
}
