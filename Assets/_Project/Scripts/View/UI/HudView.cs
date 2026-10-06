using System;
using System.Collections.Generic;
using DG.Tweening;
using Match3.Data;
using Match3.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.View.UI
{
    /// <summary>
    /// The heads-up display: which level, how many moves are left, and one icon with a counter per goal.
    /// It only shows what it is told. LevelController feeds it from the MoveCounter and GoalTracker events,
    /// so the HUD never reads (or changes) game state itself.
    /// The goal icons use the same colors as the tiles (TileVisuals), so "12 next to a red square" means 12 red tiles.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.14f, 0.85f);
        private static readonly Color NormalTextColor = Color.white;
        private static readonly Color LowMovesColor = new Color(1f, 0.45f, 0.4f);
        private static readonly Color ComboColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color DoneColor = new Color(1f, 1f, 1f, 0.45f);

        private static readonly Color CardColor = new Color(0.17f, 0.22f, 0.4f, 1f);
        private static readonly Color ChipColor = new Color(0.1f, 0.14f, 0.28f, 0.7f);
        private static readonly Color CheckColor = new Color(0.55f, 1f, 0.6f, 1f);


        private const int LowMovesThreshold = 3;
        private const float PanelHeight = 380f;
        private const float NotchFillHeight = 300f; // more than any phone's notch, in canvas units

        private TileVisuals _visuals;
        private UiStyle _style;
        private FeelSettings _feel;
        private TMP_Text _comboText;
        private Tween _comboTween;
        private TMP_Text _levelText;
        private TMP_Text _movesText;
        private RectTransform _goalRow;
        private TMP_Text _messageText;
        private Tween _messageTween;
        private readonly List<TMP_Text> _goalTexts = new List<TMP_Text>();
        private readonly List<Image> _goalIcons = new List<Image>();
        private readonly List<Image> _goalChecks = new List<Image>(); // null entry = no checkmark art, the text says "Done" instead
        private readonly List<int> _goalShown = new List<int>();      // the number each goal shows now, to punch only on a real change
        private int _movesShown;
        private StarRow _starRow;           // the stars you would get if you won right now
        private StarThresholds _thresholds;
        private int _starsShown;

        /// <summary>The player tapped the pause button.</summary>
        public event Action PauseClicked;

        /// <summary>Builds the HUD (a canvas with all its parts) as a new object. The visuals give the goal icons their colors.</summary>
        public static HudView Create(TileVisuals visuals, UiStyle style, FeelSettings feel)
        {
            HudView hud = new GameObject("HUD").AddComponent<HudView>();
            hud.Build(visuals, style, feel);
            return hud;
        }

        private void Build(TileVisuals visuals, UiStyle style, FeelSettings feel)
        {
            _visuals = visuals;
            _style = style;
            _feel = feel;
            UiFactory.EnsureEventSystem(); // the pause button needs one
            Canvas canvas = UiFactory.CreateCanvas(transform, "HUD Canvas", 10);

            // Everything of the HUD lives inside the safe area, so a notch or camera hole never covers it.
            RectTransform safeArea = UiFactory.CreateSafeArea(canvas.transform);

            // A dark strip along the top of the safe area.
            Image panel = UiFactory.CreateImage(safeArea, "Panel", PanelColor);
            panel.rectTransform.anchorMin = new Vector2(0f, 1f);
            panel.rectTransform.anchorMax = new Vector2(1f, 1f);
            panel.rectTransform.pivot = new Vector2(0.5f, 1f);
            panel.rectTransform.sizeDelta = new Vector2(0f, PanelHeight);
            panel.rectTransform.anchoredPosition = Vector2.zero;

            // The same color continues upwards, past the top of the safe area, to fill the notch.
            // It starts exactly where the panel starts (pivot at its bottom edge) and is simply cut off by the screen edge.
            Image notchFill = UiFactory.CreateImage(panel.transform, "Notch Fill", PanelColor);
            notchFill.rectTransform.anchorMin = new Vector2(0f, 1f);
            notchFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            notchFill.rectTransform.pivot = new Vector2(0.5f, 0f);
            notchFill.rectTransform.sizeDelta = new Vector2(0f, NotchFillHeight);
            notchFill.rectTransform.anchoredPosition = Vector2.zero;

            // A card behind the moves counter, so the most important number on the screen stands out.
            Image movesCard = UiFactory.CreateImage(panel.transform, "Moves Card", CardColor, _style.PanelSprite);
            UiFactory.Place(movesCard.rectTransform, new Vector2(1f, 1f), new Vector2(-175f, -100f), new Vector2(330f, 190f));

            _levelText = UiFactory.CreateText(panel.transform, "Level", "Level 1", 56f, NormalTextColor, TextAlignmentOptions.Left, _style.Font);
            UiFactory.Place(_levelText.rectTransform, new Vector2(0f, 1f), new Vector2(190f, -60f), new Vector2(300f, 80f)); // top row: level on the left, moves on the right

            // Under the level number: the stars you would earn if you won now. They drop as moves run out,
            // so it is always visible what the stars are for: winning with moves left.
            _starRow = StarRow.Create(panel.transform, "Stars", _style, 44f, 50f, 8f);
            UiFactory.Place(_starRow.Root, new Vector2(0f, 1f), new Vector2(190f, -150f), _starRow.Root.sizeDelta);

            TMP_Text movesLabel = UiFactory.CreateText(panel.transform, "MovesLabel", "MOVES", 36f, DoneColor, TextAlignmentOptions.Center, _style.Font);
            UiFactory.Place(movesLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-175f, -40f), new Vector2(300f, 50f));

            _movesText = UiFactory.CreateText(panel.transform, "Moves", "0", 110f, NormalTextColor, TextAlignmentOptions.Center, _style.Font);
            UiFactory.Place(_movesText.rectTransform, new Vector2(1f, 1f), new Vector2(-175f, -125f), new Vector2(300f, 130f));

            // The pause button sits in the top row between the level number and the moves counter.
            Vector2 pauseSize = new Vector2(120f, 120f);
            Button pause = UiFactory.CreateButton(panel.transform, "Pause", "", pauseSize, _style.SecondaryButtonSprite, _style.Font, _feel);
            UiFactory.Place((RectTransform)pause.transform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), pauseSize);
            if (_style.PauseIconSprite != null)
            {
                Image icon = UiFactory.CreateImage(pause.transform, "Icon", Color.white, _style.PauseIconSprite);
                icon.preserveAspect = true;
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
            }
            else
            {
                pause.GetComponentInChildren<TMP_Text>().text = "II"; // no icon art assigned: two letters still say "pause"
            }

            pause.onClick.AddListener(() => PauseClicked?.Invoke());

            // The goals sit in a centered row at the bottom of the strip, with a clear gap under the moves counter (the top row ends about 190 px from the top, this row starts about 230 px from the top). Their positions are worked out in Show().
            _goalRow = UiFactory.CreateRect(panel.transform, "Goals");
            UiFactory.Place(_goalRow, new Vector2(0.5f, 0f), new Vector2(0f, 95f), new Vector2(1000f, 110f));

            // A short message under the strip ("No moves left. Shuffling!"). Invisible until ShowMessage is called.
            _messageText = UiFactory.CreateText(safeArea, "Message", "", 56f, NormalTextColor, TextAlignmentOptions.Center, _style.Font);
            UiFactory.Place(_messageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -(PanelHeight + 70f)), new Vector2(1000f, 90f));
            SetMessageAlpha(0f);

            // The combo text: big, over the middle of the board. Hidden until ShowCombo is called.
            _comboText = UiFactory.CreateText(safeArea, "Combo", "", 130f, ComboColor, TextAlignmentOptions.Center, _style.Font);
            UiFactory.Place(_comboText.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(1000f, 180f));
            _comboText.gameObject.SetActive(false);
        }


        /// <summary>Starts showing a level: its number, the move limit and one counter per goal.</summary>
        public void Show(int levelNumber, int moveLimit, IReadOnlyList<GoalDefinition> goals, StarThresholds thresholds)
        {
            _levelText.text = "Level " + levelNumber;
            _messageTween?.Kill();
            SetMessageAlpha(0f);
            _comboTween?.Kill();
            _comboText.gameObject.SetActive(false);
            _thresholds = thresholds;
            _starsShown = -1;
            ShowMoves(moveLimit, false);

            // Remove the previous level's goal items, then make one per goal.
            for (int i = _goalRow.childCount - 1; i >= 0; i--)
            {
                Destroy(_goalRow.GetChild(i).gameObject);
            }

            _goalTexts.Clear();
            _goalIcons.Clear();
            _goalChecks.Clear();
            _goalShown.Clear();

            const float itemWidth = 240f; // icon (80) + gap + number, with room for 4 goals across 1080
            float firstX = -(goals.Count - 1) * itemWidth / 2f;

            for (int i = 0; i < goals.Count; i++)
            {
                RectTransform item = UiFactory.CreateRect(_goalRow, "Goal " + (i + 1));
                UiFactory.Place(item, new Vector2(0.5f, 0.5f), new Vector2(firstX + i * itemWidth, 0f), new Vector2(itemWidth - 20f, 100f)); // 20 px gap between chips

                // A dark rounded chip behind the icon and the number.
                Image chip = UiFactory.CreateImage(item, "Chip", ChipColor, _style.SlotSprite);
                UiFactory.Stretch(chip.rectTransform);

                Image icon = UiFactory.CreateImage(item, "Icon", TileArt.GetTint(_visuals, goals[i].color), TileArt.GetSprite(_visuals, goals[i].color));
                icon.preserveAspect = true; // gems are not all square
                UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(45f, 0f), new Vector2(80f, 80f));

                TMP_Text count = UiFactory.CreateText(item, "Count", goals[i].count.ToString(), 60f, NormalTextColor, TextAlignmentOptions.Left, _style.Font);
                UiFactory.Place(count.rectTransform, new Vector2(0f, 0.5f), new Vector2(180f, 0f), new Vector2(140f, 90f)); // the text is left-aligned, so it starts at x = 110: right of the icon (which ends at x = 85)

                // The checkmark takes the number's place when the goal is complete. Hidden until then.
                Image check = null;
                if (_style.CheckmarkSprite != null)
                {
                    check = UiFactory.CreateImage(item, "Check", CheckColor, _style.CheckmarkSprite);
                    check.preserveAspect = true;
                    UiFactory.Place(check.rectTransform, new Vector2(0f, 0.5f), new Vector2(165f, 0f), new Vector2(72f, 72f));
                    check.gameObject.SetActive(false);
                }

                _goalIcons.Add(icon);
                _goalTexts.Add(count);
                _goalChecks.Add(check);
                _goalShown.Add(goals[i].count);
            }
        }

        /// <summary>Shows a short message under the strip: fades in, stays for the given time, fades out. A new message replaces the old one.</summary>
        public void ShowMessage(string message, float seconds)
        {
            _messageTween?.Kill();
            _messageText.text = message;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOVirtual.Float(0f, 1f, _feel.MessageFadeInSeconds, SetMessageAlpha));
            sequence.AppendInterval(seconds);
            sequence.Append(DOVirtual.Float(1f, 0f, _feel.MessageFadeOutSeconds, SetMessageAlpha));
            sequence.SetLink(gameObject);
            _messageTween = sequence;
        }

        /// <summary>
        /// "Combo x3!": pops up big, stays a moment, fades out. A new combo replaces the one on screen.
        /// StepPlayer raises the event, LevelController passes it here; the wave number is the combo number.
        /// </summary>
        public void ShowCombo(int combo)
        {
            _comboTween?.Kill();

            _comboText.text = "Combo x" + combo + "!";
            _comboText.gameObject.SetActive(true);
            _comboText.transform.localScale = Vector3.one * 0.3f;
            SetComboAlpha(1f);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_comboText.transform.DOScale(1f, _feel.ComboPopSeconds).SetEase(Ease.OutBack));
            sequence.AppendInterval(_feel.ComboHoldSeconds);
            sequence.Append(DOVirtual.Float(1f, 0f, _feel.ComboFadeOutSeconds, SetComboAlpha));
            sequence.OnComplete(HideCombo);
            sequence.SetLink(gameObject);
            _comboTween = sequence;
        }

        private void SetComboAlpha(float alpha)
        {
            Color color = _comboText.color;
            color.a = alpha;
            _comboText.color = color;
        }

        private void HideCombo() => _comboText.gameObject.SetActive(false);

        private void SetMessageAlpha(float alpha)
        {
            Color color = _messageText.color;
            color.a = alpha;
            _messageText.color = color;
        }

        /// <summary>Shows the moves left. The number punches (a quick bounce) every time it changes.</summary>
        public void SetMoves(int movesLeft) => ShowMoves(movesLeft, true);

        private void ShowMoves(int movesLeft, bool punch)
        {
            bool changed = movesLeft != _movesShown;
            _movesShown = movesLeft;

            _movesText.text = movesLeft.ToString();
            _movesText.color = movesLeft <= LowMovesThreshold ? LowMovesColor : NormalTextColor;

            if (punch && changed) Punch(_movesText.transform);

            // Stars if the level were won with this many moves left. A lost star goes dark and the row shakes once.
            int stars = _thresholds.GetStars(movesLeft);
            if (stars != _starsShown)
            {
                bool lostAStar = punch && stars < _starsShown;
                _starsShown = stars;
                _starRow.SetEarned(stars);
                if (lostAStar) Punch(_starRow.Root);
            }
        }

        /// <summary>
        /// Shows how many tiles a goal still needs. The number punches when it changes.
        /// When the goal is complete the number is replaced by a checkmark that pops in.
        /// </summary>
        public void SetGoalRemaining(int goalIndex, int remaining)
        {
            bool done = remaining <= 0;
            int shown = done ? 0 : remaining;
            if (shown == _goalShown[goalIndex]) return; // nothing new to show (e.g. a finished goal being counted past zero)
            _goalShown[goalIndex] = shown;

            TMP_Text count = _goalTexts[goalIndex];
            Image check = _goalChecks[goalIndex];

            if (!done)
            {
                count.text = remaining.ToString();
                Punch(count.transform);
                return;
            }

            // Goal complete: dim the icon and swap the number for the checkmark.
            Color iconColor = _goalIcons[goalIndex].color;
            iconColor.a = 0.45f;
            _goalIcons[goalIndex].color = iconColor;

            if (check == null)
            {
                // No checkmark art assigned in the UiStyle: fall back to the word.
                count.text = "Done";
                count.fontSize = 44;
                count.color = DoneColor;
                Punch(count.transform);
                return;
            }

            count.gameObject.SetActive(false);
            check.gameObject.SetActive(true);
            check.transform.DOKill();
            check.transform.localScale = Vector3.zero;
            check.transform.DOScale(1f, _feel.GoalCheckPopSeconds).SetEase(Ease.OutBack).SetLink(check.gameObject);
        }

        // A quick bounce: finish any bounce still running (so the scale is back to normal), then start a new one.
        // SetLink kills the tween when the object is destroyed (the next level rebuilds the goal items).
        private void Punch(Transform target)
        {
            target.DOKill(true);
            target.DOPunchScale(Vector3.one * _feel.HudPunchStrength, _feel.HudPunchSeconds, _feel.HudPunchVibrato, _feel.HudPunchElasticity).SetLink(target.gameObject);
        }
    }
}
