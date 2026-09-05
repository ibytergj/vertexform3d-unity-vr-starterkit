// Read-only dry run. Run with Unity CLI eval_file in stopped Edit Mode.
// Writes a report outside the Asset Database; does not edit/save assets or settings.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new InvalidOperationException("Stop Play Mode before the content audit.");
string catalogPath = "Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset";
string indexPath = "Assets/UMA/InternalDataStore/InGame/Resources/AssetIndexer.asset";
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GHA.AvatarSuite.UmaAvatarCatalog>(catalogPath);
var index = UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.UMAAssetIndexer>(indexPath);
if (catalog == null || index == null) throw new InvalidOperationException("Required catalog/index missing.");
// Build only in-memory lookups. Do not call Initialize(), which also creates a generator.
index.BuildStringTypes();
index.DoInitialDictionaryLoad();
var errors = new System.Collections.Generic.List<string>();
var named = new System.Collections.Generic.Dictionary<string, UMA.AssetItem>();
var recipes = new System.Collections.Generic.List<UMA.UMAPackedRecipeBase>();
foreach (var race in catalog.races)
{
    if (race == null) { errors.Add("Null race in catalog."); continue; }
    var packed = race.baseRaceRecipe as UMA.UMAPackedRecipeBase;
    if (packed == null) errors.Add("Unsupported base recipe: " + race.name);
    else recipes.Add(packed);
}
foreach (var recipe in catalog.wardrobeRecipes)
    if (recipe == null) errors.Add("Null wardrobe in catalog."); else recipes.Add(recipe);
foreach (var recipe in catalog.defaultWardrobe)
    if (recipe == null || !catalog.wardrobeRecipes.Contains(recipe)) errors.Add("Default wardrobe absent from catalog.");
var recipeRows = new System.Collections.Generic.List<object>();
Action<Type,string,string> require = (type,name,recipeName) =>
{
    if (string.IsNullOrWhiteSpace(name)) return;
    var item = index.GetAssetItem(type, name);
    if (item == null || item.Item == null) { errors.Add(recipeName + ": missing " + type.Name + " " + name); return; }
    named[type.Name+":"+name] = item;
};
foreach (var recipe in recipes.Distinct())
{
    var packed = recipe.PackedLoad();
    if (packed == null) { errors.Add("Unreadable recipe: " + recipe.name); continue; }
    if (packed.slotsV3 != null)
    {
        foreach (var slot in packed.slotsV3)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.id)) continue;
            require(typeof(UMA.SlotDataAsset),slot.id,recipe.name);
            if (slot.overlays != null) foreach(var overlay in slot.overlays)
                if (overlay != null) require(typeof(UMA.OverlayDataAsset),overlay.id,recipe.name);
        }
    }
    else if (packed.slotsV2 != null)
    {
        foreach (var slot in packed.slotsV2)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.id)) continue;
            require(typeof(UMA.SlotDataAsset),slot.id,recipe.name);
            if (slot.overlays != null) foreach(var overlay in slot.overlays)
                if (overlay != null) require(typeof(UMA.OverlayDataAsset),overlay.id,recipe.name);
        }
    }
    else errors.Add("Recipe has no supported slot list: " + recipe.name);
    var items = index.GetAssetItems(recipe, true).Where(i=>i!=null).ToArray();
    foreach(var item in items) named[item._BaseTypeName+":"+item._Name]=item;
    recipeRows.Add(new { name=recipe.name, path=UnityEditor.AssetDatabase.GetAssetPath(recipe), label=recipe.AssignedLabel,
        resourcesOnly=recipe.resourcesOnly, items=items.Select(i=>i._BaseTypeName+":"+i._Name).Distinct().OrderBy(s=>s).ToArray() });
}
var roots = named.Values.Select(i=>i._Path).Concat(new[]{catalogPath,
    "Packages/com.vertexform3d.gha/Runtime/Animations/GHA_Locomotion_v2.controller"}).Distinct().ToArray();
var closure = new System.Collections.Generic.HashSet<string>(UnityEditor.AssetDatabase.GetDependencies(roots,true));
var candidates = index.SerializedItems.Where(i=>i!=null && closure.Contains(i._Path)).ToArray();
var excluded = index.SerializedItems.Where(i=>i!=null && !closure.Contains(i._Path)).ToArray();
var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(false);
var groups = new System.Collections.Generic.List<object>();
if(settings != null) foreach(var group in settings.groups)
{
    if(group==null) continue;
    var schema = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
    groups.Add(new { name=group.Name, entries=group.entries.Count, pack=schema!=null?schema.BundleMode.ToString():"not bundled",
        buildPathVariable=schema!=null?settings.profileSettings.GetProfileDataById(schema.BuildPath.Id)?.ProfileName:null,
        loadPathVariable=schema!=null?settings.profileSettings.GetProfileDataById(schema.LoadPath.Id)?.ProfileName:null });
}
var report = new {
    generatedUtc=DateTime.UtcNow.ToString("O"), dryRun=true,
    catalogPath, indexPath, catalogRaces=catalog.races.Count, catalogWardrobes=catalog.wardrobeRecipes.Count,
    sourceIndexRecords=index.SerializedItems.Count,
    explicitlyResolvedNamedAssets=named.Count,
    candidateRuntimeIndexRecords=candidates.Length,
    candidatesByType=candidates.GroupBy(i=>i._BaseTypeName).Select(g=>new{type=g.Key,count=g.Count()}).ToArray(),
    candidateRecords=candidates.Select(i=>new{type=i._BaseTypeName,name=i._Name,path=i._Path}).ToArray(),
    notInCandidateClosure=excluded.Select(i=>new{type=i._BaseTypeName,name=i._Name,path=i._Path}).ToArray(),
    dependencyPathCount=closure.Count, recipeRows, existingAddressableGroups=groups,
    errors, limitations=new[]{"Dependency closure is a conservative candidate, not permission to remove assets.",
    "Validate dynamic material/slot event dependencies and final built bundle graph before activating.",
    "Existing Resources index and catalog direct references must not retain streamed assets in the startup graph."}
};
string outputDir=System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName,"Logs","UMA-Streaming");
System.IO.Directory.CreateDirectory(outputDir);
string outputPath=System.IO.Path.Combine(outputDir,"content-plan-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json");
System.IO.File.WriteAllText(outputPath,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return new {report=outputPath,errors,candidateRecords=candidates.Length,originalRecords=index.SerializedItems.Count, namedAssets=named.Count,groupCount=groups.Count};
