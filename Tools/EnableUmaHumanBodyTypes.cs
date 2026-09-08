// Run with Unity CLI eval_file in Edit Mode. Append-only catalog extension; no vendor writes.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Stop Play Mode with owner approval before changing the catalog.");
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GHA.AvatarSuite.UmaAvatarCatalog>("Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset");
var male = UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.RaceData>("Assets/UMA/UMA3/Races/HumanMale30.asset");
var female = UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.RaceData>("Assets/UMA/UMA3/Races/HumanFemale30.asset");
if (catalog == null || catalog.races.Count == 0 || catalog.races[0] != male || female == null)
    throw new System.Exception("Catalog/race baseline differs from the reviewed male-at-ID-0 layout.");
foreach (var race in new[] { male, female })
    if (race.umaTarget != UMA.RaceData.UMATarget.Humanoid || race.TPose == null || race.baseRaceRecipe == null)
        throw new System.Exception("Incomplete humanoid definition: " + race.raceName);

var names = new[] { "Hoodie_turquoise_Recipe", "tshirt_turquoise_Recipe", "tanktop_yellow_Recipe", "tights_gray_Recipe", "shorts_turquoise_Recipe", "shoe_low_white_Recipe", "shoes_tall_white_Recipe" };
var additions = new System.Collections.Generic.List<UMA.CharacterSystem.UMAWardrobeRecipe>();
foreach (var name in names)
{
    var item = UnityEditor.AssetDatabase.LoadAssetAtPath<UMA.CharacterSystem.UMAWardrobeRecipe>("Assets/UMA/UMA3/Wearables/Wardrobe/" + name + ".asset");
    if (item == null || !item.compatibleRaces.Contains(female.raceName) || !female.wardrobeSlots.Contains(item.wardrobeSlot))
        throw new System.Exception("Missing/incompatible female wardrobe: " + name);
    additions.Add(item);
}
// Preserve all existing race and wardrobe IDs, even if this is run again later.
UnityEditor.Undo.RecordObject(catalog, "Enable human body types");
if (!catalog.races.Contains(female)) catalog.races.Add(female);
foreach (var item in additions)
    if (!catalog.wardrobeRecipes.Contains(item)) catalog.wardrobeRecipes.Add(item);
if (catalog.BodyType(catalog.RaceId(male)) == null)
    catalog.bodyTypes.Add(new GHA.AvatarSuite.UmaAvatarCatalog.BodyTypeDef {
        race = male, label = "Human Male",
        startingWardrobe = new System.Collections.Generic.List<UMA.CharacterSystem.UMAWardrobeRecipe>(catalog.defaultWardrobe)
    });
if (catalog.BodyType(catalog.RaceId(female)) == null)
    catalog.bodyTypes.Add(new GHA.AvatarSuite.UmaAvatarCatalog.BodyTypeDef {
        race = female, label = "Human Female",
        startingWardrobe = new System.Collections.Generic.List<UMA.CharacterSystem.UMAWardrobeRecipe> {
            additions[0], additions[3], additions[5], catalog.wardrobeRecipes[2]
        }
    });
UnityEditor.EditorUtility.SetDirty(catalog);
UnityEditor.AssetDatabase.SaveAssetIfDirty(catalog);
return new { maleId = catalog.RaceId(male), femaleId = catalog.RaceId(female), wardrobes = catalog.wardrobeRecipes.Count, saved = !UnityEditor.EditorUtility.IsDirty(catalog) };
