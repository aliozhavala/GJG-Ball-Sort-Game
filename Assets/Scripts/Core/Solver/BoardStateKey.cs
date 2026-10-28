using System;
using BallSort.Core.Model;

namespace BallSort.Core.Solver
{
    /// <summary>
    /// The identity of a board position for search and de-duplication: two boards of the same level
    /// have equal keys exactly when they are the same position, up to swapping interchangeable tubes.
    /// </summary>
    /// <remarks>
    /// The key covers everything that can change during play — each tube's balls bottom to top
    /// (colour, hidden flag, key), its cover, and the tube count — plus each tube's fixed capacity
    /// and clay flag. Tubes with the same capacity, clay flag and cover (kind and id) are
    /// interchangeable: no rule depends on a tube's index (the grid never affects legality), so
    /// their encodings are sorted before they are joined.
    /// <para>
    /// The key is a compact byte encoding compared byte for byte. The hash only speeds lookups up;
    /// a collision can never make two different positions equal, so the solver can never skip an
    /// unvisited position and wrongly report a level unsolvable.
    /// </para>
    /// </remarks>
    public readonly struct BoardStateKey : IEquatable<BoardStateKey>
    {
        private readonly byte[] _bytes;
        private readonly int _hash;

        internal BoardStateKey(byte[] bytes)
        {
            _bytes = bytes;
            _hash = Hash(bytes);
        }

        /// <summary>Size of the encoding in bytes.</summary>
        public int Length => _bytes?.Length ?? 0;

        /// <summary>The key of <paramref name="board"/>'s current position.</summary>
        public static BoardStateKey Of(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var encoder = new BoardEncoder();
            encoder.Encode(board);
            return encoder.CreateKey();
        }

        public bool Equals(BoardStateKey other)
        {
            if (_hash != other._hash || Length != other.Length)
            {
                return false;
            }

            for (int i = 0; i < Length; i++)
            {
                if (_bytes[i] != other._bytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => obj is BoardStateKey other && Equals(other);

        public override int GetHashCode() => _hash;

        public static bool operator ==(BoardStateKey left, BoardStateKey right) => left.Equals(right);

        public static bool operator !=(BoardStateKey left, BoardStateKey right) => !left.Equals(right);

        public override string ToString() => _bytes == null ? "(none)" : BitConverter.ToString(_bytes);

        // FNV-1a: cheap and well spread for short byte strings.
        private static int Hash(byte[] bytes)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < bytes.Length; i++)
                {
                    hash = (hash ^ bytes[i]) * 16777619;
                }

                return (int)hash;
            }
        }
    }
}
