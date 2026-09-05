using UnityEngine;

namespace GHA.AvatarFramework
{
    /// <summary>Measures avatar-root locomotion and controls a standard humanoid animator.</summary>
    public sealed class HumanoidLocomotionDriver
    {
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int DirectionParam = Animator.StringToHash("Direction");

        private Vector3 _lastRootPosition;
        private float _smoothedSpeed;
        private Vector3 _moveDirection;
        private bool _isMoving;
        private float _idleSettleTimer;

        private const float WalkEnterSpeed = 0.2f;
        private const float WalkExitSpeed = 0.05f;
        private const float IdleSettleSeconds = 0.2f;
        private const float WalkBlendValue = 0.5f;
        private const float RunBlendStartFraction = 0.8f;
        private const float SpeedOutlierMultiplier = 2f;
        private const float GaitBlendRate = 8f;

        public float SmoothedSpeed => _smoothedSpeed;
        public float NormalizedSpeed { get; private set; }
        public Vector3 MoveDirection => _moveDirection;
        public bool IsMoving => _isMoving;

        public void Reset(Vector3 rootPosition)
        {
            _lastRootPosition = rootPosition;
            _smoothedSpeed = 0f;
            NormalizedSpeed = 0f;
            _moveDirection = Vector3.zero;
            _isMoving = false;
            _idleSettleTimer = 0f;
        }

        public void Update(Vector3 rootPosition, Animator animator, float animatorMaxSpeed, float deltaTime)
        {
            Vector3 delta = rootPosition - _lastRootPosition;
            _lastRootPosition = rootPosition;
            delta.y = 0f;

            float speed = deltaTime > 0f ? delta.magnitude / deltaTime : 0f;
            float outlierSpeed = animatorMaxSpeed > 0f
                ? Mathf.Max(1f, animatorMaxSpeed * SpeedOutlierMultiplier)
                : 20f;
            if (speed > outlierSpeed)
            {
                // Scene corrections, recentering, and teleports are not locomotion samples.
                speed = _smoothedSpeed;
            }
            else if (delta.sqrMagnitude > 0.000001f)
            {
                _moveDirection = delta.normalized;
            }

            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, speed, 10f * deltaTime);

            if (!_isMoving)
            {
                _idleSettleTimer = 0f;
                if (_smoothedSpeed >= WalkEnterSpeed)
                    _isMoving = true;
            }
            else if (_smoothedSpeed <= WalkExitSpeed)
            {
                _idleSettleTimer += deltaTime;
                if (_idleSettleTimer >= IdleSettleSeconds)
                    _isMoving = false;
            }
            else
            {
                _idleSettleTimer = 0f;
            }

            float targetSpeedParam = 0f;
            if (_isMoving && _idleSettleTimer <= 0f)
            {
                targetSpeedParam = WalkBlendValue;
                float runEnterSpeed = animatorMaxSpeed * RunBlendStartFraction;
                if (animatorMaxSpeed > runEnterSpeed && _smoothedSpeed > runEnterSpeed)
                {
                    float runBlend = Mathf.InverseLerp(runEnterSpeed, animatorMaxSpeed, _smoothedSpeed);
                    targetSpeedParam = Mathf.Lerp(WalkBlendValue, 1f, runBlend);
                }
            }

            NormalizedSpeed = Mathf.MoveTowards(
                NormalizedSpeed,
                targetSpeedParam,
                GaitBlendRate * deltaTime);
            if (!_isMoving && NormalizedSpeed <= 0.001f)
                NormalizedSpeed = 0f;

            if (animator == null || !animator.isActiveAndEnabled)
                return;

            animator.SetFloat(SpeedParam, NormalizedSpeed);
            animator.SetFloat(DirectionParam, 0f);
        }

    }
}
