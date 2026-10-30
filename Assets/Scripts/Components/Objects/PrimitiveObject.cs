#if UNITY_EDITOR
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    [ExecuteInEditMode]
    public class PrimitiveObject : ObjectBase
    {
        [Header("Primitive Settings")]
        [Tooltip("The color of the primitive.")]
        public Color Color;

        [HideInInspector]
        [Tooltip("The type of primitive mesh to use.")]
        public PrimitiveType PrimitiveType;

        [Tooltip("Flags that control visibility and collision.")]
        public PrimitiveFlags PrimitiveFlags = PrimitiveFlags.Visible | PrimitiveFlags.Collidable;

        public override ObjectType ObjectType => ObjectType.Primitive;

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        private Material _sharedRegular;
        private Material _sharedTransparent;

        private void Update()
        {
            if (_meshRenderer == null && !TryGetComponent(out _meshRenderer))
                return;

            if (_sharedRegular == null)
                _sharedRegular = new Material((Material)Resources.Load("Materials/Regular"));

            if (_sharedTransparent == null)
                _sharedTransparent = new Material((Material)Resources.Load("Materials/Transparent"));

            if (_sharedRegular == null || _sharedTransparent == null)
                return;

            if (EditorUtility.IsPersistent(gameObject))
                return;

            _sharedRegular.color = Color;
            _sharedTransparent.color = Color;

            if (GUIUtility.hotControl == 0)
                _meshRenderer.sharedMaterial = Color.a >= 1f ? _sharedRegular : _sharedTransparent;

            _meshRenderer.enabled = PrimitiveFlags.HasFlag(PrimitiveFlags.Visible);
        }

        private void Start()
        {
            TryGetComponent(out _meshFilter);
            TryGetComponent(out _meshRenderer);

            _sharedRegular = new Material((Material)Resources.Load("Materials/Regular"));
        }

        public override void Compile(Transform root)
        {
            base.Compile(root);

            Properties = new()
            {
                ["Color"] = Color,
                ["PrimitiveType"] = PrimitiveType,
                ["PrimitiveFlags"] = (PrimitiveFlags & (PrimitiveFlags.Collidable | PrimitiveFlags.Visible)).ToString()
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            Color = Properties.TryGetValue("Color", out object color) ? YamlHelpers.ParseColor(color) : default;
            PrimitiveType = Properties.TryGetValue("PrimitiveType", out object primitiveType) ? YamlHelpers.ParseEnum<PrimitiveType>(primitiveType) : default;
            PrimitiveFlags = Properties.TryGetValue("PrimitiveFlags", out object primitiveFlags) ? YamlHelpers.ParseEnum<PrimitiveFlags>(primitiveFlags) : default;
        }

        private void OnDrawGizmos()
        {
            if (PrimitiveFlags.HasFlag(PrimitiveFlags.Visible))
                return;

            if (_meshFilter == null)
                TryGetComponent(out _meshFilter);

            if (_meshFilter == null || _meshFilter.sharedMesh == null)
                return;

            Gizmos.color = Color;
            Gizmos.DrawWireMesh(_meshFilter.sharedMesh, 0, transform.position, transform.rotation, transform.localScale);
        }
    }
}
#endif