using System;
using System.Collections.Generic;
using System.Diagnostics;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using BallSort.Core.Session;
using BallSort.Game.Layout;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Shows a <see cref="LevelSession"/>'s board from pooled tube and ball views, and keeps it in
    /// step with the session's events.
    /// </summary>
    /// <remarks>
    /// The board is built in full only when the level loads, restarts or is shuffled, reading balls,
    /// keys, covers and clay from the board state; every other event changes just the views it
    /// touches (balls change tubes, a ball turns face up, a key mark goes, a cover opens, a tube
    /// completes, a tube is appended). Tubes, balls and covers are pooled, so nothing is instantiated
    /// or destroyed once the pools are warm.
    /// <para>
    /// Covers are separate views over the tubes: one per veil, one door per lock across all of its
    /// tubes. They are refitted after every layout change.
    /// </para>
    /// <para>
    /// Moves and undos fly their balls (docs/animation-spec.md). The session's later events in the same
    /// operation play in their order, timed off the flights: the source's new top is revealed once the
    /// last moving ball has left it, a completion plays when the tube's last ball lands, and veils,
    /// keys and doors follow the landing. The model is never waited on: only the views lag behind
    /// it, and touching a tube plays what it is waiting for at once.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(BoardLayout))]
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private TubeView _tubePrefab;
        [SerializeField] private BallView _ballPrefab;
        [SerializeField] private CoverView _coverPrefab;
        [SerializeField] private ColorPalette _palette;
        [SerializeField] private LockPalette _lockPalette;
        [SerializeField] private TubeVisualConfig _tubeConfig;
        [SerializeField] private AnimationConfig _animation;
        [SerializeField] private BoardLayout _layout;
        [Tooltip("Inactive holder for pooled views.")]
        [SerializeField] private Transform _poolRoot;

        private readonly List<TubeView> _tubes = new List<TubeView>();
        private readonly List<CoverView> _covers = new List<CoverView>();
        private readonly List<BallView> _ballBuffer = new List<BallView>();
        private readonly List<TubeView> _singleTube = new List<TubeView>(1);
        private ObjectPool<TubeView> _tubePool;
        private ObjectPool<BallView> _ballPool;
        private ObjectPool<CoverView> _coverPool;
        private LevelSession _session;
        private int _liftedTube = -1;
        private Action<BallView> _onBallDeparted;
        private Action<BallView> _onBallLanded;

        // The move or undo whose events are arriving: where its balls land, and what it has done so far.
        private int _operationTarget = -1;
        private bool _operationCompleted;
        private bool _operationOpenedVeil;
#if UNITY_EDITOR
        private bool _tubeConfigChanged;
#endif

        /// <summary>Tube views in board order; index matches the board's tube index.</summary>
        public IReadOnlyList<TubeView> Tubes => _tubes;

        /// <summary>The board's layout: fit, scale and the tube-complete punch.</summary>
        public BoardLayout Layout => _layout;

        public ColorPalette Palette => _palette;

        /// <summary>True when no ball is flying and no beat of a move is still waiting to play.</summary>
        public bool IsSettled
        {
            get
            {
                foreach (TubeView tube in _tubes)
                {
                    if (!tube.IsSettled)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>The selected tube's top ball has started to lift.</summary>
        public event Action BallLifted;

        /// <summary>A lifted ball has started back into its slot (deselected, not moved).</summary>
        public event Action BallLowered;

        /// <summary>A ball has landed in its slot (move or undo: see <see cref="BallView.IsUndoFlight"/>).</summary>
        public event Action<BallView> BallLanded;

        /// <summary>
        /// A completed tube's last ball has landed and its completion starts: the tube, its colour id and
        /// the chain (the tubes completed before it in the level).
        /// </summary>
        public event Action<TubeView, int, int> CompletionStarted;

        /// <summary>A completed tube's cork has seated, <see cref="AnimationConfig.CorkImpactTime"/> into its completion.</summary>
        public event Action<TubeView> CorkSeated;

        /// <summary>Shows <paramref name="session"/>'s board and follows its events until destroyed.</summary>
        public void Initialize(LevelSession session)
        {
            Unsubscribe();
            _session = session;
            _session.MoveApplied += OnMoveApplied;
            _session.MoveUndone += OnMoveUndone;
            _session.BallRevealed += OnBallRevealed;
            _session.TubeCompleted += OnTubeCompleted;
            _session.TubeUnveiled += OnTubeUnveiled;
            _session.KeyFound += OnKeyFound;
            _session.LockOpened += OnLockOpened;
            _session.TubeAdded += OnTubeAdded;
            _session.Shuffled += Rebuild;
            _session.Restarted += Rebuild;
            Rebuild();
        }

        /// <summary>
        /// Lifts the selected tube's top ball, or lowers the previous one when <paramref name="tubeIndex"/>
        /// is negative.
        /// </summary>
        public void ShowSelection(int tubeIndex)
        {
            if (_liftedTube >= 0 && _liftedTube < _tubes.Count && _tubes[_liftedTube].Lower())
            {
                BallLowered?.Invoke();
            }

            _liftedTube = tubeIndex;
            if (tubeIndex >= 0 && _tubes[tubeIndex].Lift())
            {
                BallLifted?.Invoke();
            }
        }

        /// <summary>
        /// The invalid-move feedback on the board (docs/animation-spec.md, 2.5): the tube shakes and its
        /// balls look shocked; a covered tube shakes with its cover.
        /// </summary>
        public void ShowInvalid(int tubeIndex)
        {
            TubeView tube = _tubes[tubeIndex];
            if (TryFindClosedCover(tube, out CoverView cover))
            {
                cover.Shake(_animation, _tubeConfig.BallDiameter);
                return;
            }

            tube.Shake();
            tube.SetBallExpression(BallExpression.Shock, _palette, _animation.ShockFaceDuration);
        }

        /// <summary>Returns every tube, ball and cover to the pools.</summary>
        public void Clear()
        {
            foreach (CoverView cover in _covers)
            {
                _coverPool.Release(cover);
            }

            _covers.Clear();

            foreach (TubeView tube in _tubes)
            {
                tube.TakeAllBalls(_ballBuffer);
                _tubePool.Release(tube);
            }

            foreach (BallView ball in _ballBuffer)
            {
                _ballPool.Release(ball);
            }

            _tubes.Clear();
            _ballBuffer.Clear();
            _liftedTube = -1;
            _operationTarget = -1;
        }

        private void Rebuild()
        {
            Clear();

            LoadedLevel level = _session.Level;
            int colors = level.GetVariant(_session.Variant).Colors;
            if (colors > _palette.Count)
            {
                Debug.LogError(
                    $"{level.Name} V{_session.Variant} uses {colors} colours but {_palette.name} defines {_palette.Count}.", this);
            }

            BoardState board = _session.Board;
            for (int t = 0; t < board.TubeCount; t++)
            {
                Tube tube = board[t];
                TubeView tubeView = SpawnTube(t, tube.Capacity, tube.IsClay);

                for (int slot = 0; slot < tube.Count; slot++)
                {
                    BallView ballView = _ballPool.Get();
                    ballView.Show(tube[slot], _palette, _lockPalette, _tubeConfig.BallDiameter);
                    tubeView.AddBall(ballView);
                }

                if (board.IsCompleted(t))
                {
                    tubeView.ShowCompleted(tube.Top.ColorId, _palette, animate: false);
                }

                if (tube.IsCovered)
                {
                    GetOrSpawnCover(tube.Cover).AddTube(tubeView);
                }
            }

            _layout.Arrange(_session.Grid, _tubes, _tubeConfig);

            foreach (CoverView cover in _covers)
            {
                cover.FitToTubes(_tubeConfig);
                cover.SetOpen(false);
            }
        }

        private TubeView SpawnTube(int tubeIndex, int capacity, bool isClay)
        {
            TubeView tubeView = _tubePool.Get();
            tubeView.name = $"Tube {tubeIndex}";
            tubeView.Setup(tubeIndex, capacity, isClay, _tubeConfig, _animation);
            _tubes.Add(tubeView);
            return tubeView;
        }

        /// <summary>
        /// The view for <paramref name="cover"/>: a new one per veil, and one per lock shared by all
        /// the tubes it covers.
        /// </summary>
        private CoverView GetOrSpawnCover(TubeCover cover)
        {
            if (cover.Kind == CoverKind.Lock && TryFindDoor(cover.Id, out CoverView door))
            {
                return door;
            }

            CoverView view = _coverPool.Get();
            if (cover.Kind == CoverKind.Veil)
            {
                view.name = $"Veil {cover.Id}";
                view.Setup(CoverKind.Veil, cover.Id, _tubeConfig.VeilSprite, _tubeConfig.VeilTint,
                    _tubeConfig.VeilBadgeSprite, _palette.GetColor(cover.Id), _tubeConfig.VeilBadgeSize);
            }
            else
            {
                if (cover.Id >= _lockPalette.Count)
                {
                    Debug.LogError(
                        $"{_session.Level.Name} uses lock {cover.Id} but {_lockPalette.name} defines {_lockPalette.Count} lock colours.", this);
                }

                view.name = $"Door {cover.Id}";
                view.Setup(CoverKind.Lock, cover.Id, _tubeConfig.DoorSprite, _tubeConfig.DoorTint,
                    _tubeConfig.LockIconSprite, _lockPalette.GetTint(cover.Id), _tubeConfig.LockIconSize);
            }

            _covers.Add(view);
            return view;
        }

        private bool TryFindDoor(int lockId, out CoverView door)
        {
            foreach (CoverView cover in _covers)
            {
                if (cover.Kind == CoverKind.Lock && cover.Id == lockId)
                {
                    door = cover;
                    return true;
                }
            }

            door = null;
            return false;
        }

        private bool TryFindClosedCover(TubeView tube, out CoverView found)
        {
            foreach (CoverView cover in _covers)
            {
                if (cover.IsOpen)
                {
                    continue;
                }

                foreach (TubeView covered in cover.Tubes)
                {
                    if (covered == tube)
                    {
                        found = cover;
                        return true;
                    }
                }
            }

            found = null;
            return false;
        }

        /// <summary>
        /// Flies the top <paramref name="count"/> balls from one tube to another (docs/animation-spec.md,
        /// 2.3): each rises above the source's mouth, arcs to above the target's and drops into its
        /// slot, one chain delay after the ball above it. The views hold the balls in their new tubes
        /// at once; only the drawing travels.
        /// </summary>
        private void Transfer(int fromIndex, int toIndex, int count, bool isUndo)
        {
            TubeView source = _tubes[fromIndex];
            TubeView target = _tubes[toIndex];
            source.TakeTop(count, _ballBuffer);

            // The flight is drawn in the target's ball space: board space, shifted.
            Vector3 rise = target.BallSpace.InverseTransformPoint(source.BallSpace.TransformPoint(source.LiftPoint));
            Vector3 arcEnd = target.LiftPoint;
            float sourceClear = target.BallSpace.InverseTransformPoint(
                source.BallSpace.TransformPoint(new Vector3(0f, source.ClearHeight, 0f))).y;
            float distance = Mathf.Abs(arcEnd.x - rise.x) + Mathf.Abs(arcEnd.y - rise.y);
            float screenWidth = _layout.ScreenWidthInBoardUnits;
            float arcDuration = _animation.ArcBaseDuration + _animation.ArcDurationPerScreenWidth * distance / screenWidth;
            float arcHeight = _animation.ArcHeightBase * _tubeConfig.BallDiameter + _animation.ArcHeightPerDistance * distance;

            for (int i = 0; i < _ballBuffer.Count; i++)
            {
                BallView ball = _ballBuffer[i];
                Vector3 slot = target.ReceiveBall(ball);
                ball.MoveTo(new BallFlight(
                    fromIndex, isUndo, i * _animation.ChainDelay, rise, arcEnd, slot, arcDuration, arcHeight, screenWidth,
                    sourceClear, target.ClearHeight));
            }

            _ballBuffer.Clear();
            AssertInSync(fromIndex);
            AssertInSync(toIndex);
        }

        private void BeginOperation(int targetIndex)
        {
            _operationTarget = targetIndex;
            _operationCompleted = false;
            _operationOpenedVeil = false;
        }

        private void OnMoveApplied(MoveEventArgs e)
        {
            BeginOperation(e.ToIndex);
            Transfer(e.FromIndex, e.ToIndex, e.Count, isUndo: false);
        }

        // The event describes the original move; undoing it carries the balls back.
        private void OnMoveUndone(MoveEventArgs e)
        {
            BeginOperation(e.FromIndex);
            Transfer(e.ToIndex, e.FromIndex, e.Count, isUndo: true);
        }

        private void OnBallDeparted(BallView ball) => _tubes[ball.FlightSource].NotifyDeparted();

        private void OnBallLanded(BallView ball)
        {
            _tubes[ball.TubeIndex].NotifyLanded(ball);
            BallLanded?.Invoke(ball);
        }

        // The new top shows once the balls above it have left (a snap until the reveal flip, §3.1).
        private void OnBallRevealed(BallRevealedEventArgs e)
        {
            TubeView tube = _tubes[e.TubeIndex];
            BallView ball = tube.Balls[e.SlotIndex];
            int colorId = e.ColorId;
            tube.WhenDeparted(() => ball.Reveal(colorId, _palette));
        }

        // The completion plays when the tube's last ball lands.
        private void OnTubeCompleted(TubeCompletedEventArgs e)
        {
            _operationCompleted = true;
            TubeView tube = _tubes[e.TubeIndex];
            int colorId = e.ColorId;
            int chain = Mathf.Max(0, CountCompletedTubes() - 1);
            tube.WhenArrived(() => PlayCompletion(tube, colorId, chain));
        }

        private void PlayCompletion(TubeView tube, int colorId, int chain)
        {
            tube.ShowCompleted(colorId, _palette, animate: true);
            CompletionStarted?.Invoke(tube, colorId, chain);
            tube.AddBeat(DOVirtual.DelayedCall(_animation.CorkImpactTime, () => CorkSeated?.Invoke(tube), false)
                .OnBoardClock());
        }

        private int CountCompletedTubes()
        {
            int count = 0;
            for (int t = 0; t < _session.Board.TubeCount; t++)
            {
                if (_session.Board.IsCompleted(t))
                {
                    count++;
                }
            }

            return count;
        }

        // Snaps until the towel animation (§3.2), timed after the completion.
        private void OnTubeUnveiled(TubeUnveiledEventArgs e)
        {
            foreach (CoverView cover in _covers)
            {
                if (cover.Kind == CoverKind.Veil && cover.Tubes[0].TubeIndex == e.TubeIndex)
                {
                    _operationOpenedVeil = true;
                    AfterLanding(_animation.TowelDelay, cover.Tubes, () => cover.SetOpen(true));
                    return;
                }
            }
        }

        // Snaps until the key flight (§3.3).
        private void OnKeyFound(KeyFoundEventArgs e)
        {
            TubeView tube = _tubes[e.TubeIndex];
            BallView ball = tube.Balls[e.SlotIndex];
            _singleTube.Clear();
            _singleTube.Add(tube);
            AfterLanding(KeyDelay(), _singleTube, ball.RemoveKey);
        }

        private void OnLockOpened(LockOpenedEventArgs e)
        {
            if (TryFindDoor(e.LockId, out CoverView door))
            {
                AfterLanding(KeyDelay(), door.Tubes, () => door.SetOpen(true));
            }
        }

        /// <summary>
        /// A key mark leaves (and its door opens) at the landing: after the completion's beat when the
        /// move completed a tube, and never before a veil the same move opened.
        /// </summary>
        private float KeyDelay()
        {
            float delay = _operationCompleted ? _animation.KeyWaitAfterComplete : 0f;
            return _operationOpenedVeil ? Mathf.Max(delay, _animation.TowelDelay) : delay;
        }

        /// <summary>
        /// Plays <paramref name="action"/> <paramref name="delay"/> seconds of board time after the
        /// operation's balls have landed. Touching any of <paramref name="tubes"/> plays it at once.
        /// </summary>
        private void AfterLanding(float delay, IReadOnlyList<TubeView> tubes, TweenCallback action)
        {
            TubeView target = _operationTarget >= 0 && _operationTarget < _tubes.Count ? _tubes[_operationTarget] : null;
            var affected = new TubeView[tubes.Count];
            for (int i = 0; i < affected.Length; i++)
            {
                affected[i] = tubes[i];
            }

            void Schedule()
            {
                Tween beat = DOVirtual.DelayedCall(delay, action, false).OnBoardClock();
                foreach (TubeView tube in affected)
                {
                    tube.AddBeat(beat);
                }
            }

            if (target != null)
            {
                target.WhenArrived(Schedule);
            }
            else
            {
                Schedule();
            }
        }

        private void OnTubeAdded(TubeAddedEventArgs e)
        {
            // Added tubes are never clay or covered.
            SpawnTube(e.TubeIndex, e.Capacity, isClay: false);
            _layout.Arrange(_session.Grid, _tubes, _tubeConfig);

            // Re-centring can move the covered tubes; doors and veils follow them.
            foreach (CoverView cover in _covers)
            {
                cover.FitToTubes(_tubeConfig);
            }

            AssertInSync(e.TubeIndex);
        }

        /// <summary>Development check that a tube view holds as many balls as the model's tube.</summary>
        [Conditional("UNITY_ASSERTIONS")]
        private void AssertInSync(int tubeIndex)
        {
            // Messages are built only on failure: this runs on every move in development builds.
            if (_tubes.Count != _session.Board.TubeCount)
            {
                Debug.LogError($"Board view has {_tubes.Count} tubes, the board {_session.Board.TubeCount}.", this);
            }
            else if (_tubes[tubeIndex].Balls.Count != _session.Board[tubeIndex].Count)
            {
                Debug.LogError(
                    $"Tube {tubeIndex}: view has {_tubes[tubeIndex].Balls.Count} balls, " +
                    $"the board {_session.Board[tubeIndex].Count}.", this);
            }
        }

        private void Unsubscribe()
        {
            if (_session == null)
            {
                return;
            }

            _session.MoveApplied -= OnMoveApplied;
            _session.MoveUndone -= OnMoveUndone;
            _session.BallRevealed -= OnBallRevealed;
            _session.TubeCompleted -= OnTubeCompleted;
            _session.TubeUnveiled -= OnTubeUnveiled;
            _session.KeyFound -= OnKeyFound;
            _session.LockOpened -= OnLockOpened;
            _session.TubeAdded -= OnTubeAdded;
            _session.Shuffled -= Rebuild;
            _session.Restarted -= Rebuild;
            _session = null;
        }

        private BallView CreateBall()
        {
            BallView ball = Instantiate(_ballPrefab, _poolRoot);
            ball.Initialize(_animation, _onBallDeparted, _onBallLanded);
            return ball;
        }

        private void Awake()
        {
            if (_tubePrefab == null || _ballPrefab == null || _coverPrefab == null || _palette == null ||
                _lockPalette == null || _tubeConfig == null || _animation == null || _layout == null || _poolRoot == null)
            {
                Debug.LogError($"{name}: all references must be assigned.", this);
                enabled = false;
                return;
            }

            _onBallDeparted = OnBallDeparted;
            _onBallLanded = OnBallLanded;

            _tubePool = new ObjectPool<TubeView>(
                () => Instantiate(_tubePrefab, _poolRoot),
                tube => { tube.transform.SetParent(transform, false); tube.gameObject.SetActive(true); },
                tube => { tube.StopMotion(); tube.gameObject.SetActive(false); tube.transform.SetParent(_poolRoot, false); },
                tube => Destroy(tube.gameObject),
                collectionCheck: true, defaultCapacity: 12, maxSize: 32);

            _ballPool = new ObjectPool<BallView>(
                CreateBall,
                ball => ball.gameObject.SetActive(true),
                ball => { ball.StopMotion(); ball.gameObject.SetActive(false); ball.transform.SetParent(_poolRoot, false); },
                ball => Destroy(ball.gameObject),
                collectionCheck: true, defaultCapacity: 48, maxSize: 128);

            _coverPool = new ObjectPool<CoverView>(
                () => Instantiate(_coverPrefab, _poolRoot),
                cover => { cover.transform.SetParent(transform, false); cover.gameObject.SetActive(true); },
                cover => { cover.StopMotion(); cover.gameObject.SetActive(false); cover.transform.SetParent(_poolRoot, false); },
                cover => Destroy(cover.gameObject),
                collectionCheck: true, defaultCapacity: 4, maxSize: 18);
        }

#if UNITY_EDITOR
        // Editor tuning only: TubeVisualConfig.Changed is raised from OnValidate, where views may not be
        // built, so the change is noted and the board rebuilt on the next frame.
        private void OnEnable()
        {
            if (_tubeConfig != null)
            {
                _tubeConfig.Changed += OnTubeConfigChanged;
            }
        }

        private void OnDisable()
        {
            if (_tubeConfig != null)
            {
                _tubeConfig.Changed -= OnTubeConfigChanged;
            }
        }

        private void OnTubeConfigChanged() => _tubeConfigChanged = true;

        // Layout only, like BoardLayout's refit: rebuild with the new geometry, keeping the selection lifted.
        private void LateUpdate()
        {
            if (!_tubeConfigChanged || _session == null)
            {
                return;
            }

            _tubeConfigChanged = false;
            int lifted = _liftedTube;
            Rebuild();
            if (lifted >= 0 && lifted < _tubes.Count)
            {
                ShowSelection(lifted);
            }
        }
#endif

        private void OnDestroy() => Unsubscribe();

        private void OnValidate()
        {
            if (_layout == null)
            {
                _layout = GetComponent<BoardLayout>();
            }
        }
    }
}
