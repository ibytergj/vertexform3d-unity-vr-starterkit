using UnityEngine;

namespace GHA.AvatarFramework
{
    /// <summary>Provider-neutral full-body posture owned by the humanoid animation layer.</summary>
    public enum HumanoidPosture : byte
    {
        Standing = 0,
        Sitting = 1,
    }

    /// <summary>
    /// Applies generic posture parameters to a Humanoid Animator without assuming a provider.
    /// Preferred controllers expose an int "Posture" parameter; a bool "IsSitting" parameter
    /// is also supported. Controllers without either parameter remain safely inert.
    /// </summary>
    public sealed class HumanoidPostureAnimator
    {
        private static readonly int PostureParam = Animator.StringToHash("Posture");
        private static readonly int IsSittingParam = Animator.StringToHash("IsSitting");
        private static readonly int SpeedParam = Animator.StringToHash("Speed");

        private Animator _animator;
        private bool _hasPosture;
        private bool _hasIsSitting;
        private bool _hasSpeed;

        public HumanoidPosture Current { get; private set; } = HumanoidPosture.Standing;
        public int AnimatorPostureValue => _animator != null && _hasPosture
            ? _animator.GetInteger(PostureParam)
            : -1;

        public void Bind(Animator animator)
        {
            _animator = animator;
            _hasPosture = false;
            _hasIsSitting = false;
            _hasSpeed = false;

            if (_animator == null)
                return;

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == PostureParam && parameter.type == AnimatorControllerParameterType.Int)
                    _hasPosture = true;
                else if (parameter.nameHash == IsSittingParam && parameter.type == AnimatorControllerParameterType.Bool)
                    _hasIsSitting = true;
                else if (parameter.nameHash == SpeedParam && parameter.type == AnimatorControllerParameterType.Float)
                    _hasSpeed = true;
            }

            Apply(Current, true);
        }

        public void Apply(HumanoidPosture posture)
        {
            Apply(posture, false);
        }

        /// <summary>Reapplies the current posture after a known Animator graph rebuild.</summary>
        public void Reapply()
        {
            Apply(Current, true);
        }

        private void Apply(HumanoidPosture posture, bool force)
        {
            bool changed = Current != posture;
            Current = posture;
            if (_animator == null || !_animator.isActiveAndEnabled || (!changed && !force))
                return;

            bool isSitting = posture == HumanoidPosture.Sitting;
            if (isSitting && _hasSpeed)
                _animator.SetFloat(SpeedParam, 0f);
            if (_hasPosture)
                _animator.SetInteger(PostureParam, (int)posture);
            if (_hasIsSitting)
                _animator.SetBool(IsSittingParam, isSitting);
        }
    }
}
