// Unity CLI run_script --file Tools/ValidateGhaHomeLifecycle.cs --entry ValidateGhaHomeLifecycle.Main
// Edit Mode only. Disposable fixtures; no prefab saves, Play Mode or network access.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GHA.AvatarFramework;
using UnityEditor;
using UnityEngine;
using VertexFormCore;
using VertexFormCore.GHAIntegration;

public static class ValidateGhaHomeLifecycle
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Run in idle Edit Mode.");
        var subscribers = typeof(AvatarProviderSelection).GetField("SavedAvatarApplyRequested", BindingFlags.Static | BindingFlags.NonPublic);
        if (subscribers.GetValue(null) != null)
            throw new InvalidOperationException("An existing Save listener would invalidate this isolated check.");

        string[] keys = { AvatarProviderSelection.ModePrefsKey, "QVAS_AVATAR_MODE", "Avatar_Selection_Number" };
        var existed = new bool[keys.Length];
        var values = new int[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            existed[i] = PlayerPrefs.HasKey(keys[i]);
            values[i] = PlayerPrefs.GetInt(keys[i]);
        }
        ProjectManager originalManager = ProjectManager.instance;
        bool originalSuppression = AvatarSelectionManager.SuppressLegacyAvatars;
        byte originalDefault = AvatarProviderSelection.DefaultMode;
        var objects = new List<UnityEngine.Object>();
        var hosts = new List<VertexFormHomeAvatar>();
        var passed = new List<string>();
        try
        {
            GameObject fixtures = new GameObject("Home lifecycle validation fixtures");
            fixtures.SetActive(false);
            objects.Add(fixtures);
            var manager = fixtures.AddComponent<ProjectManager>();
            var config = ScriptableObject.CreateInstance<UILayoutConfig>();
            objects.Add(config);
            manager.uiLayoutConfig = config;
            ProjectManager.instance = manager;
            for (int i = 0; i < 2; i++)
                config.avatarDatas.Add(new AvatarData
                {
                    head = Child(fixtures.transform, "Head" + i),
                    body = Child(fixtures.transform, "Body" + i)
                });

            // No UMA adapter or puppet exists on this rig.
            var classic = MakeHost(fixtures.transform, hosts, out GameObject classicRoot);
            PlayerPrefs.SetInt(keys[0], 0);
            PlayerPrefs.SetInt(keys[2], 1);
            Invoke(classic, "Awake");
            Invoke(classic, "OnEnable");
            Require(AvatarProviderSelection.RequestApplySavedAvatar(), "Host must own the Save listener.");
            Require(!classic.IsReady, "Save before startup must wait for readiness.");
            Require(!((IEnumerator)Invoke(classic, "Start")).MoveNext(), "Desktop fixture startup must finish.");
            Require(classic.IsReady && classicRoot.activeSelf, "Classic Save must build with no UMA adapter.");
            Require(classicRoot.transform.Find("HeadTransform").GetChild(0).name == "Head1(Clone)", "Saved Classic selection must be used.");
            passed.Add("Classic Save queued before startup builds the saved head/body without a UMA adapter.");
            PlayerPrefs.SetInt(keys[2], 0);
            Require(classicRoot.transform.Find("HeadTransform").GetChild(0).name == "Head1(Clone)", "Browsing must not replace the applied body.");
            passed.Add("Changing the preview selection alone does not replace the applied body.");
            Invoke(classic, "OnDisable");
            Require(!AvatarProviderSelection.RequestApplySavedAvatar(), "Disabled host must unsubscribe.");
            passed.Add("Disabled host removes its Save listener.");

            var optional = MakeHost(fixtures.transform, hosts, out GameObject optionalRoot);
            Invoke(optional, "Awake");
            var provider = new Provider();
            Set(optional, "_provider", provider);
            // A live component stands in for the optional adapter's Unity lifetime.
            Set(optional, "_providerComponent", optional);
            optional.gameObject.SetActive(true);
            optional.transform.SetParent(null);
            objects.Add(optional.gameObject);
            Set(optional, "_startupComplete", true);
            PlayerPrefs.SetInt(keys[0], 1);
            optional.Refresh();
            Require(provider.Builds == 1 && !optionalRoot.activeSelf && optional.IsReady, "Provider builds through the contract and suppresses Classic.");
            Invoke(optional, "OnSceneLoadStarting", "FixtureWorld");
            Require(provider.Teardowns == 1 && !optional.IsReady, "Scene transition must tear down the optional instance.");
            passed.Add("Optional provider build and scene-transition teardown use the shared contract.");
            PlayerPrefs.SetInt(keys[0], 0);
            optional.Refresh();
            Require(provider.Teardowns == 2 && optional.IsReady && optionalRoot.activeSelf, "Switching to Classic must tear down the optional instance.");
            passed.Add("Classic switch tears down the optional provider before building the saved stock avatar.");

            var missing = MakeHost(fixtures.transform, hosts, out GameObject missingRoot);
            Invoke(missing, "Awake");
            Set(missing, "_startupComplete", true);
            PlayerPrefs.SetInt(keys[0], 42);
            missing.Refresh();
            Require(missingRoot.activeSelf && missing.IsReady && PlayerPrefs.GetInt(keys[0]) == 42,
                "Unavailable provider fallback must preserve the saved mode.");
            passed.Add("Unavailable provider uses Classic without overwriting its saved ID.");

            // Exercise the real installer helper on an unsaved, disposable prefab copy.
            const string homePath = "Assets/VertexForm3D/Resources/CustomEditor/HomeSceneComponent.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(homePath);
            try
            {
                var installer = Type.GetType("GHA.Integration.Editor.GhaVertexFormAssetInstaller, Assembly-CSharp-Editor", true);
                var ensure = installer.GetMethod("EnsureHomeAvatar", BindingFlags.Static | BindingFlags.NonPublic);
                var first = (VertexFormHomeAvatar)ensure.Invoke(null, new object[] { contents });
                var second = (VertexFormHomeAvatar)ensure.Invoke(null, new object[] { contents });
                Require(first == second && first.GetComponents<VertexFormHomeAvatar>().Length == 1,
                    "Host installer must be idempotent.");
                var serialized = new SerializedObject(first);
                Require(serialized.FindProperty("legacyAvatarRoot").objectReferenceValue != null, "Installer must wire the Classic root.");
                passed.Add("Host installer wires exactly one Home host on a disposable prefab copy.");
                foreach (MonoBehaviour component in first.GetComponents<MonoBehaviour>())
                {
                    if (!(component is IHomeAvatarProvider adapter)) continue;
                    Require(adapter.ResolveMode(42) == 42, "An installed adapter must preserve peer provider IDs.");
                    passed.Add("Installed provider adapter preserves unknown peer mode IDs.");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return new { passed, scope = "Edit Mode fixtures; runtime/PC Link and true package-absence checks remain pending", playMode = "not entered" };
        }
        finally
        {
            foreach (var host in hosts)
            {
                if (host == null) continue;
                Invoke(host, "OnDisable");
                Invoke(host, "OnDestroy");
            }
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
            ProjectManager.instance = originalManager;
            AvatarSelectionManager.SuppressLegacyAvatars = originalSuppression;
            AvatarProviderSelection.DefaultMode = originalDefault;
            for (int i = 0; i < keys.Length; i++)
                if (existed[i]) PlayerPrefs.SetInt(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]);
            PlayerPrefs.Save();
        }
    }

    private static VertexFormHomeAvatar MakeHost(Transform parent, List<VertexFormHomeAvatar> hosts, out GameObject legacy)
    {
        GameObject rig = Child(parent, "Fixture rig");
        legacy = Child(rig.transform, "CustomAvatar");
        Child(legacy.transform, "HeadTransform");
        Child(legacy.transform, "BodyTransform");
        var host = rig.AddComponent<VertexFormHomeAvatar>();
        host.Configure(legacy, 7, false);
        hosts.Add(host);
        return host;
    }

    private static GameObject Child(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }
    private static object Invoke(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, InstanceFlags).Invoke(target, args);
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, InstanceFlags).SetValue(target, value);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Provider : IHomeAvatarProvider, IHumanoidAvatarInstance
    {
        public int Builds, Teardowns;
        public byte Mode => 1;
        public byte DefaultMode => 1;
        public byte ResolveMode(byte requested) => requested;
        public IHumanoidAvatarInstance Instance => this;
        public Transform Root => null;
        public Animator Animator => null;
        public bool IsReady { get; private set; }
        public event Action HumanoidRebuilt { add { } remove { } }
        public void BuildSavedAvatar() { Builds++; IsReady = true; }
        public void Teardown() { Teardowns++; IsReady = false; }
        public void SetFirstPersonVisibility(AvatarVisibility visibility, int cullLayer) { }
        public bool TryGetRendererBounds(out Bounds bounds) { bounds = default; return false; }
    }
}
