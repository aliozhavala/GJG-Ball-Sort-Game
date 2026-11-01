namespace BallSort.Game.Views
{
    /// <summary>Why a ball flies; decides whether its landing is heard (SFX guide §5).</summary>
    public enum FlightKind
    {
        /// <summary>A move: the landing plays its sound and haptic.</summary>
        Move,

        /// <summary>An undo carries the ball back: silent landing.</summary>
        Undo,

        /// <summary>A shuffle moves the ball within its tube: silent landing.</summary>
        Shuffle,
    }
}
