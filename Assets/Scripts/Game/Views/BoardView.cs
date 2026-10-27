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
    /// The board is built in full only when the level loads and when it restarts; every other event
    /// changes just the views it touches (balls change tubes, a ball turns face up, a cap appears,
    /// a tube is appended). Tubes and balls are pooled, so nothing is instantiated or destroyed
    /// once the pools are warm.
    /// </remarks>
    [RequireComponent(typeof(BoardLayout))]
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private TubeView _tubePrefab;
        [SerializeField] private BallView _ballPrefab;
        [SerializeField] private ColorPalette _palette;
        [SerializeField] private TubeVisualConfig _tubeConfig;
        [SerializeField] private BoardLayout _layout;
        [Tooltip("Inactive holder for pooled views.")]
        [SerializeField] private Transform _poolRoot;

        private readonly List<TubeView> _tubes = new List<TubeView>();
        private readonly List<BallView> _ballBuffer = new List<BallView>();
        private ObjectPool<TubeView> _tubePool;
        private ObjectPool<BallView> _ballPool;
        private LevelSession _session;
        private int _maxCapacity;
        private int _liftedTube = -1;

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
            _session.TubeAdded += OnTubeAdded;
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

        /// <summary>Returns every tube and ball to the pools.</summary>
        public void Clear()
        {
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
            if (level.Colors > _palette.Count)
            {
                Debug.LogError(
                    $"{level.Name} uses {level.Colors} colours but {_palette.name} defines {_palette.Count}.", this);
            }

            BoardState board = _session.Board;
            _maxCapacity = 1;
            for (int t = 0; t < board.TubeCount; t++)
            {
                Tube tube = board[t];
                TubeView tubeView = SpawnTube(t, tube.Capacity);
                tubeView.SetCapVisible(tube.IsCompleted);

                for (int slot = 0; slot < tube.Count; slot++)
                {
                    BallView ballView = _ballPool.Get();
                    ballView.Show(tube[slot], _palette, _tubeConfig.BallDiameter);
                    tubeView.AddBall(ballView);
                }
            }

            _layout.Arrange(_session.Rows, _tubes, _maxCapacity, _tubeConfig);
        }

        private TubeView SpawnTube(int tubeIndex, int capacity)
        {
            _maxCapacity = Mathf.Max(_maxCapacity, capacity);
            TubeView tubeView = _tubePool.Get();
            tubeView.name = $"Tube {tubeIndex}";
            tubeView.Setup(tubeIndex, capacity, _tubeConfig);
            _tubes.Add(tubeView);
            return tubeView;
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

        private void OnTubeCompleted(TubeCompletedEventArgs e) => _tubes[e.TubeIndex].SetCapVisible(true);

        private void OnTubeAdded(TubeAddedEventArgs e)
        {
            SpawnTube(e.TubeIndex, e.Capacity);
            _layout.Arrange(_session.Rows, _tubes, _maxCapacity, _tubeConfig);
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
            _session.TubeAdded -= OnTubeAdded;
            _session.Restarted -= Rebuild;
            _session = null;
        }

        private void Awake()
        {
            if (_tubePrefab == null || _ballPrefab == null || _palette == null ||
                _tubeConfig == null || _layout == null || _poolRoot == null)
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
        }

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
