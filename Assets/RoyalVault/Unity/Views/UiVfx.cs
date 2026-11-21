using Coffee.UIExtensions;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>
    /// Plays a ready-made particle effect (a Unity ParticleSystem prefab, e.g. from an Asset Store VFX pack) inside the
    /// UI canvas, where particle systems do not draw on their own. A <see cref="UIParticle"/> (the MIT UIParticle
    /// package) draws it as part of the UI, in the right order with everything else; the effect removes itself after
    /// <c>seconds</c>. Effects are optional: a missing prefab (a pack not in a public checkout) plays nothing.
    /// </summary>
    public static class UiVfx
    {
        /// <param name="prefab">The effect; null does nothing.</param>
        /// <param name="parent">The UI layer to draw it in.</param>
        /// <param name="position">Where, in the parent's local space (canvas pixels).</param>
        /// <param name="scale">Effects are authored in metres for 3D scenes: this turns them into canvas pixels.</param>
        /// <param name="seconds">When to remove it.</param>
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
