using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace GHA.AvatarFramework
{
    /// <summary>Provider-neutral runtime arm IK for a Unity Humanoid Animator.</summary>
    public sealed class HumanoidArmRig
    {
        private Transform _leftTarget;
        private Transform _rightTarget;
        private Transform _avatarRoot;
        private GameObject _rigRoot;
        private RigBuilder _rigBuilder;
        private TwoBoneIKConstraint _leftArmConstraint;
        private TwoBoneIKConstraint _rightArmConstraint;

        public bool IsReady { get; private set; }
        public Transform LeftHand { get; private set; }
        public Transform RightHand { get; private set; }

        public bool Bind(GameObject avatarRoot, Animator animator, Transform leftSource, Transform rightSource)
        {
            Dispose();
            if (avatarRoot == null || animator == null || !animator.isHuman || leftSource == null || rightSource == null)
                return false;

            Transform leftUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rightLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (leftUpper == null || leftLower == null || leftHand == null
                || rightUpper == null || rightLower == null || rightHand == null)
                return false;

            LeftHand = leftHand;
            RightHand = rightHand;
            _avatarRoot = avatarRoot.transform;

            _rigRoot = new GameObject("Humanoid_VR_ArmIK");
            _rigRoot.transform.SetParent(avatarRoot.transform, false);
            Rig rig = _rigRoot.AddComponent<Rig>();
            rig.weight = 1f;
            _leftTarget = CreateTarget("LeftHandTarget", _rigRoot.transform, leftSource);
            _rightTarget = CreateTarget("RightHandTarget", _rigRoot.transform, rightSource);
            _leftArmConstraint = ConfigureConstraint("LeftArmIK", leftUpper, leftLower, leftHand, _leftTarget, _rigRoot.transform);
            _rightArmConstraint = ConfigureConstraint("RightArmIK", rightUpper, rightLower, rightHand, _rightTarget, _rigRoot.transform);

            _rigBuilder = avatarRoot.GetComponent<RigBuilder>();
            if (_rigBuilder == null)
                _rigBuilder = avatarRoot.AddComponent<RigBuilder>();
            _rigBuilder.Clear();
            _rigBuilder.layers.Clear();
            _rigBuilder.layers.Add(new RigLayer(rig));
            IsReady = _rigBuilder.Build();
            return IsReady;
        }

        public void UpdateTargets(Transform leftSource, Transform rightSource, Vector3 leftOffset, Vector3 rightOffset)
        {
            if (!IsReady)
                return;
            UpdateTarget(_leftTarget, leftSource, leftOffset);
            UpdateTarget(_rightTarget, rightSource, rightOffset);
        }

        public void SetArmTrackingActive(bool active)
        {
            float weight = active ? 1f : 0f;
            if (_leftArmConstraint != null)
                _leftArmConstraint.weight = weight;
            if (_rightArmConstraint != null)
                _rightArmConstraint.weight = weight;
        }

        public void Dispose()
        {
            if (_rigBuilder != null)
            {
                _rigBuilder.Clear();
                _rigBuilder.layers.Clear();
            }
            if (_rigRoot != null)
                Object.Destroy(_rigRoot);
            _leftTarget = null;
            _rightTarget = null;
            _avatarRoot = null;
            _rigRoot = null;
            _rigBuilder = null;
            _leftArmConstraint = null;
            _rightArmConstraint = null;
            LeftHand = null;
            RightHand = null;
            IsReady = false;
        }

        private static Transform CreateTarget(string name, Transform parent, Transform source)
        {
            Transform target = new GameObject(name).transform;
            target.SetParent(parent, false);
            target.SetPositionAndRotation(source.position, source.rotation);
            return target;
        }

        private static TwoBoneIKConstraint ConfigureConstraint(string name, Transform upper, Transform lower, Transform hand, Transform target, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TwoBoneIKConstraint constraint = go.AddComponent<TwoBoneIKConstraint>();
            constraint.weight = 1f;
            ref TwoBoneIKConstraintData data = ref constraint.data;
            data.root = upper;
            data.mid = lower;
            data.tip = hand;
            data.target = target;
            data.hint = null;
            data.targetPositionWeight = 1f;
            data.targetRotationWeight = 1f;
            data.hintWeight = 0f;
            data.maintainTargetPositionOffset = false;
            data.maintainTargetRotationOffset = false;
            return constraint;
        }

        private static void UpdateTarget(Transform target, Transform source, Vector3 offset)
        {
            if (target == null || source == null)
                return;
            target.position = source.position;
            Quaternion x = Quaternion.AngleAxis(offset.x, source.right);
            Quaternion y = Quaternion.AngleAxis(offset.y, source.up);
            Quaternion z = Quaternion.AngleAxis(offset.z, source.forward);
            target.rotation = z * y * x * source.rotation;
        }

    }
}
