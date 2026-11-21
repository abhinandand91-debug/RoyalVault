using System.Collections;
using RoyalVault.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// The story's golden seal on the vault door, set in the console. As the reels land, every Vault Seal (the scatter)
    /// cracks it a little more: light leaks through the cracks, sparks fly and a pip lights. When enough seals land
    /// for free spins the seal shatters and the vault stays open, glowing blue, for the whole round; the next paid spin
    /// mends it. It only shows what the spin already decided, so it never changes the game's maths.
    /// </summary>
    public sealed class SealMeterView : MonoBehaviour
    {
        [SerializeField] private RectTransform seal;
        [SerializeField] private Image face;
        [SerializeField] private Image[] cracks = new Image[0];
        [Tooltip("Dark fracture lines under the glowing cracks, so they read on the bright gold")]
        [SerializeField] private Image[] fractures = new Image[0];
        [SerializeField] private Image[] pips = new Image[0];
        [SerializeField] private Image glow;
        [SerializeField] private Image magic;
        [SerializeField] private UiParticleEmitter sparks;
        [SerializeField] private Color restColour = new Color(0.55f, 0.5f, 0.45f, 1f);
        [SerializeField] private Color magicColour = new Color(0.35f, 0.75f, 1f, 1f);

        private int scatter = -1;
        private int needed = 3;
        private SpinOutcome outcome;
        private bool freeSpin;
        private bool open;
        private int lit;
        private float shake;
        private Coroutine routine;

        /// <summary>Seals counted this spin.</summary>
        public int Count => lit;
        /// <summary>True while the seal is broken and the vault is open (a free spin round).</summary>
        public bool IsOpen => open;

        public void Setup(RectTransform sealRect, Image faceImage, Image[] crackImages, Image[] pipImages, Image glowImage, Image magicImage,
                          UiParticleEmitter sparkEmitter)
        {
            seal = sealRect;
            face = faceImage;
            cracks = crackImages;
            pips = pipImages;
            glow = glowImage;
            magic = magicImage;
            sparks = sparkEmitter;
        }

        public void SetupFractures(Image[] fractureImages)
        {
            fractures = fractureImages;
        }

        private void ShowCracks(int stage)
        {
            for (int i = 0; i < cracks.Length; i++) cracks[i].enabled = i < stage;
            for (int i = 0; i < fractures.Length; i++) fractures[i].enabled = i < stage;
        }

        public void Bind(MachineDefinition machine, ThemeDefinition theme, MachineView machineView, SpinFlow flow, GameSession session)
        {
            scatter = machine.FirstIndexOfKind(SymbolKind.Scatter);
            gameObject.SetActive(scatter >= 0);
            if (scatter < 0) return;

            // the fewest seals that open the vault (award free spins)
            needed = int.MaxValue;
            foreach (ScatterAward award in machine.ScatterAwards)
                if (award.FreeSpins > 0) needed = Mathf.Min(needed, award.Count);
            if (needed == int.MaxValue) needed = 3;
            needed = Mathf.Clamp(needed, 1, pips.Length);
            for (int i = 0; i < pips.Length; i++) pips[i].gameObject.SetActive(i < needed);

            string id = machine.Symbols[scatter].Id;
            foreach (ThemeDefinition.SymbolArt art in theme.symbols)
                if (art.id == id) face.sprite = art.sprite;
            magicColour = theme.characterMagic;

            flow.SpinStarted += OnSpinStarted;
            machineView.ReelStopped += OnReelStopped;
            if (session.FreeSpins.Remaining > 0) ShowOpen(true); else Mend(true);
        }

        private void OnSpinStarted(SpinContext context)
        {
            outcome = context.Outcome;
            freeSpin = context.IsFreeSpin;
            if (!freeSpin && open) Mend(false);
            else if (!freeSpin) Mend(true);
        }

        private void OnReelStopped(int reel)
        {
            if (outcome == null) return;
            int found = 0;
            for (int row = 0; row < outcome.Grid.Rows; row++)
                if (outcome.Grid[reel, row] == scatter) found++;
            for (int i = 0; i < found; i++) Crack();
        }

        private void Crack()
        {
            if (open)
            {
                // a seal in the open vault: a flourish of blue sparks
                sparks.StartColor = new Color(0.8f, 0.95f, 1f, 1f);
                sparks.Burst(18, 1.2f);
                shake = 0.6f;
                return;
            }

            lit++;
            int stage = Mathf.Min(lit, cracks.Length);
            ShowCracks(stage);
            if (lit - 1 < pips.Length) pips[lit - 1].color = new Color(1f, 0.85f, 0.5f, 1f);
            sparks.StartColor = new Color(1f, 0.9f, 0.6f, 1f);
            sparks.Burst(14 + 10 * lit, 1f + 0.3f * lit);
            shake = 1f;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(lit >= needed ? Shatter() : Pulse(lit));
        }

        private IEnumerator Pulse(int stage)
        {
            // the seal wakes a little with each crack, but stays below full gold so the light in the cracks shows
            float brightness = Mathf.Lerp(0.55f, 0.85f, stage / (float)needed);
            Color from = face.color;
            Color to = new Color(brightness + 0.25f, brightness + 0.2f, brightness, 1f);
            yield return Tween.Run(0.12f, k => { seal.localScale = Vector3.one * (1f + 0.18f * k); face.color = Color.Lerp(from, Color.white * 1.3f, k); }, Ease.OutCubic);
            yield return Tween.Run(0.35f, k => { seal.localScale = Vector3.one * (1.18f - 0.18f * k); face.color = Color.Lerp(Color.white * 1.3f, to, k); }, Ease.OutCubic);
        }

        /// <summary>The seal bursts: a flash, the face breaks away and the vault's blue light pours out.</summary>
        private IEnumerator Shatter()
        {
            sparks.StartColor = new Color(1f, 0.95f, 0.8f, 1f);
            sparks.Burst(60, 2.4f);
            yield return Tween.Run(0.15f, k => { seal.localScale = Vector3.one * (1f + 0.35f * k); glow.color = new Color(1f, 0.9f, 0.6f, k); }, Ease.OutCubic);
            ShowOpen(false);
            yield return Tween.Run(0.5f, k =>
            {
                seal.localScale = Vector3.one * (1.35f - 0.35f * k);
                glow.color = new Color(1f, 0.9f, 0.6f, 1f - k);
            }, Ease.OutCubic);
            routine = null;
        }

        private void ShowOpen(bool instant)
        {
            open = true;
            face.color = new Color(1f, 1f, 1f, 0.35f);
            ShowCracks(cracks.Length);
            foreach (Image pip in pips) pip.color = new Color(magicColour.r, magicColour.g, magicColour.b, 1f);
            if (instant) seal.localScale = Vector3.one;
        }

        /// <summary>Back to a whole, dim seal with no seals counted.</summary>
        private void Mend(bool instant)
        {
            open = false;
            lit = 0;
            if (routine != null) { StopCoroutine(routine); routine = null; }
            ShowCracks(0);
            foreach (Image pip in pips) pip.color = new Color(1f, 1f, 1f, 0.18f);
            seal.localScale = Vector3.one;
            if (instant) face.color = restColour;
            else routine = StartCoroutine(Tween.Run(0.6f, k => face.color = Color.Lerp(new Color(1f, 1f, 1f, 0.35f), restColour, k), Ease.InOutSine));
        }

        private void Update()
        {
            float t = Time.time;
            // a slow breath of light behind the seal; the open vault glows blue
            float breath = 0.5f + 0.5f * Mathf.Sin(t * 1.7f);
            Color m = magicColour;
            magic.color = new Color(m.r, m.g, m.b, open ? 0.55f + 0.3f * breath : 0.08f * lit + 0.04f * breath);
            magic.rectTransform.localScale = Vector3.one * (1f + (open ? 0.12f : 0.05f) * breath);
            foreach (Image crack in cracks)
                if (crack.enabled) crack.color = open ? new Color(m.r + 0.3f, m.g + 0.15f, m.b, 0.9f) : new Color(1f, 0.85f, 0.5f, 0.75f + 0.25f * breath);

            if (shake > 0f)
            {
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 3f);
                seal.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 60f) * 4f * shake);
            }
            else seal.localRotation = Quaternion.identity;
        }
    }
}
