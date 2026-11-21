using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit
{
    // Lightweight particles drawn inside a UI canvas
    public sealed class UiParticleEmitter : MonoBehaviour
    {
        private struct Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Life;
            public float Size;
            public float Spin;
            public float Seed;
            public bool Burst;
        }

        [Header("Look")]
        public Sprite Sprite;
        public Material Material;
        public Color StartColor = Color.white;
        public Color EndColor = new Color(1f, 1f, 1f, 0f);
        public Vector2 SizeRange = new Vector2(10f, 20f);
        [Tooltip("Size at the end of a particle's life, as a share of its start size")] public float EndSize = 0.3f;

        [Header("Emission")]
        [Tooltip("Particles per second (0 = bursts only)")] public float Rate = 10f;
        public Vector2 LifeRange = new Vector2(0.6f, 1.2f);
        [Tooltip("Particles start anywhere in this box around the emitter")] public Vector2 Area = Vector2.zero;
        [Tooltip("Where new particles start, relative to the emitter; move it to leave a trail behind a moving thing")]
        public Vector2 Origin = Vector2.zero;
        public int MaxParticles = 80;

        [Header("Motion")]
        public Vector2 VelocityMin = new Vector2(-10f, 40f);
        public Vector2 VelocityMax = new Vector2(10f, 100f);
        [Tooltip("Constant acceleration (positive y rises, like heat)")] public Vector2 Gravity = Vector2.zero;
        [Tooltip("Sideways wobble: amplitude in pixels per second")] public float Swirl;
        public float SwirlFrequency = 6f;
        public float Drag;

        private readonly List<Particle> live = new List<Particle>();
        private readonly Stack<Particle> pool = new Stack<Particle>();
        private float pending;
        private float burstSpeed = 1f;

        // Multiplies the continuous rate (a flare: set above 1 for a moment)
        public float Intensity { get; set; } = 1f;

        private void Update()
        {
            float dt = Time.deltaTime;
            pending += Rate * Intensity * dt;
            while (pending >= 1f)
            {
                pending -= 1f;
                Spawn(false);
            }

            for (int i = live.Count - 1; i >= 0; i--)
            {
                Particle p = live[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    p.Rect.gameObject.SetActive(false);
                    pool.Push(p);
                    live.RemoveAt(i);
                    continue;
                }
                p.Velocity += Gravity * dt;
                if (Drag > 0f)
                {
                    p.Velocity *= Mathf.Max(0f, 1f - Drag * dt);
                }

                float wobble = Swirl * Mathf.Sin(p.Age * SwirlFrequency + p.Seed * 6.28f);
                p.Position += (p.Velocity + new Vector2(wobble, 0f)) * dt;

                float t = p.Age / p.Life;
                float fade = Mathf.Min(1f, t * 8f) * (1f - t * t);
                Color c = Color.Lerp(StartColor, EndColor, t);
                c.a *= fade;
                p.Image.color = c;
                p.Rect.anchoredPosition = p.Position;
                p.Rect.sizeDelta = Vector2.one * p.Size * Mathf.Lerp(1f, EndSize, t);
                p.Rect.localRotation = Quaternion.Euler(0f, 0f, p.Spin * p.Age);
                live[i] = p;
            }
        }

        // Fires count particles at once, speed times faster than usual
        public void Burst(int count, float speed = 2.5f)
        {
            burstSpeed = speed;
            for (int i = 0; i < count; i++)
            {
                Spawn(true);
            }

            burstSpeed = 1f;
        }

        // Removes every live particle
        public void Clear()
        {
            foreach (Particle p in live)
            {
                p.Rect.gameObject.SetActive(false);
                pool.Push(p);
            }
            live.Clear();
        }

        private void Spawn(bool burst)
        {
            if (live.Count >= MaxParticles)
            {
                return;
            }

            Particle p = pool.Count > 0 ? pool.Pop() : Create();
            p.Rect.gameObject.SetActive(true);
            p.Rect.SetAsLastSibling();
            p.Position = Origin + new Vector2(Random.Range(-0.5f, 0.5f) * Area.x, Random.Range(-0.5f, 0.5f) * Area.y);
            var v = new Vector2(Random.Range(VelocityMin.x, VelocityMax.x), Random.Range(VelocityMin.y, VelocityMax.y));
            if (burst)
            {
                // bursts spray in every direction from the centre
                Vector2 dir = Random.insideUnitCircle.normalized;
                v = dir * v.magnitude * burstSpeed + new Vector2(0f, Mathf.Abs(v.y) * 0.4f);
            }
            p.Velocity = v;
            p.Age = 0f;
            p.Life = Random.Range(LifeRange.x, LifeRange.y) * (burst ? 1.2f : 1f);
            p.Size = Random.Range(SizeRange.x, SizeRange.y);
            p.Spin = Random.Range(-90f, 90f);
            p.Seed = Random.value;
            p.Burst = burst;
            live.Add(p);
        }

        private Particle Create()
        {
            Image image = UiFactory.Image("Particle", transform, Sprite, Vector2.zero, Vector2.one * SizeRange.y);
            image.color = Color.clear;
            if (Material != null)
            {
                image.material = Material;
            }

            return new Particle { Rect = image.rectTransform, Image = image };
        }
    }
}
