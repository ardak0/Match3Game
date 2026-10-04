using System;

namespace Match3.Core
{
    /// <summary>
    /// A cell coordinate on the board. X is the column, Y is the row.
    /// Y = 0 is the BOTTOM row, so "gravity" later means "towards smaller Y".
    /// We use this instead of Vector2Int so Core stays free of UnityEngine.
    /// </summary>
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridPos other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);

        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);

        public override string ToString() => "(" + X + "," + Y + ")";
    }
}
