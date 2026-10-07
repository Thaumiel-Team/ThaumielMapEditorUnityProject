using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System;

public class AutoUpdateSetup
{
    private const string RepoUrl = "https://github.com/Thaumiel-Team/ThaumielMapEditorUnityProject.git";
    private const string RemoteName = "origin";
    private const string BranchName = "master";

    private const string GitWindowsUrl = "https://github.com/git-for-windows/git/releases/download/v2.44.0.windows.1/Git-2.44.0-64-bit.exe";
    private const string GitMacUrl = "https://sourceforge.net/projects/git-osx-installer/files/latest/download";
    private const string PrefKey = "AutoUpdate_Enabled";

    [MenuItem("Thaumiel/Tools/Auto Update/Check for Updates")]
    private static void CheckForUpdatesMenu()
    {
        if (!IsGitInstalled())
        {
            bool installGit = EditorUtility.DisplayDialog(
                "Git Not Found",
                "Git is required to check for updates. Would you like to install it?",
                "Install Git",
                "Cancel"
            );

            if (installGit)
            {
                _ = DownloadAndInstallGitAsync();
            }
            return;
        }

        EnsureRepoConfigured();
        CheckForUpdates();
    }

    [MenuItem("Thaumiel/Tools/Auto Update/Update to Latest")]
    private static void ForceUpdateMenu()
    {
        if (!IsGitInstalled())
        {
            EditorUtility.DisplayDialog("Git Not Found", "Git is not installed.", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Confirm Update",
            "This will pull the latest changes from GitHub and may overwrite local files.\n\nContinue?",
            "Yes, Update",
            "Cancel"
        );

        if (confirm)
        {
            EnsureRepoConfigured();
            TryRunGitPull();
        }
    }

    private static void EnsureRepoConfigured()
    {
        string rootDir = Application.dataPath + "/../";

        try
        {
            string gitDir = Path.GetFullPath(Path.Combine(rootDir, ".git"));
            if (!Directory.Exists(gitDir))
            {
                bool initRepo = EditorUtility.DisplayDialog(
                    "Not a Git Repository",
                    "This project is not a Git repository. Initialize it now?",
                    "Yes, initialize",
                    "Cancel"
                );

                if (!initRepo) return;

                GitResult initResult = RunGitCommand("init", rootDir);
                if (initResult.ExitCode != 0)
                {
                    Debug.LogError($"[AutoUpdate] git init failed: {initResult.Error}");
                    return;
                }
                Debug.Log("[AutoUpdate] Initialized git repository.");
            }

            GitResult remoteResult = RunGitCommand($"remote get-url {RemoteName}", rootDir);
            string currentUrl = remoteResult.Output.Trim();

            if (string.IsNullOrEmpty(currentUrl))
            {
                RunGitCommand($"remote add {RemoteName} {RepoUrl}", rootDir);
                Debug.Log($"[AutoUpdate] Added remote '{RemoteName}' -> {RepoUrl}");
            }
            else if (!currentUrl.Equals(RepoUrl, StringComparison.OrdinalIgnoreCase))
            {
                RunGitCommand($"remote set-url {RemoteName} {RepoUrl}", rootDir);
                Debug.Log($"[AutoUpdate] Updated remote to {RepoUrl}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AutoUpdate] Config error: {ex.Message}");
        }
    }

    private static void CheckForUpdates()
    {
        string rootDir = Application.dataPath + "/../";

        try
        {
            GitResult branchResult = RunGitCommand("rev-parse --abbrev-ref HEAD", rootDir);
            string currentBranch = branchResult.Output.Trim();

            if (!currentBranch.Equals(BranchName, StringComparison.OrdinalIgnoreCase))
            {
                bool switchBranch = EditorUtility.DisplayDialog(
                    "Wrong Branch",
                    $"Currently on '{currentBranch}'. Switch to '{BranchName}' to check for updates?",
                    "Switch",
                    "Cancel"
                );

                if (!switchBranch) return;

                GitResult switchResult = RunGitCommand($"checkout {BranchName}", rootDir);
                if (switchResult.ExitCode != 0)
                {
                    RunGitCommand($"checkout -b {BranchName} {RemoteName}/{BranchName}", rootDir);
                }
            }

            EditorUtility.DisplayProgressBar("Checking for Updates", "Fetching from remote...", 0.5f);
            GitResult fetchResult = RunGitCommand($"fetch {RemoteName}", rootDir);
            EditorUtility.ClearProgressBar();

            if (fetchResult.ExitCode != 0)
            {
                EditorUtility.DisplayDialog("Fetch Failed", fetchResult.Error, "OK");
                return;
            }

            GitResult revListResult = RunGitCommand($"rev-list HEAD..{RemoteName}/{BranchName} --count", rootDir);
            if (revListResult.ExitCode != 0)
            {
                EditorUtility.DisplayDialog("Error", "Could not compare revisions.", "OK");
                return;
            }

            if (!int.TryParse(revListResult.Output.Trim(), out int count) || count <= 0)
            {
                EditorUtility.DisplayDialog("Up to Date", "No new updates available.", "OK");
                return;
            }

            GitResult logResult = RunGitCommand($"log HEAD..{RemoteName}/{BranchName} --oneline -n 5", rootDir);
            string commits = logResult.Output;

            bool update = EditorUtility.DisplayDialog(
                $"{count} Update(s) Available",
                $"Recent changes:\n{commits}\n\nUpdate now?",
                "Update Now",
                "Later"
            );

            if (update)
                TryRunGitPull();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AutoUpdate] {ex.Message}");
        }
    }

    private static void TryRunGitPull()
    {
        string rootDir = Application.dataPath + "/../";

        GitResult statusResult = RunGitCommand("status --porcelain", rootDir);
        bool hasChanges = !string.IsNullOrWhiteSpace(statusResult.Output);

        if (hasChanges)
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "Local Changes Detected",
                "You have uncommitted changes. What would you like to do?",
                "Stash & Pull",
                "Cancel",
                "Force Pull (Discard Changes)"
            );

            if (choice == 1)
                return;

            if (choice == 0)
            {
                RunGitCommand("stash push -m \"AutoUpdate stash\"", rootDir);
            }
            else if (choice == 2)
            {
                bool confirmForce = EditorUtility.DisplayDialog(
                    "WARNING",
                    "This will DISCARD all local uncommitted changes. Are you sure?",
                    "Yes, Discard",
                    "Cancel"
                );
                if (!confirmForce) return;
                RunGitCommand("reset --hard", rootDir);
            }
        }

        EditorUtility.DisplayProgressBar("Updating", "Pulling latest changes...", 0.5f);
        GitResult pullResult = RunGitCommand($"pull {RemoteName} {BranchName}", rootDir);
        EditorUtility.ClearProgressBar();

        if (pullResult.ExitCode != 0)
        {
            EditorUtility.DisplayDialog("Update Failed", pullResult.Error, "OK");
            return;
        }

        GitResult commitResult = RunGitCommand("rev-parse --short HEAD", rootDir);
        EditorUtility.DisplayDialog(
            "Update Complete",
            $"Updated to commit: {commitResult.Output.Trim()}\n\nUnity will now refresh assets.",
            "OK"
        );

        AssetDatabase.Refresh();
    }

    private static GitResult RunGitCommand(string args, string workingDir)
    {
        Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new GitResult(output, error, process.ExitCode);
    }

    private static bool IsGitInstalled()
    {
        try
        {
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch { return false; }
    }

    private static async Task DownloadAndInstallGitAsync()
    {
        string url = Application.platform == RuntimePlatform.WindowsEditor ? GitWindowsUrl : GitMacUrl;
        string extension = Application.platform == RuntimePlatform.WindowsEditor ? ".exe" : ".pkg";
        string installerPath = "Temp/git_installer" + extension;

        Directory.CreateDirectory("Temp");
        EditorUtility.DisplayProgressBar("Installing Git", "Downloading...", 0f);

        using UnityWebRequest request = new(url, UnityWebRequest.kHttpVerbGET);
        request.downloadHandler = new DownloadHandlerFile(installerPath);
        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        while (!op.isDone)
        {
            EditorUtility.DisplayProgressBar("Installing Git", "Downloading...", op.progress);
            await Task.Delay(100);
        }

        EditorUtility.ClearProgressBar();

        if (request.result != UnityWebRequest.Result.Success)
        {
            EditorUtility.DisplayDialog("Download Failed", request.error, "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("Installing Git", "Running installer...", 0.5f);
        ProcessStartInfo startInfo = Application.platform == RuntimePlatform.WindowsEditor
            ? new ProcessStartInfo { FileName = installerPath, Arguments = "/VERYSILENT /NORESTART", UseShellExecute = true }
            : new ProcessStartInfo { FileName = "sudo", Arguments = $"installer -pkg {installerPath} -target /", UseShellExecute = true };

        Process proc = Process.Start(startInfo)!;
        await Task.Run(() => proc.WaitForExit());
        EditorUtility.ClearProgressBar();

        if (File.Exists(installerPath))
            File.Delete(installerPath);

        if (proc.ExitCode == 0)
        {
            EditorUtility.DisplayDialog("Git Installed", "Git has been installed. Please restart Unity.", "OK");
        }
        else
            EditorUtility.DisplayDialog("Install Failed", "Git installation failed.", "OK");
    }

    private readonly struct GitResult
    {
        public readonly string Output;
        public readonly string Error;
        public readonly int ExitCode;
        public GitResult(string o, string e, int c) { Output = o; Error = e; ExitCode = c; }
    }
}