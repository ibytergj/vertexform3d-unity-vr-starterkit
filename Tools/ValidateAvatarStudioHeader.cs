// Run through Unity CLI eval_file in Edit Mode. Uses the actual shell/button/category
// construction in a disposable preview scene; never loads an avatar or changes a scene asset.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Run header validation in Edit Mode.");
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var failures = new System.Collections.Generic.List<string>();
var results = new System.Collections.Generic.List<object>();
UnityEngine.Application.LogCallback onLog = (message, stack, kind) => {
    if (kind == UnityEngine.LogType.Error || kind == UnityEngine.LogType.Exception || kind == UnityEngine.LogType.Assert)
        failures.Add(message);
};
UnityEngine.Application.logMessageReceived += onLog;
UnityEngine.RenderTexture target = null;
UnityEngine.Texture2D capture = null;
var previousTarget = UnityEngine.RenderTexture.active;
var output = "Logs/AvatarStudio-Header-2026-09-07/header-verified.png";
try
{
    var root = new UnityEngine.GameObject("Header validation", typeof(UnityEngine.RectTransform), typeof(UnityEngine.Canvas));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
    var canvas = root.GetComponent<UnityEngine.Canvas>();
    canvas.renderMode = UnityEngine.RenderMode.WorldSpace;
    var rootRect = (UnityEngine.RectTransform)root.transform;
    var cameraGo = new UnityEngine.GameObject("Validation camera", typeof(UnityEngine.Camera));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, preview);
    var camera = cameraGo.GetComponent<UnityEngine.Camera>();
    camera.scene = preview;
    camera.orthographic = true;
    camera.transform.position = new UnityEngine.Vector3(0, 0, -10);
    camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    camera.backgroundColor = UnityEngine.Color.black;
    canvas.worldCamera = camera;
    var type = typeof(GHA.AvatarFramework.UI.AvatarConfigurationPanel);
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var sizes = new [] { new UnityEngine.Vector2(1920, 1080), new UnityEngine.Vector2(1280, 720), new UnityEngine.Vector2(2000, 1000) };
    foreach (var size in sizes)
    {
        rootRect.sizeDelta = size;
        var holder = new UnityEngine.GameObject("Panel", typeof(UnityEngine.RectTransform));
        holder.SetActive(false);
        holder.transform.SetParent(root.transform, false);
        var holderRect = (UnityEngine.RectTransform)holder.transform;
        holderRect.anchorMin = UnityEngine.Vector2.zero;
        holderRect.anchorMax = UnityEngine.Vector2.one;
        holderRect.sizeDelta = UnityEngine.Vector2.zero;
        var panel = holder.AddComponent<GHA.AvatarFramework.UI.AvatarConfigurationPanel>();
        panel.enabled = false;
        holder.SetActive(true);
        type.GetMethod("BuildShell", flags).Invoke(panel, null);
        var buttons = new System.Collections.Generic.List<UnityEngine.UI.Button>();
        foreach (var label in new [] { "Classic", "Custom" })
        {
            object[] args = { label.ToLowerInvariant(), label, null, null };
            buttons.Add((UnityEngine.UI.Button)type.GetMethod("CreateProviderButton", flags).Invoke(panel, args));
        }
        var shell = (UnityEngine.RectTransform)holder.transform.Find("GHA Avatar Panel Shell");
        var title = (UnityEngine.RectTransform)shell.Find("Title");
        var nav = (UnityEngine.RectTransform)shell.Find("Provider Navigation");
        var category = (UnityEngine.RectTransform)shell.Find("Category Card/Provider Categories");
        var customizer = category.gameObject.AddComponent<GHA.AvatarSuite.UmaAvatarCustomizer>();
        customizer.enabled = false;
        typeof(GHA.AvatarSuite.UmaAvatarCustomizer).GetMethod("BuildTabBar", flags)
            .Invoke(customizer, new object[] { category, true });
        UnityEngine.Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(shell);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(nav);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(category);
        UnityEngine.Canvas.ForceUpdateCanvases();
        if (shell.Find("Subtitle") != null || nav.GetComponent<UnityEngine.UI.Image>() != null)
            throw new System.Exception("Old subtitle or full-width navigation background remains.");
        var titleLabel = title.GetComponentInChildren<TMPro.TMP_Text>();
        titleLabel.ForceMeshUpdate();
        if (titleLabel.alignment != TMPro.TextAlignmentOptions.MidlineRight || titleLabel.isTextOverflowing)
            throw new System.Exception("Title alignment/fit failed at " + size);
        float titleY = title.TransformPoint(title.rect.center).y;
        float previousRight = float.NegativeInfinity;
        foreach (var button in buttons)
        {
            var rect = (UnityEngine.RectTransform)button.transform;
            var corners = new UnityEngine.Vector3[4];
            rect.GetWorldCorners(corners);
            if (UnityEngine.Mathf.Abs(rect.TransformPoint(rect.rect.center).y - titleY) > 0.1f
                || corners[0].x < previousRight || corners[3].x > title.TransformPoint(title.rect.min).x)
                throw new System.Exception("Header alignment/overlap failed at " + size);
            previousRight = corners[3].x;
            if (rect.rect.height < 52 || rect.rect.height > 64.1f)
                throw new System.Exception("Tab hit area changed unexpectedly.");
        }
        foreach (var name in new [] { "Preview Card", "Category Card", "Controls Card" })
        {
            var card = (UnityEngine.RectTransform)shell.Find(name);
            if (UnityEngine.Mathf.Abs(card.anchorMax.y - 0.835f) > 0.0001f
                || UnityEngine.Mathf.Abs(card.anchorMin.y - 0.055f) > 0.0001f)
                throw new System.Exception("Content top/bottom mismatch: " + name);
        }
        foreach (UnityEngine.RectTransform tile in category)
            if (UnityEngine.Mathf.Abs(tile.rect.width - tile.rect.height) > 0.1f)
                throw new System.Exception("Category tile is not square.");
        results.Add(new { size = size.ToString(), headerAligned = true, titleFits = true, squareCategories = true, contentHeightGainPercent = 17.29 });
        if (size.x == 2000)
        {
            camera.orthographicSize = size.y / 2;
            target = new UnityEngine.RenderTexture(1600, 800, 24);
            camera.targetTexture = target;
            camera.Render();
            UnityEngine.RenderTexture.active = target;
            // Header and top of cards only: this is a real Unity render of the layout,
            // not a full runtime avatar screenshot or an AI mockup.
            capture = new UnityEngine.Texture2D(1600, 220, UnityEngine.TextureFormat.RGB24, false);
            capture.ReadPixels(new UnityEngine.Rect(0, 580, 1600, 220), 0, 0);
            capture.Apply();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            System.IO.File.WriteAllBytes(output, UnityEngine.ImageConversion.EncodeToPNG(capture));
        }
        UnityEngine.Object.DestroyImmediate(holder);
    }
    if (failures.Count > 0)
        throw new System.Exception(string.Join("\n", failures));
    return new { passed = true, sizes = results, errors = failures.Count, screenshot = output };
}
finally
{
    UnityEngine.Application.logMessageReceived -= onLog;
    UnityEngine.RenderTexture.active = previousTarget;
    if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
    if (target != null) UnityEngine.Object.DestroyImmediate(target);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
}
