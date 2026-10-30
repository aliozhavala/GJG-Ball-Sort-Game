using System.Collections.Generic;
using System.Diagnostics;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using BallSort.Core.Session;
using BallSort.Game.Layout;
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
    /// touches (balls change tubes, a ball turns face up, a key mark goes, a cover opens, a cap
    /// appears, a tube is appended). Tubes, balls and covers are pooled, so nothing is instantiated
    /// or destroyed once the pools are warm.
    /// <para>
    /// Covers are separate views over the tubes: one per veil, one door per lock across all of its
    /// tubes. They are refitted after every layout change.
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
        [SerializeField] private BoardLayout _layout;
        [Tooltip("Inactive holder for pooled views.")]
        [SerializeField] private Transform _poolRoot;

        private readonly List<TubeView> _tubes = new List<TubeView>();
        private readonly List<CoverView> _covers = new List<CoverView>();
        private readonly List<BallView> _ballBuffer = new List<BallView>();
        private ObjectPool<TubeView> _tubePool;
        private ObjectPool<BallView> _ballPool;
        private ObjectPool<CoverView> _coverPool;
        private LevelSession _session;
        private int _liftedTube = -1;
#if UNITY_EDITOR
        private bool _tubeConfigChanged;
#endif

        /// <summary>Tube views in board order; index matches the board's tube index.</summary>
        public IReadOnlyList<TubeView> Tubes => _tubes;

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
        /// Lifts the selected tube's run, or lowers the previous one when <paramref name="tubeIndex"/>
        /// is negative.
        /// </summary>
        public void ShowSelection(int tubeIndex)
        {
            if (_liftedTube >= 0 && _liftedTube < _tubes.Count)
            {
                _tubes[_liftedTube].Lower();
            }

            _liftedTube = tubeIndex;
            if (tubeIndex >= 0)
            {
                _tubes[tubeIndex].Lift(_session.Board[tubeIndex].RunLength);
            }
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
                    ShowCompleted(tubeView);
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
            tubeView.Setup(tubeIndex, capacity, isClay, _tubeConfig);
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

        private void Transfer(int fromIndex, int toIndex, int count)
        {
            _tubes[fromIndex].TakeTop(count, _ballBuffer);
            foreach (BallView ball in _ballBuffer)
            {
                _tubes[toIndex].AddBall(ball);
            }

            _ballBuffer.Clear();
            AssertInSync(fromIndex);
            AssertInSync(toIndex);
        }

        private void OnMoveApplied(MoveEventArgs e) => Transfer(e.FromIndex, e.ToIndex, e.Count);

        // The event describes the original move; undoing it carries the balls back.
        private void OnMoveUndone(MoveEventArgs e) => Transfer(e.ToIndex, e.FromIndex, e.Count);

        private void OnBallRevealed(BallRevealedEventArgs e) =>
            _tubes[e.TubeIndex].Balls[e.SlotIndex].Reveal(e.ColorId, _palette);

        private void OnTubeCompleted(TubeCompletedEventArgs e) => ShowCompleted(_tubes[e.TubeIndex]);

        /// <summary>A completed tube gets its cap, and its balls laugh.</summary>
        private void ShowCompleted(TubeView tube)
        {
            tube.SetCapVisible(true);
            tube.SetBallExpression(BallExpression.Laugh, _palette);
        }

        private void OnTubeUnveiled(TubeUnveiledEventArgs e)
        {
            foreach (CoverView cover in _covers)
            {
                if (cover.Kind == CoverKind.Veil && cover.Tubes[0].TubeIndex == e.TubeIndex)
                {
                    cover.SetOpen(true);
                    return;
                }
            }
        }

        private void OnKeyFound(KeyFoundEventArgs e) => _tubes[e.TubeIndex].Balls[e.SlotIndex].RemoveKey();

        private void OnLockOpened(LockOpenedEventArgs e)
        {
            if (TryFindDoor(e.LockId, out CoverView door))
            {
                door.SetOpen(true);
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

        private void Awake()
        {
            if (_tubePrefab == null || _ballPrefab == null || _coverPrefab == null || _palette == null ||
                _lockPalette == null || _tubeConfig == null || _layout == null || _poolRoot == null)
            {
                Debug.LogError($"{name}: all references must be assigned.", this);
                enabled = false;
                return;
            }

            _tubePool = new ObjectPool<TubeView>(
                () => Instantiate(_tubePrefab, _poolRoot),
                tube => { tube.transform.SetParent(transform, false); tube.gameObject.SetActive(true); },
                tube => { tube.gameObject.SetActive(false); tube.transform.SetParent(_poolRoot, false); },
                tube => Destroy(tube.gameObject),
                collectionCheck: true, defaultCapacity: 12, maxSize: 32);

            _ballPool = new ObjectPool<BallView>(
                () => Instantiate(_ballPrefab, _poolRoot),
                ball => ball.gameObject.SetActive(true),
                ball => { ball.gameObject.SetActive(false); ball.transform.SetParent(_poolRoot, false); },
                ball => Destroy(ball.gameObject),
                collectionCheck: true, defaultCapacity: 48, maxSize: 128);

            _coverPool = new ObjectPool<CoverView>(
                () => Instantiate(_coverPrefab, _poolRoot),
                cover => { cover.transform.SetParent(transform, false); cover.gameObject.SetActive(true); },
                cover => { cover.gameObject.SetActive(false); cover.transform.SetParent(_poolRoot, false); },
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
