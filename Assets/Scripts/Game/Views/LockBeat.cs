namespace BallSort.Game.Views
{
    /// <summary>The moments of a lock box's opening that feedback follows (docs/animation-spec.md, 3.3).</summary>
    public enum LockBeat
    {
        /// <summary>The key leaves the ball's hands and starts its flight to the slot.</summary>
        KeyFlying,

        /// <summary>The key seats in the slot; the box wobbles.</summary>
        KeySeated,

        /// <summary>The cover halves start to slide apart.</summary>
        Opening,
    }
}
