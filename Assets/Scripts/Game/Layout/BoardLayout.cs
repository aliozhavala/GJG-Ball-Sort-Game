using System.Collections.Generic;
using BallSort.Game.Views;
using UnityEngine;

namespace BallSort.Game.Layout
{
    /// <summary>
    /// Arranges tubes into rows and scales the board to fit the camera's safe area.
    /// </summary>
    /// <remarks>
    /// Lives on the board root. Tubes are placed in unscaled board space, centred on the root; the
    /// root is then uniformly scaled and moved so the whole board fits both the width and the height
    /// of the safe area minus the HUD margins. Re-fits whenever the screen changes, so rotating a
    /// device or switching Game view aspect ratios in the Editor needs no reload.
    /// </remarks>
    public sealed class BoardLayout : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("Margins")]
        [Tooltip("Fraction of the safe-area height kept free at the top for the HUD.")]
        [SerializeField, Range(0f, 0.4f)] private float _topMargin = 0.14f;
        [Tooltip("Fraction of the safe-area height kept free at the bottom for boosters.")]
        [SerializeField, Range(0f, 0.4f)] private float _bottomMargin = 0.12f;
        [Tooltip("World units kept free at the left and right edges.")]
        [SerializeField, Min(0f)] private float _sidePadding = 0.25f;
        [Tooltip("Upper bound on scale so a small level does not fill the screen with giant tubes.")]
        [SerializeField, Min(0.1f)] private float _maxScale = 1.1f;

        private Vector2 _boardSize;
        private bool _hasBoard;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private float _lastOrthographicSize;

        /// <summary>
        /// Positions <paramref name="tubes"/> row by row (top row first, shorter rows centred) and fits
        /// the board to the screen.
        /// </summary>
        /// <param name="rows">Tube counts per row; sums to <c>tubes.Count</c>.</param>
        /// <param name="tubes">Tube views in board order.</param>
        /// <param name="maxCapacity">Largest capacity in the level; sets every row's height.</param>
        public void Arrange(IReadOnlyList<int> rows, IReadOnlyList<TubeView> tubes, int maxCapacity, TubeVisualConfig config)
        {
            float pitch = config.TubeWidth + config.HorizontalSpacing;
            float rowHeight = config.TubeHeight(maxCapacity) + config.CapHeight + config.LiftHeight;

            float width = 0f;
            for (int r = 0; r < rows.Count; r++)
            {
                width = Mathf.Max(width, RowWidth(rows[r], config));
            }

            float height = rows.Count * rowHeight + (rows.Count - 1) * config.VerticalSpacing;
            _boardSize = new Vector2(width, height);

            int tubeIndex = 0;
            for (int r = 0; r < rows.Count; r++)
            {
                // Tubes stand on the row's base line; the lift area sits above the tallest tube.
                float rowTop = height * 0.5f - r * (rowHeight + config.VerticalSpacing);
                float baseY = rowTop - rowHeight;
                float firstX = -RowWidth(rows[r], config) * 0.5f + config.TubeWidth * 0.5f;

                for (int i = 0; i < rows[r]; i++, tubeIndex++)
                {
                    tubes[tubeIndex].transform.localPosition = new Vector3(firstX + i * pitch, baseY, 0f);
                }
            }

            _hasBoard = true;
            Fit();
        }

        /// <summary>Scales and centres the board root inside the current screen's safe area.</summary>
        public void Fit()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrthographicSize = _camera.orthographicSize;
            FitTo(_lastScreenSize, _lastSafeArea);
        }

        /// <summary>
        /// Scales and centres the board root inside <paramref name="safeArea"/> of a screen of
        /// <paramref name="screenSize"/> pixels, below/above the HUD margins.
        /// </summary>
        /// <remarks>
        /// Takes the screen explicitly (rather than reading <see cref="Screen"/>) so other aspect
        /// ratios can be checked from editor tooling without resizing the Game view.
        /// </remarks>
        public void FitTo(Vector2 screenSize, Rect safeArea)
        {
            if (!_hasBoard)
            {
                return;
            }

            Vector2 min = ScreenToWorld(safeArea.min, screenSize);
            Vector2 max = ScreenToWorld(safeArea.max, screenSize);
            float safeHeight = max.y - min.y;

            float left = min.x + _sidePadding;
            float right = max.x - _sidePadding;
            float bottom = min.y + safeHeight * _bottomMargin;
            float top = max.y - safeHeight * _topMargin;

            float scale = Mathf.Min((right - left) / _boardSize.x, (top - bottom) / _boardSize.y, _maxScale);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, transform.position.z);
        }

        /// <summary>Orthographic screen-to-world mapping for an arbitrary screen size.</summary>
        private Vector2 ScreenToWorld(Vector2 pixel, Vector2 screenSize)
        {
            float worldHeight = _camera.orthographicSize * 2f;
            float worldWidth = worldHeight * screenSize.x / screenSize.y;
            Vector3 centre = _camera.transform.position;
            return new Vector2(
                centre.x + (pixel.x / screenSize.x - 0.5f) * worldWidth,
                centre.y + (pixel.y / screenSize.y - 0.5f) * worldHeight);
        }

        private static float RowWidth(int tubeCount, TubeVisualConfig config) =>
            tubeCount * config.TubeWidth + (tubeCount - 1) * config.HorizontalSpacing;

        private void LateUpdate()
        {
            // Presentation only: cheap change detection so the board follows resolution changes.
            if (_hasBoard &&
                (Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y ||
                 Screen.safeArea != _lastSafeArea || !Mathf.Approximately(_camera.orthographicSize, _lastOrthographicSize)))
            {
                Fit();
            }
        }

        private void Awake()
        {
            if (_camera == null || !_camera.orthographic)
            {
                Debug.LogError($"{name}: needs an orthographic camera.", this);
            }
        }
    }
}
