#if VERTEXFORM_GHA_HOST
using System.Collections;
using GHA.AvatarFramework;
using GHA.AvatarFramework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VertexFormCore.GHAIntegration
{
    /// <summary>
    /// VertexForm contribution for the stock head/body avatar list. It keeps the stock
    /// selection key and manager behavior intact while presenting it inside the GHA shell.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VertexFormStockAvatarConfigurationProvider :
        AvatarConfigurationPanelProviderBehaviour
    {
        private const string StockSelectionPrefsKey = "Avatar_Selection_Number";
        private RectTransform _previewUiRoot;

        public override string ProviderId => "vertexform.stock";
        public override string DisplayName => "Classic";
        public override byte Mode => 0;
        public override int SortOrder => 0;

        public override bool IsAvailable
        {
            get
            {
                ProjectManager projectManager = ResolveProjectManager();
                return projectManager != null &&
                       projectManager.uiLayoutConfig != null &&
                       projectManager.uiLayoutConfig.avatarDatas != null &&
                       projectManager.uiLayoutConfig.avatarDatas.Count > 0;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RepairLegacySingletonsAfterSceneLoad()
        {
            ResolveProjectManager();
            ResolveSelectionManager();
        }

        public override GameObject CreatePanel(AvatarConfigurationPanelContext context)
        {
            _previewUiRoot = context.PreviewRoot;
            BuildCategoryRail(context.CategoryRoot);
            RectTransform root = NewRect("Stock Avatar Configuration", context.ContentRoot);
            FillParent(root);

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 22, 22);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text eyebrow = CreateLabel(
                root,
                "READY-MADE AVATARS",
                17f,
                28f,
                TextAlignmentOptions.MidlineLeft,
                AvatarConfigurationTheme.TextMuted);
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.characterSpacing = 1.8f;

            TMP_Text heading = CreateLabel(
                root,
                "Classic collection",
                32f,
                48f,
                TextAlignmentOptions.MidlineLeft,
                AvatarConfigurationTheme.TextPrimary);
            heading.fontStyle = FontStyles.Bold;
            CreateLabel(
                root,
                "Browse the included VertexForm avatars. Your choice is used the next time your player is created.",
                20f,
                58f,
                TextAlignmentOptions.TopLeft,
                AvatarConfigurationTheme.TextSecondary,
                true);

            RectTransform selectionCard = NewRect("Current Selection", root);
            LayoutElement selectionCardLayout =
                selectionCard.gameObject.AddComponent<LayoutElement>();
            selectionCardLayout.preferredHeight = 104f;
            Image selectionCardImage = selectionCard.gameObject.AddComponent<Image>();
            selectionCardImage.color = AvatarConfigurationTheme.SurfaceRaised;
            Outline selectionCardBorder = selectionCard.gameObject.AddComponent<Outline>();
            selectionCardBorder.effectColor = AvatarConfigurationTheme.Border;
            selectionCardBorder.effectDistance = new Vector2(1f, -1f);

            TMP_Text selection = CreateAnchoredLabel(
                selectionCard,
                "Selection",
                new Vector2(0.06f, 0.38f),
                new Vector2(0.94f, 0.92f),
                29f,
                TextAlignmentOptions.MidlineLeft,
                AvatarConfigurationTheme.TextPrimary);
            selection.fontStyle = FontStyles.Bold;
            CreateAnchoredLabel(
                selectionCard,
                "Use the arrows to update the live preview.",
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.43f),
                18f,
                TextAlignmentOptions.MidlineLeft,
                AvatarConfigurationTheme.TextSecondary);
            UpdateSelectionLabel(selection);

            RectTransform navigation = NewRect("Selection Navigation", root);
            LayoutElement navigationLayout = navigation.gameObject.AddComponent<LayoutElement>();
            navigationLayout.preferredHeight = 62f;
            var horizontal = navigation.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 10f;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;

            CreateButton(navigation, "‹  Previous", () =>
            {
                ChangeSelection(-1);
                UpdateSelectionLabel(selection);
            }, false);
            CreateButton(navigation, "Next  ›", () =>
            {
                ChangeSelection(1);
                UpdateSelectionLabel(selection);
            }, false);

            RectTransform spacer = NewRect("Flexible Spacer", root);
            LayoutElement spacerLayout = spacer.gameObject.AddComponent<LayoutElement>();
            spacerLayout.flexibleHeight = 1f;

            CreateSaveButton(root, () =>
            {
                AvatarProviderSelection.SaveMode(Mode);
                AvatarProviderSelection.RequestApplySavedAvatar();
                UpdateSelectionLabel(selection, "Saved");
            });

            return root.gameObject;
        }

        private static void BuildCategoryRail(RectTransform categoryRoot)
        {
            if (categoryRoot == null)
                return;

            float tileSize = categoryRoot.rect.width > 20f
                ? categoryRoot.rect.width - 12f
                : 64f;
            var layout = categoryRoot.gameObject.AddComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 8, 8);
            layout.spacing = new Vector2(0f, 8f);
            layout.cellSize = new Vector2(tileSize, tileSize);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 1;

            RectTransform tile = NewRect("Tab_Avatar", categoryRoot);
            Image background = tile.gameObject.AddComponent<Image>();
            background.color = AvatarConfigurationTheme.Accent;
            AvatarConfigurationTheme.ApplyRounded(background);
            Outline border = tile.gameObject.AddComponent<Outline>();
            border.effectColor = AvatarConfigurationTheme.Border;
            border.effectDistance = new Vector2(1f, -1f);
            RectTransform iconRoot = NewRect("Avatar Icon", tile);
            iconRoot.anchorMin = new Vector2(0.30f, 0.42f);
            iconRoot.anchorMax = new Vector2(0.70f, 0.92f);
            iconRoot.offsetMin = Vector2.zero;
            iconRoot.offsetMax = Vector2.zero;
            AddIconShape(
                iconRoot,
                "Head",
                new Vector2(0.39f, 0.58f),
                new Vector2(0.61f, 0.88f));
            AddIconShape(
                iconRoot,
                "Torso",
                new Vector2(0.27f, 0.10f),
                new Vector2(0.73f, 0.57f));

            TMP_Text label = CreateAnchoredLabel(
                tile,
                "Avatar",
                new Vector2(0.06f, 0.04f),
                new Vector2(0.94f, 0.36f),
                17f,
                TextAlignmentOptions.Center,
                AvatarConfigurationTheme.TextPrimary);
            label.fontStyle = FontStyles.Bold;
        }

        private static void AddIconShape(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform shape = NewRect(name, parent);
            shape.anchorMin = anchorMin;
            shape.anchorMax = anchorMax;
            shape.offsetMin = Vector2.zero;
            shape.offsetMax = Vector2.zero;
            Image image = shape.gameObject.AddComponent<Image>();
            image.color = AvatarConfigurationTheme.TextPrimary;
            image.raycastTarget = false;
            AvatarConfigurationTheme.ApplyRounded(image);
        }

        public override void SetSelected(bool selected)
        {
            AvatarSelectionManager manager = ResolveSelectionManager();
            ResolveProjectManager();
            Transform previewRoot = manager != null && manager.headParent != null
                ? manager.headParent.parent
                : null;
            // With SuppressLegacyAvatars set, the manager refreshes only its platform preview;
            // the player's body changes on Save through the host, never while browsing.
            if (selected && manager != null && previewRoot != null &&
                previewRoot.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                manager.InitializeAvatarSystem();
            }
            if (previewRoot != null && previewRoot.gameObject.activeSelf != selected)
                previewRoot.gameObject.SetActive(selected);
            if (selected && previewRoot != null)
                StartCoroutine(FramePreviewAfterLayout(previewRoot));
        }

        private void ChangeSelection(int direction)
        {
            AvatarSelectionManager manager = ResolveSelectionManager();
            if (manager != null)
            {
                ResolveProjectManager();
                if (direction < 0)
                    manager.PreviousAvatar();
                else
                    manager.NextAvatar();
                if (manager.headParent != null)
                    StartCoroutine(FramePreviewAfterLayout(manager.headParent.parent));
                return;
            }

            int count = AvatarCount();
            if (count <= 0)
                return;

            int current = PlayerPrefs.GetInt(StockSelectionPrefsKey, 0);
            int next = (current + direction + count) % count;
            PlayerPrefs.SetInt(StockSelectionPrefsKey, next);
            PlayerPrefs.Save();
        }

        private IEnumerator FramePreviewAfterLayout(Transform previewRoot)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            AvatarPreviewFraming.FitRenderersToRect(previewRoot, _previewUiRoot);
        }

        private static int AvatarCount()
        {
            ProjectManager projectManager = ResolveProjectManager();
            return projectManager != null &&
                   projectManager.uiLayoutConfig != null &&
                   projectManager.uiLayoutConfig.avatarDatas != null
                ? projectManager.uiLayoutConfig.avatarDatas.Count
                : 0;
        }

        private static ProjectManager ResolveProjectManager()
        {
            if (ProjectManager.instance != null)
                return ProjectManager.instance;

            ProjectManager projectManager = Object.FindFirstObjectByType<ProjectManager>(
                FindObjectsInactive.Include);
            if (projectManager != null)
                ProjectManager.instance = projectManager;
            return projectManager;
        }

        private static AvatarSelectionManager ResolveSelectionManager()
        {
            if (AvatarSelectionManager.Instance != null)
                return AvatarSelectionManager.Instance;

            AvatarSelectionManager manager = Object.FindFirstObjectByType<AvatarSelectionManager>(
                FindObjectsInactive.Include);
            if (manager != null)
                AvatarSelectionManager.Instance = manager;
            return manager;
        }

        private static void UpdateSelectionLabel(TMP_Text label, string suffix = null)
        {
            int count = AvatarCount();
            int selected = count > 0
                ? Mathf.Clamp(PlayerPrefs.GetInt(StockSelectionPrefsKey, 0), 0, count - 1)
                : 0;
            label.text = count > 0
                ? $"Avatar {selected + 1:00}  /  {count:00}{FormatSuffix(suffix)}"
                : "No stock avatars are configured.";
        }

        private static string FormatSuffix(string suffix)
        {
            return string.IsNullOrWhiteSpace(suffix) ? string.Empty : $"   •   {suffix}";
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static void FillParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string text,
            float fontSize,
            float height,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            Color? color = null,
            bool wrap = false)
        {
            RectTransform root = NewRect("Label", parent);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            var label = root.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color ?? AvatarConfigurationTheme.TextPrimary;
            label.textWrappingMode = wrap
                ? TextWrappingModes.Normal
                : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private static TMP_Text CreateAnchoredLabel(
            Transform parent,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            RectTransform root = NewRect("Label", parent);
            root.anchorMin = anchorMin;
            root.anchorMax = anchorMax;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            var label = root.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction onClick,
            bool primary,
            float preferredHeight = 60f)
        {
            RectTransform root = NewRect(label + " Button", parent);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            Image image = root.gameObject.AddComponent<Image>();
            image.color = primary
                ? AvatarConfigurationTheme.Accent
                : AvatarConfigurationTheme.SurfaceRaised;
            Button button = root.gameObject.AddComponent<Button>();
            AvatarConfigurationTheme.ConfigureButton(
                button,
                image,
                primary ? AvatarConfigurationTheme.Accent : AvatarConfigurationTheme.SurfaceRaised,
                primary ? AvatarConfigurationTheme.AccentHover : AvatarConfigurationTheme.SurfaceHover,
                primary ? AvatarConfigurationTheme.AccentPressed : AvatarConfigurationTheme.Canvas);
            button.onClick.AddListener(onClick);

            RectTransform textRoot = NewRect("Label", root);
            FillParent(textRoot);
            var text = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = primary ? 22f : 21f;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = primary ? 1.2f : 0f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = AvatarConfigurationTheme.TextPrimary;
            text.raycastTarget = false;
            return button;
        }

        private static Button CreateSaveButton(
            Transform parent,
            UnityEngine.Events.UnityAction onClick)
        {
            RectTransform root = NewRect("GHA_SaveButton", parent);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            root.anchorMin = new Vector2(1f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(1f, 0f);
            root.sizeDelta = new Vector2(
                AvatarConfigurationTheme.EmbeddedSaveButtonWidth,
                AvatarConfigurationTheme.SaveButtonHeight);
            root.anchoredPosition = new Vector2(
                -AvatarConfigurationTheme.SaveButtonMargin,
                AvatarConfigurationTheme.SaveButtonMargin);

            Image image = root.gameObject.AddComponent<Image>();
            image.color = AvatarConfigurationTheme.Accent;
            Button button = root.gameObject.AddComponent<Button>();
            AvatarConfigurationTheme.ConfigureButton(
                button,
                image,
                AvatarConfigurationTheme.Accent,
                AvatarConfigurationTheme.AccentHover,
                AvatarConfigurationTheme.AccentPressed);
            button.onClick.AddListener(onClick);

            RectTransform textRoot = NewRect("Label", root);
            FillParent(textRoot);
            var text = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "SAVE AVATAR";
            text.fontSize = 19f;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 1.2f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = AvatarConfigurationTheme.TextPrimary;
            text.raycastTarget = false;
            return button;
        }
    }
}
#endif
