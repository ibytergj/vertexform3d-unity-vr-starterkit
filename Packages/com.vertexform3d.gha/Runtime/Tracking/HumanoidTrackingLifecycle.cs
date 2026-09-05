namespace GHA.AvatarFramework
{
    /// <summary>
    /// Provider-neutral result of one local headset tracking lifecycle update.
    /// The host decides how to respond to tracking loss and how to rebuild its rig.
    /// </summary>
    public readonly struct HumanoidTrackingLifecycleUpdate
    {
        public HumanoidTrackingLifecycleUpdate(
            bool trackingLost,
            bool trackingRegained,
            bool shouldReinitialize)
        {
            TrackingLost = trackingLost;
            TrackingRegained = trackingRegained;
            ShouldReinitialize = shouldReinitialize;
        }

        public bool TrackingLost { get; }
        public bool TrackingRegained { get; }
        public bool ShouldReinitialize { get; }
    }

    /// <summary>
    /// Tracks local headset loss/regain and schedules one delayed rig reinitialization.
    /// Contains no avatar-provider, networking, or scene-host behavior.
    /// </summary>
    public sealed class HumanoidTrackingLifecycle
    {
        private bool _lastHeadTracked;
        private bool _reinitializeQueued;
        private float _reinitializeTime;

        public void Reset(bool headTracked)
        {
            _lastHeadTracked = headTracked;
            _reinitializeQueued = false;
            _reinitializeTime = 0f;
        }

        public void SynchronizeTracking(bool headTracked)
        {
            _lastHeadTracked = headTracked;
        }

        public void QueueReinitialize(float currentTime, float delaySeconds)
        {
            _reinitializeQueued = true;
            _reinitializeTime = currentTime + (delaySeconds > 0f ? delaySeconds : 0f);
        }

        public HumanoidTrackingLifecycleUpdate Update(
            bool headTracked,
            float currentTime,
            float reinitializeDelaySeconds)
        {
            bool trackingRegained = headTracked && !_lastHeadTracked;
            bool trackingLost = !headTracked && _lastHeadTracked;

            if (trackingRegained)
                QueueReinitialize(currentTime, reinitializeDelaySeconds);

            _lastHeadTracked = headTracked;

            bool shouldReinitialize =
                _reinitializeQueued
                && headTracked
                && currentTime >= _reinitializeTime;

            if (shouldReinitialize)
            {
                _reinitializeQueued = false;
                _reinitializeTime = 0f;
            }

            return new HumanoidTrackingLifecycleUpdate(
                trackingLost,
                trackingRegained,
                shouldReinitialize);
        }
    }
}

