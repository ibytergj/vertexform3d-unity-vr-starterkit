using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// Curated avatar content for a deployment. Wire ids are list indices, so the
    /// catalog asset must be identical on every client (ship it with the build).
    /// </summary>
    [CreateAssetMenu(fileName = "UmaAvatarCatalog", menuName = "GHA/Avatar Suite/UMA Avatar Catalog")]
    public class UmaAvatarCatalog : ScriptableObject
    {
        [Header("Avatar system policy (experience author's choice)")]
        [Tooltip("Allow players to use the stock VertexForm3D head/body avatars.")]
        public bool enableStockAvatars = true;
        [Tooltip("Allow players to use UMA avatars.")]
        public bool enableUmaAvatars = true;
        [Tooltip("System used by players who have not made a choice. Ignored if that system is disabled above.")]
        public AvatarSystemMode defaultSystem = AvatarSystemMode.Uma;

        public List<RaceData> races = new List<RaceData>();
        public List<UMAWardrobeRecipe> wardrobeRecipes = new List<UMAWardrobeRecipe>();

        [Tooltip("Outfit applied when a player has no saved recipe. Entries must also exist in wardrobeRecipes.")]
        public List<UMAWardrobeRecipe> defaultWardrobe = new List<UMAWardrobeRecipe>();
        public int defaultRaceId = 0;

        [Header("Body types")]
        [Tooltip("Presentation and explicit starting outfits for catalog races. Race wire IDs still come from the append-only races list.")]
        public List<BodyTypeDef> bodyTypes = new List<BodyTypeDef>();

        [System.Serializable]
        public class BodyTypeDef
        {
            public RaceData race;
            public string label;
            public List<UMAWardrobeRecipe> startingWardrobe = new List<UMAWardrobeRecipe>();
        }

        public BodyTypeDef BodyType(int raceId) =>
            raceId >= 0 && raceId < races.Count
                ? bodyTypes.Find(entry => entry != null && entry.race == races[raceId]) : null;

        // This validates the definition, not generated bone mapping or headset behavior.
        public bool IsHumanoidRace(int raceId) =>
            raceId >= 0 && raceId < races.Count && races[raceId] != null
            && races[raceId].umaTarget == RaceData.UMATarget.Humanoid
            && races[raceId].TPose != null && races[raceId].baseRaceRecipe != null;

        public bool IsWardrobeCompatible(int raceId, UMAWardrobeRecipe wardrobe)
        {
            if (!IsHumanoidRace(raceId) || wardrobe == null
                || string.IsNullOrEmpty(wardrobe.wardrobeSlot) || wardrobe.wardrobeSlot == "None")
                return false;
            RaceData race = races[raceId];
            // An unassigned race list is not evidence of compatibility for curated content.
            return race.wardrobeSlots.Contains(wardrobe.wardrobeSlot)
                && wardrobe.compatibleRaces != null && wardrobe.compatibleRaces.Count > 0
                && (wardrobe.compatibleRaces.Contains(race.raceName)
                    || race.IsCrossCompatibleWith(wardrobe.compatibleRaces));
        }

        [Header("Customizer V2 — DNA (body shape)")]
        [Tooltip("UMA DNA names exposed as body-shape sliders. APPEND-ONLY: wire id = list index, so never reorder/remove (it would break saved recipes). Names must match the race's DNA (e.g. height, headSize).")]
        public List<string> dnaNames = new List<string>
        {
            "height", "upperMuscle", "lowerMuscle", "upperWeight", "lowerWeight", "belly",
            "armLength", "legsSize", "feetSize", "handsSize",
            "headSize", "headWidth", "eyeSize", "noseSize", "mouthSize", "jawsSize", "chinSize",
        };

        [Header("Customizer V2 — Color palettes")]
        [Tooltip("Color channels offered to players (skin/hair/eyes). APPEND-ONLY: wire id = list index. Each maps a UMA shared-color channel name to a SharedColorTable palette.")]
        public List<ColorChannelDef> colorChannels = new List<ColorChannelDef>();

        /// <summary>Maps a UMA shared-color channel (e.g. "Skin") to a palette of preset swatches.</summary>
        [System.Serializable]
        public class ColorChannelDef
        {
            [Tooltip("Display name for the customizer row.")]
            public string label;
            [Tooltip("UMA shared-color channel name on the avatar recipe (e.g. Skin, Hair, Eyes).")]
            public string channelName;
            [Tooltip("Preset swatches (UMA SharedColorTable, e.g. Assets/UMA/SRP/Colors/SkinColors).")]
            public SharedColorTable palette;
            [Tooltip("Palette entry assigned when an older saved recipe has no value for this channel.")]
            public int defaultPaletteIndex;
        }

        /// <summary>More than one system enabled — players may switch between them.</summary>
        public bool MultipleSystemsEnabled => enableStockAvatars && enableUmaAvatars;

        /// <summary>The default system, corrected if the author disabled it.</summary>
        public AvatarSystemMode EffectiveDefaultSystem => ResolveMode(defaultSystem);

        /// <summary>
        /// Clamps a (persisted) player choice to the systems this experience enables.
        /// With both systems disabled (a misconfiguration), UMA wins with a warning.
        /// </summary>
        public AvatarSystemMode ResolveMode(AvatarSystemMode requested)
        {
            if (!enableStockAvatars && !enableUmaAvatars)
            {
                Debug.LogWarning($"[UmaAvatarCatalog] '{name}': both avatar systems are disabled — falling back to UMA.");
                return AvatarSystemMode.Uma;
            }
            if (requested == AvatarSystemMode.Uma && !enableUmaAvatars)
                return AvatarSystemMode.Stock;
            if (requested == AvatarSystemMode.Stock && !enableStockAvatars)
                return AvatarSystemMode.Uma;
            return requested;
        }

        public RaceData Race(int id) => (id >= 0 && id < races.Count) ? races[id] : (races.Count > 0 ? races[Mathf.Clamp(defaultRaceId, 0, races.Count - 1)] : null);
        public int RaceId(RaceData race) => races.IndexOf(race);
        public UMAWardrobeRecipe Wardrobe(int id) => (id >= 0 && id < wardrobeRecipes.Count) ? wardrobeRecipes[id] : null;
        public int WardrobeId(UMAWardrobeRecipe recipe) => wardrobeRecipes.IndexOf(recipe);

        // ---- Customizer V2 accessors (append-only id = list index) ----

        public string DnaName(int id) => (id >= 0 && id < dnaNames.Count) ? dnaNames[id] : null;
        public int DnaId(string name) => dnaNames.IndexOf(name);

        public ColorChannelDef ColorChannel(int id) => (id >= 0 && id < colorChannels.Count) ? colorChannels[id] : null;

        /// <summary>The OverlayColorData swatch for a channel's palette index, or null if out of range / no palette.</summary>
        public OverlayColorData ColorSwatch(int channelId, int paletteIndex)
        {
            ColorChannelDef ch = ColorChannel(channelId);
            if (ch == null || ch.palette == null || ch.palette.colors == null) return null;
            if (paletteIndex < 0 || paletteIndex >= ch.palette.colors.Length) return null;
            return ch.palette.colors[paletteIndex];
        }
    }
}
