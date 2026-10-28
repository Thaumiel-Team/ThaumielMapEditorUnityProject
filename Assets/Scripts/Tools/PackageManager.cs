using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Tools
{
    public class PackageManagerWindow : EditorWindow
    {
        [Serializable]
        public class ListRequestData
        {
            [JsonConverter(typeof(VersionConverter))]
            public Version UnityVersion { get; set; }
        }

        [Serializable]
        public class InstalledPackage
        {
            public string DisplayName { get; set; }
            public string PackageName { get; set; }
            public string DownloadURL { get; set; }
            public DateTime LastUpdated { get; set; }

            [JsonConverter(typeof(VersionConverter))]
            public Version Version { get; set; }
        }

        [Serializable]
        public class PackageData
        {
            public string DisplayName { get; set; }
            public string PackageName { get; set; }
            public string DownloadURL { get; set; }

            [JsonConverter(typeof(VersionConverter))]
            public Version Version { get; set; }

            public DateTime MinUnityVersion { get; set; }
            public uint DownloadCount { get; set; }
            public DateTime LastUpdated { get; set; }
            public float SizeBytes { get; set; }
        }

        [Serializable]
        public class PackageUpdateData
        {
            public string DisplayName { get; set; }
            public string DownloadURL { get; set; }

            [JsonConverter(typeof(VersionConverter))]
            public Version Version { get; set; }
        }

        [Serializable]
        private class InstalledPackageState
        {
            public string DisplayName { get; set; }
            public string Version { get; set; }
            public string InstalledAtUtc { get; set; }
            public List<string> Assets { get; set; } = new();
        }

        [Serializable]
        private class InstalledState
        {
            public Dictionary<string, InstalledPackageState> Packages { get; set; } = new();
        }

        private readonly List<PackageData> _packages = new();
        private readonly Dictionary<string, bool> _selected = new();
        private InstalledState _state = new();
        private Vector2 _scroll;
        private bool _loading;
        private bool _busy;
        private string _status;
        private string _error;
        private float _progress;

        [MenuItem("Thaumiel/Tools/Package Manager")]
        public static void Open()
        {
            PackageManagerWindow window = GetWindow<PackageManagerWindow>(false, "Package Manager");
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadState();
            RefreshAll();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawPackageList();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Packages", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_loading || _busy))
            {
                if (GUILayout.Button(_loading ? "Loading..." : "Refresh", GUILayout.Width(100f)))
                    RefreshAll();
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_error))
            {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }
            else if (_loading)
            {
                EditorGUILayout.HelpBox(_status ?? "Loading packages...", MessageType.Info);
            }
        }

        private void DrawPackageList()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            if (_packages.Count == 0)
            {
                if (!_loading && string.IsNullOrEmpty(_error))
                    EditorGUILayout.HelpBox("No packages are available.", MessageType.Info);
            }
            else
            {
                foreach (PackageData package in _packages)
                    DrawPackage(package);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPackage(PackageData package)
        {
            string key = package.PackageName;
            bool installed = IsInstalled(package);
            bool selected = IsSelected(package);
            Version installedVersion = null;
            bool updateAvailable = installed && TryGetUpdate(package, out installedVersion, out _);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();

                using (new EditorGUI.DisabledScope(_busy || _loading || string.IsNullOrEmpty(key)))
                {
                    bool next = EditorGUILayout.Toggle(selected, GUILayout.Width(18f));
                    if (next != selected && !string.IsNullOrEmpty(key))
                        _selected[key] = next;
                }

                GUIContent name = new(DisplayName(package), string.IsNullOrEmpty(key) ? null : key);
                GUILayout.Label(name, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                Color previous = GUI.contentColor;
                GUI.contentColor = updateAvailable ? new Color(1f, 0.8f, 0.35f) : installed ? new Color(0.45f, 0.9f, 0.45f) : new Color(0.65f, 0.65f, 0.65f);
                GUILayout.Label(updateAvailable ? "Update available" : installed ? "Installed" : "Not installed", EditorStyles.miniLabel);
                GUI.contentColor = previous;

                if (updateAvailable)
                {
                    using (new EditorGUI.DisabledScope(_busy || _loading))
                    {
                        GUI.backgroundColor = new Color(0.35f, 0.7f, 1f);
                        if (GUILayout.Button("Update", GUILayout.Width(76f)))
                            UpdatePackageAsync(package);

                        GUI.backgroundColor = Color.white;
                    }
                }

                EditorGUILayout.EndHorizontal();

                string details =
                    $"{FormatVersion(package.Version)}   •   " +
                    $"{FormatSize(package.SizeBytes)}   •   " +
                    $"{package.DownloadCount:N0} downloads";

                GUILayout.Label(details, EditorStyles.miniLabel);
                if (updateAvailable && installedVersion != null)
                    GUILayout.Label($"Installed {FormatVersion(installedVersion)}", EditorStyles.miniLabel);

                GUILayout.Label($"Updated {FormatDate(package.LastUpdated)}", EditorStyles.miniLabel);
            }
        }

        private void DrawFooter()
        {
            int changes = CountChanges();

            EditorGUILayout.Space(4f);

            if (_busy)
            {
                Rect bar = EditorGUILayout.GetControlRect(false, 20f);
                EditorGUI.ProgressBar(bar, _progress, $"{_status ?? "Applying changes..."}  {_progress * 100f:0}%");
                EditorGUILayout.Space(4f);
            }
            else
            {
                string summary;
                if (_loading)
                {
                    summary = "Loading...";
                }
                else if (changes == 1)
                {
                    summary = "1 change pending.";
                }
                else if (changes > 1)
                {
                    summary = $"{changes} changes pending.";
                }
                else
                    summary = _status ?? "No pending changes.";

                EditorGUILayout.LabelField(summary, EditorStyles.miniLabel);
            }

            bool canApply = !_busy && !_loading && changes > 0;
            using (new EditorGUI.DisabledScope(!canApply))
            {
                GUI.backgroundColor = canApply ? new Color(0.35f, 0.85f, 0.35f) : Color.gray;
                if (GUILayout.Button("Apply", GUILayout.Height(38f)))
                    ApplyChanges();

                GUI.backgroundColor = Color.white;
            }
        }

        private void RefreshAll() =>
            RefreshAllAsync();

        private async void RefreshAllAsync()
        {
            if (_loading || _busy)
                return;

            _loading = true;
            _error = null;
            _status = "Loading packages...";
            SafeRepaint();

            try
            {
                await FetchPackagesAsync();
                SyncSelectionWithInstalled();
                _status = $"{_packages.Count} package{(_packages.Count == 1 ? "" : "s")} available.";
            }
            catch (Exception e)
            {
                _error = e.Message;
                _status = null;
                Debug.LogError($"[Package Manager] {e}");
            }
            finally
            {
                _loading = false;
                SafeRepaint();
            }
        }

        private async Task FetchPackagesAsync()
        {
            ListRequestData payload = new()
            {
                UnityVersion = GetUnityVersion()
            };
            
            using UnityWebRequest request = UnityWebRequest.Post("https://tme-unity-packager.thaumiel-servers.workers.dev/package/list", JsonConvert.SerializeObject(payload), "application/json");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 30;

            await WaitForAsyncOperation(request.SendWebRequest());

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"Could not load packages ({request.responseCode}): {request.error}");

            _packages.Clear();
            _packages.AddRange(ParsePackages(request.downloadHandler.text).Where(package => package != null));
        }

        private void ApplyChanges() =>
            ApplyChangesAsync();

        private static Dictionary<string, DateTime> SnapshotAssets()
        {
            Dictionary<string, DateTime> map = new(StringComparer.Ordinal);
            string root = GetProjectRoot();
            string assets = Path.Combine(root, "Assets");
            if (!Directory.Exists(assets))
                return map;

            foreach (string file in Directory.EnumerateFiles(assets, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string relative = file.Substring(root.Length + 1).Replace('\\', '/');
                map[relative] = File.GetLastWriteTimeUtc(file);
            }

            return map;
        }

        private static List<string> DiffAssets(Dictionary<string, DateTime> before)
        {
            List<string> changed = new();
            foreach (KeyValuePair<string, DateTime> pair in SnapshotAssets())
            {
                if (!before.TryGetValue(pair.Key, out DateTime time) || time != pair.Value)
                    changed.Add(pair.Key);
            }

            return changed;
        }

        private async void ApplyChangesAsync()
        {
            if (_busy || _loading)
                return;

            List<(PackageData Package, bool Install)> changes = new();
            foreach (PackageData package in _packages)
            {
                if (string.IsNullOrEmpty(package.PackageName))
                    continue;

                bool installed = IsInstalled(package);
                bool selected = IsSelected(package);
                if (selected != installed)
                    changes.Add((package, selected));
            }

            if (changes.Count == 0)
                return;

            if (!EnsureDependencies(changes))
                return;

            List<PackageData> removals = changes.Where(change => !change.Install).Select(change => change.Package).ToList();
            if (removals.Count > 0)
            {
                string list = string.Join("\n", removals.Select(package => $"• {DisplayName(package)}"));
                bool confirmed = EditorUtility.DisplayDialog("Remove packages", $"Unchecking these packages will delete the added files:\n\n{list}\n\nContinue?", "Remove", "Cancel");

                if (!confirmed)
                {
                    _status = "Cancelled.";
                    SafeRepaint();
                    return;
                }
            }

            await RunChangesAsync(changes);
        }

        private async void UpdatePackageAsync(PackageData package)
        {
            if (_busy || _loading || package == null)
                return;

            await RunChangesAsync(new List<(PackageData Package, bool Install)> { (package, true) }, $"{DisplayName(package)} updated.", false);
        }

        private async Task RunChangesAsync(List<(PackageData Package, bool Install)> changes, string successStatus = "All changes applied.", bool syncSelection = true)
        {
            _busy = true;
            _error = null;
            _progress = 0f;
            _status = "Preparing changes...";
            SafeRepaint();

            List<string> errors = new();
            bool delayedImports = false;

            try
            {
                for (int index = 0; index < changes.Count; index++)
                {
                    (PackageData package, bool install) = changes[index];
                    float start = (float)index / changes.Count;
                    float end = (float)(index + 1) / changes.Count;
                    float span = end - start;

                    try
                    {
                        if (install)
                        {
                            string url = package.DownloadURL?.Trim();
                            if (string.IsNullOrEmpty(url))
                                throw new Exception("no download URL was provided.");

                            _status = $"Downloading {DisplayName(package)}...";
                            ReportProgress(start);

                            string file = await DownloadPackageAsync(package, url, fraction => ReportProgress(start + span * 0.65f * fraction));
                            _status = $"Preparing {DisplayName(package)}...";
                            ReportProgress(start + span * 0.68f);
                            string normalizedPath = GetNormalizedPackagePath(package);
                            file = await Task.Run(() => PreparePackageForImport(package, file, normalizedPath));
                            string[] assets = ReadUnityPackageAssets(file, package.PackageName, out string info, out int pathnameCount);
                            if (pathnameCount == 0)
                            {
                                TryDelete(file);
                                throw new Exception($"the download is not a valid .unitypackage ({info}).");
                            }

                            if (assets.Length == 0)
                                Debug.LogWarning($"[Package Manager] {DisplayName(package)}: no Assets/ paths found ({info}). Importing anyway, but uninstall won't be able to remove files.");

                            _status = $"Importing {DisplayName(package)}...";
                            ReportProgress(start + span * 0.7f);

                            HashSet<string> trackable = GetTrackableAssets(package, assets);
                            Dictionary<string, DateTime> before = SnapshotAssets();
                            RecordInstalled(package, trackable);
                            SaveState();

                            try
                            {
                                bool delayed = !ImportUnityPackage(file);
                                delayedImports |= delayed;

                                if (!delayed)
                                {
                                    foreach (string path in DiffAssets(before))
                                        trackable.Add(path);

                                    RecordInstalled(package, trackable);
                                    SaveState();
                                    Debug.LogError($"{DisplayName(package)}: recorded {trackable.Count} files.");

                                    OptionalPackageDefines.SetInstalled(package.PackageName, true);
                                }
                            }
                            catch (Exception)
                            {
                                _state.Packages.Remove(package.PackageName);
                                SaveState();
                                throw;
                            }
                        }
                        else
                        {
                            _status = $"Removing {DisplayName(package)}...";
                            ReportProgress(start + span * 0.1f);

                            UninstallPackage(package);
                        }
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{DisplayName(package)}: {e.Message}");
                        Debug.LogError($"[Package Manager] {e}");
                    }

                    ReportProgress(end);
                }

                if (syncSelection)
                {
                    if (delayedImports)
                    {
                        foreach (PackageData package in _packages)
                        {
                            if (!string.IsNullOrEmpty(package.PackageName))
                                _selected[package.PackageName] = _state.Packages.ContainsKey(package.PackageName);
                        }
                    }
                    else
                        SyncSelectionWithInstalled();
                }

                if (errors.Count == 0)
                {
                    _status = successStatus;
                }
                else
                {
                    _error = string.Join("\n", errors);
                    _status = "Finished with errors.";
                }
            }
            catch (Exception e)
            {
                _error = e.Message;
                _status = "Failed.";
                Debug.LogError($"[Package Manager] {e}");
            }
            finally
            {
                _busy = false;
                SafeRepaint();
            }
        }

        private bool EnsureDependencies(List<(PackageData Package, bool Install)> changes)
        {
            bool collabInstalled = IsPackageInstalled("Collab");
            bool collabSelected = _packages.Any(package => IsPackage(package, "Collab") && IsSelected(package));
            bool collabRemoval = changes.Any(change => !change.Install && IsPackage(change.Package, "Collab"));
            bool blockyInstalled = IsPackageInstalled("Blocky");
            bool blockyRemoval = changes.Any(change => !change.Install && IsPackage(change.Package, "Blocky"));
            bool blockyInstall = changes.Any(change => change.Install && IsPackage(change.Package, "Blocky"));

            bool wantsCollab = collabSelected || (collabInstalled && !collabRemoval);
            if (!wantsCollab)
                return true;

            if (blockyRemoval)
            {
                _error = "The Collab package requires Blocky. Uncheck Collab before removing Blocky.";
                _status = "Blocky is required by Collab.";
                SafeRepaint();
                return false;
            }

            if (blockyInstalled || blockyInstall)
                return true;

            PackageData blocky = _packages.FirstOrDefault(package => IsPackage(package, "Blocky"));
            if (blocky == null)
            {
                _error = "The Collab package requires Blocky, but Blocky is not available.";
                _status = "Missing package dependency.";
                SafeRepaint();
                return false;
            }

            changes.Insert(0, (blocky, true));
            _selected["Blocky"] = true;
            return true;
        }

        private bool IsPackageInstalled(string packageName) =>
            _state.Packages.Keys.Any(key => string.Equals(key, packageName, StringComparison.OrdinalIgnoreCase));

        private static bool IsPackage(PackageData package, string packageName) =>
            package != null && string.Equals(package.PackageName, packageName, StringComparison.OrdinalIgnoreCase);

        private static bool ImportUnityPackage(string path)
        {
            MethodInfo immediate = typeof(AssetDatabase).GetMethod("ImportPackageImmediately", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
            if (immediate == null)
            {
                AssetDatabase.ImportPackage(path, false);
                return false;
            }

            object result;
            try
            {
                result = immediate.Invoke(null, new object[] { path });
            }
            catch (TargetInvocationException e)
            {
                throw new Exception(e.InnerException?.Message ?? e.Message, e.InnerException);
            }

            if (result is bool success && !success)
                throw new Exception("Unity could not import the package.");

            return true;
        }

        private static async Task<string> DownloadPackageAsync(PackageData package, string url, Action<float> onProgress = null)
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new Exception("the download URL must be an http(s) link.");

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && uri.AbsolutePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                throw new Exception("the download URL points to a .zip archive, but this tool expects a .unitypackage.");

            string folder = GetCacheFolder();
            Directory.CreateDirectory(folder);

            string version = package.Version?.ToString() ?? "latest";
            string fileName = $"{SanitizeFileName(package.PackageName)}-{version}.unitypackage";
            string path = Path.Combine(folder, fileName);

            if (File.Exists(path))
            {
                try
                {
                    ValidateGzipArchive(path);
                    return path;
                }
                catch (Exception)
                {
                    TryDelete(path);
                }
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerFile(path)
                {
                    removeFileOnAbort = true
                };

                request.timeout = 600;
                request.SetRequestHeader("Accept", "application/octet-stream");
                request.SetRequestHeader("User-Agent", "ThaumielMapEditor-PackageManager");

                Action reportProgress = onProgress == null ? null : () => onProgress(GetDownloadProgress(request));
                await WaitForAsyncOperation(request.SendWebRequest(), reportProgress);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    TryDelete(path);
                    throw new Exception($"download failed ({request.responseCode}): {request.error}");
                }
            }

            ValidateGzipArchive(path);
            return path;
        }

        private static void ValidateGzipArchive(string path)
        {
            try
            {
                using FileStream stream = File.OpenRead(path);
                int first = stream.ReadByte();
                int second = stream.ReadByte();

                if (first != 0x1f || second != 0x8b)
                    throw new Exception("the download was not a gzip archive. Check that the URL is a direct download link (not a GitHub HTML page).");
            }
            catch (Exception)
            {
                TryDelete(path);
                throw;
            }
        }

        private static string GetNormalizedPackagePath(PackageData package)
        {
            string folder = GetCacheFolder();
            string version = package.Version?.ToString() ?? "latest";
            return Path.Combine(folder, $"{SanitizeFileName(package.PackageName)}-{version}-normalized.unitypackage");
        }

        private static string PreparePackageForImport(PackageData package, string sourcePath, string destination)
        {
            string folder = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            if (File.Exists(destination))
            {
                try
                {
                    ValidateGzipArchive(destination);
                    return destination;
                }
                catch (Exception)
                {
                    TryDelete(destination);
                }
            }

            bool changed = false;

            try
            {
                using FileStream source = File.OpenRead(sourcePath);
                using GZipStream input = new(source, CompressionMode.Decompress);
                using FileStream target = File.Create(destination);
                using GZipStream output = new(target, System.IO.Compression.CompressionLevel.Fastest);

                byte[] header = new byte[512];
                string longName = null;

                while (ReadExactly(input, header, header.Length))
                {
                    if (IsEmptyBlock(header))
                    {
                        output.Write(header, 0, header.Length);
                        CopyBytes(input, output, long.MaxValue);
                        break;
                    }

                    string name = longName ?? ReadNullTerminated(header, 0, 100);
                    longName = null;

                    bool ustar = Encoding.ASCII.GetString(header, 257, 5) == "ustar";
                    if (ustar)
                    {
                        string prefix = ReadNullTerminated(header, 345, 155);
                        if (!string.IsNullOrEmpty(prefix))
                            name = prefix + "/" + name;
                    }

                    long size = ReadSize(header);
                    if (size < 0)
                        throw new Exception("the package contains an invalid tar entry.");

                    char type = header[156] == 0 ? '0' : (char)header[156];
                    long padding = Padding(size);

                    if (type == 'L' || type == 'x' || type == 'g')
                    {
                        if (size > 65536 || !TryReadEntry(input, size, padding, out byte[] meta))
                            throw new Exception("the package contains an invalid long-name entry.");

                        output.Write(header, 0, header.Length);
                        output.Write(meta, 0, meta.Length);
                        WriteZeros(output, padding);

                        if (type == 'L')
                        {
                            longName = Encoding.UTF8.GetString(meta).Trim('\0', '\r', '\n', ' ');
                        }
                        else if (type == 'x')
                            longName = ReadPaxPath(meta) ?? longName;

                        continue;
                    }

                    name = name.Replace('\\', '/');
                    while (name.StartsWith("./", StringComparison.Ordinal))
                        name = name.Substring(2);

                    bool isPathname = type == '0' && (name == "pathname" || name.EndsWith("/pathname", StringComparison.Ordinal));
                    if (isPathname)
                    {
                        if (size <= 0 || size > 65536 || !TryReadEntry(input, size, padding, out byte[] data))
                            throw new Exception("the package contains an invalid pathname entry.");

                        string original = NormalizePathnameText(data);
                        string normalized = NormalizePackagePath(original, package.PackageName) ?? throw new Exception($"the package contains an unsafe asset path: '{original}'.");

                        if (string.Equals(original, normalized, StringComparison.Ordinal))
                        {
                            output.Write(header, 0, header.Length);
                            output.Write(data, 0, data.Length);
                            WriteZeros(output, padding);
                        }
                        else
                        {
                            WriteEntry(output, header, Encoding.UTF8.GetBytes(normalized));
                            changed = true;
                        }

                        continue;
                    }

                    output.Write(header, 0, header.Length);
                    if (!CopyBytes(input, output, size) || !CopyBytes(input, output, padding))
                        throw new Exception("the package ended unexpectedly.");
                }
            }
            catch (Exception)
            {
                TryDelete(destination);
                throw;
            }

            if (!changed)
            {
                TryDelete(destination);
                return sourcePath;
            }

            ValidateGzipArchive(destination);
            return destination;
        }

        private static void WriteEntry(Stream output, byte[] header, byte[] data)
        {
            byte[] copy = (byte[])header.Clone();
            WriteOctal(copy, 124, 12, data.Length);
            WriteChecksum(copy);

            output.Write(copy, 0, copy.Length);
            output.Write(data, 0, data.Length);
            WriteZeros(output, Padding(data.Length));
        }

        private static void WriteOctal(byte[] header, int offset, int length, long value)
        {
            string text = Convert.ToString(value, 8).PadLeft(length - 1, '0');
            if (text.Length > length - 1)
                text = text.Substring(text.Length - (length - 1));

            for (int index = 0; index < length - 1; index++)
                header[offset + index] = (byte)text[index];

            header[offset + length - 1] = 0;
        }

        private static void WriteChecksum(byte[] header)
        {
            for (int index = 148; index < 156; index++)
                header[index] = (byte)' ';

            int sum = 0;
            foreach (byte value in header)
                sum += value;

            string text = Convert.ToString(sum, 8).PadLeft(6, '0');
            for (int index = 0; index < 6; index++)
                header[148 + index] = (byte)text[index];

            header[154] = 0;
            header[155] = (byte)' ';
        }

        private static bool CopyBytes(Stream input, Stream output, long count)
        {
            byte[] buffer = new byte[81920];
            while (count > 0)
            {
                int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
                if (read <= 0)
                    return false;

                output.Write(buffer, 0, read);
                count -= read;
            }

            return true;
        }

        private static void WriteZeros(Stream output, long count)
        {
            byte[] buffer = new byte[512];
            while (count > 0)
            {
                int chunk = (int)Math.Min(buffer.Length, count);
                output.Write(buffer, 0, chunk);
                count -= chunk;
            }
        }

        private static string NormalizePathnameText(byte[] data)
        {
            string text = Encoding.UTF8.GetString(data).Replace("\uFEFF", string.Empty);
            int end = text.IndexOfAny(new[] { '\n', '\r', '\0' });
            if (end >= 0)
                text = text.Substring(0, end);

            return text.Trim().Replace('\\', '/');
        }

        private static string NormalizePackagePath(string path, string packageName)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim().Replace('\\', '/');
            while (path.StartsWith("./", StringComparison.Ordinal))
                path = path.Substring(2);

            path = path.TrimStart('/');
            string[] parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return null;

            foreach (string part in parts)
            {
                if (part == "." || part == ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    return null;
            }

            int assetsIndex = Array.FindIndex(parts, part => string.Equals(part, "Assets", StringComparison.OrdinalIgnoreCase));
            if (assetsIndex >= 0)
            {
                string rest = string.Join("/", parts.Skip(assetsIndex + 1));
                return string.IsNullOrEmpty(rest) ? "Assets" : "Assets/" + rest;
            }

            if (parts.Length > 1 && !string.IsNullOrEmpty(packageName) && string.Equals(parts[0], packageName, StringComparison.OrdinalIgnoreCase))
                parts = parts.Skip(1).ToArray();

            return "Assets/" + string.Join("/", parts);
        }

        private static string ReadPaxPath(byte[] meta)
        {
            foreach (string line in Encoding.UTF8.GetString(meta).Split('\n'))
            {
                int space = line.IndexOf(' ');
                if (space >= 0 && line.Substring(space + 1).StartsWith("path=", StringComparison.Ordinal))
                    return line.Substring(space + 6).Trim('\0', '\r');
            }

            return null;
        }

        private static string[] ReadUnityPackageAssets(string path, string packageName, out string info, out int pathnameCount)
        {
            List<string> assets = new();
            int entries = 0, outside = 0;
            string firstName = null, firstOutside = null, longName = null;
            pathnameCount = 0;

            using FileStream file = File.OpenRead(path);
            using GZipStream gzip = new(file, CompressionMode.Decompress);

            byte[] header = new byte[512];
            while (ReadExactly(gzip, header, header.Length))
            {
                if (IsEmptyBlock(header))
                    break;

                string name = longName ?? ReadNullTerminated(header, 0, 100);
                bool ustar = Encoding.ASCII.GetString(header, 257, 5) == "ustar";
                if (longName == null && ustar)
                {
                    string prefix = ReadNullTerminated(header, 345, 155);
                    if (!string.IsNullOrEmpty(prefix))
                        name = prefix + "/" + name;
                }

                longName = null;

                long size = ReadSize(header);
                if (size < 0)
                    break;

                char type = header[156] == 0 ? '0' : (char)header[156];
                long padding = Padding(size);

                if (type == 'L' || type == 'x' || type == 'g')
                {
                    if (size > 65536 || !TryReadEntry(gzip, size, padding, out byte[] meta))
                        break;

                    string text = Encoding.UTF8.GetString(meta);
                    if (type == 'L')
                    {
                        longName = text.Trim('\0', '\r', '\n', ' ');
                    }
                    else if (type == 'x')
                    {
                        foreach (string line in text.Split('\n'))
                        {
                            int space = line.IndexOf(' ');
                            if (space >= 0 && line.Substring(space + 1).StartsWith("path=", StringComparison.Ordinal))
                                longName = line.Substring(space + 6).Trim('\0', '\r');
                        }
                    }

                    continue;
                }

                name = name.Replace('\\', '/');
                while (name.StartsWith("./", StringComparison.Ordinal))
                    name = name.Substring(2);

                entries++;
                firstName ??= name;

                bool isPathname = type == '0' && (name == "pathname" || name.EndsWith("/pathname", StringComparison.Ordinal));
                if (isPathname && size > 0 && size <= 4096)
                {
                    if (!TryReadEntry(gzip, size, padding, out byte[] data))
                        break;

                    pathnameCount++;

                    string assetPath = NormalizePackagePath(NormalizePathnameText(data), packageName);

                    if (IsSafeAssetPath(assetPath))
                    {
                        if (!assets.Contains(assetPath))
                            assets.Add(assetPath);
                    }
                    else
                    {
                        outside++;
                        firstOutside ??= assetPath;
                    }
                }
                else if (!SkipBytes(gzip, size + padding))
                {
                    break;
                }
            }

            info = $"{entries} tar entries, {pathnameCount} pathname entries, {outside} outside Assets/ (e.g. '{firstOutside ?? "-"}'), first entry '{firstName ?? "-"}'";
            return assets.ToArray();
        }

        private static bool TryReadEntry(Stream stream, long size, long padding, out byte[] data)
        {
            data = new byte[size];
            return ReadExactly(stream, data, data.Length) && SkipBytes(stream, padding);
        }

        private static long ReadSize(byte[] header)
        {
            if ((header[124] & 0x80) != 0)
            {
                long value = header[124] & 0x7f;
                for (int i = 125; i < 136; i++)
                    value = (value << 8) | header[i];

                return value;
            }

            return ReadOctal(header, 124, 12);
        }

        private HashSet<string> GetTrackableAssets(PackageData package, string[] assets)
        {
            HashSet<string> tracked = new(assets, StringComparer.Ordinal);

            if (_state.Packages.TryGetValue(package.PackageName, out InstalledPackageState previous) && previous.Assets != null)
            {
                foreach (string asset in previous.Assets)
                    tracked.Add(asset);
            }

            tracked.ExceptWith(GetAssetsOwnedByOthers(package.PackageName));
            return tracked;
        }

        private HashSet<string> GetAssetsOwnedByOthers(string packageName)
        {
            HashSet<string> owned = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, InstalledPackageState> pair in _state.Packages)
            {
                if (string.Equals(pair.Key, packageName, StringComparison.OrdinalIgnoreCase) || pair.Value.Assets == null)
                    continue;

                foreach (string asset in pair.Value.Assets)
                    owned.Add(asset);
            }

            return owned;
        }

        private void RecordInstalled(PackageData package, HashSet<string> assets)
        {
            _state.Packages[package.PackageName] = new InstalledPackageState
            {
                DisplayName = DisplayName(package),
                Version = package.Version?.ToString(),
                InstalledAtUtc = DateTime.UtcNow.ToString("o"),
                Assets = assets.OrderBy(path => path).ToList()
            };
        }

        private void UninstallPackage(PackageData package)
        {
            if (!_state.Packages.TryGetValue(package.PackageName, out InstalledPackageState entry))
            {
                OptionalPackageDefines.SetInstalled(package.PackageName, false);
                return;
            }

            List<string> tracked = entry.Assets != null ? new List<string>(entry.Assets) : new List<string>();

            if (tracked.Count == 0)
            {
                string cached = FindCachedPackage(package, entry);
                if (cached != null)
                {
                    tracked = ReadUnityPackageAssets(cached, package.PackageName, out _, out _).ToList();
                    Debug.Log($"[Package Manager] Recovered {tracked.Count} file paths for {DisplayName(package)} from cache.");
                }
                else
                    Debug.LogWarning($"[Package Manager] No tracked files and no cached package for {DisplayName(package)}. Reinstall it once so its files are recorded.");
            }

            HashSet<string> shared = GetAssetsOwnedByOthers(package.PackageName);
            List<string> paths = tracked.Where(IsSafeAssetPath).OrderByDescending(path => path.Length).ToList();

            int skippedShared = paths.Count(shared.Contains);
            if (skippedShared > 0)
                Debug.LogWarning($"[Package Manager] Keeping {skippedShared} file(s) that another installed package also lists (e.g. {paths.First(shared.Contains)}).");

            paths = paths.Where(path => !shared.Contains(path)).ToList();

            int deleted = 0, failed = 0;
            foreach (string assetPath in paths)
            {
                string fullPath = ToAbsolutePath(assetPath);
                if (!File.Exists(fullPath))
                    continue;

                bool ok = AssetDatabase.DeleteAsset(assetPath);
                if (!ok && File.Exists(fullPath))
                {
                    TryDelete(fullPath);
                    TryDelete(fullPath + ".meta");
                    ok = !File.Exists(fullPath);
                }
 
                if (ok)
                {
                    deleted++;
                }
                else
                {
                    failed++;
                    Debug.LogWarning($"[Package Manager] Could not delete {assetPath}");
                }
            }

            Debug.Log($"[Package Manager] Uninstalled {DisplayName(package)}: {deleted} deleted, {failed} failed, {paths.Count} considered.");

            foreach (string directory in paths.Select(p => Path.GetDirectoryName(p)?.Replace('\\', '/')).Where(d => !string.IsNullOrEmpty(d) && d != "Assets").Distinct().OrderByDescending(d => d.Length))
            {
                string fullPath = ToAbsolutePath(directory);
                if (Directory.Exists(fullPath) && IsEffectivelyEmpty(fullPath))
                    AssetDatabase.DeleteAsset(directory);
            }

            _state.Packages.Remove(package.PackageName);
            SaveState();

            AssetDatabase.Refresh();

            OptionalPackageDefines.SetInstalled(package.PackageName, false);
        }

        private static string FindCachedPackage(PackageData package, InstalledPackageState entry)
        {
            string folder = GetCacheFolder();
            if (!Directory.Exists(folder))
                return null;

            string exact = Path.Combine(folder, $"{SanitizeFileName(package.PackageName)}-{entry.Version ?? "latest"}.unitypackage");
            if (File.Exists(exact))
                return exact;

            return Directory.GetFiles(folder, $"{SanitizeFileName(package.PackageName)}-*.unitypackage").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        }

        private static bool IsEffectivelyEmpty(string directory) =>
            !Directory.EnumerateFileSystemEntries(directory).Any(entry => !entry.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));

        private void SyncSelectionWithInstalled()
        {
            bool stateChanged = false;

            foreach (string packageName in _state.Packages.Keys.ToList())
            {
                InstalledPackageState entry = _state.Packages[packageName];
                if (entry.Assets == null || entry.Assets.Count == 0)
                    continue;

                if (!entry.Assets.Any(TrackedAssetExists))
                {
                    _state.Packages.Remove(packageName);
                    stateChanged = true;
                }
            }

            if (stateChanged)
                SaveState();

            _selected.Clear();
            foreach (PackageData package in _packages)
            {
                if (string.IsNullOrEmpty(package.PackageName))
                    continue;

                _selected[package.PackageName] = IsInstalled(package);
            }
        }

        private int CountChanges()
        {
            int count = 0;
            foreach (PackageData package in _packages)
            {
                if (string.IsNullOrEmpty(package.PackageName))
                    continue;

                if (IsSelected(package) != IsInstalled(package))
                    count++;
            }

            return count;
        }

        private bool IsInstalled(PackageData package)
        {
            if (string.IsNullOrEmpty(package.PackageName))
                return false;

            if (!_state.Packages.TryGetValue(package.PackageName, out InstalledPackageState entry))
                return false;

            if (entry.Assets == null || entry.Assets.Count == 0)
                return true;

            return entry.Assets.Any(TrackedAssetExists);
        }

        private bool IsSelected(PackageData package) =>
            !string.IsNullOrEmpty(package.PackageName) && _selected.TryGetValue(package.PackageName, out bool selected) && selected;

        private bool TryGetUpdate(PackageData package, out Version installedVersion, out Version availableVersion)
        {
            installedVersion = null;
            availableVersion = package?.Version;

            if (package == null || availableVersion == null || string.IsNullOrEmpty(package.PackageName))
                return false;

            if (!_state.Packages.TryGetValue(package.PackageName, out InstalledPackageState entry))
                return false;

            if (entry.Assets == null || entry.Assets.Count == 0)
                return true;

            if (!Version.TryParse(entry.Version, out Version parsed))
                return true;

            installedVersion = parsed;
            return parsed.CompareTo(availableVersion) < 0;
        }

        private void LoadState()
        {
            try
            {
                if (!File.Exists(StatePath))
                {
                    _state = new InstalledState();
                    return;
                }

                string json = File.ReadAllText(StatePath);
                _state = JsonConvert.DeserializeObject<InstalledState>(json) ?? new InstalledState();
                _state.Packages ??= new Dictionary<string, InstalledPackageState>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Package Manager] could not read install state: {e.Message}");
                _state = new InstalledState();
            }
        }

        private void SaveState()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StatePath) ?? GetProjectRoot());
                File.WriteAllText(StatePath, JsonConvert.SerializeObject(_state, Formatting.Indented));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Package Manager] could not save install state: {e.Message}");
            }
        }

        private static bool AssetExists(string assetPath)
        {
            if (!IsSafeAssetPath(assetPath))
                return false;

            string fullPath = ToAbsolutePath(assetPath);
            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }

        private static bool TrackedAssetExists(string assetPath)
        {
            if (!IsSafeAssetPath(assetPath))
                return false;

            return File.Exists(ToAbsolutePath(assetPath));
        }

        private static bool IsSafeAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                return false;

            if (assetPath.Length <= "Assets/".Length)
                return false;

            return !assetPath.Contains("..");
        }

        private static string ToAbsolutePath(string assetPath) =>
            Path.Combine(GetProjectRoot(), assetPath.Replace('/', Path.DirectorySeparatorChar));

        private static bool ReadExactly(Stream stream, byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                    return false;

                offset += read;
            }

            return true;
        }

        private static bool SkipBytes(Stream stream, long count)
        {
            byte[] buffer = new byte[8192];
            while (count > 0)
            {
                int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
                if (read <= 0)
                    return false;

                count -= read;
            }

            return true;
        }

        private static int Padding(long size) =>
            (int)((512 - (size % 512)) % 512);

        private static bool IsEmptyBlock(byte[] block)
        {
            foreach (byte value in block)
            {
                if (value != 0)
                    return false;
            }

            return true;
        }

        private static string ReadNullTerminated(byte[] buffer, int offset, int length)
        {
            int end = offset;
            int limit = offset + length;

            while (end < limit && buffer[end] != 0)
                end++;

            return Encoding.UTF8.GetString(buffer, offset, end - offset).TrimEnd('\0', ' ');
        }

        private static long ReadOctal(byte[] buffer, int offset, int length)
        {
            string text = Encoding.ASCII.GetString(buffer, offset, length).Trim('\0', ' ');
            if (string.IsNullOrEmpty(text))
                return 0;

            try
            {
                return Convert.ToInt64(text, 8);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception)
            {
                // Ignore cleanup failures.
            }
        }

        private static Version GetUnityVersion()
        {
            string text = Application.unityVersion;
            int length = 0;

            while (length < text.Length && (char.IsDigit(text[length]) || text[length] == '.'))
                length++;

            return length > 0 && Version.TryParse(text.Substring(0, length), out Version version) ? version : new Version(0, 0);
        }

        private static string GetProjectRoot() =>
            Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();

        private static string GetCacheFolder() =>
            Path.Combine(GetProjectRoot(), "Library", "ThaumielPackages");

        private static string StatePath =>
            Path.Combine(GetProjectRoot(), "ProjectSettings", "StateFileName");

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "package";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');

            return value;
        }

        private static string DisplayName(PackageData package) =>
            string.IsNullOrEmpty(package.DisplayName) ? package.PackageName ?? "(unnamed package)" : package.DisplayName;

        private static string FormatVersion(Version version) =>
            version == null ? "v?" : $"v{version}";

        private static string FormatSize(float bytes)
        {
            if (bytes <= 0f)
                return "unknown size";

            string[] units = { "B", "KB", "MB", "GB" };
            double value = bytes;
            int unit = 0;

            while (value >= 1024d && unit < units.Length - 1)
            {
                value /= 1024d;
                unit++;
            }

            return $"{value:0.#} {units[unit]}";
        }

        private static string FormatDate(DateTime date) =>
            date == default ? "unknown" : date.ToLocalTime().ToString("d", CultureInfo.CurrentCulture);

        private static List<PackageData> ParsePackages(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<PackageData>();

            text = text.Trim();

            try
            {
                if (text.StartsWith("[", StringComparison.Ordinal))
                    return JsonConvert.DeserializeObject<List<PackageData>>(text) ?? new List<PackageData>();

                if (!text.StartsWith("{", StringComparison.Ordinal))
                    throw new Exception("The package server did not return JSON.");

                JToken root = JToken.Parse(text);
                if (root is JObject obj)
                {
                    JToken array = obj.Properties().Select(property => property.Value).FirstOrDefault(value => value.Type == JTokenType.Array);
                    if (array != null)
                        return array.ToObject<List<PackageData>>() ?? new List<PackageData>();
                }
            }
            catch (JsonException e)
            {
                throw new Exception($"The package server returned an invalid response: {e.Message}");
            }

            throw new Exception("The package server returned an unexpected response.");
        }

        private static Task WaitForAsyncOperation(AsyncOperation operation, Action onProgress = null)
        {
            if (operation.isDone)
            {
                onProgress?.Invoke();
                return Task.CompletedTask;
            }

            TaskCompletionSource<bool> completion = new();

            void Report()
            {
                if (!operation.isDone)
                    onProgress?.Invoke();
            }

            if (onProgress != null)
                EditorApplication.update += Report;

            operation.completed += _ =>
            {
                if (onProgress != null)
                    EditorApplication.update -= Report;

                completion.TrySetResult(true);
            };

            return completion.Task;
        }

        private static float GetDownloadProgress(UnityWebRequest request)
        {
            if (ulong.TryParse(request.GetResponseHeader("Content-Length"), out ulong total) && total > 0)
                return Mathf.Clamp01((float)((double)request.downloadedBytes / total));

            return Mathf.Clamp01(request.downloadProgress);
        }

        private void ReportProgress(float value)
        {
            _progress = Mathf.Clamp01(value);
            SafeRepaint();
        }

        private void SafeRepaint()
        {
            if (this != null)
                Repaint();
        }
    }
}
