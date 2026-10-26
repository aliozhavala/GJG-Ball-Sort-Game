using System;

namespace BallSort.Core.Model
{
    /// <summary>
    /// A single ball: a colour id plus whether the player is allowed to see that colour yet.
    /// </summary>
    /// <remarks>
    /// A hidden ball still carries its true <see cref="ColorId"/> — the solver needs full knowledge
    /// of the board. It is the view layer's job never to render the colour of a hidden ball.
    /// <para>
    /// This is an immutable value type so that the solver can copy board states without allocating.
    /// "Revealing" a ball means replacing it with a revealed copy, not mutating it in place.
    /// </para>
    /// </remarks>
    public readonly struct Ball : IEquatable<Ball>
    {
        /// <summary>Colour index into the level's palette, in the range <c>0 .. colors - 1</c>.</summary>
        public int ColorId { get; }

        /// <summary>True while the player must not be shown this ball's colour.</summary>
        public bool IsHidden { get; }

        /// <summary>Convenience inverse of <see cref="IsHidden"/>; only revealed balls form runs.</summary>
        public bool IsRevealed => !IsHidden;

        /// <param name="colorId">Colour index; must not be negative.</param>
        /// <param name="isHidden">True to create a ball whose colour is concealed from the player.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="colorId"/> is negative.</exception>
        public Ball(int colorId, bool isHidden = false)
        {
            if (colorId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(colorId), colorId, "Colour ids must be non-negative.");
            }

            ColorId = colorId;
            IsHidden = isHidden;
        }

        /// <summary>Returns this ball with <see cref="IsHidden"/> cleared, or itself if already revealed.</summary>
        public Ball AsRevealed() => IsHidden ? new Ball(ColorId) : this;

        /// <summary>Returns this ball with <see cref="IsHidden"/> set, or itself if already hidden.</summary>
        public Ball AsHidden() => IsHidden ? this : new Ball(ColorId, true);

        public bool Equals(Ball other) => ColorId == other.ColorId && IsHidden == other.IsHidden;

        public override bool Equals(object obj) => obj is Ball other && Equals(other);

        // Colour ids are small and non-negative, so shifting in the hidden flag keeps the hash dense.
        public override int GetHashCode() => (ColorId << 1) | (IsHidden ? 1 : 0);

        public static bool operator ==(Ball left, Ball right) => left.Equals(right);

        public static bool operator !=(Ball left, Ball right) => !left.Equals(right);

        public override string ToString() => IsHidden ? $"?{ColorId}" : ColorId.ToString();
    }
}
