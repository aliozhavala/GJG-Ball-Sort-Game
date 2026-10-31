namespace BallSort.Game.Views
{
    /// <summary>How a tube is drawn while the Shuffle booster waits for the player to pick one.</summary>
    public enum TubeHighlight
    {
        /// <summary>As usual.</summary>
        None,

        /// <summary>The Shuffle booster can pick it: outlined by the glow in the highlight tint.</summary>
        Eligible,

        /// <summary>The Shuffle booster cannot pick it: darkened, balls and cover too.</summary>
        Dimmed,
    }
}
