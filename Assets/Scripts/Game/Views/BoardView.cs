using System.Collections.Generic;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using BallSort.Core.Session;
using BallSort.Game.Layout;
using UnityEngine;
using UnityEngine.Pool;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Builds the on-screen board for a level from pooled tube and ball views.
    /// </summary>
    /// <remarks>
    /// Tubes and balls are pooled so moving between levels (and, later, restart) never instantiates
    /// or destroys anything once the pools are warm.
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
        private readonly List<BallView> _releaseBuffer = new List<BallView>();
        private ObjectPool<TubeView> _tubePool;
        private ObjectPool<BallView> _ballPool;

        /// <summary>Tube views in board order; index matches the board's tube index.</summary>
        public IReadOnlyList<TubeView> Tubes => _tubes;

        /// <summary>
        /// Clears any previous board and shows the session's current board. Call again after the
        /// session restarts, since restart swaps in a new board.
        /// </summary>
        public void Build(LevelSession session)
        {
            Clear();

            LoadedLevel level = session.Level;
            if (level.Colors > _palette.Count)
            {
                Debug.LogError(
                    $"{level.Name} uses {level.Colors} colours but {_palette.name} defines {_palette.Count}.", this);
            }

            BoardState board = session.Board;
            int maxCapacity = 1;
            for (int t = 0; t < board.TubeCount; t++)
            {
                Tube tube = board[t];
                maxCapacity = Mathf.Max(maxCapacity, tube.Capacity);

                TubeView tubeView = _tubePool.Get();
                tubeView.name = $"Tube {t}";
                tubeView.Setup(t, tube.Capacity, _tubeConfig);
                _tubes.Add(tubeView);

                for (int slot = 0; slot < tube.Count; slot++)
                {
                    BallView ballView = _ballPool.Get();
                    ballView.Show(tube[slot], _palette, _tubeConfig.BallDiameter);
                    tubeView.AddBall(ballView);
                }
            }

            _layout.Arrange(session.Rows, _tubes, maxCapacity, _tubeConfig);
        }

        /// <summary>Returns every tube and ball to the pools.</summary>
        public void Clear()
        {
            foreach (TubeView tube in _tubes)
            {
                tube.TakeAllBalls(_releaseBuffer);
                _tubePool.Release(tube);
            }

            foreach (BallView ball in _releaseBuffer)
            {
                _ballPool.Release(ball);
            }

            _tubes.Clear();
            _releaseBuffer.Clear();
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

        private void OnValidate()
        {
            if (_layout == null)
            {
                _layout = GetComponent<BoardLayout>();
            }
        }
    }
}
