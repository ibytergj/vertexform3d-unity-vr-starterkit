using System;
using UnityEngine;

namespace GHA.AvatarFramework
{
    public enum AvatarVisibility
    {
        Visible,
        HeadOnly,
        WholeBody
    }

    public interface IHumanoidAvatarInstance
    {
        Transform Root { get; }
        Animator Animator { get; }
        bool IsReady { get; }
        event Action HumanoidRebuilt;
        void SetFirstPersonVisibility(AvatarVisibility visibility, int cullLayer);
        bool TryGetRendererBounds(out Bounds bounds);
    }

    /// <summary>Provider-neutral pose targets and tracking state supplied by a host XR rig.</summary>
    public interface IHumanoidRigInput
    {
        Transform MainAvatar { get; }
        Transform Body { get; }
        Transform HeadTarget { get; }
        Transform TrackedHead { get; }
        Transform LeftHand { get; }
        Transform RightHand { get; }
        bool IsVrStyle { get; }
        bool IsLocalAuthority { get; }
        bool IsHeadTracked { get; }
        bool IsHandTracking { get; }
        bool TryGetTrackingSpaceEyeHeight(out float eyeHeight);
        bool TryApplySeatedViewCorrection(Vector3 worldCorrection);
    }
}
