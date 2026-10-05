namespace Match3.Core
{
    /// <summary>
    /// A ColorBomb combo turns a plain tile into a rocket or bomb before it fires.
    /// The SpecialResolver only decides it; BoardResolver changes the board and tells the view with a ConvertStep.
    /// </summary>
    public readonly struct SpecialConversion
    {
        public readonly GridPos Position;
        public readonly SpecialType Special;

        public SpecialConversion(GridPos position, SpecialType special)
        {
            Position = position;
            Special = special;
        }
    }
}
