// Unity CLI eval_file method body. Edit Mode only; uses disposable test assets.
UnityEditor.SessionState.SetString("GHA.UmaIndexValidation", "queued");
UnityEditor.EditorApplication.delayCall += () =>
{
try
{
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ||
    UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
    throw new System.InvalidOperationException("Run after import/compilation in Edit Mode.");

var installer = System.AppDomain.CurrentDomain.GetAssemblies()
    .Select(a => a.GetType("GHA.Integration.Editor.GhaUmaAssetInstaller"))
    .FirstOrDefault(t => t != null);
var ensure = installer?.GetMethod("EnsureAssetIndex",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
if (ensure == null) throw new System.Exception("UMA provider installer is not compiled/enabled.");

var passed = new System.Collections.Generic.List<string>();
string testRoot = "Assets/GhaUmaIndexValidation-" + System.Guid.NewGuid().ToString("N");
string projectPath = testRoot + "/Resources/AssetIndexerProject.asset";
string legacyPath = testRoot + "/Legacy/AssetIndexer.asset";
System.Func<string, string> hash = path =>
{
    using (var sha = System.Security.Cryptography.SHA256.Create())
        return System.BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(path)));
};
System.Func<string, string, object[]> invoke = (project, legacy) =>
{
    object[] args = { project, legacy, false };
    var result = ensure.Invoke(null, args);
    return new object[] { result, args[2] };
};
try
{
    var result = invoke(projectPath, legacyPath);
    var index = result[0] as UMA.UMAAssetIndexer;
    if (!(bool)result[1] || index == null || index.SerializedItems.Count == 0 ||
        UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.UMAAssetIndexer>(projectPath) != index)
        throw new System.Exception("Fresh creation did not produce a populated persistent index.");
    int savedCount = index.SerializedItems.Count;
    UnityEngine.Resources.UnloadAsset(index);
    index = UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.UMAAssetIndexer>(projectPath);
    if (index == null || index.SerializedItems.Count != savedCount)
        throw new System.Exception("Generated entries did not survive unloading and reloading the saved index.");
    index.BuildStringTypes();
    index.DoInitialDictionaryLoad();
    if (!index.GetCounts().ContainsKey("RaceData"))
        throw new System.Exception("New index is missing standard race dictionaries.");
    if (index.SerializedItems.Any(item => !item._Path.StartsWith("Assets/UMA/", System.StringComparison.Ordinal)))
        throw new System.Exception("First-time population included content outside the standard UMA tree.");
    foreach (string race in new[] { "Human Male 3.0", "Human Female 3.0" })
        if (index.GetAssetItem<UMA.RaceData>(race)?.Item == null)
            throw new System.Exception("Default human race missing: " + race);
    passed.Add("Fresh path creates a populated default-UMA-only index with both human races (" + index.SerializedItems.Count + " entries)");
    passed.Add("Generated entries and human race references survive asset unload/reload");

    // Preserve user configuration, serialized bytes and GUID on repeated installation.
    index.typeFolders.Add(new UMA.UMAAssetIndexer.TypeFolders
    {
        typeName = "RaceData",
        Folders = new[] { "Assets/UMA/UMA3" }
    });
    UnityEditor.EditorUtility.SetDirty(index);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(index);
    string before = hash(projectPath);
    string guid = UnityEditor.AssetDatabase.AssetPathToGUID(projectPath);
    result = invoke(projectPath, legacyPath);
    if ((bool)result[1] || result[0] != index || hash(projectPath) != before ||
        UnityEditor.AssetDatabase.AssetPathToGUID(projectPath) != guid)
        throw new System.Exception("Repeated initialization changed the project-owned index.");
    passed.Add("Existing project index, configuration, bytes and GUID preserved");

    UMA.UMAPathUtility.EnsureAssetFolder(testRoot + "/Legacy");
    var legacyIndex = UnityEngine.ScriptableObject.CreateInstance<UMA.UMAAssetIndexer>();
    UnityEditor.AssetDatabase.CreateAsset(legacyIndex, legacyPath);
    string legacyBefore = hash(legacyPath);
    result = invoke(testRoot + "/NotCreated/AssetIndexerProject.asset", legacyPath);
    if ((bool)result[1] || result[0] != legacyIndex || hash(legacyPath) != legacyBefore ||
        UnityEditor.AssetDatabase.IsValidFolder(testRoot + "/NotCreated"))
        throw new System.Exception("Existing install index was not preserved.");
    passed.Add("Existing install index retained without creating a second index");
    result = invoke(projectPath, legacyPath);
    if ((bool)result[1] || result[0] != index || hash(legacyPath) != legacyBefore)
        throw new System.Exception("Project index did not take precedence over install index.");
    passed.Add("Project index takes precedence, matching UMA's loader");

    string occupied = testRoot + "/Occupied.asset";
    UnityEditor.AssetDatabase.CreateAsset(new UnityEngine.AnimationClip(), occupied);
    string occupiedBefore = hash(occupied);
    foreach (var paths in new[]
    {
        new[] { occupied, legacyPath },
        new[] { testRoot + "/Missing.asset", occupied }
    })
    {
        bool rejected = false;
        try { invoke(paths[0], paths[1]); }
        catch (System.Reflection.TargetInvocationException ex)
        {
            rejected = ex.InnerException is System.InvalidOperationException &&
                ex.InnerException.Message.Contains("not a loadable UMAAssetIndexer");
        }
        if (!rejected || hash(occupied) != occupiedBefore ||
            System.IO.File.Exists(testRoot + "/Missing.asset"))
            throw new System.Exception("Wrong-type asset was not rejected without mutation.");
    }
    passed.Add("Wrong-type assets at either index path rejected without overwrite");

    // This development project already has an index. Verify its real installer path is a no-op.
    string actualProject = UMA.UMAPathUtility.ProjectIndexerPath;
    string actualLegacy = UMA.UMAPathUtility.ResolveInstallAssetPath(
        "InternalDataStore/InGame/Resources/AssetIndexer.asset");
    string actualPath = UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.UMAAssetIndexer>(actualProject) != null
        ? actualProject : actualLegacy;
    if (UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.UMAAssetIndexer>(actualPath) == null)
        throw new System.Exception("Live-project preservation check requires an existing index.");
    string actualBefore = hash(actualPath);
    int rootsBefore = UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount;
    result = invoke(actualProject, actualLegacy);
    if ((bool)result[1] || hash(actualPath) != actualBefore ||
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount != rootsBefore)
        throw new System.Exception("Existing live index or scene roots changed.");
    passed.Add("Actual GHA index preserved; no scene generator created");
}
finally
{
    // Only this invocation's uniquely named test folder is removed.
    if (UnityEditor.AssetDatabase.IsValidFolder(testRoot) && !UnityEditor.AssetDatabase.DeleteAsset(testRoot))
        throw new System.Exception("Could not remove temporary validation folder: " + testRoot);
}
string report = Newtonsoft.Json.JsonConvert.SerializeObject(new
{
    passed = passed.ToArray(), temporaryAssetsRemoved = !System.IO.Directory.Exists(testRoot)
});
UnityEditor.SessionState.SetString("GHA.UmaIndexValidation", report);
UnityEngine.Debug.Log("GHA UMA index validation: " + report);
}
catch (System.Exception ex)
{
    UnityEditor.SessionState.SetString("GHA.UmaIndexValidation", "FAILED: " + ex);
    UnityEngine.Debug.LogException(ex);
}
};
return "Queued Edit Mode index validation; read SessionState GHA.UmaIndexValidation for the result.";
