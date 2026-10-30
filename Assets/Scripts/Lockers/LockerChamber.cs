#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Assets.Scripts.Binary;
using Assets.Scripts.Enums;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Assets.Scripts.Lockers
{
    [Serializable]
    public class LockerChamber
    {
        [HideInInspector, BinaryIgnore, YamlIgnore]
        public string Name;

        [HideInInspector]
        public uint Index;
        public DoorPermissionFlags Permissions;
        public List<ChamberData> Data = new();
    }

    [Serializable]
    public class ChamberData
    {
        public ItemType ItemType;

        [Range(0f, 100f)]
        public float SpawnPercent;

        public int AmountToSpawn;
    }
}
#endif