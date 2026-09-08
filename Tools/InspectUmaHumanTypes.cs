var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GHA.AvatarSuite.UmaAvatarCatalog>("Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset");
var result = new System.Collections.Generic.List<object>();
foreach (var raceName in new[] { "HumanMale30", "HumanFemale30" })
{
    var race = UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.RaceData>("Assets/UMA/UMA3/Races/" + raceName + ".asset");
    result.Add(new { asset = raceName, race.raceName, target = race.umaTarget.ToString(), tpose = race.TPose != null, baseRecipe = race.baseRaceRecipe != null ? race.baseRaceRecipe.name : null, slots = race.wardrobeSlots, cross = race.GetCrossCompatibleRaces() });
}
var clothes = new System.Collections.Generic.List<object>();
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:UMAWardrobeRecipe", new[] { "Assets/UMA/UMA3" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var item = UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.CharacterSystem.UMAWardrobeRecipe>(path);
    if (new[] { "Hair", "Chest", "Legs", "Feet" }.Contains(item.wardrobeSlot) && (item.compatibleRaces.Contains("Human Female 3.0") || catalog.WardrobeId(item) >= 0))
        clothes.Add(new { item.name, slot = item.wardrobeSlot, races = string.Join(",", item.compatibleRaces), id = catalog.WardrobeId(item) });
}
return new { races = result, clothes, defaults = catalog.defaultWardrobe.ConvertAll(w => w.name) };
