#if UNITY_EDITOR
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
using Assets.Scripts.Binary;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts
{
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

            AssetDatabase.SaveAssets();
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
                LOD = LODSettings
            };

            if (config.ExportUsingBinary)
            {
                BinarySerializer.SerializeToFile(Path.Combine(directoryPath, $"{name}.bin"), schematic);
            }
            else
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
            Dictionary<string, string> animationBundles = BuildAnimationBundles(directoryPath);

            foreach (ObjectBase block in GetComponentsInChildren<ObjectBase>())
            {
                bool isServerSide = serverSide != null && block.transform.IsChildOf(serverSide.transform);
                bool hasAnimator = block.TryGetComponent(out Animator animator) && animator.runtimeAnimatorController != null;

                if (hasAnimator)
                {
                    RuntimeAnimatorController runtimeAnimatorController = animator.runtimeAnimatorController;
                    string controllerPath = AssetDatabase.GetAssetPath(runtimeAnimatorController);

                    block.AnimatorName = animationBundles.TryGetValue(controllerPath, out string animatorName) ? animatorName : runtimeAnimatorController.name;
                }
                else
                    block.AnimatorName = string.Empty;

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

        private Dictionary<string, string> BuildAnimationBundles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            Dictionary<string, string> animatorNamesByAssetPath = new(StringComparer.Ordinal);
            Dictionary<string, string> assetPathsByAnimatorName = new(StringComparer.OrdinalIgnoreCase);
            List<AssetBundleBuild> builds = new();

            foreach (ObjectBase block in GetComponentsInChildren<ObjectBase>())
            {
                if (!block.TryGetComponent(out Animator animator) || animator.runtimeAnimatorController == null)
                    continue;

                RuntimeAnimatorController controller = animator.runtimeAnimatorController;
                string controllerPath = AssetDatabase.GetAssetPath(controller);

                if (string.IsNullOrEmpty(controllerPath))
                {
                    Debug.LogWarning($"[Builder] Skipping animation '{controller.name}' on '{block.name}': the animator controller is not saved as an asset and cannot be exported in an asset bundle.");
                    continue;
                }

                if (animatorNamesByAssetPath.ContainsKey(controllerPath))
                    continue;

                string animatorName = GetUniqueAnimatorName(controller.name, controllerPath, assetPathsByAnimatorName);
                animatorNamesByAssetPath[controllerPath] = animatorName;

                builds.Add(new AssetBundleBuild
                {
                    assetBundleName = $"{name}-{animatorName}",
                    assetNames = new[] { controllerPath }
                });
            }

            if (builds.Count == 0)
                return animatorNamesByAssetPath;

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                directoryPath,
                builds.ToArray(),
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.StrictMode,
                EditorUserBuildSettings.activeBuildTarget);

            if (manifest == null)
            {
                Debug.LogError($"[Builder] Failed to build animation bundles for '{name}'.");
                animatorNamesByAssetPath.Clear();
                return animatorNamesByAssetPath;
            }

            Dictionary<string, string> builtNamesByBundleName = new(StringComparer.OrdinalIgnoreCase);
            foreach (string builtName in manifest.GetAllAssetBundles())
                builtNamesByBundleName[builtName] = builtName;

            foreach (string animatorName in animatorNamesByAssetPath.Values)
            {
                string bundleName = $"{name}-{animatorName}";

                if (!builtNamesByBundleName.TryGetValue(bundleName, out string builtName))
                {
                    Debug.LogError($"[Builder] Animation bundle '{bundleName}' was not produced.");
                    continue;
                }

                RestoreBundleFileCase(directoryPath, builtName, bundleName);

                if (!File.Exists(Path.Combine(directoryPath, bundleName)))
                {
                    Debug.LogWarning($"[Builder] Built animation bundle '{builtName}' but could not rename it to '{bundleName}'.");
                    continue;
                }

                Debug.Log($"[Builder] Built animation bundle '{bundleName}'.");
            }

            return animatorNamesByAssetPath;
        }

        private static string GetUniqueAnimatorName(string controllerName, string controllerPath, Dictionary<string, string> assetPathsByAnimatorName)
        {
            string baseName = string.IsNullOrWhiteSpace(controllerName) ? "Animation" : controllerName.Trim();
            string animatorName = baseName;

            for (int suffix = 2; assetPathsByAnimatorName.TryGetValue(animatorName, out string existingPath) && existingPath != controllerPath; suffix++)
                animatorName = $"{baseName}_{suffix}";

            assetPathsByAnimatorName[animatorName] = controllerPath;
            return animatorName;
        }

        private static void RestoreBundleFileCase(string directoryPath, string builtName, string expectedName)
        {
            if (string.Equals(builtName, expectedName, StringComparison.Ordinal))
                return;

            RenameFile(directoryPath, builtName, expectedName);
        }

        private static void RenameFile(string directoryPath, string builtName, string expectedName)
        {
            string builtPath = Path.Combine(directoryPath, builtName);
            if (!File.Exists(builtPath))
                return;

            string expectedPath = Path.Combine(directoryPath, expectedName);
            string tempPath = $"{builtPath}.tme-tmp";

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            File.Move(builtPath, tempPath);

            if (File.Exists(expectedPath))
                File.Delete(expectedPath);

            File.Move(tempPath, expectedPath);
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

        private string GetOutputDirectory()
        {
            string parentDirectoryPath = Directory.Exists(config.ExportPath) ? config.ExportPath : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ThaumielMapEditor");
            return Path.Combine(parentDirectoryPath, name);
        }

        private void SetupOutput(out string directoryPath)
        {
            directoryPath = GetOutputDirectory();

            string parentDirectoryPath = Path.GetDirectoryName(directoryPath);
            if (!string.IsNullOrEmpty(parentDirectoryPath) && !Directory.Exists(parentDirectoryPath))
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
#endif