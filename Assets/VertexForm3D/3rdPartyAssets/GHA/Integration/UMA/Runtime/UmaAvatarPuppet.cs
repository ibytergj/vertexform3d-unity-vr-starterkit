#if VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using GHA.AvatarFramework;
using VertexFormCore;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// Builds and animates a UMA avatar on a player rig — networking-agnostic.
    /// Used by UmaAvatarBridge (networked players) and UmaHomeAvatar (HomeScene rig).
    /// Handles: building from a recipe, anchoring to the CharacterController capsule
    /// bottom, facing the movement direction, and driving the locomotion animator.
    /// Added via AddComponent by its host; all fields are configured in code.
    /// </summary>
    public class UmaAvatarPuppet : MonoBehaviour, IHumanoidAvatarInstance
    {
        public RuntimeAnimatorController animationController;
        public float movingTurnSpeed = 720f;
        public float idleTurnSpeed = 200f;
        public float faceMovementThreshold = 0.6f;
        [Tooltip("Speed (m/s) mapped to the top of the locomotion blend (sprint). The animator's Speed parameter is normalized 0..1 against this.")]
        public float animatorMaxSpeed = 5f;
        [Tooltip("In VR-style players, rotate the UMA head bone to match the synced AvatarHead target.")]
        public bool driveVrHeadBone = true;
        [Tooltip("Log local VR embodiment offsets while tuning head/body alignment.")]
        public bool debugVrEmbodimentAlignment = true;
        [Tooltip("Minimum seconds between local VR alignment dumps after values have changed.")]
        public float debugVrEmbodimentLogInterval = 1f;
        [Tooltip("Minimum transform delta in meters before another VR alignment dump is emitted.")]
        public float debugVrEmbodimentPositionThreshold = 0.05f;
        [Tooltip("Minimum angle delta in degrees before another VR alignment dump is emitted.")]
        public float debugVrEmbodimentAngleThreshold = 5f;
        [Header("VR Embodiment Tuning")]
        [Tooltip("Local-space offset from the player root to the UMA avatar floor point in VR.")]
        public Vector3 vrAvatarRootOffset = Vector3.zero;
        [Tooltip("Additional world-space offset after matching the UMA head bone to the tracked headset.")]
        public Vector3 vrTrackedHeadMatchOffset = Vector3.zero;
        [Tooltip("Meters forward from the UMA head bone to treat as the avatar eye/camera point in local VR.")]
        public float vrHeadBoneToEyeForwardOffset = 0.1f;
        [Tooltip("Meters upward from the UMA head bone to treat as the avatar eye/camera point in local VR.")]
        public float vrHeadBoneToEyeUpOffset = 0f;
        [Tooltip("Calibrate the local VR UMA avatar once after the player reaches a stable standing height, then lock its scale.")]
        public bool autoScaleVrAvatarToPlayerHeight = true;
        [Tooltip("Minimum headset eye height above the tracked floor that can begin standing calibration. Seated players keep the UMA authored scale.")]
        public float standingCalibrationMinEyeHeight = 1.35f;
        [Tooltip("Seconds the headset must remain above the standing threshold before avatar scale is calibrated and locked.")]
        public float standingCalibrationHoldSeconds = 1f;
        [Tooltip("Maximum eye-height movement allowed during the standing calibration hold period.")]
        public float standingCalibrationMaxEyeHeightDrift = 0.02f;
        [Tooltip("Smallest allowed automatic local VR avatar scale.")]
        public float minVrAvatarAutoScale = 0.75f;
        [Tooltip("Largest allowed automatic local VR avatar scale.")]
        public float maxVrAvatarAutoScale = 1.35f;
        [Header("Flat Platform Grounding")]
        [Tooltip("Small visual clearance above the CharacterController contact plane for footwear whose sole extends below the UMA avatar root.")]
        public float flatAvatarGroundClearance = 0.02f;
        [Tooltip("Animator state that represents the stable seated loop and permits final pelvis-to-seat alignment.")]
        public string seatedLoopStateName = "BasicMotions@SitMed01_B - Loop";
        [Tooltip("Delay after focus/tracking regain before rebuilding VR IK proxies and realigning.")]
        public float trackingResumeReinitializeDelay = 0.2f;
        [Tooltip("Extra wrist rotation applied to UMA IK targets while using tracked hand mode.")]
        public Vector3 leftTrackedHandWristRotationOffset = new Vector3(90f, 45f, 180f);
        public Vector3 rightTrackedHandWristRotationOffset = new Vector3(90f, 45f, 180f);
        [Tooltip("Extra wrist rotation applied to UMA IK targets while using controller mode.")]
        public Vector3 leftControllerWristRotationOffset = new Vector3(90f, 0f, 180f);
        public Vector3 rightControllerWristRotationOffset = new Vector3(90f, 0f, 180f);

        public DynamicCharacterAvatar Dca => _dca;
        public bool HasVrArmIk => _humanoidArmRig.IsReady;
        public Transform Root => _dcaRoot != null ? _dcaRoot.transform : null;
        public Animator Animator => _avatarAnimator;
        public bool IsReady => _dcaRoot != null && _avatarAnimator != null;
        public event System.Action HumanoidRebuilt;
        public string LoadTimingHost { get; set; } = "unspecified";

        private UmaAvatarCatalog _catalog;
        private AvatarWireRecipe _lastRecipe;
        private AvatarInputConverter _inputConverter;
        private PlayerNetworkSetup _playerSetup;
        private VertexFormHumanoidRigInput _vertexRigInput;
        private IHumanoidRigInput _rigInput;
        private CharacterController _characterController;
        private DynamicCharacterAvatar _dca;
        private GameObject _dcaRoot;
        private Animator _avatarAnimator;
        private Transform _headBone;
        private Transform _leftEyeBone;
        private Transform _rightEyeBone;
        private Quaternion _headAnimationFallbackLocalRotation;
        private bool _hasHeadAnimationFallback;
        private readonly HumanoidArmRig _humanoidArmRig = new HumanoidArmRig();
        private readonly HumanoidLocomotionDriver _locomotionDriver = new HumanoidLocomotionDriver();
        private readonly HumanoidPostureAnimator _postureAnimator = new HumanoidPostureAnimator();
        private readonly HumanoidSeatedPelvisAlignment _seatedPelvisAlignment = new HumanoidSeatedPelvisAlignment();
        private Transform _seatedPelvisTarget;
        private bool _seatedPelvisAlignmentLogged;
        private bool VrArmIkReady => _humanoidArmRig.IsReady;
        private float _nextVrArmIkRetryTime;
        private float _nextVrAlignmentLogTime;
        private readonly HumanoidTrackingLifecycle _trackingLifecycle = new HumanoidTrackingLifecycle();
        private readonly HumanoidVrAlignment _vrAlignment = new HumanoidVrAlignment();
        private float _nextVrVisibilityLogTime;
        private VrAlignmentDebugSnapshot _lastAlignmentSnapshot;
        private bool _hasLastAlignmentSnapshot;
        private VrVisibilityDebugSnapshot _lastVisibilitySnapshot;
        private bool _hasLastVisibilitySnapshot;

        // First-person head hiding: the head slots are routed to their own renderer so we
        // can toggle just the head's layer locally (body stays visible looking down).
        private UMARendererAsset _headRenderer;
        private bool _hideHead;
        private bool _hideBody;
        private bool _readinessHideBody;
        private bool _hideDuringSeatedTransition;
        private int _cullLayer;
        private string _avatarLoadTraceId;
        private double _avatarLoadStartedAt = -1d;
        private bool _avatarLoadPending;
        private double _umaBuildCharacterBegunAt = -1d;
        private double _umaRecipeUpdatedAt = -1d;
        private long _dcaBuildTicksAtStart;
        private long _dcaLoadTicksAtStart;
        private long _dcaInitializeTicksAtStart;
        private long _dcaPhase1TicksAtStart;
        private long _dcaPhase2TicksAtStart;
        private long _dcaPhase3TicksAtStart;
        private long _dcaPhase4TicksAtStart;
        private long _dcaLoadPhase1TicksAtStart;
        private long _dcaLoadPhase2TicksAtStart;
        private long _dcaLoadPhase3TicksAtStart;
        private long _dcaLoadPhase4TicksAtStart;

        private bool IsVrEmbodiment =>
            _rigInput != null
            && _rigInput.IsVrStyle
            && _rigInput.Body != null;

        private bool IsLocalVrEmbodiment =>
            IsVrEmbodiment
            && _rigInput.IsLocalAuthority;

        private bool IsTrackedLocalVrEmbodiment =>
            IsLocalVrEmbodiment && IsHeadTracked();

        private void Awake()
        {
            _inputConverter = GetComponent<AvatarInputConverter>();
            _playerSetup = GetComponent<PlayerNetworkSetup>();
            _vertexRigInput = new VertexFormHumanoidRigInput(_inputConverter, _playerSetup);
            _rigInput = _vertexRigInput;
            _characterController = GetComponent<CharacterController>();
            _locomotionDriver.Reset(transform.position);
        }

        /// <summary>Applies provider-neutral posture; lower-body animation remains authoritative.</summary>
        public void SetPosture(HumanoidPosture posture, Transform seatedPelvisTarget = null)
        {
            bool targetChanged = _seatedPelvisTarget != seatedPelvisTarget;
            bool enteringSitting = posture == HumanoidPosture.Sitting
                && _postureAnimator.Current != HumanoidPosture.Sitting;
            if (targetChanged)
                _seatedPelvisAlignmentLogged = false;
            _seatedPelvisTarget = posture == HumanoidPosture.Sitting ? seatedPelvisTarget : null;

            if (posture == HumanoidPosture.Standing)
                SetSeatedTransitionHidden(false);
            else if (_seatedPelvisTarget != null && (enteringSitting || targetChanged))
                SetSeatedTransitionHidden(true);

            _postureAnimator.Apply(posture);
        }

        /// <summary>Build (or rebuild) the avatar from a recipe. Safe to call repeatedly.</summary>
        public void Build(UmaAvatarCatalog catalog, AvatarWireRecipe recipe)
        {
            _catalog = catalog;
            _lastRecipe = recipe;
            RaceData race = catalog != null ? catalog.Race(recipe.raceId) : null;
            if (catalog == null || !catalog.IsHumanoidRace(recipe.raceId))
            {
                Debug.LogError(catalog == null
                    ? "[UmaAvatarPuppet] Avatar catalog is not assigned. Rerun Install UMA Provider Layer to wire the default catalog before building an avatar."
                    : $"[UmaAvatarPuppet] Catalog '{catalog.name}', race ID {recipe.raceId}: race={(race != null ? race.raceName : "missing")}, target={(race != null ? race.umaTarget.ToString() : "missing")}, T-pose={race != null && race.TPose != null}, base recipe={race != null && race.baseRaceRecipe != null}. A complete Humanoid definition is required.");
                return;
            }

            bool firstBuild = _dca == null;
            BeginAvatarLoadTiming(recipe, race, firstBuild);
            if (firstBuild)
                CreateAvatarObject(race);

            bool raceChanged = !firstBuild && _dca.activeRace.name != race.raceName;
            if (raceChanged)
            {
                _dca.BuildCharacterEnabled = false;
                _dca.cacheCurrentState = false; // The GHA wire recipe is authoritative.
                _dca.ChangeRace(race, DynamicCharacterAvatar.ChangeRaceOptions.keepBodyColors);
            }

            if (!firstBuild)
                _dca.ClearSlots();

            int applied = 0;
            if (recipe.wardrobeIds != null)
            {
                foreach (int id in recipe.wardrobeIds)
                {
                    UMAWardrobeRecipe wardrobe = catalog.Wardrobe(id);
                    if (catalog.IsWardrobeCompatible(recipe.raceId, wardrobe))
                    {
                        if (_dca.SetSlot(wardrobe)) applied++;
                    }
                }
            }

            // DNA and complete UMA 3 color data are staged so the DCA bakes them during
            // both its initial Start build and later rebuilds.
            ApplyDna(recipe);
            StageColors();
            LogAvatarLoadPhase(
                "HOST_CONFIGURED",
                $"firstBuild={firstBuild} active={_dcaRoot.activeInHierarchy} enabled={_dca.enabled}");

            // First build: configure and let the DCA's own Start() build — calling
            // BuildCharacter before Start drops the wardrobe (hard-won lesson).
            if (raceChanged)
                _dca.BuildCharacterEnabled = true; // Enabling builds the fully staged recipe once.
            else if (!firstBuild)
                _dca.BuildCharacter(true);

            Debug.Log($"[UmaAvatarPuppet] {(firstBuild ? "Configured initial" : "Rebuilt")} avatar on '{gameObject.name}' — race '{race.raceName}', {applied} wardrobe item(s), {(recipe.dna?.Count ?? 0)} dna, {(recipe.colors?.Count ?? 0)} color(s).");
        }

        /// <summary>Loads the recipe's body-shape DNA into predefinedDNA so the build applies it.</summary>
        private void ApplyDna(AvatarWireRecipe recipe)
        {
            if (_dca == null || _catalog == null || recipe.dna == null || recipe.dna.Count == 0)
                return;
            var pre = new UMAPredefinedDNA();
            foreach (DnaValue d in recipe.dna)
            {
                string dnaName = _catalog.DnaName(d.id);
                if (!string.IsNullOrEmpty(dnaName))
                    pre.AddDNA(dnaName, d.value / 255f);
            }
            _dca.predefinedDNA = pre;
            _dca.keepPredefinedDNA = true; // survive UMA's internal rebuilds
        }

        /// <summary>Stages the recipe's complete UMA 3 color data for the next build.</summary>
        private void StageColors()
        {
            if (_dca == null || _catalog == null || _lastRecipe.colors == null || _lastRecipe.colors.Count == 0)
                return;
            foreach (ColorValue c in _lastRecipe.colors)
            {
                UmaAvatarCatalog.ColorChannelDef ch = _catalog.ColorChannel(c.channelId);
                OverlayColorData swatch = _catalog.ColorSwatch(c.channelId, c.paletteIndex);
                if (ch == null || string.IsNullOrEmpty(ch.channelName) || swatch == null)
                    continue;
                // UMA 3 color tables can carry shader properties (hair root/tip colors).
                // SetColor only copies the legacy color fields; SetRawColor preserves all
                // OverlayColorData properties for the generation pass.
                _dca.SetRawColor(ch.channelName, swatch, false);
            }
        }

        /// <summary>
        /// Destroys the built avatar, if any. Safe to call repeatedly; a later Build()
        /// recreates from scratch. Used when the player switches to the stock avatar system.
        /// </summary>
        public void Teardown()
        {
            CancelAvatarLoadTiming("TEARDOWN");
            if (_dcaRoot != null)
            {
                // Destroy is deferred until frame end. Hide the outgoing provider now.
                _dcaRoot.SetActive(false);
                Destroy(_dcaRoot);
            }
            _dcaRoot = null;
            _dca = null;
            _avatarAnimator = null;
            _postureAnimator.Bind(null);
            _headBone = null;
            _leftEyeBone = null;
            _rightEyeBone = null;
            _headAnimationFallbackLocalRotation = Quaternion.identity;
            _hasHeadAnimationFallback = false;
            _humanoidArmRig.Dispose();
            _nextVrArmIkRetryTime = 0f;
            _nextVrAlignmentLogTime = 0f;
            _trackingLifecycle.Reset(false);
            _vrAlignment.Reset();
            _seatedPelvisAlignment.Reset();
            _seatedPelvisTarget = null;
            _seatedPelvisAlignmentLogged = false;
            _hideDuringSeatedTransition = false;
            _locomotionDriver.Reset(transform.position);
        }

        private void CreateAvatarObject(RaceData race)
        {
            _dcaRoot = new GameObject("GHA_UMA_Avatar");
            AttachAvatarRoot();

            // Splitter renderer for the head slots — keep its layer at the default (0).
            // The cull layer is applied at runtime to the resulting renderer, never baked
            // into the asset (that would hide remote players' heads for everyone).
            if (_headRenderer == null)
                _headRenderer = ScriptableObject.CreateInstance<UMARendererAsset>();

            _dca = _dcaRoot.AddComponent<DynamicCharacterAvatar>();
            _dca.activeRace.name = race.raceName;
            _dca.activeRace.data = race;
            _dca.loadFileOnStart = false;
            _dca.cacheCurrentState = false;
            _dca.RecreateAnimatorOnRaceChange = true;
            if (animationController != null)
                _dca.animationController = animationController;

            // Serialized event fields are null on AddComponent-created DCAs.
            if (_dca.CharacterBegun == null)
                _dca.CharacterBegun = new UMADataEvent();
            _dca.CharacterBegun.AddListener(OnCharacterBegun);
            if (_dca.CharacterCreated == null)
                _dca.CharacterCreated = new UMADataEvent();
            _dca.CharacterCreated.AddListener(OnCharacterCreated);
            if (_dca.CharacterStart == null)
                _dca.CharacterStart = new UMACharacterEvent();
            _dca.CharacterStart.AddListener(OnDcaCharacterStart);
            if (_dca.BuildCharacterBegun == null)
                _dca.BuildCharacterBegun = new UMADataEvent();
            _dca.BuildCharacterBegun.AddListener(OnDcaBuildCharacterBegun);
            if (_dca.RecipeUpdated == null)
                _dca.RecipeUpdated = new UMADataEvent();
            _dca.RecipeUpdated.AddListener(OnDcaRecipeUpdated);
        }

        private void OnDcaCharacterStart(DynamicCharacterAvatar avatar)
        {
            LogAvatarLoadPhase(
                "UMA_DCA_START_INITIALIZED",
                $"active={avatar != null && avatar.gameObject.activeInHierarchy} " +
                $"enabled={avatar != null && avatar.enabled} " +
                $"buildEnabled={avatar != null && avatar.BuildCharacterEnabled}");
        }

        private void OnDcaBuildCharacterBegun(UMAData umaData)
        {
            _umaBuildCharacterBegunAt = AvatarLoadTimingLog.Now;
            _umaRecipeUpdatedAt = -1d;
            _dcaBuildTicksAtStart = DynamicCharacterAvatar.Ticks_BuildCharacter;
            _dcaLoadTicksAtStart = DynamicCharacterAvatar.Ticks_LoadCharacter;
            _dcaInitializeTicksAtStart = DynamicCharacterAvatar.Ticks_InitializeBuild;
            _dcaPhase1TicksAtStart = DynamicCharacterAvatar.Ticks_Phase1;
            _dcaPhase2TicksAtStart = DynamicCharacterAvatar.Ticks_Phase2;
            _dcaPhase3TicksAtStart = DynamicCharacterAvatar.Ticks_Phase3;
            _dcaPhase4TicksAtStart = DynamicCharacterAvatar.Ticks_Phase4;
            _dcaLoadPhase1TicksAtStart = DynamicCharacterAvatar.Ticks_LoadPhase1;
            _dcaLoadPhase2TicksAtStart = DynamicCharacterAvatar.Ticks_LoadPhase2;
            _dcaLoadPhase3TicksAtStart = DynamicCharacterAvatar.Ticks_LoadPhase3;
            _dcaLoadPhase4TicksAtStart = DynamicCharacterAvatar.Ticks_LoadPhase4;

            LogAvatarLoadPhase(
                "UMA_BUILD_CHARACTER_BEGUN",
                $"slotCount={RecipeSlotCount(umaData)}");
        }

        private void OnDcaRecipeUpdated(UMAData umaData)
        {
            _umaRecipeUpdatedAt = AvatarLoadTimingLog.Now;
            double buildToRecipeMs = _umaBuildCharacterBegunAt >= 0d
                ? (_umaRecipeUpdatedAt - _umaBuildCharacterBegunAt) * 1000d
                : -1d;
            LogAvatarLoadPhase(
                "UMA_RECIPE_UPDATED",
                $"buildToRecipeMs={buildToRecipeMs:F1} slotCount={RecipeSlotCount(umaData)}");
        }

        /// <summary>
        /// Set first-person visibility. The head slots live on their own renderer, so the
        /// head can be hidden from the local camera while the body stays visible (looking
        /// down) and mirrors/remote players are unaffected. hideBody hides everything
        /// (e.g. VR until arm IK lands). cullLayer is the layer the local camera culls but
        /// mirror cameras still render. Cheap to call every frame.
        /// </summary>
        public void SetVisibility(bool hideHead, bool hideBody, int cullLayer)
        {
            if (_hideHead == hideHead && _hideBody == hideBody && _cullLayer == cullLayer)
                return;
            _hideHead = hideHead;
            _hideBody = hideBody;
            _cullLayer = cullLayer;
            ApplyVisibility();
        }

        public void SetFirstPersonVisibility(AvatarVisibility visibility, int cullLayer)
        {
            SetVisibility(
                hideHead: visibility != AvatarVisibility.Visible,
                hideBody: visibility == AvatarVisibility.WholeBody,
                cullLayer);
        }

        public bool TryGetRendererBounds(out Bounds bounds)
        {
            bounds = default;
            if (_dcaRoot == null)
                return false;

            Renderer[] renderers = _dcaRoot.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }

        /// <summary>Routes the head slots (+ eyes/lashes/mouth/hair) onto the head renderer before mesh combine.</summary>
        private void OnCharacterBegun(UMAData umaData)
        {
            double now = AvatarLoadTimingLog.Now;
            double buildToBegunMs = _umaBuildCharacterBegunAt >= 0d
                ? (now - _umaBuildCharacterBegunAt) * 1000d
                : -1d;
            double recipeToBegunMs = _umaRecipeUpdatedAt >= 0d
                ? (now - _umaRecipeUpdatedAt) * 1000d
                : -1d;
            double buildCpuMs = TicksToMilliseconds(
                DynamicCharacterAvatar.Ticks_BuildCharacter - _dcaBuildTicksAtStart);
            double loadCpuMs = TicksToMilliseconds(
                DynamicCharacterAvatar.Ticks_LoadCharacter - _dcaLoadTicksAtStart);
            double unattributedMs = buildToBegunMs >= 0d
                ? Mathf.Max(0f, (float)(buildToBegunMs - buildCpuMs - loadCpuMs))
                : -1d;

            LogAvatarLoadPhase(
                "UMA_PRE_GENERATION_BREAKDOWN",
                $"buildToBegunMs={buildToBegunMs:F1} " +
                $"buildToRecipeMs={ElapsedMilliseconds(_umaBuildCharacterBegunAt, _umaRecipeUpdatedAt):F1} " +
                $"recipeToBegunMs={recipeToBegunMs:F1} " +
                $"dcaBuildCpuMs={buildCpuMs:F1} dcaLoadCpuMs={loadCpuMs:F1} " +
                $"unattributedWaitOrPreprocessMs={unattributedMs:F1} " +
                $"buildPhasesMs=[init:{DcaDeltaMs(DynamicCharacterAvatar.Ticks_InitializeBuild, _dcaInitializeTicksAtStart):F1}," +
                $"p1:{DcaDeltaMs(DynamicCharacterAvatar.Ticks_Phase1, _dcaPhase1TicksAtStart):F1}," +
                $"p2:{DcaDeltaMs(DynamicCharacterAvatar.Ticks_Phase2, _dcaPhase2TicksAtStart):F1}," +
                $"p3:{DcaDeltaMs(DynamicCharacterAvatar.Ticks_Phase3, _dcaPhase3TicksAtStart):F1}," +
                $"p4:{DcaDeltaMs(DynamicCharacterAvatar.Ticks_Phase4, _dcaPhase4TicksAtStart):F1}] " +
                $"loadPhasesMs=[p1:{DcaDeltaMs(DynamicCharacterAvatar.Ticks_LoadPhase1, _dcaLoadPhase1TicksAtStart):F1}," +
                $"p2:{DcaDeltaMs(DynamicCharacterAvatar.Ticks_LoadPhase2, _dcaLoadPhase2TicksAtStart):F1}," +
                $"p3:{DcaDeltaMs(DynamicCharacterAvatar.Ticks_LoadPhase3, _dcaLoadPhase3TicksAtStart):F1}," +
                $"p4:{DcaDeltaMs(DynamicCharacterAvatar.Ticks_LoadPhase4, _dcaLoadPhase4TicksAtStart):F1}]");
            LogAvatarLoadPhase(
                "UMA_CHARACTER_BEGUN",
                $"rendererCount={(umaData != null ? umaData.RendererCount : 0)}");
            if (_headRenderer == null || umaData == null || umaData.umaRecipe == null)
                return;
            SlotData[] slots = umaData.umaRecipe.slotDataList;
            if (slots == null) return;
            foreach (SlotData slot in slots)
            {
                if (slot == null || string.IsNullOrEmpty(slot.slotName)) continue;
                if (slot.HasTag("Head") || IsHeadAuxSlot(slot.slotName))
                    slot.rendererAsset = _headRenderer;
            }
        }

        private static int RecipeSlotCount(UMAData umaData)
        {
            return umaData?.umaRecipe?.slotDataList?.Length ?? 0;
        }

        private static double ElapsedMilliseconds(double startedAt, double finishedAt)
        {
            return startedAt >= 0d && finishedAt >= startedAt
                ? (finishedAt - startedAt) * 1000d
                : -1d;
        }

        private static double DcaDeltaMs(long currentTicks, long startingTicks)
        {
            return TicksToMilliseconds(currentTicks - startingTicks);
        }

        private static double TicksToMilliseconds(long ticks)
        {
            return ticks <= 0L
                ? 0d
                : ticks * 1000d / System.Diagnostics.Stopwatch.Frequency;
        }

        // The body UDIM head tiles carry the "Head" tag, but eyes/eyelashes/inner-mouth
        // tag "Head" on their overlays (not the slot) and hair is a wardrobe slot — so
        // match those by name. (Verified against BaseRaceRecipe_HumanMale30.)
        private static bool IsHeadAuxSlot(string slotName)
        {
            return slotName.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) >= 0
                || slotName.IndexOf("Mouth", System.StringComparison.OrdinalIgnoreCase) >= 0
                || slotName.IndexOf("Hair", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnCharacterCreated(UMAData data)
        {
            LogAvatarLoadPhase(
                "UMA_CHARACTER_CREATED",
                $"rendererCount={(data != null ? data.RendererCount : 0)}");
            // UMA recreates the Animator on each build, so re-fetch it here.
            _avatarAnimator = _dca != null ? _dca.GetComponent<Animator>() : null;
            _postureAnimator.Bind(_avatarAnimator);
            CacheHumanoidBones();
            SetupVrArmIk();
            _postureAnimator.Reapply();
            if (!VrArmIkReady)
                _nextVrArmIkRetryTime = Time.time + 0.25f;
            bool headTracked = IsHeadTracked();
            _trackingLifecycle.SynchronizeTracking(headTracked);
            _humanoidArmRig.SetArmTrackingActive(IsVrEmbodiment && (!IsLocalVrEmbodiment || headTracked));
            if (autoScaleVrAvatarToPlayerHeight && IsLocalVrEmbodiment && _dcaRoot != null)
                _dcaRoot.transform.localScale = Vector3.one;
            _vrAlignment.Reset();
            _readinessHideBody = IsLocalVrEmbodiment;
            if (_postureAnimator.Current == HumanoidPosture.Sitting && _seatedPelvisTarget != null)
            {
                _seatedPelvisAlignmentLogged = false;
                _hideDuringSeatedTransition = true;
            }
            // Rebuilds spawn fresh renderers on the default layer — re-apply visibility.
            ApplyVisibility();
            HumanoidRebuilt?.Invoke();
            LogVrAlignmentSnapshot("character-created", true);
            FinishAvatarLoadTiming(
                "SUCCESS",
                $"rendererCount={(data != null ? data.RendererCount : 0)} animatorReady={_avatarAnimator != null}");
        }

        private void BeginAvatarLoadTiming(AvatarWireRecipe recipe, RaceData race, bool firstBuild)
        {
            if (_avatarLoadPending)
                CancelAvatarLoadTiming("SUPERSEDED");

            _avatarLoadTraceId = AvatarLoadTimingLog.NewTraceId("avatar");
            _avatarLoadStartedAt = AvatarLoadTimingLog.Now;
            _avatarLoadPending = true;
            AvatarLoadTimingLog.Write(
                _avatarLoadTraceId,
                "avatar",
                "START",
                LoadTimingHost,
                _avatarLoadStartedAt,
                $"object='{gameObject.name}' build={(firstBuild ? "initial" : "rebuild")} " +
                $"race='{race.raceName}' wardrobe={recipe.wardrobeIds?.Count ?? 0} " +
                $"dna={recipe.dna?.Count ?? 0} colors={recipe.colors?.Count ?? 0}");
        }

        private void LogAvatarLoadPhase(string phase, string details = null)
        {
            if (!_avatarLoadPending)
                return;

            AvatarLoadTimingLog.Write(
                _avatarLoadTraceId,
                "avatar",
                phase,
                LoadTimingHost,
                _avatarLoadStartedAt,
                $"object='{gameObject.name}'{(string.IsNullOrEmpty(details) ? string.Empty : $" {details}")}");
        }

        private void FinishAvatarLoadTiming(string result, string details = null)
        {
            if (!_avatarLoadPending)
                return;

            AvatarLoadTimingLog.Write(
                _avatarLoadTraceId,
                "avatar",
                "FINISH",
                LoadTimingHost,
                _avatarLoadStartedAt,
                $"object='{gameObject.name}' result={result}" +
                $"{(string.IsNullOrEmpty(details) ? string.Empty : $" {details}")}");
            _avatarLoadPending = false;
            _avatarLoadStartedAt = -1d;
        }

        private void CancelAvatarLoadTiming(string reason)
        {
            if (!_avatarLoadPending)
                return;

            AvatarLoadTimingLog.Write(
                _avatarLoadTraceId,
                "avatar",
                "CANCELLED",
                LoadTimingHost,
                _avatarLoadStartedAt,
                $"object='{gameObject.name}' reason={reason}");
            _avatarLoadPending = false;
            _avatarLoadStartedAt = -1d;
        }

        private void OnDestroy()
        {
            CancelAvatarLoadTiming("OBJECT_DESTROYED");
        }

        /// <summary>
        /// Applies the current hide state per-renderer: the head renderer goes to the cull
        /// layer when the head (or whole body) is hidden; body renderers only when the body
        /// is hidden. Queried live from UMAData so it survives rebuilds.
        /// </summary>
        private void ApplyVisibility()
        {
            if (_dca == null || _dca.umaData == null) return;
            UMAData umaData = _dca.umaData;
            int count = umaData.RendererCount;
            bool matchedHead = false;
            int headRendererCount = 0;
            int hiddenRendererCount = 0;
            for (int i = 0; i < count; i++)
            {
                SkinnedMeshRenderer smr = umaData.GetRenderer(i);
                if (smr == null) continue;
                bool isHead = _headRenderer != null && umaData.GetRendererAsset(i) == _headRenderer;
                if (isHead)
                {
                    matchedHead = true;
                    headRendererCount++;
                }
                bool effectiveHideBody = _hideBody || _readinessHideBody;
                bool hide = isHead ? (_hideHead || effectiveHideBody) : effectiveHideBody;
                smr.gameObject.layer = hide ? _cullLayer : 0;
                // Provider-specific execution of the generic seated-transition policy.
                // forceRenderingOff preserves each renderer's authored enabled state.
                smr.forceRenderingOff = _hideDuringSeatedTransition;
                if (hide)
                    hiddenRendererCount++;
            }
            if (_hideHead && !_hideBody && !matchedHead)
                Debug.LogWarning("[UmaAvatarPuppet] Head-only hide requested but no head renderer was produced — head slots may not have routed (race without 'Head' tags?).");

            if (debugVrEmbodimentAlignment && IsLocalVrEmbodiment)
            {
                var snapshot = new VrVisibilityDebugSnapshot(
                    _hideHead,
                    _hideBody,
                    _cullLayer,
                    count,
                    headRendererCount,
                    hiddenRendererCount,
                    matchedHead,
                    VrArmIkReady);
                if (!_hasLastVisibilitySnapshot || !snapshot.Equals(_lastVisibilitySnapshot))
                {
                    _hasLastVisibilitySnapshot = true;
                    _lastVisibilitySnapshot = snapshot;
                    Debug.LogWarning(
                        $"[GHA VR VIS] '{gameObject.name}' hideHead={_hideHead} hideBody={_hideBody} cullLayer={_cullLayer} " +
                        $"rendererCount={count} headRendererCount={headRendererCount} hiddenRendererCount={hiddenRendererCount} " +
                        $"matchedHead={matchedHead} vrArmIk={VrArmIkReady}");
                }
            }
        }

        private void LateUpdate()
        {
            if (_dcaRoot == null) return;

            _seatedPelvisAlignment.ClearPreviousOffset(_dcaRoot.transform);
            AttachAvatarRoot();
            UpdateTrackingLifecycle();
            RetryVrArmIkSetup();
            AlignVrAvatarUnderTrackedHead();
            UpdateVrReadinessVisibility();
            UpdateLocomotionAnimation();
            UpdateVrIkTargets();
            UpdateFacing();
            AlignSeatedPelvis();
            UpdateVrTrackedBones();
        }

        private void AlignSeatedPelvis()
        {
            if (!_seatedPelvisAlignment.TryAlign(
                    _dcaRoot.transform,
                    _avatarAnimator,
                    _seatedPelvisTarget,
                    _postureAnimator.Current,
                    seatedLoopStateName,
                    out Vector3 correction))
            {
                return;
            }

            if (_seatedPelvisAlignmentLogged)
                return;

            bool correctedLocalView = _rigInput != null
                && _rigInput.IsLocalAuthority
                && _rigInput.TryApplySeatedViewCorrection(correction);
            _seatedPelvisAlignmentLogged = true;
            SetSeatedTransitionHidden(false);
            Debug.Log(
                $"[GHA SIT ALIGN] Aligned humanoid hips on '{gameObject.name}' to " +
                $"'{_seatedPelvisTarget.name}' with visual-root correction {correction:F3} " +
                $"and target-forward yaw {_seatedPelvisTarget.rotation.eulerAngles.y:F1} degrees; " +
                $"correctedLocalView={correctedLocalView}.");
        }

        private void SetSeatedTransitionHidden(bool hidden)
        {
            if (_hideDuringSeatedTransition == hidden)
                return;

            _hideDuringSeatedTransition = hidden;
            ApplyVisibility();
            Debug.Log(
                $"[GHA SIT VIS] Avatar renderers on '{gameObject.name}' are now " +
                $"{(hidden ? "hidden while the seated pose settles" : "visible at the final seated pose")}.");
        }

        private void UpdateLocomotionAnimation()
        {
            if (_postureAnimator.Current == HumanoidPosture.Sitting)
            {
                _locomotionDriver.Reset(transform.position);
                return;
            }

            _locomotionDriver.Update(transform.position, _avatarAnimator, animatorMaxSpeed, Time.deltaTime);
        }

        private void UpdateVrReadinessVisibility()
        {
            bool headTracked = IsHeadTracked();
            bool shouldHide = IsLocalVrEmbodiment
                && headTracked
                && (!VrArmIkReady || !_vrAlignment.IsReady);
            if (_readinessHideBody == shouldHide)
                return;

            _readinessHideBody = shouldHide;
            ApplyVisibility();
            Debug.Log(
                $"[UmaAvatarPuppet] Local VR embodiment readiness on '{gameObject.name}': " +
                $"ready={!shouldHide} tracked={headTracked} ik={VrArmIkReady} " +
                $"scaleReady={_vrAlignment.IsCalibrated} floorReady={_vrAlignment.IsFloorReady} " +
                $"sessionEyeHeight={_vrAlignment.SessionEyeHeight:F3} floorY={_vrAlignment.FloorY:F3}.");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                QueueTrackingReinitialize("application focus");
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                QueueTrackingReinitialize("application resume");
        }

        private void UpdateTrackingLifecycle()
        {
            if (!IsLocalVrEmbodiment)
                return;

            HumanoidTrackingLifecycleUpdate trackingUpdate = _trackingLifecycle.Update(
                IsHeadTracked(),
                Time.time,
                trackingResumeReinitializeDelay);

            if (trackingUpdate.TrackingRegained)
            {
                Debug.Log($"[UmaAvatarPuppet] Queued VR tracking reinitialize on '{gameObject.name}' after HMD tracking regained.");
            }
            else if (trackingUpdate.TrackingLost)
            {
                ReleaseTrackedPoseToAnimator();
                _readinessHideBody = false;
                ApplyVisibility();
                Debug.Log($"[UmaAvatarPuppet] VR tracking lost on '{gameObject.name}' - the {_postureAnimator.Current} Animator posture now owns the full humanoid pose while the headset sleeps.");
            }

            if (trackingUpdate.ShouldReinitialize)
                ReinitializeVrTrackingState();
        }

        private void QueueTrackingReinitialize(string reason)
        {
            if (!IsLocalVrEmbodiment)
                return;

            _trackingLifecycle.QueueReinitialize(Time.time, trackingResumeReinitializeDelay);
            Debug.Log($"[UmaAvatarPuppet] Queued VR tracking reinitialize on '{gameObject.name}' after {reason}.");
        }

        private void ReleaseTrackedPoseToAnimator()
        {
            _humanoidArmRig.SetArmTrackingActive(false);

            // The tracked head is written after normal Animator evaluation in LateUpdate.
            // Restore a known animation-owned local pose immediately, then evaluate the
            // current Animator state once with arm IK disabled so the full rig snaps back.
            if (_headBone != null && _hasHeadAnimationFallback)
                _headBone.localRotation = _headAnimationFallbackLocalRotation;
            if (_avatarAnimator != null && _avatarAnimator.isActiveAndEnabled)
            {
                _postureAnimator.Reapply();
                _avatarAnimator.Update(0f);
            }
        }

        private void ReinitializeVrTrackingState()
        {
            _locomotionDriver.Reset(transform.position);
            _vrAlignment.ResetSceneFloor();

            _avatarAnimator = _dca != null ? _dca.GetComponent<Animator>() : null;
            _postureAnimator.Bind(_avatarAnimator);
            CacheHumanoidBones();
            SetupVrArmIk();
            _postureAnimator.Reapply();
            AttachAvatarRoot();
            AlignVrAvatarUnderTrackedHead();
            UpdateVrIkTargets();
            LogVrAlignmentSnapshot("tracking-reinitialize", true);

            Debug.Log($"[UmaAvatarPuppet] Reinitialized VR tracking state on '{gameObject.name}' after HMD resume/focus.");
        }

        private bool IsHeadTracked() => _rigInput != null && _rigInput.IsHeadTracked;

        private void UpdateFacing()
        {
            if (IsVrEmbodiment)
                return;

            Quaternion target;
            float turnSpeed;

            if (_locomotionDriver.SmoothedSpeed > faceMovementThreshold && _locomotionDriver.MoveDirection.sqrMagnitude > 0.5f)
            {
                target = Quaternion.LookRotation(_locomotionDriver.MoveDirection, Vector3.up);
                turnSpeed = movingTurnSpeed;
            }
            else if (_rigInput != null && _rigInput.Body != null)
            {
                target = Quaternion.Euler(0f, _rigInput.Body.rotation.eulerAngles.y, 0f);
                turnSpeed = idleTurnSpeed;
            }
            else
            {
                return;
            }

            _dcaRoot.transform.rotation = Quaternion.RotateTowards(_dcaRoot.transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        private void CacheHumanoidBones()
        {
            Transform previousHeadBone = _headBone;
            _headBone = null;
            _leftEyeBone = null;
            _rightEyeBone = null;
            if (_avatarAnimator == null || !_avatarAnimator.isHuman)
                return;

            _headBone = _avatarAnimator.GetBoneTransform(HumanBodyBones.Head);
            _leftEyeBone = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftEye);
            _rightEyeBone = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightEye);

            // Capture once per generated skeleton, before tracked-head LateUpdate ownership.
            // Reinitializing the same skeleton must not replace this with a tracked rotation.
            if (_headBone != null
                && (_headBone != previousHeadBone || !_hasHeadAnimationFallback))
            {
                _headAnimationFallbackLocalRotation = _headBone.localRotation;
                _hasHeadAnimationFallback = true;
            }
        }

        private void SetupVrArmIk()
        {
            // Desktop rigs still expose hand transforms, but those must never override
            // the humanoid Animator. Only VR avatars may create tracking constraints.
            if (!IsVrEmbodiment || _dcaRoot == null)
            {
                _humanoidArmRig.Dispose();
                return;
            }

            bool ready = _humanoidArmRig.Bind(
                _dcaRoot,
                _avatarAnimator,
                _rigInput.LeftHand,
                _rigInput.RightHand);
            if (!ready)
                Debug.LogWarning("[UmaAvatarPuppet] Failed to build VR arm IK rig; local VR self-view will keep hiding the whole avatar.");
            else
            {
                Debug.Log($"[UmaAvatarPuppet] VR arm IK ready on '{gameObject.name}' using target proxies for '{_rigInput.LeftHand.name}' and '{_rigInput.RightHand.name}'.");
                Debug.Log($"[UmaAvatarPuppet] VR wrist offsets on '{gameObject.name}' tracked L/R={leftTrackedHandWristRotationOffset}/{rightTrackedHandWristRotationOffset}, controller L/R={leftControllerWristRotationOffset}/{rightControllerWristRotationOffset}.");
            }
        }

        private void RetryVrArmIkSetup()
        {
            if (VrArmIkReady || !IsVrEmbodiment || Time.time < _nextVrArmIkRetryTime)
                return;

            _avatarAnimator = _dca != null ? _dca.GetComponent<Animator>() : null;
            _postureAnimator.Bind(_avatarAnimator);
            CacheHumanoidBones();
            SetupVrArmIk();
            _postureAnimator.Reapply();
            _nextVrArmIkRetryTime = Time.time + 0.5f;
        }

        private void UpdateVrTrackedBones()
        {
            if (!driveVrHeadBone || !IsVrEmbodiment)
                return;
            if (IsLocalVrEmbodiment && !IsHeadTracked())
                return;
            if (_rigInput == null || _rigInput.HeadTarget == null || _headBone == null)
                return;

            // AvatarInputConverter already owns the local HMD -> AvatarHead path and
            // MultiplayerVRSynchronization syncs that target for remotes. Apply it after
            // root facing so the bone receives the final world-space HMD rotation.
            _headBone.rotation = _rigInput.HeadTarget.rotation;
        }

        private void AttachAvatarRoot()
        {
            if (_dcaRoot == null) return;

            if (IsVrEmbodiment)
            {
                if (_dcaRoot.transform.parent != transform)
                    _dcaRoot.transform.SetParent(transform, true);
                return;
            }

            if (_dcaRoot.transform.parent != transform)
                _dcaRoot.transform.SetParent(transform, false);
            if (_characterController == null) return;
            // Non-XR rigs rest the CharacterController's contact surface one skin width
            // below its geometric capsule bottom. Footwear commonly extends slightly below
            // the UMA root, so retain a small visual clearance above that contact plane.
            float bottomY = _characterController.center.y
                - _characterController.height * 0.5f
                - _characterController.skinWidth
                + Mathf.Max(0f, flatAvatarGroundClearance);
            Vector3 lp = _dcaRoot.transform.localPosition;
            if (!Mathf.Approximately(lp.y, bottomY))
                _dcaRoot.transform.localPosition = new Vector3(0f, bottomY, 0f);
        }

        private void AlignVrAvatarUnderTrackedHead()
        {
            if (!IsVrEmbodiment)
                return;

            Transform bodyAnchor = _rigInput.Body != null ? _rigInput.Body : _rigInput.MainAvatar;
            Transform trackedHead = _rigInput.TrackedHead;
            bool calibrated = _vrAlignment.Align(
                _dcaRoot.transform,
                _headBone,
                _leftEyeBone,
                _rightEyeBone,
                _rigInput,
                VrGroundY(),
                vrAvatarRootOffset,
                vrTrackedHeadMatchOffset,
                vrHeadBoneToEyeForwardOffset,
                vrHeadBoneToEyeUpOffset,
                autoScaleVrAvatarToPlayerHeight,
                standingCalibrationMinEyeHeight,
                standingCalibrationHoldSeconds,
                standingCalibrationMaxEyeHeightDrift,
                minVrAvatarAutoScale,
                maxVrAvatarAutoScale,
                Time.time,
                out float playerEyeHeight,
                out float targetScale);
            if (calibrated)
            {
                Debug.Log(
                    $"[UmaAvatarPuppet] Locked VR height calibration on '{gameObject.name}': " +
                    $"eyeHeight={playerEyeHeight:F3}m avatarScale={targetScale:F3} " +
                    $"eyeLandmark={(_leftEyeBone != null || _rightEyeBone != null ? "humanoid-eye-bones" : "head-offset-fallback")}.");
            }

            LogVrAlignment("late-update", trackedHead, bodyAnchor, false);
        }

        private void UpdateVrIkTargets()
        {
            if (!IsVrEmbodiment)
            {
                // Also release an existing rig when leaving VR (for example in a web
                // session), not just when the avatar is first built on desktop.
                if (VrArmIkReady)
                    ReleaseTrackedPoseToAnimator();
                _humanoidArmRig.Dispose();
                return;
            }

            if (!VrArmIkReady || _rigInput == null)
                return;

            bool driveTrackedArms = !IsLocalVrEmbodiment || IsHeadTracked();
            _humanoidArmRig.SetArmTrackingActive(driveTrackedArms);
            if (driveTrackedArms)
            {
                _humanoidArmRig.UpdateTargets(
                    _rigInput.LeftHand,
                    _rigInput.RightHand,
                    LeftWristRotationOffset(),
                    RightWristRotationOffset());
            }

            // Lower-body ownership stays exclusively with the humanoid Animator.
            // HumanoidArmRig contains upper-body constraints only.
        }

        private Vector3 LeftWristRotationOffset()
        {
            return _rigInput != null && _rigInput.IsHandTracking
                ? leftTrackedHandWristRotationOffset
                : leftControllerWristRotationOffset;
        }

        private Vector3 RightWristRotationOffset()
        {
            return _rigInput != null && _rigInput.IsHandTracking
                ? rightTrackedHandWristRotationOffset
                : rightControllerWristRotationOffset;
        }

        private void LogVrAlignmentSnapshot(string reason, bool force)
        {
            if (!IsVrEmbodiment)
                return;

            Transform bodyAnchor = _rigInput.Body != null ? _rigInput.Body : _rigInput.MainAvatar;
            Transform trackedHead = _rigInput.TrackedHead;
            LogVrAlignment(reason, trackedHead, bodyAnchor, force);
        }

        private void LogVrAlignment(string reason, Transform trackedHead, Transform bodyAnchor, bool force)
        {
            if (!debugVrEmbodimentAlignment || _synclessRemoteOrNotLocal())
                return;
            Vector3 headOffset = _headBone != null && trackedHead != null ? _headBone.position - trackedHead.position : Vector3.zero;
            Vector3 leftOffset = Vector3.zero;
            Vector3 rightOffset = Vector3.zero;
            Vector3 leftRotDelta = Vector3.zero;
            Vector3 rightRotDelta = Vector3.zero;
            if (_humanoidArmRig.LeftHand != null && _rigInput.LeftHand != null)
            {
                leftOffset = _humanoidArmRig.LeftHand.position - _rigInput.LeftHand.position;
                leftRotDelta = (Quaternion.Inverse(_rigInput.LeftHand.rotation) * _humanoidArmRig.LeftHand.rotation).eulerAngles;
            }
            if (_humanoidArmRig.RightHand != null && _rigInput.RightHand != null)
            {
                rightOffset = _humanoidArmRig.RightHand.position - _rigInput.RightHand.position;
                rightRotDelta = (Quaternion.Inverse(_rigInput.RightHand.rotation) * _humanoidArmRig.RightHand.rotation).eulerAngles;
            }

            Vector3 xrHeadPosition = trackedHead != null ? trackedHead.position : Vector3.zero;
            Vector3 avatarHeadPosition = _rigInput.HeadTarget != null ? _rigInput.HeadTarget.position : Vector3.zero;
            Vector3 avatarBodyPosition = _rigInput.Body != null ? _rigInput.Body.position : Vector3.zero;
            Vector3 mainAvatarPosition = _rigInput.MainAvatar != null ? _rigInput.MainAvatar.position : Vector3.zero;
            Vector3 bodyAnchorPosition = bodyAnchor != null ? bodyAnchor.position : Vector3.zero;
            Vector3 umaRootPosition = _dcaRoot != null ? _dcaRoot.transform.position : Vector3.zero;
            Vector3 avatarEyePoint = HumanoidVrAlignment.ResolveAvatarEyePoint(
                _dcaRoot != null ? _dcaRoot.transform : null,
                _headBone,
                _leftEyeBone,
                _rightEyeBone,
                vrHeadBoneToEyeForwardOffset,
                vrHeadBoneToEyeUpOffset);
            Vector3 playerRootPosition = transform.position;
            Vector3 ccBottom = CharacterControllerBottomWorld();
            float playerEyeHeight = trackedHead != null ? trackedHead.position.y - VrGroundY() : 0f;
            float trackingSpaceEyeHeight = _rigInput.TryGetTrackingSpaceEyeHeight(out float trackedEyeHeight)
                ? trackedEyeHeight
                : 0f;
            float avatarEyeHeight = _dcaRoot != null
                ? Mathf.Abs(avatarEyePoint.y - _dcaRoot.transform.position.y)
                : 0f;
            float avatarScale = _dcaRoot != null ? _dcaRoot.transform.localScale.y : 0f;
            float animatorPlaybackSpeed = _avatarAnimator != null ? _avatarAnimator.speed : 0f;
            AnimatorStateInfo animatorState = _avatarAnimator != null
                ? _avatarAnimator.GetCurrentAnimatorStateInfo(0)
                : default;
            bool animatorInTransition = _avatarAnimator != null && _avatarAnimator.IsInTransition(0);
            string animatorClipName = "none";
            if (_avatarAnimator != null)
            {
                AnimatorClipInfo[] clipInfo = _avatarAnimator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0 && clipInfo[0].clip != null)
                    animatorClipName = clipInfo[0].clip.name;
            }
            bool headTracked = IsHeadTracked();
            bool handTracking = _rigInput.IsHandTracking;
            float playerYaw = transform.rotation.eulerAngles.y;
            float bodyAnchorYaw = bodyAnchor != null ? bodyAnchor.rotation.eulerAngles.y : 0f;

            var snapshot = new VrAlignmentDebugSnapshot(
                headTracked,
                handTracking,
                VrArmIkReady,
                _hideHead,
                _hideBody,
                playerRootPosition,
                ccBottom,
                xrHeadPosition,
                avatarHeadPosition,
                avatarBodyPosition,
                mainAvatarPosition,
                bodyAnchorPosition,
                umaRootPosition,
                headOffset,
                leftOffset,
                rightOffset,
                playerYaw,
                bodyAnchorYaw);
            if (!ShouldLogAlignmentSnapshot(snapshot, force))
                return;
            _hasLastAlignmentSnapshot = true;
            _lastAlignmentSnapshot = snapshot;
            _nextVrAlignmentLogTime = Time.time + Mathf.Max(0.25f, debugVrEmbodimentLogInterval);

            Debug.LogWarning(
                $"[GHA VR ALIGN] reason={reason} player='{gameObject.name}' handTracking={handTracking} " +
                $"tracked={headTracked} ik={VrArmIkReady} hideHead={_hideHead} hideBody={_hideBody} locomotionSpeed={_locomotionDriver.SmoothedSpeed:F3} speedParam={_locomotionDriver.NormalizedSpeed:F3} speedScale={animatorMaxSpeed:F2} posture={_postureAnimator.Current} postureParam={_postureAnimator.AnimatorPostureValue} animatorPlayback={animatorPlaybackSpeed:F2} stateHash={animatorState.fullPathHash} stateTime={animatorState.normalizedTime:F3} inTransition={animatorInTransition} clip='{animatorClipName}' lowerBody=Animator legIk=False\n" +
                $"  playerRoot={Fmt(playerRootPosition)} yaw={playerYaw:F1} ccBottom={Fmt(ccBottom)} ccCenter={Fmt(_characterController != null ? _characterController.center : Vector3.zero)} ccHeight={(_characterController != null ? _characterController.height : 0f):F3}\n" +
                $"  worldEyeHeight={playerEyeHeight:F3} trackingEyeHeight={trackingSpaceEyeHeight:F3} avatarEyeHeight={avatarEyeHeight:F3} avatarScale={avatarScale:F3} autoScale={autoScaleVrAvatarToPlayerHeight} scaleCalibrated={_vrAlignment.IsCalibrated} standingThreshold={standingCalibrationMinEyeHeight:F2} scaleClamp=({minVrAvatarAutoScale:F2}, {maxVrAvatarAutoScale:F2}) hostGroundY={VrGroundY():F3} resolvedFloorY={ResolvedVrFloorY():F3} floorReady={_vrAlignment.IsFloorReady}\n" +
                $"  xrHead={Fmt(xrHeadPosition)} avatarHeadTarget={Fmt(avatarHeadPosition)} avatarBodyTarget={Fmt(avatarBodyPosition)} mainAvatar={Fmt(mainAvatarPosition)} bodyAnchor={Fmt(bodyAnchorPosition)}\n" +
                $"  umaRoot={Fmt(umaRootPosition)} umaRootParent='{(_dcaRoot != null && _dcaRoot.transform.parent != null ? _dcaRoot.transform.parent.name : "null")}' umaRootToBody={Fmt(umaRootPosition - bodyAnchorPosition)} bodyToXRHead={Fmt(xrHeadPosition - bodyAnchorPosition)} avatarHeadToXRHead={Fmt(avatarHeadPosition - xrHeadPosition)} umaHeadToXRHead={Fmt(headOffset)} avatarEyeToXRHead={Fmt(avatarEyePoint - xrHeadPosition)}\n" +
                $"  leftHandOffset={Fmt(leftOffset)} rightHandOffset={Fmt(rightOffset)} leftRotDelta={leftRotDelta:F1} rightRotDelta={rightRotDelta:F1} vrAvatarRootOffset={Fmt(vrAvatarRootOffset)} vrTrackedHeadMatchOffset={Fmt(vrTrackedHeadMatchOffset)} eyeLandmark={(_leftEyeBone != null || _rightEyeBone != null ? "humanoid-eye-bones" : "head-offset-fallback")} eyeOffset=(forward {vrHeadBoneToEyeForwardOffset:F3}, up {vrHeadBoneToEyeUpOffset:F3}) inputHeadOffsetVR={Fmt(_inputConverter.headPositionOffsetVR)}");
        }

        private bool ShouldLogAlignmentSnapshot(VrAlignmentDebugSnapshot snapshot, bool force)
        {
            if (force || !_hasLastAlignmentSnapshot)
                return true;
            if (Time.time < _nextVrAlignmentLogTime)
                return false;

            float positionThreshold = Mathf.Max(0.001f, debugVrEmbodimentPositionThreshold);
            float angleThreshold = Mathf.Max(0.1f, debugVrEmbodimentAngleThreshold);
            return snapshot.StateChanged(_lastAlignmentSnapshot)
                || snapshot.PositionChanged(_lastAlignmentSnapshot, positionThreshold)
                || snapshot.AngleChanged(_lastAlignmentSnapshot, angleThreshold);
        }

        private Vector3 CharacterControllerBottomWorld()
        {
            if (_characterController == null)
                return Vector3.zero;
            Vector3 localBottom = _characterController.center + Vector3.down * (_characterController.height * 0.5f);
            return transform.TransformPoint(localBottom);
        }

        private float VrGroundY()
        {
            if (_characterController != null)
                return CharacterControllerBottomWorld().y;
            return transform.position.y;
        }

        private float ResolvedVrFloorY()
        {
            return _vrAlignment.IsFloorReady ? _vrAlignment.FloorY : VrGroundY();
        }

        private static string Fmt(Vector3 value)
        {
            return $"({value.x:F3}, {value.y:F3}, {value.z:F3})";
        }

        private readonly struct VrVisibilityDebugSnapshot
        {
            private readonly bool _hideHead;
            private readonly bool _hideBody;
            private readonly int _cullLayer;
            private readonly int _rendererCount;
            private readonly int _headRendererCount;
            private readonly int _hiddenRendererCount;
            private readonly bool _matchedHead;
            private readonly bool _vrArmIkReady;

            public VrVisibilityDebugSnapshot(
                bool hideHead,
                bool hideBody,
                int cullLayer,
                int rendererCount,
                int headRendererCount,
                int hiddenRendererCount,
                bool matchedHead,
                bool vrArmIkReady)
            {
                _hideHead = hideHead;
                _hideBody = hideBody;
                _cullLayer = cullLayer;
                _rendererCount = rendererCount;
                _headRendererCount = headRendererCount;
                _hiddenRendererCount = hiddenRendererCount;
                _matchedHead = matchedHead;
                _vrArmIkReady = vrArmIkReady;
            }

            public bool Equals(VrVisibilityDebugSnapshot other)
            {
                return _hideHead == other._hideHead
                    && _hideBody == other._hideBody
                    && _cullLayer == other._cullLayer
                    && _rendererCount == other._rendererCount
                    && _headRendererCount == other._headRendererCount
                    && _hiddenRendererCount == other._hiddenRendererCount
                    && _matchedHead == other._matchedHead
                    && _vrArmIkReady == other._vrArmIkReady;
            }
        }

        private readonly struct VrAlignmentDebugSnapshot
        {
            private readonly bool _headTracked;
            private readonly bool _handTracking;
            private readonly bool _vrArmIkReady;
            private readonly bool _hideHead;
            private readonly bool _hideBody;
            private readonly Vector3 _playerRoot;
            private readonly Vector3 _ccBottom;
            private readonly Vector3 _xrHead;
            private readonly Vector3 _avatarHead;
            private readonly Vector3 _avatarBody;
            private readonly Vector3 _mainAvatar;
            private readonly Vector3 _bodyAnchor;
            private readonly Vector3 _umaRoot;
            private readonly Vector3 _umaHeadToXrHead;
            private readonly Vector3 _leftHandOffset;
            private readonly Vector3 _rightHandOffset;
            private readonly float _playerYaw;
            private readonly float _bodyAnchorYaw;

            public VrAlignmentDebugSnapshot(
                bool headTracked,
                bool handTracking,
                bool vrArmIkReady,
                bool hideHead,
                bool hideBody,
                Vector3 playerRoot,
                Vector3 ccBottom,
                Vector3 xrHead,
                Vector3 avatarHead,
                Vector3 avatarBody,
                Vector3 mainAvatar,
                Vector3 bodyAnchor,
                Vector3 umaRoot,
                Vector3 umaHeadToXrHead,
                Vector3 leftHandOffset,
                Vector3 rightHandOffset,
                float playerYaw,
                float bodyAnchorYaw)
            {
                _headTracked = headTracked;
                _handTracking = handTracking;
                _vrArmIkReady = vrArmIkReady;
                _hideHead = hideHead;
                _hideBody = hideBody;
                _playerRoot = playerRoot;
                _ccBottom = ccBottom;
                _xrHead = xrHead;
                _avatarHead = avatarHead;
                _avatarBody = avatarBody;
                _mainAvatar = mainAvatar;
                _bodyAnchor = bodyAnchor;
                _umaRoot = umaRoot;
                _umaHeadToXrHead = umaHeadToXrHead;
                _leftHandOffset = leftHandOffset;
                _rightHandOffset = rightHandOffset;
                _playerYaw = playerYaw;
                _bodyAnchorYaw = bodyAnchorYaw;
            }

            public bool StateChanged(VrAlignmentDebugSnapshot other)
            {
                return _headTracked != other._headTracked
                    || _handTracking != other._handTracking
                    || _vrArmIkReady != other._vrArmIkReady
                    || _hideHead != other._hideHead
                    || _hideBody != other._hideBody;
            }

            public bool PositionChanged(VrAlignmentDebugSnapshot other, float threshold)
            {
                return Moved(_playerRoot, other._playerRoot, threshold)
                    || Moved(_ccBottom, other._ccBottom, threshold)
                    || Moved(_xrHead, other._xrHead, threshold)
                    || Moved(_avatarHead, other._avatarHead, threshold)
                    || Moved(_avatarBody, other._avatarBody, threshold)
                    || Moved(_mainAvatar, other._mainAvatar, threshold)
                    || Moved(_bodyAnchor, other._bodyAnchor, threshold)
                    || Moved(_umaRoot, other._umaRoot, threshold)
                    || Moved(_umaHeadToXrHead, other._umaHeadToXrHead, threshold);
            }

            public bool AngleChanged(VrAlignmentDebugSnapshot other, float threshold)
            {
                return Mathf.Abs(Mathf.DeltaAngle(_playerYaw, other._playerYaw)) >= threshold
                    || Mathf.Abs(Mathf.DeltaAngle(_bodyAnchorYaw, other._bodyAnchorYaw)) >= threshold;
            }

            private static bool Moved(Vector3 current, Vector3 previous, float threshold)
            {
                return (current - previous).sqrMagnitude >= threshold * threshold;
            }
        }

        private bool _synclessRemoteOrNotLocal()
        {
            return _playerSetup != null
                && _playerSetup.Object != null
                && !_playerSetup.Object.HasInputAuthority;
        }
    }
}
#endif
