namespace BallSort.Game.Views
{
    /// <summary>A ball's face. Each colour has one sprite per expression in the <see cref="ColorPalette"/>.</summary>
    public enum BallExpression
    {
        /// <summary>Eyes open: the normal look.</summary>
        Open,

        /// <summary>Eyes half closed: a blink frame.</summary>
        HalfOpen,

        /// <summary>Eyes closed: a blink frame.</summary>
        Closed,

        /// <summary>Laughing: balls in a completed tube.</summary>
        Laugh,

        /// <summary>Shocked, for a moment: an invalid tap on the ball's tube.</summary>
        Shock,
    }
}
