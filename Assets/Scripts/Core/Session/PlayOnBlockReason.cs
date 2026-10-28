namespace BallSort.Core.Session
{
    /// <summary>Why Play on (pay coins for an extra tube while stuck) cannot be used right now.</summary>
    public enum PlayOnBlockReason
    {
        /// <summary>Play on is available.</summary>
        None = 0,

        /// <summary>The board is not stuck; Play on is only offered as a way out of a dead end.</summary>
        NotStuck,

        /// <summary>Every grid row already has eight cells, so there is nowhere to put the extra tube.</summary>
        NoRoom,

        /// <summary>The player cannot afford the price.</summary>
        NotEnoughCoins,
    }
}
