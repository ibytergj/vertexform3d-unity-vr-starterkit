#if VERTEXFORM_GHA_HOST
using System.Collections;
using UnityEngine;
using GHA.AvatarFramework;
using AvatarLoadTimingLog = GHA.AvatarSuite.AvatarLoadTimingLog;

namespace VertexFormCore.GHAIntegration
{
    /// <summary>
    /// Owns saved-avatar application, Classic embodiment and local Home rig lifecycle.
    /// An optional provider supplies only its saved-avatar build and instance contract.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VertexFormHomeAvatar : MonoBehaviour
    {
        [Tooltip("The rig's legacy avatar root (CustomAvatar) â€” active only for a saved Classic choice.")]
        [SerializeField] private GameObject legacyAvatarRoot;
        [Tooltip("Layer the avatar moves to in first person so it doesn't block the camera. The mirror camera still renders this layer (legacy head behavior).")]
        [SerializeField] private int firstPersonHiddenLayer = 7;

        [Header("Temporary load-isolation test")]
        [Tooltip("When enabled, Home starts without an avatar and does not begin provider loading until the customizer explicitly requests it.")]
        [SerializeField] private bool delayInitialBuildUntilRequested;

        private IHomeAvatarProvider _provider;
        private MonoBehaviour _providerComponent;
        private bool _stockReady;
        private const string StockSelectionPrefsKey = "Avatar_Selection_Number";

        // Retains old installed prefab settings while installers adopt the host component.
        public void Configure(GameObject legacyRoot, int hiddenLayer, bool delayInitialBuild)
        {
            legacyAvatarRoot = legacyRoot;
            firstPersonHiddenLayer = hiddenLayer;
            delayInitialBuildUntilRequested = delayInitialBuild;
        }

        private bool ProviderAvailable => _providerComponent != null && _providerComponent.isActiveAndEnabled;
        private IHumanoidAvatarInstance ProviderInstance => ProviderAvailable ? _provider.Instance : null;
        public bool IsReady => _stockReady || (ProviderInstance != null && ProviderInstance.IsReady);

        private void FindProvider()
        {
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (!component.enabled || !(component is IHomeAvatarProvider provider))
                    continue;
                if (_provider != null)
                    throw new System.InvalidOperationException(
                        "Home currently supports one optional provider adapter; multiple adapters require the D3 registry.");
                _provider = provider;
                _providerComponent = component;
            }
        }

        private byte ResolveSavedMode()
        {
            byte requested = AvatarProviderSelection.LoadMode(AvatarProviderSelection.DefaultMode);
            byte resolved = ProviderAvailable ? _provider.ResolveMode(requested) : requested;
            if (resolved != 0 && (!ProviderAvailable || resolved != _provider.Mode))
            {
                Debug.LogWarning($"[VertexFormHomeAvatar] Provider mode {resolved} is unavailable; using Classic without changing the saved choice.");
                return 0;
            }
            return resolved;
        }
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
            FindProvider();
            if (legacyAvatarRoot == null)
                legacyAvatarRoot = transform.Find("CustomAvatar")?.gameObject;
            VertexFormCore.SceneLoader.SceneLoadStarting += OnSceneLoadStarting;
            // Switch the legacy avatar system off at the source (no per-frame fighting):
            // AvatarSelectionManager skips legacy instantiation/reactivation entirely.
            VertexFormCore.AvatarSelectionManager.SuppressLegacyAvatars = true;
            // The Studio's default tab must match the provider this host builds when
            // nothing has been saved yet (the installed provider's default system).
            AvatarProviderSelection.DefaultMode =
                ProviderAvailable ? _provider.DefaultMode : (byte)0;

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
            if (!ProviderAvailable)
                return;

            string traceId = AvatarLoadTimingLog.NewTraceId("transition");
            AvatarLoadTimingLog.Write(
                traceId,
                "avatar-dispatch",
                "HOME_TEARDOWN_FOR_SCENE",
                "home",
                details: $"target='{sceneName}' object='{gameObject.name}'");
            _provider.Teardown();
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
        /// frames while the ordinary asynchronous provider build runs; it does not synchronously
        /// wait on Addressables or poll from Update().
        /// </summary>
        public IEnumerator EnsureAvatarLoadedForCustomizer()
        {
            while (!_startupComplete)
                yield return null;

            bool useProvider = ResolveSavedMode() != 0;
            bool ready = IsReady;

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
            while (useProvider && (ProviderInstance == null || !ProviderInstance.IsReady))
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
            // Classic also needs the local camera to exclude the head-hiding layer.
            // Presentation.Apply accepts a null provider instance for that host policy.
            if (!_startupComplete)
                return;

            bool vrStyle = ProjectManager.instance != null
                && ProjectManager.instance.platforms != null
                && ProjectManager.instance.platforms.IsVrStylePlatform();
            bool firstPerson = vrStyle || (_rigController != null && !_rigController.isThirdPerson);
            Camera localCamera = firstPerson ? ResolveLocalCamera() : null;

            _presentation.Apply(
                ProviderInstance,
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
        /// Rebuilds from the saved choice. Browsing a preview never invokes this path.
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

            byte mode = ResolveSavedMode();
            _stockReady = false;
            if (mode != 0)
            {
                if (legacyAvatarRoot != null)
                    legacyAvatarRoot.SetActive(false);
                _provider.BuildSavedAvatar();
            }
            else
            {
                if (ProviderAvailable)
                    _provider.Teardown();
                _stockReady = BuildStockEmbodiment();
                if (legacyAvatarRoot != null)
                    legacyAvatarRoot.SetActive(_stockReady);
            }
        }

        /// <summary>
        /// Instantiates the chosen stock avatar into the rig's legacy AvatarHolder â€”
        /// the same head/body build the legacy AvatarSelectionManager performed before
        /// SuppressLegacyAvatars gated it off.
        /// </summary>
        private bool BuildStockEmbodiment()
        {
            if (legacyAvatarRoot == null) return false;

            var datas = ProjectManager.instance != null && ProjectManager.instance.uiLayoutConfig != null
                ? ProjectManager.instance.uiLayoutConfig.avatarDatas
                : null;
            int selection = PlayerPrefs.GetInt(StockSelectionPrefsKey, 0);
            if (datas == null || selection < 0 || selection >= datas.Count)
            {
                Debug.LogWarning($"[VertexFormHomeAvatar] Classic avatar {selection} unavailable (avatar database missing or index out of range).");
                return false;
            }

            Transform headAnchor = legacyAvatarRoot.transform.Find("HeadTransform");
            Transform bodyAnchor = legacyAvatarRoot.transform.Find("BodyTransform");
            if (headAnchor == null || bodyAnchor == null)
            {
                Debug.LogWarning("[VertexFormHomeAvatar] Legacy avatar root has no HeadTransform/BodyTransform anchors â€” classic embodiment unavailable.");
                return false;
            }

            if (datas[selection].head == null || datas[selection].body == null)
            {
                Debug.LogWarning($"[VertexFormHomeAvatar] Classic avatar {selection} is missing its head or body prefab.");
                return false;
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

            Debug.Log($"[VertexFormHomeAvatar] Built classic avatar {selection} on the rig.");
            return true;
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
