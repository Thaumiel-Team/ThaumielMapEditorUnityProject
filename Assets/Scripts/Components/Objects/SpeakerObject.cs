using System;
using Assets.Scripts.Enums;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class SpeakerObject : ObjectBase
    {
        [Tooltip("The volume of the speaker, expressed as a percentage between 0 and 100.")]
        public float Volume;

        [Tooltip("Whether the speaker uses spatial audio, affecting volume and panning based on listener position.")]
        public bool IsSpatial;

        [Tooltip("The minimum distance at which the speaker begins to attenuate. Within this distance, audio plays at full volume.")]
        public float MinDistance;

        [Tooltip("The maximum distance at which the speaker can be heard. Beyond this distance, audio is inaudible.")]
        public float MaxDistance;

        [Tooltip("Whether the speaker loops its audio.")]
        public bool Loop;

        [Tooltip("The absolute or local file path of the audio file this speaker will play.")]
        public string Path;

        public override ObjectType ObjectType => ObjectType.Speaker;

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["Volume"] = Volume,
                ["IsSpatial"] = IsSpatial,
                ["MinDistance"] = MinDistance,
                ["MaxDistance"] = MaxDistance,
                ["Loop"] = Loop,
                ["Path"] = Path,
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);
            Volume = Properties.TryGetValue("Volume", out object volume) ? Convert.ToSingle(volume) : 25;
            IsSpatial = Properties.TryGetValue("IsSpatial", out object spatial) && Convert.ToBoolean(spatial);
            MinDistance = Properties.TryGetValue("MinDistance", out object min) ? Convert.ToSingle(min) : 25;
            MaxDistance = Properties.TryGetValue("MaxDistance", out object max) ? Convert.ToSingle(max) : 25;
            Loop = Properties.TryGetValue("Loop", out object loop) && Convert.ToBoolean(loop);
            Path = Properties.TryGetValue("Path", out object path) ? Convert.ToString(path) : string.Empty;
        }
    }
}