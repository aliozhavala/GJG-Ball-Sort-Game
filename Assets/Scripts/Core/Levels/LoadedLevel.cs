using System.Collections.Generic;
using BallSort.Core.Model;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// A validated level ready to play: the live board, its resolved row layout, and the source data.
    /// </summary>
    public sealed class LoadedLevel
    {
        /// <summary>Display name, usually the file name; used in logs and error messages.</summary>
        public string Name { get; }

        /// <summary>The validated source data. Restart rebuilds the board from it, re-hiding hidden balls.</summary>
        public LevelDto Source { get; }

        /// <summary>The playable board, freshly built from <see cref="Source"/>.</summary>
        public BoardState Board { get; }

        /// <summary>Tube counts per row, top row first; always sums to the board's tube count.</summary>
        public IReadOnlyList<int> Rows { get; }

        /// <summary>Colour count declared by the level; may exceed <see cref="BoardState.ColorCount"/> only for invalid data.</summary>
        public int Colors => Source.Colors;

        internal LoadedLevel(string name, LevelDto source, BoardState board, IReadOnlyList<int> rows)
        {
            Name = name;
            Source = source;
            Board = board;
            Rows = rows;
        }
    }
}
