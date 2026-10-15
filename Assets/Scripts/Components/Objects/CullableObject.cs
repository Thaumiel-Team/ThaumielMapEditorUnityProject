using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    [ExecuteInEditMode]
    public class CullableObject : ObjectBase
    {
        public override ObjectType ObjectType => ObjectType.CullingParent;

        public Vector3 Bounds;

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["BoundsSize"] = Bounds,
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            Bounds = Properties.TryGetValue("BoundsSize", out object bounds) ? YamlHelpers.ParseVector3(bounds) : default;
        }

        private static GUIStyle _labelStyle;
        private bool _previewApplied;

        private void LateUpdate()
        {
            if (EditorUtility.IsPersistent(gameObject))
                return;

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                if (_previewApplied)
                {
                    SetChildrenHidden(false);
                    _previewApplied = false;
                }

                return;
            }

            if (Bounds == Vector3.zero)
            {
                if (_previewApplied)
                {
                    SetChildrenHidden(false);
                    _previewApplied = false;
                }

                return;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            bool culled = !IsCameraInsideBounds(sceneView.camera, matrix);
            if (culled != _previewApplied)
            {
                SetChildrenHidden(culled);
                _previewApplied = culled;
            }
        }

        private void OnDisable()
        {
            if (_previewApplied)
            {
                ClearForcedHiding();
                _previewApplied = false;
            }
        }

        private void OnDestroy()
        {
            if (_previewApplied)
            {
                ClearForcedHiding();
                _previewApplied = false;
            }
        }

        private void SetChildrenHidden(bool hidden)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.transform == transform)
                    continue;

                if (IsUnderNestedCullable(r.transform))
                    continue;

                r.forceRenderingOff = hidden;
            }
        }

        private void ClearForcedHiding()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null)
                    continue;

                if (r.transform != transform && IsUnderNestedCullable(r.transform))
                    continue;

                r.forceRenderingOff = false;
            }
        }

        private bool IsUnderNestedCullable(Transform t)
        {
            Transform parent = t.parent;
            while (parent != null && parent != transform)
            {
                if (parent.GetComponent<CullableObject>() != null)
                    return true;

                parent = parent.parent;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            SceneView drawingView = SceneView.currentDrawingSceneView;
            bool culled = drawingView != null && drawingView.camera != null && Bounds != Vector3.zero && !IsCameraInsideBounds(drawingView.camera, matrix);

            if (IsSelfOrChildSelected())
            {
                Gizmos.color = ColorHelper.GetColor(this);
                Gizmos.matrix = matrix;
                Gizmos.DrawWireCube(Vector3.zero, Bounds);

                DrawEdgeLabel(matrix);
            }

            if (culled)
                DrawChildrenWireframe();
        }

        private void DrawChildrenWireframe()
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = ColorHelper.GetColor(this);

            foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf == null || mf.sharedMesh == null)
                    continue;

                if (mf.transform != transform && IsUnderNestedCullable(mf.transform))
                    continue;

                Transform t = mf.transform;
                Gizmos.DrawWireMesh(mf.sharedMesh, 0, t.position, t.rotation, t.lossyScale);
            }
        }

        private void DrawEdgeLabel(Matrix4x4 matrix)
        {
            SceneView sceneView = SceneView.currentDrawingSceneView;
            if (sceneView == null || sceneView.camera == null)
                return;

            Camera cam = sceneView.camera;

            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = ColorHelper.GetColor(this) }
            };

            Vector3 half = Bounds * 0.5f;
            Vector3 Corner(int i) => new(
                (i & 1) == 0 ? -half.x : half.x,
                (i & 2) == 0 ? -half.y : half.y,
                (i & 4) == 0 ? -half.z : half.z
            );

            const int steps = 16;
            float bestDist = float.MaxValue;
            Vector3 bestPoint = default;
            bool found = false;

            for (int i = 0; i < 8; i++)
            {
                foreach (int bit in new[] { 1, 2, 4 })
                {
                    if ((i & bit) != 0)
                        continue;

                    int j = i | bit;

                    for (int s = 0; s <= steps; s++)
                    {
                        Vector3 world = matrix.MultiplyPoint3x4(Vector3.Lerp(Corner(i), Corner(j), s / (float)steps));
                        Vector3 vp = cam.WorldToViewportPoint(world);

                        if (vp.z <= 0f || vp.x < 0.05f || vp.x > 0.85f || vp.y < 0.05f || vp.y > 0.95f)
                            continue;

                        float dist = ((Vector2)vp - new Vector2(0.5f, 0.5f)).sqrMagnitude;
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestPoint = world;
                            found = true;
                        }
                    }
                }
            }

            if (found)
            {
                string status = IsCameraInsideBounds(cam, matrix) ? "Loaded area" : "Culled area";
                Handles.Label(bestPoint, $"{name} {status}", _labelStyle);
            }
        }

        private bool IsCameraInsideBounds(Camera cam, Matrix4x4 matrix)
        {
            Vector3 local = matrix.inverse.MultiplyPoint3x4(cam.transform.position);
            Vector3 half = Bounds * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private bool IsSelfOrChildSelected()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.transform.IsChildOf(transform))
                    return true;
            }

            return false;
        }
    }
}