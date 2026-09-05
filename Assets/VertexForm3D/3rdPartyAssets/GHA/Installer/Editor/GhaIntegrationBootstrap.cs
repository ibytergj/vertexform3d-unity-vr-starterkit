#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GHA.Integration.Editor
{
    [InitializeOnLoad]
    public static class GhaIntegrationBootstrap
    {
        public const string HostSymbol = "VERTEXFORM_GHA_HOST";
        public const string UmaSymbol = "VERTEXFORM_GHA_UMA";

        private const string HostPatchPath =
            "Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/Patches/VertexFormGhaHost.patch";
        private const string PendingKeyPrefix = "GHA.Integration.PendingOperation.";
        private const string HostAssetInstallerType =
            "GHA.Integration.Editor.GhaVertexFormAssetInstaller, Assembly-CSharp-Editor";
        private const string UmaAssetInstallerType =
            "GHA.Integration.Editor.GhaUmaAssetInstaller, Assembly-CSharp-Editor";

        private static readonly NamedBuildTarget[] SupportedTargets =
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Android,
            NamedBuildTarget.WebGL
        };

        static GhaIntegrationBootstrap()
        {
            EditorApplication.delayCall += ResumePendingOperation;
        }

        [MenuItem("Tools/GHA/Integration/Install GHA Host Layer", priority = 2000)]
        public static void InstallHost()
        {
            EnsureEditorReady();
            SetPatchInstalled(true);
            EditorPrefs.SetString(PendingKey, "InstallHostAssets");
            SetSymbol(HostSymbol, true);
            AssetDatabase.Refresh();
            ResumePendingOperation();
        }

        [MenuItem("Tools/GHA/Integration/Uninstall GHA Host Layer", priority = 2003)]
        public static void UninstallHost()
        {
            EnsureEditorReady();
            if (HasSymbol(UmaSymbol))
                throw new InvalidOperationException(
                    "Uninstall the UMA provider layer before uninstalling the GHA host layer.");

            InvokeLayerInstaller(HostAssetInstallerType, "UninstallAssets");
            SetPatchInstalled(false);
            SetSymbol(HostSymbol, false);
            AssetDatabase.Refresh();
            Debug.Log("GHA host integration uninstalled. Stock VertexForm source and prefabs were restored.");
        }

        [MenuItem("Tools/GHA/Integration/Install UMA Provider Layer", priority = 2001)]
        public static void InstallUma()
        {
            EnsureEditorReady();
            if (!HasSymbol(HostSymbol))
                throw new InvalidOperationException(
                    "Install the GHA host layer before installing the UMA provider layer.");
            if (!File.Exists(ProjectPath("Assets/UMA/Core/DynamicCharacterSystem/Scripts/DynamicCharacterAvatar.cs")))
                throw new InvalidOperationException(
                    "A compatible UMA 3 installation was not found under Assets/UMA.");

            EditorPrefs.SetString(PendingKey, "InstallUmaAssets");
            SetSymbol(UmaSymbol, true);
            AssetDatabase.Refresh();
            ResumePendingOperation();
        }

        [MenuItem("Tools/GHA/Integration/Uninstall UMA Provider Layer", priority = 2002)]
        public static void UninstallUma()
        {
            EnsureEditorReady();
            if (HasSymbol(UmaSymbol))
                InvokeLayerInstaller(UmaAssetInstallerType, "UninstallAssets");
            SetSymbol(UmaSymbol, false);
            AssetDatabase.Refresh();
            Debug.Log("GHA UMA provider integration uninstalled. The provider-neutral GHA host layer remains installed.");
        }

        [MenuItem("Tools/GHA/Integration/Install GHA Host Layer", validate = true)]
        [MenuItem("Tools/GHA/Integration/Install UMA Provider Layer", validate = true)]
        [MenuItem("Tools/GHA/Integration/Uninstall UMA Provider Layer", validate = true)]
        [MenuItem("Tools/GHA/Integration/Uninstall GHA Host Layer", validate = true)]
        private static bool ValidateMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling;
        }

        private static void ResumePendingOperation()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            string operation = EditorPrefs.GetString(PendingKey, string.Empty);
            if (string.IsNullOrEmpty(operation))
                return;

            try
            {
                switch (operation)
                {
                    case "InstallHostAssets":
                        if (!HasSymbol(HostSymbol))
                            return;
                        InvokeLayerInstaller(HostAssetInstallerType, "InstallAssets");
                        break;
                    case "InstallUmaAssets":
                        if (!HasSymbol(UmaSymbol))
                            return;
                        InvokeLayerInstaller(UmaAssetInstallerType, "InstallAssets");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown GHA pending operation '{operation}'.");
                }

                EditorPrefs.DeleteKey(PendingKey);
            }
            catch (Exception exception)
            {
                Debug.LogError($"GHA integration operation '{operation}' failed: {exception}");
            }
        }

        private static void InvokeLayerInstaller(string typeName, string methodName)
        {
            Type type = Type.GetType(typeName, false);
            if (type == null)
                throw new InvalidOperationException(
                    $"Integration installer '{typeName}' is not compiled. Check scripting symbols and compilation errors.");

            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new MissingMethodException(type.FullName, methodName);

            try
            {
                method.Invoke(null, null);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw exception.InnerException;
            }
        }

        private static void SetPatchInstalled(bool installed)
        {
            string patchPath = ProjectPath(HostPatchPath);
            if (!File.Exists(patchPath))
                throw new FileNotFoundException("The GHA host patch file is missing.", patchPath);

            if (GitApplyCheck(patchPath, reverse: !installed, out _))
            {
                RunGitApply(patchPath, reverse: !installed, checkOnly: false);
                return;
            }

            if (GitApplyCheck(patchPath, reverse: installed, out string diagnostic))
                return;

            string action = installed ? "install" : "uninstall";
            throw new InvalidOperationException(
                $"Cannot {action} the GHA host source patch cleanly. No files were changed.\n{diagnostic}");
        }

        private static bool GitApplyCheck(string patchPath, bool reverse, out string diagnostic)
        {
            (int exitCode, string output) = RunGitApply(patchPath, reverse, checkOnly: true);
            diagnostic = output;
            return exitCode == 0;
        }

        private static (int exitCode, string output) RunGitApply(
            string patchPath,
            bool reverse,
            bool checkOnly)
        {
            string arguments = "apply";
            if (checkOnly)
                arguments += " --check";
            if (reverse)
                arguments += " --reverse";
            arguments += " --whitespace=nowarn \"" + patchPath.Replace("\"", "\\\"") + "\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ResolveGitExecutable(),
                Arguments = arguments,
                WorkingDirectory = ProjectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start Git.");
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, (stdout + Environment.NewLine + stderr).Trim());
        }

        private static string ResolveGitExecutable()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                return "git";

            string[] candidates =
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Git", "cmd", "git.exe"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Git", "cmd", "git.exe")
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return "git";
        }

        private static void SetSymbol(string symbol, bool enabled)
        {
            foreach (NamedBuildTarget target in SupportedTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out string[] current);
                var symbols = new HashSet<string>(
                    current.Where(candidate => !string.IsNullOrWhiteSpace(candidate)),
                    StringComparer.Ordinal);
                bool changed = enabled ? symbols.Add(symbol) : symbols.Remove(symbol);
                if (!changed)
                    continue;
                PlayerSettings.SetScriptingDefineSymbols(
                    target,
                    symbols.OrderBy(candidate => candidate, StringComparer.Ordinal).ToArray());
            }
        }

        private static bool HasSymbol(string symbol)
        {
            foreach (NamedBuildTarget target in SupportedTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out string[] current);
                if (current.Contains(symbol, StringComparer.Ordinal))
                    return true;
            }
            return false;
        }

        private static void EnsureEditorReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before changing GHA integration layers.");
            if (EditorApplication.isCompiling)
                throw new InvalidOperationException("Wait for Unity script compilation to finish.");
        }

        private static string ProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath));
        }

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Unity project root could not be resolved.");

        private static string PendingKey =>
            PendingKeyPrefix + Hash128.Compute(Application.dataPath).ToString();
    }

    [Serializable]
    internal sealed class GhaIntegrationState
    {
        public bool hostInstalled;
        public bool umaInstalled;
        public bool capturedHomeState;
        public Vector3 stockPreviewLocalPosition;
        public bool previousButtonActive;
        public bool nextButtonActive;
        public GhaFileSnapshot[] hostSnapshots;
        public GhaFileSnapshot[] umaSnapshots;
    }

    [Serializable]
    internal sealed class GhaFileSnapshot
    {
        public string assetPath;
        public string backupPath;
        public string installedSha256;
    }

    internal static class GhaIntegrationStateStore
    {
        private const string RelativePath = "UserSettings/GHAIntegrationState.json";
        private const string BackupRoot = "UserSettings/GHAIntegrationBackups";

        public static GhaIntegrationState Load()
        {
            string path = AbsolutePath;
            if (!File.Exists(path))
                return new GhaIntegrationState();
            try
            {
                return JsonUtility.FromJson<GhaIntegrationState>(File.ReadAllText(path))
                       ?? new GhaIntegrationState();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Could not read GHA integration state '{path}'.", exception);
            }
        }

        public static void Save(GhaIntegrationState state)
        {
            string path = AbsolutePath;
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(state, true));
        }

        public static GhaFileSnapshot[] CaptureSnapshots(
            string layer,
            IEnumerable<string> assetPaths)
        {
            string projectRoot = ProjectRoot;
            string backupDirectory = Path.Combine(projectRoot, BackupRoot, layer);
            Directory.CreateDirectory(backupDirectory);
            return assetPaths.Select((assetPath, index) =>
            {
                string source = Path.Combine(projectRoot, assetPath);
                if (!File.Exists(source))
                    throw new FileNotFoundException(
                        $"Cannot snapshot missing integration target '{assetPath}'.", source);
                string backupRelative = Path.Combine(
                    BackupRoot,
                    layer,
                    $"{index:D2}-{Path.GetFileName(assetPath)}.backup");
                string backup = Path.Combine(projectRoot, backupRelative);
                File.Copy(source, backup, true);
                return new GhaFileSnapshot
                {
                    assetPath = assetPath,
                    backupPath = backupRelative,
                    installedSha256 = string.Empty
                };
            }).ToArray();
        }

        public static void RecordInstalledHashes(GhaFileSnapshot[] snapshots)
        {
            if (snapshots == null)
                return;
            foreach (GhaFileSnapshot snapshot in snapshots)
                snapshot.installedSha256 = Sha256(ProjectFile(snapshot.assetPath));
        }

        public static bool TryRestoreUnchangedSnapshots(
            GhaFileSnapshot[] snapshots,
            string layer)
        {
            if (snapshots == null || snapshots.Length == 0)
                return false;
            foreach (GhaFileSnapshot snapshot in snapshots)
            {
                string current = ProjectFile(snapshot.assetPath);
                if (!File.Exists(current) ||
                    string.IsNullOrEmpty(snapshot.installedSha256) ||
                    !string.Equals(
                        Sha256(current),
                        snapshot.installedSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning(
                        $"GHA {layer} uninstall detected edits after installation in " +
                        $"'{snapshot.assetPath}'. Exact snapshot restore was skipped; " +
                        "the installer will remove only owned objects/components.");
                    return false;
                }
            }

            foreach (GhaFileSnapshot snapshot in snapshots)
            {
                string backup = ProjectFile(snapshot.backupPath);
                if (!File.Exists(backup))
                    throw new FileNotFoundException(
                        $"GHA {layer} backup is missing for '{snapshot.assetPath}'.", backup);
                File.Copy(backup, ProjectFile(snapshot.assetPath), true);
                if (snapshot.assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                    snapshot.assetPath.StartsWith("Packages/", StringComparison.Ordinal))
                {
                    AssetDatabase.ImportAsset(
                        snapshot.assetPath,
                        ImportAssetOptions.ForceUpdate);
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static string Sha256(string path)
        {
            using SHA256 algorithm = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ProjectFile(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath));
        }

        private static string AbsolutePath
        {
            get
            {
                return Path.Combine(ProjectRoot, RelativePath);
            }
        }

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Unity project root could not be resolved.");
    }
}
#endif
