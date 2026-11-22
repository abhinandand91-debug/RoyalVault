using UnityEngine;

namespace RoyalVault.EditorTools
{
    // Art for the Treasury, drawn as textures: a black-lacquer canopic urn banded in gold, and the guardian's eye
    public static partial class RoyalVaultBuilder
    {
        private const int UrnWidth = 256;
        private const int UrnHeight = 320;
        private const int EyeWidth = 256;
        private const int EyeHeight = 160;
        private const int Samples = 3;

        private static readonly Color UrnLacquer = new Color32(26, 22, 30, 255);
        private static readonly Color UrnGold = new Color32(214, 170, 92, 255);

        private static void WriteTreasuryTextures()
        {
            Write("urn", UrnWidth, UrnHeight, UrnPixel);
            Write("eye", EyeWidth, EyeHeight, EyePixel);
        }

        // Supersampled so the outline stays smooth
        private static Color UrnPixel(int x, int y)
        {
            Color sum = Color.clear;
            for (int sy = 0; sy < Samples; sy++)
            {
                for (int sx = 0; sx < Samples; sx++)
                {
                    float u = (x + (sx + 0.5f) / Samples) / UrnWidth * 2f - 1f;
                    float v = (y + (sy + 0.5f) / Samples) / UrnHeight;
                    sum += UrnSample(u, v);
                }
            }

            Color colour = sum / (Samples * Samples);
            if (colour.a > 0f)
            {
                colour.r /= colour.a;
                colour.g /= colour.a;
                colour.b /= colour.a;
            }

            return colour;
        }

        // u runs -1 to 1 across, v 0 to 1 up; the colour comes back with its alpha multiplied in
        private static Color UrnSample(float u, float v)
        {
            float radius = UrnRadius(v);
            float lid = LidCoverage(u, v);
            bool body = radius > 0f && Mathf.Abs(u) <= radius;
            if (!body && lid <= 0f)
            {
                return Color.clear;
            }

            float across = body ? u / radius : u / 0.56f;
            Color colour = body ? BodyColour(across, v, radius - Mathf.Abs(u)) : LidColour(across, v);
            return colour;
        }

        // The jar's half-width at height v: a foot, a full shoulder and a narrow neck
        private static float UrnRadius(float v)
        {
            if (v < 0.03f || v > 0.75f)
            {
                return 0f;
            }

            if (v < 0.1f)
            {
                return 0.44f;
            }

            if (v < 0.71f)
            {
                float t = (v - 0.1f) / 0.61f;
                return 0.42f + 0.4f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.85f));
            }

            return 0.46f;
        }

        // A domed lid with a knob on top
        private static float LidCoverage(float u, float v)
        {
            float dx = u / 0.56f;
            float dy = (v - 0.75f) / 0.17f;
            bool dome = v >= 0.75f && dx * dx + dy * dy <= 1f;
            float kx = u / 0.09f;
            float ky = (v - 0.94f) / 0.045f;
            bool knob = kx * kx + ky * ky <= 1f;
            return dome || knob ? 1f : 0f;
        }

        private static Color BodyColour(float across, float v, float edge)
        {
            bool foot = v < 0.1f;
            bool band = Within(v, 0.17f, 0.195f) || Within(v, 0.6f, 0.625f) || Within(v, 0.7f, 0.75f);
            bool rim = edge < 0.022f;
            bool dots = Within(v, 0.385f, 0.425f) && Mathf.Repeat(across * 4.5f + 0.25f, 1f) < 0.32f;
            Color baseColour = foot || band || rim || dots ? UrnGold : UrnLacquer;
            return Shade(baseColour, across);
        }

        private static Color LidColour(float across, float v)
        {
            bool rim = v < 0.765f;
            bool knob = v > 0.9f;
            return Shade(rim || knob ? UrnGold : UrnLacquer, across);
        }

        // Round shading with a soft highlight to the upper left
        private static Color Shade(Color colour, float across)
        {
            float a = Mathf.Clamp(across, -1f, 1f);
            float roundness = Mathf.Sqrt(1f - a * a);
            float light = 0.45f + 0.55f * roundness;
            float highlight = Mathf.Exp(-Mathf.Pow((a + 0.45f) / 0.16f, 2f)) * 0.55f;
            Color shaded = colour * light + Color.white * highlight * (colour == UrnGold ? 0.6f : 0.25f);
            shaded.a = 1f;
            return shaded;
        }

        private static bool Within(float v, float from, float to)
        {
            return v >= from && v <= to;
        }

        // White, so the game can tint it: an almond eye with its iris, a brow and the falcon's tear line below
        private static Color EyePixel(int x, int y)
        {
            int hits = 0;
            for (int sy = 0; sy < 4; sy++)
            {
                for (int sx = 0; sx < 4; sx++)
                {
                    float u = (x + (sx + 0.5f) / 4f) / EyeWidth * 2f - 1f;
                    float v = ((y + (sy + 0.5f) / 4f) / EyeHeight * 2f - 1f) * EyeHeight / EyeWidth;
                    if (EyeShape(u, v))
                    {
                        hits++;
                    }
                }
            }

            return new Color(1f, 1f, 1f, hits / 16f);
        }

        private static bool EyeShape(float u, float v)
        {
            float lid = 0.26f * (1f - u * u / 0.62f);
            bool insideLids = Mathf.Abs(u) < 0.78f && Mathf.Abs(v) <= lid;
            bool outline = insideLids && Mathf.Abs(v) >= lid - 0.045f;
            float iris = (u * u + (v + 0.01f) * (v + 0.01f));
            bool pupil = iris <= 0.16f * 0.16f;
            bool brow = u > -0.7f && u < 0.85f && Mathf.Abs(v - (0.38f - 0.12f * u * u)) < 0.035f;
            bool tail = u > 0.55f && u < 0.95f && Mathf.Abs(v - (-0.02f - 0.05f * (u - 0.55f))) < 0.03f;
            bool tear = Mathf.Abs(u + 0.05f) < 0.035f && v < -0.18f && v > -0.42f;
            return outline || pupil || brow || tail || tear;
        }
    }
}
