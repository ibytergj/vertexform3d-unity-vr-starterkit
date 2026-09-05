#if VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using UnityEngine;
using Fusion;
using GHA.AvatarFramework;
using VertexFormCore;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// UMA avatar logic on the player prefab. All networked state lives in the generic
    /// AvatarExtensionSync component (the only add-on NetworkBehaviour on the prefab â€”
    /// the layout-parity path for eventual vanilla-client interop); this component is a
    /// plain MonoBehaviour driving it.
    ///
    /// Interop: each player carries a networked Mode â€” stock (legacy head/body prefabs)
    /// or UMA. The AvatarConstructionOverride hook suppresses the legacy build only for
    /// players whose Mode is UMA, so stock and UMA avatars coexist in one session and
    /// every client renders each player with that player's chosen system, locally and
    /// remotely. Recipes sync as versioned bytes; no mesh data crosses the wire.
    /// </summary>
    [RequireComponent(typeof(AvatarExtensionSync))]
    public class UmaAvatarBridge : MonoBehaviour
    {
        public const byte UmaMode = 1;

        [SerializeField] private UmaAvatarCatalog catalog;
        [SerializeField] private RuntimeAnimatorController animationController;
        [Tooltip("Layer used to hide the avatar from the local first-person camera (matches AvatarHolder head layer).")]
        [SerializeField] private int localVrCullLayer = 7;

        [Header("Turning")]
        [Tooltip("Degrees/second the avatar turns toward its movement direction while moving.")]
        [SerializeField] private float movingTurnSpeed = 720f;
        [Tooltip("Degrees/second the avatar settles toward the look direction while idle.")]
        [SerializeField] private float idleTurnSpeed = 200f;
        [Tooltip("Speed (m/s) above which the avatar faces its movement direction instead of the look direction.")]
        [SerializeField] private float faceMovementThreshold = 0.6f;

        private AvatarExtensionSync _sync;
        private PlayerNetworkSetup _playerSetup;
        private AvatarInputConverter _inputConverter;
        private XRRigController _rigController;
        private UmaAvatarPuppet _puppet;
        private XrRigStartupStabilizer _xrRigStartupStabilizer;
        private bool _localUmaBuildPending;
        private string _xrReadinessTraceId;
        private double _xrReadinessStartedAt = -1d;
        private int _builtRevision = -1;
        private int _lastSeenStockSelection = int.MinValue;
        private readonly HumanoidAvatarPresentationController _presentation =
            new HumanoidAvatarPresentationController();

        /// <summary>True when this player's networked choice is the UMA system.</summary>
        public bool IsUmaModeActive => _sync != null && _sync.IsSpawned && _sync.Mode == UmaMode;

        /// <summary>
        /// Per-player answer for the construction hook. The hook can fire for the local
        /// player synchronously inside PlayerNetworkSetup.Spawned â€” before this object's
        /// own sync component has spawned â€” so fall back to the persisted local choice.
        /// </summary>
        public bool ShouldSuppressLegacyAvatar(PlayerNetworkSetup setup)
        {
            if (_sync != null && _sync.IsSpawned)
                return _sync.Mode == UmaMode;
            if (setup != null && setup.Object != null && setup.Object.HasInputAuthority)
                return UmaRecipeStore.LoadMode(catalog) == AvatarSystemMode.Uma;
            // Remote queried before spawn â€” effectively unreachable (remote init is delayed
            // well past the spawn pass). Default to the legacy path.
            return false;
        }

        /// <summary>Suppresses the legacy head/body avatar path per player, by that player's networked choice.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterHook()
        {
            PlayerNetworkSetup.AvatarConstructionOverride = (setup, selectionNumber) =>
            {
                var bridge = setup.GetComponent<UmaAvatarBridge>();
                return bridge != null && bridge.isActiveAndEnabled && bridge.ShouldSuppressLegacyAvatar(setup);
            };
        }

        private void Awake()
        {
            _sync = GetComponent<AvatarExtensionSync>();
            if (_sync == null)
            {
                Debug.LogError("[UmaAvatarBridge] AvatarExtensionSync component missing on the player prefab â€” UMA avatars disabled.");
                enabled = false;
                return;
            }
            _sync.SpawnedEvent += OnSyncSpawned;
            _sync.StateChanged += OnSyncStateChanged;
        }

        private void OnDestroy()
        {
            if (_playerSetup != null)
                _playerSetup.SittingStateChanged -= OnLocalSittingStateChanged;
            if (_sync != null)
            {
                _sync.SpawnedEvent -= OnSyncSpawned;
                _sync.StateChanged -= OnSyncStateChanged;
            }
        }

        private void OnSyncSpawned()
        {
            _playerSetup = GetComponent<PlayerNetworkSetup>();
            if (_playerSetup != null)
                _playerSetup.SittingStateChanged += OnLocalSittingStateChanged;
            if (_playerSetup != null && _playerSetup.LocalXRRigGameobject != null)
                _inputConverter = _playerSetup.LocalXRRigGameobject.GetComponent<AvatarInputConverter>();

            _puppet = gameObject.AddComponent<UmaAvatarPuppet>();
            _puppet.animationController = animationController;
            _puppet.movingTurnSpeed = movingTurnSpeed;
            _puppet.idleTurnSpeed = idleTurnSpeed;
            _puppet.faceMovementThreshold = faceMovementThreshold;
            _puppet.LoadTimingHost = _sync.HasInputAuthority ? "network-local" : "network-remote";

            if (_sync.HasInputAuthority)
            {
                if (_playerSetup != null && _playerSetup.NetworkedIsVrStyle())
                {
                    _xrRigStartupStabilizer = GetComponent<XrRigStartupStabilizer>();
                    if (_xrRigStartupStabilizer == null)
                        _xrRigStartupStabilizer = gameObject.AddComponent<XrRigStartupStabilizer>();
                    _xrRigStartupStabilizer.ActivateForLocalRig();
                }

                // Same fallback chain PlayerNetworkSetup uses â€” the controller is not
                // guaranteed to live on this exact GameObject in the player prefab.
                _rigController = GetComponent<XRRigController>()
                    ?? GetComponentInParent<XRRigController>()
                    ?? GetComponentInChildren<XRRigController>();
                if (_rigController == null)
                    Debug.LogWarning("[UmaAvatarBridge] No XRRigController found â€” first-person avatar hiding disabled.");

                // Push the saved recipe regardless of mode so a later switch to UMA is
                // instant for observers; Mode is the authority on what gets rendered.
                AvatarWireRecipe recipe = UmaRecipeStore.LoadOrDefault(catalog);
                PushRecipeToNetwork(recipe);
                _builtRevision = _sync.Revision;
                _sync.Mode = UmaRecipeStore.LoadMode(catalog) == AvatarSystemMode.Uma
                    ? UmaMode
                    : AvatarExtensionSync.StockMode;
                if (_sync.Mode == UmaMode)
                {
                    if (_xrRigStartupStabilizer == null || _xrRigStartupStabilizer.IsReady)
                        _puppet.Build(catalog, recipe);
                    else
                    {
                        _localUmaBuildPending = true;
                        _xrReadinessTraceId = AvatarLoadTimingLog.NewTraceId("xr");
                        _xrReadinessStartedAt = AvatarLoadTimingLog.Now;
                        AvatarLoadTimingLog.Write(
                            _xrReadinessTraceId,
                            "xr-readiness",
                            "WAIT_START",
                            "network-local",
                            _xrReadinessStartedAt,
                            $"object='{gameObject.name}'");
                    }
                }
                // Stock mode: PlayerNetworkSetup's own init builds the legacy avatar
                // (the hook answers false for this player).
            }
            else if (_sync.Mode == UmaMode)
            {
                // Remote players must not run the local hand animation driver (mirrors the legacy path).
                DestroyAnimateHands();
                if (_sync.Length > 0)
                    TryBuildFromNetwork();
                // else: recipe hasn't arrived yet â€” StateChanged picks it up.
            }
            // Remote stock players: the legacy lazy init path builds them untouched.

            if (_playerSetup != null)
                _lastSeenStockSelection = _playerSetup.AvatarSelectionNumber;

            if (_sync.HasInputAuthority)
                PublishLocalPosture();
            ApplyPostureFromSync();
        }

        private void OnSyncStateChanged(string propertyName)
        {
            if (propertyName == nameof(AvatarExtensionSync.Posture)
                || propertyName == nameof(AvatarExtensionSync.PostureRevision)
                || propertyName == nameof(AvatarExtensionSync.SeatId))
            {
                ApplyPostureFromSync();
                return;
            }

            if (_sync.HasInputAuthority)
                return; // local changes are applied directly by the Apply* methods

            if (propertyName == nameof(AvatarExtensionSync.Mode))
            {
                OnRemoteModeChanged();
                ApplyPostureFromSync();
            }
            else if (propertyName == nameof(AvatarExtensionSync.Revision) && _sync.Mode == UmaMode)
                TryBuildFromNetwork();
        }

        private void OnRemoteModeChanged()
        {
            if (_sync.Mode == UmaMode)
            {
                TearDownStockAvatar();
                DestroyAnimateHands();
                _builtRevision = -1;
                TryBuildFromNetwork();
            }
            else
            {
                _puppet.Teardown();
                _builtRevision = -1;
                RestoreLegacyHandVisuals();
                if (_playerSetup != null)
                {
                    TearDownStockAvatar();
                    _playerSetup.InitializeSelectedAvatarModel(_playerSetup.AvatarSelectionNumber);
                    _lastSeenStockSelection = _playerSetup.AvatarSelectionNumber;
                }
            }
        }

        private void LateUpdate()
        {
            if (_sync == null || !_sync.IsSpawned || _playerSetup == null)
                return;

            if (_localUmaBuildPending
                && _sync.HasInputAuthority
                && _sync.Mode == UmaMode
                && _xrRigStartupStabilizer != null
                && _xrRigStartupStabilizer.IsReady)
            {
                _localUmaBuildPending = false;
                AvatarLoadTimingLog.Write(
                    _xrReadinessTraceId,
                    "xr-readiness",
                    "WAIT_FINISH",
                    "network-local",
                    _xrReadinessStartedAt,
                    $"object='{gameObject.name}'");
                _xrReadinessStartedAt = -1d;
                _puppet.Build(catalog, UmaRecipeStore.LoadOrDefault(catalog));
                Debug.Log("[UmaAvatarBridge] Local UMA construction released after XR Floor pose became ready.");
            }

            TrackRemoteStockSelection();

            if (!IsUmaModeActive)
                return;

            if (_puppet == null || _puppet.Dca == null)
                return;

            // The full-body avatar replaces the legacy floating hand/controller visuals.
            // VR keeps them only until the UMA arm IK is ready, so testers still have
            // tracked hand feedback if the IK rig cannot build.
            bool hideLegacyHands = _playerSetup.NetworkedIsDesktopStyle()
                || (_playerSetup.NetworkedIsVrStyle() && _puppet.HasVrArmIk);
            if (hideLegacyHands)
            {
                HideIfActive(_playerSetup.leftControllerHand);
                HideIfActive(_playerSetup.rightControllerHand);
                HideIfActive(_playerSetup.leftHand);
                HideIfActive(_playerSetup.rightHand);
            }

            // The local host supplies only presentation policy and camera discovery.
            // The generic controller applies semantic visibility and camera-layer culling.
            if (_sync.HasInputAuthority)
            {
                bool vrStyle = _playerSetup.NetworkedIsVrStyle();
                bool firstPerson = vrStyle || (_rigController != null && !_rigController.isThirdPerson);
                bool headOnlyVisibilityAvailable = vrStyle && _puppet.HasVrArmIk;
                Camera localCamera = firstPerson ? ResolveLocalCamera() : null;

                _presentation.Apply(
                    _puppet,
                    firstPerson,
                    headOnlyVisibilityAvailable,
                    localVrCullLayer,
                    localCamera,
                    $"player='{gameObject.name}'");
            }
        }

        private void OnLocalSittingStateChanged(bool isSitting)
        {
            PublishLocalPosture();
            ApplyPostureFromSync();
        }

        private void PublishLocalPosture()
        {
            if (_sync == null || !_sync.IsSpawned || !_sync.HasInputAuthority || _playerSetup == null)
                return;

            byte localPosture = _playerSetup.IsSitting
                ? AvatarExtensionSync.SittingPosture
                : AvatarExtensionSync.StandingPosture;
            NetworkId seatId = default;
            SitSpot currentSeat = _playerSetup.CurrentSitSpot;
            if (localPosture == AvatarExtensionSync.SittingPosture
                && currentSeat != null
                && currentSeat.Object != null)
            {
                seatId = currentSeat.Object.Id;
            }
            _sync.WritePosture(localPosture, seatId);
        }

        private void ApplyPostureFromSync()
        {
            if (_puppet == null || !IsUmaModeActive)
                return;

            HumanoidPosture posture = _sync.Posture == AvatarExtensionSync.SittingPosture
                ? HumanoidPosture.Sitting
                : HumanoidPosture.Standing;
            _puppet.SetPosture(posture, ResolveSeatedPelvisTarget(posture));
        }

        private Transform ResolveSeatedPelvisTarget(HumanoidPosture posture)
        {
            if (posture != HumanoidPosture.Sitting || _sync == null)
                return null;

            SitSpot seat = _sync.HasInputAuthority && _playerSetup != null
                ? _playerSetup.CurrentSitSpot
                : null;
            if (seat == null
                && _sync.SeatId != default
                && _sync.Runner != null
                && _sync.Runner.TryFindObject(_sync.SeatId, out NetworkObject seatObject))
            {
                seat = seatObject.GetComponent<SitSpot>();
            }

            return seat != null ? seat.SeatedPelvisTarget : null;
        }

        private Camera ResolveLocalCamera()
        {
            Camera localCamera = _playerSetup != null ? _playerSetup.cam : null;
            if (localCamera == null && _inputConverter != null && _inputConverter.XRHead != null)
                localCamera = _inputConverter.XRHead.GetComponentInChildren<Camera>(true);
            if (localCamera == null)
                localCamera = Camera.main;
            return localCamera;
        }
        /// <summary>
        /// Remote stock players can change which stock avatar they wear mid-session; the
        /// legacy system only builds once, so rebuild on selection changes ourselves.
        /// </summary>
        private void TrackRemoteStockSelection()
        {
            int selection = _playerSetup.AvatarSelectionNumber;
            if (_lastSeenStockSelection == int.MinValue)
            {
                _lastSeenStockSelection = selection;
                return;
            }
            if (selection == _lastSeenStockSelection)
                return;
            _lastSeenStockSelection = selection;

            // Only rebuild an avatar that already exists â€” before the legacy lazy init has
            // run, it will pick up the current selection by itself (rebuilding here too
            // would stack a second instance).
            if (!_sync.HasInputAuthority && _sync.Mode == AvatarExtensionSync.StockMode && StockAvatarInstanceExists())
            {
                TearDownStockAvatar();
                _playerSetup.InitializeSelectedAvatarModel(selection);
            }
        }

        /// <summary>Called by customization UI on the local player; persists, syncs, rebuilds as UMA.</summary>
        public void ApplyLocalRecipe(AvatarWireRecipe recipe)
        {
            if (_sync == null || !_sync.HasInputAuthority) return;
            if (catalog != null && !catalog.enableUmaAvatars)
            {
                Debug.LogWarning("[UmaAvatarBridge] UMA avatars are disabled by the experience author; ignoring.");
                return;
            }
            UmaRecipeStore.Save(recipe);
            UmaRecipeStore.SaveMode(AvatarSystemMode.Uma);
            bool wasStock = _sync.Mode != UmaMode;
            _sync.Mode = UmaMode;
            PushRecipeToNetwork(recipe);
            _builtRevision = _sync.Revision;
            if (wasStock)
                TearDownStockAvatar();
            _puppet.Build(catalog, recipe);
        }

        /// <summary>Called by customization UI on the local player; switches to a stock (legacy) avatar.</summary>
        public void ApplyLocalStockAvatar(int selectionNumber)
        {
            if (_sync == null || !_sync.HasInputAuthority || _playerSetup == null) return;
            if (catalog != null && !catalog.enableStockAvatars)
            {
                Debug.LogWarning("[UmaAvatarBridge] Stock avatars are disabled by the experience author; ignoring.");
                return;
            }
            UmaRecipeStore.SaveMode(AvatarSystemMode.Stock);
            UmaRecipeStore.SaveStockSelection(selectionNumber);
            _sync.Mode = AvatarExtensionSync.StockMode;
            _playerSetup.AvatarSelectionNumber = selectionNumber;
            _puppet.Teardown();
            _builtRevision = -1;
            TearDownStockAvatar();
            _playerSetup.InitializeSelectedAvatarModel(selectionNumber);
            RestoreLegacyHandVisuals();
            _lastSeenStockSelection = selectionNumber;
        }

        private void PushRecipeToNetwork(AvatarWireRecipe recipe)
        {
            byte[] buffer = new byte[UmaRecipeCodec.MaxBytes];
            int length = UmaRecipeCodec.Encode(recipe, buffer);
            if (length <= 0)
            {
                Debug.LogWarning("[UmaAvatarBridge] Recipe failed to encode; not syncing.");
                return;
            }
            _sync.WriteData(buffer, length);
        }

        private void TryBuildFromNetwork()
        {
            if (_sync.Length == 0 || _builtRevision == _sync.Revision) return;
            byte[] buffer = _sync.ReadData();
            if (UmaRecipeCodec.TryDecode(buffer, buffer.Length, out AvatarWireRecipe recipe))
            {
                _builtRevision = _sync.Revision;
                _puppet.Build(catalog, recipe);
            }
            else
            {
                Debug.LogWarning("[UmaAvatarBridge] Received undecodable avatar recipe.");
            }
        }

        // ---------------------------------------------------------------- legacy-avatar helpers

        /// <summary>
        /// Destroys the stock avatar instances the legacy path created: the head and body
        /// "(Clone)"s (reparented under the rig's sync targets) and the shadow-only head
        /// copy. The AvatarHolder's hand transforms are persistent rig pieces â€” untouched.
        /// </summary>
        private void TearDownStockAvatar()
        {
            AvatarHolder holder = _playerSetup != null ? _playerSetup.avatarHolder : null;
            if (holder == null) return;
            if (holder.HeadTransform != null && holder.HeadTransform.name.EndsWith("(Clone)"))
                Destroy(holder.HeadTransform.gameObject);
            if (holder.BodyTransform != null && holder.BodyTransform.name.EndsWith("(Clone)"))
                Destroy(holder.BodyTransform.gameObject);
            if (holder.ShadowHeadTransform != null)
            {
                for (int i = holder.ShadowHeadTransform.childCount - 1; i >= 0; i--)
                    Destroy(holder.ShadowHeadTransform.GetChild(i).gameObject);
            }
        }

        private bool StockAvatarInstanceExists()
        {
            AvatarHolder holder = _playerSetup != null ? _playerSetup.avatarHolder : null;
            return holder != null
                && holder.HeadTransform != null
                && holder.HeadTransform.name.EndsWith("(Clone)");
        }

        /// <summary>
        /// Undoes the per-frame hand hiding after a switch to the stock system. Which
        /// visuals a stock player shows is derivable on every client from the networked
        /// isHandTracking flag (mirrors RPC_EnableHand/RPC_EnableHandController).
        /// </summary>
        private void RestoreLegacyHandVisuals()
        {
            if (_playerSetup == null || !_playerSetup.NetworkedIsDesktopStyle()) return;
            bool handTracking = _playerSetup.isHandTracking;
            SetActiveSafe(_playerSetup.leftHand, handTracking);
            SetActiveSafe(_playerSetup.rightHand, handTracking);
            SetActiveSafe(_playerSetup.leftControllerHand, !handTracking);
            SetActiveSafe(_playerSetup.rightControllerHand, !handTracking);
        }

        private void DestroyAnimateHands()
        {
            if (_inputConverter == null) return;
            DestroyAnimateHandUnder(_inputConverter.AvatarHand_Left);
            DestroyAnimateHandUnder(_inputConverter.AvatarHand_Right);
        }

        private static void DestroyAnimateHandUnder(Transform hand)
        {
            if (hand == null) return;
            var animateHand = hand.GetComponentInChildren<AnimateHand>();
            if (animateHand != null)
                Destroy(animateHand);
        }

        private static void HideIfActive(GameObject go)
        {
            if (go != null && go.activeSelf)
                go.SetActive(false);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }
    }
}
#endif
