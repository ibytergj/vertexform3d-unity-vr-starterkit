// Isolated Edit Mode tests of real catalog/state/selector code. No PlayerPrefs or scene writes.
if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Edit Mode required.");
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GHA.AvatarSuite.UmaAvatarCatalog>("Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset");
void Check(bool condition, string message) { if (!condition) throw new System.Exception(message); }
Check(catalog.races.Count == 2 && catalog.races[0].raceName == "Human Male 3.0" && catalog.races[1].raceName == "Human Female 3.0", "Race IDs changed.");
var originalNames = new[] { "male_sportpants_grey_Recipe", "male_shoes_tall_Recipe", "Hair_Poofy", "male_hoodie_grey_Recipe", "male_hoodie_blue_Recipe", "male_tshirt_white_Recipe", "male_tanktop_yellow_Recipe", "male_jacket_hive_Recipe", "male_shorts_black_cotton_Recipe", "male_sweatpants_black_Recipe", "male_sportpants_blueWhite_Recipe", "male_shoe_low_white.001_Recipe", "male_shoes_tall_turquoise_Recipe", "bb_male_haircut_Recipe", "Hair_MessyPomp_Recipe", "HairPonytail_Recipe", "bb_Male_Military_Hair_Recipe" };
for (int i = 0; i < originalNames.Length; i++) Check(catalog.Wardrobe(i).name == originalNames[i], "Existing wardrobe ID changed: " + i);
var report = new System.Collections.Generic.List<object>();
for (int id = 0; id < catalog.races.Count; id++)
{
    var race = catalog.races[id];
    Check(catalog.IsHumanoidRace(id), "Unsupported humanoid: " + race.raceName);
    var missingDna = catalog.dnaNames.Where(n => !race.GetDNANames().Contains(n)).ToArray();
    Check(missingDna.Length == 0, "Missing DNA: " + string.Join(",", missingDna));
    Check(catalog.BodyType(id) != null, "Missing body type configuration.");
    foreach (var item in catalog.BodyType(id).startingWardrobe)
        Check(catalog.WardrobeId(item) >= 0 && catalog.IsWardrobeCompatible(id, item), "Invalid starter item: " + item.name);
    report.Add(new { race.raceName, humanoidDefinition = true, dna = race.GetDNANames().Count, options = catalog.wardrobeRecipes.Count(w => catalog.IsWardrobeCompatible(id, w)) });
}
Check(!catalog.IsWardrobeCompatible(1, catalog.Wardrobe(3)), "Male hoodie offered to female.");
Check(!catalog.IsWardrobeCompatible(0, catalog.Wardrobe(17)), "Female hoodie offered to male.");
Check(catalog.IsWardrobeCompatible(0, catalog.Wardrobe(15)) && catalog.IsWardrobeCompatible(1, catalog.Wardrobe(15)), "Shared hair rejected.");
Check(!catalog.IsHumanoidRace(-1) && !catalog.IsHumanoidRace(999), "Invalid IDs accepted.");
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var temporaryRace = UnityEngine.Object.Instantiate(catalog.races[0]);
var temporaryCatalog = UnityEngine.Object.Instantiate(catalog);
UnityEngine.RenderTexture target = null;
UnityEngine.Texture2D capture = null;
var previousTarget = UnityEngine.RenderTexture.active;
try
{
    temporaryCatalog.races = new System.Collections.Generic.List<UMA.RaceData> { temporaryRace };
    temporaryRace.umaTarget = UMA.RaceData.UMATarget.Generic;
    Check(!temporaryCatalog.IsHumanoidRace(0), "Generic rig accepted.");
    temporaryRace.umaTarget = UMA.RaceData.UMATarget.Humanoid;
    temporaryRace.TPose = null;
    Check(!temporaryCatalog.IsHumanoidRace(0), "Missing T-pose accepted.");
    var root = new UnityEngine.GameObject("Body type validation", typeof(UnityEngine.RectTransform));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
    var customizer = root.AddComponent<GHA.AvatarSuite.UmaAvatarCustomizer>();
    customizer.enabled = false;
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var type = customizer.GetType();
    object Field(string name) => type.GetField(name, flags).GetValue(customizer);
    void Set(string name, object value) => type.GetField(name, flags).SetValue(customizer, value);
    object Call(string name, params object[] args) => type.GetMethod(name, flags).Invoke(customizer, args);
    Set("catalog", catalog);
    Set("_recipe", GHA.AvatarSuite.UmaRecipeStore.Default(catalog));
    Call("BuildSlotGroups");
    var selected = (System.Collections.Generic.Dictionary<string, int>)Field("_slotSelection");
    foreach (var item in catalog.defaultWardrobe) selected[item.wardrobeSlot] = catalog.WardrobeId(item);
    selected["Chest"] = 5;
    selected["Hair"] = 15;
    var dna = (System.Collections.Generic.Dictionary<int, byte>)Field("_dna");
    var colors = (System.Collections.Generic.Dictionary<int, int>)Field("_colors");
    dna[0] = 153; colors[0] = 4;
    Call("BuildRaceSelector", (UnityEngine.RectTransform)root.transform);
    var row = (UnityEngine.RectTransform)root.transform.Find("RaceSelector");
    var next = row.Find("Btn_>").GetComponent<UnityEngine.UI.Button>();
    var previous = row.Find("Btn_<").GetComponent<UnityEngine.UI.Button>();
    next.onClick.Invoke();
    Check((int)Field("_raceIndex") == 1 && selected["Chest"] == 17 && selected["Legs"] == 20 && selected["Feet"] == 22 && selected["Hair"] == 15, "First female switch outfit incorrect.");
    Check(((TMPro.TMP_Text)Field("_raceValueLabel")).text == "Human Female", "Body type label not updated.");
    var groups = (System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>)Field("_slotGroups");
    Check(groups.Values.SelectMany(v => v).All(id => catalog.IsWardrobeCompatible(1, catalog.Wardrobe(id))), "Incompatible UI choices.");
    selected["Chest"] = 18; selected["Legs"] = -1;
    previous.onClick.Invoke();
    Check((int)Field("_raceIndex") == 0 && selected["Chest"] == 5 && selected["Legs"] == 0 && selected["Hair"] == 15, "Male choices were not restored.");
    previous.onClick.Invoke(); // Wrap backwards to female.
    Check((int)Field("_raceIndex") == 1 && selected["Chest"] == 18 && selected["Legs"] == -1, "Female choices/None were not restored.");
    Check(dna[0] == 153 && colors[0] == 4, "Shape/color selections changed.");
    var recipe = (GHA.AvatarSuite.AvatarWireRecipe)Field("_recipe");
    var bytes = new byte[GHA.AvatarSuite.UmaRecipeCodec.MaxBytes];
    int length = GHA.AvatarSuite.UmaRecipeCodec.Encode(recipe, bytes);
    Check(length > 0 && GHA.AvatarSuite.UmaRecipeCodec.TryDecode(bytes, length, out var decoded)
        && decoded.raceId == 1 && decoded.wardrobeIds.Contains(18) && decoded.dna[0].value == 153 && decoded.colors[0].paletteIndex == 4, "Female wire recipe round-trip failed.");
    foreach (float width in new[] { 620f, 800f })
    {
        row.sizeDelta = new UnityEngine.Vector2(width, 56);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        var arrow = (UnityEngine.RectTransform)next.transform;
        var label = ((TMPro.TMP_Text)Field("_raceValueLabel")).rectTransform;
        Check(UnityEngine.Mathf.Abs(arrow.rect.width - 56) < 0.1f && UnityEngine.Mathf.Abs(arrow.rect.height - 56) < 0.1f, "Arrow is not square.");
        Check(label.rect.width >= 260, "Body type label cramped.");
    }
    Check(Field("_previewDca") == null, "Edit Mode test unexpectedly created an avatar.");
    UnityEngine.Object.DestroyImmediate(row.gameObject);
    Set("sliderPrefab", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Packages/com.vertexform3d.gha.uma/Runtime/UI/UMADNASlider.prefab").GetComponent<UnityEngine.UI.Slider>());
    var canvas = root.AddComponent<UnityEngine.Canvas>();
    canvas.renderMode = UnityEngine.RenderMode.WorldSpace;
    ((UnityEngine.RectTransform)root.transform).sizeDelta = new UnityEngine.Vector2(800, 540);
    var body = (UnityEngine.GameObject)Call("BuildDnaTab", (UnityEngine.RectTransform)root.transform, false);
    var cameraObject = new UnityEngine.GameObject("Body type test camera", typeof(UnityEngine.Camera));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, preview);
    var camera = cameraObject.GetComponent<UnityEngine.Camera>();
    camera.scene = preview;
    camera.orthographic = true;
    camera.orthographicSize = 280;
    camera.transform.position = new UnityEngine.Vector3(0, 0, -10);
    camera.backgroundColor = UnityEngine.Color.black;
    camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    canvas.worldCamera = camera;
    UnityEngine.Canvas.ForceUpdateCanvases();
    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((UnityEngine.RectTransform)body.transform);
    UnityEngine.Canvas.ForceUpdateCanvases();
    target = new UnityEngine.RenderTexture(820, 560, 24);
    camera.targetTexture = target;
    camera.Render();
    UnityEngine.RenderTexture.active = target;
    capture = new UnityEngine.Texture2D(820, 560, UnityEngine.TextureFormat.RGB24, false);
    capture.ReadPixels(new UnityEngine.Rect(0, 0, 820, 560), 0, 0);
    capture.Apply();
    string screenshot = "Logs/AvatarStudio-Header-2026-09-07/body-type-verified.png";
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(screenshot));
    System.IO.File.WriteAllBytes(screenshot, UnityEngine.ImageConversion.EncodeToPNG(capture));
    return new { passed = true, races = report, screenshot, tests = "Stable IDs, humanoid/T-pose gate, DNA names, starter outfits, compatibility filtering, next/previous clicks and wraparound, per-type choices/None, shape/colors preserved, wire round-trip, square arrows and label widths. Real Body controls rendered. No PlayerPrefs writes." };
}
finally
{
    UnityEngine.RenderTexture.active = previousTarget;
    // Destroy the preview camera before releasing its render target.
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
    if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
    if (target != null) UnityEngine.Object.DestroyImmediate(target);
    UnityEngine.Object.DestroyImmediate(temporaryCatalog);
    UnityEngine.Object.DestroyImmediate(temporaryRace);
}
