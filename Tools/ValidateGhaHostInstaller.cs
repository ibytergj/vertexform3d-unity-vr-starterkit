// Run with Unity CLI run_script, entry ValidateGhaHostInstaller.Main. Edit Mode only.
// Uses disposable copies of the original broken prefab, then reruns the host install twice.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ValidateGhaHostInstaller
{
    public static object Main(string brokenPrefabFixture = "UserSettings/GHAIntegrationBackups/host/00-NewGenericMRVRPrefab.prefab.backup")
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Run in stopped, idle Edit Mode.");
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!string.Equals(project.Replace('\\', '/'), "E:/Test/GHA-Review", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This recovery acceptance test is scoped to E:/Test/GHA-Review.");
        var installer = Type.GetType("GHA.Integration.Editor.GhaVertexFormAssetInstaller, Assembly-CSharp-Editor", true);
        var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        var validate = installer.GetMethod("ValidatePrefabTargets", flags);
        var save = installer.GetMethod("SavePrefabChecked", flags);
        var install = installer.GetMethod("InstallAssets", flags);
        var playerPaths = (string[])installer.GetField("PlayerPrefabPaths", flags).GetValue(null);
        string[] targets = playerPaths.Concat(new[] {
            "Assets/VertexForm3D/Resources/CustomEditor/HomeSceneComponent.prefab",
            "Assets/VertexForm3D/3rdPartyAssets/GHA/Generated/GHAAvatarPanel.prefab"
        }).ToArray();
        var passed = new List<string>();
        var expectedErrors = new List<string>();
        Application.LogCallback log = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception) expectedErrors.Add(message);
        };
        string folder = "Assets/GhaHostInstallerValidation-" + Guid.NewGuid().ToString("N");
        string broken = folder + "/BrokenPlayer.prefab";
        string backup = "UserSettings/GHAIntegrationBackups/host/00-NewGenericMRVRPrefab.prefab.backup";
        string originalPlayerPath = playerPaths[0];
        GameObject contents = null;
        Func<string, string> hash = path => {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
        };
        Action<Action, string> rejects = (action, message) => {
            try { action(); }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) {
                passed.Add(message); return;
            }
            throw new Exception("Expected rejection: " + message);
        };
        try
        {
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            if (!File.ReadAllText(brokenPrefabFixture).Contains("74f14e1eb550b9a4fb6c0a2f0456845b"))
                throw new Exception("Supply an original pre-repair player prefab as brokenPrefabFixture (retained under Logs/ReviewTransfer/CesiumRepair-*).");
            File.Copy(brokenPrefabFixture, broken);
            AssetDatabase.ImportAsset(broken, ImportAssetOptions.ForceSynchronousImport);
            validate.Invoke(null, new object[] { targets });
            passed.Add("All real host target prefabs pass preflight");
            var hashes = targets.Select(hash).ToArray();
            string backupHash = hash(backup);
            rejects(() => validate.Invoke(null, new object[] { new[] { folder + "/Absent.prefab" } }), "Missing target is rejected");
            playerPaths[0] = broken;
            rejects(() => install.Invoke(null, null), "Full installer rejects a missing script before modifying any host prefab");
            playerPaths[0] = originalPlayerPath;
            if (!hashes.SequenceEqual(targets.Select(hash)) || hash(backup) != backupHash)
                throw new Exception("Rejected install changed a target or replaced its original backup.");
            string statePath = "UserSettings/GHAIntegrationState.json";
            if (!File.ReadAllText(statePath).Contains("\"hostInstalled\": false"))
                throw new Exception("Failed attempt still marked installed.");
            passed.Add("Failed attempt clears installed status and preserves assets/original snapshots");
            contents = PrefabUtility.LoadPrefabContents(broken);
            Application.logMessageReceived += log;
            rejects(() => save.Invoke(null, new object[] { contents, broken }), "Unity prefab-save failure is propagated, not reported as success");
            Application.logMessageReceived -= log;
            PrefabUtility.UnloadPrefabContents(contents);
            contents = null;
            install.Invoke(null, null);
            passed.Add("Host installation succeeds after the prefab repair, without uninstalling");
            if (hash(backup) != backupHash) throw new Exception("Successful retry replaced original backup.");
            hashes = targets.Select(hash).ToArray();
            install.Invoke(null, null);
            if (!hashes.SequenceEqual(targets.Select(hash))) throw new Exception("Repeated install changed prefab bytes.");
            passed.Add("Second successful install leaves all four prefab files byte-identical");
            foreach (string path in playerPaths)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root.GetComponentsInChildren<Transform>(true).Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0))
                    throw new Exception("Missing script after installation: " + path);
                if (root.GetComponents<Component>().Count(c => c != null && c.GetType().FullName == "VertexFormCore.AvatarExtensionSync") != 1)
                    throw new Exception("Expected exactly one AvatarExtensionSync: " + path);
                if (root.GetComponents<Component>().Count(c => c != null && c.GetType().FullName == "GHA.AvatarSuite.UmaAvatarBridge") != 1)
                    throw new Exception("Existing UMA bridge was changed: " + path);
            }
            passed.Add("Both player roots retain one sync and one UMA bridge, with zero missing scripts");
            return new { passed, expectedSaveFailureMessages = expectedErrors, playMode = "not entered" };
        }
        finally
        {
            Application.logMessageReceived -= log;
            playerPaths[0] = originalPlayerPath;
            if (contents != null) PrefabUtility.UnloadPrefabContents(contents);
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
        }
    }
}
