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
        private static readonly Vector3 PreviewLocalPosition = new(-0.35f, -0.50f, 0f);
        private static readonly Vector3 PreviewLocalEuler = new(0f, 180f, 0f);
        private static readonly Vector3 PreviewLocalScale = Vector3.one * 0.40f;
        // UMA's library rebuild starts UnloadUnusedAssets. Unity does not scan local
        // variables on the execution stack; keep installation inputs rooted until saved.
        private static UnityEngine.Object[] installationAssets;

        public static void InstallAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Run the UMA installer after import/compilation, outside Play Mode.");
            if (installationAssets != null)
                throw new InvalidOperationException("UMA provider installation is already running.");
            try
            {
                InstallAssetsCore();
            }
            catch
            {
                GhaIntegrationState failedState = GhaIntegrationStateStore.Load();
                failedState.umaInstalled = false;
                GhaIntegrationStateStore.Save(failedState);
                throw;
            }
            finally
            {
                installationAssets = null;
            }
        }

        private static void InstallAssetsCore()
        {
            EnsureHostInstalled();
            GhaVertexFormAssetInstaller.ValidatePrefabTargets(
                PlayerPrefabPaths.Concat(new[] { HomePrefabPath, PanelPrefabPath }));
            UmaAvatarCatalog catalog = LoadRequiredAsset<UmaAvatarCatalog>(CatalogPath);
            RuntimeAnimatorController previewController =
                LoadRequiredAsset<RuntimeAnimatorController>(PreviewControllerPath);
            RuntimeAnimatorController runtimeController =
                LoadRequiredAsset<RuntimeAnimatorController>(RuntimeControllerPath);
            Slider sliderPrefab = LoadSliderPrefab();
            installationAssets = new UnityEngine.Object[] { catalog, previewController, runtimeController, sliderPrefab.gameObject };
            ValidateCatalogBodyTypes(catalog);
            UMAAssetIndexer indexer = EnsureAssetIndex(
                UMAPathUtility.ProjectIndexerPath,
                UMAPathUtility.ResolveInstallAssetPath("InternalDataStore/InGame/Resources/AssetIndexer.asset"),
                out bool indexCreated);
            if (indexCreated)
                Debug.Log(
                    $"GHA created the UMA asset index from the installed default UMA content at " +
                    $"{AssetDatabase.GetAssetPath(indexer)} ({indexer.SerializedItems.Count} entries).");
            GhaIntegrationState state = GhaIntegrationStateStore.Load();
            if (state.umaSnapshots == null || state.umaSnapshots.Length == 0)
            {
                state.umaSnapshots = GhaIntegrationStateStore.CaptureSnapshots(
                    "uma",
                    PlayerPrefabPaths.Concat(
                        new[] { HomePrefabPath, PanelPrefabPath }));
                GhaIntegrationStateStore.Save(state);
            }

            EnsureCatalogColorChannels(catalog);
            InstallPanelProvider(catalog, previewController, sliderPrefab);
            InstallHomeProvider(catalog, runtimeController);
            foreach (string playerPrefabPath in PlayerPrefabPaths)
                InstallPlayerProvider(playerPrefabPath, catalog, runtimeController);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateInstalledAssets(catalog, previewController, runtimeController, sliderPrefab);
            GhaIntegrationStateStore.RecordInstalledHashes(state.umaSnapshots);
            state.umaInstalled = true;
            GhaIntegrationStateStore.Save(state);
            Debug.Log(
                "GHA UMA provider assets installed: panel contribution, Home avatar/preview, " +
                "and UmaAvatarBridge on both player prefabs.");
        }

        private static void ValidateCatalogBodyTypes(UmaAvatarCatalog catalog)
        {
            if (catalog == null || !catalog.IsHumanoidRace(catalog.defaultRaceId))
                throw new InvalidOperationException("UMA catalog must specify a valid default Humanoid race with a T-pose and base recipe before installation.");
            for (int id = 0; id < catalog.races.Count; id++)
                if (!catalog.IsHumanoidRace(id))
                    throw new InvalidOperationException($"UMA catalog race ID {id} needs a Humanoid race, T-pose and base recipe. Correct the catalog/import before installation.");
        }

        private static void ValidateInstalledAssets(UmaAvatarCatalog catalog,
            RuntimeAnimatorController previewController, RuntimeAnimatorController runtimeController, Slider sliderPrefab)
        {
            ValidateCatalogBodyTypes(catalog);
            GameObject panel = LoadRequiredAsset<GameObject>(PanelPrefabPath);
            var provider = panel.GetComponent<UmaAvatarConfigurationProvider>();
            RequireSavedReference(provider, "catalog", catalog, PanelPrefabPath);
            RequireSavedReference(provider, "previewAnimationController", previewController, PanelPrefabPath);
            RequireSavedReference(provider, "sliderPrefab", sliderPrefab, PanelPrefabPath);
            GameObject home = LoadRequiredAsset<GameObject>(HomePrefabPath);
            var homeAvatar = home.GetComponentInChildren<UmaHomeAvatar>(true);
            RequireSavedReference(homeAvatar, "catalog", catalog, HomePrefabPath);
            RequireSavedReference(homeAvatar, "animationController", runtimeController, HomePrefabPath);
            foreach (string path in PlayerPrefabPaths)
            {
                var bridge = LoadRequiredAsset<GameObject>(path).GetComponent<UmaAvatarBridge>();
                RequireSavedReference(bridge, "catalog", catalog, path);
                RequireSavedReference(bridge, "animationController", runtimeController, path);
            }
        }

        private static void RequireSavedReference(Component component, string propertyName,
            UnityEngine.Object expected, string path)
        {
            if (component == null || expected == null ||
                new SerializedObject(component).FindProperty(propertyName)?.objectReferenceValue != expected)
                throw new InvalidOperationException($"UMA installation did not retain '{propertyName}' on '{path}'. Installation is incomplete; do not enter Play Mode.");
        }

        // Follow UMA's project-index / install-index precedence without initializing its
        // singleton (which also creates a generator in the open scene). Existing data is
        // never copied, replaced or rebuilt by the provider installer. First-time population
        // uses UMA's rebuild API, restricted to the separately installed UMA content tree.
        private static UMAAssetIndexer EnsureAssetIndex(
            string projectIndexPath, string installIndexPath, out bool created)
        {
            created = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException(
                    "Wait until Unity is in Edit Mode and has finished importing and compiling before installing UMA.");

            foreach (string path in new[] { projectIndexPath, installIndexPath })
            {
                UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
                if (existing is UMAAssetIndexer indexer)
                    return indexer;
                if (existing != null || System.IO.File.Exists(path) || System.IO.Directory.Exists(path))
                    throw new InvalidOperationException(
                        $"Cannot initialize UMA: {path} exists but is not a loadable UMAAssetIndexer. " +
                        "Resolve the asset/import error before retrying; nothing has been overwritten.");
            }

            UMAPathUtility.EnsureAssetFolder(System.IO.Path.GetDirectoryName(projectIndexPath));
            UMAAssetIndexer newIndexer = ScriptableObject.CreateInstance<UMAAssetIndexer>();
            try
            {
                newIndexer.name = System.IO.Path.GetFileNameWithoutExtension(projectIndexPath);
                newIndexer.BuildStringTypes();
                newIndexer.DoInitialDictionaryLoad();
                PopulateDefaultUmaIndex(newIndexer);
                AssetDatabase.CreateAsset(newIndexer, projectIndexPath);
                if (!AssetDatabase.Contains(newIndexer))
                    throw new InvalidOperationException($"Unity could not create the UMA asset index at {projectIndexPath}.");
                AssetDatabase.SaveAssetIfDirty(newIndexer);
                created = true;
                return newIndexer;
            }
            finally
            {
                if (!AssetDatabase.Contains(newIndexer))
                    UnityEngine.Object.DestroyImmediate(newIndexer);
            }
        }

        private static void PopulateDefaultUmaIndex(UMAAssetIndexer indexer)
        {
            if (!AssetDatabase.IsValidFolder(UMAPathUtility.LegacyInstallRoot))
                throw new InvalidOperationException("Install the supported UMA content into Assets/UMA before installing its GHA provider.");

            // Only the initial scan is scoped; do not impose permanent filters on future
            // content authoring. The trailing slash excludes similarly named sibling folders.
            foreach (Type type in indexer.GetTypes())
                indexer.TypeFolderSearch[type.Name] =
                    new System.Collections.Generic.List<string> { UMAPathUtility.LegacyInstallRoot + "/" };
            try
            {
                indexer.RebuildLibrary();
                foreach (string raceName in new[] { "Human Male 3.0", "Human Female 3.0" })
                    if (indexer.GetAssetItem<RaceData>(raceName)?.Item == null)
                        throw new InvalidOperationException(
                            $"Default UMA content is incomplete: {raceName} was not indexed. " +
                            "Check the supported UMA import and its errors, then retry installation.");
                if (indexer.GetAssetItems<SlotDataAsset>().Count == 0 ||
                    indexer.GetAssetItems<OverlayDataAsset>().Count == 0 ||
                    indexer.GetAssetItems<UMA.CharacterSystem.UMAWardrobeRecipe>().Count == 0)
                    throw new InvalidOperationException("Default UMA content is incomplete: slots, overlays or wardrobe recipes are missing.");
            }
            finally
            {
                indexer.TypeFolderSearch.Clear();
                EditorUtility.ClearProgressBar();
            }
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
                GhaVertexFormAssetInstaller.SavePrefabChecked(contents, PanelPrefabPath);
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
                GhaVertexFormAssetInstaller.SavePrefabChecked(contents, PanelPrefabPath);
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
                    anchor.localPosition = PreviewLocalPosition;
                    anchor.localRotation = Quaternion.Euler(PreviewLocalEuler);
                    anchor.localScale = PreviewLocalScale;
                }
                // Existing anchors retain their authored framing on repeated installation.

                GhaVertexFormAssetInstaller.SavePrefabChecked(contents, HomePrefabPath);
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
                GhaVertexFormAssetInstaller.SavePrefabChecked(contents, HomePrefabPath);
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
                GhaVertexFormAssetInstaller.SavePrefabChecked(contents, prefabPath);
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
                GhaVertexFormAssetInstaller.SavePrefabChecked(contents, prefabPath);
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
