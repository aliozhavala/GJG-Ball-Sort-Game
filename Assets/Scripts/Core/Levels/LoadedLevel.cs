using BallSort.Core.Model;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// A validated level ready to play: the live board, its grid, and the source data.
    /// </summary>
    public sealed class LoadedLevel
    {
        /// <summary>Display name, usually the file name; used in logs and error messages.</summary>
        public string Name { get; }

        /// <summary>The validated source data. Restart rebuilds the board and grid from it, re-hiding hidden balls.</summary>
        public LevelDto Source { get; }

        /// <summary>The playable board, freshly built from <see cref="Source"/>.</summary>
        public BoardState Board { get; }

        /// <summary>The board's grid, freshly built from <see cref="Source"/>; the session adds tubes to it.</summary>
        public BoardGrid Grid { get; }

        /// <summary>Colour count declared by the level; may exceed <see cref="BoardState.ColorCount"/> only for invalid data.</summary>
        public int Colors => Source.Colors;

        internal LoadedLevel(string name, LevelDto source, BoardState board, BoardGrid grid)
        {
            Name = name;
            Source = source;
            Board = board;
            Grid = grid;
        }
    }
}
