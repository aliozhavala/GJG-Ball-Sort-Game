using System;
using System.Collections.Generic;
using BallSort.Core.Commands;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using BallSort.Core.Player;
using BallSort.Core.Rules;

namespace BallSort.Core.Session
{
    /// <summary>
    /// One play-through of one level, and the single gameplay entry point: input, HUD and boosters
    /// talk to this class only, never to the board or the history directly.
    /// </summary>
    /// <remarks>
    /// Wraps the live <see cref="BoardState"/>, its <see cref="CommandHistory"/> and the player's
    /// global <see cref="BoosterInventory"/>, and owns the rules that span them: booster spending,
    /// the stuck state, added tubes and restart.
    /// <para>
    /// Restart builds a brand-new <see cref="BoardState"/>, so the session re-raises every board
    /// event as its own. Subscribers attach once, to the session, and survive restarts; they
    /// rebuild their view of the board on <see cref="Restarted"/>.
    /// </para>
    /// <para>
    /// The stuck state is re-evaluated after every move, undo, added tube and restart, and
    /// <see cref="StuckChanged"/> fires after that operation's board events.
    /// </para>
    /// </remarks>
    public sealed class LevelSession
    {
        private readonly LoadedLevel _level;
        private readonly BoosterInventory _boosters;
        private readonly int _addedTubeCapacity;
        private readonly CommandHistory _history = new CommandHistory();

        private BoardState _board;
        private int[] _rows;

        /// <summary>The level being played; its <see cref="LoadedLevel.Source"/> is what restart rebuilds from.</summary>
        public LoadedLevel Level => _level;

        /// <summary>
        /// The live board. Replaced by <see cref="Restart"/>, so do not cache it across a
        /// <see cref="Restarted"/> event. Read-only from outside Core: its mutators are internal.
        /// </summary>
        public BoardState Board => _board;

        /// <summary>Tube counts per row, top row first, including any added tubes.</summary>
        public IReadOnlyList<int> Rows => _rows;

        /// <summary>Moves made since the level started or was restarted, including sealed ones.</summary>
        public int MoveCount => _history.MoveCount;

        /// <summary>True when every tube is either empty or completed.</summary>
        public bool IsWon => _board.IsWon;

        /// <summary>True when the level is not won and no legal move exists.</summary>
        public bool IsStuck { get; private set; }

        /// <summary>Why undo is unavailable, or <see cref="Commands.UndoBlockReason.None"/>.</summary>
        /// <remarks>
        /// History reasons outrank the booster count: telling the player they are out of undos
        /// when a corked tube is the real obstacle would send them looking for a booster that
        /// cannot help.
        /// </remarks>
        public UndoBlockReason UndoBlockReason
        {
            get
            {
                if (IsStuck)
                {
                    return UndoBlockReason.Stuck;
                }

                UndoBlockReason historyReason = _history.UndoBlockReason;
                if (historyReason != UndoBlockReason.None)
                {
                    return historyReason;
                }

                return _boosters.GetCount(BoosterType.Undo) > 0
                    ? UndoBlockReason.None
                    : UndoBlockReason.NoBoostersLeft;
            }
        }

        /// <summary>Why add tube is unavailable, or <see cref="Session.AddTubeBlockReason.None"/>.</summary>
        public AddTubeBlockReason AddTubeBlockReason
        {
            get
            {
                if (IsWon)
                {
                    return AddTubeBlockReason.LevelWon;
                }

                if (IsStuck)
                {
                    return AddTubeBlockReason.Stuck;
                }

                if (!LevelLayout.CanAppendTube(_rows))
                {
                    return AddTubeBlockReason.NoRoom;
                }

                return _boosters.GetCount(BoosterType.AddTube) > 0
                    ? AddTubeBlockReason.None
                    : AddTubeBlockReason.NoBoostersLeft;
            }
        }

        /// <summary>
        /// Why shuffle is unavailable. Until the solver exists this is never
        /// <see cref="Session.ShuffleBlockReason.None"/>.
        /// </summary>
        public ShuffleBlockReason ShuffleBlockReason
        {
            get
            {
                if (IsWon)
                {
                    return ShuffleBlockReason.LevelWon;
                }

                if (IsStuck)
                {
                    return ShuffleBlockReason.Stuck;
                }

                // Checked before the booster count: "coming soon" is the honest answer even to a
                // player who has run out.
                return ShuffleBlockReason.NotAvailable;
            }
        }

        /// <summary>Forwarded from the board: a move has been applied.</summary>
        public event Action<MoveEventArgs> MoveApplied;

        /// <summary>Forwarded from the board: a hidden ball has been revealed.</summary>
        public event Action<BallRevealedEventArgs> BallRevealed;

        /// <summary>Forwarded from the board: a tube has been completed, by a move or an undo.</summary>
        public event Action<TubeCompletedEventArgs> TubeCompleted;

        /// <summary>Forwarded from the board: the level has been won.</summary>
        public event Action LevelCompleted;

        /// <summary>Forwarded from the board: a move has been undone.</summary>
        public event Action<MoveEventArgs> MoveUndone;

        /// <summary>
        /// Forwarded from the board: an empty tube has been appended. <see cref="Rows"/> already
        /// includes it when this fires.
        /// </summary>
        public event Action<TubeAddedEventArgs> TubeAdded;

        /// <summary>Raised when <see cref="IsStuck"/> changes, with its new value.</summary>
        public event Action<bool> StuckChanged;

        /// <summary>
        /// Raised after <see cref="Restart"/> has swapped in a fresh board. Views rebuild from
        /// <see cref="Board"/> and <see cref="Rows"/>.
        /// </summary>
        public event Action Restarted;

        /// <param name="level">A freshly loaded level; the session plays on its board.</param>
        /// <param name="boosters">The player's global inventory; boosters spent here are spent for good.</param>
        /// <param name="addedTubeCapacity">Slots in each tube the Add-tube booster appends.</param>
        public LevelSession(LoadedLevel level, BoosterInventory boosters, int addedTubeCapacity)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _boosters = boosters ?? throw new ArgumentNullException(nameof(boosters));

            if (addedTubeCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(addedTubeCapacity), addedTubeCapacity, "An added tube needs at least one slot.");
            }

            _addedTubeCapacity = addedTubeCapacity;
            _board = level.Board;
            _rows = CopyRows(level.Rows);
            Subscribe(_board);
            IsStuck = ComputeStuck();
        }

        /// <summary>
        /// Moves the run on top of one tube onto another, if the rules allow it.
        /// </summary>
        /// <param name="fromIndex">Index of the tube to take from.</param>
        /// <param name="toIndex">Index of the tube to place into.</param>
        /// <param name="rejection">Why the move was refused, when this returns false.</param>
        /// <returns>True when the move has been applied.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If either index is not a valid tube index.</exception>
        public bool TryMove(int fromIndex, int toIndex, out MoveRejection rejection)
        {
            if (!_history.TryExecuteMove(_board, fromIndex, toIndex, out _, out rejection))
            {
                return false;
            }

            RefreshStuck();
            return true;
        }

        /// <summary>
        /// Undoes the most recent move and spends one Undo booster, if <see cref="UndoBlockReason"/> allows.
        /// </summary>
        /// <param name="reason">Why undo was refused, when this returns false.</param>
        /// <returns>True when a move has been undone.</returns>
        public bool TryUndo(out UndoBlockReason reason)
        {
            reason = UndoBlockReason;
            if (reason != UndoBlockReason.None)
            {
                return false;
            }

            _history.TryUndo();
            _boosters.TrySpend(BoosterType.Undo);
            RefreshStuck();
            return true;
        }

        /// <summary>
        /// Appends an empty tube and spends one Add-tube booster, if <see cref="AddTubeBlockReason"/> allows.
        /// </summary>
        /// <remarks>
        /// Not recorded in the history: it cannot be undone and does not seal. The tube joins the
        /// last row, or starts a new one — see <see cref="LevelLayout.TryAppendTube"/>.
        /// </remarks>
        /// <param name="reason">Why the tube was not added, when this returns false.</param>
        /// <returns>True when a tube has been added.</returns>
        public bool TryAddTube(out AddTubeBlockReason reason)
        {
            reason = AddTubeBlockReason;
            if (reason != AddTubeBlockReason.None)
            {
                return false;
            }

            _boosters.TrySpend(BoosterType.AddTube);
            AppendTube();
            return true;
        }

        /// <summary>
        /// Shuffles the board and spends one Shuffle booster. Not implemented until the solver
        /// exists, so this always refuses — see <see cref="ShuffleBlockReason"/>.
        /// </summary>
        /// <param name="reason">Why the shuffle was refused.</param>
        /// <returns>Always false for now.</returns>
        public bool TryShuffle(out ShuffleBlockReason reason)
        {
            reason = ShuffleBlockReason;
            return false;
        }

        /// <summary>
        /// Reloads the level from its source data: hidden balls are hidden again, added tubes are
        /// gone and the history is cleared. Boosters already spent stay spent.
        /// </summary>
        public void Restart()
        {
            Unsubscribe(_board);
            _board = LevelFactory.CreateBoard(_level.Source);
            _rows = CopyRows(_level.Rows);
            _history.Clear();
            Subscribe(_board);

            Restarted?.Invoke();
            RefreshStuck();
        }

        private void AppendTube()
        {
            // Rows first, so TubeAdded subscribers already see the new layout.
            LevelLayout.TryAppendTube(_rows, out int[] rows);
            _rows = rows;
            _board.AddTube(_addedTubeCapacity);
            RefreshStuck();
        }

        private bool ComputeStuck() => !_board.IsWon && !MoveRules.HasAnyLegalMove(_board);

        private void RefreshStuck()
        {
            bool stuck = ComputeStuck();
            if (stuck == IsStuck)
            {
                return;
            }

            IsStuck = stuck;
            StuckChanged?.Invoke(stuck);
        }

        private static int[] CopyRows(IReadOnlyList<int> rows)
        {
            var copy = new int[rows.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = rows[i];
            }

            return copy;
        }

        private void Subscribe(BoardState board)
        {
            board.MoveApplied += OnMoveApplied;
            board.BallRevealed += OnBallRevealed;
            board.TubeCompleted += OnTubeCompleted;
            board.LevelCompleted += OnLevelCompleted;
            board.MoveUndone += OnMoveUndone;
            board.TubeAdded += OnTubeAdded;
        }

        private void Unsubscribe(BoardState board)
        {
            board.MoveApplied -= OnMoveApplied;
            board.BallRevealed -= OnBallRevealed;
            board.TubeCompleted -= OnTubeCompleted;
            board.LevelCompleted -= OnLevelCompleted;
            board.MoveUndone -= OnMoveUndone;
            board.TubeAdded -= OnTubeAdded;
        }

        private void OnMoveApplied(MoveEventArgs e) => MoveApplied?.Invoke(e);

        private void OnBallRevealed(BallRevealedEventArgs e) => BallRevealed?.Invoke(e);

        private void OnTubeCompleted(TubeCompletedEventArgs e) => TubeCompleted?.Invoke(e);

        private void OnLevelCompleted() => LevelCompleted?.Invoke();

        private void OnMoveUndone(MoveEventArgs e) => MoveUndone?.Invoke(e);

        private void OnTubeAdded(TubeAddedEventArgs e) => TubeAdded?.Invoke(e);
    }
}
