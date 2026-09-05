// Read-only reference/dependency audit. Does not rebuild or save the Global Library.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new InvalidOperationException("Run in stopped Edit Mode.");
string indexPath = "Assets/UMA/InternalDataStore/InGame/Resources/AssetIndexer.asset";
var index = UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.UMAAssetIndexer>(indexPath);
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GHA.AvatarSuite.UmaAvatarCatalog>("Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset");
var dependencies = UnityEditor.AssetDatabase.GetDependencies(indexPath, true);
var catalogDependencies = UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(catalog), true);
return new
{
    indexPath,
    items = index.SerializedItems.Count,
    hardReferences = index.SerializedItems.Count(i => i != null && i._SerializedItem != null),
    addressable = index.SerializedItems.Count(i => i != null && i.IsAddressable),
    ignored = index.SerializedItems.Count(i => i != null && i.Ignore),
    types = index.SerializedItems.Where(i=>i!=null).GroupBy(i=>i._BaseTypeName).Select(g=>new {type=g.Key, count=g.Count(), hardReferences=g.Count(i=>i._SerializedItem!=null)}).ToArray(),
    indexDependencies = dependencies.Length,
    catalogDependencies = catalogDependencies.Length,
    indexOnlyDependencies = dependencies.Except(catalogDependencies).Count(),
    defineSymbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup)
};
