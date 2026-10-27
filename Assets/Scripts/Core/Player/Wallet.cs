using System;

namespace BallSort.Core.Player
{
    /// <summary>The player's coin balance. Never goes negative.</summary>
    public sealed class Wallet
    {
        /// <summary>Current balance.</summary>
        public int Coins { get; private set; }

        /// <summary>Raised after the balance changes, with the new balance.</summary>
        public event Action<int> Changed;

        /// <param name="coins">Starting balance; not negative.</param>
        public Wallet(int coins)
        {
            if (coins < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coins), coins, "A balance cannot be negative.");
            }

            Coins = coins;
        }

        /// <summary>Whether the balance covers <paramref name="amount"/>.</summary>
        public bool CanAfford(int amount) => amount >= 0 && Coins >= amount;

        /// <summary>Deducts <paramref name="amount"/> if the balance covers it.</summary>
        /// <returns>False, changing nothing, when the balance is too low.</returns>
        public bool TrySpend(int amount)
        {
            RequireNonNegative(amount);
            if (Coins < amount)
            {
                return false;
            }

            if (amount > 0)
            {
                Coins -= amount;
                Changed?.Invoke(Coins);
            }

            return true;
        }

        /// <summary>Adds <paramref name="amount"/> to the balance, e.g. a level reward.</summary>
        public void Add(int amount)
        {
            RequireNonNegative(amount);
            if (amount == 0)
            {
                return;
            }

            Coins += amount;
            Changed?.Invoke(Coins);
        }

        private static void RequireNonNegative(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be negative.");
            }
        }
    }
}
