using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Tubes are only ever appended (<see cref="AddTube"/>), never removed or reordered, so a tube's
    /// index stays valid for the life of the board and recorded moves never point at the wrong tube.
    /// </para>
    /// <para>
    /// After every move and undo the board settles its covers (see <c>docs/mechanics.md</c>): a
    /// completion opens the veils of its colour, then every key that is now part of a selectable run
    /// is found and opens its lock, repeated until nothing changes. Like reveals, opened covers and
    /// found keys are never put back by an undo.
    /// </para>
    /// <para>
    /// All mutation finishes before the first event fires, so subscribers always observe the final
    /// board. Events then fire in this order: <see cref="MoveApplied"/>, <see cref="MoveUndone"/> or
    /// <see cref="TubeShuffled"/>,
    /// <see cref="BallRevealed"/>, <see cref="TubeCompleted"/>, each <see cref="TubeUnveiled"/>, each
    /// <see cref="KeyFound"/> followed by its <see cref="LockOpened"/>, and <see cref="LevelCompleted"/>.
    /// Subscribers must not change the board from inside an event.
    /// </para>
    /// <para>
    /// <see cref="Clone"/> deliberately does not copy subscribers, so solver copies are silent and
    /// cannot leak into the view layer.
    /// </para>
    /// </remarks>
    public sealed class BoardState
    {
        private static readonly IReadOnlyList<int> NoTubes = Array.AsReadOnly(new int[0]);

        private readonly List<Tube> _tubes;

        // Balls are never created or destroyed, so the per-colour totals are fixed for the board's
        // life. Counted once and shared with clones: completion checks are the solver's hot path.
        private readonly int[] _ballsPerColor;

        // Which tubes each lock covered at the start; fixed, so also shared with clones. Read-only
        // because the lists go out in LockOpened events.
        private readonly Dictionary<int, ReadOnlyCollection<int>> _lockTubes;

        // Cover changes of the operation in progress, raised once the board is final. Reused so a
        // move allocates nothing.
        private readonly List<CoverChange> _coverChanges = new List<CoverChange>();

        private int _keysRemaining;

        /// <summary>Tubes in board order; the index is a tube's stable identity within the level.</summary>
        public IReadOnlyList<Tube> Tubes => _tubes;

        public int TubeCount => _tubes.Count;

        /// <summary>
        /// Number of distinct colours the level uses, derived as <c>max colour id + 1</c>. Zero for a
        /// board with no balls.
        /// </summary>
        public int ColorCount => _ballsPerColor.Length;

        /// <summary>
        /// True when every tube is either empty or completed. A covered tube that holds balls must be
        /// uncovered and completed first; a covered empty tube counts as empty.
        /// </summary>
        public bool IsWon
        {
            get
            {
                for (int i = 0; i < _tubes.Count; i++)
                {
                    if (!_tubes[i].IsEmpty && !IsCompleted(_tubes[i]))
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

        /// <summary>
        /// Raised when a tube becomes completed: normally by a move, but also by an undo that puts a
        /// run back onto balls revealed in the meantime.
        /// </summary>
        public event Action<TubeCompletedEventArgs> TubeCompleted;

        /// <summary>Raised for each tube whose veil opened because a tube completed in the veil's colour.</summary>
        public event Action<TubeUnveiledEventArgs> TubeUnveiled;

        /// <summary>Raised when a key's ball becomes part of a selectable run. Its <see cref="LockOpened"/> follows.</summary>
        public event Action<KeyFoundEventArgs> KeyFound;

        /// <summary>Raised when a found key opens its lock, uncovering every tube the lock covered.</summary>
        public event Action<LockOpenedEventArgs> LockOpened;

        /// <summary>Raised when a move or an undo leaves the board in a won state.</summary>
        public event Action LevelCompleted;

        /// <summary>Raised after a move has been undone. Revealed balls stay revealed.</summary>
        public event Action<MoveEventArgs> MoveUndone;

        /// <summary>
        /// Raised after the Shuffle booster rearranged one tube. Like a move, it may be followed by
        /// <see cref="BallRevealed"/> (the new top), <see cref="TubeCompleted"/>, cover changes and
        /// <see cref="LevelCompleted"/>.
        /// </summary>
        public event Action<TubeShuffledEventArgs> TubeShuffled;

        /// <param name="tubes">The level's tubes, in board order. At least one, none null.</param>
        public BoardState(IEnumerable<Tube> tubes)
        {
            if (tubes == null)
            {
                throw new ArgumentNullException(nameof(tubes));
            }

            _tubes = new List<Tube>(tubes);
            if (_tubes.Count == 0)
            {
                throw new ArgumentException("A board needs at least one tube.", nameof(tubes));
            }

            for (int i = 0; i < _tubes.Count; i++)
            {
                if (_tubes[i] == null)
                {
                    throw new ArgumentException($"Tube at index {i} is null.", nameof(tubes));
                }
            }

            _ballsPerColor = CountBallsPerColor(_tubes, out _keysRemaining);
            _lockTubes = MapLockTubes(_tubes);
        }

        // Clone path: the fixed per-level tables are shared, not recounted.
        private BoardState(
            List<Tube> tubes, int[] ballsPerColor, Dictionary<int, ReadOnlyCollection<int>> lockTubes, int keysRemaining)
        {
            _tubes = tubes;
            _ballsPerColor = ballsPerColor;
            _lockTubes = lockTubes;
            _keysRemaining = keysRemaining;
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
                        nameof(tubeIndex), tubeIndex, $"Board has {_tubes.Count} tube(s).");
                }

                return _tubes[tubeIndex];
            }
        }

        public bool IsValidTubeIndex(int tubeIndex) => tubeIndex >= 0 && tubeIndex < _tubes.Count;

        /// <summary>How many balls of <paramref name="colorId"/> the board holds in total; zero for an unused colour.</summary>
        public int GetBallCount(int colorId) =>
            colorId >= 0 && colorId < _ballsPerColor.Length ? _ballsPerColor[colorId] : 0;

        /// <summary>
        /// True when the tube is finished and corked: full, every ball revealed and of one colour, and
        /// holding every ball of that colour on the board.
        /// </summary>
        /// <remarks>
        /// The last condition is what stops four of ten purples in a capacity-4 tube from counting:
        /// those balls can still be moved on. A full single-colour tube that conceals a ball is not
        /// complete either — the player cannot verify it, so it must not be corked behind their back.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="tubeIndex"/> is not a valid tube index.</exception>
        public bool IsCompleted(int tubeIndex) => IsCompleted(this[tubeIndex]);

        /// <summary>
        /// An independent copy of the board, with no event subscribers. Intended for the solver.
        /// </summary>
        public BoardState Clone()
        {
            var clones = new List<Tube>(_tubes.Count);
            for (int i = 0; i < _tubes.Count; i++)
            {
                clones.Add(_tubes[i].Clone());
            }

            return new BoardState(clones, _ballsPerColor, _lockTubes, _keysRemaining);
        }

        /// <summary>Keys not found yet. A found key never comes back, so this only goes down.</summary>
        internal int KeysRemaining => _keysRemaining;

        /// <summary>True when some key sits in a selectable run, i.e. would be found by the next settle.</summary>
        internal bool HasFindableKey => _keysRemaining > 0 && TryFindKey(out _, out _);

        /// <summary>
        /// Overwrites every tube's balls and cover with <paramref name="source"/>'s, without allocating
        /// and without raising events. Both boards must come from the same level (one cloned from
        /// the other), so their tubes match in count, capacity and clay flag.
        /// </summary>
        /// <remarks>
        /// Lets the solver reuse two boards for a whole search instead of cloning per move, and lets
        /// a shuffle found on a copy be written back into the live board.
        /// </remarks>
        internal void CopyStateFrom(BoardState source)
        {
            if (source._tubes.Count != _tubes.Count)
            {
                throw new ArgumentException(
                    $"Boards differ in tube count ({source._tubes.Count} vs {_tubes.Count}).", nameof(source));
            }

            for (int i = 0; i < _tubes.Count; i++)
            {
                _tubes[i].CopyStateFrom(source._tubes[i]);
            }

            _keysRemaining = source._keysRemaining;
        }

        /// <summary>Recounts unfound keys after the solver has written tube contents directly.</summary>
        internal void RecountKeys()
        {
            int keys = 0;
            foreach (Tube tube in _tubes)
            {
                for (int slot = 0; slot < tube.Count; slot++)
                {
                    if (tube[slot].HasKey)
                    {
                        keys++;
                    }
                }
            }

            _keysRemaining = keys;
        }

        /// <summary>
        /// Moves <paramref name="count"/> balls from one tube to another, reveals the source's new
        /// top ball, settles the covers, then announces what happened.
        /// </summary>
        /// <remarks>Assumes the move is legal; callers go through <c>MoveRules</c> first.</remarks>
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
            bool completed = IsCompleted(target);
            SettleCovers(completed ? colorId : -1);

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

            RaiseCoverChanges();

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
        /// <para>
        /// Because of that, an undo can <em>complete</em> the source tube: if the move revealed a
        /// same-coloured ball, putting the run back fills a single-colour tube with nothing hidden.
        /// The tube is then corked exactly as if a move had completed it, and opens veils the same way.
        /// </para>
        /// </remarks>
        /// <returns>True when the undo completed the source tube.</returns>
        internal bool RevertMove(int fromIndex, int toIndex, int count)
        {
            Tube source = _tubes[fromIndex];
            Tube target = _tubes[toIndex];
            int colorId = target.Top.ColorId;

            for (int i = 0; i < count; i++)
            {
                source.Push(target.Pop());
            }

            bool completed = IsCompleted(source);
            SettleCovers(completed ? colorId : -1);

            MoveUndone?.Invoke(new MoveEventArgs(fromIndex, toIndex, count, colorId));

            if (completed)
            {
                TubeCompleted?.Invoke(new TubeCompletedEventArgs(fromIndex, colorId));
            }

            RaiseCoverChanges();

            // Only a completion can win: the source now holds balls, so it must be finished.
            if (completed && IsWon)
            {
                LevelCompleted?.Invoke();
            }

            return completed;
        }

        /// <summary>
        /// Writes a shuffled order into one tube, reveals its new top ball, settles the covers (a key
        /// now in the top run is found and opens its lock), then announces what happened.
        /// </summary>
        /// <remarks>
        /// The order comes from <c>ShuffleRules.Arrange</c>: the tube's own balls, so the colour totals
        /// are unchanged. The tube's balls may all be one colour with one of them hidden, so revealing
        /// the top can complete it, and that completion opens veils and can win, as after a move.
        /// </remarks>
        /// <param name="tubeIndex">The shuffled tube.</param>
        /// <param name="fromSlots">
        /// The new order, bottom → top, as the slot each ball comes from: a permutation of the tube's slots.
        /// </param>
        internal void ShuffleTube(int tubeIndex, int[] fromSlots)
        {
            Tube tube = _tubes[tubeIndex];
            if (fromSlots == null || fromSlots.Length != tube.Count)
            {
                throw new ArgumentException($"Tube {tubeIndex} holds {tube.Count} ball(s).", nameof(fromSlots));
            }

            var before = new Ball[tube.Count];
            var used = new bool[tube.Count];
            for (int slot = 0; slot < tube.Count; slot++)
            {
                before[slot] = tube[slot];
                int from = fromSlots[slot];
                if (from < 0 || from >= tube.Count || used[from])
                {
                    throw new ArgumentException($"Not a permutation of tube {tubeIndex}'s slots.", nameof(fromSlots));
                }

                used[from] = true;
            }

            int top = tube.Count - 1;
            bool revealed = before[fromSlots[top]].IsHidden;
            var placed = new Ball[before.Length];
            for (int slot = 0; slot <= top; slot++)
            {
                Ball ball = before[fromSlots[slot]];
                placed[slot] = slot == top ? ball.AsRevealed() : ball;
                tube.SetBall(slot, placed[slot]);
            }

            bool completed = IsCompleted(tube);
            int colorId = tube.Top.ColorId;
            SettleCovers(completed ? colorId : -1);

            TubeShuffled?.Invoke(new TubeShuffledEventArgs(
                tubeIndex, Array.AsReadOnly(placed), Array.AsReadOnly((int[])fromSlots.Clone())));

            if (revealed)
            {
                BallRevealed?.Invoke(new BallRevealedEventArgs(tubeIndex, top, colorId));
            }

            if (completed)
            {
                TubeCompleted?.Invoke(new TubeCompletedEventArgs(tubeIndex, colorId));
            }

            RaiseCoverChanges();

            if (completed && IsWon)
            {
                LevelCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Appends an empty tube at the end of the board, so every existing index stays valid.
        /// </summary>
        /// <remarks>
        /// Silent: where the tube goes on screen is the session's grid's business, so the session
        /// announces it. Callers enforce the limits (layout room, booster count).
        /// </remarks>
        /// <returns>The new tube's index.</returns>
        internal int AddTube(int capacity)
        {
            _tubes.Add(new Tube(capacity));
            return _tubes.Count - 1;
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            for (int i = 0; i < _tubes.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(' ');
                }

                text.Append(_tubes[i]);
            }

            return text.ToString();
        }

        private bool IsCompleted(Tube tube) =>
            tube.IsFull && tube.RunLength == tube.Count && _ballsPerColor[tube.Top.ColorId] == tube.Count;

        /// <summary>
        /// Opens the veils of <paramref name="completedColor"/> (none when negative), then finds keys
        /// until none is left in a selectable run. Records each change for <see cref="RaiseCoverChanges"/>.
        /// </summary>
        private void SettleCovers(int completedColor)
        {
            _coverChanges.Clear();

            if (completedColor >= 0)
            {
                for (int i = 0; i < _tubes.Count; i++)
                {
                    TubeCover cover = _tubes[i].Cover;
                    if (cover.Kind == CoverKind.Veil && cover.Id == completedColor)
                    {
                        _tubes[i].RemoveCover();
                        _coverChanges.Add(new CoverChange(CoverChangeKind.Unveiled, i, 0, completedColor));
                    }
                }
            }

            // An opened lock can uncover a tube whose run holds another key, so keep scanning.
            while (_keysRemaining > 0 && TryFindKey(out int tubeIndex, out int slotIndex))
            {
                int lockId = _tubes[tubeIndex][slotIndex].KeyLockId;
                _tubes[tubeIndex].RemoveKey(slotIndex);
                _keysRemaining--;
                _coverChanges.Add(new CoverChange(CoverChangeKind.KeyFound, tubeIndex, slotIndex, lockId));

                if (OpenLock(lockId))
                {
                    _coverChanges.Add(new CoverChange(CoverChangeKind.LockOpened, 0, 0, lockId));
                }
            }
        }

        /// <summary>
        /// The first key, by tube index then from the top down, whose ball is part of a selectable
        /// run: the top run of a tube that is neither covered nor clay.
        /// </summary>
        private bool TryFindKey(out int tubeIndex, out int slotIndex)
        {
            for (int t = 0; t < _tubes.Count; t++)
            {
                Tube tube = _tubes[t];
                if (tube.IsCovered || tube.IsClay)
                {
                    continue;
                }

                int runStart = tube.Count - tube.RunLength;
                for (int slot = tube.Count - 1; slot >= runStart; slot--)
                {
                    if (tube[slot].HasKey)
                    {
                        tubeIndex = t;
                        slotIndex = slot;
                        return true;
                    }
                }
            }

            tubeIndex = -1;
            slotIndex = -1;
            return false;
        }

        /// <returns>True when at least one tube was still covered by the lock.</returns>
        private bool OpenLock(int lockId)
        {
            bool opened = false;
            IReadOnlyList<int> tubes = GetLockTubes(lockId);
            for (int i = 0; i < tubes.Count; i++)
            {
                int tubeIndex = tubes[i];
                if (_tubes[tubeIndex].Cover == TubeCover.Lock(lockId))
                {
                    _tubes[tubeIndex].RemoveCover();
                    opened = true;
                }
            }

            return opened;
        }

        private IReadOnlyList<int> GetLockTubes(int lockId) =>
            _lockTubes.TryGetValue(lockId, out ReadOnlyCollection<int> tubes) ? tubes : NoTubes;

        private void RaiseCoverChanges()
        {
            for (int i = 0; i < _coverChanges.Count; i++)
            {
                CoverChange change = _coverChanges[i];
                switch (change.Kind)
                {
                    case CoverChangeKind.Unveiled:
                        TubeUnveiled?.Invoke(new TubeUnveiledEventArgs(change.TubeIndex, change.Id));
                        break;
                    case CoverChangeKind.KeyFound:
                        KeyFound?.Invoke(new KeyFoundEventArgs(change.TubeIndex, change.SlotIndex, change.Id));
                        break;
                    case CoverChangeKind.LockOpened:
                        LockOpened?.Invoke(new LockOpenedEventArgs(change.Id, GetLockTubes(change.Id)));
                        break;
                }
            }

            _coverChanges.Clear();
        }

        private static int[] CountBallsPerColor(List<Tube> tubes, out int keys)
        {
            int maxColorId = -1;
            foreach (Tube tube in tubes)
            {
                for (int slot = 0; slot < tube.Count; slot++)
                {
                    maxColorId = Math.Max(maxColorId, tube[slot].ColorId);
                }
            }

            var counts = new int[maxColorId + 1];
            keys = 0;
            foreach (Tube tube in tubes)
            {
                for (int slot = 0; slot < tube.Count; slot++)
                {
                    counts[tube[slot].ColorId]++;
                    if (tube[slot].HasKey)
                    {
                        keys++;
                    }
                }
            }

            return counts;
        }

        private static Dictionary<int, ReadOnlyCollection<int>> MapLockTubes(List<Tube> tubes)
        {
            var lists = new Dictionary<int, List<int>>();
            for (int i = 0; i < tubes.Count; i++)
            {
                TubeCover cover = tubes[i].Cover;
                if (cover.Kind != CoverKind.Lock)
                {
                    continue;
                }

                if (!lists.TryGetValue(cover.Id, out List<int> list))
                {
                    list = new List<int>();
                    lists.Add(cover.Id, list);
                }

                list.Add(i);
            }

            var map = new Dictionary<int, ReadOnlyCollection<int>>(lists.Count);
            foreach (KeyValuePair<int, List<int>> entry in lists)
            {
                map.Add(entry.Key, entry.Value.AsReadOnly());
            }

            return map;
        }

        private enum CoverChangeKind
        {
            Unveiled,
            KeyFound,
            LockOpened,
        }

        /// <summary>One recorded cover change; <see cref="Id"/> is the veil colour or the lock id.</summary>
        private readonly struct CoverChange
        {
            public CoverChangeKind Kind { get; }
            public int TubeIndex { get; }
            public int SlotIndex { get; }
            public int Id { get; }

            public CoverChange(CoverChangeKind kind, int tubeIndex, int slotIndex, int id)
            {
                Kind = kind;
                TubeIndex = tubeIndex;
                SlotIndex = slotIndex;
                Id = id;
            }
        }
    }
}
