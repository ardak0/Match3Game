using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// How the game FEELS: the timings and strengths of every bit of juice (pops, particles, squash, shake, streaks,
    /// combo text, button presses, the end panel). Nothing here changes the rules of the game; it is only view tuning.
    /// Change a number while the game is playing in the editor and watch the effect, no code needed.
    /// Create one with: right-click in the Project window, Create > Match3 > Feel Settings.
    /// </summary>
    [CreateAssetMenu(fileName = "FeelSettings", menuName = "Match3/Feel Settings")]
    public sealed class FeelSettings : ScriptableObject
    {
        [Header("Board motion (seconds)")]
        [SerializeField, Range(0.05f, 0.6f)] private float swapDuration = 0.18f;
        [Tooltip("How long a cleared tile takes to shrink away (after its pop).")]
        [SerializeField, Range(0.05f, 0.6f)] private float clearDuration = 0.22f;
        [Tooltip("Fall time = base + perSqrtCell * sqrt(cells fallen), so long falls feel faster per cell.")]
        [SerializeField, Range(0.02f, 0.6f)] private float fallBaseSeconds = 0.12f;
        [SerializeField, Range(0f, 0.3f)] private float fallPerSqrtCell = 0.09f;
        [Tooltip("How far (in cells) a falling tile dips past its cell before settling. Small = slight bounce.")]
        [SerializeField, Range(0f, 0.4f)] private float landingBounceInCells = 0.08f;

        [Header("Shuffle (seconds)")]
        [Tooltip("Pause before the tiles start to move, so the player can read the \"no moves\" message.")]
        [SerializeField, Range(0f, 1.5f)] private float shuffleDelay = 0.4f;
        [SerializeField, Range(0.1f, 1.5f)] private float shuffleDuration = 0.5f;

        [Header("Special tiles (seconds)")]
        [Tooltip("How long a rocket takes to stretch into a beam, or a bomb to swell to its blast size.")]
        [SerializeField, Range(0.05f, 0.6f)] private float activationDuration = 0.18f;
        [Tooltip("Delay per chain-reaction link: tiles hit by a special clear this much later than the special itself. Keep it at least as long as the activation.")]
        [SerializeField, Range(0.05f, 0.6f)] private float chainDelay = 0.2f;
        [Tooltip("How long a newly created special tile takes to pop in.")]
        [SerializeField, Range(0.05f, 0.6f)] private float popInDuration = 0.18f;
        [Tooltip("Thickness of a rocket's beam (the tile stretching across the board), in cells.")]
        [SerializeField, Range(0.1f, 1f)] private float beamThicknessInCells = 0.35f;

        [Header("HUD numbers")]
        [Tooltip("How big the bounce of a changing number is. 0.35 = 35% bigger at the peak.")]
        [SerializeField, Range(0f, 1f)] private float hudPunchStrength = 0.35f;
        [SerializeField, Range(0.05f, 1f)] private float hudPunchSeconds = 0.3f;
        [SerializeField, Range(1, 15)] private int hudPunchVibrato = 6;
        [SerializeField, Range(0f, 1f)] private float hudPunchElasticity = 0.6f;
        [Tooltip("How long the checkmark of a completed goal takes to pop in.")]
        [SerializeField, Range(0.05f, 1f)] private float goalCheckPopSeconds = 0.4f;
        [Tooltip("Fade in / fade out time of the short HUD message (\"No moves left. Shuffling!\").")]
        [SerializeField, Range(0.05f, 1f)] private float messageFadeInSeconds = 0.25f;
        [SerializeField, Range(0.05f, 1f)] private float messageFadeOutSeconds = 0.35f;
        [SerializeField, Range(0.05f, 1f)] private float comboFadeOutSeconds = 0.3f;

        [Header("Clear pop (a tile swells a little, then shrinks away)")]
        [SerializeField, Range(1f, 1.6f)] private float clearPopScale = 1.15f;
        [SerializeField, Range(0.01f, 0.3f)] private float clearPopSeconds = 0.07f;

        [Header("Particle burst when a tile clears")]
        [Tooltip("0 turns particles off. The pool is sized from this when a level starts, so change it before pressing Play.")]
        [SerializeField, Range(0, 12)] private int burstParticleCount = 6;
        [Tooltip("How far a particle flies, in cells.")]
        [SerializeField, Range(0.2f, 3f)] private float burstDistanceInCells = 0.9f;
        [SerializeField, Range(0.1f, 1.5f)] private float burstSeconds = 0.45f;
        [Tooltip("Size of a particle at the start, in cells.")]
        [SerializeField, Range(0.05f, 1f)] private float burstParticleSizeInCells = 0.35f;

        [Header("Landing squash (a tile that finished falling gets squashed flat for a moment)")]
        [Tooltip("0.2 = 20% wider and 20% shorter at the strongest point.")]
        [SerializeField, Range(0f, 0.5f)] private float squashStrength = 0.18f;
        [SerializeField, Range(0.05f, 0.8f)] private float squashSeconds = 0.25f;
        [SerializeField, Range(1, 10)] private int squashVibrato = 2;
        [SerializeField, Range(0f, 1f)] private float squashElasticity = 0.5f;

        [Header("Bomb camera shake")]
        [Tooltip("How far the camera is thrown, in cells.")]
        [SerializeField, Range(0f, 1f)] private float shakeStrengthInCells = 0.12f;
        [SerializeField, Range(0.05f, 1f)] private float shakeSeconds = 0.35f;
        [SerializeField, Range(1, 60)] private int shakeVibrato = 24;

        [Header("Rocket streak")]
        [Tooltip("How wide the glowing streak is along a rocket's line, in cells.")]
        [SerializeField, Range(0.2f, 3f)] private float streakThicknessInCells = 1.2f;
        [SerializeField, Range(0.1f, 1.5f)] private float streakSeconds = 0.35f;
        [SerializeField] private Color streakColor = new Color(1f, 0.95f, 0.7f, 0.9f);

        [Header("Combo text")]
        [Tooltip("From which clear wave of one move the combo text appears. 1 = the player's own match, 2 = the first cascade, 3 = the second cascade.")]
        [SerializeField, Range(1, 10)] private int comboMinimumWave = 3;
        [SerializeField, Range(0.05f, 1f)] private float comboPopSeconds = 0.3f;
        [SerializeField, Range(0.1f, 3f)] private float comboHoldSeconds = 0.7f;

        [Header("Buttons")]
        [Tooltip("Size of a button while it is held down. 1 = no change.")]
        [SerializeField, Range(0.7f, 1f)] private float buttonPressedScale = 0.92f;
        [SerializeField, Range(0.02f, 0.4f)] private float buttonPressSeconds = 0.08f;

        [Header("Win / lose panel")]
        [Tooltip("How long the dark background of the panel takes to fade in.")]
        [SerializeField, Range(0.05f, 1f)] private float endFadeSeconds = 0.25f;
        [SerializeField, Range(0.05f, 1.5f)] private float endPanelSeconds = 0.35f;
        [Tooltip("Pause between the panel, the title and each button appearing.")]
        [SerializeField, Range(0f, 0.5f)] private float endStaggerSeconds = 0.08f;

        public float SwapDuration => swapDuration;
        public float ClearDuration => clearDuration;
        public float FallBaseSeconds => fallBaseSeconds;
        public float FallPerSqrtCell => fallPerSqrtCell;
        public float LandingBounceInCells => landingBounceInCells;

        public float ShuffleDelay => shuffleDelay;
        public float ShuffleDuration => shuffleDuration;

        public float ActivationDuration => activationDuration;
        public float ChainDelay => chainDelay;
        public float PopInDuration => popInDuration;
        public float BeamThicknessInCells => beamThicknessInCells;

        public float HudPunchStrength => hudPunchStrength;
        public float HudPunchSeconds => hudPunchSeconds;
        public int HudPunchVibrato => hudPunchVibrato;
        public float HudPunchElasticity => hudPunchElasticity;
        public float GoalCheckPopSeconds => goalCheckPopSeconds;
        public float MessageFadeInSeconds => messageFadeInSeconds;
        public float MessageFadeOutSeconds => messageFadeOutSeconds;
        public float ComboFadeOutSeconds => comboFadeOutSeconds;

        public float ClearPopScale => clearPopScale;
        public float ClearPopSeconds => clearPopSeconds;

        public int BurstParticleCount => burstParticleCount;
        public float BurstDistanceInCells => burstDistanceInCells;
        public float BurstSeconds => burstSeconds;
        public float BurstParticleSizeInCells => burstParticleSizeInCells;

        public float SquashStrength => squashStrength;
        public float SquashSeconds => squashSeconds;
        public int SquashVibrato => squashVibrato;
        public float SquashElasticity => squashElasticity;

        public float ShakeStrengthInCells => shakeStrengthInCells;
        public float ShakeSeconds => shakeSeconds;
        public int ShakeVibrato => shakeVibrato;

        public float StreakThicknessInCells => streakThicknessInCells;
        public float StreakSeconds => streakSeconds;
        public Color StreakColor => streakColor;

        public int ComboMinimumWave => comboMinimumWave;
        public float ComboPopSeconds => comboPopSeconds;
        public float ComboHoldSeconds => comboHoldSeconds;

        public float ButtonPressedScale => buttonPressedScale;
        public float ButtonPressSeconds => buttonPressSeconds;

        public float EndFadeSeconds => endFadeSeconds;
        public float EndPanelSeconds => endPanelSeconds;
        public float EndStaggerSeconds => endStaggerSeconds;
    }
}
