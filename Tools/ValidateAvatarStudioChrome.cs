// Isolated Edit Mode regression test of the actual category construction/event handlers.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Run this validation in Edit Mode.");
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
UnityEngine.RenderTexture target = null;
UnityEngine.Texture2D capture = null;
var previousTarget = UnityEngine.RenderTexture.active;
var failures = new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback onLog = (message, stack, type) => {
    if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Assert || type == UnityEngine.LogType.Exception)
        failures.Add(message);
};
UnityEngine.Application.logMessageReceived += onLog;
try
{
    var root = new UnityEngine.GameObject("UI validation (temporary)", typeof(UnityEngine.RectTransform), typeof(UnityEngine.Canvas));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
    var canvas = root.GetComponent<UnityEngine.Canvas>();
    canvas.renderMode = UnityEngine.RenderMode.WorldSpace;
    var canvasRect = (UnityEngine.RectTransform)root.transform;
    canvasRect.sizeDelta = new UnityEngine.Vector2(340, 470);
    var cameraGo = new UnityEngine.GameObject("UI validation camera", typeof(UnityEngine.Camera));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, preview);
    var camera = cameraGo.GetComponent<UnityEngine.Camera>();
    camera.scene = preview;
    camera.orthographic = true;
    camera.orthographicSize = 235;
    camera.transform.position = new UnityEngine.Vector3(0, 0, -10);
    camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    camera.backgroundColor = new UnityEngine.Color(0.035f, 0.043f, 0.067f, 1);
    canvas.worldCamera = camera;
    var type = typeof(GHA.AvatarSuite.UmaAvatarCustomizer);
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var tabType = type.GetNestedType("Tab", System.Reflection.BindingFlags.NonPublic);
    var add = type.GetMethod("AddTabButton", flags);
    var show = type.GetMethod("ShowTab", flags);
    var labels = new[] { "Body", "Face", "Colors", "Outfits" };
    var enumNames = new[] { "Body", "Face", "Color", "Clothing" };
    for (int column = 0; column < 2; column++)
    {
        var holder = new UnityEngine.GameObject("Test rail " + column, typeof(UnityEngine.RectTransform));
        holder.transform.SetParent(root.transform, false);
        var customizer = holder.AddComponent<GHA.AvatarSuite.UmaAvatarCustomizer>();
        customizer.enabled = false; // Never start avatars, recipes, networking, or a scene.
        var buttons = new UnityEngine.UI.Button[4];
        var borders = new UnityEngine.UI.Image[4];
        for (int i = 0; i < 4; i++)
        {
            add.Invoke(customizer, new object[] { (UnityEngine.RectTransform)holder.transform,
                System.Enum.Parse(tabType, enumNames[i]), labels[i], true });
            var rt = (UnityEngine.RectTransform)holder.transform.Find("Tab_" + labels[i]);
            rt.sizeDelta = new UnityEngine.Vector2(96, 96);
            rt.anchoredPosition = new UnityEngine.Vector2(column == 0 ? -70 : 70, 165 - i * 108);
            buttons[i] = rt.GetComponent<UnityEngine.UI.Button>();
            borders[i] = rt.Find("StateBorder").GetComponent<UnityEngine.UI.Image>();
            if (borders[i].raycastTarget || borders[i].sprite == null || borders[i].transform.parent != rt)
                throw new System.Exception("Invalid independent border on " + labels[i]);
            if (buttons[i].targetGraphic == borders[i])
                throw new System.Exception("Button still tints its border.");
        }
        show.Invoke(customizer, new object[] { System.Enum.Parse(tabType, "Body") });
        var trigger = buttons[1].GetComponent<UnityEngine.EventSystems.EventTrigger>();
        var enter = trigger.triggers.Find(x => x.eventID == UnityEngine.EventSystems.EventTriggerType.PointerEnter);
        var exit = trigger.triggers.Find(x => x.eventID == UnityEngine.EventSystems.EventTriggerType.PointerExit);
        enter.callback.Invoke(new UnityEngine.EventSystems.BaseEventData(null));
        if (borders[0].color.r < 0.95f || borders[1].color != new UnityEngine.Color(0.54f, 0.4f, 1, 0.95f))
            throw new System.Exception("Selected or hover state incorrect.");
        if (column == 1)
        {
            exit.callback.Invoke(new UnityEngine.EventSystems.BaseEventData(null));
            buttons[3].onClick.Invoke();
            if (borders[3].color.r < 0.95f || borders[0].color != GHA.AvatarFramework.UI.AvatarConfigurationTheme.OptionBorder
                || borders[1].color != GHA.AvatarFramework.UI.AvatarConfigurationTheme.OptionBorder)
                throw new System.Exception("Selection or hover did not reset.");
        }
        foreach (var border in borders)
            if (border.canvasRenderer.GetColor() != UnityEngine.Color.white)
                throw new System.Exception("Border renderer is tinted.");
    }
    UnityEngine.Canvas.ForceUpdateCanvases();
    target = new UnityEngine.RenderTexture(680, 940, 24);
    camera.targetTexture = target;
    camera.Render();
    UnityEngine.RenderTexture.active = target;
    capture = new UnityEngine.Texture2D(680, 940, UnityEngine.TextureFormat.RGB24, false);
    capture.ReadPixels(new UnityEngine.Rect(0, 0, 680, 940), 0, 0);
    capture.Apply();
    var output = "Logs/MetaOpenXR-Fix-2026-09-04/category-states-verified.png";
    System.IO.File.WriteAllBytes(output, UnityEngine.ImageConversion.EncodeToPNG(capture));
    if (failures.Count > 0) throw new System.Exception(string.Join("\n", failures));
    return new { passed = true, errors = failures.Count, screenshot = output,
        tests = "Packaged sprites load; borders independent and non-raycastable; hover, exit, click, old selection reset; actual controls rendered." };
}
finally
{
    UnityEngine.Application.logMessageReceived -= onLog;
    UnityEngine.RenderTexture.active = previousTarget;
    if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
    if (target != null) UnityEngine.Object.DestroyImmediate(target);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
}
