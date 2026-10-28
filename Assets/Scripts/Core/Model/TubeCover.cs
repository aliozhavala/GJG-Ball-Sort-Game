using System;

namespace BallSort.Core.Model
{
    /// <summary>
    /// The cover on a tube: none, a veil of one ball colour, or a lock with an id.
    /// </summary>
    /// <remarks>
    /// A tube has at most one cover, and removing it is permanent — undo never puts it back. While
    /// covered, the tube's contents are drawn covered and it is neither a move source nor a target.
    /// </remarks>
    public readonly struct TubeCover : IEquatable<TubeCover>
    {
        /// <summary>No cover.</summary>
        public static TubeCover None => default;

        public CoverKind Kind { get; }

        /// <summary>The veil's ball colour id, or the lock's id. Zero when <see cref="Kind"/> is <see cref="CoverKind.None"/>.</summary>
        public int Id { get; }

        public bool IsCovered => Kind != CoverKind.None;

        private TubeCover(CoverKind kind, int id)
        {
            Kind = kind;
            Id = id;
        }

        /// <summary>A veil that opens when a tube completes in <paramref name="colorId"/>.</summary>
        public static TubeCover Veil(int colorId)
        {
            if (colorId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(colorId), colorId, "Colour ids must be non-negative.");
            }

            return new TubeCover(CoverKind.Veil, colorId);
        }

        /// <summary>A lock that opens when the key for <paramref name="lockId"/> is found.</summary>
        public static TubeCover Lock(int lockId)
        {
            if (lockId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lockId), lockId, "Lock ids must be non-negative.");
            }

            return new TubeCover(CoverKind.Lock, lockId);
        }

        public bool Equals(TubeCover other) => Kind == other.Kind && Id == other.Id;

        public override bool Equals(object obj) => obj is TubeCover other && Equals(other);

        public override int GetHashCode() => (Id << 2) | (int)Kind;

        public static bool operator ==(TubeCover left, TubeCover right) => left.Equals(right);

        public static bool operator !=(TubeCover left, TubeCover right) => !left.Equals(right);

        public override string ToString() => Kind switch
        {
            CoverKind.Veil => $"veil {Id}",
            CoverKind.Lock => $"lock {Id}",
            _ => "none",
        };
    }
}
