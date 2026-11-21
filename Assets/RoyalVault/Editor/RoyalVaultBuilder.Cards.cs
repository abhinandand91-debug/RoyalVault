using UnityEngine;

namespace RoyalVault.EditorTools
{
    // Playing cards for Ruby's Last Hand: an ivory face, a lacquered back and the four suits, drawn as textures so no font
    // needs suit glyphs
    public static partial class RoyalVaultBuilder
    {
        private const int CardWidth = 192;
        private const int CardHeight = 268;
        private const int SuitSize = 128;

        private static void WriteCardTextures()
        {
            Color ivory = new Color32(246, 239, 222, 255);
            Color gold = new Color32(184, 135, 59, 255);
            Write("card_face", CardWidth, CardHeight, (x, y) => RoundedRect(x, y, CardWidth, CardHeight, 18f, ivory, gold, 4f));
            Write("card_back", CardWidth, CardHeight, CardBackPixel);
            Write("suit_hearts", SuitSize, SuitSize, (x, y) => Suit(x, y, Heart));
            Write("suit_diamonds", SuitSize, SuitSize, (x, y) => Suit(x, y, Diamond));
            Write("suit_clubs", SuitSize, SuitSize, (x, y) => Suit(x, y, Club));
            Write("suit_spades", SuitSize, SuitSize, (x, y) => Suit(x, y, Spade));
        }

        // Deep red lacquer with a gold border and a fine diamond lattice
        private static Color CardBackPixel(int x, int y)
        {
            Color gold = new Color32(214, 170, 92, 255);
            Color edge = RoundedRect(x, y, CardWidth, CardHeight, 18f, new Color32(112, 16, 26, 255), gold, 4f);
            if (edge.a <= 0f)
            {
                return edge;
            }

            float inset = Mathf.Min(Mathf.Min(x, CardWidth - 1 - x), Mathf.Min(y, CardHeight - 1 - y));
            bool border = inset > 13f && inset < 16f;
            float u = (x + y) % 22f;
            float v = (x - y + 1000f) % 22f;
            bool lattice = inset > 20f && (u < 1.4f || v < 1.4f);
            if (border || lattice)
            {
                Color line = Color.Lerp(edge, gold, border ? 0.9f : 0.35f);
                line.a = edge.a;
                return line;
            }

            return edge;
        }

        // White shape with 4 x 4 supersampled edges; the game tints it red or black
        private static Color Suit(int x, int y, System.Func<float, float, bool> inside)
        {
            int hits = 0;
            for (int sy = 0; sy < 4; sy++)
            {
                for (int sx = 0; sx < 4; sx++)
                {
                    float u = (x + (sx + 0.5f) / 4f) / SuitSize * 2f - 1f;
                    float v = (y + (sy + 0.5f) / 4f) / SuitSize * 2f - 1f;
                    if (inside(u, v))
                    {
                        hits++;
                    }
                }
            }

            return new Color(1f, 1f, 1f, hits / 16f);
        }

        private static bool Diamond(float u, float v)
        {
            return Mathf.Abs(u) / 0.62f + Mathf.Abs(v) / 0.92f <= 1f;
        }

        // The classic heart curve, lobes up and the point down
        private static bool Heart(float u, float v)
        {
            float x = u * 1.22f;
            float y = v * 1.22f + 0.22f;
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f;
        }

        // An upside-down heart over a flared stem
        private static bool Spade(float u, float v)
        {
            return Heart(u * 1.05f, -(v - 0.18f) * 1.05f) || Stem(u, v);
        }

        private static bool Club(float u, float v)
        {
            return Circle(u, v, 0f, 0.4f, 0.34f) || Circle(u, v, -0.38f, -0.08f, 0.34f) || Circle(u, v, 0.38f, -0.08f, 0.34f)
                   || Circle(u, v, 0f, 0.05f, 0.2f) || Stem(u, v);
        }

        private static bool Circle(float u, float v, float cx, float cy, float radius)
        {
            float dx = u - cx;
            float dy = v - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        // Narrow at the top, wide at the foot
        private static bool Stem(float u, float v)
        {
            if (v > -0.1f || v < -0.9f)
            {
                return false;
            }

            float t = (-0.1f - v) / 0.8f;
            return Mathf.Abs(u) <= Mathf.Lerp(0.06f, 0.3f, t * t);
        }
    }
}
