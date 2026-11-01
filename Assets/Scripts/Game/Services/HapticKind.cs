namespace BallSort.Game.Services
{
    /// <summary>The haptics the SFX guide pairs with sounds (docs/design/SFX_GUIDE.md §2), by their iOS names.</summary>
    public enum HapticKind
    {
        /// <summary>UISelectionFeedbackGenerator: a ball is lifted.</summary>
        Selection,

        /// <summary>Impact · Light: button presses, boosters.</summary>
        Light,

        /// <summary>Impact · Medium: a ball lands.</summary>
        Medium,

        /// <summary>Notification · Warning: a move or tap refused, stuck, a life lost.</summary>
        Warning,

        /// <summary>Notification · Success: a cork seats, a purchase.</summary>
        Success,

        /// <summary>Impact · Heavy, then Success: the ice breaks, a towel lifts, a lock box bursts.</summary>
        Heavy,

        /// <summary>Success plus a custom pattern: the win.</summary>
        Win,
    }
}
