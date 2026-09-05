#if VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using System.Collections.Generic;
using UMA.CharacterSystem;
using UnityEngine;
using VertexFormCore;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// Local-only fix for remote-avatar camera clipping: when the LOCAL camera comes within
    /// <see cref="HideRadius"/> of another player's avatar geometry — whoever closed the gap,
    /// you into them or them into you — that remote avatar is hidden on this client so you
    /// never see inside it. Their floating name tag stays visible (it lives under the camera,
    /// not the avatar). Each client only ever hides OTHER players; your own avatar is handled
    /// by the first-person path. Purely client-side — no networking, works for both stock and
    /// UMA avatars.
    ///
    /// TODO (nice-to-have): fade the remote avatar out/in instead of an instant hide.
    ///
    /// Self-bootstrapped as a persistent singleton so it needs no core/prefab changes.
    /// </summary>
    public class RemoteAvatarProximityHider : MonoBehaviour
    {
        /// <summary>Hide a remote avatar once the local camera is within this distance of its geometry.</summary>
        public static float HideRadius = 0.4f;
        /// <summary>Extra distance the camera must travel back out before the avatar reappears (anti-flicker).</summary>
        public static float ShowBuffer = 0.15f;
        /// <summary>Flip on to log throttled status + hide/show transitions for debugging.</summary>
        public static bool VerboseLog = false;

        private readonly Dictionary<PlayerNetworkSetup, bool> _hidden = new Dictionary<PlayerNetworkSetup, bool>();
        private float _nextLogTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("GHA_RemoteAvatarProximityHider");
            DontDestroyOnLoad(go);
            go.AddComponent<RemoteAvatarProximityHider>();
        }

        private void LateUpdate()
        {
            bool log = VerboseLog && Time.unscaledTime >= _nextLogTime;
            if (log) _nextLogTime = Time.unscaledTime + 1f;

            RoomManager rm = RoomManager.Instance;
            if (rm == null) return;

            PlayerNetworkSetup local = rm.GetLocalPlayerSetup();
            if (local == null || local.cam == null) return;
            Vector3 camPos = local.cam.transform.position;

            float hideSqr = HideRadius * HideRadius;
            float showSqr = (HideRadius + ShowBuffer) * (HideRadius + ShowBuffer);

            List<PlayerNetworkSetup> players = rm.allPlayers;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerNetworkSetup p = players[i];
                if (p == null || p == local) continue;
                // Only remote players (their own client owns input authority).
                if (p.Object == null || p.Object.HasInputAuthority) continue;

                // Distance to the actual avatar geometry (world bounds of its renderers), so it
                // works regardless of where the player root sits. SqrDistance is 0 when the
                // camera is inside any of their geometry.
                if (!TryGetAvatarBounds(p, out Bounds b)) continue;
                float sqr = b.SqrDistance(camPos);

                bool wasHidden = _hidden.TryGetValue(p, out bool h) && h;
                bool hide = sqr < (wasHidden ? showSqr : hideSqr);
                if (log)
                    Debug.Log($"[ProximityHider] '{p.name}' dist={Mathf.Sqrt(sqr):F2} (hideR={HideRadius:F2}) hidden={hide}");
                if (hide != wasHidden)
                {
                    SetAvatarVisible(p, !hide);
                    _hidden[p] = hide;
                }
            }
        }

        /// <summary>
        /// Toggles a player's avatar geometry. Targets the geometry roots specifically — the
        /// stock avatar under AvatarHolder and the UMA avatar under its DynamicCharacterAvatar
        /// — so the name tag (parented to the camera) is never touched.
        /// </summary>
        private static void SetAvatarVisible(PlayerNetworkSetup p, bool visible)
        {
            if (p.avatarHolder != null)
                ToggleRenderers(p.avatarHolder.transform, visible);

            DynamicCharacterAvatar dca = p.GetComponentInChildren<DynamicCharacterAvatar>(true);
            if (dca != null)
                ToggleRenderers(dca.transform, visible);
        }

        private static void ToggleRenderers(Transform root, bool visible)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = visible;
        }

        /// <summary>World-space bounds of a remote player's avatar geometry (stock + UMA), or false if none yet.</summary>
        private static bool TryGetAvatarBounds(PlayerNetworkSetup p, out Bounds bounds)
        {
            bounds = default;
            bool has = false;
            if (p.avatarHolder != null)
                AppendBounds(p.avatarHolder.transform, ref bounds, ref has);
            DynamicCharacterAvatar dca = p.GetComponentInChildren<DynamicCharacterAvatar>(true);
            if (dca != null)
                AppendBounds(dca.transform, ref bounds, ref has);
            return has;
        }

        private static void AppendBounds(Transform root, ref Bounds bounds, ref bool has)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }
        }
    }
}
#endif
