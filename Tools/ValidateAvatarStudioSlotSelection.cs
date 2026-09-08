// Edit Mode validation of actual outfit-subsection buttons, with no avatars or recipe loads.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Run this validation in Edit Mode.");
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
UnityEngine.RenderTexture target = null;
UnityEngine.Texture2D capture = null;
var previous = UnityEngine.RenderTexture.active;
var errors = new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback onLog = (message, stack, kind) => {
    if (kind == UnityEngine.LogType.Error || kind == UnityEngine.LogType.Exception || kind == UnityEngine.LogType.Assert)
        errors.Add(message);
};
UnityEngine.Application.logMessageReceived += onLog;
try
{
    var root = new UnityEngine.GameObject("Slot validation", typeof(UnityEngine.RectTransform), typeof(UnityEngine.Canvas));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
    var canvas = root.GetComponent<UnityEngine.Canvas>();
    canvas.renderMode = UnityEngine.RenderMode.WorldSpace;
    ((UnityEngine.RectTransform)root.transform).sizeDelta = new UnityEngine.Vector2(330, 250);
    var cameraGo = new UnityEngine.GameObject("Validation camera", typeof(UnityEngine.Camera));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, preview);
    var camera = cameraGo.GetComponent<UnityEngine.Camera>();
    camera.scene = preview;
    camera.orthographic = true;
    camera.orthographicSize = 125;
    camera.transform.position = new UnityEngine.Vector3(0, 0, -10);
    camera.backgroundColor = UnityEngine.Color.black;
    camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    canvas.worldCamera = camera;
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var type = typeof(GHA.AvatarSuite.UmaAvatarCustomizer);
    var labels = new [] { "Chest", "Feet", "Hair", "Legs" };
    for (int column = 0; column < 2; column++)
    {
        var rail = new UnityEngine.GameObject("Rail " + column, typeof(UnityEngine.RectTransform));
        rail.transform.SetParent(root.transform, false);
        var customizer = rail.AddComponent<GHA.AvatarSuite.UmaAvatarCustomizer>();
        customizer.enabled = false;
        var buttons = new UnityEngine.UI.Button[4];
        var borders = new UnityEngine.UI.Image[4];
        for (int i = 0; i < labels.Length; i++)
        {
            type.GetMethod("AddSlotButton", flags).Invoke(customizer, new object[] { (UnityEngine.RectTransform)rail.transform, labels[i] });
            var rect = (UnityEngine.RectTransform)rail.transform.Find("Btn_" + labels[i]);
            rect.sizeDelta = new UnityEngine.Vector2(136, 52);
            rect.anchoredPosition = new UnityEngine.Vector2(column == 0 ? -80 : 80, 87 - i * 58);
            buttons[i] = rect.GetComponent<UnityEngine.UI.Button>();
            borders[i] = rect.Find("StateBorder").GetComponent<UnityEngine.UI.Image>();
            if (borders[i].raycastTarget || borders[i].sprite == null || buttons[i].targetGraphic == borders[i])
                throw new System.Exception("Invalid independent border.");
            if (buttons[i].colors.normalColor != UnityEngine.Color.white)
                throw new System.Exception("Button tint would darken the selected fill.");
        }
        buttons[0].onClick.Invoke();
        var trigger = buttons[3].GetComponent<UnityEngine.EventSystems.EventTrigger>();
        var enter = trigger.triggers.Find(e => e.eventID == UnityEngine.EventSystems.EventTriggerType.PointerEnter);
        var exit = trigger.triggers.Find(e => e.eventID == UnityEngine.EventSystems.EventTriggerType.PointerExit);
        enter.callback.Invoke(new UnityEngine.EventSystems.BaseEventData(null));
        if (borders[0].color != new UnityEngine.Color(1, 1, 1, 0.96f)
            || borders[3].color != new UnityEngine.Color(0.54f, 0.4f, 1, 0.95f)
            || buttons[0].GetComponent<UnityEngine.UI.Image>().color != GHA.AvatarFramework.UI.AvatarConfigurationTheme.Accent)
            throw new System.Exception("Active and hover states are not independent.");
        if (column == 1)
        {
            buttons[3].onClick.Invoke();
            exit.callback.Invoke(new UnityEngine.EventSystems.BaseEventData(null));
            // Switching away and back must preserve selection but clear stale hover.
            var tabType = type.GetNestedType("Tab", System.Reflection.BindingFlags.NonPublic);
            type.GetMethod("ShowTab", flags).Invoke(customizer, new object[] { System.Enum.Parse(tabType, "Body") });
            type.GetMethod("ShowTab", flags).Invoke(customizer, new object[] { System.Enum.Parse(tabType, "Clothing") });
            if (borders[3].color != new UnityEngine.Color(1, 1, 1, 0.96f)
                || borders[0].color != GHA.AvatarFramework.UI.AvatarConfigurationTheme.OptionBorder
                || buttons[3].GetComponent<UnityEngine.UI.Image>().color != GHA.AvatarFramework.UI.AvatarConfigurationTheme.Accent
                || buttons[0].GetComponent<UnityEngine.UI.Image>().color == GHA.AvatarFramework.UI.AvatarConfigurationTheme.Accent)
                throw new System.Exception("Selection did not persist or old selection did not reset.");
        }
    }
    UnityEngine.Canvas.ForceUpdateCanvases();
    target = new UnityEngine.RenderTexture(660, 500, 24);
    camera.targetTexture = target;
    camera.Render();
    UnityEngine.RenderTexture.active = target;
    capture = new UnityEngine.Texture2D(660, 500, UnityEngine.TextureFormat.RGB24, false);
    capture.ReadPixels(new UnityEngine.Rect(0, 0, 660, 500), 0, 0);
    capture.Apply();
    string output = "Logs/AvatarStudio-Header-2026-09-07/slot-selection-verified.png";
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
    System.IO.File.WriteAllBytes(output, UnityEngine.ImageConversion.EncodeToPNG(capture));
    if (errors.Count > 0) throw new System.Exception(string.Join("\n", errors));
    return new { passed = true, errors = errors.Count, screenshot = output,
        tests = "Active fill/border, independent hover, click, pointer exit, old selection reset, selection survives category change; actual controls rendered." };
}
finally
{
    UnityEngine.Application.logMessageReceived -= onLog;
    UnityEngine.RenderTexture.active = previous;
    if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
    if (target != null) UnityEngine.Object.DestroyImmediate(target);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
}
