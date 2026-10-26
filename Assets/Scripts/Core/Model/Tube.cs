using System;
using System.Collections.Generic;
using System.Text;

namespace BallSort.Core.Model
{
    /// <summary>
    /// One tube: a fixed-capacity stack of <see cref="Ball"/>s, indexed bottom to top.
    /// </summary>
    /// <remarks>
    /// Mutation is <c>internal</c> on purpose. Outside the Core assembly the only way to change a
    /// tube is to run a move through <c>MoveRules</c> and a <c>MoveCommand</c>, so the rules can
    /// never be bypassed by accident.
    /// </remarks>
    public sealed class Tube
    {
        private readonly Ball[] _balls;
        private int _count;

        /// <summary>How many balls this tube holds when full. Varies per tube (variable-capacity mechanic).</summary>
        public int Capacity => _balls.Length;

        /// <summary>How many balls the tube currently holds.</summary>
        public int Count => _count;

        /// <summary>
        /// True while this tube refuses to be a move source or target.
        /// </summary>
        /// <remarks>
        /// The unlock trigger is a level-design concern that is not designed yet, so Core treats this
        /// as a plain flag and never clears it on its own. Levels using locked tubes are responsible
        /// for staying solvable.
        /// </remarks>
        public bool IsLocked { get; set; }

        public bool IsEmpty => _count == 0;

        public bool IsFull => _count == Capacity;

        /// <summary>Remaining slots; the ceiling on how many balls a single move can deposit here.</summary>
        public int FreeSpace => Capacity - _count;

        /// <summary>The topmost ball.</summary>
        /// <exception cref="InvalidOperationException">If the tube is empty.</exception>
        public Ball Top
        {
            get
            {
                if (_count == 0)
                {
                    throw new InvalidOperationException("An empty tube has no top ball.");
                }

                return _balls[_count - 1];
            }
        }

        /// <summary>Balls by slot, <c>0</c> = bottom, <see cref="Count"/> - 1 = top.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="slotIndex"/> is outside the filled range.</exception>
        public Ball this[int slotIndex]
        {
            get
            {
                if (slotIndex < 0 || slotIndex >= _count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(slotIndex), slotIndex, $"Tube holds {_count} ball(s).");
                }

                return _balls[slotIndex];
            }
        }

        /// <summary>
        /// True when the tube is full and every ball shares one colour — the tube is finished and corked.
        /// </summary>
        /// <remarks>
        /// Hidden balls count towards this. A tube can therefore complete while still concealing balls,
        /// because the balls beneath a revealed top may happen to match it. That is the literal rule from
        /// the brief; whether such a tube should reveal its contents is an open design question.
        /// </remarks>
        public bool IsCompleted
        {
            get
            {
                if (!IsFull || _count == 0)
                {
                    return false;
                }

                int color = _balls[0].ColorId;
                for (int i = 1; i < _count; i++)
                {
                    if (_balls[i].ColorId != color)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Length of the run: the contiguous block of same-coloured <em>revealed</em> balls at the top.
        /// Zero when the tube is empty or its top ball is hidden.
        /// </summary>
        public int RunLength
        {
            get
            {
                if (_count == 0 || _balls[_count - 1].IsHidden)
                {
                    return 0;
                }

                int color = _balls[_count - 1].ColorId;
                int length = 1;
                for (int i = _count - 2; i >= 0; i--)
                {
                    // A hidden ball terminates the run even when its concealed colour matches.
                    if (_balls[i].IsHidden || _balls[i].ColorId != color)
                    {
                        break;
                    }

                    length++;
                }

                return length;
            }
        }

        /// <param name="capacity">Slots in this tube; must be at least one.</param>
        /// <param name="isLocked">Whether the tube starts locked.</param>
        public Tube(int capacity, bool isLocked = false)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A tube needs at least one slot.");
            }

            _balls = new Ball[capacity];
            _count = 0;
            IsLocked = isLocked;
        }

        /// <param name="capacity">Slots in this tube; must be at least one.</param>
        /// <param name="bottomToTop">Initial contents, bottom first. May hold at most <paramref name="capacity"/> balls.</param>
        /// <param name="isLocked">Whether the tube starts locked.</param>
        /// <exception cref="ArgumentException">If more balls are supplied than the tube can hold.</exception>
        public Tube(int capacity, IEnumerable<Ball> bottomToTop, bool isLocked = false)
            : this(capacity, isLocked)
        {
            if (bottomToTop == null)
            {
                throw new ArgumentNullException(nameof(bottomToTop));
            }

            foreach (Ball ball in bottomToTop)
            {
                if (_count == capacity)
                {
                    throw new ArgumentException(
                        $"Too many balls for a tube of capacity {capacity}.", nameof(bottomToTop));
                }

                _balls[_count++] = ball;
            }
        }

        /// <summary>Non-throwing form of <see cref="Top"/>.</summary>
        public bool TryGetTop(out Ball top)
        {
            if (_count == 0)
            {
                top = default;
                return false;
            }

            top = _balls[_count - 1];
            return true;
        }

        /// <summary>
        /// Reads the run at the top of this tube.
        /// </summary>
        /// <param name="colorId">The run's colour, when there is one.</param>
        /// <param name="length">The run's length, when there is one.</param>
        /// <returns>False when the tube is empty or its top ball is hidden.</returns>
        public bool TryGetRun(out int colorId, out int length)
        {
            length = RunLength;
            if (length == 0)
            {
                colorId = default;
                return false;
            }

            colorId = _balls[_count - 1].ColorId;
            return true;
        }

        /// <summary>An independent copy; safe to mutate without affecting this tube.</summary>
        public Tube Clone()
        {
            var clone = new Tube(Capacity, IsLocked);
            Array.Copy(_balls, clone._balls, _count);
            clone._count = _count;
            return clone;
        }

        internal void Push(Ball ball)
        {
            if (IsFull)
            {
                throw new InvalidOperationException("Cannot push onto a full tube.");
            }

            _balls[_count++] = ball;
        }

        internal Ball Pop()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("Cannot pop from an empty tube.");
            }

            return _balls[--_count];
        }

        /// <summary>
        /// Reveals the top ball if it is hidden.
        /// </summary>
        /// <returns>True when a ball actually changed from hidden to revealed.</returns>
        internal bool RevealTop()
        {
            if (_count == 0 || _balls[_count - 1].IsRevealed)
            {
                return false;
            }

            _balls[_count - 1] = _balls[_count - 1].AsRevealed();
            return true;
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            text.Append('[');
            for (int i = 0; i < _count; i++)
            {
                if (i > 0)
                {
                    text.Append(' ');
                }

                text.Append(_balls[i]);
            }

            text.Append(']');
            text.Append('/');
            text.Append(Capacity);
            if (IsLocked)
            {
                text.Append(" (locked)");
            }

            return text.ToString();
        }
    }
}
