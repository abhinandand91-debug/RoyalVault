using GameKit;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Four-point stars that flare and fade at random spots over the symbol; jewels twinkle in colour
    public sealed class StarsFx : SymbolFx
    {
        public const int MaxStars = 6;

        private static readonly Color[] JewelColours =
        {
            new Color(1f, 0.45f, 0.45f, 1f),
            new Color(0.5f, 1f, 0.6f, 1f),
            new Color(0.55f, 0.7f, 1f, 1f),
            Color.white,
        };

        private readonly Image[] stars = new Image[MaxStars];
        private readonly Color[] colours = new Color[MaxStars];
        private int count;
        private Vector2 cell;

        public static StarsFx Create(Transform parent, Sprite sparkle, Material additive)
        {
            RectTransform group = UiFactory.Rect("Stars", parent, Vector2.zero, Vector2.one);
            StarsFx fx = group.gameObject.AddComponent<StarsFx>();
            for (int i = 0; i < MaxStars; i++)
            {
                Image star = UiFactory.Image("Star", group, sparkle, Vector2.zero, Vector2.one * 30f);
                star.material = additive;
                fx.stars[i] = star;
            }

            return fx;
        }

        public void Play(Vector2 centre, Vector2 cellSize, int starCount, Color starColour, bool jewels, float seconds)
        {
            cell = cellSize;
            count = Mathf.Min(starCount, MaxStars);
            ((RectTransform)transform).sizeDelta = cellSize;
            for (int i = 0; i < MaxStars; i++)
            {
                bool used = i < count;
                stars[i].gameObject.SetActive(used);
                colours[i] = jewels ? JewelColours[i % JewelColours.Length] : starColour;
                stars[i].rectTransform.anchoredPosition = RandomSpot();
            }

            Begin(centre, seconds);
        }

        protected override void Animate(float time, float seconds)
        {
            for (int i = 0; i < count; i++)
            {
                float local = Mathf.Repeat(time * 1.25f + i / (float)count, 1f);
                if (local < Time.deltaTime * 1.25f + 0.001f)
                {
                    stars[i].rectTransform.anchoredPosition = RandomSpot();
                }

                float alpha = Mathf.Pow(Mathf.Sin(local * Mathf.PI), 3f) * Envelope(time, seconds);
                stars[i].color = WithAlpha(colours[i], alpha);
                stars[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(14f, cell.y * 0.32f, alpha);
                stars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f * local);
            }
        }

        private Vector2 RandomSpot()
        {
            float x = Random.Range(-0.36f, 0.36f) * cell.x;
            float y = Random.Range(-0.36f, 0.36f) * cell.y;
            return new Vector2(x, y);
        }
    }
}
