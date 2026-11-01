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
    /// of the space left by the HUD (between the UI it keeps clear of, or the safe area minus the
    /// margin fractions), with the space it does not need shared out above and below it. Re-fits whenever the screen changes, so
    /// rotating a device or switching Game view aspect ratios in the Editor needs no reload.
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
        [Tooltip("UI the board stays clear of, shown or not (title box, mascot frame, settings button, booster box, " +
                 "the stuck bar's parts): those whose centre is above the screen's middle bound it from above, " +
                 "the others from below. When set, they replace the margin fractions.")]
        [SerializeField] private RectTransform[] _keepClear = new RectTransform[0];
        [Tooltip("UI the board stays clear of only while it is shown (the tier label).")]
        [SerializeField] private RectTransform[] _keepClearWhenShown = new RectTransform[0];
        [Tooltip("World units kept free between the tubes and that UI.")]
        [SerializeField, Min(0f)] private float _hudClearance = 0.15f;
        [Tooltip("How much of the top row's lift gap (the empty space above it where a lifted ball goes) may " +
                 "reach up past the top HUD's line: 0 keeps it all below, 1 lets the tubes themselves touch it.")]
        [SerializeField, Range(0f, 1f)] private float _topLiftOverlap = 1f;
        [Tooltip("Where height the board does not need goes (a board fitted to the width): 0 all of it below the " +
                 "board, so it stands at the top; 0.5 half above, half below; 1 all above.")]
        [SerializeField, Range(0f, 1f)] private float _spareAbove = 0.4f;

        private readonly Vector3[] _corners = new Vector3[4];

        private Vector2 _boardSize;
        private float _liftGap;
        private bool _hasBoard;
        private float _fitScale = 1f;
        private float _punch = 1f;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private float _lastOrthographicSize;

        /// <summary>The screen's width in unscaled board units (the flight's speed and stretch are measured in it).</summary>
        public float ScreenWidthInBoardUnits =>
            _camera.orthographicSize * 2f * _camera.aspect / Mathf.Max(_fitScale, 0.0001f);

        /// <summary>A point of the screen (viewport: 0,0 bottom left, 1,1 top right) in world space, at the board's depth.</summary>
        public Vector3 ViewportToWorld(Vector2 viewport)
        {
            Vector3 point = _camera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, 0f));
            point.z = transform.position.z;
            return point;
        }

        /// <summary>
        /// Multiplies the fitted scale (the tube-complete punch); 1 is the fitted board. Scales about
        /// the board's centre, which is the root's position.
        /// </summary>
        public void SetPunch(float factor)
        {
            _punch = factor;
            ApplyScale();
        }

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
            _liftGap = LevelLayout.RowGapSlots * config.SlotHeight;

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
                tubes[t].Place(new Vector3(firstX + placement.Cell * pitchX, baseY, 0f));
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
        /// Scales and places the board root on the current screen: between the UI it keeps clear of
        /// when set, otherwise inside the safe area minus the margin fractions.
        /// </summary>
        public void Fit()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrthographicSize = _camera.orthographicSize;

            if (_keepClear.Length == 0)
            {
                FitTo(_lastScreenSize, _lastSafeArea);
                return;
            }

            // The HUD's share of the screen changes with the aspect ratio (the canvas scaler matches
            // width and height halfway), so fixed fractions cannot keep the board clear of it on
            // every device; the UI's own rects can.
            Canvas.ForceUpdateCanvases();
            float middle = _camera.transform.position.y;
            float top = ScreenToWorld(_lastSafeArea.max, _lastScreenSize).y;
            float bottom = ScreenToWorld(_lastSafeArea.min, _lastScreenSize).y;
            foreach (RectTransform rect in _keepClear)
            {
                Clear(rect, middle, ref top, ref bottom);
            }

            foreach (RectTransform rect in _keepClearWhenShown)
            {
                if (rect != null && rect.gameObject.activeInHierarchy)
                {
                    Clear(rect, middle, ref top, ref bottom);
                }
            }

            FitWithin(_lastScreenSize, _lastSafeArea, bottom + _hudClearance, top - _hudClearance, _topLiftOverlap);
        }

        // Lowers the top below a rect in the upper half of the screen, or raises the bottom above one in the lower half.
        private void Clear(RectTransform rect, float middle, ref float top, ref float bottom)
        {
            if (rect == null)
            {
                return;
            }

            float rectBottom = UiToWorld(rect, corner: 0).y;
            float rectTop = UiToWorld(rect, corner: 2).y;
            if ((rectBottom + rectTop) * 0.5f > middle)
            {
                top = Mathf.Min(top, rectBottom);
            }
            else
            {
                bottom = Mathf.Max(bottom, rectTop);
            }
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
            FitWithin(screenSize, safeArea, min.y + safeHeight * _bottomMargin, max.y - safeHeight * _topMargin, 0f);
        }

        /// <param name="bottom">World y of the lowest point the board may use.</param>
        /// <param name="top">World y of the highest point the tubes may reach.</param>
        /// <param name="liftOverlap">Share of the top row's lift gap allowed above <paramref name="top"/>.</param>
        private void FitWithin(Vector2 screenSize, Rect safeArea, float bottom, float top, float liftOverlap)
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
            // The part of the lift gap allowed above the line does not count towards the height to fit.
            float overlap = _liftGap * liftOverlap;
            float scale = Mathf.Max(0.01f,
                Mathf.Min((right - left) / _boardSize.x, (top - bottom) / (_boardSize.y - overlap), _maxScale));
            _fitScale = scale;
            ApplyScale();

            // Height the board does not need is shared out above and below it.
            float spare = Mathf.Max(0f, (top - bottom) - (_boardSize.y - overlap) * scale);
            float y = top - spare * _spareAbove + overlap * scale - _boardSize.y * scale * 0.5f;
            transform.position = new Vector3((left + right) * 0.5f, y, transform.position.z);
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

        private void ApplyScale()
        {
            float scale = _fitScale * _punch;
            transform.localScale = new Vector3(scale, scale, 1f);
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
