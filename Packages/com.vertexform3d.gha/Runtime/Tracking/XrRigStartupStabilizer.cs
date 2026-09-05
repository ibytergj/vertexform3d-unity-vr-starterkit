using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace GHA.AvatarFramework
{
    /// <summary>
    /// One-shot readiness gate for a local OpenXR rig. It prevents XRI locomotion from
    /// consuming a transitional headset pose while the runtime establishes Floor tracking.
    /// The gate becomes inactive after a stable floor-relative pose is accepted and only
    /// re-arms when the XR runtime reports a tracking-origin change.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XROrigin))]
    public sealed class XrRigStartupStabilizer : MonoBehaviour
    {
        private const float DefaultMinimumCapsuleHeight = 0.5f;

        [Header("Floor pose readiness")]
        [SerializeField, Min(0.1f)] private float minimumFloorEyeHeight = 1.0f;
        [SerializeField, Min(0.1f)] private float maximumFloorEyeHeight = 2.25f;
        [SerializeField, Min(0f)] private float stableHoldSeconds = 0.5f;
        [SerializeField, Min(0.001f)] private float stableHeightTolerance = 0.02f;
        [SerializeField, Min(0.01f)] private float floorPoseAgreementTolerance = 0.15f;
        [SerializeField, Min(0.1f)] private float originRequestRetrySeconds = 1.0f;

        private readonly List<XRInputSubsystem> _inputSubsystems = new List<XRInputSubsystem>();

        private XROrigin _xrOrigin;
        private CharacterController _characterController;
        private XRBodyTransformer _bodyTransformer;
        private GravityProvider _gravityProvider;
        private XRInputSubsystem _inputSubsystem;

        private bool _activatedForLocalRig;
        private bool _waitingForReadyPose;
        private bool _bodyTransformerWasEnabled;
        private bool _gravityProviderWasEnabled;
        private bool _locomotionSuspended;
        private float _nextOriginRequestTime;
        private float _stableSince = -1f;
        private float _lastStableHeight;
        private string _lastWaitingReason;

        public bool IsReady { get; private set; }

        /// <summary>
        /// Activates the gate for the one local XR rig. Remote network player instances
        /// must not call this method.
        /// </summary>
        public void ActivateForLocalRig()
        {
            if (_activatedForLocalRig)
                return;

            _activatedForLocalRig = true;
            ResolveRigComponents();
            BeginReadinessGate("local-rig-activation");
        }

        private void OnDisable()
        {
            UnsubscribeInputSubsystem();
            RestoreLocomotion();
        }

        private void OnDestroy()
        {
            UnsubscribeInputSubsystem();
            RestoreLocomotion();
        }

        private void Update()
        {
            if (!_activatedForLocalRig || !_waitingForReadyPose)
                return;

            if (!TryResolveRunningInputSubsystem())
            {
                ReportWaiting("no-running-xr-input-subsystem");
                return;
            }

            if (!IsOpenXrSessionFocused())
            {
                ResetStableWindow();
                ReportWaiting("openxr-session-not-focused");
                return;
            }

            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!TryGetPresentTrackedHead(head, out Vector3 devicePosition))
            {
                ResetStableWindow();
                ReportWaiting("headset-not-present-and-position-tracked");
                return;
            }

            RequestFloorOriginIfNeeded();
            TrackingOriginModeFlags currentOrigin = _inputSubsystem.GetTrackingOriginMode();
            if ((currentOrigin & TrackingOriginModeFlags.Floor) == 0)
            {
                ResetStableWindow();
                ReportWaiting($"tracking-origin-{currentOrigin}-not-floor");
                return;
            }

            float cameraHeight = _xrOrigin != null
                ? _xrOrigin.CameraInOriginSpaceHeight
                : float.NaN;
            if (!IsPlausibleFloorHeight(devicePosition.y)
                || !IsPlausibleFloorHeight(cameraHeight)
                || Mathf.Abs(devicePosition.y - cameraHeight) > floorPoseAgreementTolerance)
            {
                ResetStableWindow();
                ReportWaiting(
                    $"floor-pose-not-ready deviceY={devicePosition.y:F3} cameraY={cameraHeight:F3}");
                return;
            }

            if (_stableSince < 0f
                || Mathf.Abs(cameraHeight - _lastStableHeight) > stableHeightTolerance)
            {
                _stableSince = Time.unscaledTime;
                _lastStableHeight = cameraHeight;
                ReportWaiting($"stabilizing-floor-pose height={cameraHeight:F3}");
                return;
            }

            _lastStableHeight = cameraHeight;
            if (Time.unscaledTime - _stableSince < stableHoldSeconds)
                return;

            CompleteReadinessGate(devicePosition.y, cameraHeight, currentOrigin);
        }

        private void ResolveRigComponents()
        {
            _xrOrigin = GetComponent<XROrigin>();
            _characterController = GetComponent<CharacterController>();
            _bodyTransformer = GetComponentInChildren<XRBodyTransformer>(true);
            _gravityProvider = GetComponentInChildren<GravityProvider>(true);
        }

        private void BeginReadinessGate(string reason)
        {
            IsReady = false;
            _waitingForReadyPose = true;
            _nextOriginRequestTime = 0f;
            ResetStableWindow();
            SuspendLocomotion();
            RepairInvalidCapsule();

            if (_xrOrigin != null
                && _xrOrigin.RequestedTrackingOriginMode != XROrigin.TrackingOriginMode.Floor)
            {
                _xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            }

            Debug.Log(
                $"[GHA XR READY] '{name}' gate armed reason={reason}; " +
                "waiting for a present, tracked, stable Floor-relative headset pose.");
        }

        private bool TryResolveRunningInputSubsystem()
        {
            if (_inputSubsystem != null && _inputSubsystem.running)
                return true;

            UnsubscribeInputSubsystem();
            _inputSubsystems.Clear();
            SubsystemManager.GetSubsystems(_inputSubsystems);
            for (int i = 0; i < _inputSubsystems.Count; i++)
            {
                XRInputSubsystem candidate = _inputSubsystems[i];
                if (candidate == null || !candidate.running)
                    continue;

                _inputSubsystem = candidate;
                _inputSubsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
                Debug.Log($"[GHA XR READY] '{name}' subscribed to XR tracking-origin changes.");
                return true;
            }

            return false;
        }

        private void UnsubscribeInputSubsystem()
        {
            if (_inputSubsystem != null)
                _inputSubsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
            _inputSubsystem = null;
        }

        private void OnTrackingOriginUpdated(XRInputSubsystem subsystem)
        {
            if (!_activatedForLocalRig || subsystem == null || subsystem != _inputSubsystem)
                return;

            TrackingOriginModeFlags origin = subsystem.GetTrackingOriginMode();
            Debug.Log(
                $"[GHA XR READY] '{name}' tracking origin updated to {origin}; " +
                "revalidating the floor-relative pose once.");

            if (!_waitingForReadyPose)
                BeginReadinessGate("tracking-origin-updated");
            else
                ResetStableWindow();
        }

        private void RequestFloorOriginIfNeeded()
        {
            if (_inputSubsystem == null || Time.unscaledTime < _nextOriginRequestTime)
                return;

            _nextOriginRequestTime = Time.unscaledTime + originRequestRetrySeconds;
            bool accepted = _inputSubsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            TrackingOriginModeFlags current = _inputSubsystem.GetTrackingOriginMode();
            Debug.Log(
                $"[GHA XR READY] '{name}' requested Floor tracking origin; " +
                $"accepted={accepted} current={current}.");
        }

        private static bool TryGetPresentTrackedHead(InputDevice head, out Vector3 devicePosition)
        {
            devicePosition = default;
            if (!head.isValid
                || !head.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked)
                || !isTracked)
            {
                return false;
            }

            if (head.TryGetFeatureValue(CommonUsages.userPresence, out bool userPresent)
                && !userPresent)
            {
                return false;
            }

            if (head.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState trackingState)
                && (trackingState & InputTrackingState.Position) == 0)
            {
                return false;
            }

            return head.TryGetFeatureValue(CommonUsages.devicePosition, out devicePosition)
                && IsFinite(devicePosition);
        }

        private static bool IsOpenXrSessionFocused()
        {
            XRManagerSettings manager = XRGeneralSettings.Instance != null
                ? XRGeneralSettings.Instance.Manager
                : null;
            return manager == null
                || manager.activeLoader is not OpenXRLoaderBase
                || OpenXRUtility.IsSessionFocused;
        }

        private bool IsPlausibleFloorHeight(float height)
        {
            return float.IsFinite(height)
                && height >= minimumFloorEyeHeight
                && height <= maximumFloorEyeHeight;
        }

        private void SuspendLocomotion()
        {
            if (_locomotionSuspended)
                return;

            if (_bodyTransformer != null)
            {
                _bodyTransformerWasEnabled = _bodyTransformer.enabled;
                _bodyTransformer.enabled = false;
            }

            if (_gravityProvider != null)
            {
                _gravityProviderWasEnabled = _gravityProvider.enabled;
                _gravityProvider.enabled = false;
            }

            _locomotionSuspended = true;
        }

        private void RestoreLocomotion()
        {
            if (!_locomotionSuspended)
                return;

            if (_bodyTransformer != null)
                _bodyTransformer.enabled = _bodyTransformerWasEnabled;
            if (_gravityProvider != null)
                _gravityProvider.enabled = _gravityProviderWasEnabled;

            _locomotionSuspended = false;
        }

        private void RepairInvalidCapsule()
        {
            if (_characterController == null)
                return;

            float minimumHeight = Mathf.Max(DefaultMinimumCapsuleHeight, _characterController.radius * 2f);
            if (float.IsFinite(_characterController.height)
                && _characterController.height >= minimumHeight)
            {
                return;
            }

            Vector3 center = _characterController.center;
            center.y = minimumHeight * 0.5f + _characterController.skinWidth;
            _characterController.height = minimumHeight;
            _characterController.center = center;
            Debug.LogWarning(
                $"[GHA XR READY] '{name}' repaired invalid CharacterController height " +
                $"to {minimumHeight:F3} while Floor tracking initializes.");
        }

        private void CompleteReadinessGate(
            float deviceHeight,
            float cameraHeight,
            TrackingOriginModeFlags currentOrigin)
        {
            if (_characterController != null)
            {
                float capsuleHeight = Mathf.Max(
                    cameraHeight,
                    Mathf.Max(DefaultMinimumCapsuleHeight, _characterController.radius * 2f));
                Vector3 cameraLocal = _xrOrigin.CameraInOriginSpacePos;
                _characterController.height = capsuleHeight;
                _characterController.center = new Vector3(
                    cameraLocal.x,
                    capsuleHeight * 0.5f + _characterController.skinWidth,
                    cameraLocal.z);
            }

            _waitingForReadyPose = false;
            IsReady = true;
            _lastWaitingReason = null;
            RestoreLocomotion();

            Debug.Log(
                $"[GHA XR READY] '{name}' READY origin={currentOrigin} " +
                $"deviceY={deviceHeight:F3} cameraY={cameraHeight:F3} " +
                $"ccHeight={(_characterController != null ? _characterController.height : float.NaN):F3}; " +
                "locomotion released.");
        }

        private void ResetStableWindow()
        {
            _stableSince = -1f;
            _lastStableHeight = 0f;
        }

        private void ReportWaiting(string reason)
        {
            if (_lastWaitingReason == reason)
                return;
            _lastWaitingReason = reason;
            Debug.Log($"[GHA XR READY] '{name}' waiting: {reason}.");
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x)
                && float.IsFinite(value.y)
                && float.IsFinite(value.z);
        }
    }
}
