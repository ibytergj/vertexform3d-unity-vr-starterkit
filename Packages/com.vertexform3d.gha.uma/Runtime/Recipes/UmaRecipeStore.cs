using System.Collections.Generic;
using GHA.AvatarFramework;
using UnityEngine;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// Single source of truth for the local player's saved avatar recipe.
    /// Used by the customizer UI (write) and UmaAvatarBridge (read at spawn).
    /// </summary>
    /// <summary>Which avatar system the local player has chosen.</summary>
    public enum AvatarSystemMode : byte
    {
        Stock = 0, // Must remain wire-compatible with AvatarExtensionSync.StockMode.
        Uma = 1,
    }

    public static class UmaRecipeStore
    {
        public const string PlayerPrefsKey = "GHA_UMA_RECIPE";
        public const string ModePrefsKey = AvatarProviderSelection.ModePrefsKey;
        public const string StockSelectionPrefsKey = "Avatar_Selection_Number";
        private const string LegacyPlayerPrefsKey = "QVAS_UMA_RECIPE";

        /// <summary>
        /// Chosen avatar system, clamped to the experience author's policy on the catalog.
        /// With no saved choice, the author's default system applies.
        /// </summary>
        public static AvatarSystemMode LoadMode(UmaAvatarCatalog catalog)
        {
            int saved = AvatarProviderSelection.LoadMode(byte.MaxValue);
            AvatarSystemMode mode;
            if (saved == byte.MaxValue)
                mode = catalog != null ? catalog.EffectiveDefaultSystem : AvatarSystemMode.Uma;
            else
                mode = saved == (int)AvatarSystemMode.Stock ? AvatarSystemMode.Stock : AvatarSystemMode.Uma;
            return catalog != null ? catalog.ResolveMode(mode) : mode;
        }

        public static void SaveMode(AvatarSystemMode mode)
        {
            AvatarProviderSelection.SaveMode((byte)mode);
        }

        /// <summary>Stock avatar index — same PlayerPrefs key the legacy system reads, so the choice is shared.</summary>
        public static int LoadStockSelection()
        {
            return PlayerPrefs.GetInt(StockSelectionPrefsKey, 0);
        }

        public static void SaveStockSelection(int index)
        {
            PlayerPrefs.SetInt(StockSelectionPrefsKey, index);
            PlayerPrefs.Save();
        }

        public static AvatarWireRecipe LoadOrDefault(UmaAvatarCatalog catalog)
        {
            string saved = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            bool migratedLegacyPreference = false;
            if (string.IsNullOrEmpty(saved) && PlayerPrefs.HasKey(LegacyPlayerPrefsKey))
            {
                saved = PlayerPrefs.GetString(LegacyPlayerPrefsKey, string.Empty);
                migratedLegacyPreference = !string.IsNullOrEmpty(saved);
            }

            if (!string.IsNullOrEmpty(saved))
            {
                try
                {
                    byte[] bytes = System.Convert.FromBase64String(saved);
                    if (UmaRecipeCodec.TryDecode(bytes, bytes.Length, out AvatarWireRecipe recipe))
                    {
                        bool addedCatalogDefaults = AddMissingCatalogColors(ref recipe, catalog);
                        if (migratedLegacyPreference || addedCatalogDefaults)
                            Save(recipe);

                        return recipe;
                    }
                }
                catch (System.FormatException) { /* fall through to default */ }
            }
            return Default(catalog);
        }

        public static void Save(AvatarWireRecipe recipe)
        {
            byte[] buffer = new byte[UmaRecipeCodec.MaxBytes];
            int length = UmaRecipeCodec.Encode(recipe, buffer);
            if (length <= 0)
            {
                Debug.LogWarning("[UmaRecipeStore] Recipe failed to encode; not saved.");
                return;
            }
            byte[] trimmed = new byte[length];
            System.Array.Copy(buffer, trimmed, length);
            PlayerPrefs.SetString(PlayerPrefsKey, System.Convert.ToBase64String(trimmed));
            PlayerPrefs.Save();
        }

        public static AvatarWireRecipe Default(UmaAvatarCatalog catalog)
        {
            var recipe = AvatarWireRecipe.Empty;
            if (catalog != null)
            {
                recipe.raceId = catalog.defaultRaceId;
                foreach (var wardrobe in catalog.defaultWardrobe)
                {
                    int id = catalog.WardrobeId(wardrobe);
                    if (id >= 0)
                        recipe.wardrobeIds.Add(id);
                }
                AddMissingCatalogColors(ref recipe, catalog);
            }
            return recipe;
        }

        /// <summary>
        /// Adds only color choices absent from an older recipe. Existing player choices are
        /// preserved, and invalid configured defaults are clamped to their palette.
        /// </summary>
        private static bool AddMissingCatalogColors(
            ref AvatarWireRecipe recipe,
            UmaAvatarCatalog catalog)
        {
            bool changed = false;
            if (recipe.wardrobeIds == null)
            {
                recipe.wardrobeIds = new List<int>();
                changed = true;
            }
            if (recipe.dna == null)
            {
                recipe.dna = new List<DnaValue>();
                changed = true;
            }
            if (recipe.colors == null)
            {
                recipe.colors = new List<ColorValue>();
                changed = true;
            }

            if (catalog == null || catalog.colorChannels == null)
                return changed;

            for (int channelId = 0; channelId < catalog.colorChannels.Count; channelId++)
            {
                UmaAvatarCatalog.ColorChannelDef channel = catalog.colorChannels[channelId];
                if (channel == null ||
                    channel.palette == null ||
                    channel.palette.colors == null ||
                    channel.palette.colors.Length == 0)
                {
                    continue;
                }

                bool alreadySpecified = false;
                for (int i = 0; i < recipe.colors.Count; i++)
                {
                    if (recipe.colors[i].channelId == channelId)
                    {
                        alreadySpecified = true;
                        break;
                    }
                }
                if (alreadySpecified)
                    continue;

                recipe.colors.Add(new ColorValue
                {
                    channelId = channelId,
                    paletteIndex = Mathf.Clamp(
                        channel.defaultPaletteIndex,
                        0,
                        channel.palette.colors.Length - 1),
                });
                changed = true;
            }

            return changed;
        }
    }
}
