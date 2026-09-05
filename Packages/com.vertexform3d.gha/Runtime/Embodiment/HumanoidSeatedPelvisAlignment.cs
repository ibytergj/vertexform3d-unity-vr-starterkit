using UnityEngine;

namespace GHA.AvatarFramework
{
    /// <summary>
    /// Applies a visual-root correction that places a humanoid's hips on an authored seat target.
    /// The correction is active only while a stable looping seated clip owns the lower body, so
    /// the sit-down and stand-up transition clips remain free to animate normally.
    /// </summary>
    public sealed class HumanoidSeatedPelvisAlignment
    {
        private Vector3 _appliedLocalOffset;
        private bool _hasAppliedOffset;
        private string _seatedLoopStateName;
        private int _seatedLoopStateHash;

        public bool IsAligned { get; private set; }

        /// <summary>Removes the previous frame's correction before normal root alignment runs.</summary>
        public void ClearPreviousOffset(Transform avatarRoot)
        {
            if (!_hasAppliedOffset || avatarRoot == null)
                return;

            avatarRoot.localPosition -= _appliedLocalOffset;
            _appliedLocalOffset = Vector3.zero;
            _hasAppliedOffset = false;
            IsAligned = false;
        }

        public bool TryAlign(
            Transform avatarRoot,
            Animator animator,
            Transform seatedPelvisTarget,
            HumanoidPosture posture,
            string seatedLoopStateName,
            out Vector3 worldCorrection)
        {
            worldCorrection = Vector3.zero;
            if (posture != HumanoidPosture.Sitting
                || avatarRoot == null
                || animator == null
                || !animator.isHuman
                || seatedPelvisTarget == null
                || animator.IsInTransition(0)
                || !IsSeatedLoopState(animator, seatedLoopStateName))
            {
                return false;
            }

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return false;

            // The authored target's positive Z axis is the final seated facing direction.
            // Rotate first, then correct position because rotating the visual root also moves
            // the animated hips around that root.
            avatarRoot.rotation = Quaternion.Euler(
                0f,
                seatedPelvisTarget.rotation.eulerAngles.y,
                0f);
            worldCorrection = seatedPelvisTarget.position - hips.position;
            _appliedLocalOffset = avatarRoot.parent != null
                ? avatarRoot.parent.InverseTransformVector(worldCorrection)
                : worldCorrection;
            avatarRoot.localPosition += _appliedLocalOffset;
            _hasAppliedOffset = true;
            IsAligned = true;
            return true;
        }

        public void Reset()
        {
            _appliedLocalOffset = Vector3.zero;
            _hasAppliedOffset = false;
            IsAligned = false;
        }

        private bool IsSeatedLoopState(Animator animator, string seatedLoopStateName)
        {
            if (string.IsNullOrWhiteSpace(seatedLoopStateName))
                return false;

            if (_seatedLoopStateName != seatedLoopStateName)
            {
                _seatedLoopStateName = seatedLoopStateName;
                _seatedLoopStateHash = Animator.StringToHash(seatedLoopStateName);
            }

            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == _seatedLoopStateHash;
        }
    }
}
