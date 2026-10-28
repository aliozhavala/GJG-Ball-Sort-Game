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

        /// <summary>A revealed ball carrying the key for <paramref name="lockId"/>.</summary>
        public static Ball K(int colorId, int lockId) => new Ball(colorId, false, lockId);

        /// <summary>A hidden ball carrying the key for <paramref name="lockId"/>.</summary>
        public static Ball HK(int colorId, int lockId) => new Ball(colorId, true, lockId);

        /// <summary>A tube of <paramref name="capacity"/> slots holding <paramref name="bottomToTop"/>.</summary>
        public static Tube MakeTube(int capacity, params Ball[] bottomToTop) =>
            new Tube(capacity, bottomToTop);

        /// <summary>As <see cref="MakeTube"/>, under a veil of <paramref name="colorId"/>.</summary>
        public static Tube MakeVeiledTube(int colorId, int capacity, params Ball[] bottomToTop) =>
            new Tube(capacity, bottomToTop, TubeCover.Veil(colorId));

        /// <summary>As <see cref="MakeTube"/>, covered by lock <paramref name="lockId"/>.</summary>
        public static Tube MakeLockedTube(int lockId, int capacity, params Ball[] bottomToTop) =>
            new Tube(capacity, bottomToTop, TubeCover.Lock(lockId));

        /// <summary>As <see cref="MakeTube"/>, but clay: never a move source.</summary>
        public static Tube MakeClayTube(int capacity, params Ball[] bottomToTop) =>
            new Tube(capacity, bottomToTop, isClay: true);

        /// <summary>An empty tube of <paramref name="capacity"/> slots.</summary>
        public static Tube MakeEmptyTube(int capacity) => new Tube(capacity);

        public static BoardState MakeBoard(params Tube[] tubes) => new BoardState(tubes);
    }
}
