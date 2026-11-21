using Coffee.UIExtensions;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Plays a ready-made particle effect (a Unity ParticleSystem prefab, e.g
    public static class UiVfx
    {
        public static GameObject Play(GameObject prefab, Transform parent, Vector2 position, float scale, float seconds)
        {
            if (prefab == null || parent == null) return null;
            var holder = new GameObject(prefab.name + " (UI)", typeof(RectTransform));
            var rect = (RectTransform)holder.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.zero;

            var ui = holder.AddComponent<UIParticle>();
            ui.raycastTarget = false;
            ui.scale = scale;
            ui.SetParticleSystemInstance(Object.Instantiate(prefab), true);
            ui.Play();
            Object.Destroy(holder, seconds);
            return holder;
        }
    }
}
