#if VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using System.Collections;
using System.Collections.Generic;
using GHA.AvatarFramework;
using GHA.AvatarFramework.UI;
using TMPro;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// HomeScene avatar customizer (Customizer V2) — a tabbed UI built at runtime under the
    /// station canvas: Body / Face (DNA sliders), Color (palette swatches), Clothing (slot
    /// rail + scrollable option grid), and Type (UMA / Classic system). Drives a live preview
    /// mannequin and persists to UmaRecipeStore, which UmaAvatarBridge/UmaHomeAvatar read.
    /// The 3D avatar preview sits beside this panel and updates as choices are made.
    /// </summary>
    public class UmaAvatarCustomizer : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private UmaAvatarCatalog catalog;
        [SerializeField] private RuntimeAnimatorController previewAnimationController;

        [Header("Station wiring")]
        [Tooltip("The station Canvas the tabbed panel is built under.")]
        [SerializeField] private Canvas stationCanvas;
        [Tooltip("Legacy preview avatar root (AvatarHolder/Avatar) — used to preview Classic avatars; hidden otherwise.")]
        [SerializeField] private GameObject legacyPreviewRoot;
        [Tooltip("The ready-made-avatar nav chevrons (Button_Previous / Button_Next). The customizer only shows them while a stock avatar is selected in the Type tab, and hides them in UMA mode. Their existing click behavior is untouched. Leave null to skip.")]
        [SerializeField] private GameObject legacyPrevButton;
        [SerializeField] private GameObject legacyNextButton;

        [Header("Temporary load-isolation test")]
        [Tooltip("Home menu button that starts the deferred UMA load.")]
        [SerializeField] private Button changeAvatarButton;
        [Tooltip("Home embodiment whose initial UMA build is deferred.")]
        [SerializeField] private UmaHomeAvatar homeAvatar;
        [Tooltip("When enabled, close the station while the first Home avatar loads and reopen it only when the avatar is ready.")]
        [SerializeField] private bool delayOpenUntilHomeAvatarLoaded;

        [Header("Preview mannequin")]
        [SerializeField] private bool showPreviewMannequin = true;
        [Tooltip("Scene anchor the UMA preview mannequin spawns on (position/rotate/scale by hand).")]
        [SerializeField] private Transform previewParent;
        [SerializeField] private Vector3 previewLocalPosition = new Vector3(0f, -0.55f, 0f);
        [SerializeField] private Vector3 previewLocalEuler = Vector3.zero;
        [SerializeField] private float previewScale = 0.40f;

        [Header("Panel layout (fraction of the station canvas; avatar preview sits to the right)")]
        [Tooltip("Bottom-left corner of the panel as a fraction of the canvas (0,0 = bottom-left).")]
        [SerializeField] private Vector2 panelAnchorMin = new Vector2(0.02f, 0.07f);
        [Tooltip("Top-right corner of the panel as a fraction of the canvas (1,1 = top-right). Keep x small enough that the avatar on the right stays clear.")]
        [SerializeField] private Vector2 panelAnchorMax = new Vector2(0.60f, 0.93f);
        [Tooltip("Extra pixels to pull the panel's right edge inward (negative pushes it out wider). 0 = flush to the anchor.")]
        [SerializeField] private float panelRightInset = 0f;

        [Header("Screen swap (taller customizer)")]
        [Tooltip("Monitor mesh shown while the customizer is CLOSED (normal height). Hidden while open. Leave null to skip the swap.")]
        [SerializeField] private GameObject normalScreenModel;
        [Tooltip("Taller monitor mesh shown while the customizer is OPEN. Place it in the scene, inactive. Leave null to skip the swap.")]
        [SerializeField] private GameObject tallScreenModel;
        [Tooltip("Target world-space canvas height while the customizer is open; the canvas grows upward from a fixed bottom edge. 0 = leave the canvas size unchanged.")]
        [SerializeField] private float customizerCanvasHeight = 0f;

        [Header("Slider")]
        [Tooltip("Required: the Slider prefab instantiated for each DNA row. Author one stock Unity slider (GameObject > UI > Slider) in this add-on's folder and assign it here.")]
        [SerializeField] private Slider sliderPrefab;

        [Header("System radio group (centered on the Save button's bottom band)")]
        [Tooltip("Width of the system radio group. It auto-centers in the space to the right of the panel.")]
        [SerializeField] private float systemRadioWidth = 400f;

        private RectTransform _embeddedRoot;
        private RectTransform _embeddedCategoryRoot;
        private RectTransform _embeddedPreviewUiRoot;
        private bool _embeddedPanelMode;
        private bool _providerSelected = true;
        private bool _initialized;

        // ---- palette of UI colors (in-game UI, not editor) ----
        private static readonly Color PanelBg = AvatarConfigurationTheme.Surface;
        private static readonly Color TileBg = AvatarConfigurationTheme.SurfaceRaised;
        private static readonly Color Accent = AvatarConfigurationTheme.Accent;
        private static readonly Color AccentSoft = AvatarConfigurationTheme.AccentSoft;
        private static readonly Color TextCol = AvatarConfigurationTheme.TextPrimary;
        private static readonly Color TextDim = AvatarConfigurationTheme.TextSecondary;

        private enum Tab { Body, Face, Color, Clothing }

        private AvatarWireRecipe _recipe;
        private DynamicCharacterAvatar _previewDca;
        private string _previewLoadTraceId;
        private double _previewLoadStartedAt = -1d;
        private bool _previewBuildInProgress;
        private bool _previewRebuildQueued;
        private bool _previewNeedsBuild = true;
        private bool _saveRefreshPending;
        private string _saveWaitTraceId;
        private double _saveWaitStartedAt = -1d;
        private bool _deferredOpenInProgress;
        private string _deferredOpenTraceId;
        private double _deferredOpenStartedAt = -1d;

        // Working state (converted to/from the recipe).
        private int _raceIndex;
        private readonly Dictionary<int, byte> _dna = new Dictionary<int, byte>();      // dnaId -> 0..255
        private readonly Dictionary<int, int> _colors = new Dictionary<int, int>();     // channelId -> paletteIndex
        private readonly Dictionary<string, List<int>> _slotGroups = new Dictionary<string, List<int>>(); // wardrobeSlot -> catalog ids
        private readonly Dictionary<string, int> _slotSelection = new Dictionary<string, int>();           // wardrobeSlot -> selected catalog id (-1 = none)
        private readonly Dictionary<int, Dictionary<string, int>> _raceWardrobeSelections =
            new Dictionary<int, Dictionary<string, int>>();

        // Avatar systems: an extensible, ordered list the Type tab cycles through (Classic, UMA,
        // and any future systems). To add one: append a kind here, populate it in InitSystemMode,
        // and handle its selection screen in UpdateTabAvailability / the preview.
        private enum AvatarSystemKind { Uma, Classic }
        private readonly List<AvatarSystemKind> _systems = new List<AvatarSystemKind>();
        private int _systemIndex;         // index into _systems = the active system
        private int _stockIndex;          // which Classic avatar is selected (0..StockAvatarCount()-1)

        private AvatarSystemKind CurrentSystem => _systems.Count > 0 ? _systems[Mathf.Clamp(_systemIndex, 0, _systems.Count - 1)] : AvatarSystemKind.Uma;
        private bool UmaMode => CurrentSystem == AvatarSystemKind.Uma;
        private Transform UiRoot => _embeddedRoot != null
            ? _embeddedRoot
            : stationCanvas != null ? stationCanvas.transform : null;

        private RectTransform _root;
        private GameObject _tabBar;    // UMA panel tab bar (hidden in Classic mode)
        private Image _panelBg;        // UMA panel background (hidden in Classic mode)
        private GameObject _loadingOverlay; // "Loading avatar..." shown while UMA builds the preview
        private GameObject _prevBtnGo; // resolved Classic prev/next chevrons (serialized refs or found by name)
        private GameObject _nextBtnGo;

        // Screen-swap state (taller customizer). Original canvas dims captured in Start before any swap.
        private RectTransform _canvasRt;
        private Vector2 _origCanvasSize;
        private Vector2 _origCanvasPos;
        private bool _wasOpen;

        private readonly Dictionary<Tab, GameObject> _tabPanels = new Dictionary<Tab, GameObject>();
        private readonly Dictionary<Tab, Image> _tabButtons = new Dictionary<Tab, Image>();
        private readonly Dictionary<Tab, Image> _tabButtonBorders = new Dictionary<Tab, Image>();
        private readonly HashSet<Tab> _hoveredTabs = new HashSet<Tab>();
        private Tab _activeTab = Tab.Clothing;
        private RectTransform _clothingGridContent;
        private string _activeSlot;

        /// <summary>
        /// Configures this customizer as UMA-only provider content inside the provider-neutral
        /// GHA avatar panel.
        /// </summary>
        public void ConfigureEmbedded(
            UmaAvatarCatalog embeddedCatalog,
            RuntimeAnimatorController embeddedPreviewController,
            Slider embeddedSliderPrefab,
            RectTransform embeddedRoot,
            RectTransform embeddedCategoryRoot,
            Transform embeddedPreviewParent,
            RectTransform embeddedPreviewUiRoot)
        {
            catalog = embeddedCatalog;
            previewAnimationController = embeddedPreviewController;
            sliderPrefab = embeddedSliderPrefab;
            _embeddedRoot = embeddedRoot;
            _embeddedCategoryRoot = embeddedCategoryRoot;
            _embeddedPreviewUiRoot = embeddedPreviewUiRoot;
            previewParent = embeddedPreviewParent;
            _embeddedPanelMode = true;
            _providerSelected = false;
            showPreviewMannequin = true;
            delayOpenUntilHomeAvatarLoaded = false;
            legacyPreviewRoot = null;
            legacyPrevButton = null;
            legacyNextButton = null;
            panelAnchorMin = Vector2.zero;
            panelAnchorMax = Vector2.one;
            panelRightInset = 0f;
        }

        private IEnumerator Start()
        {
            if (catalog == null || UiRoot == null)
            {
                Debug.LogError("[UmaAvatarCustomizer] Missing catalog/UI root; customizer disabled.");
                yield break;
            }

            if (!_embeddedPanelMode)
                VertexFormCore.AvatarSelectionManager.SuppressLegacyAvatars = true;

            yield return null; // let the legacy AvatarSelectionManager finish its Start

            // Capture the canvas's resting size/position before any screen swap can run.
            _canvasRt = UiRoot as RectTransform;
            if (_canvasRt != null)
            {
                _origCanvasSize = _canvasRt.sizeDelta;
                _origCanvasPos = _canvasRt.anchoredPosition;
            }

            if (legacyPreviewRoot != null)
                legacyPreviewRoot.SetActive(false);

            LoadStateFromRecipe();
            BuildUI();
            ResolveStockNavButtons();
            UpdateTabAvailability();
            VertexFormCore.SceneLoader.SceneLoadStarting += OnSceneLoadStarting;
            _initialized = true;
            if (!_embeddedPanelMode)
                CloseStationOnStartup();

            if (delayOpenUntilHomeAvatarLoaded)
            {
                if (changeAvatarButton == null || homeAvatar == null)
                {
                    Debug.LogError("[UmaAvatarCustomizer] Deferred-load test is enabled but Change Avatar button/Home avatar wiring is missing.");
                }
                else
                {
                    changeAvatarButton.onClick.AddListener(OnDeferredChangeAvatarClicked);
                }
            }
        }

        public void SetProviderSelected(bool selected)
        {
            _providerSelected = selected;
            if (!_embeddedPanelMode)
                return;

            if (!selected)
            {
                if (_previewDca != null)
                    _previewDca.gameObject.SetActive(false);
                ShowLoading(false);
                return;
            }

            _previewNeedsBuild = true;
            if (_initialized && isActiveAndEnabled && IsStationOpen())
                BuildPreview();
        }

        // ---------------------------------------------------------------- state <-> recipe

        private void LoadStateFromRecipe()
        {
            _recipe = UmaRecipeStore.LoadOrDefault(catalog);
            _raceIndex = Mathf.Clamp(_recipe.raceId, 0, Mathf.Max(0, catalog.races.Count - 1));

            BuildSlotGroups();
            _raceWardrobeSelections.Clear();
            _slotSelection.Clear();
            // selection per slot from the recipe's wardrobe ids
            foreach (var kv in _slotGroups)
                _slotSelection[kv.Key] = -1;
            if (_recipe.wardrobeIds != null)
            {
                foreach (int id in _recipe.wardrobeIds)
                {
                    UMAWardrobeRecipe w = catalog.Wardrobe(id);
                    if (!catalog.IsWardrobeCompatible(_raceIndex, w)) continue;
                    string slot = SlotKey(w);
                    if (_slotGroups.ContainsKey(slot)) _slotSelection[slot] = id;
                }
            }

            _dna.Clear();
            if (_recipe.dna != null)
                foreach (DnaValue d in _recipe.dna) _dna[d.id] = d.value;

            _colors.Clear();
            if (_recipe.colors != null)
                foreach (ColorValue c in _recipe.colors) _colors[c.channelId] = c.paletteIndex;

            InitSystemMode();
        }

        private void SyncRecipe()
        {
            _recipe.raceId = _raceIndex;
            _recipe.wardrobeIds.Clear();
            foreach (var kv in _slotSelection)
                if (kv.Value >= 0) _recipe.wardrobeIds.Add(kv.Value);

            _recipe.dna.Clear();
            foreach (var kv in _dna)
                _recipe.dna.Add(new DnaValue { id = kv.Key, value = kv.Value });

            _recipe.colors.Clear();
            foreach (var kv in _colors)
                _recipe.colors.Add(new ColorValue { channelId = kv.Key, paletteIndex = kv.Value });
        }

        private void BuildSlotGroups()
        {
            _slotGroups.Clear();
            for (int id = 0; id < catalog.wardrobeRecipes.Count; id++)
            {
                UMAWardrobeRecipe w = catalog.wardrobeRecipes[id];
                if (!catalog.IsWardrobeCompatible(_raceIndex, w)) continue;
                string slot = SlotKey(w);
                if (!_slotGroups.TryGetValue(slot, out List<int> list))
                {
                    list = new List<int>();
                    _slotGroups.Add(slot, list);
                }
                list.Add(id);
            }
        }

        private static string SlotKey(UMAWardrobeRecipe w) =>
            string.IsNullOrEmpty(w.wardrobeSlot) ? "Other" : w.wardrobeSlot;

        private void InitSystemMode()
        {
            _systems.Clear();
            if (_embeddedPanelMode)
            {
                _systems.Add(AvatarSystemKind.Uma);
                _systemIndex = 0;
                return;
            }

            if (catalog.enableUmaAvatars) _systems.Add(AvatarSystemKind.Uma);
            if (catalog.enableStockAvatars && StockAvatarCount() > 0) _systems.Add(AvatarSystemKind.Classic);
            if (_systems.Count == 0) _systems.Add(AvatarSystemKind.Uma);

            _stockIndex = Mathf.Clamp(UmaRecipeStore.LoadStockSelection(), 0, Mathf.Max(0, StockAvatarCount() - 1));
            AvatarSystemMode mode = UmaRecipeStore.LoadMode(catalog);
            AvatarSystemKind wanted = mode == AvatarSystemMode.Uma ? AvatarSystemKind.Uma : AvatarSystemKind.Classic;
            _systemIndex = Mathf.Max(0, _systems.IndexOf(wanted));
        }

        private static int StockAvatarCount() =>
            ProjectManager.instance != null && ProjectManager.instance.uiLayoutConfig != null
                && ProjectManager.instance.uiLayoutConfig.avatarDatas != null
                ? ProjectManager.instance.uiLayoutConfig.avatarDatas.Count : 0;

        // ---------------------------------------------------------------- UI build

        private void BuildUI()
        {
            // Replace any previously generated panel (V2 and the old v1 row panel).
            Transform uiRoot = UiRoot;
            Transform existing = uiRoot.Find("GHA_CustomizerPanelV2");
            if (existing != null) Destroy(existing.gameObject);
            Transform oldV1 = uiRoot.Find("GHA_CustomizerPanel");
            if (oldV1 != null) Destroy(oldV1.gameObject);
            Transform oldSave = uiRoot.Find("GHA_SaveButton");
            if (oldSave != null) Destroy(oldSave.gameObject);
            Transform oldRadio = uiRoot.Find("GHA_SystemRadio");
            if (oldRadio != null) Destroy(oldRadio.gameObject);
            Transform oldLoading = uiRoot.Find("GHA_Loading");
            if (oldLoading != null) Destroy(oldLoading.gameObject);

            _root = NewRect("GHA_CustomizerPanelV2", uiRoot);
            _root.anchorMin = panelAnchorMin;
            _root.anchorMax = panelAnchorMax;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = new Vector2(-panelRightInset, 0f); // pull right edge in so the avatar fits
            Image rootImg = _root.gameObject.AddComponent<Image>();
            rootImg.color = PanelBg;
            _panelBg = rootImg;

            const float pad = 12f, tabH = 56f, gap = 12f;
            float actionBarReserve = _embeddedPanelMode ? 70f : 0f;

            // Embedded mode uses the shell's left-hand category rail. The legacy standalone
            // customizer keeps its horizontal top tabs.
            RectTransform tabBar = NewRect(
                "TabBar",
                _embeddedPanelMode && _embeddedCategoryRoot != null
                    ? _embeddedCategoryRoot
                    : _root);
            if (_embeddedPanelMode && _embeddedCategoryRoot != null)
            {
                tabBar.anchorMin = Vector2.zero;
                tabBar.anchorMax = Vector2.one;
                tabBar.offsetMin = Vector2.zero;
                tabBar.offsetMax = Vector2.zero;
            }
            else
            {
                tabBar.anchorMin = new Vector2(0f, 1f);
                tabBar.anchorMax = new Vector2(1f, 1f);
                tabBar.pivot = new Vector2(0.5f, 1f);
                tabBar.offsetMin = new Vector2(pad, -(pad + tabH));
                tabBar.offsetMax = new Vector2(-pad, -pad);
            }
            _tabBar = tabBar.gameObject;
            BuildTabBar(tabBar, _embeddedPanelMode);

            // Content fills the middle; the old save-bar band at the bottom is now reclaimed.
            RectTransform content = NewRect("Content", _root);
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(pad, pad + actionBarReserve);
            content.offsetMax = new Vector2(
                -pad,
                _embeddedPanelMode ? -pad : -(pad + tabH + gap));

            _tabPanels[Tab.Body] = BuildDnaTab(content, faceTab: false);
            _tabPanels[Tab.Face] = BuildDnaTab(content, faceTab: true);
            _tabPanels[Tab.Color] = BuildColorTab(content);
            _tabPanels[Tab.Clothing] = BuildClothingTab(content);

            BuildSaveButton();
            BuildSystemRadio();
            BuildLoadingOverlay();

            ShowTab(Tab.Clothing);
        }

        private void BuildTabBar(RectTransform bar, bool vertical)
        {
            if (vertical)
            {
                float tileSize = bar.rect.width > 20f
                    ? bar.rect.width - 12f
                    : 64f;
                var grid = bar.gameObject.AddComponent<GridLayoutGroup>();
                grid.padding = new RectOffset(6, 6, 8, 8);
                grid.spacing = new Vector2(0f, 8f);
                grid.cellSize = new Vector2(tileSize, tileSize);
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 1;
            }
            else
            {
                var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 6f;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }

            AddTabButton(bar, Tab.Body, "Body", vertical);
            AddTabButton(bar, Tab.Face, "Face", vertical);
            AddTabButton(bar, Tab.Color, "Colors", vertical);
            AddTabButton(bar, Tab.Clothing, "Outfits", vertical);
        }

        private void AddTabButton(
            RectTransform parent,
            Tab tab,
            string label,
            bool vertical)
        {
            RectTransform rt = NewRect("Tab_" + label, parent);
            Image bg = rt.gameObject.AddComponent<Image>();
            bg.color = TileBg;
            AvatarConfigurationTheme.ApplyRounded(bg);
            // A separate renderer keeps the border independent of Button's background tint.
            RectTransform borderRect = NewRect("StateBorder", rt);
            FillParent(borderRect);
            Image border = borderRect.gameObject.AddComponent<Image>();
            AvatarConfigurationTheme.ApplyRoundedBorder(border);
            border.color = AvatarConfigurationTheme.OptionBorder;
            Button btn = rt.gameObject.AddComponent<Button>();
            AvatarConfigurationTheme.ConfigureButton(
                btn,
                bg,
                Color.white,
                Color.white,
                new Color(0.8f, 0.8f, 0.8f, 1f));
            btn.onClick.AddListener(() => ShowTab(tab));

            EventTrigger pointerStates = rt.gameObject.AddComponent<EventTrigger>();
            pointerStates.triggers = new List<EventTrigger.Entry>();
            EventTrigger.Entry enter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter,
            };
            enter.callback.AddListener(_ => SetTabHover(tab, true));
            pointerStates.triggers.Add(enter);
            EventTrigger.Entry exit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit,
            };
            exit.callback.AddListener(_ => SetTabHover(tab, false));
            pointerStates.triggers.Add(exit);

            if (vertical)
                BuildCategoryIcon(rt, tab);
            TMP_Text tabLabel = Label(rt, label, vertical ? 17 : 22, TextAlignmentOptions.Center, TextCol);
            tabLabel.fontStyle = FontStyles.Bold;
            if (vertical)
            {
                RectTransform labelRect = tabLabel.rectTransform;
                labelRect.anchorMin = new Vector2(0.06f, 0.04f);
                labelRect.anchorMax = new Vector2(0.94f, 0.36f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }
            _tabButtons[tab] = bg;
            _tabButtonBorders[tab] = border;
        }

        private static void BuildCategoryIcon(RectTransform parent, Tab tab)
        {
            RectTransform iconRoot = NewRect("Icon", parent);
            iconRoot.anchorMin = new Vector2(0.25f, 0.42f);
            iconRoot.anchorMax = new Vector2(0.75f, 0.92f);
            iconRoot.offsetMin = Vector2.zero;
            iconRoot.offsetMax = Vector2.zero;

            string resourcePath;
            switch (tab)
            {
                case Tab.Body:
                    resourcePath = "GHA/AvatarStudio/CategoryIcons/Body";
                    break;
                case Tab.Face:
                    resourcePath = "GHA/AvatarStudio/CategoryIcons/Face";
                    break;
                case Tab.Color:
                    resourcePath = "GHA/AvatarStudio/CategoryIcons/Colors";
                    break;
                case Tab.Clothing:
                    resourcePath = "GHA/AvatarStudio/CategoryIcons/Outfits";
                    break;
                default:
                    return;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"Avatar Studio category icon could not be loaded: {resourcePath}");
                return;
            }

            RawImage image = iconRoot.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private void ShowTab(Tab tab)
        {
            _hoveredSlots.Clear();
            RefreshSlotSelection();
            _activeTab = tab;
            foreach (var kv in _tabPanels)
                kv.Value.SetActive(kv.Key == tab);
            foreach (var kv in _tabButtons)
                kv.Value.color = kv.Key == tab ? Accent : TileBg;
            foreach (var kv in _tabButtonBorders)
                ApplyTabBorder(kv.Value, kv.Key == tab ? TabBorderState.Active
                    : _hoveredTabs.Contains(kv.Key) ? TabBorderState.Hovered : TabBorderState.Idle);
        }

        private void SetTabHover(Tab tab, bool hovered)
        {
            if (hovered) _hoveredTabs.Add(tab);
            else _hoveredTabs.Remove(tab);
            if (!_tabButtonBorders.TryGetValue(tab, out Image border))
                return;

            ApplyTabBorder(
                border,
                tab == _activeTab
                    ? TabBorderState.Active
                    : hovered ? TabBorderState.Hovered : TabBorderState.Idle);
        }

        private enum TabBorderState
        {
            Idle,
            Hovered,
            Active,
        }

        private static void ApplyTabBorder(Image border, TabBorderState state)
        {
            if (border == null)
                return;

            switch (state)
            {
                case TabBorderState.Active:
                    border.color = new Color(1f, 1f, 1f, 0.96f);
                    break;
                case TabBorderState.Hovered:
                    Color hover = AvatarConfigurationTheme.AccentHover;
                    border.color = new Color(hover.r, hover.g, hover.b, 0.95f);
                    break;
                default:
                    border.color = AvatarConfigurationTheme.OptionBorder;
                    break;
            }
        }

        // The UMA panel (tabs + content + background) only applies to the UMA system. In Classic
        // mode the station shows the legacy selection screen instead (chevrons + stock preview),
        // so the whole panel chrome hides — the radio group, Close and Save stay (they're separate).
        private void UpdateTabAvailability()
        {
            _hoveredTabs.Clear();
            bool uma = UmaMode;
            if (_tabBar != null) _tabBar.SetActive(uma);
            if (_panelBg != null) _panelBg.enabled = uma;
            if (uma)
                ShowTab(Tab.Clothing);
            else
                HideAllTabs();
        }

        private void HideAllTabs()
        {
            foreach (var kv in _tabPanels)
                kv.Value.SetActive(false);
        }

        // ---- Clothing tab: slot rail + scrollable option grid ----

        private GameObject BuildClothingTab(RectTransform parent)
        {
            RectTransform tab = NewRect("Tab.Clothing", parent);
            FillParent(tab);

            const float railWidth = 136f, gap = 12f;

            // Slot rail — a deterministic fixed-width left strip. (HorizontalLayoutGroup splits the
            // width evenly here no matter the flags, so anchor it by hand like the rest of the panel.)
            RectTransform rail = NewRect("SlotRail", tab);
            rail.anchorMin = new Vector2(0f, 0f);
            rail.anchorMax = new Vector2(0f, 1f);
            rail.pivot = new Vector2(0f, 0.5f);
            rail.anchoredPosition = Vector2.zero;
            rail.sizeDelta = new Vector2(railWidth, 0f);
            var rv = rail.gameObject.AddComponent<VerticalLayoutGroup>();
            rv.spacing = 4f;
            rv.childControlWidth = rv.childControlHeight = true;
            rv.childForceExpandWidth = true;
            rv.childForceExpandHeight = false;

            var slotKeys = new List<string>(_slotGroups.Keys);
            slotKeys.Sort();
            foreach (string slot in slotKeys)
            {
                AddSlotButton(rail, slot);
            }

            // Option grid — fills the rest of the tab to the right of the rail. BuildScrollGrid
            // fills its target, so re-anchor afterwards to inset past the rail.
            RectTransform gridArea = NewRect("GridArea", tab);
            _clothingGridContent = BuildScrollGrid(gridArea, out _);
            gridArea.anchorMin = new Vector2(0f, 0f);
            gridArea.anchorMax = new Vector2(1f, 1f);
            gridArea.offsetMin = new Vector2(railWidth + gap, 0f);
            gridArea.offsetMax = Vector2.zero;

            _activeSlot = slotKeys.Count > 0 ? slotKeys[0] : null;
            if (_activeSlot != null) SelectSlot(_activeSlot);
            return tab.gameObject;
        }

        private readonly Dictionary<string, Image> _slotRailButtons = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _slotRailBorders = new Dictionary<string, Image>();
        private readonly HashSet<string> _hoveredSlots = new HashSet<string>();

        private void AddSlotButton(RectTransform rail, string slot)
        {
            Button button = TextButton(rail, Prettify(slot), 20, () => SelectSlot(slot), out Image background);
            button.gameObject.AddComponent<LayoutElement>().minHeight = 52f;
            // Match the main categories: state owns the fill and an independent border.
            // Neutral button tint prevents the persistent accent from being multiplied dark.
            button.GetComponent<Outline>().enabled = false;
            AvatarConfigurationTheme.ConfigureButton(button, background, Color.white, Color.white,
                new Color(0.8f, 0.8f, 0.8f, 1f));
            RectTransform borderRect = NewRect("StateBorder", button.transform);
            FillParent(borderRect);
            Image border = borderRect.gameObject.AddComponent<Image>();
            AvatarConfigurationTheme.ApplyRoundedBorder(border);
            _slotRailButtons[slot] = background;
            _slotRailBorders[slot] = border;
            ApplyTabBorder(border, TabBorderState.Idle);

            EventTrigger pointerStates = button.gameObject.AddComponent<EventTrigger>();
            pointerStates.triggers = new List<EventTrigger.Entry>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => SetSlotHover(slot, true));
            pointerStates.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => SetSlotHover(slot, false));
            pointerStates.triggers.Add(exit);
        }

        private void SetSlotHover(string slot, bool hovered)
        {
            if (hovered) _hoveredSlots.Add(slot);
            else _hoveredSlots.Remove(slot);
            RefreshSlotSelection();
        }

        private void RefreshSlotSelection()
        {
            foreach (var kv in _slotRailButtons)
                kv.Value.color = kv.Key == _activeSlot ? Accent : TileBg;
            foreach (var kv in _slotRailBorders)
                ApplyTabBorder(kv.Value, kv.Key == _activeSlot ? TabBorderState.Active
                    : _hoveredSlots.Contains(kv.Key) ? TabBorderState.Hovered : TabBorderState.Idle);
        }

        private void SelectSlot(string slot)
        {
            _activeSlot = slot;
            RefreshSlotSelection();
            PopulateClothingGrid(slot);
        }

        private void PopulateClothingGrid(string slot)
        {
            if (_clothingGridContent == null) return;
            for (int i = _clothingGridContent.childCount - 1; i >= 0; i--)
                Destroy(_clothingGridContent.GetChild(i).gameObject);

            int current = _slotSelection.TryGetValue(slot, out int sel) ? sel : -1;

            // "None" tile.
            AddGridTile(_clothingGridContent, "None", null, current < 0, () => { _slotSelection[slot] = -1; SelectSlot(slot); RebuildPreview(); });

            if (_slotGroups.TryGetValue(slot, out List<int> ids))
            {
                foreach (int id in ids)
                {
                    int captured = id;
                    UMAWardrobeRecipe w = catalog.Wardrobe(id);
                    string nm = w != null ? w.name.Replace("_Recipe", "") : "?";
                    Sprite thumb = GetThumb(w);
                    AddGridTile(_clothingGridContent, nm, thumb, current == id,
                        () => { _slotSelection[slot] = captured; SelectSlot(slot); RebuildPreview(); });
                }
            }
        }

        // ---- Color tab: swatch grids per channel ----

        private GameObject BuildColorTab(RectTransform parent)
        {
            RectTransform tab = NewRect("Tab.Color", parent);
            FillParent(tab);
            RectTransform content = BuildScrollColumn(tab);

            if (catalog.colorChannels != null)
            {
                for (int ci = 0; ci < catalog.colorChannels.Count; ci++)
                {
                    var ch = catalog.colorChannels[ci];
                    if (ch == null || ch.palette == null || ch.palette.colors == null) continue;
                    int channelId = ci;
                    TMP_Text channelLabel = Label(content, string.IsNullOrEmpty(ch.label) ? ch.channelName : ch.label, 24, TextAlignmentOptions.Left, TextDim);
                    channelLabel.fontStyle = FontStyles.Bold;
                    channelLabel
                        .gameObject.AddComponent<LayoutElement>().minHeight = 40f;

                    RectTransform row = NewRect("Swatches_" + ch.channelName, content);
                    var g = row.gameObject.AddComponent<GridLayoutGroup>();
                    g.cellSize = new Vector2(60, 60);
                    g.spacing = new Vector2(10, 10);
                    var fit = row.gameObject.AddComponent<ContentSizeFitter>();
                    fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    int currentIdx = _colors.TryGetValue(channelId, out int v) ? v : -1;
                    for (int pi = 0; pi < ch.palette.colors.Length; pi++)
                    {
                        int paletteIndex = pi;
                        OverlayColorData ocd = ch.palette.colors[pi];
                        // Several shipped UMA tables have stale or incorrect displayColor values
                        // (for example, Gray eyes are black and Blue eyes are white). Preview the
                        // actual material base color when present; otherwise use the tint mask
                        // that UMA applies to the generated avatar.
                        if (!TryGetSwatchPreviewColor(ocd, out Color swatch))
                        {
                            string entryName = ocd != null && !string.IsNullOrEmpty(ocd.name)
                                ? ocd.name
                                : "<unnamed>";
                            Debug.LogError(
                                $"[UmaAvatarCustomizer] Omitting invalid color swatch: " +
                                $"channel='{ch.channelName}', palette='{ch.palette.name}', " +
                                $"index={pi}, entry='{entryName}'. The entry has neither a " +
                                "supported material base-color property nor a channel tint.");
                            continue;
                        }
                        AddSwatchTile(row, swatch, currentIdx == pi,
                            () => { _colors[channelId] = paletteIndex; RebuildColorsOnly(); RefreshColorTab(); });
                    }
                }
            }
            return tab.gameObject;
        }

        private void RefreshColorTab()
        {
            // Cheap: rebuild the color tab swatch highlights by regenerating it.
            if (_tabPanels.TryGetValue(Tab.Color, out GameObject old))
            {
                bool wasActive = old.activeSelf;
                Transform parent = old.transform.parent;
                int sib = old.transform.GetSiblingIndex();
                Destroy(old);
                GameObject rebuilt = BuildColorTab((RectTransform)parent);
                rebuilt.transform.SetSiblingIndex(sib);
                rebuilt.SetActive(wasActive);
                _tabPanels[Tab.Color] = rebuilt;
            }
        }

        // ---- Body / Face tabs: DNA sliders ----

        private GameObject BuildDnaTab(RectTransform parent, bool faceTab)
        {
            RectTransform tab = NewRect(faceTab ? "Tab.Face" : "Tab.Body", parent);
            FillParent(tab);
            RectTransform content = BuildScrollColumn(tab);

            // Race selector at the top of the Body tab.
            if (!faceTab && catalog.races.Count > 1)
                BuildRaceSelector(content);

            if (catalog.dnaNames != null)
            {
                for (int i = 0; i < catalog.dnaNames.Count; i++)
                {
                    string dnaName = catalog.dnaNames[i];
                    if (string.IsNullOrEmpty(dnaName)) continue;
                    if (IsFaceDna(dnaName) != faceTab) continue;
                    BuildDnaSlider(content, i, dnaName);
                }
            }
            return tab.gameObject;
        }

        private void BuildDnaSlider(RectTransform parent, int dnaId, string dnaName)
        {
            RectTransform row = NewRect("Dna_" + dnaName, parent);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 58f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.childAlignment = TextAnchor.MiddleLeft;

            TMP_Text lab = Label(row, Prettify(dnaName), 21, TextAlignmentOptions.Left, TextCol);
            lab.gameObject.AddComponent<LayoutElement>().preferredWidth = 170f;

            RectTransform sliderRt = NewRect("Slider", row);
            sliderRt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Slider slider = BuildSlider(sliderRt);
            if (slider == null) return; // no prefab assigned (error already logged)
            float current = _dna.TryGetValue(dnaId, out byte b) ? b / 255f : 0.5f;
            slider.SetValueWithoutNotify(current);

            RectTransform valueBadge = NewRect("Value", row);
            LayoutElement valueLayout = valueBadge.gameObject.AddComponent<LayoutElement>();
            valueLayout.minWidth = 58f;
            valueLayout.preferredWidth = 58f;
            Image valueBackground = valueBadge.gameObject.AddComponent<Image>();
            valueBackground.color = AvatarConfigurationTheme.SurfaceRaised;
            AvatarConfigurationTheme.ApplyRounded(valueBackground);
            Outline valueBorder = valueBadge.gameObject.AddComponent<Outline>();
            valueBorder.effectColor = AvatarConfigurationTheme.Border;
            valueBorder.effectDistance = new Vector2(1f, -1f);
            TMP_Text valueLabel = Label(
                valueBadge,
                Mathf.RoundToInt(current * 100f) + "%",
                17,
                TextAlignmentOptions.Center,
                TextCol);
            FillParent(valueLabel.rectTransform);

            string capturedDna = dnaName;
            slider.onValueChanged.AddListener(v =>
            {
                _dna[dnaId] = (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
                valueLabel.text = Mathf.RoundToInt(v * 100f) + "%";
                SetPreviewDna(capturedDna, v); // apply live, on change (UMA slider pattern)
            });
        }

        private static bool TryGetSwatchPreviewColor(
            OverlayColorData swatch,
            out Color previewColor)
        {
            previewColor = default;
            if (swatch == null)
                return false;

            if (swatch.PropertyBlock != null && swatch.PropertyBlock.shaderProperties != null)
            {
                // SRP skin tables use _Base_Color; SRP hair tables use _BaseColor. Those are
                // the values actually pushed onto the generated material by SetRawColor.
                string[] preferredNames = { "_BaseColor", "_Base_Color", "_Color" };
                for (int nameIndex = 0; nameIndex < preferredNames.Length; nameIndex++)
                {
                    for (int propertyIndex = 0;
                         propertyIndex < swatch.PropertyBlock.shaderProperties.Count;
                         propertyIndex++)
                    {
                        UMAProperty property = swatch.PropertyBlock.shaderProperties[propertyIndex];
                        if (property is UMAColorProperty colorProperty &&
                            string.Equals(
                                property.name,
                                preferredNames[nameIndex],
                                System.StringComparison.Ordinal))
                        {
                            Color materialColor = colorProperty.Value;
                            materialColor.a = 1f;
                            previewColor = materialColor;
                            return true;
                        }
                    }
                }
            }

            if (swatch.channelMask != null && swatch.channelMask.Length > 0)
            {
                Color tint = swatch.channelMask[0];
                tint.a = 1f;
                previewColor = tint;
                return true;
            }

            return false;
        }

        private void BuildRaceSelector(RectTransform parent)
        {
            RectTransform row = NewRect("RaceSelector", parent);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 56f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            Label(row, "Body type", 24, TextAlignmentOptions.Left, TextCol).gameObject.AddComponent<LayoutElement>().preferredWidth = 132f;
            Button previous = TextButton(row, "<", 30, () => CycleRace(-1), out _);
            LayoutElement previousSize = previous.gameObject.AddComponent<LayoutElement>();
            previousSize.minWidth = previousSize.preferredWidth = previousSize.preferredHeight = 56f;
            _raceValueLabel = Label(row, BodyTypeLabel(), 26, TextAlignmentOptions.Center, TextCol);
            _raceValueLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Button next = TextButton(row, ">", 30, () => CycleRace(1), out _);
            LayoutElement nextSize = next.gameObject.AddComponent<LayoutElement>();
            nextSize.minWidth = nextSize.preferredWidth = nextSize.preferredHeight = 56f;
        }

        private TMP_Text _raceValueLabel;

        private void CycleRace(int dir)
        {
            if (catalog.races.Count < 2 || dir == 0) return;
            int next = _raceIndex;
            for (int step = 0; step < catalog.races.Count; step++)
            {
                next = (next + (dir > 0 ? 1 : -1) + catalog.races.Count) % catalog.races.Count;
                if (catalog.IsHumanoidRace(next)) break;
            }
            if (next == _raceIndex || !catalog.IsHumanoidRace(next)) return;
            SwitchBodyType(next);
            if (_raceValueLabel != null) _raceValueLabel.text = BodyTypeLabel();
            RefreshClothingTab();
            RebuildPreview();
        }

        private string BodyTypeLabel()
        {
            var definition = catalog.BodyType(_raceIndex);
            return definition != null && !string.IsNullOrWhiteSpace(definition.label)
                ? definition.label : RaceDisplayName();
        }

        private void SwitchBodyType(int raceId)
        {
            if (!catalog.IsHumanoidRace(raceId) || raceId == _raceIndex) return;
            var previous = new Dictionary<string, int>(_slotSelection);
            _raceWardrobeSelections[_raceIndex] = previous;
            _raceIndex = raceId;
            BuildSlotGroups();
            _slotSelection.Clear();
            foreach (string slot in _slotGroups.Keys) _slotSelection[slot] = -1;

            if (_raceWardrobeSelections.TryGetValue(raceId, out var remembered))
            {
                foreach (var entry in remembered)
                    if (_slotGroups.ContainsKey(entry.Key) && (entry.Value < 0
                        || catalog.IsWardrobeCompatible(raceId, catalog.Wardrobe(entry.Value))))
                        _slotSelection[entry.Key] = entry.Value;
            }
            else
            {
                var definition = catalog.BodyType(raceId);
                if (definition != null)
                    foreach (var item in definition.startingWardrobe)
                        if (catalog.IsWardrobeCompatible(raceId, item) && catalog.WardrobeId(item) >= 0)
                            _slotSelection[SlotKey(item)] = catalog.WardrobeId(item);
                // Keep explicitly selected compatible items (including shared hair), or None.
                foreach (var entry in previous)
                    if (_slotGroups.ContainsKey(entry.Key) && (entry.Value < 0
                        || catalog.IsWardrobeCompatible(raceId, catalog.Wardrobe(entry.Value))))
                        _slotSelection[entry.Key] = entry.Value;
            }
            SyncRecipe();
        }

        private void RefreshClothingTab()
        {
            if (!_tabPanels.TryGetValue(Tab.Clothing, out GameObject old) || old == null) return;
            bool wasActive = old.activeSelf;
            var parent = (RectTransform)old.transform.parent;
            int sibling = old.transform.GetSiblingIndex();
            string selectedSlot = _activeSlot;
            old.SetActive(false);
            Destroy(old);
            _slotRailButtons.Clear();
            _slotRailBorders.Clear();
            _hoveredSlots.Clear();
            GameObject rebuilt = BuildClothingTab(parent);
            rebuilt.transform.SetSiblingIndex(sibling);
            rebuilt.SetActive(wasActive);
            _tabPanels[Tab.Clothing] = rebuilt;
            if (selectedSlot != null && selectedSlot != _activeSlot && _slotGroups.ContainsKey(selectedSlot))
                SelectSlot(selectedSlot);
        }

        // ---- Type tab: UMA / Classic ----

        // ---- System radio group: a standalone control between Close and the panel that toggles ----
        // ---- which avatar system is active. Cycles through _systems (Classic, UMA, future).    ----

        // "Loading avatar..." overlay, centered below the avatar. Shown while UMA builds the preview
        // (the build is queued and runs on the generator's next frame, so this renders before the
        // unavoidable single-frame build hitch and stays on screen through it).
        private void BuildLoadingOverlay()
        {
            if (_embeddedPanelMode && _embeddedPreviewUiRoot != null)
            {
                RectTransform embeddedOverlay =
                    NewRect("GHA_Loading", _embeddedPreviewUiRoot);
                embeddedOverlay.anchorMin = new Vector2(0f, 0f);
                embeddedOverlay.anchorMax = new Vector2(1f, 0f);
                embeddedOverlay.pivot = new Vector2(0.5f, 0f);
                embeddedOverlay.anchoredPosition = Vector2.zero;
                embeddedOverlay.sizeDelta = new Vector2(0f, 64f);
                embeddedOverlay.gameObject.AddComponent<Image>().color =
                    new Color(0f, 0f, 0f, 0.8f);
                TMP_Text embeddedLabel = Label(
                    embeddedOverlay,
                    "Loading avatar...",
                    28,
                    TextAlignmentOptions.Center,
                    TextCol);
                FillParent(embeddedLabel.rectTransform);
                _loadingOverlay = embeddedOverlay.gameObject;
                _loadingOverlay.SetActive(false);
                return;
            }

            float canvasW = _canvasRt != null ? _canvasRt.rect.width : 1920f;
            float panelRight = panelAnchorMax.x * canvasW - panelRightInset;
            RectTransform rt = NewRect("GHA_Loading", UiRoot);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2((panelRight + canvasW) * 0.5f - 260f, 0f);
            rt.sizeDelta = new Vector2(520f, 96f);
            rt.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
            TMP_Text lab = Label(rt, "Loading avatar...", 34, TextAlignmentOptions.Center, TextCol);
            FillParent(lab.rectTransform);
            _loadingOverlay = rt.gameObject;
            _loadingOverlay.SetActive(false);
        }

        private void ShowLoading(bool show)
        {
            if (_loadingOverlay != null && _loadingOverlay.activeSelf != show)
                _loadingOverlay.SetActive(show);
        }

        private readonly List<Image> _systemRadioImages = new List<Image>();

        private void BuildSystemRadio()
        {
            _systemRadioImages.Clear();
            if (_systems.Count <= 1) return; // nothing to toggle between

            RectTransform rt = NewRect("GHA_SystemRadio", UiRoot);
            // Center it in the band to the right of the panel (under the avatar), on the Save row.
            float canvasW = _canvasRt != null ? _canvasRt.rect.width : 1920f;
            float panelRight = panelAnchorMax.x * canvasW - panelRightInset;
            float x = (panelRight + canvasW) * 0.5f - systemRadioWidth * 0.5f;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, BottomBarMargin);
            rt.sizeDelta = new Vector2(systemRadioWidth, BottomBarHeight);
            rt.gameObject.AddComponent<Image>().color = PanelBg;

            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f; h.padding = new RectOffset(8, 8, 8, 8);
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;

            for (int i = 0; i < _systems.Count; i++)
            {
                int captured = i;
                TextButton(rt, SystemName(_systems[i]), 26, () => SelectSystem(captured), out Image img);
                _systemRadioImages.Add(img);
            }
            RefreshSystemRadio();
        }

        // Pick a system from the radio group; swaps the station to that system's selection screen.
        private void SelectSystem(int index)
        {
            if (index < 0 || index >= _systems.Count) return;
            _systemIndex = index;
            RefreshSystemRadio();
            UpdateTabAvailability();
            RebuildPreview();
        }

        private void RefreshSystemRadio()
        {
            for (int i = 0; i < _systemRadioImages.Count; i++)
                if (_systemRadioImages[i] != null)
                    _systemRadioImages[i].color = i == _systemIndex ? Accent : TileBg;
        }

        private static string SystemName(AvatarSystemKind kind)
        {
            switch (kind)
            {
                case AvatarSystemKind.Uma: return "UMA";
                case AvatarSystemKind.Classic: return "Classic";
                default: return kind.ToString();
            }
        }

        private int CurrentSystemOption => UmaMode ? -1 : _stockIndex;
        private string SystemDisplayName() => UmaMode ? "UMA Avatar" : $"Classic {_stockIndex + 1}";

        // ---------------------------------------------------------------- save

        // Shared bottom band for the Save button and the system radio group, so they line up.
        private const float BottomBarMargin = AvatarConfigurationTheme.SaveButtonMargin;
        private const float BottomBarHeight = AvatarConfigurationTheme.SaveButtonHeight;

        // Small SAVE button anchored to the bottom-right of the canvas (under the avatar preview),
        // outside the panel so it no longer eats panel real estate.
        private void BuildSaveButton()
        {
            RectTransform rt = NewRect("GHA_SaveButton", UiRoot);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(
                _embeddedPanelMode ? AvatarConfigurationTheme.EmbeddedSaveButtonWidth : 112f,
                BottomBarHeight);
            rt.anchoredPosition = new Vector2(-BottomBarMargin, BottomBarMargin);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = Accent;
            Button btn = rt.gameObject.AddComponent<Button>();
            AvatarConfigurationTheme.ConfigureButton(
                btn,
                img,
                Accent,
                AvatarConfigurationTheme.AccentHover,
                AvatarConfigurationTheme.AccentPressed);
            btn.onClick.AddListener(OnSave);
            TMP_Text lab = Label(
                rt,
                _embeddedPanelMode ? "SAVE AVATAR" : "SAVE",
                _embeddedPanelMode ? 19 : 21,
                TextAlignmentOptions.Center,
                TextCol);
            lab.fontStyle = FontStyles.Bold;
            lab.characterSpacing = 1.2f;
            FillParent(lab.rectTransform);
        }

        private void OnSave()
        {
            SyncRecipe();
            UmaRecipeStore.Save(_recipe);
            if (CurrentSystemOption < 0)
                UmaRecipeStore.SaveMode(AvatarSystemMode.Uma);
            else
            {
                UmaRecipeStore.SaveMode(AvatarSystemMode.Stock);
                UmaRecipeStore.SaveStockSelection(CurrentSystemOption);
            }
            Debug.Log($"[UmaAvatarCustomizer] Saved — system '{SystemDisplayName()}', race {_recipe.raceId}, {_recipe.wardrobeIds.Count} wardrobe, {_recipe.dna.Count} dna, {_recipe.colors.Count} color(s).");

            if (CurrentSystemOption < 0 && (_previewBuildInProgress || _previewRebuildQueued))
            {
                _saveRefreshPending = true;
                if (_saveWaitStartedAt < 0d)
                {
                    _saveWaitTraceId = AvatarLoadTimingLog.NewTraceId("save");
                    _saveWaitStartedAt = AvatarLoadTimingLog.Now;
                    AvatarLoadTimingLog.Write(
                        _saveWaitTraceId,
                        "avatar-dispatch",
                        "WAIT_PREVIEW_START",
                        "home-customizer",
                        _saveWaitStartedAt,
                        "reason=prevent-overlapping-home-rebuild");
                }
                return;
            }

            RefreshHomeAfterSave();
        }

        private void RefreshHomeAfterSave()
        {
            if (_saveWaitStartedAt >= 0d)
            {
                AvatarLoadTimingLog.Write(
                    _saveWaitTraceId,
                    "avatar-dispatch",
                    "WAIT_PREVIEW_FINISH",
                    "home-customizer",
                    _saveWaitStartedAt);
                _saveWaitStartedAt = -1d;
            }
            _saveRefreshPending = false;

            if (!AvatarProviderSelection.RequestApplySavedAvatar())
                Debug.LogError(
                    "[UmaAvatarCustomizer] No UmaHomeAvatar is installed on the Home rig; " +
                    "the saved avatar cannot be applied. Run Tools/GHA/Install Avatar Configuration UI.");
        }

        // ---------------------------------------------------------------- preview

        private void BuildPreview()
        {
            if (!isActiveAndEnabled || !IsStationOpen() || (_embeddedPanelMode && !_providerSelected))
                return;

            SyncRecipe();
            UpdateStockPreview();
            bool stockSelected = CurrentSystemOption >= 0;

            if (!showPreviewMannequin || stockSelected)
            {
                if (_previewDca != null) _previewDca.gameObject.SetActive(false);
                _previewNeedsBuild = false;
                return;
            }

            RaceData race = catalog.Race(_recipe.raceId);
            if (!catalog.IsHumanoidRace(_recipe.raceId))
            {
                Debug.LogError("[UmaAvatarCustomizer] Selected body type needs a Humanoid race, T-pose and base recipe.");
                return;
            }

            if (_previewDca == null)
            {
                var go = new GameObject("GHA_UMA_Preview");
                if (previewParent != null)
                {
                    if (!previewParent.gameObject.activeSelf) previewParent.gameObject.SetActive(true);
                    // The authored anchor owns placement and display scale. Keep the new
                    // avatar at its origin; do not re-center it after UMA builds or DNA changes.
                    go.transform.SetParent(previewParent, false);
                }
                else
                {
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = previewLocalPosition;
                    go.transform.localRotation = Quaternion.Euler(previewLocalEuler);
                    go.transform.localScale = Vector3.one * previewScale;
                }
                _previewDca = go.AddComponent<DynamicCharacterAvatar>();
                _previewDca.activeRace.name = race.raceName;
                _previewDca.activeRace.data = race;
                _previewDca.loadFileOnStart = false;
                _previewDca.cacheCurrentState = false; // GHA's recipe and UI own the selected state.
                _previewDca.RecreateAnimatorOnRaceChange = true;
                if (previewAnimationController != null) _previewDca.animationController = previewAnimationController;
                _previewDca.CharacterBegun = _previewDca.CharacterBegun ?? new UMADataEvent();
                _previewDca.CharacterBegun.AddListener(OnPreviewCharacterBegun);
                _previewDca.CharacterUpdated = _previewDca.CharacterUpdated ?? new UMADataEvent();
                _previewDca.CharacterUpdated.AddListener(OnPreviewCharacterUpdated);
                ApplyWardrobeToPreview();
                ApplyPreviewDna();
                ApplyPreviewColors(false);
                BeginPreviewLoadTiming("initial");
                ShowLoading(true); // first build is the heaviest; the DCA builds on its next frame
                return;
            }

            if (!_previewDca.gameObject.activeSelf) _previewDca.gameObject.SetActive(true);
            bool raceChanged = _previewDca.activeRace.name != race.raceName;
            if (raceChanged)
            {
                // ChangeRace normally builds immediately. Stage the full outfit/DNA/colors first.
                _previewDca.BuildCharacterEnabled = false;
                _previewDca.cacheCurrentState = false;
                _previewDca.ChangeRace(race, DynamicCharacterAvatar.ChangeRaceOptions.keepBodyColors);
            }
            _previewDca.ClearSlots();
            ApplyWardrobeToPreview();
            ApplyPreviewDna();
            ApplyPreviewColors(false);
            BeginPreviewLoadTiming("rebuild");
            ShowLoading(true); // shown this frame; the queued build runs (and hitches) next frame
            if (raceChanged) _previewDca.BuildCharacterEnabled = true; // Enabling performs the one build.
            else _previewDca.BuildCharacter(true);
        }

        private void BeginPreviewLoadTiming(string buildKind)
        {
            if (_previewLoadStartedAt >= 0d)
            {
                AvatarLoadTimingLog.Write(
                    _previewLoadTraceId,
                    "avatar-preview",
                    "CANCELLED",
                    "home-customizer",
                    _previewLoadStartedAt,
                    "reason=SUPERSEDED");
            }

            _previewLoadTraceId = AvatarLoadTimingLog.NewTraceId("preview");
            _previewLoadStartedAt = AvatarLoadTimingLog.Now;
            _previewBuildInProgress = true;
            _previewNeedsBuild = false;
            AvatarLoadTimingLog.Write(
                _previewLoadTraceId,
                "avatar-preview",
                "START",
                "home-customizer",
                _previewLoadStartedAt,
                $"build={buildKind} race='{_previewDca.activeRace.name}' " +
                $"wardrobe={_recipe.wardrobeIds?.Count ?? 0} dna={_recipe.dna?.Count ?? 0} " +
                $"colors={_recipe.colors?.Count ?? 0}");
        }

        private void OnPreviewCharacterBegun(UMAData data)
        {
            if (_previewLoadStartedAt < 0d)
                return;

            AvatarLoadTimingLog.Write(
                _previewLoadTraceId,
                "avatar-preview",
                "UMA_CHARACTER_BEGUN",
                "home-customizer",
                _previewLoadStartedAt,
                $"rendererCount={(data != null ? data.RendererCount : 0)}");
        }

        private void OnPreviewCharacterUpdated(UMAData data)
        {
            ShowLoading(false);
            if (_previewLoadStartedAt < 0d)
                return;

            AvatarLoadTimingLog.Write(
                _previewLoadTraceId,
                "avatar-preview",
                "FINISH",
                "home-customizer",
                _previewLoadStartedAt,
                $"result=SUCCESS rendererCount={(data != null ? data.RendererCount : 0)}");
            _previewLoadStartedAt = -1d;
            _previewBuildInProgress = false;

            if (_previewRebuildQueued)
            {
                _previewRebuildQueued = false;
                BuildPreview();
                return;
            }

            if (_saveRefreshPending)
                RefreshHomeAfterSave();
        }

        private void ApplyWardrobeToPreview()
        {
            foreach (var kv in _slotSelection)
            {
                if (kv.Value < 0) continue;
                UMAWardrobeRecipe w = catalog.Wardrobe(kv.Value);
                if (w != null) _previewDca.SetSlot(w);
            }
        }

        private void ApplyPreviewDna()
        {
            if (_previewDca == null || _dna.Count == 0) return;
            var pre = new UMAPredefinedDNA();
            foreach (var kv in _dna)
            {
                string n = catalog.DnaName(kv.Key);
                if (!string.IsNullOrEmpty(n)) pre.AddDNA(n, kv.Value / 255f);
            }
            _previewDca.predefinedDNA = pre;
            _previewDca.keepPredefinedDNA = true;
        }

        private void ApplyPreviewColors(bool updateTexture)
        {
            if (_previewDca == null) return;
            bool any = false;
            foreach (var kv in _colors)
            {
                var ch = catalog.ColorChannel(kv.Key);
                OverlayColorData swatch = catalog.ColorSwatch(kv.Key, kv.Value);
                if (ch == null || string.IsNullOrEmpty(ch.channelName) || swatch == null) continue;
                // UMA 3 palette entries can include material property blocks (notably
                // hair base/root/tip colors), so preserve the complete color payload.
                _previewDca.SetRawColor(ch.channelName, swatch, false);
                any = true;
            }
            if (any && updateTexture)
                _previewDca.UpdateColors(true);
        }

        private void RebuildPreview()
        {
            SyncRecipe();
            _previewNeedsBuild = true;
            if (_previewBuildInProgress)
            {
                _previewRebuildQueued = true;
                return;
            }

            if (IsStationOpen() && (!_embeddedPanelMode || _providerSelected))
                BuildPreview();
        }

        private void RebuildColorsOnly()
        {
            SyncRecipe();
            ApplyPreviewColors(true);
        }

        private void UpdateStockPreview()
        {
            if (legacyPreviewRoot == null || CurrentSystemOption < 0) { if (legacyPreviewRoot != null) legacyPreviewRoot.SetActive(false); return; }
            int option = CurrentSystemOption;
            var datas = ProjectManager.instance != null && ProjectManager.instance.uiLayoutConfig != null
                ? ProjectManager.instance.uiLayoutConfig.avatarDatas : null;
            if (datas == null || option >= datas.Count) return;
            Transform headParent = legacyPreviewRoot.transform.Find("HeadParent");
            Transform bodyParent = legacyPreviewRoot.transform.Find("BodyParent");
            if (headParent == null || bodyParent == null) return;
            legacyPreviewRoot.SetActive(true);
            ClearChildren(headParent); ClearChildren(bodyParent);
            GameObject body = Instantiate(datas[option].body, bodyParent, false);
            GameObject head = Instantiate(datas[option].head, headParent, false);
            body.transform.localPosition = head.transform.localPosition = Vector3.zero;
        }

        // ---------------------------------------------------------------- station open/close + visibility

        private void CloseStationOnStartup()
        {
            if (_embeddedPanelMode || stationCanvas == null) return;
            Transform stationRoot = stationCanvas.transform.parent;
            if (stationRoot != null) stationRoot.gameObject.SetActive(false);
        }

        private void OnDeferredChangeAvatarClicked()
        {
            if (!delayOpenUntilHomeAvatarLoaded || _deferredOpenInProgress || homeAvatar == null)
                return;

            _deferredOpenInProgress = true;
            _deferredOpenTraceId = AvatarLoadTimingLog.NewTraceId("customizer-gate");
            _deferredOpenStartedAt = AvatarLoadTimingLog.Now;
            AvatarLoadProfilerCapture.Begin(_deferredOpenTraceId);
            AvatarLoadTimingLog.Write(
                _deferredOpenTraceId,
                "avatar-dispatch",
                "CHANGE_AVATAR_CLICKED",
                "home-customizer",
                _deferredOpenStartedAt);

            if (changeAvatarButton != null)
                changeAvatarButton.interactable = false;

            // The button's existing persistent listener activates Avatar Handler,
            // which is also the parent of this component. Do not call the stock
            // close method here: it deactivates Avatar Handler and prevents this
            // component from running the deferred-load coroutine. Hide only the
            // visible station so no preview build begins before Home UMA is ready.
            CloseStationOnStartup();

            StartCoroutine(OpenCustomizerAfterDeferredAvatarLoad());
        }

        private IEnumerator OpenCustomizerAfterDeferredAvatarLoad()
        {
            yield return homeAvatar.EnsureAvatarLoadedForCustomizer();

            VertexFormCore.AvatarSelectionManager manager =
                VertexFormCore.AvatarSelectionManager.Instance;
            if (manager != null)
                manager.OnTapChangeAvatar();

            if (changeAvatarButton != null)
                changeAvatarButton.interactable = true;

            AvatarLoadTimingLog.Write(
                _deferredOpenTraceId,
                "avatar-dispatch",
                "CUSTOMIZER_OPENED_AFTER_LOAD",
                "home-customizer",
                _deferredOpenStartedAt);
            StartCoroutine(AvatarLoadProfilerCapture.CompleteAfterFrames(_deferredOpenTraceId, 2));
            _deferredOpenStartedAt = -1d;
            _deferredOpenInProgress = false;
        }

        private static void SetActiveIfNeeded(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        // Resolve the Classic prev/next chevrons (serialized refs, else found by name under the
        // canvas) and wire them to cycle the Classic avatar. The buttons keep their existing legacy
        // listener too; it's suppressed and harmless, and our handler drives the visible preview.
        private void ResolveStockNavButtons()
        {
            _prevBtnGo = legacyPrevButton != null ? legacyPrevButton : FindCanvasChild("Button_Previous");
            _nextBtnGo = legacyNextButton != null ? legacyNextButton : FindCanvasChild("Button_Next");
            AddNavListener(_prevBtnGo, -1);
            AddNavListener(_nextBtnGo, +1);
        }

        private GameObject FindCanvasChild(string childName)
        {
            Transform t = UiRoot != null ? UiRoot.Find(childName) : null;
            return t != null ? t.gameObject : null;
        }

        private void AddNavListener(GameObject buttonGo, int dir)
        {
            if (buttonGo == null) return;
            Button b = buttonGo.GetComponent<Button>();
            if (b != null) b.onClick.AddListener(() => CycleStock(dir));
        }

        // Cycle which Classic avatar is selected (wrapping) and refresh the stock preview.
        private void CycleStock(int dir)
        {
            int count = StockAvatarCount();
            if (UmaMode || count <= 0) return;
            _stockIndex = (_stockIndex + dir + count) % count;
            RebuildPreview();
        }

        private void Update()
        {
            bool open = IsStationOpen();
            bool umaVisible =
                open &&
                (!_embeddedPanelMode || _providerSelected) &&
                showPreviewMannequin &&
                CurrentSystemOption < 0;
            bool stockVisible = open && CurrentSystemOption >= 0;
            if (_previewDca != null && _previewDca.gameObject.activeSelf != umaVisible)
                _previewDca.gameObject.SetActive(umaVisible);
            if (legacyPreviewRoot != null && legacyPreviewRoot.activeSelf != stockVisible)
                legacyPreviewRoot.SetActive(stockVisible);

            // Classic nav chevrons: shown only while the Classic system is active; hidden in UMA mode.
            bool showNav = open && CurrentSystemOption >= 0;
            SetActiveIfNeeded(_prevBtnGo, showNav);
            SetActiveIfNeeded(_nextBtnGo, showNav);

            // Swap the monitor mesh + grow/shrink the canvas on the open/close transition.
            if (open != _wasOpen)
            {
                ApplyScreenState(open);
                if (open)
                {
                    RefreshSystemRadio();
                    UpdateTabAvailability();
                    if (_previewNeedsBuild || (_previewDca == null && CurrentSystemOption < 0))
                        BuildPreview();
                }
                _wasOpen = open;
            }

        }

        // UMA's own slider pattern: apply the changed DNA right in the value-changed callback —
        // set the DnaSetter + a cheap DNA-only ForceUpdate (skeleton; the skinned mesh follows).
        // No per-frame polling.
        private bool IsStationOpen()
        {
            return _root != null && _root.gameObject.activeInHierarchy;
        }

        private void OnDisable()
        {
            // The preview is parented to the scene's authored anchor, not this UI.
            // Closing the station or deselecting Custom must hide it immediately.
            if (_previewDca != null)
                _previewDca.gameObject.SetActive(false);
        }

        private void DestroyPreview()
        {
            if (_previewDca == null)
                return;

            DynamicCharacterAvatar preview = _previewDca;
            _previewDca = null;
            preview.CharacterBegun?.RemoveListener(OnPreviewCharacterBegun);
            preview.CharacterUpdated?.RemoveListener(OnPreviewCharacterUpdated);
            preview.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(preview.gameObject);
            else
                DestroyImmediate(preview.gameObject);
        }

        private void OnSceneLoadStarting(string sceneName)
        {
            if (_previewLoadStartedAt >= 0d)
            {
                AvatarLoadTimingLog.Write(
                    _previewLoadTraceId,
                    "avatar-preview",
                    "CANCELLED",
                    "home-customizer",
                    _previewLoadStartedAt,
                    $"reason=SCENE_TRANSITION target='{sceneName}'");
            }

            _previewLoadStartedAt = -1d;
            _previewBuildInProgress = false;
            _previewRebuildQueued = false;
            _saveRefreshPending = false;
            if (_saveWaitStartedAt >= 0d)
            {
                AvatarLoadTimingLog.Write(
                    _saveWaitTraceId,
                    "avatar-dispatch",
                    "CANCELLED",
                    "home-customizer",
                    _saveWaitStartedAt,
                    $"reason=SCENE_TRANSITION target='{sceneName}'");
                _saveWaitStartedAt = -1d;
            }

            DestroyPreview();
        }

        private void OnDestroy()
        {
            // Reopening/rebuilding the Studio destroys its old UI. Release the
            // externally parented mannequin too, so no orphan remains visible.
            DestroyPreview();
            if (changeAvatarButton != null)
                changeAvatarButton.onClick.RemoveListener(OnDeferredChangeAvatarClicked);
            VertexFormCore.SceneLoader.SceneLoadStarting -= OnSceneLoadStarting;
        }

        private void SetPreviewDna(string dnaName, float value01)
        {
            if (_previewDca == null || _previewDca.umaData == null || CurrentSystemOption >= 0) return;
            var dna = _previewDca.GetDNA();
            if (dna != null && dna.TryGetValue(dnaName, out DnaSetter setter))
            {
                setter.Set(value01);
                _previewDca.ForceUpdate(true, false, false);
            }
        }

        /// <summary>
        /// Swaps the normal/tall monitor meshes and resizes the world-space canvas to match the
        /// customizer's open/closed state. Inert when the screen-swap fields are unset (null models,
        /// customizerCanvasHeight == 0), so it's safe to ship before the tall mesh exists.
        /// </summary>
        private void ApplyScreenState(bool open)
        {
            if (normalScreenModel != null) normalScreenModel.SetActive(!open);
            if (tallScreenModel != null) tallScreenModel.SetActive(open);

            if (customizerCanvasHeight > 0f && _canvasRt != null)
            {
                if (open)
                {
                    // Grow upward from a fixed bottom: bump height and lift the center by half the delta.
                    float delta = customizerCanvasHeight - _origCanvasSize.y;
                    _canvasRt.sizeDelta = new Vector2(_origCanvasSize.x, customizerCanvasHeight);
                    _canvasRt.anchoredPosition = new Vector2(_origCanvasPos.x, _origCanvasPos.y + delta * 0.5f);
                }
                else
                {
                    _canvasRt.sizeDelta = _origCanvasSize;
                    _canvasRt.anchoredPosition = _origCanvasPos;
                }
            }
        }

        // ---------------------------------------------------------------- UGUI helpers

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void FillParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static TMP_Text Label(Transform parent, string text, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.alignment = align; tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap; tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button TextButton(Transform parent, string text, float size, System.Action onClick, out Image bg)
        {
            RectTransform rt = NewRect("Btn_" + text, parent);
            bg = rt.gameObject.AddComponent<Image>();
            bg.color = TileBg;
            AvatarConfigurationTheme.ApplyRounded(bg);
            Outline border = rt.gameObject.AddComponent<Outline>();
            border.effectColor = AvatarConfigurationTheme.OptionBorder;
            border.effectDistance = new Vector2(2.5f, -2.5f);
            Button btn = rt.gameObject.AddComponent<Button>();
            AvatarConfigurationTheme.ConfigureButton(
                btn,
                bg,
                TileBg,
                AvatarConfigurationTheme.SurfaceHover,
                AvatarConfigurationTheme.AccentPressed);
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            TMP_Text lab = Label(rt, text, size, TextAlignmentOptions.Center, TextCol);
            lab.fontStyle = FontStyles.Bold;
            RectTransform labRt = lab.rectTransform; FillParent(labRt);
            return btn;
        }

        private void AddGridTile(Transform parent, string label, Sprite thumb, bool selected, System.Action onClick)
        {
            RectTransform rt = NewRect("Tile_" + label, parent);
            Image frame = rt.gameObject.AddComponent<Image>();
            frame.color = selected ? Accent : AvatarConfigurationTheme.OptionBorder;
            AvatarConfigurationTheme.ApplyRounded(frame);

            RectTransform surface = NewRect("Surface", rt);
            FillParent(surface);
            surface.offsetMin = new Vector2(3f, 3f);
            surface.offsetMax = new Vector2(-3f, -3f);
            Image bg = surface.gameObject.AddComponent<Image>();
            bg.color = TileBg;
            bg.raycastTarget = false;
            AvatarConfigurationTheme.ApplyRounded(bg);

            Button btn = rt.gameObject.AddComponent<Button>();
            AvatarConfigurationTheme.ConfigureButton(
                btn,
                bg,
                TileBg,
                AvatarConfigurationTheme.SurfaceHover,
                AvatarConfigurationTheme.AccentPressed);
            btn.onClick.AddListener(() => onClick());
            if (thumb != null)
            {
                RectTransform img = NewRect("Thumb", surface); FillParent(img);
                img.offsetMin = new Vector2(4f, 4f);
                img.offsetMax = new Vector2(-4f, -4f);
                Image im = img.gameObject.AddComponent<Image>();
                im.sprite = thumb; im.preserveAspect = true; im.raycastTarget = false;
            }
            else
            {
                TMP_Text lab = Label(surface, label, 18, TextAlignmentOptions.Center, selected ? Color.white : TextDim);
                FillParent(lab.rectTransform);
            }
        }

        private void AddSwatchTile(Transform parent, Color color, bool selected, System.Action onClick)
        {
            RectTransform rt = NewRect("Swatch", parent);
            Texture2D circleTexture = Resources.Load<Texture2D>("GHA/AvatarStudio/SwatchCircle");
            Texture2D ringTexture = Resources.Load<Texture2D>("GHA/AvatarStudio/SwatchRing");
            Texture2D lightingTexture = Resources.Load<Texture2D>("GHA/AvatarStudio/SwatchLighting");

            RectTransform shadowRoot = NewRect("Shadow", rt);
            FillParent(shadowRoot);
            shadowRoot.offsetMin = new Vector2(7f, 2f);
            shadowRoot.offsetMax = new Vector2(-3f, -8f);
            RawImage swatchShadow = shadowRoot.gameObject.AddComponent<RawImage>();
            swatchShadow.texture = circleTexture;
            swatchShadow.color = new Color(0f, 0f, 0f, 0.55f);
            swatchShadow.raycastTarget = false;

            RectTransform surface = NewRect("Surface", rt);
            FillParent(surface);
            surface.offsetMin = new Vector2(5f, 5f);
            surface.offsetMax = new Vector2(-5f, -5f);
            RawImage bg = surface.gameObject.AddComponent<RawImage>();
            bg.texture = circleTexture;
            bg.color = color;
            bg.raycastTarget = true;

            if (lightingTexture != null)
            {
                RectTransform lightingRoot = NewRect("Lighting", surface);
                FillParent(lightingRoot);
                RawImage lighting = lightingRoot.gameObject.AddComponent<RawImage>();
                lighting.texture = lightingTexture;
                lighting.color = Color.white;
                lighting.raycastTarget = false;
            }

            RectTransform ringRoot = NewRect("Selection Ring", rt);
            FillParent(ringRoot);
            ringRoot.offsetMin = Vector2.one;
            ringRoot.offsetMax = -Vector2.one;
            RawImage ring = ringRoot.gameObject.AddComponent<RawImage>();
            ring.texture = ringTexture;
            ring.color = selected ? Color.white : AvatarConfigurationTheme.OptionBorder;
            ring.raycastTarget = false;

            Button btn = rt.gameObject.AddComponent<Button>();
            AvatarConfigurationTheme.ConfigureButton(
                btn,
                bg,
                color,
                Color.Lerp(color, Color.white, 0.16f),
                Color.Lerp(color, Color.black, 0.12f));
            btn.onClick.AddListener(() => onClick());
        }

        // Instantiate the authored stock-slider prefab for a DNA row. The prefab carries the real
        // Unity slider (round knob, sprites baked in). Returns null if no prefab is assigned.
        private Slider BuildSlider(RectTransform parent)
        {
            FillParent(parent);
            if (sliderPrefab == null)
            {
                Debug.LogError("[UmaAvatarCustomizer] No Slider Prefab assigned on the UMA Customizer component — DNA sliders cannot be built.");
                return null;
            }
            Slider inst = Instantiate(sliderPrefab, parent, false);
            var prt = (RectTransform)inst.transform;
            StyleDnaSlider(inst);
            // Stretch to the row width but keep a fixed control height, so the handle remains round.
            const float height = 32f;
            prt.anchorMin = new Vector2(0f, 0.5f);
            prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(0f, height);
            inst.minValue = 0f; inst.maxValue = 1f;
            inst.direction = Slider.Direction.LeftToRight;
            return inst;
        }

        private static void StyleDnaSlider(Slider slider)
        {
            if (slider == null)
                return;

            Transform backgroundTransform = slider.transform.Find("Background");
            if (backgroundTransform != null)
            {
                RectTransform backgroundRect = backgroundTransform as RectTransform;
                backgroundRect.anchorMin = new Vector2(0f, 0.39f);
                backgroundRect.anchorMax = new Vector2(1f, 0.61f);
                backgroundRect.offsetMin = Vector2.zero;
                backgroundRect.offsetMax = Vector2.zero;
                Image background = backgroundTransform.GetComponent<Image>();
                if (background != null)
                {
                    background.color = new Color(0.035f, 0.043f, 0.067f, 1f);
                    // Keep the full track raycastable so users can click anywhere, not only drag
                    // the handle. The Slider on the parent receives the bubbled pointer event.
                    background.raycastTarget = true;
                    AvatarConfigurationTheme.ApplyRounded(background);
                    Outline trackBorder = background.gameObject.GetComponent<Outline>() ??
                        background.gameObject.AddComponent<Outline>();
                    trackBorder.effectColor = AvatarConfigurationTheme.Border;
                    trackBorder.effectDistance = new Vector2(1f, -1f);
                }
            }

            Transform fillAreaTransform = slider.transform.Find("Fill Area");
            if (fillAreaTransform is RectTransform fillArea)
            {
                fillArea.anchorMin = new Vector2(0f, 0.39f);
                fillArea.anchorMax = new Vector2(1f, 0.61f);
                fillArea.anchoredPosition = new Vector2(-7f, 0f);
                fillArea.sizeDelta = new Vector2(-28f, 0f);
            }

            Image fill = fillAreaTransform != null
                ? fillAreaTransform.Find("Fill")?.GetComponent<Image>()
                : null;
            if (fill != null)
            {
                fill.color = AvatarConfigurationTheme.Accent;
                fill.raycastTarget = false;
                AvatarConfigurationTheme.ApplyRounded(fill);
                Shadow fillGlow = fill.gameObject.GetComponent<Shadow>() ??
                    fill.gameObject.AddComponent<Shadow>();
                fillGlow.effectColor = new Color(
                    AvatarConfigurationTheme.Accent.r,
                    AvatarConfigurationTheme.Accent.g,
                    AvatarConfigurationTheme.Accent.b,
                    0.35f);
                fillGlow.effectDistance = new Vector2(1f, -1f);
                fillGlow.useGraphicAlpha = true;
            }

            Transform handleAreaTransform = slider.transform.Find("Handle Slide Area");
            if (handleAreaTransform is RectTransform handleArea)
                handleArea.sizeDelta = new Vector2(-30f, 0f);

            RectTransform handleRect = slider.handleRect;
            if (handleRect != null)
            {
                // Slider owns this rectangle and may stretch it vertically while updating its
                // anchors. Keep it as the forgiving pointer target, and put the visible knob in
                // an independently sized square child so the artwork can never become an oval.
                handleRect.anchorMin = new Vector2(handleRect.anchorMin.x, 0f);
                handleRect.anchorMax = new Vector2(handleRect.anchorMax.x, 1f);
                handleRect.sizeDelta = new Vector2(34f, 0f);

                Image authoredHandle = handleRect.GetComponent<Image>();
                if (authoredHandle != null)
                {
                    authoredHandle.enabled = false;
                    authoredHandle.raycastTarget = false;
                }

                Texture2D circleTexture = Resources.Load<Texture2D>("GHA/AvatarStudio/SwatchCircle");
                Texture2D ringTexture = Resources.Load<Texture2D>("GHA/AvatarStudio/SwatchRing");
                Texture2D lightingTexture = Resources.Load<Texture2D>("GHA/AvatarStudio/SwatchLighting");

                RectTransform visualRoot = NewRect("Visual", handleRect);
                visualRoot.anchorMin = visualRoot.anchorMax = new Vector2(0.5f, 0.5f);
                visualRoot.pivot = new Vector2(0.5f, 0.5f);
                visualRoot.anchoredPosition = Vector2.zero;
                visualRoot.sizeDelta = new Vector2(30f, 30f);

                RectTransform shadowRoot = NewRect("Shadow", visualRoot);
                FillParent(shadowRoot);
                shadowRoot.offsetMin = new Vector2(2f, -2f);
                shadowRoot.offsetMax = new Vector2(2f, -2f);
                RawImage handleShadow = shadowRoot.gameObject.AddComponent<RawImage>();
                handleShadow.texture = circleTexture;
                handleShadow.color = new Color(0f, 0f, 0f, 0.55f);
                handleShadow.raycastTarget = false;

                RectTransform surfaceRoot = NewRect("Surface", visualRoot);
                FillParent(surfaceRoot);
                surfaceRoot.offsetMin = new Vector2(2f, 2f);
                surfaceRoot.offsetMax = new Vector2(-2f, -2f);
                RawImage handleSurface = surfaceRoot.gameObject.AddComponent<RawImage>();
                handleSurface.texture = circleTexture;
                handleSurface.color = AvatarConfigurationTheme.Accent;
                handleSurface.raycastTarget = true;

                if (lightingTexture != null)
                {
                    RectTransform lightingRoot = NewRect("Lighting", surfaceRoot);
                    FillParent(lightingRoot);
                    RawImage lighting = lightingRoot.gameObject.AddComponent<RawImage>();
                    lighting.texture = lightingTexture;
                    lighting.color = Color.white;
                    lighting.raycastTarget = false;
                }

                RectTransform ringRoot = NewRect("Ring", visualRoot);
                FillParent(ringRoot);
                RawImage ring = ringRoot.gameObject.AddComponent<RawImage>();
                ring.texture = ringTexture;
                ring.color = new Color(1f, 1f, 1f, 0.9f);
                ring.raycastTarget = false;

                slider.targetGraphic = handleSurface;
                ColorBlock colors = slider.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
                colors.selectedColor = Color.white;
                colors.pressedColor = new Color(0.82f, 0.78f, 1f, 1f);
                colors.disabledColor = new Color(0.55f, 0.55f, 0.62f, 0.5f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                slider.colors = colors;
            }

            RectTransform ticks = NewRect("Ticks", slider.transform);
            FillParent(ticks);
            ticks.SetSiblingIndex(Mathf.Max(1, ticks.GetSiblingIndex() - 1));
            for (int i = 0; i < 5; i++)
            {
                RectTransform tick = NewRect("Tick " + i, ticks);
                float x = Mathf.Lerp(0.04f, 0.96f, i / 4f);
                tick.anchorMin = tick.anchorMax = new Vector2(x, 0.5f);
                tick.pivot = new Vector2(0.5f, 0.5f);
                tick.anchoredPosition = Vector2.zero;
                tick.sizeDelta = new Vector2(2f, i == 2 ? 10f : 6f);
                Image mark = tick.gameObject.AddComponent<Image>();
                Color markColor = i == 2
                    ? AvatarConfigurationTheme.TextSecondary
                    : AvatarConfigurationTheme.TextMuted;
                mark.color = new Color(markColor.r, markColor.g, markColor.b, i == 2 ? 0.65f : 0.38f);
                mark.raycastTarget = false;
            }
        }

        private RectTransform BuildScrollColumn(RectTransform parent)
        {
            RectTransform content = BuildScrollBase(parent, out var layoutGo);
            var v = layoutGo.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8f; v.padding = new RectOffset(4, 4, 4, 4);
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return content;
        }

        private RectTransform BuildScrollGrid(RectTransform parent, out ScrollRect scroll)
        {
            RectTransform content = BuildScrollBase(parent, out var layoutGo, out scroll);
            var g = layoutGo.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(196, 196);
            g.spacing = new Vector2(12, 12);
            g.padding = new RectOffset(4, 4, 4, 12);
            scroll.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = ScrollbarGap;
            return content;
        }

        private RectTransform BuildScrollBase(RectTransform parent, out GameObject contentGo) => BuildScrollBase(parent, out contentGo, out _);

        private const float ScrollbarWidth = 8f;   // thin scrollbar on the right
        private const float ScrollbarGap = 4f;      // gap between content and scrollbar

        private RectTransform BuildScrollBase(RectTransform parent, out GameObject contentGo, out ScrollRect scroll)
        {
            FillParent(parent);
            scroll = parent.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 24f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport leaves room on the right for the scrollbar so content never slides under it.
            RectTransform viewport = NewRect("Viewport", parent);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = new Vector2(-(ScrollbarWidth + ScrollbarGap), 0f);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0, content.offsetMin.y);
            content.offsetMax = new Vector2(0, content.offsetMax.y);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            // Thin, always-visible vertical scrollbar pinned to the right edge.
            scroll.verticalScrollbar = BuildVerticalScrollbar(parent);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            contentGo = content.gameObject;
            return content;
        }

        private Scrollbar BuildVerticalScrollbar(RectTransform parent)
        {
            RectTransform sb = NewRect("Scrollbar Vertical", parent);
            sb.anchorMin = new Vector2(1f, 0f); sb.anchorMax = new Vector2(1f, 1f);
            sb.pivot = new Vector2(1f, 1f);
            sb.sizeDelta = new Vector2(ScrollbarWidth, 0f);
            sb.anchoredPosition = Vector2.zero;
            Image track = sb.gameObject.AddComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.06f);

            Scrollbar bar = sb.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;

            RectTransform slideArea = NewRect("Sliding Area", sb);
            FillParent(slideArea);
            RectTransform handle = NewRect("Handle", slideArea);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one;
            handle.offsetMin = Vector2.zero; handle.offsetMax = Vector2.zero;
            Image handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = AvatarConfigurationTheme.Accent;

            bar.targetGraphic = handleImg;
            bar.handleRect = handle;
            return bar;
        }

        // ---------------------------------------------------------------- misc helpers

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        private string RaceDisplayName()
        {
            RaceData r = catalog.Race(_raceIndex);
            return r != null ? r.raceName : "(none)";
        }

        private Sprite GetThumb(UMAWardrobeRecipe recipe)
        {
            if (recipe == null || recipe.wardrobeRecipeThumbs == null) return null;
            string raceName = RaceDisplayName();
            Sprite fallback = null;
            foreach (WardrobeRecipeThumb entry in recipe.wardrobeRecipeThumbs)
            {
                if (entry == null || entry.thumb == null) continue;
                if (entry.race == raceName) return entry.thumb;
                if (fallback == null) fallback = entry.thumb;
            }
            return fallback;
        }

        private static bool IsFaceDna(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("head") || n.Contains("eye") || n.Contains("nose") || n.Contains("mouth")
                || n.Contains("jaw") || n.Contains("chin") || n.Contains("ear") || n.Contains("cheek")
                || n.Contains("lip") || n.Contains("forehead") || n.Contains("neck") || n.Contains("brow")
                || n.Contains("face") || n.Contains("mandible");
        }

        private static string Prettify(string dnaName)
        {
            if (string.IsNullOrEmpty(dnaName)) return dnaName;
            var sb = new System.Text.StringBuilder();
            sb.Append(char.ToUpperInvariant(dnaName[0]));
            for (int i = 1; i < dnaName.Length; i++)
            {
                if (char.IsUpper(dnaName[i])) sb.Append(' ');
                sb.Append(dnaName[i]);
            }
            return sb.ToString();
        }
    }
}
#endif
