using System.Collections.Generic;

namespace GHA.AvatarSuite
{
    /// <summary>One DNA value in a recipe. Id indexes UmaAvatarCatalog.dnaNames (append-only). Value is 0..255 ↔ 0..1.</summary>
    public struct DnaValue
    {
        public int id;
        public byte value;
    }

    /// <summary>One color choice. ChannelId indexes UmaAvatarCatalog.colorChannels (append-only); paletteIndex indexes that channel's palette.</summary>
    public struct ColorValue
    {
        public int channelId;
        public int paletteIndex;
    }

    /// <summary>A decoded avatar recipe. Ids index into the shared UmaAvatarCatalog.</summary>
    public struct AvatarWireRecipe
    {
        public int raceId;
        public List<int> wardrobeIds;
        public List<DnaValue> dna;       // body-shape sliders (Customizer V2)
        public List<ColorValue> colors;  // skin/hair/eyes palette choices (Customizer V2)

        public static AvatarWireRecipe Empty => new AvatarWireRecipe
        {
            raceId = 0,
            wardrobeIds = new List<int>(),
            dna = new List<DnaValue>(),
            colors = new List<ColorValue>(),
        };
    }

    /// <summary>
    /// Compact wire format for avatar recipes, synced via Fusion networked bytes.
    /// v1 layout: [1][raceId:1][wardrobeCount:1][wardrobe ids: count*2 LE]
    /// v2 layout: v1 body, then [dnaCount:1][(dnaId:1, value:1) * dnaCount][colorCount:1][(channelId:1, paletteIndex:1) * colorCount]
    /// Decode reads either version (v1 recipes saved before V2 still load). Encode always writes v2.
    /// </summary>
    public static class UmaRecipeCodec
    {
        public const byte Version = 2;
        public const int MaxBytes = 128;

        public static int Encode(AvatarWireRecipe recipe, byte[] buffer)
        {
            int wardrobe = recipe.wardrobeIds != null ? recipe.wardrobeIds.Count : 0;
            int dnaCount = recipe.dna != null ? recipe.dna.Count : 0;
            int colorCount = recipe.colors != null ? recipe.colors.Count : 0;
            int required = 3 + wardrobe * 2 + 1 + dnaCount * 2 + 1 + colorCount * 2;
            if (buffer == null || buffer.Length < required || required > MaxBytes
                || wardrobe > byte.MaxValue || dnaCount > byte.MaxValue || colorCount > byte.MaxValue)
                return -1;

            int p = 0;
            buffer[p++] = Version;
            buffer[p++] = (byte)recipe.raceId;
            buffer[p++] = (byte)wardrobe;
            for (int i = 0; i < wardrobe; i++)
            {
                int id = recipe.wardrobeIds[i];
                buffer[p++] = (byte)(id & 0xFF);
                buffer[p++] = (byte)((id >> 8) & 0xFF);
            }
            buffer[p++] = (byte)dnaCount;
            for (int i = 0; i < dnaCount; i++)
            {
                buffer[p++] = (byte)recipe.dna[i].id;
                buffer[p++] = recipe.dna[i].value;
            }
            buffer[p++] = (byte)colorCount;
            for (int i = 0; i < colorCount; i++)
            {
                buffer[p++] = (byte)recipe.colors[i].channelId;
                buffer[p++] = (byte)recipe.colors[i].paletteIndex;
            }
            return p;
        }

        public static bool TryDecode(byte[] buffer, int length, out AvatarWireRecipe recipe)
        {
            recipe = AvatarWireRecipe.Empty;
            if (buffer == null || length < 3)
                return false;
            byte version = buffer[0];
            if (version != 1 && version != 2)
                return false;

            int p = 1;
            recipe.raceId = buffer[p++];
            int wardrobe = buffer[p++];
            if (length < p + wardrobe * 2)
                return false;
            for (int i = 0; i < wardrobe; i++)
                recipe.wardrobeIds.Add(buffer[p++] | (buffer[p++] << 8));

            if (version == 1)
                return true; // v1 has no DNA/colors

            if (p >= length) return true; // tolerate truncated v2 (no dna/colors written)
            int dnaCount = buffer[p++];
            if (length < p + dnaCount * 2)
                return false;
            for (int i = 0; i < dnaCount; i++)
                recipe.dna.Add(new DnaValue { id = buffer[p++], value = buffer[p++] });

            if (p >= length) return true;
            int colorCount = buffer[p++];
            if (length < p + colorCount * 2)
                return false;
            for (int i = 0; i < colorCount; i++)
                recipe.colors.Add(new ColorValue { channelId = buffer[p++], paletteIndex = buffer[p++] });

            return true;
        }
    }
}
