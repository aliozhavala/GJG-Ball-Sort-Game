using System;
using System.Collections.Generic;
using System.Text;

namespace BallSort.Core.Model
{
    /// <summary>
    /// The full mutable state of one level: an ordered set of tubes, plus the events that announce
    /// changes to them.
    /// </summary>
    /// <remarks>
    /// Moves are applied through <c>MoveCommand</c>, never directly — the mutators here are
    /// <c>internal</c> and assume the move has already been validated by <c>MoveRules</c>.
    /// <para>
    /// <see cref="Clone"/> deliberately does not copy subscribers, so solver copies are silent and
    /// cannot leak into the view layer.
    /// </para>
    /// </remarks>
    public sealed class BoardState
    {
        private readonly Tube[] _tubes;

        /// <summary>Tubes in board order; the index is a tube's stable identity within the level.</summary>
        public IReadOnlyList<Tube> Tubes => _tubes;

        public int TubeCount => _tubes.Length;

        /// <summary>
        /// Number of distinct colours the level uses, derived as <c>max colour id + 1</c>. Zero for a
        /// board with no balls.
        /// </summary>
        public int ColorCount { get; }

        /// <summary>
        /// True when every tube is either empty or completed.
        /// </summary>
        /// <remarks>
        /// A locked tube is not exempt: if it holds a mixed stack the level is not won. Levels are
        /// responsible for providing an unlock path.
        /// </remarks>
        public bool IsWon
        {
            get
            {
                for (int i = 0; i < _tubes.Length; i++)
                {
                    if (!_tubes[i].IsEmpty && !_tubes[i].IsCompleted)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Raised after a move has been fully applied.</summary>
        public event Action<MoveEventArgs> MoveApplied;

        /// <summary>Raised for each ball that changed from hidden to revealed.</summary>
        public event Action<BallRevealedEventArgs> BallRevealed;

        /// <summary>Raised when a move completes a tube.</summary>
        public event Action<TubeCompletedEventArgs> TubeCompleted;

        /// <summary>Raised when a move leaves the board in a won state.</summary>
        public event Action LevelCompleted;

        /// <summary>Raised after a move has been undone. Revealed balls stay revealed.</summary>
        public event Action<MoveEventArgs> MoveUndone;

        /// <param name="tubes">The level's tubes, in board order. At least one, none null.</param>
        public BoardState(IEnumerable<Tube> tubes)
        {
            if (tubes == null)
            {
                throw new ArgumentNullException(nameof(tubes));
            }

            _tubes = new List<Tube>(tubes).ToArray();
            if (_tubes.Length == 0)
            {
                throw new ArgumentException("A board needs at least one tube.", nameof(tubes));
            }

            int maxColorId = -1;
            for (int i = 0; i < _tubes.Length; i++)
            {
                if (_tubes[i] == null)
                {
                    throw new ArgumentException($"Tube at index {i} is null.", nameof(tubes));
                }

                Tube tube = _tubes[i];
                for (int slot = 0; slot < tube.Count; slot++)
                {
                    if (tube[slot].ColorId > maxColorId)
                    {
                        maxColorId = tube[slot].ColorId;
                    }
                }
            }

            ColorCount = maxColorId + 1;
        }

        /// <summary>Tubes by board index.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="tubeIndex"/> is not a valid tube index.</exception>
        public Tube this[int tubeIndex]
        {
            get
            {
                if (!IsValidTubeIndex(tubeIndex))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(tubeIndex), tubeIndex, $"Board has {_tubes.Length} tube(s).");
                }

                return _tubes[tubeIndex];
            }
        }

        public bool IsValidTubeIndex(int tubeIndex) => tubeIndex >= 0 && tubeIndex < _tubes.Length;

        /// <summary>
        /// An independent copy of the board, with no event subscribers. Intended for the solver.
        /// </summary>
        public BoardState Clone()
        {
            var clones = new Tube[_tubes.Length];
            for (int i = 0; i < _tubes.Length; i++)
            {
                clones[i] = _tubes[i].Clone();
            }

            return new BoardState(clones);
        }

        /// <summary>
        /// Moves <paramref name="count"/> balls from one tube to another, reveals the source's new
        /// top ball, then announces what happened.
        /// </summary>
        /// <remarks>
        /// Assumes the move is legal; callers go through <c>MoveRules</c> first. All mutation
        /// finishes before the first event fires, so subscribers always observe a consistent board.
        /// </remarks>
        internal void ApplyMove(int fromIndex, int toIndex, int count)
        {
            Tube source = _tubes[fromIndex];
            Tube target = _tubes[toIndex];
            int colorId = source.Top.ColorId;

            // Popping and pushing one at a time preserves order: the run is uniform, so the stack
            // reversal is invisible, and this keeps the loop allocation-free.
            for (int i = 0; i < count; i++)
            {
                target.Push(source.Pop());
            }

            bool revealed = source.RevealTop();
            bool completed = target.IsCompleted;

            MoveApplied?.Invoke(new MoveEventArgs(fromIndex, toIndex, count, colorId));

            if (revealed)
            {
                BallRevealed?.Invoke(
                    new BallRevealedEventArgs(fromIndex, source.Count - 1, source.Top.ColorId));
            }

            if (completed)
            {
                TubeCompleted?.Invoke(new TubeCompletedEventArgs(toIndex, colorId));
            }

            if (IsWon)
            {
                LevelCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Puts the balls of a previously applied move back where they came from.
        /// </summary>
        /// <remarks>
        /// Reveal state is intentionally left alone: information the player has already gained is
        /// never taken back. Restarting a level reloads it instead, which re-hides the balls.
        /// </remarks>
        internal void RevertMove(int fromIndex, int toIndex, int count)
        {
            Tube source = _tubes[fromIndex];
            Tube target = _tubes[toIndex];
            int colorId = target.Top.ColorId;

            for (int i = 0; i < count; i++)
            {
                source.Push(target.Pop());
            }

            MoveUndone?.Invoke(new MoveEventArgs(fromIndex, toIndex, count, colorId));
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            for (int i = 0; i < _tubes.Length; i++)
            {
                if (i > 0)
                {
                    text.Append(' ');
                }

                text.Append(_tubes[i]);
            }

            return text.ToString();
        }
    }
}
