using System;
using System.Collections.Generic;
using BallSort.Core.Model;
using BallSort.Core.Rules;

namespace BallSort.Core.Commands
{
    /// <summary>
    /// The move log for one level, and the authority on which moves can still be undone.
    /// </summary>
    /// <remarks>
    /// A completed tube is corked for good, so a move that completes one can never be undone. Because
    /// undo is last-in-first-out, such a move <em>seals</em> everything up to and including itself:
    /// the player can still undo moves made after the completion, but can never reach back past it.
    /// That is what <see cref="_sealedDepth"/> tracks. An undo that completes a tube seals the same way.
    /// <para>
    /// This class knows nothing about booster counts; spending an Undo booster is
    /// <c>LevelSession</c>'s job. There is deliberately no "undo everything" operation either —
    /// restarting a level reloads it, so the hidden balls go back to being hidden.
    /// </para>
    /// </remarks>
    public sealed class CommandHistory
    {
        private readonly List<MoveCommand> _commands = new List<MoveCommand>();

        /// <summary>
        /// Number of leading commands that can never be undone. Equal to the depth of the history
        /// immediately after the most recent tube completion.
        /// </summary>
        private int _sealedDepth;

        /// <summary>Total moves made this level, including sealed ones. The player's move counter.</summary>
        public int MoveCount => _commands.Count;

        /// <summary>True when <see cref="TryUndo"/> would succeed.</summary>
        public bool CanUndo => UndoBlockReason == UndoBlockReason.None;

        /// <summary>
        /// Why the history cannot undo, or <see cref="UndoBlockReason.None"/> when it can. Only ever
        /// <see cref="UndoBlockReason.NoMovesToUndo"/> or <see cref="UndoBlockReason.SealedByTubeCompletion"/>;
        /// the booster and stuck reasons are added by <c>LevelSession</c>.
        /// </summary>
        public UndoBlockReason UndoBlockReason
        {
            get
            {
                if (_commands.Count == 0)
                {
                    return UndoBlockReason.NoMovesToUndo;
                }

                if (_commands.Count <= _sealedDepth)
                {
                    return UndoBlockReason.SealedByTubeCompletion;
                }

                return UndoBlockReason.None;
            }
        }

        /// <summary>The most recent move, sealed or not; null when no moves have been made.</summary>
        public MoveCommand LastMove => _commands.Count > 0 ? _commands[_commands.Count - 1] : null;

        /// <summary>
        /// Validates a move against <see cref="MoveRules"/>, applies it, and records it.
        /// </summary>
        /// <param name="board">The board to move on.</param>
        /// <param name="fromIndex">Index of the tube to take from.</param>
        /// <param name="toIndex">Index of the tube to place into.</param>
        /// <param name="command">The recorded command when this returns true; null otherwise.</param>
        /// <param name="rejection">Why the move was refused when this returns false.</param>
        /// <returns>True when the move was legal and has been applied.</returns>
        public bool TryExecuteMove(
            BoardState board,
            int fromIndex,
            int toIndex,
            out MoveCommand command,
            out MoveRejection rejection)
        {
            if (!MoveRules.TryGetMove(board, fromIndex, toIndex, out MoveInfo move, out rejection))
            {
                command = null;
                return false;
            }

            command = new MoveCommand(board, move);
            Execute(command);
            return true;
        }

        /// <summary>Convenience overload for callers that do not need the refusal reason.</summary>
        public bool TryExecuteMove(BoardState board, int fromIndex, int toIndex, out MoveCommand command) =>
            TryExecuteMove(board, fromIndex, toIndex, out command, out _);

        /// <summary>
        /// Executes an already-built command and pushes it onto the history.
        /// </summary>
        /// <remarks>
        /// If the move completes a tube, the history is sealed at the new depth — see the class remarks.
        /// </remarks>
        public void Execute(MoveCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.Execute();
            _commands.Add(command);

            if (command.CompletedTube)
            {
                _sealedDepth = _commands.Count;
            }
        }

        /// <summary>
        /// Undoes the most recent move, if it is not behind the seal.
        /// </summary>
        /// <remarks>
        /// If the undo itself completes a tube, the history is sealed at the new depth.
        /// </remarks>
        /// <returns>False, changing nothing, when <see cref="CanUndo"/> is false.</returns>
        public bool TryUndo()
        {
            if (!CanUndo)
            {
                return false;
            }

            int last = _commands.Count - 1;
            MoveCommand command = _commands[last];
            _commands.RemoveAt(last);
            command.Undo();

            if (command.UndoCompletedTube)
            {
                _sealedDepth = _commands.Count;
            }

            return true;
        }

        /// <summary>
        /// Drops the whole log and lifts the seal, without touching the board. Call this when loading
        /// or restarting a level, alongside building a fresh <see cref="BoardState"/>.
        /// </summary>
        public void Clear()
        {
            _commands.Clear();
            _sealedDepth = 0;
        }
    }
}
