// Run once with Unity CLI eval_file in Edit Mode. Produces persistent, build-included sprites.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Create UI assets only in Edit Mode.");
var folder = "Packages/com.vertexform3d.gha/Runtime/Resources/GHA/AvatarStudio/Chrome";
var parts = folder.Split('/');
var current = parts[0];
for (int i = 1; i < parts.Length; i++)
{
    var next = current + "/" + parts[i];
    if (!UnityEditor.AssetDatabase.IsValidFolder(next))
        UnityEditor.AssetDatabase.CreateFolder(current, parts[i]);
    current = next;
}
foreach (var name in new[] { "RoundedPanel", "RoundedBorder" })
{
    var path = folder + "/" + name + ".png";
    if (System.IO.File.Exists(path))
        throw new System.InvalidOperationException("Asset already exists: " + path);
    var texture = new UnityEngine.Texture2D(64, 64, UnityEngine.TextureFormat.RGBA32, false);
    try
    {
        var pixels = new UnityEngine.Color[64 * 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            // Signed distance to a rounded rectangle, inset one pixel, with a ten-pixel radius.
            float qx = UnityEngine.Mathf.Abs(x + 0.5f - 32f) - 21f;
            float qy = UnityEngine.Mathf.Abs(y + 0.5f - 32f) - 21f;
            float distance = new UnityEngine.Vector2(UnityEngine.Mathf.Max(qx, 0), UnityEngine.Mathf.Max(qy, 0)).magnitude
                + UnityEngine.Mathf.Min(UnityEngine.Mathf.Max(qx, qy), 0) - 10f;
            float alpha = UnityEngine.Mathf.Clamp01(0.5f - distance);
            if (name == "RoundedBorder")
                alpha *= UnityEngine.Mathf.Clamp01(distance + 3.5f);
            pixels[y * 64 + x] = new UnityEngine.Color(1, 1, 1, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        System.IO.File.WriteAllBytes(path, UnityEngine.ImageConversion.EncodeToPNG(texture));
    }
    finally { UnityEngine.Object.DestroyImmediate(texture); }
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    importer.spritePixelsPerUnit = 100;
    importer.spriteBorder = new UnityEngine.Vector4(12, 12, 12, 12);
    importer.mipmapEnabled = false;
    importer.alphaIsTransparency = true;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    importer.filterMode = UnityEngine.FilterMode.Bilinear;
    importer.SaveAndReimport();
    if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path) == null)
        throw new System.InvalidOperationException("Sprite import failed: " + path);
}
return "Created and imported both persistent rounded UI sprites.";
