using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace GHA.AvatarFramework
{
    /// <summary>
    /// Provider-neutral first-person camera policy. Ensures the local camera does not render
    /// the layer used by an avatar provider for locally hidden head/body geometry.
    /// </summary>
    public sealed class AvatarCameraVisibilityController
    {
        private CameraDebugSnapshot _lastSnapshot;
        private bool _hasLastSnapshot;

        public void EnsureHiddenLayerCulled(Camera localCamera, int hiddenLayer, string context)
        {
            if (localCamera == null)
                return;

            int layerMask = 1 << hiddenLayer;
            bool wasRenderingHiddenLayer = (localCamera.cullingMask & layerMask) != 0;
            if (wasRenderingHiddenLayer)
                localCamera.cullingMask &= ~layerMask;

            XROrigin xrOrigin = localCamera.GetComponentInParent<XROrigin>();
            Transform cameraOffset = xrOrigin != null && xrOrigin.CameraFloorOffsetObject != null
                ? xrOrigin.CameraFloorOffsetObject.transform
                : localCamera.transform.parent;
            float cameraOffsetY = cameraOffset != null ? cameraOffset.localPosition.y : float.NaN;
            XROrigin.TrackingOriginMode requestedOrigin = xrOrigin != null
                ? xrOrigin.RequestedTrackingOriginMode
                : XROrigin.TrackingOriginMode.NotSpecified;
            TrackingOriginModeFlags currentOrigin = xrOrigin != null
                ? xrOrigin.CurrentTrackingOriginMode
                : TrackingOriginModeFlags.Unknown;
            float cameraYOffset = xrOrigin != null ? xrOrigin.CameraYOffset : float.NaN;
            float cameraInOriginHeight = xrOrigin != null ? xrOrigin.CameraInOriginSpaceHeight : float.NaN;

            var snapshot = new CameraDebugSnapshot(
                localCamera,
                hiddenLayer,
                wasRenderingHiddenLayer,
                (localCamera.cullingMask & layerMask) != 0,
                localCamera.cullingMask,
                localCamera.nearClipPlane,
                requestedOrigin,
                currentOrigin,
                cameraYOffset,
                cameraOffsetY);
            if (_hasLastSnapshot && snapshot.Equals(_lastSnapshot))
                return;

            _hasLastSnapshot = true;
            _lastSnapshot = snapshot;

            InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            Vector3 headDevicePosition = default;
            bool hasHeadDevicePosition = headDevice.isValid
                && headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out headDevicePosition);
            Debug.LogWarning(
                $"[GHA VR CAM] {context} camera='{localCamera.name}' cullLayer={hiddenLayer} " +
                $"wasRenderingHiddenLayer={wasRenderingHiddenLayer} nowRenderingHiddenLayer={(localCamera.cullingMask & layerMask) != 0} " +
                $"cullingMask=0x{localCamera.cullingMask:X8} nearClip={localCamera.nearClipPlane:F3} " +
                $"originRequested={requestedOrigin} originCurrent={currentOrigin} cameraYOffset={cameraYOffset:F3} " +
                $"cameraOffsetY={cameraOffsetY:F3} cameraInOriginHeight={cameraInOriginHeight:F3} " +
                $"headDeviceY={(hasHeadDevicePosition ? headDevicePosition.y : float.NaN):F3}");
        }

        private readonly struct CameraDebugSnapshot
        {
            private readonly int _cameraId;
            private readonly int _cullLayer;
            private readonly bool _wasRenderingHiddenLayer;
            private readonly bool _nowRenderingHiddenLayer;
            private readonly int _cullingMask;
            private readonly float _nearClipPlane;
            private readonly XROrigin.TrackingOriginMode _requestedOrigin;
            private readonly TrackingOriginModeFlags _currentOrigin;
            private readonly float _cameraYOffset;
            private readonly float _cameraOffsetY;

            public CameraDebugSnapshot(
                Camera camera,
                int cullLayer,
                bool wasRenderingHiddenLayer,
                bool nowRenderingHiddenLayer,
                int cullingMask,
                float nearClipPlane,
                XROrigin.TrackingOriginMode requestedOrigin,
                TrackingOriginModeFlags currentOrigin,
                float cameraYOffset,
                float cameraOffsetY)
            {
                _cameraId = camera != null ? camera.GetInstanceID() : 0;
                _cullLayer = cullLayer;
                _wasRenderingHiddenLayer = wasRenderingHiddenLayer;
                _nowRenderingHiddenLayer = nowRenderingHiddenLayer;
                _cullingMask = cullingMask;
                _nearClipPlane = nearClipPlane;
                _requestedOrigin = requestedOrigin;
                _currentOrigin = currentOrigin;
                _cameraYOffset = cameraYOffset;
                _cameraOffsetY = cameraOffsetY;
            }

            public bool Equals(CameraDebugSnapshot other)
            {
                return _cameraId == other._cameraId
                    && _cullLayer == other._cullLayer
                    && _wasRenderingHiddenLayer == other._wasRenderingHiddenLayer
                    && _nowRenderingHiddenLayer == other._nowRenderingHiddenLayer
                    && _cullingMask == other._cullingMask
                    && SameFloat(_nearClipPlane, other._nearClipPlane)
                    && _requestedOrigin == other._requestedOrigin
                    && _currentOrigin == other._currentOrigin
                    && SameFloat(_cameraYOffset, other._cameraYOffset)
                    && SameFloat(_cameraOffsetY, other._cameraOffsetY);
            }

            private static bool SameFloat(float left, float right)
            {
                return (float.IsNaN(left) && float.IsNaN(right))
                    || Mathf.Abs(left - right) <= 0.001f;
            }
        }
    }
}
