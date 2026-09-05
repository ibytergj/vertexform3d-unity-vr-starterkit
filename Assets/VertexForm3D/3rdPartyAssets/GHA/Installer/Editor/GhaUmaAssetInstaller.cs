#if UNITY_EDITOR && VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using System;
using GHA.AvatarFramework.UI;
using GHA.AvatarSuite;
using UMA;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;
using System.Linq;

namespace GHA.Integration.Editor
{
    /// <summary>Owns only the optional UMA provider contribution to the installed GHA host.</summary>
    public static class GhaUmaAssetInstaller
    {
        private const string PanelPrefabPath =
            "Assets/VertexForm3D/3rdPartyAssets/GHA/Generated/GHAAvatarPanel.prefab";
        private const string HomePrefabPath =
            "Assets/VertexForm3D/Resources/CustomEditor/HomeSceneComponent.prefab";
        private const string CatalogPath =
            "Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset";
        private const string PreviewControllerPath =
            "Assets/UMA/UMA3/Animation/IdleController.controller";
        private const string RuntimeControllerPath =
            "Packages/com.vertexform3d.gha/Runtime/Animations/GHA_Locomotion_v2.controller";
        private const string SliderPrefabPath =
            "Packages/com.vertexform3d.gha.uma/Runtime/UI/UMADNASlider.prefab";
        private const string SkinColorsPath =
            "Assets/UMA/SRP/Colors/SkinColors.asset";
        private const string HairColorsPath =
            "Assets/UMA/SRP/Colors/HairColors.asset";
        private const string EyeColorsPath =
            "Assets/UMA/SRP/Colors/EyeColors.asset";
        private static readonly string[] PlayerPrefabPaths =
        {
            "Assets/VertexForm3D/Resources/NewGenericMRVRPrefab.prefab",
            "Assets/VertexForm3D/Resources/NewGenericMRDesktopPrefab.prefab"
        };
        private static readonly Vector3 PreviewLocalPosition = new(0.9f, -0.50f, 0f);
        private static readonly Vector3 PreviewLocalEuler = new(0f, 180f, 0f);
        private static readonly Vector3 PreviewLocalScale = Vector3.one * 0.40f;

        public static void InstallAssets()
        {
            EnsureHostInstalled();
            GhaIntegrationState state = GhaIntegrationStateStore.Load();
            if (!state.umaInstalled)
            {
                state.umaSnapshots = GhaIntegrationStateStore.CaptureSnapshots(
                    "uma",
                    PlayerPrefabPaths.Concat(
                        new[] { HomePrefabPath, PanelPrefabPath }));
            }
            UmaAvatarCatalog catalog = LoadRequiredAsset<UmaAvatarCatalog>(CatalogPath);
            RuntimeAnimatorController previewController =
                LoadRequiredAsset<RuntimeAnimatorController>(PreviewControllerPath);
            RuntimeAnimatorController runtimeController =
                LoadRequiredAsset<RuntimeAnimatorController>(RuntimeControllerPath);
            Slider sliderPrefab = LoadSliderPrefab();

            EnsureCatalogColorChannels(catalog);
            InstallPanelProvider(catalog, previewController, sliderPrefab);
            InstallHomeProvider(catalog, runtimeController);
            foreach (string playerPrefabPath in PlayerPrefabPaths)
                InstallPlayerProvider(playerPrefabPath, catalog, runtimeController);

            state.umaInstalled = true;
            GhaIntegrationStateStore.RecordInstalledHashes(state.umaSnapshots);
            GhaIntegrationStateStore.Save(state);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "GHA UMA provider assets installed: panel contribution, Home avatar/preview, " +
                "and UmaAvatarBridge on both player prefabs.");
        }

        private static void EnsureCatalogColorChannels(UmaAvatarCatalog catalog)
        {
            bool changed = false;
            changed |= AddMissingColorChannel(
                catalog,
                "Skin",
                "Skin",
                SkinColorsPath,
                7); // Fair
            changed |= AddMissingColorChannel(
                catalog,
                "Hair",
                "Hair",
                HairColorsPath,
                0); // Blonde
            changed |= AddMissingColorChannel(
                catalog,
                "Eyes",
                "Eyes",
                EyeColorsPath,
                0); // Gray

            if (changed)
                EditorUtility.SetDirty(catalog);
        }

        private static bool AddMissingColorChannel(
            UmaAvatarCatalog catalog,
            string label,
            string channelName,
            string palettePath,
            int defaultPaletteIndex)
        {
            if (catalog.colorChannels.Any(
                    channel => channel != null &&
                               string.Equals(
                                   channel.channelName,
                                   channelName,
                                   StringComparison.Ordinal)))
            {
                return false;
            }

            SharedColorTable palette = LoadRequiredAsset<SharedColorTable>(palettePath);
            if (palette.colors == null || palette.colors.Length == 0)
                throw new InvalidOperationException(
                    $"UMA color palette '{palettePath}' has no swatches.");
            if (defaultPaletteIndex < 0 || defaultPaletteIndex >= palette.colors.Length)
                throw new InvalidOperationException(
                    $"Default index {defaultPaletteIndex} is outside UMA color palette " +
                    $"'{palettePath}' ({palette.colors.Length} swatches).");

            catalog.colorChannels.Add(new UmaAvatarCatalog.ColorChannelDef
            {
                label = label,
                channelName = channelName,
                palette = palette,
                defaultPaletteIndex = defaultPaletteIndex,
            });
            return true;
        }

        public static void UninstallAssets()
        {
            GhaIntegrationState state = GhaIntegrationStateStore.Load();
            bool restoredExact = GhaIntegrationStateStore.TryRestoreUnchangedSnapshots(
                state.umaSnapshots,
                "UMA");
            if (!restoredExact)
            {
                RemovePanelProvider();
                RemoveHomeProvider();
                foreach (string playerPrefabPath in PlayerPrefabPaths)
                    RemovePlayerProvider(playerPrefabPath);
            }
            state.umaInstalled = false;
            state.umaSnapshots = null;
            GhaIntegrationStateStore.Save(state);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureHostInstalled()
        {
            GameObject panel = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            if (panel == null ||
                panel.GetComponent<AvatarConfigurationPanel>() == null ||
                panel.GetComponent<VertexFormCore.GHAIntegration.VertexFormStockAvatarConfigurationProvider>() == null)
            {
                throw new InvalidOperationException(
                    "The provider-neutral GHA host panel is not installed. Install the GHA host layer first.");
            }
        }

        private static void InstallPanelProvider(
            UmaAvatarCatalog catalog,
            RuntimeAnimatorController previewController,
            Slider sliderPrefab)
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
                UmaAvatarConfigurationProvider provider =
                    contents.GetComponent<UmaAvatarConfigurationProvider>();
                if (provider == null)
                    provider = contents.AddComponent<UmaAvatarConfigurationProvider>();

                var serialized = new SerializedObject(provider);
                serialized.FindProperty("catalog").objectReferenceValue = catalog;
                serialized.FindProperty("previewAnimationController").objectReferenceValue =
                    previewController;
                serialized.FindProperty("sliderPrefab").objectReferenceValue = sliderPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, PanelPrefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void RemovePanelProvider()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath) == null)
                return;
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
                UmaAvatarConfigurationProvider provider =
                    contents.GetComponent<UmaAvatarConfigurationProvider>();
                if (provider != null)
                    UnityEngine.Object.DestroyImmediate(provider);
                PrefabUtility.SaveAsPrefabAsset(contents, PanelPrefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void InstallHomeProvider(
            UmaAvatarCatalog catalog,
            RuntimeAnimatorController runtimeController)
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(HomePrefabPath);
                Transform homeRig = contents.transform.Find("XR Origin (XR Rig)");
                if (homeRig == null)
                    throw new InvalidOperationException(
                        "HomeSceneComponent has no 'XR Origin (XR Rig)' child.");
                Transform legacyAvatarRoot = homeRig.Find("CustomAvatar");
                if (legacyAvatarRoot == null)
                    throw new InvalidOperationException(
                        "The Home XR rig has no 'CustomAvatar' child.");

                UmaHomeAvatar homeAvatar = homeRig.GetComponent<UmaHomeAvatar>();
                if (homeAvatar == null)
                    homeAvatar = homeRig.gameObject.AddComponent<UmaHomeAvatar>();
                var serialized = new SerializedObject(homeAvatar);
                serialized.FindProperty("catalog").objectReferenceValue = catalog;
                serialized.FindProperty("animationController").objectReferenceValue =
                    runtimeController;
                serialized.FindProperty("legacyAvatarRoot").objectReferenceValue =
                    legacyAvatarRoot.gameObject;
                serialized.FindProperty("firstPersonHiddenLayer").intValue = 7;
                serialized.FindProperty("delayInitialBuildUntilRequested").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                AvatarSelectionManager manager =
                    contents.GetComponentInChildren<AvatarSelectionManager>(true);
                if (manager == null || manager.customAvatarSelectionUI == null)
                    throw new InvalidOperationException(
                        "HomeSceneComponent must expose AvatarSelectionManager.customAvatarSelectionUI.");

                Transform anchor = manager.customAvatarSelectionUI.transform.Find(
                    UmaAvatarConfigurationProvider.PreviewAnchorName);
                if (anchor == null)
                {
                    var anchorObject =
                        new GameObject(UmaAvatarConfigurationProvider.PreviewAnchorName);
                    anchor = anchorObject.transform;
                    anchor.SetParent(manager.customAvatarSelectionUI.transform, false);
                }
                anchor.localPosition = PreviewLocalPosition;
                anchor.localRotation = Quaternion.Euler(PreviewLocalEuler);
                anchor.localScale = PreviewLocalScale;

                PrefabUtility.SaveAsPrefabAsset(contents, HomePrefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void RemoveHomeProvider()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HomePrefabPath) == null)
                return;
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(HomePrefabPath);
                Transform homeRig = contents.transform.Find("XR Origin (XR Rig)");
                UmaHomeAvatar homeAvatar =
                    homeRig != null ? homeRig.GetComponent<UmaHomeAvatar>() : null;
                if (homeAvatar != null)
                    UnityEngine.Object.DestroyImmediate(homeAvatar);

                AvatarSelectionManager manager =
                    contents.GetComponentInChildren<AvatarSelectionManager>(true);
                Transform anchor =
                    manager != null && manager.customAvatarSelectionUI != null
                        ? manager.customAvatarSelectionUI.transform.Find(
                            UmaAvatarConfigurationProvider.PreviewAnchorName)
                        : null;
                if (anchor != null)
                    UnityEngine.Object.DestroyImmediate(anchor.gameObject);
                PrefabUtility.SaveAsPrefabAsset(contents, HomePrefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void InstallPlayerProvider(
            string prefabPath,
            UmaAvatarCatalog catalog,
            RuntimeAnimatorController runtimeController)
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                if (contents.GetComponent<AvatarExtensionSync>() == null)
                    throw new InvalidOperationException(
                        $"'{prefabPath}' is missing the host-owned AvatarExtensionSync component.");

                UmaAvatarBridge bridge = contents.GetComponent<UmaAvatarBridge>();
                if (bridge == null)
                    bridge = contents.AddComponent<UmaAvatarBridge>();
                var serialized = new SerializedObject(bridge);
                serialized.FindProperty("catalog").objectReferenceValue = catalog;
                serialized.FindProperty("animationController").objectReferenceValue =
                    runtimeController;
                serialized.FindProperty("localVrCullLayer").intValue = 7;
                serialized.FindProperty("movingTurnSpeed").floatValue = 720f;
                serialized.FindProperty("idleTurnSpeed").floatValue = 200f;
                serialized.FindProperty("faceMovementThreshold").floatValue = 0.6f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void RemovePlayerProvider(string prefabPath)
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                UmaAvatarBridge bridge = contents.GetComponent<UmaAvatarBridge>();
                if (bridge != null)
                    UnityEngine.Object.DestroyImmediate(bridge);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static Slider LoadSliderPrefab()
        {
            GameObject root = LoadRequiredAsset<GameObject>(SliderPrefabPath);
            return root.GetComponentInChildren<Slider>(true)
                   ?? throw new InvalidOperationException(
                       $"'{SliderPrefabPath}' does not contain a Slider.");
        }

        private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null
                ? asset
                : throw new InvalidOperationException(
                    $"Required asset '{path}' could not be loaded as {typeof(T).Name}.");
        }

    }
}
#endif
