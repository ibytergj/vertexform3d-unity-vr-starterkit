using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GHA.AvatarFramework.UI
{
    /// <summary>
    /// Shared runtime palette and interaction states for the provider-neutral avatar studio.
    /// Providers use these tokens so optional integrations feel like one product without moving
    /// provider-specific UI into the core package.
    /// </summary>
    public static class AvatarConfigurationTheme
    {
        public const float EmbeddedSaveButtonWidth = 176f;
        public const float SaveButtonHeight = 56f;
        public const float SaveButtonMargin = 12f;

        public static readonly Color Canvas = new(0.035f, 0.043f, 0.067f, 0.985f);
        public static readonly Color Surface = new(0.075f, 0.086f, 0.125f, 0.98f);
        public static readonly Color PreviewBackdrop = Color.black;
        public static readonly Color SurfaceRaised = new(0.105f, 0.12f, 0.17f, 1f);
        public static readonly Color SurfaceHover = new(0.14f, 0.155f, 0.215f, 1f);
        public static readonly Color Border = new(0.55f, 0.59f, 0.72f, 0.28f);
        public static readonly Color OptionBorder = new(0.55f, 0.59f, 0.72f, 0.72f);
        public static readonly Color Accent = new(0.455f, 0.30f, 0.96f, 1f);
        public static readonly Color AccentHover = new(0.54f, 0.40f, 1f, 1f);
        public static readonly Color AccentPressed = new(0.37f, 0.22f, 0.84f, 1f);
        public static readonly Color AccentSoft = new(0.455f, 0.30f, 0.96f, 0.24f);
        public static readonly Color TextPrimary = new(0.96f, 0.97f, 1f, 1f);
        public static readonly Color TextSecondary = new(0.68f, 0.71f, 0.80f, 1f);
        public static readonly Color TextMuted = new(0.49f, 0.52f, 0.62f, 1f);
        public static readonly Color Success = new(0.30f, 0.82f, 0.61f, 1f);

        private static Sprite _roundedPanel;
        private static Sprite _roundedBorder;

        private static Sprite RequireChromeSprite(ref Sprite cached, string name)
        {
            if (cached == null)
                cached = Resources.Load<Sprite>("GHA/AvatarStudio/Chrome/" + name);
            if (cached == null)
                throw new MissingReferenceException("Avatar Studio requires its packaged UI sprite: " + name);
            return cached;
        }

        public static void ApplyRounded(Image image)
        {
            if (image == null)
                return;

            // Persistent package asset, included in players; no Editor-only resource lookup
            // and no runtime-created texture/sprite lifetime to manage across domain reloads.
            image.sprite = RequireChromeSprite(ref _roundedPanel, "RoundedPanel");
            image.type = Image.Type.Sliced;
        }

        public static void ApplyRoundedBorder(Image image)
        {
            if (image == null)
                return;
            image.sprite = RequireChromeSprite(ref _roundedBorder, "RoundedBorder");
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.raycastTarget = false;
        }

        public static void ConfigureButton(
            Button button,
            Graphic target,
            Color normal,
            Color highlighted,
            Color pressed)
        {
            button.targetGraphic = target;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.selectedColor = highlighted;
            colors.pressedColor = pressed;
            colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.38f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }
    }

    /// <summary>
    /// Positions world-space avatar previews against the provider preview rectangle. Providers
    /// may either fit complete bounds or preserve authored scale and align to an edge.
    /// </summary>
    public static class AvatarPreviewFraming
    {
        public const float DefaultFill = 0.94f;

        public static bool FitRenderersToRect(
            Transform rendererRoot,
            RectTransform targetRect,
            float fill = DefaultFill)
        {
            if (rendererRoot == null || targetRect == null)
                return false;

            Canvas canvas = targetRect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.worldCamera != null
                ? canvas.worldCamera
                : Camera.main;
            if (camera == null)
                return false;

            if (!TryGetScreenRect(targetRect, camera, out Rect targetScreenRect) ||
                !TryGetRendererBounds(rendererRoot, out Bounds rendererBounds) ||
                !TryGetScreenRect(rendererBounds, camera, out Rect rendererScreenRect) ||
                rendererScreenRect.width <= Mathf.Epsilon ||
                rendererScreenRect.height <= Mathf.Epsilon)
            {
                return false;
            }

            float clampedFill = Mathf.Clamp(fill, 0.1f, 1f);
            float scaleFactor = Mathf.Min(
                targetScreenRect.width * clampedFill / rendererScreenRect.width,
                targetScreenRect.height * clampedFill / rendererScreenRect.height);
            if (!float.IsFinite(scaleFactor) || scaleFactor <= Mathf.Epsilon)
                return false;

            rendererRoot.localScale *= scaleFactor;
            CenterRendererBounds(rendererRoot, targetScreenRect, camera);
            CenterRendererBounds(rendererRoot, targetScreenRect, camera);
            return true;
        }

        public static bool AlignRenderersToBottomCenter(
            Transform rendererRoot,
            RectTransform targetRect)
        {
            if (rendererRoot == null || targetRect == null)
                return false;

            Canvas canvas = targetRect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.worldCamera != null
                ? canvas.worldCamera
                : Camera.main;
            if (camera == null ||
                !TryGetScreenRect(targetRect, camera, out Rect targetScreenRect))
            {
                return false;
            }

            AlignRendererBoundsToBottomCenter(rendererRoot, targetScreenRect, camera);
            AlignRendererBoundsToBottomCenter(rendererRoot, targetScreenRect, camera);
            return true;
        }

        private static void CenterRendererBounds(
            Transform rendererRoot,
            Rect targetScreenRect,
            Camera camera)
        {
            if (!TryGetRendererBounds(rendererRoot, out Bounds rendererBounds) ||
                !TryGetScreenRect(rendererBounds, camera, out Rect rendererScreenRect))
            {
                return;
            }

            Vector3 boundsScreenCenter = camera.WorldToScreenPoint(rendererBounds.center);
            Vector2 screenOffset = targetScreenRect.center - rendererScreenRect.center;
            Vector3 currentWorld = camera.ScreenToWorldPoint(boundsScreenCenter);
            Vector3 desiredWorld = camera.ScreenToWorldPoint(new Vector3(
                boundsScreenCenter.x + screenOffset.x,
                boundsScreenCenter.y + screenOffset.y,
                boundsScreenCenter.z));
            rendererRoot.position += desiredWorld - currentWorld;
        }

        private static void AlignRendererBoundsToBottomCenter(
            Transform rendererRoot,
            Rect targetScreenRect,
            Camera camera)
        {
            if (!TryGetRendererBounds(rendererRoot, out Bounds rendererBounds) ||
                !TryGetScreenRect(rendererBounds, camera, out Rect rendererScreenRect))
            {
                return;
            }

            Vector3 boundsScreenCenter = camera.WorldToScreenPoint(rendererBounds.center);
            Vector2 screenOffset = new Vector2(
                targetScreenRect.center.x - rendererScreenRect.center.x,
                targetScreenRect.yMin - rendererScreenRect.yMin);
            Vector3 currentWorld = camera.ScreenToWorldPoint(boundsScreenCenter);
            Vector3 desiredWorld = camera.ScreenToWorldPoint(new Vector3(
                boundsScreenCenter.x + screenOffset.x,
                boundsScreenCenter.y + screenOffset.y,
                boundsScreenCenter.z));
            rendererRoot.position += desiredWorld - currentWorld;
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
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

        private static bool TryGetScreenRect(
            RectTransform rectTransform,
            Camera camera,
            out Rect screenRect)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            screenRect = default;
            bool found = false;
            foreach (Vector3 corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                Encapsulate(point, ref screenRect, ref found);
            }
            return found;
        }

        private static bool TryGetScreenRect(
            Bounds bounds,
            Camera camera,
            out Rect screenRect)
        {
            screenRect = default;
            bool found = false;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 worldPoint = bounds.center + Vector3.Scale(
                    bounds.extents,
                    new Vector3(x, y, z));
                Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);
                if (screenPoint.z <= 0f)
                    continue;
                Encapsulate(screenPoint, ref screenRect, ref found);
            }
            return found;
        }

        private static void Encapsulate(Vector2 point, ref Rect rect, ref bool found)
        {
            if (!found)
            {
                rect = new Rect(point, Vector2.zero);
                found = true;
                return;
            }

            float xMin = Mathf.Min(rect.xMin, point.x);
            float yMin = Mathf.Min(rect.yMin, point.y);
            float xMax = Mathf.Max(rect.xMax, point.x);
            float yMax = Mathf.Max(rect.yMax, point.y);
            rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }

    public readonly struct AvatarConfigurationPanelContext
    {
        public AvatarConfigurationPanelContext(
            RectTransform categoryRoot,
            RectTransform contentRoot,
            RectTransform previewRoot,
            Canvas canvas)
        {
            CategoryRoot = categoryRoot;
            ContentRoot = contentRoot;
            PreviewRoot = previewRoot;
            Canvas = canvas;
        }

        public RectTransform CategoryRoot { get; }
        public RectTransform ContentRoot { get; }
        public RectTransform PreviewRoot { get; }
        public Canvas Canvas { get; }
    }

    /// <summary>
    /// A provider-contributed avatar configuration surface. Provider mode values are stable
    /// transport identifiers; provider IDs are stable UI identities.
    /// </summary>
    public interface IAvatarConfigurationPanelProvider
    {
        string ProviderId { get; }
        string DisplayName { get; }
        byte Mode { get; }
        int SortOrder { get; }
        bool IsAvailable { get; }
        GameObject CreatePanel(AvatarConfigurationPanelContext context);
        void SetSelected(bool selected);
    }

    /// <summary>
    /// Runtime registry for optional avatar configuration providers.
    /// </summary>
    public static class AvatarConfigurationProviderRegistry
    {
        private static readonly List<IAvatarConfigurationPanelProvider> ProvidersInternal = new();

        public static event Action Changed;

        public static IReadOnlyList<IAvatarConfigurationPanelProvider> Providers
        {
            get
            {
                RemoveDestroyedProviders();
                return ProvidersInternal;
            }
        }

        public static void Register(IAvatarConfigurationPanelProvider provider)
        {
            if (provider == null || ProvidersInternal.Contains(provider))
                return;

            ProvidersInternal.Add(provider);
            Changed?.Invoke();
        }

        public static void Unregister(IAvatarConfigurationPanelProvider provider)
        {
            if (provider == null || !ProvidersInternal.Remove(provider))
                return;

            Changed?.Invoke();
        }

        private static void RemoveDestroyedProviders()
        {
            ProvidersInternal.RemoveAll(provider =>
                provider == null ||
                provider is UnityEngine.Object unityObject && unityObject == null);
        }
    }

    public abstract class AvatarConfigurationPanelProviderBehaviour :
        MonoBehaviour,
        IAvatarConfigurationPanelProvider
    {
        public abstract string ProviderId { get; }
        public abstract string DisplayName { get; }
        public abstract byte Mode { get; }
        public virtual int SortOrder => 0;
        public virtual bool IsAvailable => true;
        public abstract GameObject CreatePanel(AvatarConfigurationPanelContext context);
        public virtual void SetSelected(bool selected)
        {
        }

        protected virtual void OnEnable()
        {
            AvatarConfigurationProviderRegistry.Register(this);
        }

        protected virtual void OnDisable()
        {
            AvatarConfigurationProviderRegistry.Unregister(this);
        }
    }

    /// <summary>
    /// Provider-neutral custom-panel shell. It owns panel chrome and provider switching while
    /// installed providers own only their configuration content.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AvatarConfigurationPanel : MonoBehaviour
    {
        private readonly List<ProviderView> _providerViews = new();
        private RectTransform _generatedRoot;
        private RectTransform _navigationRoot;
        private RectTransform _categoryRoot;
        private RectTransform _contentRoot;
        private RectTransform _previewRoot;
        private Canvas _canvas;
        private TMP_Text _previewCaption;
        private string _selectedProviderId;
        private bool _rebuildScheduled;

        private sealed class ProviderView
        {
            public IAvatarConfigurationPanelProvider Provider;
            public GameObject CategoryPanel;
            public GameObject Panel;
            public Image ButtonImage;
            public TMP_Text ButtonLabel;
        }

        private void OnEnable()
        {
            AvatarConfigurationProviderRegistry.Changed += ScheduleRebuild;
            ScheduleRebuild();
        }

        private void OnDisable()
        {
            AvatarConfigurationProviderRegistry.Changed -= ScheduleRebuild;
            _rebuildScheduled = false;
        }

        private void ScheduleRebuild()
        {
            if (!isActiveAndEnabled || _rebuildScheduled)
                return;

            _rebuildScheduled = true;
            StartCoroutine(RebuildNextFrame());
        }

        private IEnumerator RebuildNextFrame()
        {
            yield return null;
            _rebuildScheduled = false;
            Rebuild();
        }

        private void Rebuild()
        {
            DestroyGeneratedRoot();
            BuildShell();

            List<IAvatarConfigurationPanelProvider> providers =
                AvatarConfigurationProviderRegistry.Providers
                    .Where(IsProviderInThisPanel)
                    .Where(provider => provider.IsAvailable)
                    .GroupBy(provider => provider.ProviderId, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .OrderBy(provider => provider.SortOrder)
                    .ThenBy(provider => provider.DisplayName, StringComparer.Ordinal)
                    .ToList();

            if (providers.Count == 0)
            {
                CreateLabel(_contentRoot, "No avatar configuration providers are available.", 30f);
                return;
            }

            if (string.IsNullOrEmpty(_selectedProviderId) ||
                providers.All(provider => provider.ProviderId != _selectedProviderId))
            {
                byte selectedMode = AvatarProviderSelection.LoadMode(providers[0].Mode);
                IAvatarConfigurationPanelProvider selected =
                    providers.FirstOrDefault(provider => provider.Mode == selectedMode) ?? providers[0];
                _selectedProviderId = selected.ProviderId;
            }

            foreach (IAvatarConfigurationPanelProvider provider in providers)
                AddProvider(provider);

            ApplySelection();
        }

        private bool IsProviderInThisPanel(IAvatarConfigurationPanelProvider provider)
        {
            if (provider is not Component component || component == null)
                return false;

            return component.transform == transform || component.transform.IsChildOf(transform);
        }

        private void BuildShell()
        {
            _canvas = GetComponentInParent<Canvas>(true);
            _generatedRoot = NewRect("GHA Avatar Panel Shell", transform);
            FillParent(_generatedRoot);

            BuildBackingFrame(_generatedRoot);

            RectTransform title = NewRect("Title", _generatedRoot);
            title.anchorMin = new Vector2(0.035f, 0.905f);
            title.anchorMax = new Vector2(0.965f, 0.975f);
            title.offsetMin = Vector2.zero;
            title.offsetMax = Vector2.zero;
            TMP_Text titleLabel = CreateLabel(
                title,
                "AVATAR STUDIO",
                42f,
                TextAlignmentOptions.MidlineLeft,
                AvatarConfigurationTheme.TextPrimary);
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.characterSpacing = 1.5f;

            RectTransform subtitle = NewRect("Subtitle", _generatedRoot);
            subtitle.anchorMin = new Vector2(0.035f, 0.852f);
            subtitle.anchorMax = new Vector2(0.965f, 0.91f);
            subtitle.offsetMin = Vector2.zero;
            subtitle.offsetMax = Vector2.zero;
            CreateLabel(
                subtitle,
                "Choose a style, then make it yours.",
                21f,
                TextAlignmentOptions.MidlineLeft,
                AvatarConfigurationTheme.TextSecondary);

            _navigationRoot = NewRect("Provider Navigation", _generatedRoot);
            _navigationRoot.anchorMin = new Vector2(0.035f, 0.755f);
            _navigationRoot.anchorMax = new Vector2(0.965f, 0.835f);
            _navigationRoot.offsetMin = Vector2.zero;
            _navigationRoot.offsetMax = Vector2.zero;
            Image navBackground = _navigationRoot.gameObject.AddComponent<Image>();
            navBackground.color = AvatarConfigurationTheme.Surface;
            AvatarConfigurationTheme.ApplyRounded(navBackground);
            AddBorder(_navigationRoot);
            HorizontalLayoutGroup navigationLayout =
                _navigationRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            navigationLayout.padding = new RectOffset(8, 8, 8, 8);
            navigationLayout.spacing = 8f;
            navigationLayout.childControlWidth = true;
            navigationLayout.childControlHeight = true;
            navigationLayout.childForceExpandWidth = false;
            navigationLayout.childForceExpandHeight = true;

            RectTransform previewCard = NewRect("Preview Card", _generatedRoot);
            previewCard.anchorMin = new Vector2(0.145f, 0.055f);
            previewCard.anchorMax = new Vector2(0.54f, 0.72f);
            previewCard.offsetMin = Vector2.zero;
            previewCard.offsetMax = Vector2.zero;
            Image previewImage = previewCard.gameObject.AddComponent<Image>();
            previewImage.color = AvatarConfigurationTheme.PreviewBackdrop;
            AvatarConfigurationTheme.ApplyRounded(previewImage);
            AddBorder(previewCard);

            RectTransform previewEyebrow = NewRect("Preview Eyebrow", previewCard);
            previewEyebrow.anchorMin = new Vector2(0.065f, 0.90f);
            previewEyebrow.anchorMax = new Vector2(0.935f, 0.975f);
            previewEyebrow.offsetMin = Vector2.zero;
            previewEyebrow.offsetMax = Vector2.zero;
            _previewCaption = CreateLabel(
                previewEyebrow,
                "LIVE PREVIEW",
                18f,
                TextAlignmentOptions.MidlineLeft,
                AvatarConfigurationTheme.TextMuted);
            _previewCaption.fontStyle = FontStyles.Bold;
            _previewCaption.characterSpacing = 2f;

            _previewRoot = NewRect("Provider Preview", previewCard);
            _previewRoot.anchorMin = new Vector2(0.04f, 0.035f);
            _previewRoot.anchorMax = new Vector2(0.96f, 0.89f);
            _previewRoot.offsetMin = Vector2.zero;
            _previewRoot.offsetMax = Vector2.zero;

            RectTransform categoryCard = NewRect("Category Card", _generatedRoot);
            categoryCard.anchorMin = new Vector2(0.035f, 0.055f);
            categoryCard.anchorMax = new Vector2(0.125f, 0.72f);
            categoryCard.offsetMin = Vector2.zero;
            categoryCard.offsetMax = Vector2.zero;
            Image categoryImage = categoryCard.gameObject.AddComponent<Image>();
            categoryImage.color = AvatarConfigurationTheme.Surface;
            AvatarConfigurationTheme.ApplyRounded(categoryImage);
            AddBorder(categoryCard);

            _categoryRoot = NewRect("Provider Categories", categoryCard);
            FillParent(_categoryRoot);
            _categoryRoot.offsetMin = new Vector2(10f, 12f);
            _categoryRoot.offsetMax = new Vector2(-10f, -12f);

            RectTransform contentCard = NewRect("Controls Card", _generatedRoot);
            contentCard.anchorMin = new Vector2(0.56f, 0.055f);
            contentCard.anchorMax = new Vector2(0.965f, 0.72f);
            contentCard.offsetMin = Vector2.zero;
            contentCard.offsetMax = Vector2.zero;
            Image contentImage = contentCard.gameObject.AddComponent<Image>();
            contentImage.color = AvatarConfigurationTheme.Surface;
            AvatarConfigurationTheme.ApplyRounded(contentImage);
            AddBorder(contentCard);

            _contentRoot = NewRect("Provider Content", contentCard);
            _contentRoot.anchorMin = Vector2.zero;
            _contentRoot.anchorMax = Vector2.one;
            _contentRoot.offsetMin = new Vector2(12f, 12f);
            _contentRoot.offsetMax = new Vector2(-12f, -12f);
        }

        private void AddProvider(IAvatarConfigurationPanelProvider provider)
        {
            RectTransform buttonRoot = NewRect(provider.ProviderId + " Provider Button", _navigationRoot);
            LayoutElement layout = buttonRoot.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 220f;
            layout.minHeight = 52f;
            layout.flexibleWidth = 0f;

            Image buttonImage = buttonRoot.gameObject.AddComponent<Image>();
            buttonImage.color = AvatarConfigurationTheme.SurfaceRaised;
            AvatarConfigurationTheme.ApplyRounded(buttonImage);
            Button button = buttonRoot.gameObject.AddComponent<Button>();
            AvatarConfigurationTheme.ConfigureButton(
                button,
                buttonImage,
                AvatarConfigurationTheme.SurfaceRaised,
                AvatarConfigurationTheme.SurfaceHover,
                AvatarConfigurationTheme.AccentPressed);
            TMP_Text buttonLabel = CreateLabel(
                buttonRoot,
                provider.DisplayName,
                24f,
                TextAlignmentOptions.Center,
                AvatarConfigurationTheme.TextSecondary);
            buttonLabel.fontStyle = FontStyles.Bold;
            button.onClick.AddListener(() => SelectProvider(provider.ProviderId));

            RectTransform categoryPanel = NewRect(
                provider.ProviderId + " Categories",
                _categoryRoot);
            FillParent(categoryPanel);
            var context = new AvatarConfigurationPanelContext(
                categoryPanel,
                _contentRoot,
                _previewRoot,
                _canvas);
            GameObject panel = provider.CreatePanel(context);
            if (panel != null && panel.transform.parent != _contentRoot)
                panel.transform.SetParent(_contentRoot, false);

            _providerViews.Add(new ProviderView
            {
                Provider = provider,
                CategoryPanel = categoryPanel.gameObject,
                Panel = panel,
                ButtonImage = buttonImage,
                ButtonLabel = buttonLabel,
            });
        }

        private void SelectProvider(string providerId)
        {
            _selectedProviderId = providerId;
            ApplySelection();
        }

        private void ApplySelection()
        {
            foreach (ProviderView view in _providerViews)
            {
                bool selected = string.Equals(
                    view.Provider.ProviderId,
                    _selectedProviderId,
                    StringComparison.Ordinal);
                if (view.Panel != null)
                    view.Panel.SetActive(selected);
                if (view.CategoryPanel != null)
                    view.CategoryPanel.SetActive(selected);
                view.Provider.SetSelected(selected);
                if (view.ButtonImage != null)
                    view.ButtonImage.color = selected
                        ? AvatarConfigurationTheme.Accent
                        : AvatarConfigurationTheme.SurfaceRaised;
                if (view.ButtonLabel != null)
                    view.ButtonLabel.color = selected
                        ? AvatarConfigurationTheme.TextPrimary
                        : AvatarConfigurationTheme.TextSecondary;
                if (selected && _previewCaption != null)
                    _previewCaption.text =
                        view.Provider.DisplayName.ToUpperInvariant() + " · LIVE PREVIEW";
            }
        }

        private void DestroyGeneratedRoot()
        {
            _providerViews.Clear();
            if (_generatedRoot == null)
                return;

            if (Application.isPlaying)
                Destroy(_generatedRoot.gameObject);
            else
                DestroyImmediate(_generatedRoot.gameObject);
            _generatedRoot = null;
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

        private static void BuildBackingFrame(RectTransform parent)
        {
            AddBackingRegion(parent, "Header Backing", new Vector2(0f, 0.72f), Vector2.one);
            AddBackingRegion(parent, "Footer Backing", Vector2.zero, new Vector2(1f, 0.055f));
            AddBackingRegion(
                parent,
                "Left Backing",
                new Vector2(0f, 0.055f),
                new Vector2(0.035f, 0.72f));
            AddBackingRegion(
                parent,
                "Category Preview Gutter Backing",
                new Vector2(0.125f, 0.055f),
                new Vector2(0.145f, 0.72f));
            AddBackingRegion(
                parent,
                "Preview Controls Gutter Backing",
                new Vector2(0.54f, 0.055f),
                new Vector2(0.56f, 0.72f));
            AddBackingRegion(
                parent,
                "Right Backing",
                new Vector2(0.965f, 0.055f),
                new Vector2(1f, 0.72f));
        }

        private static void AddBackingRegion(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform region = NewRect(name, parent);
            region.anchorMin = anchorMin;
            region.anchorMax = anchorMax;
            region.offsetMin = Vector2.zero;
            region.offsetMax = Vector2.zero;
            region.gameObject.AddComponent<Image>().color = AvatarConfigurationTheme.Canvas;
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            Color? color = null)
        {
            RectTransform rectTransform = NewRect("Label", parent);
            FillParent(rectTransform);
            var label = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color ?? AvatarConfigurationTheme.TextPrimary;
            label.raycastTarget = false;
            return label;
        }

        private static void AddBorder(RectTransform root)
        {
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = AvatarConfigurationTheme.Border;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
        }
    }
}
