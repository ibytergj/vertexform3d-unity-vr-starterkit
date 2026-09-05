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
            GhaIntegrationState state = GhaIntegrationStateStore.Load();
            if (!state.hostInstalled)
            {
                state.hostSnapshots = GhaIntegrationStateStore.CaptureSnapshots(
                    "host",
                    PlayerPrefabPaths.Concat(new[] { HomePrefabPath }));
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
            state.hostInstalled = true;
            GhaIntegrationStateStore.RecordInstalledHashes(state.hostSnapshots);
            GhaIntegrationStateStore.Save(state);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
                    existing = PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
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
                    existing = PrefabUtility.SaveAsPrefabAsset(contents, PanelPrefabPath);
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
                PrefabUtility.SaveAsPrefabAsset(contents, HomePrefabPath);
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
                PrefabUtility.SaveAsPrefabAsset(contents, HomePrefabPath);
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
                if (contents.GetComponent<T>() == null)
                    contents.AddComponent<T>();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
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
                if (component != null)
                    UnityEngine.Object.DestroyImmediate(component);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
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
