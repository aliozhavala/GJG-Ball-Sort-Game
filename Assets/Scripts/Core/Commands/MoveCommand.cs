using System;
using BallSort.Core.Model;
using BallSort.Core.Rules;

namespace BallSort.Core.Commands
{
    /// <summary>
    /// One move, as an undoable command. Built from a <see cref="MoveInfo"/> that
    /// <see cref="MoveRules"/> has already validated.
    /// </summary>
    public sealed class MoveCommand
    {
        private readonly BoardState _board;
        private bool _isExecuted;

        /// <summary>Index of the tube the balls were taken from.</summary>
        public int FromIndex { get; }

        /// <summary>Index of the tube the balls were placed into.</summary>
        public int ToIndex { get; }

        /// <summary>How many balls this move carries.</summary>
        public int Count { get; }

        /// <summary>Shared colour of the moved balls.</summary>
        public int ColorId { get; }

        /// <summary>True when the target could not take the whole run.</summary>
        public bool IsPartial { get; }

        /// <summary>
        /// Whether executing this move completed the target tube.
        /// </summary>
        /// <remarks>
        /// Meaningful only after <see cref="Execute"/>. <c>CommandHistory</c> uses this to seal the
        /// undo history: a corked tube never returns to an unfinished state.
        /// </remarks>
        public bool CompletedTube { get; private set; }

        /// <summary>
        /// Whether the most recent <see cref="Undo"/> completed the source tube.
        /// </summary>
        /// <remarks>
        /// Possible because reveals survive an undo: putting a run back onto a ball it uncovered can
        /// fill a single-colour tube. <c>CommandHistory</c> seals on this exactly as on
        /// <see cref="CompletedTube"/>.
        /// </remarks>
        public bool UndoCompletedTube { get; private set; }

        /// <param name="board">The board this command acts on.</param>
        /// <param name="move">A move previously validated by <see cref="MoveRules"/>.</param>
        public MoveCommand(BoardState board, MoveInfo move)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));

            if (move.Count < 1)
            {
                throw new ArgumentException("A move must carry at least one ball.", nameof(move));
            }

            FromIndex = move.FromIndex;
            ToIndex = move.ToIndex;
            Count = move.Count;
            ColorId = move.ColorId;
            IsPartial = move.IsPartial;
        }

        /// <summary>Applies the move and records whether it completed the target tube.</summary>
        /// <exception cref="InvalidOperationException">If already executed and not yet undone.</exception>
        public void Execute()
        {
            if (_isExecuted)
            {
                throw new InvalidOperationException("This move has already been executed.");
            }

            _board.ApplyMove(FromIndex, ToIndex, Count);
            CompletedTube = _board[ToIndex].IsCompleted;
            UndoCompletedTube = false;
            _isExecuted = true;
        }

        /// <summary>
        /// Returns the balls to the source tube. Reveal state is left untouched by design.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the move is not currently executed.</exception>
        public void Undo()
        {
            if (!_isExecuted)
            {
                throw new InvalidOperationException("This move is not currently executed.");
            }

            UndoCompletedTube = _board.RevertMove(FromIndex, ToIndex, Count);
            _isExecuted = false;
        }

        public override string ToString() =>
            $"{Count}x color {ColorId}: tube {FromIndex} -> tube {ToIndex}" +
            $"{(IsPartial ? " (partial)" : string.Empty)}{(CompletedTube ? " [completed]" : string.Empty)}";
    }
}
