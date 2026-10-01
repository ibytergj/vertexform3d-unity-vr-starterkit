#if VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using System.Collections;
using GHA.AvatarFramework;
using UnityEngine;
using VertexFormCore.GHAIntegration;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// UMA's optional Home build adapter. Serialized fields and public entry points
    /// remain compatible with existing prefabs; the VertexForm host owns orchestration.
    /// </summary>
    [DisallowMultipleComponent]
    public class UmaHomeAvatar : MonoBehaviour, IHomeAvatarProvider
    {
        [SerializeField] private UmaAvatarCatalog catalog;
        [SerializeField] private RuntimeAnimatorController animationController;

        // Compatibility settings for prefabs installed before the Home host extraction.
        [SerializeField, HideInInspector] private GameObject legacyAvatarRoot;
        [SerializeField, HideInInspector] private int firstPersonHiddenLayer = 7;
        [SerializeField, HideInInspector] private bool delayInitialBuildUntilRequested;

        private UmaAvatarPuppet _puppet;
        private VertexFormHomeAvatar _host;

        public byte Mode => (byte)AvatarSystemMode.Uma;
        public byte DefaultMode => (byte)(catalog != null ? catalog.EffectiveDefaultSystem : AvatarSystemMode.Uma);
        public IHumanoidAvatarInstance Instance => _puppet;

        public byte ResolveMode(byte requested)
        {
            // Only UMA/Classic policy belongs to this catalog. Keep peer IDs intact.
            return catalog != null && requested <= (byte)AvatarSystemMode.Uma
                ? (byte)catalog.ResolveMode((AvatarSystemMode)requested)
                : requested;
        }

        private void Awake()
        {
            _host = GetComponent<VertexFormHomeAvatar>();
            if (_host == null)
            {
                // Compatibility for existing installed prefabs. New host-only installs
                // contain their own host and require no UMA component.
                _host = gameObject.AddComponent<VertexFormHomeAvatar>();
                _host.Configure(legacyAvatarRoot, firstPersonHiddenLayer, delayInitialBuildUntilRequested);
            }
        }

        public void BuildSavedAvatar()
        {
            if (_puppet == null)
            {
                _puppet = gameObject.AddComponent<UmaAvatarPuppet>();
                _puppet.animationController = animationController;
                _puppet.LoadTimingHost = "home";
            }
            _puppet.Build(catalog, UmaRecipeStore.LoadOrDefault(catalog));
        }

        public void Teardown()
        {
            if (_puppet != null)
                _puppet.Teardown();
        }

        // Retain callers and serialized UI references while the host seam is adopted.
        public void Refresh() => _host.Refresh();
        public IEnumerator EnsureAvatarLoadedForCustomizer() => _host.EnsureAvatarLoadedForCustomizer();
    }
}
#endif
