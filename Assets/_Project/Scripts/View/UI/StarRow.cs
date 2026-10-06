using Match3.Data;
using Match3.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.View.UI
{
    /// <summary>
    /// A row of three stars: a dark slot for each, and a gold star on top of the slots that were earned.
    /// The level map shows it small under each node; the end screen shows it big and animates the gold stars in.
    /// </summary>
    public sealed class StarRow
    {
        private static readonly Color SlotColor = new Color(0.08f, 0.1f, 0.2f, 0.85f);
        private static readonly Color EarnedColor = new Color(1f, 0.82f, 0.25f, 1f);

        private readonly Image[] _earned = new Image[StarThresholds.MaxStars];

        private StarRow(RectTransform root)
        {
            Root = root;
        }

        public RectTransform Root { get; }

        /// <summary>
        /// starSize and spacing are in canvas units. The middle star sits middleLift higher than the outer two.
        /// </summary>
        public static StarRow Create(Transform parent, string name, UiStyle style, float starSize, float spacing, float middleLift)
        {
            RectTransform root = UiFactory.CreateRect(parent, name);
            root.sizeDelta = new Vector2((StarThresholds.MaxStars - 1) * spacing + starSize, starSize + middleLift);
            StarRow row = new StarRow(root);

            float middle = (StarThresholds.MaxStars - 1) / 2f;
            for (int i = 0; i < StarThresholds.MaxStars; i++)
            {
                float fromMiddle = Mathf.Abs(i - middle) / middle; // 0 for the middle star, 1 for the outer ones
                Vector2 offset = new Vector2((i - middle) * spacing, (1f - 2f * fromMiddle) * middleLift / 2f);

                Image slot = UiFactory.CreateImage(root, "Slot " + (i + 1), SlotColor, style.StarSprite);
                slot.preserveAspect = true;
                UiFactory.Place(slot.rectTransform, new Vector2(0.5f, 0.5f), offset, new Vector2(starSize, starSize));

                Image earned = UiFactory.CreateImage(slot.transform, "Earned", EarnedColor, style.StarSprite);
                earned.preserveAspect = true;
                UiFactory.Stretch(earned.rectTransform);
                row._earned[i] = earned;
            }

            return row;
        }

        /// <summary>Shows exactly this many gold stars (0 to 3), at once.</summary>
        public void SetEarned(int count)
        {
            for (int i = 0; i < _earned.Length; i++)
            {
                _earned[i].gameObject.SetActive(i < count);
                _earned[i].transform.localScale = Vector3.one;
            }
        }

        /// <summary>The gold star of slot i, so the end screen can animate it.</summary>
        public Transform GetEarnedStar(int index) => _earned[index].transform;
    }
}
