using UnityEngine;

namespace GHA.AvatarFramework
{
    /// <summary>Provider-neutral one-time standing eye-height calibration.</summary>
    public sealed class VrHeightCalibration
    {
        private float _candidateSince = -1f;
        private float _candidateEyeHeight;

        public bool IsCalibrated { get; private set; }

        public void Reset()
        {
            IsCalibrated = false;
            _candidateSince = -1f;
            _candidateEyeHeight = 0f;
        }

        public bool TryCalculateScale(
            float now,
            float playerEyeHeight,
            float avatarEyeHeightAtCurrentScale,
            float currentScale,
            float minimumStandingEyeHeight,
            float holdSeconds,
            float maximumEyeHeightDrift,
            float minimumScale,
            float maximumScale,
            out float targetScale)
        {
            targetScale = currentScale;
            if (IsCalibrated)
                return false;

            if (playerEyeHeight < Mathf.Max(0.1f, minimumStandingEyeHeight))
            {
                _candidateSince = -1f;
                _candidateEyeHeight = 0f;
                return false;
            }

            if (_candidateSince < 0f)
            {
                _candidateSince = now;
                _candidateEyeHeight = playerEyeHeight;
                return false;
            }

            if (Mathf.Abs(playerEyeHeight - _candidateEyeHeight) > Mathf.Max(0.001f, maximumEyeHeightDrift))
            {
                _candidateSince = now;
                _candidateEyeHeight = playerEyeHeight;
                return false;
            }

            if (now - _candidateSince < Mathf.Max(0f, holdSeconds))
                return false;

            float safeCurrentScale = Mathf.Max(0.0001f, currentScale);
            float scaleOneEyeHeight = avatarEyeHeightAtCurrentScale / safeCurrentScale;
            if (playerEyeHeight <= 0.1f || scaleOneEyeHeight <= 0.1f)
                return false;

            targetScale = Mathf.Clamp(
                playerEyeHeight / scaleOneEyeHeight,
                Mathf.Max(0.01f, minimumScale),
                Mathf.Max(minimumScale, maximumScale));
            IsCalibrated = true;
            _candidateSince = -1f;
            _candidateEyeHeight = 0f;
            return true;
        }
    }

}
