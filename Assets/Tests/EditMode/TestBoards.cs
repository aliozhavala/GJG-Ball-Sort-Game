using BallSort.Core.Model;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Terse board builders for the tests. Ball arrays always read bottom to top, matching the
    /// level JSON format.
    /// </summary>
    internal static class TestBoards
    {
        /// <summary>A revealed ball of the given colour.</summary>
        public static Ball R(int colorId) => new Ball(colorId);

        /// <summary>A hidden ball of the given colour.</summary>
        public static Ball H(int colorId) => new Ball(colorId, true);

        /// <summary>A tube of <paramref name="capacity"/> slots holding <paramref name="bottomToTop"/>.</summary>
        public static Tube MakeTube(int capacity, params Ball[] bottomToTop) =>
            new Tube(capacity, bottomToTop);

        /// <summary>As <see cref="MakeTube"/>, but the tube starts locked.</summary>
        public static Tube MakeLockedTube(int capacity, params Ball[] bottomToTop) =>
            new Tube(capacity, bottomToTop, isLocked: true);

        /// <summary>An empty tube of <paramref name="capacity"/> slots.</summary>
        public static Tube MakeEmptyTube(int capacity) => new Tube(capacity);

        public static BoardState MakeBoard(params Tube[] tubes) => new BoardState(tubes);
    }
}
