using GameKit;
using PrimeTween;
using SlotKit;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // The jackpot plaques: the top tier in the centre, the next ones left and right, values counting live
    public sealed class JackpotPanelView : MonoBehaviour
    {
        [SerializeField, Tooltip("Centre, left, right")]
        private Image[] plaques = new Image[0];

        public RectTransform TopPlaque => plaques.Length > 0 ? plaques[0].rectTransform : null;
        [SerializeField]
        private TextMeshProUGUI[] names = new TextMeshProUGUI[0];
        [SerializeField]
        private CountingText[] values = new CountingText[0];

        private JackpotPool pool;
        // plaque slot for each tier, -1 when the tier has no plaque
        private int[] slotOfTier = new int[0];

        public void Setup(Image[] plaqueImages, TextMeshProUGUI[] nameLabels, CountingText[] valueLabels)
        {
            plaques = plaqueImages;
            names = nameLabels;
            values = valueLabels;
        }

        public void Bind(JackpotPool jackpots)
        {
            pool = jackpots;
            slotOfTier = new int[pool.Count];
            for (int slot = 0; slot < plaques.Length; slot++)
            {
                int tier = pool.Count - 1 - slot; // the largest tier goes in the centre
                bool used = tier >= 0;
                plaques[slot].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                slotOfTier[tier] = slot;
                names[slot].text = pool.Tier(tier).Name;
                values[slot].Set(pool.Value(tier));
            }
            for (int tier = 0; tier < pool.Count; tier++)
            {
                if (pool.Count - 1 - tier >= plaques.Length)
                {
                    slotOfTier[tier] = -1;
                }
            }

            pool.Changed += OnChanged;
        }

        private void OnDestroy()
        {
            if (pool != null)
            {
                pool.Changed -= OnChanged;
            }
        }

        private void OnChanged(int tier)
        {
            int slot = slotOfTier[tier];
            if (slot >= 0)
            {
                values[slot].Set(pool.Value(tier), 0.6f);
            }
        }

        // Flashes the plaque of a tier that was just won
        public void Celebrate(int tier)
        {
            if (tier < 0 || tier >= slotOfTier.Length || slotOfTier[tier] < 0)
            {
                return;
            }

            Transform plaque = plaques[slotOfTier[tier]].transform;
            Tween.StopAll(plaque);
            Tween.Scale(plaque, 1f, 1.08f, 0.18f, Ease.OutCubic, cycles: 8, cycleMode: CycleMode.Yoyo);
        }
    }
}
