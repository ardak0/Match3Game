using System;

namespace Match3.Game
{
    /// <summary>
    /// What gets saved: the best stars per level. stars[i] is 0 for a level that was never won.
    /// A level is unlocked when it is the first one or the level before it has at least 1 star,
    /// so there is no separate "unlocked" list that could disagree with the stars.
    /// The public fields are what Unity's JsonUtility writes to the file.
    /// </summary>
    [Serializable]
    public sealed class ProgressData
    {
        /// <summary>Bump this when the saved shape changes, so old files are not misread.</summary>
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int[] stars = new int[0];
    }
}
