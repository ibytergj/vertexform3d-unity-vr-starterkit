using System.Collections.Generic;
using UnityEngine;

namespace GHA.AvatarFramework
{
    /// <summary>Process-wide, provider-neutral player height calibration shared by scene hosts.</summary>
    public static class VrCalibrationSession
    {
        private const string EyeHeightPreference = "GHA.CalibratedStandingEyeHeight";
        private const string LegacyEyeHeightPreference = "QVAS.CalibratedStandingEyeHeight";
        private const int MaximumSamples = 240;
        private static readonly List<TimedSample> Samples = new List<TimedSample>();
        private static bool _loaded;

        public static bool HasStandingEyeHeight { get; private set; }
        public static float StandingEyeHeight { get; private set; }

        public static bool ObserveStandingEyeHeight(float now, float eyeHeight, float minimumStandingEyeHeight, float settleSeconds, float maximumWindowRange)
        {
            EnsureLoaded();
            if (HasStandingEyeHeight)
                return false;
            if (!float.IsFinite(eyeHeight) || eyeHeight < Mathf.Max(0.1f, minimumStandingEyeHeight))
            {
                Samples.Clear();
                return false;
            }

            Samples.Add(new TimedSample(now, eyeHeight));
            // Retain a small margin beyond the requested window. Trimming to exactly
            // settleSeconds before testing makes the retained frame-to-frame span stay
            // infinitesimally shorter than the requirement forever.
            TrimSamples(now - Mathf.Max(0.5f, settleSeconds) - 0.25f);
            if (Samples.Count < 2 || Samples[Samples.Count - 1].Time - Samples[0].Time < settleSeconds)
                return false;

            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            var values = new List<float>(Samples.Count);
            foreach (TimedSample sample in Samples)
            {
                minimum = Mathf.Min(minimum, sample.Value);
                maximum = Mathf.Max(maximum, sample.Value);
                values.Add(sample.Value);
            }
            if (maximum - minimum > Mathf.Max(0.01f, maximumWindowRange))
                return false;

            values.Sort();
            StandingEyeHeight = values[values.Count / 2];
            HasStandingEyeHeight = true;
            Samples.Clear();
            PlayerPrefs.SetFloat(EyeHeightPreference, StandingEyeHeight);
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetObservation() => Samples.Clear();

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;
            _loaded = true;

            float saved = PlayerPrefs.GetFloat(EyeHeightPreference, 0f);
            bool migratedLegacyPreference = false;
            if ((!float.IsFinite(saved) || saved < 1.0f || saved > 2.4f)
                && PlayerPrefs.HasKey(LegacyEyeHeightPreference))
            {
                saved = PlayerPrefs.GetFloat(LegacyEyeHeightPreference, 0f);
                migratedLegacyPreference = true;
            }

            if (float.IsFinite(saved) && saved >= 1.0f && saved <= 2.4f)
            {
                StandingEyeHeight = saved;
                HasStandingEyeHeight = true;

                if (migratedLegacyPreference)
                {
                    PlayerPrefs.SetFloat(EyeHeightPreference, saved);
                    PlayerPrefs.Save();
                }
            }
        }

        private static void TrimSamples(float earliestTime)
        {
            while (Samples.Count > MaximumSamples || (Samples.Count > 0 && Samples[0].Time < earliestTime))
                Samples.RemoveAt(0);
        }

        private readonly struct TimedSample
        {
            public TimedSample(float time, float value) { Time = time; Value = value; }
            public float Time { get; }
            public float Value { get; }
        }
    }
}
