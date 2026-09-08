#if UNITY_EDITOR && VERTEXFORM_GHA_HOST
using System;
using System.Linq;
using GHA.AvatarFramework.UI;
using UnityEditor;
using UnityEngine;
using VertexFormCore;
using VertexFormCore.GHAIntegration;

namespace GHA.Integration.Editor
{
    /// <summary>Owns only provider-neutral VertexForm prefab and UI integration.</summary>
    public static class GhaVertexFormAssetInstaller
    {
        private const string GeneratedFolder =
            "Assets/VertexForm3D/3rdPartyAssets/GHA/Generated";
        private const string PanelPrefabPath = GeneratedFolder + "/GHAAvatarPanel.prefab";
        private const string PanelInstanceName = "GHA Avatar Configuration";
        private const string HomePrefabPath =
            "Assets/VertexForm3D/Resources/CustomEditor/HomeSceneComponent.prefab";
        private static readonly string[] PlayerPrefabPaths =
        {
            "Assets/VertexForm3D/Resources/NewGenericMRVRPrefab.prefab",
            "Assets/VertexForm3D/Resources/NewGenericMRDesktopPrefab.prefab"
        };

        public static void InstallAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait for Unity to finish importing/compiling and exit Play Mode before installing GHA.");

            try
            {
                InstallAssetsCore();
            }
            catch
            {
                // A previous run may have reported success despite a failed prefab save.
                // Retain recovery snapshots, but never leave a failed attempt marked installed.
                GhaIntegrationState failedState = GhaIntegrationStateStore.Load();
                failedState.hostInstalled = false;
                GhaIntegrationStateStore.Save(failedState);
                throw;
            }
        }

        private static void InstallAssetsCore()
        {
            ValidatePrefabTargets(PlayerPrefabPaths.Concat(new[] { HomePrefabPath }));
            if (System.IO.File.Exists(PanelPrefabPath))
                ValidatePrefabTargets(new[] { PanelPrefabPath });
            ValidateHomeStation(AssetDatabase.LoadAssetAtPath<GameObject>(HomePrefabPath));

            GhaIntegrationState state = GhaIntegrationStateStore.Load();
            if (state.hostSnapshots == null || state.hostSnapshots.Length == 0)
            {
                state.hostSnapshots = GhaIntegrationStateStore.CaptureSnapshots(
                    "host",
                    PlayerPrefabPaths.Concat(new[] { HomePrefabPath }));
                // Persist before the first asset edit, so a retry cannot replace the originals.
                GhaIntegrationStateStore.Save(state);
            }
            GhaFileSnapshot[] hostSnapshots = state.hostSnapshots;
            EnsureFolder(GeneratedFolder);
            GameObject panelPrefab = EnsureHostPanelPrefab();
            foreach (string playerPrefabPath in PlayerPrefabPaths)
                EnsureComponent<AvatarExtensionSync>(playerPrefabPath);
            InstallHomePanel(panelPrefab);

            // InstallHomePanel saves the pre-install Home values. Reload them before
            // saving layer metadata so this stale state instance cannot overwrite them.
            state = GhaIntegrationStateStore.Load();
            state.hostSnapshots = hostSnapshots;
            GhaIntegrationStateStore.RecordInstalledHashes(state.hostSnapshots);
            AssetDatabase.Refresh();
            state.hostInstalled = true;
            GhaIntegrationStateStore.Save(state);
            Debug.Log(
                "GHA host assets installed: provider-neutral panel, Home Change Avatar integration, " +
                "and AvatarExtensionSync on both player prefabs.");
        }

        public static void UninstallAssets()
        {
            GhaIntegrationState state = GhaIntegrationStateStore.Load();
            bool restoredExact = GhaIntegrationStateStore.TryRestoreUnchangedSnapshots(
                state.hostSnapshots,
                "host");
            if (!restoredExact)
            {
                UninstallHomePanel(state);
                foreach (string playerPrefabPath in PlayerPrefabPaths)
                    RemoveComponent<AvatarExtensionSync>(playerPrefabPath);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath) != null)
                AssetDatabase.DeleteAsset(PanelPrefabPath);

            state.hostInstalled = false;
            state.capturedHomeState = false;
            state.hostSnapshots = null;
            GhaIntegrationStateStore.Save(state);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject EnsureHostPanelPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            if (existing == null)
            {
                var root = new GameObject(
                    "GHAAvatarPanel",
                    typeof(RectTransform),
                    typeof(AvatarConfigurationPanel),
                    typeof(VertexFormStockAvatarConfigurationProvider));
                try
                {
                    Stretch((RectTransform)root.transform);
                    existing = SavePrefabChecked(root, PanelPrefabPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            else
            {
                GameObject contents = null;
                try
                {
                    contents = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
                    if (contents.GetComponent<AvatarConfigurationPanel>() == null)
                        contents.AddComponent<AvatarConfigurationPanel>();
                    if (contents.GetComponent<VertexFormStockAvatarConfigurationProvider>() == null)
                        contents.AddComponent<VertexFormStockAvatarConfigurationProvider>();
                    RectTransform rect = contents.GetComponent<RectTransform>();
                    if (rect == null)
                        throw new InvalidOperationException(
                            $"'{PanelPrefabPath}' must have a RectTransform root.");
                    Stretch(rect);
                    existing = SavePrefabChecked(contents, PanelPrefabPath);
                }
                finally
                {
                    if (contents != null)
                        PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            return existing ?? throw new InvalidOperationException(
                $"Unity did not create or update '{PanelPrefabPath}'.");
        }

        private static void InstallHomePanel(GameObject panelPrefab)
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(HomePrefabPath);
                AvatarSelectionManager manager =
                    contents.GetComponentInChildren<AvatarSelectionManager>(true);
                if (manager == null || manager.customAvatarSelectionUI == null)
                    throw new InvalidOperationException(
                        "HomeSceneComponent must expose AvatarSelectionManager.customAvatarSelectionUI.");

                Canvas stationCanvas =
                    manager.customAvatarSelectionUI.GetComponentInChildren<Canvas>(true);
                if (stationCanvas == null)
                    throw new InvalidOperationException("The Change Avatar station has no Canvas.");

                Transform stockPreview =
                    manager.customAvatarSelectionUI.transform.Find("AvatarHolder");
                if (stockPreview == null)
                    throw new InvalidOperationException(
                        "The Change Avatar station has no AvatarHolder.");

                GhaIntegrationState state = GhaIntegrationStateStore.Load();
                if (!state.capturedHomeState)
                {
                    bool adoptingLegacyInstall =
                        Mathf.Approximately(stockPreview.localPosition.x, 1f) &&
                        manager.previousButton != null &&
                        !manager.previousButton.gameObject.activeSelf &&
                        manager.nextButton != null &&
                        !manager.nextButton.gameObject.activeSelf &&
                        stationCanvas.transform.Find(PanelInstanceName) != null;
                    state.stockPreviewLocalPosition = stockPreview.localPosition;
                    if (adoptingLegacyInstall)
                        state.stockPreviewLocalPosition.x = 0f;
                    state.previousButtonActive = adoptingLegacyInstall ||
                        (manager.previousButton != null &&
                         manager.previousButton.gameObject.activeSelf);
                    state.nextButtonActive = adoptingLegacyInstall ||
                        (manager.nextButton != null &&
                         manager.nextButton.gameObject.activeSelf);
                    state.capturedHomeState = true;
                    GhaIntegrationStateStore.Save(state);
                }

                Vector3 previewPosition = stockPreview.localPosition;
                previewPosition.x = 1f;
                stockPreview.localPosition = previewPosition;
                if (manager.previousButton != null)
                    manager.previousButton.gameObject.SetActive(false);
                if (manager.nextButton != null)
                    manager.nextButton.gameObject.SetActive(false);

                Transform existing = stationCanvas.transform.Find(PanelInstanceName);
                GameObject panelInstance = existing != null ? existing.gameObject : null;
                if (panelInstance == null)
                {
                    panelInstance =
                        PrefabUtility.InstantiatePrefab(panelPrefab, stationCanvas.transform)
                        as GameObject;
                    if (panelInstance == null)
                        throw new InvalidOperationException(
                            $"Unity did not instantiate '{PanelPrefabPath}'.");
                    panelInstance.name = PanelInstanceName;
                }

                RectTransform rect = panelInstance.GetComponent<RectTransform>()
                    ?? throw new InvalidOperationException(
                        $"'{PanelInstanceName}' has no RectTransform.");
                Stretch(rect);
                rect.SetSiblingIndex(0);
                panelInstance.SetActive(true);
                EditorUtility.SetDirty(manager);
                SavePrefabChecked(contents, HomePrefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void UninstallHomePanel(GhaIntegrationState state)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HomePrefabPath) == null)
                return;

            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(HomePrefabPath);
                AvatarSelectionManager manager =
                    contents.GetComponentInChildren<AvatarSelectionManager>(true);
                if (manager == null || manager.customAvatarSelectionUI == null)
                    return;

                Canvas stationCanvas =
                    manager.customAvatarSelectionUI.GetComponentInChildren<Canvas>(true);
                Transform installedPanel =
                    stationCanvas != null
                        ? stationCanvas.transform.Find(PanelInstanceName)
                        : null;
                if (installedPanel != null)
                    UnityEngine.Object.DestroyImmediate(installedPanel.gameObject);

                if (state.capturedHomeState)
                {
                    Transform stockPreview =
                        manager.customAvatarSelectionUI.transform.Find("AvatarHolder");
                    if (stockPreview != null &&
                        Mathf.Approximately(stockPreview.localPosition.x, 1f))
                    {
                        stockPreview.localPosition = state.stockPreviewLocalPosition;
                    }

                    if (manager.previousButton != null &&
                        !manager.previousButton.gameObject.activeSelf)
                    {
                        manager.previousButton.gameObject.SetActive(state.previousButtonActive);
                    }
                    if (manager.nextButton != null &&
                        !manager.nextButton.gameObject.activeSelf)
                    {
                        manager.nextButton.gameObject.SetActive(state.nextButtonActive);
                    }
                }

                EditorUtility.SetDirty(manager);
                SavePrefabChecked(contents, HomePrefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void EnsureComponent<T>(string prefabPath) where T : Component
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                if (contents.GetComponent<T>() != null)
                    return;
                contents.AddComponent<T>();
                SavePrefabChecked(contents, prefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void RemoveComponent<T>(string prefabPath) where T : Component
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                T component = contents.GetComponent<T>();
                if (component == null)
                    return;
                UnityEngine.Object.DestroyImmediate(component);
                SavePrefabChecked(contents, prefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        internal static void ValidatePrefabTargets(System.Collections.Generic.IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
                    throw new InvalidOperationException($"GHA cannot load required prefab '{path}'. Repair it before retrying installation.");
                if ((System.IO.File.GetAttributes(path) & System.IO.FileAttributes.ReadOnly) != 0)
                    throw new InvalidOperationException($"GHA cannot save read-only prefab '{path}'. Make it writable before retrying installation.");
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                {
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                    if (missing != 0 || PrefabUtility.IsPrefabAssetMissing(child.gameObject))
                        throw new InvalidOperationException(
                            $"GHA cannot install into '{path}': '{child.name}' has {missing} missing script(s) or a missing nested prefab. " +
                            "Repair the asset first, then rerun Install GHA Host Layer. No prefab changes were made by this preflight.");
                }
            }
        }

        internal static GameObject SavePrefabChecked(GameObject contents, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, path, out bool success);
            if (!success)
                throw new InvalidOperationException(
                    $"Unity failed to save GHA prefab '{path}'. Installation did not complete. " +
                    "Resolve the preceding Console error and rerun the installer; do not uninstall first.");
            return saved;
        }

        private static void ValidateHomeStation(GameObject contents)
        {
            AvatarSelectionManager manager = contents.GetComponentInChildren<AvatarSelectionManager>(true);
            if (manager == null || manager.customAvatarSelectionUI == null)
                throw new InvalidOperationException("HomeSceneComponent must expose AvatarSelectionManager.customAvatarSelectionUI.");
            if (manager.customAvatarSelectionUI.GetComponentInChildren<Canvas>(true) == null)
                throw new InvalidOperationException("The Change Avatar station has no Canvas.");
            if (manager.customAvatarSelectionUI.transform.Find("AvatarHolder") == null)
                throw new InvalidOperationException("The Change Avatar station has no AvatarHolder.");
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }
    }
}
#endif
