using System;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEngine;
using CameraType = Assets.Scripts.Enums.CameraType;

namespace Assets.Scripts.Components.Objects
{
    public class CameraObject : ObjectBase
    {
        [HideInInspector]
        public CameraType CameraType;

        [Tooltip("The name displayed on the camera feed in the SCP-079 interface.")]
        public string Label;

        [Tooltip("The room this camera is associated with. Used by SCP-079 to locate and switch to this camera.")]
        public RoomName Room;

        [Tooltip("The minimum and maximum vertical angle the camera can look. X is the lower bound, Y is the upper bound.")]
        public Vector2 VerticalConstraint;

        [Tooltip("The minimum and maximum horizontal angle the camera can pan. X is the lower bound, Y is the upper bound.")]
        public Vector2 HorizontalConstraint;

        [Tooltip("The minimum and maximum zoom level of the camera. X is the minimum zoom, Y is the maximum zoom.")]
        public Vector2 ZoomConstraint;

        public override ObjectType ObjectType => ObjectType.Camera;

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["CameraType"] = CameraType,
                ["Label"] = Label,
                ["Room"] = Room,
                ["VerticalConstraint"] = VerticalConstraint,
                ["HorizontalConstraint"] = HorizontalConstraint,
                ["ZoomConstraint"] = ZoomConstraint,
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            CameraType = Properties.TryGetValue("CameraType", out object cameraType) ? YamlHelpers.ParseEnum<CameraType>(cameraType) : default;
            Label = Properties.TryGetValue("Label", out object label) ? Convert.ToString(label) : string.Empty;
            Room = Properties.TryGetValue("Room", out object room) ? YamlHelpers.ParseEnum<RoomName>(room) : default;
            VerticalConstraint = Properties.TryGetValue("VerticalConstraint", out object verticalConstraint) ? YamlHelpers.ParseVector2(verticalConstraint) : default;
            HorizontalConstraint = Properties.TryGetValue("HorizontalConstraint", out object horizontalConstraint) ? YamlHelpers.ParseVector2(horizontalConstraint) : default;
            ZoomConstraint = Properties.TryGetValue("ZoomConstraint", out object zoomConstraint) ? YamlHelpers.ParseVector2(zoomConstraint) : default;
        }
    }
}