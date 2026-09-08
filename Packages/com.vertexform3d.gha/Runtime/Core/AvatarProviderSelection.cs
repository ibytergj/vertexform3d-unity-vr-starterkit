using System;
using UnityEngine;

namespace GHA.AvatarFramework
{
    /// <summary>
    /// Provider-neutral persistence for the selected avatar provider mode.
    /// Mode values are transport identifiers and must remain stable once shipped.
    /// </summary>
    public static class AvatarProviderSelection
    {
        public const string ModePrefsKey = "GHA_AVATAR_MODE";
        private const string LegacyModePrefsKey = "QVAS_AVATAR_MODE";

        /// <summary>Applies a saved choice to the current host, independently of preview tabs.</summary>
        public static event Action SavedAvatarApplyRequested;

        public static bool RequestApplySavedAvatar()
        {
            Action apply = SavedAvatarApplyRequested;
            if (apply == null)
                return false;
            apply.Invoke();
            return true;
        }

        public static byte LoadMode(byte defaultMode = 0)
        {
            if (PlayerPrefs.HasKey(ModePrefsKey))
                return ClampToByte(PlayerPrefs.GetInt(ModePrefsKey, defaultMode));

            if (!PlayerPrefs.HasKey(LegacyModePrefsKey))
                return defaultMode;

            byte migrated = ClampToByte(PlayerPrefs.GetInt(LegacyModePrefsKey, defaultMode));
            SaveMode(migrated);
            return migrated;
        }

        public static void SaveMode(byte mode)
        {
            PlayerPrefs.SetInt(ModePrefsKey, mode);
            PlayerPrefs.Save();
        }

        private static byte ClampToByte(int value)
        {
            return (byte)Mathf.Clamp(value, byte.MinValue, byte.MaxValue);
        }
    }
}
