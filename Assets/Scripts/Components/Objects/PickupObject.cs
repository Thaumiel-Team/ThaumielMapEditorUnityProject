using System;
using System.Collections.Generic;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class PickupObject : ObjectBase
    {
        private const string ItemPreviewName = "ItemPreview";
        private const string ItemReferenceFolder = "Assets/Parts/References/Items";

        private static readonly Dictionary<ItemType, string> ItemPrefabNames = new()
        {
            [ItemType.KeycardMTFPrivate] = "KeycardMTF",
            [ItemType.KeycardFacilityManager] = "KeycardManagement",
            [ItemType.KeycardChaosInsurgency] = "KeycardChaos",
            [ItemType.KeycardO5] = "KeycardO5",
            [ItemType.Radio] = "RadioPickup",
            [ItemType.GunCOM15] = "Com15Worldmodel",
            [ItemType.GunCOM18] = "Com18Worldmodel",
            [ItemType.GunCom45] = "Com45Worldmodel",
            [ItemType.GunE11SR] = "E11SRWorldmodel",
            [ItemType.GunCrossvec] = "CrossvecWorldmodel",
            [ItemType.GunFSP9] = "Fsp9Worldmodel",
            [ItemType.GunLogicer] = "LogicerWorldmodel",
            [ItemType.GunRevolver] = "RevolverWorldmodel",
            [ItemType.GunAK] = "AkWorldmodel",
            [ItemType.GunShotgun] = "ShotgunWorldmodel",
            [ItemType.GunFRMG0] = "FRMG0Worldmodel",
            [ItemType.GunA7] = "A7Worldmodel",
            [ItemType.GunSCP127] = "SCP127Worldmodel",
            [ItemType.ParticleDisruptor] = "ParticleDisruptorWorldmodel",
            [ItemType.SCP500] = "SCP500Pickup",
            [ItemType.SCP207] = "SCP207Pickup",
            [ItemType.AntiSCP207] = "AntiSCP207Pickup",
            [ItemType.SCP1853] = "SCP1853Pickup",
            [ItemType.SCP1576] = "SCP1576Pickup",
            [ItemType.SCP244a] = "SCP244APickup Variant",
            [ItemType.SCP244b] = "SCP244BPickup Variant",
            [ItemType.SCP2176] = "Scp2176Viewmodel",
            [ItemType.SCP1509] = "Scp1509Pickup",
            [ItemType.Ammo12gauge] = "Ammo12gaPickup",
            [ItemType.Ammo556x45] = "Ammo556mmPickup",
            [ItemType.Ammo44cal] = "Ammo44calPickup",
            [ItemType.Ammo762x39] = "Ammo762mmPickup",
            [ItemType.Ammo9x19] = "Ammo9mmPickup",
            [ItemType.GrenadeHE] = "HegPickup",
            [ItemType.GrenadeFlash] = "FlashbangPickup",
            [ItemType.Adrenaline] = "AdrenalinePrefab",
            [ItemType.Coin] = "CoinPickup",
            [ItemType.Coal] = "CoalPickup",
            [ItemType.ArmorLight] = "Light Armor Pickup",
            [ItemType.ArmorCombat] = "Combat Armor Pickup",
            [ItemType.ArmorHeavy] = "Heavy Armor Pickup",
            [ItemType.Jailbird] = "JailbirdPickup",
            [ItemType.Lantern] = "LanternPickup",
            [ItemType.Snowball] = "SnowballProjectile",
        };

        [Header("Pickup Settings")]
        [Tooltip("The item that will be spawned.")]
        public ItemType ItemToSpawn;

        [Tooltip("The chance for this pickup to spawn, from 0 to 100.")]
        public float SpawnPercentage;

        [Tooltip("The maximum number of items that can spawn.")]
        public uint MaxAmount;

        [Tooltip("If enabled, this pickup will spawn infinitely.")]
        public bool IsInfinite;

        public override ObjectType ObjectType => ObjectType.Pickup;

        private bool _previewRefreshScheduled;

        private void OnValidate()
        {
            if (_previewRefreshScheduled)
                return;

            _previewRefreshScheduled = true;
            EditorApplication.delayCall += () =>
            {
                _previewRefreshScheduled = false;
                if (this == null)
                    return;

                RefreshItemPreview();
            };
        }

        public void RefreshItemPreview()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name == ItemPreviewName)
                    DestroyImmediate(child.gameObject);
            }

            if (!ItemPrefabNames.TryGetValue(ItemToSpawn, out string prefabName))
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ItemReferenceFolder}/{prefabName}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"Pickup preview: could not find '{ItemReferenceFolder}/{prefabName}.prefab' for {ItemToSpawn}.");
                return;
            }

            GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            if (preview == null)
            {
                Debug.LogWarning($"Pickup preview: could not instantiate '{ItemReferenceFolder}/{prefabName}.prefab' for {ItemToSpawn}.");
                return;
            }

            preview.name = ItemPreviewName;
            preview.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            preview.hideFlags = HideFlags.NotEditable;
        }

        public override void Compile(Transform root)
        {
            base.Compile(root);

            Properties = new()
            {
                ["ItemToSpawn"] = ItemToSpawn,
                ["SpawnPercentage"] = SpawnPercentage,
                ["MaxAmount"] = MaxAmount,
                ["IsInfinite"] = IsInfinite
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            ItemToSpawn = Properties.TryGetValue("ItemToSpawn", out object itemToSpawn) ? YamlHelpers.ParseEnum<ItemType>(itemToSpawn) : default;
            SpawnPercentage = Properties.TryGetValue("SpawnPercentage", out object spawnPercentage) ? Convert.ToSingle(spawnPercentage) : default;
            MaxAmount = Properties.TryGetValue("MaxAmount", out object maxAmount) ? Convert.ToUInt32(maxAmount) : default;
            IsInfinite = Properties.TryGetValue("IsInfinite", out object isInfinite) && Convert.ToBoolean(isInfinite);

            RefreshItemPreview();
        }
    }
}