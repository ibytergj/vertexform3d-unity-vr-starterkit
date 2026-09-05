#if VERTEXFORM_GHA_HOST
using GHA.AvatarFramework;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using VertexFormCore;

namespace GHA.AvatarSuite
{
    /// <summary>Translates the VertexForm player rig into the shared humanoid rig contract.</summary>
    public sealed class VertexFormHumanoidRigInput : IHumanoidRigInput
    {
        private readonly AvatarInputConverter _converter;
        private readonly PlayerNetworkSetup _playerSetup;

        public VertexFormHumanoidRigInput(AvatarInputConverter converter, PlayerNetworkSetup playerSetup)
        {
            _converter = converter;
            _playerSetup = playerSetup;
        }

        public Transform MainAvatar => _converter != null ? _converter.MainAvatarTransform : null;
        public Transform Body => _converter != null ? _converter.AvatarBody : null;
        public Transform HeadTarget => _converter != null ? _converter.AvatarHead : null;
        public Transform TrackedHead => _converter != null && _converter.XRHead != null
            ? _converter.XRHead
            : HeadTarget;
        public Transform LeftHand => _converter != null ? _converter.AvatarHand_Left : null;
        public Transform RightHand => _converter != null ? _converter.AvatarHand_Right : null;

        public bool IsVrStyle
        {
            get
            {
                if (_playerSetup != null)
                    return _playerSetup.NetworkedIsVrStyle();
                return ProjectManager.instance != null
                    && ProjectManager.instance.platforms != null
                    && ProjectManager.instance.platforms.IsVrStylePlatform();
            }
        }

        public bool IsLocalAuthority => _playerSetup == null
            || _playerSetup.Object == null
            || _playerSetup.Object.HasInputAuthority;

        public bool IsHeadTracked
        {
            get
            {
                if (!IsLocalAuthority)
                    return IsVrStyle && HeadTarget != null;
                InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (!head.isValid
                    || !head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked)
                    || !tracked)
                    return false;

                // Some runtimes keep isTracked and userPresence true while the headset is
                // unmounted. An active OpenXR session leaves the Focused state in that case,
                // which is the authoritative signal that live pose input no longer owns the rig.
                if (head.TryGetFeatureValue(CommonUsages.userPresence, out bool userPresent)
                    && !userPresent)
                    return false;

                XRManagerSettings manager = XRGeneralSettings.Instance != null
                    ? XRGeneralSettings.Instance.Manager
                    : null;
                if (manager != null
                    && manager.activeLoader is OpenXRLoaderBase
                    && !OpenXRUtility.IsSessionFocused)
                    return false;

                return true;
            }
        }

        public bool IsHandTracking => _playerSetup != null && _playerSetup.isHandTracking;

        public bool TryGetTrackingSpaceEyeHeight(out float eyeHeight)
        {
            eyeHeight = 0f;
            if (!IsLocalAuthority)
                return false;

            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!head.isValid
                || !head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 devicePosition)
                || !float.IsFinite(devicePosition.y)
                || devicePosition.y <= 0f)
                return false;

            eyeHeight = devicePosition.y;
            return true;
        }

        public bool TryApplySeatedViewCorrection(Vector3 worldCorrection)
        {
            return IsLocalAuthority
                && _converter != null
                && _converter.TryApplySeatedViewCorrection(worldCorrection);
        }

    }
}
#endif
