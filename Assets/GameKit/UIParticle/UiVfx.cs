using System.Collections.Generic;
using Coffee.UIExtensions;
using PrimeTween;
using UnityEngine;

namespace GameKit
{
    // Plays a ready-made ParticleSystem prefab inside a UI canvas; finished effects are kept and reused, not destroyed
    public static class UiVfx
    {
        private static readonly Dictionary<GameObject, Stack<UIParticle>> Pools = new Dictionary<GameObject, Stack<UIParticle>>();
        private static readonly Dictionary<UIParticle, GameObject> Sources = new Dictionary<UIParticle, GameObject>();
        private static readonly List<UIParticle> Destroyed = new List<UIParticle>();

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

            UIParticle effect = Take(prefab, parent);
            effect.gameObject.SetActive(false);
            Pools[prefab].Push(effect);
        }

        // Builds the effect once for the first parent that asks; it moves with later requests
        private static UIParticle Take(GameObject prefab, Transform parent)
        {
            if (!Pools.TryGetValue(prefab, out Stack<UIParticle> pool))
            {
                pool = new Stack<UIParticle>();
                Pools[prefab] = pool;
            }

            while (pool.Count > 0)
            {
                UIParticle pooled = pool.Pop();
                if (pooled != null)
                {
                    pooled.transform.SetParent(parent, false);
                    return pooled;
                }
            }

            return Create(prefab, parent);
        }

        private static UIParticle Create(GameObject prefab, Transform parent)
        {
            var holder = new GameObject(prefab.name + " (UI)", typeof(RectTransform));
            var rect = (RectTransform)holder.transform;
            rect.SetParent(parent, false);
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
            effect.Stop();
            effect.gameObject.SetActive(false);
            Pools[Sources[effect]].Push(effect);
        }
    }
}
