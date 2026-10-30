#if UNITY_EDITOR
using System;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(TextMeshPro))]
    public class TextToyObject : ObjectBase
    {
        [Header("Text Settings")]
        [Tooltip("The size of the text display area.")]
        public Vector2 DisplaySize;

        [Tooltip("The text that will be displayed.")]
        public string Text;

        public override ObjectType ObjectType => ObjectType.TextToy;

        private TMP_Text _textMesh;

        private void Awake()
        {
            TryGetComponent(out _textMesh);
        }

        private void OnValidate()
        {
            RefreshText();

            if (_textMesh != null)
            {
                DisplaySize = _textMesh.rectTransform.sizeDelta;
                _textMesh.margin = Vector4.zero;
            }
        }

        private void RefreshText()
        {
            if (_textMesh == null || string.IsNullOrEmpty(Text))
                return;

            _textMesh.SetText(Text);
        }

        public override void Compile(Transform root)
        {
            base.Compile(root);

            Properties = new()
            {
                ["DisplaySize"] = DisplaySize,
                ["Text"] = Text
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            DisplaySize = Properties.TryGetValue("DisplaySize", out object displaySize) ? YamlHelpers.ParseVector2(displaySize) : default;
            Text = Properties.TryGetValue("Text", out object text) ? Convert.ToString(text) : string.Empty;

            RefreshText();
        }
    }
}
#endif