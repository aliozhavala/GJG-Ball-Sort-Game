using System;
using BallSort.Core.Model;

namespace BallSort.Core.Solver
{
    /// <summary>
    /// Packs a board into bytes and back. The packed form, in board order, is how the solver stores
    /// positions; <see cref="CreateKey"/> reorders it into the canonical <see cref="BoardStateKey"/>.
    /// </summary>
    /// <remarks>
    /// Layout: the tube count, then one segment per tube —
    /// capacity, a flags byte (bit 0 clay, bits 1–2 cover kind), the cover id if covered, the ball
    /// count, then each ball bottom to top as <c>colour &lt;&lt; 2 | hasKey &lt;&lt; 1 | hidden</c>
    /// followed by the key's lock id when it has one. Every number is a varint, so segments are
    /// self-delimiting and almost every value fits in one byte.
    /// <para>
    /// One instance reuses its buffers across calls and is not thread-safe; each search owns one.
    /// </para>
    /// </remarks>
    internal sealed class BoardEncoder
    {
        private byte[] _packed = new byte[128];
        private int _length;
        private int _headerLength;
        private int[] _segmentStart = new int[16];
        private int[] _segmentLength = new int[16];
        private int[] _order = new int[16];
        private int _tubeCount;

        /// <summary>Encodes <paramref name="board"/> into the internal buffer, replacing the previous board.</summary>
        public void Encode(BoardState board)
        {
            _length = 0;
            _tubeCount = board.TubeCount;
            EnsureTubeCapacity(_tubeCount);

            WriteVarint(_tubeCount);
            _headerLength = _length;

            for (int t = 0; t < _tubeCount; t++)
            {
                _segmentStart[t] = _length;
                Tube tube = board[t];
                WriteVarint(tube.Capacity);
                WriteByte((byte)((tube.IsClay ? 1 : 0) | ((int)tube.Cover.Kind << 1)));
                if (tube.IsCovered)
                {
                    WriteVarint(tube.Cover.Id);
                }

                WriteVarint(tube.Count);
                for (int slot = 0; slot < tube.Count; slot++)
                {
                    Ball ball = tube[slot];
                    WriteVarint((ball.ColorId << 2) | (ball.HasKey ? 2 : 0) | (ball.IsHidden ? 1 : 0));
                    if (ball.HasKey)
                    {
                        WriteVarint(ball.KeyLockId);
                    }
                }

                _segmentLength[t] = _length - _segmentStart[t];
            }
        }

        /// <summary>The last encoded board in board order, as a new array.</summary>
        public byte[] CopyPacked()
        {
            var packed = new byte[_length];
            Buffer.BlockCopy(_packed, 0, packed, 0, _length);
            return packed;
        }

        /// <summary>
        /// The last encoded board's canonical key: the tube segments sorted bytewise. Each segment
        /// starts with its tube's capacity, clay flag and cover, so sorting can only ever line up
        /// interchangeable tubes differently; the key is therefore identical for two positions
        /// exactly when one is the other with interchangeable tubes swapped.
        /// </summary>
        public BoardStateKey CreateKey()
        {
            for (int i = 0; i < _tubeCount; i++)
            {
                _order[i] = i;
            }

            // Insertion sort: boards hold a few dozen tubes at most, and it allocates nothing.
            for (int i = 1; i < _tubeCount; i++)
            {
                int current = _order[i];
                int j = i - 1;
                while (j >= 0 && CompareSegments(_order[j], current) > 0)
                {
                    _order[j + 1] = _order[j];
                    j--;
                }

                _order[j + 1] = current;
            }

            var key = new byte[_length];
            Buffer.BlockCopy(_packed, 0, key, 0, _headerLength);
            int offset = _headerLength;
            for (int i = 0; i < _tubeCount; i++)
            {
                int tube = _order[i];
                Buffer.BlockCopy(_packed, _segmentStart[tube], key, offset, _segmentLength[tube]);
                offset += _segmentLength[tube];
            }

            return new BoardStateKey(key);
        }

        /// <summary>
        /// Writes a packed position back into <paramref name="board"/>, which must belong to the same
        /// level as the board that was packed (same tubes, capacities and clay flags).
        /// </summary>
        public static void Unpack(byte[] packed, BoardState board)
        {
            int position = 0;
            int tubeCount = ReadVarint(packed, ref position);
            if (tubeCount != board.TubeCount)
            {
                throw new ArgumentException($"Packed board has {tubeCount} tubes; the board has {board.TubeCount}.");
            }

            for (int t = 0; t < tubeCount; t++)
            {
                Tube tube = board[t];
                int capacity = ReadVarint(packed, ref position);
                int flags = packed[position++];
                if (capacity != tube.Capacity || (flags & 1) != (tube.IsClay ? 1 : 0))
                {
                    throw new ArgumentException($"Packed tube {t} does not match the board's tube {t}.");
                }

                var kind = (CoverKind)(flags >> 1);
                TubeCover cover = kind == CoverKind.Veil ? TubeCover.Veil(ReadVarint(packed, ref position))
                    : kind == CoverKind.Lock ? TubeCover.Lock(ReadVarint(packed, ref position))
                    : TubeCover.None;

                tube.Reset(cover);
                int count = ReadVarint(packed, ref position);
                for (int slot = 0; slot < count; slot++)
                {
                    int value = ReadVarint(packed, ref position);
                    int keyLockId = (value & 2) != 0 ? ReadVarint(packed, ref position) : Ball.NoKey;
                    tube.Push(new Ball(value >> 2, (value & 1) != 0, keyLockId));
                }
            }

            board.RecountKeys();
        }

        private int CompareSegments(int a, int b)
        {
            int startA = _segmentStart[a];
            int startB = _segmentStart[b];
            int lengthA = _segmentLength[a];
            int lengthB = _segmentLength[b];
            int common = Math.Min(lengthA, lengthB);
            for (int i = 0; i < common; i++)
            {
                int difference = _packed[startA + i] - _packed[startB + i];
                if (difference != 0)
                {
                    return difference;
                }
            }

            return lengthA - lengthB;
        }

        private void EnsureTubeCapacity(int tubeCount)
        {
            if (_segmentStart.Length < tubeCount)
            {
                _segmentStart = new int[tubeCount];
                _segmentLength = new int[tubeCount];
                _order = new int[tubeCount];
            }
        }

        private void WriteVarint(int value)
        {
            uint remaining = (uint)value;
            while (remaining >= 0x80)
            {
                WriteByte((byte)(remaining | 0x80));
                remaining >>= 7;
            }

            WriteByte((byte)remaining);
        }

        private void WriteByte(byte value)
        {
            if (_length == _packed.Length)
            {
                Array.Resize(ref _packed, _packed.Length * 2);
            }

            _packed[_length++] = value;
        }

        private static int ReadVarint(byte[] data, ref int position)
        {
            uint value = 0;
            int shift = 0;
            byte current;
            do
            {
                current = data[position++];
                value |= (uint)(current & 0x7F) << shift;
                shift += 7;
            }
            while ((current & 0x80) != 0);

            return (int)value;
        }
    }
}
