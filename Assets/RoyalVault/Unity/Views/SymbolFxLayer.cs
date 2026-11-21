using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Light around winning symbols, drawn outside the reel masks
    public sealed class SymbolFxLayer : MonoBehaviour
    {
        private RectTransform back;
        private RectTransform front;
        private Sprite rays;
        private Sprite glow;
        private Sprite sparkle;
        private Material additive;
        private Color magic = new Color(0.35f, 0.75f, 1f, 1f);
        private readonly List<GameObject> live = new List<GameObject>();

        public void Setup(RectTransform backLayer, RectTransform frontLayer, ThemeDefinition theme)
        {
            back = backLayer;
            front = frontLayer;
            rays = theme.rays;
            glow = theme.glow;
            sparkle = theme.sparkle;
            additive = theme.additive;
            magic = theme.characterMagic;
        }

        // Plays the win effects of art around the cell at centre
        public void Play(Vector2 centre, Vector2 cell, ThemeDefinition.SymbolArt art, float seconds)
        {
            if (art == null || back == null) return;
            Color tint = art.tint;
            switch (art.style)
            {
                case SymbolStyle.Wild:
                    Track(StartRays(centre, cell * 1.5f, new Color(1f, 0.92f, 0.7f, 1f), 0.95f, 30f, seconds));
                    Track(StartStars(centre, cell, 6, Color.white, seconds));
                    break;
                case SymbolStyle.Jackpot:
                    Track(StartRays(centre, cell * 1.45f, new Color(1f, 0.8f, 0.4f, 1f), 0.85f, -22f, seconds));
                    Track(StartStars(centre, cell, 5, new Color(1f, 0.95f, 0.85f, 1f), seconds, true));
                    break;
                case SymbolStyle.Scatter:
                    Track(StartRays(centre, cell * 1.45f, magic, 0.9f, 26f, seconds));
                    Track(StartEmitter(centre, cell, magic, seconds, false));
                    break;
                case SymbolStyle.Gem:
                    Track(StartGlow(centre, cell * 1.15f, tint, 0.6f, seconds));
                    Track(StartStars(centre, cell, 4, Color.white, seconds));
                    break;
                case SymbolStyle.Treasure:
                    Track(StartGlow(centre, cell * 1.1f, tint, 0.35f, seconds));
                    Track(StartEmitter(centre, cell, new Color(1f, 0.85f, 0.5f, 1f), seconds, true));
                    break;
                default:
                    Track(StartGlow(centre, cell, tint, 0.22f, seconds));
                    Track(StartStars(centre, cell, 2, new Color(1f, 0.95f, 0.85f, 1f), seconds));
                    break;
            }
        }

        // A ring of light opens from a special symbol as it lands
        public void Land(Vector2 centre, Vector2 cell, ThemeDefinition.SymbolArt art)
        {
            if (art == null || back == null) return;
            Color c = art.style == SymbolStyle.Scatter ? magic : art.tint;
            Image ring = Light("Land Ring", front, glow, centre, cell * 0.6f, c);
            Track(ring.gameObject);
            StartCoroutine(Fade(ring, 0.45f, k =>
            {
                ring.rectTransform.sizeDelta = cell * Mathf.Lerp(0.6f, 1.7f, Ease.OutCubic(k));
                ring.color = new Color(c.r, c.g, c.b, 0.85f * (1f - k));
            }));
        }

        public void StopAll()
        {
            foreach (GameObject g in live) if (g != null) Destroy(g);
            live.Clear();
        }

        private void Track(GameObject g)
        {
            live.RemoveAll(x => x == null);
            live.Add(g);
        }

        private Image Light(string name, RectTransform parent, Sprite sprite, Vector2 centre, Vector2 size, Color colour)
        {
            Image image = UiFactory.Image(name, parent, sprite, centre, size);
            image.material = additive;
            image.color = colour;
            return image;
        }

        private GameObject StartRays(Vector2 centre, Vector2 size, Color c, float strength, float spin, float seconds)
        {
            Image r = Light("Rays", back, rays, centre, size, new Color(c.r, c.g, c.b, 0f));
            Image inner = Light("Inner Rays", r.rectTransform, rays, Vector2.zero, size * 0.75f, new Color(c.r, c.g, c.b, 0f));
            StartCoroutine(Fade(r, seconds, k =>
            {
                float t = k * seconds;
                float alpha = strength * Envelope(t, seconds) * (0.75f + 0.25f * Mathf.Sin(t * 4f));
                r.color = new Color(c.r, c.g, c.b, alpha);
                r.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spin * t);
                r.rectTransform.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Min(1f, t * 4f));
                // a second, smaller set of rays turns the other way, so the light shimmers instead of just turning
                inner.color = new Color(c.r, c.g, c.b, alpha * 0.8f);
                inner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -spin * 0.6f * t + 15f);
            }));
            return r.gameObject;
        }

        private GameObject StartGlow(Vector2 centre, Vector2 size, Color c, float strength, float seconds)
        {
            Image g = Light("Glow", back, glow, centre, size, new Color(c.r, c.g, c.b, 0f));
            StartCoroutine(Fade(g, seconds, k =>
            {
                float t = k * seconds;
                g.color = new Color(c.r, c.g, c.b, strength * Envelope(t, seconds) * (0.7f + 0.3f * Mathf.Sin(t * 5.6f)));
            }));
            return g.gameObject;
        }

        // Four-point stars that flare and fade at random spots over the symbol; jewels twinkle in colour
        private GameObject StartStars(Vector2 centre, Vector2 cell, int count, Color c, float seconds, bool jewels = false)
        {
            RectTransform group = UiFactory.Rect("Stars", front, centre, cell);
            var stars = new Image[count];
            var phase = new float[count];
            var colours = new Color[count];
            Color[] jewel = { new Color(1f, 0.45f, 0.45f, 1f), new Color(0.5f, 1f, 0.6f, 1f), new Color(0.55f, 0.7f, 1f, 1f), Color.white };
            for (int i = 0; i < count; i++)
            {
                stars[i] = Light("Star", group, sparkle, Vector2.zero, Vector2.one * 30f, new Color(1f, 1f, 1f, 0f));
                phase[i] = i / (float)count;
                colours[i] = jewels ? jewel[i % jewel.Length] : c;
                stars[i].rectTransform.anchoredPosition = RandomSpot(cell);
            }
            StartCoroutine(Fade(stars.Length > 0 ? stars[0] : null, seconds, k =>
            {
                float t = k * seconds;
                for (int i = 0; i < stars.Length; i++)
                {
                    float cycle = t * 1.25f + phase[i];
                    float local = Mathf.Repeat(cycle, 1f);
                    if (local < Time.deltaTime * 1.25f + 0.001f) stars[i].rectTransform.anchoredPosition = RandomSpot(cell);
                    float a = Mathf.Pow(Mathf.Sin(local * Mathf.PI), 3f) * Envelope(t, seconds);
                    Color s = colours[i];
                    stars[i].color = new Color(s.r, s.g, s.b, a);
                    stars[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(14f, cell.y * 0.32f, a);
                    stars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f * local);
                }
            }, group.gameObject));
            return group.gameObject;
        }

        private GameObject StartEmitter(Vector2 centre, Vector2 cell, Color c, float seconds, bool dust)
        {
            RectTransform rect = UiFactory.Rect(dust ? "Gold Dust" : "Sparks", front, centre - new Vector2(0f, cell.y * 0.3f), Vector2.one * 10f);
            var emitter = rect.gameObject.AddComponent<UiParticleEmitter>();
            emitter.Sprite = dust ? glow : sparkle;
            emitter.Material = additive;
            emitter.Rate = dust ? 16f : 12f;
            emitter.LifeRange = new Vector2(0.7f, 1.3f);
            emitter.SizeRange = dust ? new Vector2(8f, 16f) : new Vector2(12f, 24f);
            emitter.EndSize = 0.3f;
            emitter.Area = new Vector2(cell.x * 0.6f, 20f);
            emitter.VelocityMin = new Vector2(-20f, 60f);
            emitter.VelocityMax = new Vector2(20f, 140f);
            emitter.Swirl = 18f;
            emitter.SwirlFrequency = 3f;
            emitter.MaxParticles = 40;
            emitter.StartColor = new Color(c.r, c.g, c.b, 0.9f);
            emitter.EndColor = new Color(c.r, c.g, c.b, 0f);
            StartCoroutine(Fade(null, seconds, k => emitter.Intensity = k < 0.85f ? 1f : 0f, rect.gameObject, 1.2f));
            return rect.gameObject;
        }

        // Fades in fast and out over the last fifth
        private static float Envelope(float t, float seconds)
        {
            return Mathf.Clamp01(t * 6f) * Mathf.Clamp01((seconds - t) / Mathf.Max(0.05f, seconds * 0.2f));
        }

        private static Vector2 RandomSpot(Vector2 cell)
        {
            return new Vector2(Random.Range(-0.36f, 0.36f) * cell.x, Random.Range(-0.36f, 0.36f) * cell.y);
        }

        // Runs step from 0 to 1 over seconds, then removes the effect
        private IEnumerator Fade(Graphic graphic, float seconds, System.Action<float> step, GameObject owner = null, float linger = 0f)
        {
            GameObject target = owner != null ? owner : graphic != null ? graphic.gameObject : null;
            float t = 0f;
            while (t < seconds)
            {
                if (target == null) yield break;
                step(t / seconds);
                t += Time.deltaTime;
                yield return null;
            }
            if (linger > 0f) yield return new WaitForSeconds(linger);
            if (target != null) Destroy(target);
        }
    }
}
