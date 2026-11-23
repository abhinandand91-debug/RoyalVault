using System.Collections;
using System.Collections.Generic;
using GameKit;
using PrimeTween;
using SlotKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Emerald energy runs from the bonus symbols into a crest, and the banner announces the round
    public sealed class BonusActivationView : DialogView
    {
        private const float LinkStagger = 0.09f;

        [SerializeField]
        private RectTransform crest;
        [SerializeField]
        private RectTransform crestRing;
        [SerializeField]
        private CanvasGroup banner;
        [SerializeField]
        private Image[] links = new Image[0];

        private readonly TweenGroup tweens = new TweenGroup(4);
        private MachineView machine;
        private List<GridPosition> cells;

        public void Setup(RectTransform crestRect, RectTransform ringRect, CanvasGroup bannerGroup, Image[] linkImages)
        {
            crest = crestRect;
            crestRing = ringRect;
            banner = bannerGroup;
            links = linkImages;
        }

        public void Bind(MachineView machineView)
        {
            machine = machineView;
        }

        public IEnumerator Play(List<GridPosition> bonusCells)
        {
            cells = bonusCells;
            yield return Present();
        }

        protected override void OnOpening()
        {
            crest.localScale = Vector3.zero;
            banner.alpha = 0f;
            for (int i = 0; i < links.Length; i++)
            {
                PlaceLink(i);
            }
        }

        // Stretches a link from the crest to its bonus symbol
        private void PlaceLink(int index)
        {
            bool used = index < cells.Count;
            RectTransform link = links[index].rectTransform;
            link.gameObject.SetActive(used);
            if (!used)
            {
                return;
            }

            Vector2 from = crest.anchoredPosition;
            Vector2 span = machine.CellCentre(cells[index].Reel, cells[index].Row) - from;
            link.anchoredPosition = from;
            link.sizeDelta = new Vector2(span.magnitude, link.sizeDelta.y);
            link.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg);
            link.localScale = new Vector3(0f, 1f, 1f);
        }

        // The links reach the symbols, the crest pops up, the banner lands and it all holds a moment
        protected override IEnumerator Content()
        {
            machine.HighlightCells(cells, 2.6f);
            int count = Mathf.Min(cells.Count, links.Length);
            for (int i = 0; i < count; i++)
            {
                tweens.Add(Tween.ScaleX(links[i].rectTransform, 0f, 1f, 0.4f, Ease.OutCubic, startDelay: i * LinkStagger));
                tweens.Add(UiMotion.Pulse(links[i], 0.55f, 1f, 0.8f));
            }

            yield return Waits.Seconds(0.4f + count * LinkStagger);
            tweens.Add(UiMotion.PopIn(crest, 0f, 0.5f));
            tweens.Add(UiMotion.Spin(crestRing, 70f));
            yield return Waits.Seconds(0.3f);
            tweens.Add(UiMotion.FadeIn(banner, 0.3f));
            tweens.Add(UiMotion.PopIn(banner.transform, 0.7f, 0.4f));
            yield return Waits.Seconds(1.7f);
        }

        protected override void OnClosed()
        {
            tweens.StopAll();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            tweens.StopAll();
        }
    }
}
