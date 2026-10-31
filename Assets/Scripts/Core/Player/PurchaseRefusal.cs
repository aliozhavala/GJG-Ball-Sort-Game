namespace BallSort.Core.Player
{
    /// <summary>Why a purchase with coins was refused, or <see cref="None"/> when it went through.</summary>
    public enum PurchaseRefusal
    {
        /// <summary>Bought: the coins are spent and the goods granted.</summary>
        None = 0,

        /// <summary>The player has fewer coins than the price. Nothing changed.</summary>
        NotEnoughCoins,

        /// <summary>Lives are already at the maximum, so a refill would buy nothing. Nothing changed.</summary>
        LivesFull,
    }
}
