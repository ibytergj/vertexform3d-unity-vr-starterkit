#if VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using GHA.AvatarFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// UMA contribution to the provider-neutral GHA avatar configuration panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UmaAvatarConfigurationProvider :
        AvatarConfigurationPanelProviderBehaviour
    {
        public const string PreviewAnchorName = "GHA UMA Preview";

        [SerializeField] private UmaAvatarCatalog catalog;
        [SerializeField] private RuntimeAnimatorController previewAnimationController;
        [SerializeField] private Slider sliderPrefab;

        private UmaAvatarCustomizer _customizer;

        public override string ProviderId => "gha.uma";
        public override string DisplayName => "Custom";
        public override byte Mode => (byte)AvatarSystemMode.Uma;
        public override int SortOrder => 100;
        public override bool IsAvailable => catalog != null && catalog.enableUmaAvatars;

        private void Awake()
        {
            // Same default the UMA hosts use, so the panel opens on the provider that is built.
            if (catalog != null)
                GHA.AvatarFramework.AvatarProviderSelection.DefaultMode = (byte)catalog.EffectiveDefaultSystem;
        }

        public override GameObject CreatePanel(AvatarConfigurationPanelContext context)
        {
            var root = new GameObject("UMA Avatar Configuration", typeof(RectTransform));
            var rectTransform = (RectTransform)root.transform;
            rectTransform.SetParent(context.ContentRoot, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _customizer = root.AddComponent<UmaAvatarCustomizer>();
            _customizer.ConfigureEmbedded(
                catalog,
                previewAnimationController,
                sliderPrefab,
                rectTransform,
                context.CategoryRoot,
                ResolvePreviewAnchor(),
                context.PreviewRoot);
            return root;
        }

        public override void SetSelected(bool selected)
        {
            if (_customizer != null)
                _customizer.SetProviderSelected(selected);
        }

        private static Transform ResolvePreviewAnchor()
        {
            VertexFormCore.AvatarSelectionManager manager =
                VertexFormCore.AvatarSelectionManager.Instance;
            if (manager == null)
            {
                manager = Object.FindFirstObjectByType<VertexFormCore.AvatarSelectionManager>(
                    FindObjectsInactive.Include);
                if (manager != null)
                    VertexFormCore.AvatarSelectionManager.Instance = manager;
            }
            return manager != null && manager.customAvatarSelectionUI != null
                ? manager.customAvatarSelectionUI.transform.Find(PreviewAnchorName)
                : null;
        }
    }
}
#endif
