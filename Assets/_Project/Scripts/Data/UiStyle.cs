using TMPro;
using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// How the user interface looks: the font and the sprites for panels, buttons and icons.
    /// The HUD and the end screen are built in code, and they take everything they draw from here,
    /// so restyling the UI means swapping assets in this one object, not editing code.
    /// Create one with: right-click in the Project window, Create > Match3 > Ui Style.
    /// </summary>
    [CreateAssetMenu(fileName = "UiStyle", menuName = "Match3/Ui Style")]
    public sealed class UiStyle : ScriptableObject
    {
        [Header("Text")]
        [Tooltip("A TextMeshPro SDF font asset (Fredoka-Bold SDF).")]
        [SerializeField] private TMP_FontAsset font;

        [Header("Panels (9-slice sprites)")]
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite slotSprite;

        [Header("Buttons (9-slice sprites)")]
        [Tooltip("The main action: Next Level / Play Again.")]
        [SerializeField] private Sprite primaryButtonSprite;

        [Tooltip("The other action: Retry.")]
        [SerializeField] private Sprite secondaryButtonSprite;

        [Header("Icons")]
        [SerializeField] private Sprite checkmarkSprite;
        [SerializeField] private Sprite playIconSprite;
        [SerializeField] private Sprite retryIconSprite;

        public TMP_FontAsset Font => font;
        public Sprite PanelSprite => panelSprite;
        public Sprite SlotSprite => slotSprite;
        public Sprite PrimaryButtonSprite => primaryButtonSprite;
        public Sprite SecondaryButtonSprite => secondaryButtonSprite;
        public Sprite CheckmarkSprite => checkmarkSprite;
        public Sprite PlayIconSprite => playIconSprite;
        public Sprite RetryIconSprite => retryIconSprite;
    }
}
