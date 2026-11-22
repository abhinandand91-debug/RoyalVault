using System.Collections.Generic;
using Coffee.UIExtensions;
using PrimeTween;
using UnityEngine;
using UnityEngine.Pool;

namespace GameKit
{
    // Plays a ready-made ParticleSystem prefab inside a UI canvas; finished effects go back to a pool per prefab
    // (Unity's ObjectPool) and are reused, not destroyed
    public static class UiVfx
    {
        private const int PoolCapacity = 4;
        private const int PoolLimit = 16;

        private static readonly Dictionary<GameObject, ObjectPool<UIParticle>> Pools = new Dictionary<GameObject, ObjectPool<UIParticle>>();
        private static readonly Dictionary<UIParticle, GameObject> Sources = new Dictionary<UIParticle, GameObject>();
        private static readonly List<UIParticle> Destroyed = new List<UIParticle>();
        // The parent the next effect a pool builds is made under (pools build on demand inside Get)
        private static Transform building;

        public static GameObject Play(GameObject prefab, Transform parent, Vector2 position, float scale, float seconds)
        {
            if (prefab == null || parent == null)
            {
                return null;
            }

            UIParticle effect = Take(prefab, parent);
            var rect = (RectTransform)effect.transform;
            rect.anchoredPosition = position;
            effect.scale = scale;
            effect.gameObject.SetActive(true);
            effect.Clear();
            effect.Play();
            Tween.Delay(effect, seconds, finished => Release(finished), warnIfTargetDestroyed: false);
            return effect.gameObject;
        }

        // Builds an effect ahead of time, so its first use does not stall the frame
        public static void Prewarm(GameObject prefab, Transform parent)
        {
            if (prefab == null || parent == null)
            {
                return;
            }

            Release(Take(prefab, parent));
        }

        // A pooled effect for the prefab, moved under parent; effects destroyed with an old scene are skipped
        private static UIParticle Take(GameObject prefab, Transform parent)
        {
            ObjectPool<UIParticle> pool = PoolFor(prefab);
            building = parent;
            UIParticle effect = pool.Get();
            while (effect == null)
            {
                effect = pool.Get();
            }

            effect.transform.SetParent(parent, false);
            return effect;
        }

        private static ObjectPool<UIParticle> PoolFor(GameObject prefab)
        {
            if (!Pools.TryGetValue(prefab, out ObjectPool<UIParticle> pool))
            {
                pool = new ObjectPool<UIParticle>(() => Create(prefab), null, Park, Discard, false, PoolCapacity, PoolLimit);
                Pools[prefab] = pool;
            }

            return pool;
        }

        private static UIParticle Create(GameObject prefab)
        {
            var holder = new GameObject(prefab.name + " (UI)", typeof(RectTransform));
            var rect = (RectTransform)holder.transform;
            rect.SetParent(building, false);
            rect.sizeDelta = Vector2.zero;
            var effect = holder.AddComponent<UIParticle>();
            effect.raycastTarget = false;
            effect.SetParticleSystemInstance(Object.Instantiate(prefab), true);
            ForgetDestroyed();
            Sources[effect] = prefab;
            return effect;
        }

        // Effects die with their scene; drop them so the lookup does not grow
        private static void ForgetDestroyed()
        {
            Destroyed.Clear();
            foreach (UIParticle effect in Sources.Keys)
            {
                if (effect == null)
                {
                    Destroyed.Add(effect);
                }
            }

            foreach (UIParticle effect in Destroyed)
            {
                Sources.Remove(effect);
            }
        }

        private static void Release(UIParticle effect)
        {
            if (effect == null || !Sources.TryGetValue(effect, out GameObject prefab))
            {
                return;
            }

            Pools[prefab].Release(effect);
        }

        // A finished effect stops and hides until it is needed again
        private static void Park(UIParticle effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.Stop();
            effect.gameObject.SetActive(false);
        }

        // More finished effects than the pool keeps are destroyed
        private static void Discard(UIParticle effect)
        {
            if (effect != null)
            {
                Sources.Remove(effect);
                Object.Destroy(effect.gameObject);
            }
        }
    }
}
