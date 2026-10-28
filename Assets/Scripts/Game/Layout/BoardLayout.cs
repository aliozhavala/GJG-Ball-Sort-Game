using System.Collections.Generic;
using BallSort.Core.Levels;
using BallSort.Game.Views;
using UnityEngine;

namespace BallSort.Game.Layout
{
    /// <summary>
    /// Arranges tubes on the level's grid and scales the board to fit the camera's safe area.
    /// </summary>
    /// <remarks>
    /// Lives on the board root. Tubes are placed in unscaled board space, centred on the root; the
    /// root is then uniformly scaled and moved so the whole board fits both the width and the height
    /// of the space left by the HUD (the board area rect, or the safe area minus the margin
    /// fractions). Re-fits whenever the screen changes, so rotating a
    /// device or switching Game view aspect ratios in the Editor needs no reload.
    /// </remarks>
    public sealed class BoardLayout : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("Margins")]
        [Tooltip("Fraction of the safe-area height kept free at the top for the HUD, when no board area is set.")]
        [SerializeField, Range(0f, 0.4f)] private float _topMargin = 0.14f;
        [Tooltip("Fraction of the safe-area height kept free at the bottom for boosters, when no board area is set.")]
        [SerializeField, Range(0f, 0.4f)] private float _bottomMargin = 0.12f;
        [Tooltip("World units kept free at the left and right edges.")]
        [SerializeField, Min(0f)] private float _sidePadding = 0.25f;
        [Tooltip("Upper bound on scale so a small level does not fill the screen with giant tubes.")]
        [SerializeField, Min(0.1f)] private float _maxScale = 1.1f;

        [Header("HUD")]
        [Tooltip("Optional UI rect marking the space between the top HUD and the booster bar. When set, the " +
                 "board fits vertically inside it instead of using the margin fractions.")]
        [SerializeField] private RectTransform _boardArea;

        private readonly Vector3[] _corners = new Vector3[4];

        private Vector2 _boardSize;
        private bool _hasBoard;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private float _lastOrthographicSize;

        /// <summary>
        /// Positions every tube on <paramref name="grid"/> and fits the board to the screen.
        /// </summary>
        /// <remarks>
        /// Geometry (docs/mechanics.md): a row is as tall as a capacity-4 tube, with a gap of two
        /// ball slots above it for the lifted run, so rows sit one row pitch apart and capacity-10
        /// and capacity-16 tubes reach exactly as high as the top row of their span. Free rows are
        /// centred on their own; aligned rows share columns, and their block is centred by its
        /// widest row. Every tube stands on the base line of its bottom row.
        /// <para>
        /// The scale is refitted only when the board's size changes, which during play means an
        /// added tube widened the widest row; otherwise tubes are just repositioned.
        /// </para>
        /// </remarks>
        /// <param name="grid">The session's grid, including any added tubes.</param>
        /// <param name="tubes">Tube views in board order; one per tube on the grid.</param>
        public void Arrange(BoardGrid grid, IReadOnlyList<TubeView> tubes, TubeVisualConfig config)
        {
            float pitchX = config.TubeWidth + config.HorizontalSpacing;
            float pitchY = LevelLayout.RowPitchSlots * config.SlotHeight;

            int alignedCells = 0;
            for (int r = 0; r < grid.RowCount; r++)
            {
                if (grid.IsAligned(r))
                {
                    alignedCells = Mathf.Max(alignedCells, grid.GetCellCount(r));
                }
            }

            // Each row owns one pitch of height: its tubes plus the lift gap above them.
            var size = new Vector2(RowWidth(grid.WidestRowCellCount, config), grid.RowCount * pitchY);

            for (int t = 0; t < tubes.Count; t++)
            {
                GridPlacement placement = grid.GetPlacement(t);
                int rowCells = grid.IsAligned(placement.Row) ? alignedCells : grid.GetCellCount(placement.Row);
                float firstX = (config.TubeWidth - RowWidth(rowCells, config)) * 0.5f;
                float baseY = size.y * 0.5f - (placement.Row + 1) * pitchY;
                tubes[t].transform.localPosition = new Vector3(firstX + placement.Cell * pitchX, baseY, 0f);
            }

            if (_hasBoard && size == _boardSize)
            {
                return;
            }

            _boardSize = size;
            _hasBoard = true;
            Fit();
        }

        /// <summary>
        /// Scales and centres the board root on the current screen: inside the board area rect when
        /// one is set, otherwise inside the safe area minus the margin fractions.
        /// </summary>
        public void Fit()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrthographicSize = _camera.orthographicSize;

            if (_boardArea == null)
            {
                FitTo(_lastScreenSize, _lastSafeArea);
                return;
            }

            // The HUD's share of the screen changes with the aspect ratio (the canvas scaler matches
            // width and height halfway), so fixed fractions cannot keep the board clear of it on
            // every device; the UI's own rect can.
            Canvas.ForceUpdateCanvases();
            Vector2 areaMin = UiToWorld(_boardArea, corner: 0);
            Vector2 areaMax = UiToWorld(_boardArea, corner: 2);
            FitWithin(_lastScreenSize, _lastSafeArea, areaMin.y, areaMax.y);
        }

        /// <summary>
        /// Scales and centres the board root inside <paramref name="safeArea"/> of a screen of
        /// <paramref name="screenSize"/> pixels, below/above the HUD margin fractions (the board area
        /// rect only describes the current screen, so it is not used here).
        /// </summary>
        /// <remarks>
        /// Takes the screen explicitly (rather than reading <see cref="Screen"/>) so other aspect
        /// ratios can be checked from editor tooling without resizing the Game view.
        /// </remarks>
        public void FitTo(Vector2 screenSize, Rect safeArea)
        {
            Vector2 min = ScreenToWorld(safeArea.min, screenSize);
            Vector2 max = ScreenToWorld(safeArea.max, screenSize);
            float safeHeight = max.y - min.y;
            FitWithin(screenSize, safeArea, min.y + safeHeight * _bottomMargin, max.y - safeHeight * _topMargin);
        }

        /// <param name="bottom">World y of the lowest point the board may use.</param>
        /// <param name="top">World y of the highest point the board may use.</param>
        private void FitWithin(Vector2 screenSize, Rect safeArea, float bottom, float top)
        {
            if (!_hasBoard)
            {
                return;
            }

            Vector2 min = ScreenToWorld(safeArea.min, screenSize);
            Vector2 max = ScreenToWorld(safeArea.max, screenSize);

            float left = min.x + _sidePadding;
            float right = max.x - _sidePadding;

            // Floor: a screen too short for the HUD must not flip the board with a negative scale.
            float scale = Mathf.Max(0.01f,
                Mathf.Min((right - left) / _boardSize.x, (top - bottom) / _boardSize.y, _maxScale));
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

        /// <summary>World position of one of a UI rect's corners (0 = bottom-left, 2 = top-right) on the current screen.</summary>
        private Vector2 UiToWorld(RectTransform rect, int corner)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.rootCanvas.worldCamera;

            rect.GetWorldCorners(_corners);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, _corners[corner]);
            return ScreenToWorld(screen, new Vector2(Screen.width, Screen.height));
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
