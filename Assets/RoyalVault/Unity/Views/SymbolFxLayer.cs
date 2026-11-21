using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace RoyalVault.Unity
{
    // Light around winning symbols, drawn outside the reel masks; every effect comes from a pool
    public sealed class SymbolFxLayer : MonoBehaviour
    {
        private const int PoolCapacity = 16;
        private const int PoolLimit = 64;

        private static readonly Color WarmWhite = new Color(1f, 0.95f, 0.85f, 1f);

        private readonly List<SymbolFx> live = new List<SymbolFx>();
        private Sprite glow;
        private Sprite sparkle;
        private Color magic = new Color(0.35f, 0.75f, 1f, 1f);
        private ObjectPool<SymbolFx> raysPool;
        private ObjectPool<SymbolFx> glowPool;
        private ObjectPool<SymbolFx> starsPool;
        private ObjectPool<SymbolFx> sparksPool;
        private ObjectPool<SymbolFx> ringPool;

        public void Setup(RectTransform backLayer, RectTransform frontLayer, ThemeDefinition theme)
        {
            glow = theme.glow;
            sparkle = theme.sparkle;
            magic = theme.characterMagic;
            Material additive = theme.additive;
            raysPool = NewPool(() => RaysFx.Create(backLayer, theme.rays, additive));
            glowPool = NewPool(() => GlowFx.Create(backLayer, glow, additive));
            starsPool = NewPool(() => StarsFx.Create(frontLayer, sparkle, additive));
            sparksPool = NewPool(() => SparksFx.Create(frontLayer, additive));
            ringPool = NewPool(() => RingFx.Create(frontLayer, glow, additive));
            Prewarm(raysPool, 6);
            Prewarm(glowPool, 15);
            Prewarm(starsPool, 15);
            Prewarm(sparksPool, 15);
            Prewarm(ringPool, 6);
        }

        // Builds effects up front, so the first big win does not stall while they are created
        private static void Prewarm(ObjectPool<SymbolFx> pool, int count)
        {
            var built = new SymbolFx[count];
            for (int i = 0; i < count; i++)
            {
                built[i] = pool.Get();
            }

            foreach (SymbolFx fx in built)
            {
                pool.Release(fx);
            }
        }

        // Plays the win effects of art around the cell at centre
        public void Play(Vector2 centre, Vector2 cell, ThemeDefinition.SymbolArt art, float seconds)
        {
            if (art == null || raysPool == null)
            {
                return;
            }

            switch (art.style)
            {
                case SymbolStyle.Wild:
                    Rays(centre, cell * 1.5f, new Color(1f, 0.92f, 0.7f, 1f), 0.95f, 30f, seconds);
                    Stars(centre, cell, 6, Color.white, false, seconds);
                    break;
                case SymbolStyle.Jackpot:
                    Rays(centre, cell * 1.45f, new Color(1f, 0.8f, 0.4f, 1f), 0.85f, -22f, seconds);
                    Stars(centre, cell, 5, WarmWhite, true, seconds);
                    break;
                case SymbolStyle.Scatter:
                    Rays(centre, cell * 1.45f, magic, 0.9f, 26f, seconds);
                    Sparks(centre, cell, magic, false, seconds);
                    break;
                case SymbolStyle.Gem:
                    Glow(centre, cell * 1.15f, art.tint, 0.6f, seconds);
                    Stars(centre, cell, 4, Color.white, false, seconds);
                    break;
                case SymbolStyle.Treasure:
                    Glow(centre, cell * 1.1f, art.tint, 0.35f, seconds);
                    Sparks(centre, cell, new Color(1f, 0.85f, 0.5f, 1f), true, seconds);
                    break;
                default:
                    Glow(centre, cell, art.tint, 0.22f, seconds);
                    Stars(centre, cell, 2, WarmWhite, false, seconds);
                    break;
            }
        }

        // A ring of light opens from a special symbol as it lands
        public void Land(Vector2 centre, Vector2 cell, ThemeDefinition.SymbolArt art)
        {
            if (art == null || ringPool == null)
            {
                return;
            }

            Color colour = art.style == SymbolStyle.Scatter ? magic : art.tint;
            Take<RingFx>(ringPool).Play(centre, cell, colour);
        }

        public void StopAll()
        {
            foreach (SymbolFx fx in live)
            {
                fx.Release();
            }

            live.Clear();
        }

        private void Rays(Vector2 centre, Vector2 size, Color colour, float strength, float spin, float seconds)
        {
            Take<RaysFx>(raysPool).Play(centre, size, colour, strength, spin, seconds);
        }

        private void Glow(Vector2 centre, Vector2 size, Color colour, float strength, float seconds)
        {
            Take<GlowFx>(glowPool).Play(centre, size, colour, strength, seconds);
        }

        private void Stars(Vector2 centre, Vector2 cell, int count, Color colour, bool jewels, float seconds)
        {
            Take<StarsFx>(starsPool).Play(centre, cell, count, colour, jewels, seconds);
        }

        private void Sparks(Vector2 centre, Vector2 cell, Color colour, bool dust, float seconds)
        {
            Take<SparksFx>(sparksPool).Play(centre, cell, colour, dust, dust ? glow : sparkle, seconds);
        }

        private T Take<T>(ObjectPool<SymbolFx> pool) where T : SymbolFx
        {
            SymbolFx fx = pool.Get();
            live.Add(fx);
            return (T)fx;
        }

        // Effects are built once and reused; the pool only allocates when more are on screen than ever before
        private static ObjectPool<SymbolFx> NewPool(Func<SymbolFx> create)
        {
            ObjectPool<SymbolFx> pool = null;
            pool = new ObjectPool<SymbolFx>(
                () => Created(create(), pool),
                null,
                fx => fx.gameObject.SetActive(false),
                fx => Destroy(fx.gameObject),
                false,
                PoolCapacity,
                PoolLimit);
            return pool;
        }

        private static SymbolFx Created(SymbolFx fx, ObjectPool<SymbolFx> pool)
        {
            fx.Pool = pool;
            fx.gameObject.SetActive(false);
            return fx;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                if (!live[i].Tick(deltaTime))
                {
                    live[i].Release();
                    live.RemoveAt(i);
                }
            }
        }
    }
}
