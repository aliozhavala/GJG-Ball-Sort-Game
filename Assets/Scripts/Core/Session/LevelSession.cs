using System;
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
    /// the stuck state, added tubes, shuffle and restart.
    /// <para>
    /// Restart builds a brand-new <see cref="BoardState"/>, so the session re-raises every board
    /// event as its own. Subscribers attach once, to the session, and survive restarts; they
    /// rebuild their view of the board on <see cref="Restarted"/>.
    /// </para>
    /// <para>
    /// The stuck state (<see cref="StuckDetector"/>) is re-evaluated after every move, undo, added tube
    /// (booster or Play on), shuffle and restart.
    /// Event order within one operation:
    /// <list type="bullet">
    /// <item>Move or undo: <see cref="MoveApplied"/> or <see cref="MoveUndone"/>, <see cref="BallRevealed"/>,
    /// <see cref="TubeCompleted"/>, each <see cref="TubeUnveiled"/>, each <see cref="KeyFound"/> followed
    /// by its <see cref="LockOpened"/>, <see cref="LevelCompleted"/>.</item>
    /// <item>Added tube (booster or Play on): <see cref="TubeAdded"/>.</item>
    /// <item>Shuffle: <see cref="Shuffled"/>.</item>
    /// <item>Restart: <see cref="Restarted"/>.</item>
    /// </list>
    /// Each operation then ends with <see cref="StuckChanged"/> (only if it changed) and finally
    /// <see cref="Changed"/>, once the whole operation — board, history seal, boosters, stuck state — is done.
    /// </para>
    /// </remarks>
    public sealed class LevelSession
    {
        private readonly LoadedLevel _level;
        private readonly BoosterInventory _boosters;
        private readonly int _addedTubeCapacity;
        private readonly CommandHistory _history = new CommandHistory();

        private BoardState _board;
        private BoardGrid _grid;

        // Bumped by every change to the board, so a shuffle searched on an older copy is refused.
        private int _revision;

        /// <summary>The level being played; its <see cref="LoadedLevel.Source"/> is what restart rebuilds from.</summary>
        public LoadedLevel Level => _level;

        /// <summary>The variant being played, 1 to <see cref="LoadedLevel.VariantCount"/>. Changed only by <see cref="Restart"/>.</summary>
        public int Variant { get; private set; }

        /// <summary>
        /// The live board. Replaced by <see cref="Restart"/>, so do not cache it across a
        /// <see cref="Restarted"/> event. Read-only from outside Core: its mutators are internal.
        /// </summary>
        public BoardState Board => _board;

        /// <summary>
        /// The live grid, including any added tubes. Replaced by <see cref="Restart"/>, like
        /// <see cref="Board"/>; change it only through the session.
        /// </summary>
        public BoardGrid Grid => _grid;

        /// <summary>Moves made since the level started, was restarted or was shuffled, including sealed ones.</summary>
        public int MoveCount => _history.MoveCount;

        /// <summary>True when every tube is either empty or completed.</summary>
        public bool IsWon => _board.IsWon;

        /// <summary>
        /// True when the level is not won and nothing meaningful is left to do: no legal move, or only
        /// moves that go round in a loop (see <see cref="StuckDetector"/>).
        /// </summary>
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

                if (!_grid.CanAddTube)
                {
                    return AddTubeBlockReason.NoRoom;
                }

                return _boosters.GetCount(BoosterType.AddTube) > 0
                    ? AddTubeBlockReason.None
                    : AddTubeBlockReason.NoBoostersLeft;
            }
        }

        /// <summary>
        /// Why shuffle is unavailable, or <see cref="Session.ShuffleBlockReason.None"/>. Whether a valid
        /// arrangement exists is only known after searching, so
        /// <see cref="Session.ShuffleBlockReason.NoValidShuffle"/> never appears here.
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

                return _boosters.GetCount(BoosterType.Shuffle) > 0
                    ? ShuffleBlockReason.None
                    : ShuffleBlockReason.NoBoostersLeft;
            }
        }

        /// <summary>
        /// Why Play on is unavailable for a player holding <paramref name="wallet"/>, or
        /// <see cref="Session.PlayOnBlockReason.None"/>.
        /// </summary>
        /// <param name="wallet">The player's coins.</param>
        /// <param name="price">Coins Play on costs; not negative.</param>
        public PlayOnBlockReason GetPlayOnBlockReason(Wallet wallet, int price)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price), price, "A price cannot be negative.");
            }

            if (!IsStuck)
            {
                return PlayOnBlockReason.NotStuck;
            }

            if (!_grid.CanAddTube)
            {
                return PlayOnBlockReason.NoRoom;
            }

            return wallet.CanAfford(price) ? PlayOnBlockReason.None : PlayOnBlockReason.NotEnoughCoins;
        }

        /// <summary>Forwarded from the board: a move has been applied.</summary>
        public event Action<MoveEventArgs> MoveApplied;

        /// <summary>Forwarded from the board: a hidden ball has been revealed.</summary>
        public event Action<BallRevealedEventArgs> BallRevealed;

        /// <summary>Forwarded from the board: a tube has been completed, by a move or an undo.</summary>
        public event Action<TubeCompletedEventArgs> TubeCompleted;

        /// <summary>Forwarded from the board: a veil has opened because a tube completed in its colour.</summary>
        public event Action<TubeUnveiledEventArgs> TubeUnveiled;

        /// <summary>Forwarded from the board: a key has been found; its <see cref="LockOpened"/> follows.</summary>
        public event Action<KeyFoundEventArgs> KeyFound;

        /// <summary>Forwarded from the board: a lock has opened, uncovering all of its tubes.</summary>
        public event Action<LockOpenedEventArgs> LockOpened;

        /// <summary>Forwarded from the board: the level has been won.</summary>
        public event Action LevelCompleted;

        /// <summary>Forwarded from the board: a move has been undone.</summary>
        public event Action<MoveEventArgs> MoveUndone;

        /// <summary>
        /// An empty tube has been appended to the board and placed on the grid; the payload says which
        /// row and cell it went to. <see cref="Board"/> and <see cref="Grid"/> already include it.
        /// </summary>
        public event Action<TubeAddedEventArgs> TubeAdded;

        /// <summary>
        /// The balls of the open tubes have been rearranged and the undo history cleared. Tubes,
        /// covers and indices are unchanged; views refresh the balls from <see cref="Board"/>.
        /// </summary>
        public event Action Shuffled;

        /// <summary>Raised when <see cref="IsStuck"/> changes, with its new value.</summary>
        public event Action<bool> StuckChanged;

        /// <summary>
        /// Raised once after every successful move, undo, added tube (booster or Play on), shuffle
        /// and restart, when all of the session's state is final.
        /// </summary>
        /// <remarks>
        /// Board events fire mid-operation: during <see cref="MoveApplied"/> the history has not
        /// yet recorded the move or its completion seal, so <see cref="UndoBlockReason"/> is stale. Anything
        /// that shows derived state (booster availability, counts) should refresh on this instead.
        /// </remarks>
        public event Action Changed;

        /// <summary>
        /// Raised after <see cref="Restart"/> has swapped in a fresh board. Views rebuild from
        /// <see cref="Board"/> and <see cref="Grid"/>.
        /// </summary>
        public event Action Restarted;

        /// <param name="level">A loaded level.</param>
        /// <param name="variant">The variant to play, 1 to <see cref="LoadedLevel.VariantCount"/>; the try rules pick it (see <see cref="VariantSelector"/>).</param>
        /// <param name="boosters">The player's global inventory; boosters spent here are spent for good.</param>
        /// <param name="addedTubeCapacity">Slots in each tube the Add-tube booster appends.</param>
        public LevelSession(LoadedLevel level, int variant, BoosterInventory boosters, int addedTubeCapacity)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _boosters = boosters ?? throw new ArgumentNullException(nameof(boosters));

            if (addedTubeCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(addedTubeCapacity), addedTubeCapacity, "An added tube needs at least one slot.");
            }

            _addedTubeCapacity = addedTubeCapacity;
            _board = level.CreateBoard(variant);
            _grid = level.CreateGrid();
            Variant = variant;
            Subscribe(_board);
            IsStuck = ComputeStuck();
        }

        /// <summary>
        /// Whether the tube can start a move: it has a revealed ball on top and is not completed,
        /// covered or clay. Input uses this to decide whether a tap selects the tube.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the index is not a valid tube index.</exception>
        public bool CanSelect(int tubeIndex) => MoveRules.CanSelect(_board, tubeIndex);

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

            _revision++;
            RefreshStuck();
            Changed?.Invoke();
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
            _revision++;
            RefreshStuck();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Appends an empty tube and spends one Add-tube booster, if <see cref="AddTubeBlockReason"/> allows.
        /// </summary>
        /// <remarks>
        /// Not recorded in the history: it cannot be undone and does not seal. The tube joins the
        /// row with the fewest cells — see <see cref="BoardGrid.TryAddTube"/>.
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
        /// The paid way out of a dead end: spends <paramref name="price"/> coins and appends an empty
        /// tube, which gives a stuck board new moves; the stuck state is then evaluated again.
        /// </summary>
        /// <remarks>
        /// Only allowed while <see cref="IsStuck"/>. Unlike Add tube it ignores the booster
        /// inventory (it costs coins instead), but the layout limit still applies. Like Add tube it
        /// is not undoable and does not seal the history.
        /// </remarks>
        /// <param name="wallet">The player's coins; charged only when the tube is added.</param>
        /// <param name="price">Coins to charge; not negative.</param>
        /// <param name="reason">Why Play on was refused, when this returns false.</param>
        /// <returns>True when the coins were spent and a tube was added.</returns>
        public bool TryPlayOn(Wallet wallet, int price, out PlayOnBlockReason reason)
        {
            reason = GetPlayOnBlockReason(wallet, price);
            if (reason != PlayOnBlockReason.None)
            {
                return false;
            }

            wallet.TrySpend(price);
            AppendTube();
            return true;
        }

        /// <summary>
        /// Shuffles the open tubes and spends one Shuffle booster, searching on the calling thread.
        /// </summary>
        /// <remarks>
        /// The search can take a while (up to 20 solver runs), so the game uses
        /// <see cref="TryBeginShuffle"/>, <see cref="PendingShuffle.Search"/> on a worker thread and
        /// <see cref="TryApplyShuffle"/> instead; this is the same in one blocking call.
        /// </remarks>
        /// <param name="random">Source of the arrangements.</param>
        /// <param name="reason">Why nothing was shuffled, when this returns false.</param>
        /// <returns>True when the board has been shuffled.</returns>
        public bool TryShuffle(Random random, out ShuffleBlockReason reason)
        {
            if (!TryBeginShuffle(out PendingShuffle shuffle, out reason))
            {
                return false;
            }

            shuffle.Search(random);
            return TryApplyShuffle(shuffle, out reason);
        }

        /// <summary>
        /// Starts a shuffle, if <see cref="ShuffleBlockReason"/> allows: takes a private copy of the
        /// board for <see cref="PendingShuffle.Search"/>. Nothing is spent yet. Main thread only.
        /// </summary>
        /// <param name="shuffle">The shuffle to search, when this returns true.</param>
        /// <param name="reason">Why no shuffle can start, when this returns false.</param>
        public bool TryBeginShuffle(out PendingShuffle shuffle, out ShuffleBlockReason reason)
        {
            reason = ShuffleBlockReason;
            shuffle = reason == ShuffleBlockReason.None ? new PendingShuffle(this, _board.Clone(), _revision) : null;
            return shuffle != null;
        }

        /// <summary>
        /// Applies a searched shuffle: writes its arrangement into the board, clears the undo history
        /// and spends one Shuffle booster. Main thread only.
        /// </summary>
        /// <remarks>
        /// Refused, with nothing changed or spent, when the board changed since
        /// <see cref="TryBeginShuffle"/> (<see cref="Session.ShuffleBlockReason.BoardChanged"/>), when
        /// shuffling is no longer allowed, or when the search found nothing
        /// (<see cref="Session.ShuffleBlockReason.NoValidShuffle"/>). A shuffle cannot be undone.
        /// </remarks>
        /// <param name="shuffle">A shuffle begun on this session and searched.</param>
        /// <param name="reason">Why the shuffle was not applied, when this returns false.</param>
        /// <exception cref="ArgumentException">If <paramref name="shuffle"/> was begun on another session.</exception>
        public bool TryApplyShuffle(PendingShuffle shuffle, out ShuffleBlockReason reason)
        {
            if (shuffle == null)
            {
                throw new ArgumentNullException(nameof(shuffle));
            }

            if (shuffle.Session != this)
            {
                throw new ArgumentException("The shuffle was begun on another session.", nameof(shuffle));
            }

            reason = shuffle.Revision != _revision ? ShuffleBlockReason.BoardChanged : ShuffleBlockReason;
            if (reason == ShuffleBlockReason.None && !shuffle.IsFound)
            {
                reason = ShuffleBlockReason.NoValidShuffle;
            }

            if (reason != ShuffleBlockReason.None)
            {
                return false;
            }

            _board.CopyStateFrom(shuffle.Result);
            _history.Clear();
            _boosters.TrySpend(BoosterType.Shuffle);
            _revision++;

            Shuffled?.Invoke();
            RefreshStuck();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Reloads the level from its source data in <paramref name="variant"/>, the new try's
        /// variant: hidden balls are hidden again, added tubes are gone and the history is cleared.
        /// Boosters already spent stay spent. Every variant has the same tubes and grid, so only the
        /// balls and covers can differ from the board being replaced.
        /// </summary>
        /// <param name="variant">1 to <see cref="LoadedLevel.VariantCount"/>; may be the variant already played.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the level has no such variant.</exception>
        public void Restart(int variant)
        {
            BoardState board = _level.CreateBoard(variant);
            Unsubscribe(_board);
            _board = board;
            _grid = _level.CreateGrid();
            Variant = variant;
            _history.Clear();
            _revision++;
            Subscribe(_board);

            Restarted?.Invoke();
            RefreshStuck();
            Changed?.Invoke();
        }

        /// <remarks>Callers have already checked <see cref="BoardGrid.CanAddTube"/>.</remarks>
        private void AppendTube()
        {
            _grid.TryAddTube(out int row, out int cell);
            int tubeIndex = _board.AddTube(_addedTubeCapacity);
            _revision++;
            TubeAdded?.Invoke(new TubeAddedEventArgs(tubeIndex, _addedTubeCapacity, row, cell));
            RefreshStuck();
            Changed?.Invoke();
        }

        private bool ComputeStuck() => StuckDetector.IsStuck(_board);

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

        private void Subscribe(BoardState board)
        {
            board.MoveApplied += OnMoveApplied;
            board.BallRevealed += OnBallRevealed;
            board.TubeCompleted += OnTubeCompleted;
            board.TubeUnveiled += OnTubeUnveiled;
            board.KeyFound += OnKeyFound;
            board.LockOpened += OnLockOpened;
            board.LevelCompleted += OnLevelCompleted;
            board.MoveUndone += OnMoveUndone;
        }

        private void Unsubscribe(BoardState board)
        {
            board.MoveApplied -= OnMoveApplied;
            board.BallRevealed -= OnBallRevealed;
            board.TubeCompleted -= OnTubeCompleted;
            board.TubeUnveiled -= OnTubeUnveiled;
            board.KeyFound -= OnKeyFound;
            board.LockOpened -= OnLockOpened;
            board.LevelCompleted -= OnLevelCompleted;
            board.MoveUndone -= OnMoveUndone;
        }

        private void OnMoveApplied(MoveEventArgs e) => MoveApplied?.Invoke(e);

        private void OnBallRevealed(BallRevealedEventArgs e) => BallRevealed?.Invoke(e);

        private void OnTubeCompleted(TubeCompletedEventArgs e) => TubeCompleted?.Invoke(e);

        private void OnTubeUnveiled(TubeUnveiledEventArgs e) => TubeUnveiled?.Invoke(e);

        private void OnKeyFound(KeyFoundEventArgs e) => KeyFound?.Invoke(e);

        private void OnLockOpened(LockOpenedEventArgs e) => LockOpened?.Invoke(e);

        private void OnLevelCompleted() => LevelCompleted?.Invoke();

        private void OnMoveUndone(MoveEventArgs e) => MoveUndone?.Invoke(e);
    }
}
