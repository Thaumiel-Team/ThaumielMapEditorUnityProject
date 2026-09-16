using System;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
#pragma warning disable CS0618
    public class LightObject : ObjectBase
    {
        private Light _light;

        [Header("Light Settings")]
        [Tooltip("The brightness of the light.")]
        public float Intensity = 1f;

        [Tooltip("The maximum distance the light reaches.")]
        public float Range = 10f;

        [Tooltip("The color emitted by the light.")]
        public Color Color = Color.white;

        [Tooltip("The type of shadows cast by the light.")]
        public LightShadows ShadowType = LightShadows.None;

        [Tooltip("How dark the shadows appear. A value of 0 means no visible shadows.")]
        public float ShadowStrength = 1f;

        [Tooltip("The type of light being emitted (Point, Spot, Directional, etc).")]
        public LightType LightType = LightType.Point;

        [Tooltip("The shape of the light emission when using area lights.")]
        public LightShape LightShape = LightShape.Cone;

        [Tooltip("The outer angle of the light cone when using a spot light.")]
        public float SpotAngle = 30f;

        [Tooltip("The inner angle of the light cone when using a spot light.")]
        public float InnerSpotAngle = 20f;

        public override ObjectType ObjectType => ObjectType.Light;

        private void Awake()
        {
            EnsureLight();
            SyncLight();
        }

        private void OnValidate()
        {
            EnsureLight();
            SyncLight();
        }

        private void EnsureLight()
        {
            if (_light == null)
                _light = gameObject.GetComponent<Light>() ?? gameObject.AddComponent<Light>();
        }

        private void SyncLight()
        {
            if (_light == null)
                return;

            _light.type = LightType;
            _light.color = Color;
            _light.intensity = Intensity;
            _light.range = Range;

            _light.shadows = ShadowType;
            _light.shadowStrength = ShadowStrength;

            _light.shape = LightShape;

            switch (LightType)
            {
                case LightType.Spot:
                    _light.spotAngle = SpotAngle;
                    _light.innerSpotAngle = InnerSpotAngle;
                    break;

                case LightType.Rectangle or LightType.Disc:
                    _light.areaSize = new Vector2(Range, Range);
                    break;
            }
        }

        public override void Compile(Transform root)
        {
            base.Compile(root);

            Properties = new()
            {
                ["LightIntensity"] = Intensity,
                ["LightRange"] = Range,
                ["LightColor"] = Color,
                ["ShadowType"] = ShadowType,
                ["ShadowStrength"] = ShadowStrength,
                ["LightType"] = LightType,
                ["LightShape"] = LightShape,
                ["SpotAngle"] = SpotAngle,
                ["InnerSpotAngle"] = InnerSpotAngle
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            Intensity = Properties.TryGetValue("LightIntensity", out object intensity) ? Convert.ToSingle(intensity) : 1f;
            Range = Properties.TryGetValue("LightRange", out object range) ? Convert.ToSingle(range) : 10f;
            Color = Properties.TryGetValue("LightColor", out object color) ? YamlHelpers.ParseColor(color) : Color.white;
            ShadowType = Properties.TryGetValue("ShadowType", out object shadowType) ? YamlHelpers.ParseEnum<LightShadows>(shadowType) : LightShadows.None;
            ShadowStrength = Properties.TryGetValue("ShadowStrength", out object shadowStrength) ? Convert.ToSingle(shadowStrength) : 1f;
            LightType = Properties.TryGetValue("LightType", out object lightType) ? YamlHelpers.ParseEnum<LightType>(lightType) : LightType.Point;
            LightShape = Properties.TryGetValue("LightShape", out object lightShape) ? YamlHelpers.ParseEnum<LightShape>(lightShape) : LightShape.Cone;
            SpotAngle = Properties.TryGetValue("SpotAngle", out object spotAngle) ? Convert.ToSingle(spotAngle) : 30f;
            InnerSpotAngle = Properties.TryGetValue("InnerSpotAngle", out object innerSpotAngle) ? Convert.ToSingle(innerSpotAngle) : 20f;

            SyncLight();
        }
    }
}