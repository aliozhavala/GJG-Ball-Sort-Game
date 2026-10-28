using System;

namespace BallSort.Core.Model
{
    /// <summary>
    /// A single ball: a colour id, whether the player is allowed to see that colour yet, and an
    /// optional key.
    /// </summary>
    /// <remarks>
    /// A hidden ball still carries its true <see cref="ColorId"/> — the solver needs full knowledge
    /// of the board. It is the view layer's job never to render the colour of a hidden ball.
    /// <para>
    /// This is an immutable value type so that the solver can copy board states without allocating.
    /// "Revealing" a ball means replacing it with a revealed copy, not mutating it in place, and
    /// finding its key means replacing it with a keyless copy.
    /// </para>
    /// <para>
    /// A keyed ball never moves while it still holds its key: balls only move as part of a
    /// selectable run, and a key in a selectable run is found at once (see <c>docs/mechanics.md</c>).
    /// </para>
    /// </remarks>
    public readonly struct Ball : IEquatable<Ball>
    {
        /// <summary>Value of <see cref="KeyLockId"/> for a ball without a key.</summary>
        public const int NoKey = -1;

        // Stored as lock id + 1 so that default(Ball) carries no key.
        private readonly int _key;

        /// <summary>Colour index into the level's palette, in the range <c>0 .. colors - 1</c>.</summary>
        public int ColorId { get; }

        /// <summary>True while the player must not be shown this ball's colour.</summary>
        public bool IsHidden { get; }

        /// <summary>Convenience inverse of <see cref="IsHidden"/>; only revealed balls form runs.</summary>
        public bool IsRevealed => !IsHidden;

        /// <summary>True while this ball carries a key that has not been found yet.</summary>
        public bool HasKey => _key != 0;

        /// <summary>Id of the lock this ball's key opens, or <see cref="NoKey"/>.</summary>
        public int KeyLockId => _key - 1;

        /// <param name="colorId">Colour index; must not be negative.</param>
        /// <param name="isHidden">True to create a ball whose colour is concealed from the player.</param>
        /// <param name="keyLockId">Id of the lock this ball's key opens, or <see cref="NoKey"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="colorId"/> is negative or <paramref name="keyLockId"/> is below <see cref="NoKey"/>.</exception>
        public Ball(int colorId, bool isHidden = false, int keyLockId = NoKey)
        {
            if (colorId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(colorId), colorId, "Colour ids must be non-negative.");
            }

            if (keyLockId < NoKey)
            {
                throw new ArgumentOutOfRangeException(nameof(keyLockId), keyLockId, "Lock ids must be non-negative.");
            }

            ColorId = colorId;
            IsHidden = isHidden;
            _key = keyLockId + 1;
        }

        /// <summary>Returns this ball with <see cref="IsHidden"/> cleared, or itself if already revealed.</summary>
        public Ball AsRevealed() => IsHidden ? new Ball(ColorId, false, KeyLockId) : this;

        /// <summary>Returns this ball with <see cref="IsHidden"/> set, or itself if already hidden.</summary>
        public Ball AsHidden() => IsHidden ? this : new Ball(ColorId, true, KeyLockId);

        /// <summary>Returns this ball with its key removed, or itself if it has none.</summary>
        public Ball WithoutKey() => HasKey ? new Ball(ColorId, IsHidden) : this;

        public bool Equals(Ball other) =>
            ColorId == other.ColorId && IsHidden == other.IsHidden && _key == other._key;

        public override bool Equals(object obj) => obj is Ball other && Equals(other);

        // Colour ids and lock ids are small and non-negative, so packing them keeps the hash dense.
        public override int GetHashCode() => (((_key << 8) | ColorId) << 1) | (IsHidden ? 1 : 0);

        public static bool operator ==(Ball left, Ball right) => left.Equals(right);

        public static bool operator !=(Ball left, Ball right) => !left.Equals(right);

        public override string ToString() =>
            $"{(IsHidden ? "?" : string.Empty)}{ColorId}{(HasKey ? $"k{KeyLockId}" : string.Empty)}";
    }
}
