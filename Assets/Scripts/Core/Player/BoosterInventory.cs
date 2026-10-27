using System;

namespace BallSort.Core.Player
{
    /// <summary>
    /// The player's booster counts. Global, not per level: a booster spent in one level is gone
    /// for every level after it.
    /// </summary>
    public sealed class BoosterInventory
    {
        private const int TypeCount = 3;

        private readonly int[] _counts = new int[TypeCount];

        /// <summary>Raised after a count changes, with the booster and its new count.</summary>
        public event Action<BoosterType, int> Changed;

        /// <param name="undo">Starting Undo count; not negative.</param>
        /// <param name="shuffle">Starting Shuffle count; not negative.</param>
        /// <param name="addTube">Starting Add-tube count; not negative.</param>
        public BoosterInventory(int undo, int shuffle, int addTube)
        {
            _counts[(int)BoosterType.Undo] = RequireNonNegative(undo, nameof(undo));
            _counts[(int)BoosterType.Shuffle] = RequireNonNegative(shuffle, nameof(shuffle));
            _counts[(int)BoosterType.AddTube] = RequireNonNegative(addTube, nameof(addTube));
        }

        /// <summary>How many of <paramref name="type"/> the player holds.</summary>
        public int GetCount(BoosterType type) => _counts[IndexOf(type)];

        /// <summary>Spends one <paramref name="type"/> booster if the player has any.</summary>
        /// <returns>False, changing nothing, when the count is zero.</returns>
        public bool TrySpend(BoosterType type)
        {
            int index = IndexOf(type);
            if (_counts[index] == 0)
            {
                return false;
            }

            _counts[index]--;
            Changed?.Invoke(type, _counts[index]);
            return true;
        }

        /// <summary>Gives the player more of <paramref name="type"/>, e.g. as a reward.</summary>
        public void Add(BoosterType type, int amount)
        {
            RequireNonNegative(amount, nameof(amount));
            if (amount == 0)
            {
                return;
            }

            int index = IndexOf(type);
            _counts[index] += amount;
            Changed?.Invoke(type, _counts[index]);
        }

        private static int IndexOf(BoosterType type)
        {
            int index = (int)type;
            if (index < 0 || index >= TypeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown booster type.");
            }

            return index;
        }

        private static int RequireNonNegative(int value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Cannot be negative.");
            }

            return value;
        }
    }
}
