using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Plays a player's swap on the board, all at once, and reports what happened.
    /// The model decides everything here; the view only animates the returned steps.
    ///
    /// ResolveSwap:
    ///   1. Check the swap is allowed: both cells on the board, next to each other, both holding a tile,
    ///      and the swap must either create a match or involve a special tile. Otherwise the board stays
    ///      as it was and we return Invalid.
    ///   2. Repeat until nothing is left to clear (one round = one "wave"):
    ///        collect what this wave clears (match cells, swapped specials, combos),
    ///        turn tiles into rockets or bombs if a ColorBomb combo asks for it,
    ///        set off the special tiles among them (chain reaction),
    ///        clear everything collected, leave behind any special the matches created,
    ///        let tiles fall, refill from above, look for new matches.
    ///   3. Return the ordered steps plus how many tiles of each color were cleared.
    /// </summary>
    public sealed class BoardResolver
    {
        private readonly MatchFinder _matchFinder = new MatchFinder();
        private readonly GravityResolver _gravity = new GravityResolver();
        private readonly SpecialResolver _specials;
        private readonly Refiller _refiller;
        private readonly List<Match> _matches = new List<Match>();
        private readonly ClearSet _clearSet = new ClearSet();
        private readonly List<SpecialCreation> _creations = new List<SpecialCreation>();

        public BoardResolver(IRandom random, IReadOnlyList<TileColor> colors)
        {
            _refiller = new Refiller(random, colors);
            _specials = new SpecialResolver(random); // the same random: it only picks the direction of rockets a ColorBomb makes
        }

        public ResolveResult ResolveSwap(Board board, GridPos a, GridPos b)
        {
            if (!IsLegalSwapTarget(board, a, b)) return ResolveResult.Invalid;

            // Remember the tiles BEFORE swapping: the SwapStep needs their ids, and the specials decide what the swap does.
            Tile tileA = board.Get(a);
            Tile tileB = board.Get(b);
            bool swapInvolvesSpecial = tileA.Special != SpecialType.None || tileB.Special != SpecialType.None;

            board.Swap(a, b);
            _matchFinder.FindMatches(board, _matches);
            if (_matches.Count == 0 && !swapInvolvesSpecial)
            {
                board.Swap(a, b); // put everything back: an invalid move must not change the board
                return ResolveResult.Invalid;
            }

            List<ResolveStep> steps = new List<ResolveStep>();
            int[] clearedByColor = new int[Enum.GetValues(typeof(TileColor)).Length];
            steps.Add(new SwapStep(0, a, b, tileA.Id, tileB.Id));

            int wave = 1;
            bool firstWave = true; // only the first wave has swapped cells (and swapped specials)
            while (_matches.Count > 0 || (firstWave && swapInvolvesSpecial))
            {
                _specials.BeginWave();
                CollectCellsToClear(board, firstWave, a, b, tileA.Special, tileB.Special);

                // The conversions must be on the board before ActivateSpecials looks for specials to set off.
                ConvertStep convert = ApplyConversions(board, wave);
                _specials.ActivateSpecials(board, _clearSet);

                // What the view plays, in order: beams from the ColorBombs, tiles turning special, then the clear.
                AddColorBombFires(wave, steps);
                if (convert != null) steps.Add(convert);
                steps.Add(ClearCollectedCells(board, wave, clearedByColor));
                AddCreatedSpecials(board, wave, steps);

                FallStep fall = _gravity.Apply(board, wave);
                if (fall != null) steps.Add(fall);

                SpawnStep spawn = _refiller.Refill(board, wave);
                if (spawn != null) steps.Add(spawn);

                // New tiles or tiles that landed next to each other may have made a new match.
                _matchFinder.FindMatches(board, _matches);
                firstWave = false;
                wave++;
            }

            return new ResolveResult(steps, clearedByColor, wave - 1);
        }

        private static bool IsLegalSwapTarget(Board board, GridPos a, GridPos b)
        {
            if (!board.IsInside(a) || !board.IsInside(b)) return false;

            bool nextToEachOther = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1; // no diagonals, not the same cell
            if (!nextToEachOther) return false;

            return board.Get(a) != null && board.Get(b) != null;
        }

        // Fills _clearSet with the starting cells of this wave and _creations with the specials the matches leave behind.
        // (Special tiles that these cells hit are added afterwards by ActivateSpecials.)
        private void CollectCellsToClear(Board board, bool firstWave, GridPos a, GridPos b, SpecialType specialFromA, SpecialType specialFromB)
        {
            _clearSet.Reset(board);
            _creations.Clear();

            for (int i = 0; i < _matches.Count; i++)
            {
                Match match = _matches[i];
                for (int j = 0; j < match.Positions.Count; j++)
                {
                    _clearSet.Mark(match.Positions[j], 0);
                }

                if (_specials.TryGetCreation(match, firstWave, a, b, out SpecialCreation creation))
                {
                    _creations.Add(creation);
                }
            }

            if (!firstWave) return;

            // The swap itself. The tile that was at A is now at B and the other way round.
            bool aWasSpecial = specialFromA != SpecialType.None;
            bool bWasSpecial = specialFromB != SpecialType.None;

            if (specialFromA == SpecialType.ColorBomb || specialFromB == SpecialType.ColorBomb)
            {
                _specials.MarkColorBombSwap(board, _clearSet, a, b); // the ColorBomb's own rules, for every partner
            }
            else if (aWasSpecial && bWasSpecial)
            {
                _specials.MarkCombo(board, _clearSet, a, b, specialFromA, specialFromB); // the combo is centered on B
            }
            else if (aWasSpecial)
            {
                _clearSet.Mark(b, 0); // that special is now at B, and swapping it sets it off
            }
            else if (bWasSpecial)
            {
                _clearSet.Mark(a, 0);
            }
        }

        // Turns the tiles the SpecialResolver asked for into rockets or bombs. The new tile has the SAME id and color:
        // it is the same piece, so the view can change it in place. Returns null if nothing was converted.
        private ConvertStep ApplyConversions(Board board, int wave)
        {
            IReadOnlyList<SpecialConversion> conversions = _specials.Conversions;
            if (conversions.Count == 0) return null;

            ConvertedTile[] converted = new ConvertedTile[conversions.Count];
            for (int i = 0; i < converted.Length; i++)
            {
                SpecialConversion conversion = conversions[i];
                Tile old = board.Get(conversion.Position);
                board.Set(conversion.Position, new Tile(old.Id, old.Color, conversion.Special));
                converted[i] = new ConvertedTile(old.Id, old.Color, conversion.Position, conversion.Special);
            }

            return new ConvertStep(wave, converted);
        }

        private void AddColorBombFires(int wave, List<ResolveStep> steps)
        {
            IReadOnlyList<ColorBombFire> fires = _specials.ColorBombFires;
            for (int i = 0; i < fires.Count; i++)
            {
                ColorBombFire fire = fires[i];
                steps.Add(new ColorBombFireStep(wave, fire.TileId, fire.Position, fire.Depth, fire.Targets));
            }
        }

        // Removes every collected tile from the board and records it.
        private ClearStep ClearCollectedCells(Board board, int wave, int[] clearedByColor)
        {
            ClearedTile[] cleared = new ClearedTile[_clearSet.Count];

            for (int i = 0; i < cleared.Length; i++)
            {
                GridPos pos = _clearSet.PositionAt(i);
                Tile tile = board.Get(pos);
                cleared[i] = new ClearedTile(tile.Id, tile.Color, pos, tile.Special, _clearSet.DepthAt(pos));
                clearedByColor[(int)tile.Color]++;
                board.Set(pos, null);
            }

            return new ClearStep(wave, cleared);
        }

        // Puts the special tiles that this wave's matches created into the cells that were just cleared.
        // They are placed before gravity, so they fall like any other tile if the cells below them were cleared too.
        private void AddCreatedSpecials(Board board, int wave, List<ResolveStep> steps)
        {
            for (int i = 0; i < _creations.Count; i++)
            {
                SpecialCreation creation = _creations[i];
                Tile tile = board.NewTile(creation.Color, creation.Special);
                board.Set(creation.Position, tile);
                steps.Add(new SpecialCreatedStep(wave, tile.Id, creation.Color, creation.Special, creation.Position));
            }
        }
    }
}
