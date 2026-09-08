#if VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using System.Collections;
using UnityEngine;
using GHA.AvatarFramework;

namespace GHA.AvatarSuite
{
    // Note: XRRigController lives in the global namespace (no using needed).

    /// <summary>
    /// Replaces the HomeScene rig's legacy head/body avatar with a UMA avatar built
    /// from the player's saved recipe â€” so the player embodies (and sees in the
    /// mirror) the same avatar they will spawn with in worlds.
    /// Lives on the HomeScene XR Origin (which carries AvatarInputConverter and
    /// CharacterController for the puppet to use).
    /// </summary>
    public class UmaHomeAvatar : MonoBehaviour
    {
        [SerializeField] private UmaAvatarCatalog catalog;
        [SerializeField] private RuntimeAnimatorController animationController;
        [Tooltip("The rig's legacy avatar root (CustomAvatar) â€” kept deactivated at runtime.")]
        [SerializeField] private GameObject legacyAvatarRoot;
        [Tooltip("Layer the avatar moves to in first person so it doesn't block the camera. The mirror camera still renders this layer (legacy head behavior).")]
        [SerializeField] private int firstPersonHiddenLayer = 7;

        [Header("Temporary load-isolation test")]
        [Tooltip("When enabled, Home starts without an avatar and does not begin UMA loading until the customizer explicitly requests it.")]
        [SerializeField] private bool delayInitialBuildUntilRequested;

        private UmaAvatarPuppet _puppet;
        private XRRigController _rigController;
        private XrRigStartupStabilizer _xrRigStartupStabilizer;
        private string _xrReadinessTraceId;
        private double _xrReadinessStartedAt = -1d;
        private bool _startupComplete;
        private bool _initialBuildRequested;
        private string _deferredLoadTraceId;
        private double _deferredLoadStartedAt = -1d;
        private readonly HumanoidAvatarPresentationController _presentation =
            new HumanoidAvatarPresentationController();

        private void OnEnable()
        {
            AvatarProviderSelection.SavedAvatarApplyRequested += Refresh;
        }

        private void OnDisable()
        {
            AvatarProviderSelection.SavedAvatarApplyRequested -= Refresh;
        }

        private void Awake()
        {
            VertexFormCore.SceneLoader.SceneLoadStarting += OnSceneLoadStarting;
            // Switch the legacy avatar system off at the source (no per-frame fighting):
            // AvatarSelectionManager skips legacy instantiation/reactivation entirely.
            VertexFormCore.AvatarSelectionManager.SuppressLegacyAvatars = true;

            if (legacyAvatarRoot != null)
                legacyAvatarRoot.SetActive(false);

            bool vrStyle = ProjectManager.instance != null
                && ProjectManager.instance.platforms != null
                && ProjectManager.instance.platforms.IsVrStylePlatform();
            if (vrStyle)
            {
                _xrRigStartupStabilizer = GetComponent<XrRigStartupStabilizer>();
                if (_xrRigStartupStabilizer == null)
                    _xrRigStartupStabilizer = gameObject.AddComponent<XrRigStartupStabilizer>();
                _xrRigStartupStabilizer.ActivateForLocalRig();
            }
        }

        private void OnSceneLoadStarting(string sceneName)
        {
            if (_puppet == null)
                return;

            string traceId = AvatarLoadTimingLog.NewTraceId("transition");
            AvatarLoadTimingLog.Write(
                traceId,
                "avatar-dispatch",
                "HOME_TEARDOWN_FOR_SCENE",
                "home",
                details: $"target='{sceneName}' object='{gameObject.name}'");
            _puppet.Teardown();
        }

        private void OnDestroy()
        {
            VertexFormCore.SceneLoader.SceneLoadStarting -= OnSceneLoadStarting;
        }

        private IEnumerator Start()
        {
            if (_xrRigStartupStabilizer != null && !_xrRigStartupStabilizer.IsReady)
            {
                _xrReadinessTraceId = AvatarLoadTimingLog.NewTraceId("xr");
                _xrReadinessStartedAt = AvatarLoadTimingLog.Now;
                AvatarLoadTimingLog.Write(
                    _xrReadinessTraceId,
                    "xr-readiness",
                    "WAIT_START",
                    "home",
                    _xrReadinessStartedAt,
                    $"object='{gameObject.name}'");
            }

            while (_xrRigStartupStabilizer != null && !_xrRigStartupStabilizer.IsReady)
                yield return null;

            if (_xrReadinessStartedAt >= 0d)
            {
                AvatarLoadTimingLog.Write(
                    _xrReadinessTraceId,
                    "xr-readiness",
                    "WAIT_FINISH",
                    "home",
                    _xrReadinessStartedAt,
                    $"object='{gameObject.name}'");
                _xrReadinessStartedAt = -1d;
            }

            if (legacyAvatarRoot != null)
                legacyAvatarRoot.SetActive(false);

            _rigController = GetComponent<XRRigController>();
            _puppet = gameObject.AddComponent<UmaAvatarPuppet>();
            _puppet.animationController = animationController;
            _puppet.LoadTimingHost = "home";
            _startupComplete = true;

            if (delayInitialBuildUntilRequested && !_initialBuildRequested)
            {
                _deferredLoadTraceId = AvatarLoadTimingLog.NewTraceId("deferred");
                _deferredLoadStartedAt = AvatarLoadTimingLog.Now;
                AvatarLoadTimingLog.Write(
                    _deferredLoadTraceId,
                    "avatar-dispatch",
                    "INITIAL_BUILD_DEFERRED",
                    "home",
                    _deferredLoadStartedAt,
                    $"object='{gameObject.name}' trigger='ChangeAvatarButton'");
            }
            else
            {
                _initialBuildRequested = true;
                Refresh();
            }
        }

        /// <summary>
        /// Temporary load-isolation gate used by the Home customizer. The coroutine yields
        /// frames while the ordinary asynchronous UMA build runs; it does not synchronously
        /// wait on Addressables or poll from Update().
        /// </summary>
        public IEnumerator EnsureAvatarLoadedForCustomizer()
        {
            while (!_startupComplete)
                yield return null;

            bool useUma = UmaRecipeStore.LoadMode(catalog) == AvatarSystemMode.Uma;
            bool ready = useUma
                ? _puppet != null && _puppet.IsReady
                : legacyAvatarRoot != null && legacyAvatarRoot.activeSelf;

            if (!ready && !_initialBuildRequested)
            {
                _initialBuildRequested = true;
                AvatarLoadTimingLog.Write(
                    _deferredLoadTraceId,
                    "avatar-dispatch",
                    "DEFERRED_BUILD_REQUESTED",
                    "home",
                    _deferredLoadStartedAt,
                    $"object='{gameObject.name}' trigger='ChangeAvatarButton'");
                Refresh();
            }

            const double timeoutSeconds = 90d;
            double waitStartedAt = AvatarLoadTimingLog.Now;
            while (useUma && (_puppet == null || !_puppet.IsReady))
            {
                if (AvatarLoadTimingLog.Now - waitStartedAt >= timeoutSeconds)
                {
                    AvatarLoadTimingLog.Write(
                        _deferredLoadTraceId,
                        "avatar-dispatch",
                        "DEFERRED_BUILD_TIMEOUT",
                        "home",
                        _deferredLoadStartedAt,
                        $"object='{gameObject.name}' timeoutSeconds={timeoutSeconds:F0}");
                    yield break;
                }
                yield return null;
            }

            AvatarLoadTimingLog.Write(
                _deferredLoadTraceId,
                "avatar-dispatch",
                "DEFERRED_BUILD_READY",
                "home",
                _deferredLoadStartedAt,
                $"object='{gameObject.name}'");
        }

        private void LateUpdate()
        {
            if (_puppet == null)
                return;

            bool vrStyle = ProjectManager.instance != null
                && ProjectManager.instance.platforms != null
                && ProjectManager.instance.platforms.IsVrStylePlatform();
            bool firstPerson = vrStyle || (_rigController != null && !_rigController.isThirdPerson);
            Camera localCamera = firstPerson ? ResolveLocalCamera() : null;

            _presentation.Apply(
                _puppet,
                firstPerson,
                vrStyle,
                firstPersonHiddenLayer,
                localCamera,
                "home");
        }

        private Camera ResolveLocalCamera()
        {
            Camera localCamera = Camera.main;
            if (localCamera == null)
                localCamera = GetComponentInChildren<Camera>(true);
            return localCamera;
        }
        /// <summary>
        /// Rebuilds the rig avatar from the saved choice (called after the customizer
        /// saves). Mode-aware: UMA builds the puppet from the saved recipe; Classic
        /// (stock) tears the puppet down and builds the chosen head/body prefabs into
        /// the rig's legacy AvatarHolder, mirroring the legacy selection flow.
        /// </summary>
        public void Refresh()
        {
            // The Home host owns both embodiments. Selecting Classic must not return
            // ownership to the legacy manager, which can reactivate it behind UMA.
            VertexFormCore.AvatarSelectionManager.SuppressLegacyAvatars = true;
            if (!_startupComplete)
            {
                _initialBuildRequested = true;
                return;
            }

            bool useUma = UmaRecipeStore.LoadMode(catalog) == AvatarSystemMode.Uma;
            Debug.Log($"[UmaHomeAvatar] Refresh â€” mode {(useUma ? "UMA" : "Classic")}, legacyRoot {(legacyAvatarRoot != null ? "ok" : "MISSING")}, puppet {(_puppet != null ? "ok" : "MISSING")}");
            if (useUma)
            {
                if (legacyAvatarRoot != null)
                    legacyAvatarRoot.SetActive(false);
                if (_puppet != null)
                    _puppet.Build(catalog, UmaRecipeStore.LoadOrDefault(catalog));
            }
            else
            {
                if (_puppet != null)
                    _puppet.Teardown();
                if (legacyAvatarRoot != null)
                    legacyAvatarRoot.SetActive(true);
                BuildStockEmbodiment();
            }
        }

        /// <summary>
        /// Instantiates the chosen stock avatar into the rig's legacy AvatarHolder â€”
        /// the same head/body build the legacy AvatarSelectionManager performed before
        /// SuppressLegacyAvatars gated it off.
        /// </summary>
        private void BuildStockEmbodiment()
        {
            if (legacyAvatarRoot == null) return;

            var datas = ProjectManager.instance != null && ProjectManager.instance.uiLayoutConfig != null
                ? ProjectManager.instance.uiLayoutConfig.avatarDatas
                : null;
            int selection = UmaRecipeStore.LoadStockSelection();
            if (datas == null || selection < 0 || selection >= datas.Count)
            {
                Debug.LogWarning($"[UmaHomeAvatar] Classic avatar {selection} unavailable (avatar database missing or index out of range).");
                return;
            }

            Transform headAnchor = legacyAvatarRoot.transform.Find("HeadTransform");
            Transform bodyAnchor = legacyAvatarRoot.transform.Find("BodyTransform");
            if (headAnchor == null || bodyAnchor == null)
            {
                Debug.LogWarning("[UmaHomeAvatar] Legacy avatar root has no HeadTransform/BodyTransform anchors â€” classic embodiment unavailable.");
                return;
            }

            // Clears previous instances AND the shadow copies SetAvatar parents here
            // (this rig's ShadowHead/ShadowBody transforms alias the same anchors).
            ClearChildren(headAnchor);
            ClearChildren(bodyAnchor);

            GameObject body = Instantiate(datas[selection].body, bodyAnchor, false);
            GameObject head = Instantiate(datas[selection].head, headAnchor, false);
            body.transform.localPosition = head.transform.localPosition = Vector3.zero;

            var holder = legacyAvatarRoot.GetComponent<VertexFormCore.AvatarHolder>();
            if (holder != null)
                holder.SetAvatar(head, body, true); // legacy parity: shadow copy + head on the FP-hidden layer

            Debug.Log($"[UmaHomeAvatar] Built classic avatar {selection} on the rig.");
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                parent.GetChild(i).gameObject.SetActive(false);
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
#endif
