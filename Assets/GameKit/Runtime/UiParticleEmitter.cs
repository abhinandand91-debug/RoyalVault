using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace GameKit
{
    // Lightweight particles drawn inside a UI canvas as one mesh: one graphic, one draw, no objects per particle
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiParticleEmitter : MaskableGraphic
    {
        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Life;
            public float Size;
            public float Spin;
            public float Seed;
            public float Flip;
        }

        [Header("Look")]
        public Sprite Sprite;
        public Color StartColor = Color.white;
        public Color EndColor = new Color(1f, 1f, 1f, 0f);
        public Vector2 SizeRange = new Vector2(10f, 20f);
        [Tooltip("Size at the end of a particle's life, as a share of its start size")]
        public float EndSize = 0.3f;
        [Tooltip("Fade in at birth and out towards the end of life; off keeps particles solid, like coins")]
        public bool Fades = true;
        [Tooltip("Degrees per second each particle turns, picked between these")]
        public Vector2 SpinRange = new Vector2(-90f, 90f);
        [Tooltip("Turns per second around the vertical axis, like a tumbling coin (0 = flat)")]
        public Vector2 FlipRange = Vector2.zero;

        [Header("Emission")]
        [Tooltip("Particles per second (0 = bursts only)")]
        public float Rate = 10f;
        public Vector2 LifeRange = new Vector2(0.6f, 1.2f);
        [Tooltip("Particles start anywhere in this box around the emitter")]
        public Vector2 Area = Vector2.zero;
        [Tooltip("Where new particles start, relative to the emitter; move it to leave a trail behind a moving thing")]
        public Vector2 Origin = Vector2.zero;
        public int MaxParticles = 80;

        [Header("Motion")]
        public Vector2 VelocityMin = new Vector2(-10f, 40f);
        public Vector2 VelocityMax = new Vector2(10f, 100f);
        [Tooltip("Constant acceleration (positive y rises, like heat)")]
        public Vector2 Gravity = Vector2.zero;
        [Tooltip("Sideways wobble: amplitude in pixels per second")]
        public float Swirl;
        public float SwirlFrequency = 6f;
        public float Drag;

        private readonly List<Particle> live = new List<Particle>();
        private float pending;
        private float burstSpeed = 1f;
        private bool drawn;

        // Multiplies the continuous rate (a flare: set above 1 for a moment)
        public float Intensity { get; set; } = 1f;

        public Material Material
        {
            get
            {
                return material;
            }
            set
            {
                material = value;
            }
        }

        public override Texture mainTexture
        {
            get
            {
                return Sprite != null ? Sprite.texture : s_WhiteTexture;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            EmitDue(deltaTime);
            AgeParticles(deltaTime);
            RedrawWhileMoving();
        }

        // New particles at the emitter's rate
        private void EmitDue(float deltaTime)
        {
            pending += Rate * Intensity * deltaTime;
            while (pending >= 1f)
            {
                pending -= 1f;
                Spawn(false);
            }
        }

        // Each particle grows older and moves; the dead are dropped
        private void AgeParticles(float deltaTime)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Particle particle = live[i];
                particle.Age += deltaTime;
                if (particle.Age >= particle.Life)
                {
                    live.RemoveAt(i);
                    continue;
                }

                Move(ref particle, deltaTime);
                live[i] = particle;
            }
        }

        // Redraw while particles move, and once more after the last one dies
        private void RedrawWhileMoving()
        {
            if (live.Count > 0 || drawn)
            {
                drawn = live.Count > 0;
                SetVerticesDirty();
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
            live.Clear();
            pending = 0f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Vector4 uv = Sprite != null ? DataUtility.GetOuterUV(Sprite) : new Vector4(0f, 0f, 1f, 1f);
            for (int i = 0; i < live.Count; i++)
            {
                AddQuad(vertices, live[i], uv);
            }
        }

        private void Move(ref Particle particle, float deltaTime)
        {
            particle.Velocity += Gravity * deltaTime;
            if (Drag > 0f)
            {
                particle.Velocity *= Mathf.Max(0f, 1f - Drag * deltaTime);
            }

            float wobble = Swirl * Mathf.Sin(particle.Age * SwirlFrequency + particle.Seed * 6.28f);
            particle.Position += (particle.Velocity + new Vector2(wobble, 0f)) * deltaTime;
        }

        private void AddQuad(VertexHelper vertices, Particle particle, Vector4 uv)
        {
            float t = particle.Age / particle.Life;
            float fade = Fades ? Mathf.Min(1f, t * 8f) * (1f - t * t) : 1f;
            Color tint = Color.Lerp(StartColor, EndColor, t) * color;
            tint.a *= fade;

            float half = particle.Size * Mathf.Lerp(1f, EndSize, t) * 0.5f;
            float angle = particle.Spin * particle.Age * Mathf.Deg2Rad;
            var right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * half;
            var up = new Vector2(-right.y, right.x);
            if (particle.Flip > 0f)
            {
                right *= Mathf.Abs(Mathf.Cos(particle.Age * particle.Flip)) * 0.8f + 0.2f;
            }

            Vector2 centre = particle.Position;

            int start = vertices.currentVertCount;
            vertices.AddVert(centre - right - up, tint, new Vector4(uv.x, uv.y));
            vertices.AddVert(centre - right + up, tint, new Vector4(uv.x, uv.w));
            vertices.AddVert(centre + right + up, tint, new Vector4(uv.z, uv.w));
            vertices.AddVert(centre + right - up, tint, new Vector4(uv.z, uv.y));
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start + 2, start + 3, start);
        }

        private void Spawn(bool burst)
        {
            if (live.Count >= MaxParticles)
            {
                return;
            }

            var particle = new Particle
            {
                Position = Origin + new Vector2(Random.Range(-0.5f, 0.5f) * Area.x, Random.Range(-0.5f, 0.5f) * Area.y),
                Velocity = StartVelocity(burst),
                Life = Random.Range(LifeRange.x, LifeRange.y) * (burst ? 1.2f : 1f),
                Size = Random.Range(SizeRange.x, SizeRange.y),
                Spin = Random.Range(SpinRange.x, SpinRange.y),
                Seed = Random.value,
                Flip = Random.Range(FlipRange.x, FlipRange.y),
            };
            live.Add(particle);
        }

        // Bursts spray in every direction from the centre
        private Vector2 StartVelocity(bool burst)
        {
            var velocity = new Vector2(Random.Range(VelocityMin.x, VelocityMax.x), Random.Range(VelocityMin.y, VelocityMax.y));
            if (!burst)
            {
                return velocity;
            }

            Vector2 direction = Random.insideUnitCircle.normalized;
            return direction * velocity.magnitude * burstSpeed + new Vector2(0f, Mathf.Abs(velocity.y) * 0.4f);
        }
    }
}
