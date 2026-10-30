#if UNITY_EDITOR
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Lockers;
using Assets.Scripts.Yaml;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    [CustomEditor(typeof(LockerObject), true)]
    public class LockerObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script", "chambers");

            SerializedProperty chambers = serializedObject.FindProperty("chambers");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Chambers", "The set of chambers contained within this locker."), EditorStyles.boldLabel);

            if (chambers == null || chambers.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No chambers have been configured.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < chambers.arraySize; i++)
                    DrawChamber(chambers.GetArrayElementAtIndex(i), i);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawChamber(SerializedProperty chamber, int index)
        {
            string name = chamber.FindPropertyRelative("Name")?.stringValue;
            string label = string.IsNullOrWhiteSpace(name) ? $"Chamber {index}" : $"{name}";

            chamber.isExpanded = EditorGUILayout.Foldout(chamber.isExpanded, label, true);
            if (!chamber.isExpanded)
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(chamber.FindPropertyRelative("Permissions"));
            DrawData(chamber.FindPropertyRelative("Data"));

            EditorGUI.indentLevel--;
        }

        private static void DrawData(SerializedProperty data)
        {
            data.isExpanded = EditorGUILayout.Foldout(data.isExpanded, $"Data ({data.arraySize})", true);
            if (!data.isExpanded)
                return;

            EditorGUI.indentLevel++;

            int removeAt = -1;
            for (int i = 0; i < data.arraySize; i++)
            {
                SerializedProperty item = data.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(item.FindPropertyRelative("ItemType"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("SpawnPercent"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("AmountToSpawn"));
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("-", GUILayout.Width(22)))
                    removeAt = i;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(4);
            }

            if (removeAt >= 0)
                data.DeleteArrayElementAtIndex(removeAt);

            if (GUILayout.Button("Add Item"))
                data.arraySize++;

            EditorGUI.indentLevel--;
        }
    }

    public abstract class LockerObject : ObjectBase
    {
        [Header("Locker Settings")]
        [HideInInspector]
        public virtual LockerType LockerType { get; set; }

        [Tooltip("The chambers contained within this locker.")]
        [SerializeField, HideInInspector]
        private LockerChamber[] chambers;

        public LockerChamber[] Chambers
        {
            get
            {
                if (chambers == null || chambers.Length == 0)
                    chambers = CreateDefaultChambers();

                return chambers;
            }
            set => chambers = value;
        }

        public override ObjectType ObjectType => ObjectType.Locker;

        protected abstract LockerChamber[] CreateDefaultChambers();

        protected virtual void Reset()
        {
            chambers = CreateDefaultChambers();
            RefreshChamberIndices();
        }

        protected virtual void OnValidate()
        {
            if (chambers == null || chambers.Length == 0)
                chambers = CreateDefaultChambers();

            RefreshChamberIndices();
        }

        private void RefreshChamberIndices()
        {
            if (chambers == null)
                return;

            for (int i = 0; i < chambers.Length; i++)
            {
                if (chambers[i] == null)
                    continue;

                chambers[i].Index = (uint)i;
            }
        }

        public override void Compile(Transform root)
        {
            base.Compile(root);

            Properties = new()
            {
                ["LockerType"] = LockerType,
                ["Chambers"] = Chambers
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            LockerType = Properties.TryGetValue("LockerType", out object lockerType) ? YamlHelpers.ParseEnum<LockerType>(lockerType) : default;
            Chambers = Properties.TryGetValue("Chambers", out object chambers) && chambers is object[] chamberList ? chamberList.Select(c => YamlHelpers.ParseChamber(c)).ToArray() : new LockerChamber[9];
        }
    }
}
#endif