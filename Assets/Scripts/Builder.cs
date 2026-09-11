using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Assets.Scripts.Components;
using Assets.Scripts.Components.Tools;
using Assets.Scripts.Yaml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Assets.Scripts.Components.Objects;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts
{
#pragma warning disable CS0618
    [ExecuteInEditMode]
    public class Builder : MonoBehaviour
    {
        public static event Action<Builder, YamlSchematic> OnSchematicCompiled;

        private Config config;

        [field: SerializeField]
        public List<YamlLOD> LODSettings { get; set; } = new();

        private static readonly Color[] LodColors = new Color[]
        {
            Color.green,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            Color.red,
            Color.white
        };

        private static readonly Color CullingAreaColor = new Color(1f, 0.5f, 0f);

        [SerializeField]
        internal ServerSide server;

        private void Awake()
        {
            server = GetComponentInChildren<ServerSide>();
            if (server != null)
                return;

            GameObject obj = new("ServerSideObjects");
            obj.transform.SetParent(transform);
            server = obj.AddComponent<ServerSide>();
        }

        private void LateUpdate()
        {
            if (server == null)
                return;

            server.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }

        private void OnDrawGizmosSelected()
        {
            if (LODSettings == null)
                return;

            for (int i = 0; i < LODSettings.Count; i++)
            {
                YamlLOD lod = LODSettings[i];
                if (lod == null)
                    continue;

                Color lodColor = LodColors[i % LodColors.Length];

                Gizmos.color = lodColor;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(Vector3.zero, lod.Bounds);

                if (lod.Primitives == null)
                    continue;

                Gizmos.matrix = Matrix4x4.identity;

                foreach (PrimitiveObject primitive in GetComponentsInChildren<PrimitiveObject>())
                {
                    if (!lod.Primitives.Contains(primitive.PrimitiveType))
                        continue;

                    DrawCulledX(primitive.transform, lodColor);
                }
            }
        }

        private static void DrawCulledX(Transform t, Color color)
        {
            Gizmos.color = color;

            Vector3 center = t.position;
            float size = Mathf.Max(t.lossyScale.x, t.lossyScale.y, t.lossyScale.z) * 0.3f;

            Vector3 right = t.right * size;
            Vector3 up = t.up * size;

            Gizmos.DrawLine(center - right - up, center + right + up);
            Gizmos.DrawLine(center + right - up, center - right + up);
        }

        public void CompileData()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.Log("Saving scene before export...");
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            config = ConfigBuilder.LoadConfig();
            SetupOutput(out string directoryPath);

            (List<YamlCustomObject> objects, List<YamlCustomObject> serverObjects) = CompileAllObjects(directoryPath);

            YamlSchematic schematic = new()
            {
                RootObjectId = transform.gameObject.GetInstanceID(),
                FileName = name.Replace(' ', '_'),
                Rotation = transform.rotation.eulerAngles,
                Scale = transform.localScale,
                Objects = objects,
                ServerSideObjects = serverObjects,
                Areas = new(),
                LOD = LODSettings
            };

            File.WriteAllText(Path.Combine(directoryPath, $"{name}.yml"), YamlParser.Serializer.Serialize(schematic));
            OnSchematicCompiled?.Invoke(this, schematic);

            if (config.OpenExportAfterCompiling)
            {
                Process.Start(config.ExportPath);
            }

            if (config.CompressExport)
            {
                ZipFile.CreateFromDirectory(directoryPath, $"{directoryPath}.zip", CompressionLevel.Optimal, true);
                Directory.Delete(directoryPath, true);
            }
        }

        public (List<YamlCustomObject> Objects, List<YamlCustomObject> ServerSideObjects) CompileAllObjects(string directoryPath)
        {
            List<YamlCustomObject> objects = new();
            List<YamlCustomObject> serverObjects = new();

            ServerSide serverSide = GetComponentInChildren<ServerSide>();

            foreach (ObjectBase block in GetComponentsInChildren<ObjectBase>())
            {
                bool isServerSide = serverSide != null && block.transform.IsChildOf(serverSide.transform);
                bool hasAnimator = block.TryGetComponent(out Animator animator) && animator.runtimeAnimatorController != null;

                if (hasAnimator)
                {
                    RuntimeAnimatorController runtimeAnimatorController = animator.runtimeAnimatorController;
                    string animatorName = runtimeAnimatorController.name;
                    block.AnimatorName = animatorName;
                    AssetBundleBuild bundleBuild = new()
                    {
                        assetBundleName = animatorName,
                        assetNames = new[]
                        {
                            AssetDatabase.GetAssetPath(runtimeAnimatorController)
                        }
                    };

                    BuildPipeline.BuildAssetBundles(directoryPath, new[] { bundleBuild }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.StrictMode, EditorUserBuildSettings.activeBuildTarget);
                }

                block.ServerSide = isServerSide || hasAnimator;
                block.Compile(transform);

                YamlCustomObject customObject = new()
                {
                    ObjectId = block.ObjectId,
                    ParentId = block.ParentId,
                    Name = block.Name,
                    Position = block.Position,
                    Rotation = block.Rotation,
                    Scale = block.Scale,
                    IsStatic = block.Static,
                    MovementSmoothing = block.MovementSmoothing,
                    ObjectType = block.ObjectType,
                    AnimatorName = block.AnimatorName ?? string.Empty,
                    Values = block.Properties,
                    CullingSettings = block.CullingSettings,
                    Tools = CompileTools(block)
                };

                if (isServerSide || hasAnimator)
                {
                    serverObjects.Add(customObject);
                }
                else
                    objects.Add(customObject);
            }

            return (objects, serverObjects);
        }

        public List<YamlTool> CompileTools(ObjectBase block)
        {
            List<YamlTool> tools = new();

            foreach (ToolBase toolBase in block.GetComponents<ToolBase>())
            {
                toolBase.Compile();
                YamlTool tool = new()
                {
                    ToolName = toolBase.ToolType.ToString(),
                    Properties = toolBase.Properties
                };
                
                tools.Add(tool);
            }

            return tools;
        }

        private void SetupOutput(out string directoryPath)
        {
            string parentDirectoryPath = Directory.Exists(config.ExportPath) ? config.ExportPath : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ThaumielMapEditor");
            directoryPath = Path.Combine(parentDirectoryPath, name);

            if (!Directory.Exists(parentDirectoryPath))
                Directory.CreateDirectory(parentDirectoryPath);

            if (Directory.Exists(directoryPath))
                DeleteDirectory(directoryPath);

            if (File.Exists($"{directoryPath}.zip"))
                File.Delete($"{directoryPath}.zip");

            Directory.CreateDirectory(directoryPath);
        }

        private static void DeleteDirectory(string path)
        {
            string[] files = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
                DeleteDirectory(dir);

            Directory.Delete(path, false);
        }
    }
}