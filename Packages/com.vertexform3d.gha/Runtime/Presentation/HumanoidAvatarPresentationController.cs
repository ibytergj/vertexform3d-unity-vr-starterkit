using UnityEngine;

namespace GHA.AvatarFramework
{
    /// <summary>
    /// Applies provider-neutral first-person avatar visibility and local-camera culling.
    /// Scene/network hosts retain authority, presentation-style, and camera discovery policy.
    /// </summary>
    public sealed class HumanoidAvatarPresentationController
    {
        private readonly AvatarCameraVisibilityController _cameraVisibility =
            new AvatarCameraVisibilityController();

        public AvatarVisibility Apply(
            IHumanoidAvatarInstance avatar,
            bool firstPerson,
            bool headOnlyVisibilityAvailable,
            int hiddenLayer,
            Camera localCamera,
            string context)
        {
            AvatarVisibility visibility = ResolveVisibility(
                firstPerson,
                headOnlyVisibilityAvailable);

            avatar?.SetFirstPersonVisibility(visibility, hiddenLayer);
            if (firstPerson)
            {
                _cameraVisibility.EnsureHiddenLayerCulled(
                    localCamera,
                    hiddenLayer,
                    context);
            }

            return visibility;
        }

        public static AvatarVisibility ResolveVisibility(
            bool firstPerson,
            bool headOnlyVisibilityAvailable)
        {
            if (!firstPerson)
                return AvatarVisibility.Visible;

            return headOnlyVisibilityAvailable
                ? AvatarVisibility.HeadOnly
                : AvatarVisibility.WholeBody;
        }
    }
}
