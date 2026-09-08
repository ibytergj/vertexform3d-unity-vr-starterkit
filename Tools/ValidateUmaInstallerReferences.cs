// Unity CLI run_script entry ValidateUmaInstallerReferences.Main, stopped Edit Mode.
// Retention stress test followed by installation/reinstallation in the review project.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GHA.AvatarSuite;

public static class ValidateUmaInstallerReferences
{
    public static async Task<object> Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Run in idle Edit Mode.");
        if (Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/') != "E:/Test/GHA-Review")
            throw new InvalidOperationException("This acceptance test is scoped to E:/Test/GHA-Review.");
        const string catalogPath = "Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset";
        const string sliderPath = "Packages/com.vertexform3d.gha.uma/Runtime/UI/UMADNASlider.prefab";
        const string indexPath = "Assets/UMAProjectData/Resources/AssetIndexerProject.asset";
        var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        var installer = Type.GetType("GHA.Integration.Editor.GhaUmaAssetInstaller, Assembly-CSharp-Editor", true);
        var roots = installer.GetField("installationAssets", flags);
        var validate = installer.GetMethod("ValidateCatalogBodyTypes", flags);
        var install = installer.GetMethod("InstallAssets", flags);
        var verify = installer.GetMethod("ValidateInstalledAssets", flags);
        var passed = new List<string>();
        Func<string, string> hash = path => {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
        };
        string originalCatalog = hash(catalogPath);
        string originalIndex = hash(indexPath);
        string savedRecipe = PlayerPrefs.GetString("GHA_UMA_RECIPE", "");
        UmaAvatarCatalog invalid = null;
        try
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UmaAvatarCatalog>(catalogPath);
            var sliderRoot = AssetDatabase.LoadAssetAtPath<GameObject>(sliderPath);
            roots.SetValue(null, new UnityEngine.Object[] { catalog, sliderRoot });
            var unload = Resources.UnloadUnusedAssets();
            while (!unload.isDone) await Task.Yield();
            if (catalog == null || sliderRoot == null || sliderRoot.GetComponentInChildren<Slider>(true) == null)
                throw new Exception("Required input lost during unused-asset cleanup.");
            validate.Invoke(null, new object[] { catalog });
            passed.Add("Catalog and slider survive the same cleanup operation used by UMA's rebuild");
            var recipe = UmaRecipeStore.Default(catalog);
            if (!catalog.IsHumanoidRace(recipe.raceId) || recipe.raceId != 0 || catalog.races.Count != 2)
                throw new Exception("Default human catalog definition is invalid.");
            passed.Add("Default recipe selects Human Male ID 0; both human definitions are complete");
            invalid = ScriptableObject.CreateInstance<UmaAvatarCatalog>();
            bool rejected = false;
            try { validate.Invoke(null, new object[] { invalid }); }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { rejected = true; }
            if (!rejected) throw new Exception("Invalid default catalog accepted.");
            passed.Add("Missing/default-invalid catalog is rejected before prefab wiring");
            roots.SetValue(null, null);
            install.Invoke(null, null);
            passed.Add("Repair install restores catalog assignments on Home, both players and the panel, plus panel slider");
            string[] targets = {
                "Assets/VertexForm3D/Resources/CustomEditor/HomeSceneComponent.prefab",
                "Assets/VertexForm3D/Resources/NewGenericMRVRPrefab.prefab",
                "Assets/VertexForm3D/Resources/NewGenericMRDesktopPrefab.prefab",
                "Assets/VertexForm3D/3rdPartyAssets/GHA/Generated/GHAAvatarPanel.prefab"
            };
            var hashes = targets.Select(hash).ToArray();
            install.Invoke(null, null);
            if (!hashes.SequenceEqual(targets.Select(hash))) throw new Exception("Second install changed prefab bytes.");
            passed.Add("Repeat install leaves all four prefabs byte-identical");
            unload = Resources.UnloadUnusedAssets();
            while (!unload.isDone) await Task.Yield();
            verify.Invoke(null, new object[] {
                AssetDatabase.LoadAssetAtPath<UmaAvatarCatalog>(catalogPath),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/UMA/UMA3/Animation/IdleController.controller"),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Packages/com.vertexform3d.gha/Runtime/Animations/GHA_Locomotion_v2.controller"),
                AssetDatabase.LoadAssetAtPath<GameObject>(sliderPath).GetComponentInChildren<Slider>(true)
            });
            passed.Add("All installed references remain valid after post-install unused-asset cleanup");
            if (hash(catalogPath) != originalCatalog || hash(indexPath) != originalIndex ||
                PlayerPrefs.GetString("GHA_UMA_RECIPE", "") != savedRecipe)
                throw new Exception("Repair changed catalog content, existing index or saved avatar choice.");
            passed.Add("Catalog IDs/defaults, existing index and saved avatar choice are unchanged");
            return new { passed, playMode = "not entered" };
        }
        finally
        {
            roots.SetValue(null, null);
            if (invalid != null) UnityEngine.Object.DestroyImmediate(invalid);
        }
    }
}
