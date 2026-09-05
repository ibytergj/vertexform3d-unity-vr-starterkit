using System.Collections.Generic;
using UnityEngine;

namespace GHA.AvatarFramework
{
    /// <summary>Aligns a humanoid root to semantic XR rig targets while preserving floor contact.</summary>
    public sealed class HumanoidVrAlignment
    {
        private readonly List<TimedFloorSample> _floorSamples = new List<TimedFloorSample>();
        private bool _scaleApplied;
        private bool _floorReady;
        private float _floorY;

        public bool IsCalibrated => _scaleApplied;
        public bool IsReady => _scaleApplied && _floorReady;
        public bool IsFloorReady => _floorReady;
        public float FloorY => _floorY;
        public float SessionEyeHeight => VrCalibrationSession.HasStandingEyeHeight ? VrCalibrationSession.StandingEyeHeight : 0f;

        public void Reset()
        {
            _scaleApplied = false;
            ResetSceneFloor();
        }

        public void ResetSceneFloor()
        {
            _floorSamples.Clear();
            _floorReady = false;
            _floorY = 0f;
        }

        public bool Align(
            Transform avatarRoot,
            Transform headBone,
            Transform leftEyeBone,
            Transform rightEyeBone,
            IHumanoidRigInput input,
            float groundY,
            Vector3 rootOffset,
            Vector3 trackedHeadMatchOffset,
            float headBoneToEyeForwardOffset,
            float headBoneToEyeUpOffset,
            bool autoScale,
            float standingMinEyeHeight,
            float standingHoldSeconds,
            float standingMaxEyeHeightDrift,
            float minScale,
            float maxScale,
            float time,
            out float calibratedEyeHeight,
            out float calibratedScale)
        {
            calibratedEyeHeight = 0f;
            calibratedScale = avatarRoot != null ? avatarRoot.localScale.y : 1f;
            if (avatarRoot == null || input == null || !input.IsVrStyle)
                return false;

            Transform bodyAnchor = input.Body != null ? input.Body : input.MainAvatar;
            if (bodyAnchor == null)
                return false;

            Transform trackedHead = input.TrackedHead;
            avatarRoot.rotation = Quaternion.Euler(0f, bodyAnchor.rotation.eulerAngles.y, 0f);
            if (!autoScale)
                _scaleApplied = true;

            // CharacterController bottoms are host-specific in VertexForm: the Home rig and
            // spawned network rig can place their roots at different vertical offsets. For a
            // tracked local player, convert the device's floor-relative eye height into the
            // corresponding world-space floor. This keeps grounding and eye alignment in the
            // same coordinate space without using the mutable world height for body scaling.
            float alignmentGroundY = _floorReady ? _floorY : groundY;
            if (input.IsLocalAuthority && input.IsHeadTracked && trackedHead != null)
            {
                bool hasDeviceEyeHeight = input.TryGetTrackingSpaceEyeHeight(out float trackingSpaceEyeHeight);
                bool deviceHeightIsFloorRelative = hasDeviceEyeHeight
                    && trackingSpaceEyeHeight >= standingMinEyeHeight
                    && trackingSpaceEyeHeight <= 2.2f;
                float worldEyeHeight = trackedHead.position.y - groundY;
                bool hostFallbackIsPlausible = !deviceHeightIsFloorRelative
                    && float.IsFinite(worldEyeHeight)
                    && worldEyeHeight >= standingMinEyeHeight
                    && worldEyeHeight <= 2.0f;

                if (deviceHeightIsFloorRelative || hostFallbackIsPlausible)
                {
                    float observedEyeHeight = deviceHeightIsFloorRelative
                        ? trackingSpaceEyeHeight
                        : worldEyeHeight;
                    VrCalibrationSession.ObserveStandingEyeHeight(
                        time,
                        observedEyeHeight,
                        standingMinEyeHeight,
                        Mathf.Max(2.5f, standingHoldSeconds),
                        Mathf.Max(0.06f, standingMaxEyeHeightDrift * 3f));
                }
                // Floor readiness is independent of standing-height plausibility. Home can
                // report device-relative HMD Y near zero while still supplying a stable,
                // authored host floor. Waiting for a standing-height sample in that case
                // leaves the local first-person body hidden forever.
                float observedFloorY = deviceHeightIsFloorRelative
                    ? trackedHead.position.y - trackingSpaceEyeHeight
                    : groundY;
                ObserveSceneFloor(time, observedFloorY, groundY);
                if (_floorReady)
                    alignmentGroundY = _floorY;
            }

            bool scaleApplied = TryApplyScale(
                avatarRoot,
                headBone,
                leftEyeBone,
                rightEyeBone,
                trackedHead,
                input,
                alignmentGroundY,
                autoScale,
                standingMinEyeHeight,
                standingHoldSeconds,
                standingMaxEyeHeightDrift,
                minScale,
                maxScale,
                time,
                out calibratedEyeHeight,
                out calibratedScale);

            Vector3 horizontalOffset = bodyAnchor.TransformVector(new Vector3(rootOffset.x, 0f, rootOffset.z));
            avatarRoot.position = new Vector3(
                bodyAnchor.position.x + horizontalOffset.x,
                alignmentGroundY + rootOffset.y,
                bodyAnchor.position.z + horizontalOffset.z);

            if (trackedHead != null && headBone != null && input.IsHeadTracked)
            {
                Vector3 avatarEyePoint = ResolveAvatarEyePoint(
                    avatarRoot,
                    headBone,
                    leftEyeBone,
                    rightEyeBone,
                    headBoneToEyeForwardOffset,
                    headBoneToEyeUpOffset);
                Vector3 correction = trackedHead.position - avatarEyePoint + trackedHeadMatchOffset;
                correction.y = 0f;
                avatarRoot.position += correction;
            }

            return scaleApplied;
        }

        private bool TryApplyScale(
            Transform avatarRoot,
            Transform headBone,
            Transform leftEyeBone,
            Transform rightEyeBone,
            Transform trackedHead,
            IHumanoidRigInput input,
            float groundY,
            bool autoScale,
            float standingMinEyeHeight,
            float standingHoldSeconds,
            float standingMaxEyeHeightDrift,
            float minScale,
            float maxScale,
            float time,
            out float playerEyeHeight,
            out float targetScale)
        {
            playerEyeHeight = 0f;
            targetScale = avatarRoot.localScale.y;
            if (!autoScale || _scaleApplied || !input.IsLocalAuthority
                || !input.IsHeadTracked || trackedHead == null || headBone == null)
                return false;

            if (!VrCalibrationSession.HasStandingEyeHeight)
                return false;
            playerEyeHeight = VrCalibrationSession.StandingEyeHeight;
            float currentScale = Mathf.Max(0.0001f, avatarRoot.localScale.y);
            Vector3 avatarEyePoint = ResolveAvatarEyePoint(
                avatarRoot,
                headBone,
                leftEyeBone,
                rightEyeBone,
                0f,
                0f);
            float avatarEyeHeight = Mathf.Abs(avatarEyePoint.y - avatarRoot.position.y);
            float scaleOneEyeHeight = avatarEyeHeight / currentScale;
            if (scaleOneEyeHeight <= 0.1f)
                return false;

            targetScale = Mathf.Clamp(
                playerEyeHeight / scaleOneEyeHeight,
                Mathf.Max(0.01f, minScale),
                Mathf.Max(minScale, maxScale));

            if (!Mathf.Approximately(avatarRoot.localScale.x, targetScale))
                avatarRoot.localScale = Vector3.one * targetScale;
            _scaleApplied = true;
            return true;
        }

        public static Vector3 ResolveAvatarEyePoint(
            Transform avatarRoot,
            Transform headBone,
            Transform leftEyeBone,
            Transform rightEyeBone,
            float headBoneToEyeForwardOffset,
            float headBoneToEyeUpOffset)
        {
            if (leftEyeBone != null && rightEyeBone != null)
                return (leftEyeBone.position + rightEyeBone.position) * 0.5f;
            if (leftEyeBone != null)
                return leftEyeBone.position;
            if (rightEyeBone != null)
                return rightEyeBone.position;
            if (avatarRoot == null || headBone == null)
                return Vector3.zero;

            return headBone.position
                + avatarRoot.forward * headBoneToEyeForwardOffset
                + Vector3.up * headBoneToEyeUpOffset;
        }

        private void ObserveSceneFloor(float now, float floorY, float hostGroundY)
        {
            if (!float.IsFinite(floorY))
                return;

            if (_floorReady)
            {
                const float teleportFloorDelta = 0.25f;
                const float hostAgreementTolerance = 0.25f;
                bool hostFloorMoved = float.IsFinite(hostGroundY)
                    && Mathf.Abs(hostGroundY - _floorY) >= teleportFloorDelta;
                bool candidateMatchesHost = float.IsFinite(hostGroundY)
                    && Mathf.Abs(floorY - hostGroundY) <= hostAgreementTolerance;
                if (hostFloorMoved && candidateMatchesHost)
                {
                    // Teleports can move the whole XR host between authored floor levels.
                    // Rebase placement immediately while keeping scale/calibration locked and
                    // keeping readiness true, so the first-person body never disappears.
                    _floorY = floorY;
                    _floorSamples.Clear();
                }
                return;
            }

            _floorSamples.Add(new TimedFloorSample(now, floorY));
            float earliest = now - 1f;
            while (_floorSamples.Count > 120 || (_floorSamples.Count > 0 && _floorSamples[0].Time < earliest))
                _floorSamples.RemoveAt(0);
            if (_floorSamples.Count < 2 || now - _floorSamples[0].Time < 0.75f)
                return;

            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            var values = new List<float>(_floorSamples.Count);
            foreach (TimedFloorSample sample in _floorSamples)
            {
                minimum = Mathf.Min(minimum, sample.Value);
                maximum = Mathf.Max(maximum, sample.Value);
                values.Add(sample.Value);
            }
            if (maximum - minimum > 0.03f)
                return;

            values.Sort();
            _floorY = values[values.Count / 2];
            _floorReady = true;
            _floorSamples.Clear();
        }

        private readonly struct TimedFloorSample
        {
            public TimedFloorSample(float time, float value) { Time = time; Value = value; }
            public float Time { get; }
            public float Value { get; }
        }
    }
}

