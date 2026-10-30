using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One tube on screen: the tube drawn behind the balls from a top, a stretched middle and a bottom
    /// part, the balls, an optional glass front, a cap shown once the tube is completed, the clay at
    /// the base of clay tubes and a flash for refused taps. The transform's origin is the tube's base
    /// centre.
    /// </summary>
    /// <remarks>
    /// Covers are not part of the tube: a <see cref="CoverView"/> may span several tubes, and hides
    /// their balls through <see cref="SetBallsVisible"/>.
    /// </remarks>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TubeView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _top;
        [SerializeField] private SpriteRenderer _middle;
        [FormerlySerializedAs("_back")]
        [SerializeField] private SpriteRenderer _bottom;
        [Tooltip("Optional glass front over the balls; its sorting layer is also where lifted balls draw.")]
        [SerializeField] private SpriteRenderer _front;
        [SerializeField] private SpriteRenderer _cap;
        [Tooltip("Clay at the base of a clay tube, behind the balls.")]
        [SerializeField] private SpriteRenderer _clay;
        [Tooltip("Placeholder feedback for a refused tap; on the VFX layer so it also shows over covers.")]
        [SerializeField] private SpriteRenderer _flash;
        [Tooltip("Parent for this tube's balls.")]
        [SerializeField] private Transform _ballRoot;
        [SerializeField] private BoxCollider2D _tapTarget;
        [Tooltip("Sorting order of a lifted run on the glass front's layer: above the glass, cap, clay and covers.")]
        [SerializeField] private int _raisedSortingOrder = 10;

        private readonly List<BallView> _balls = new List<BallView>();
        private TubeVisualConfig _config;
        private int _liftedCount;
        private Tween _flashTween;

        /// <summary>Board index of the tube this view shows.</summary>
        public int TubeIndex { get; private set; }

        /// <summary>Balls currently parented to this tube, bottom first.</summary>
        public IReadOnlyList<BallView> Balls => _balls;

        /// <summary>Height of the tube body above its base, in board units.</summary>
        public float BodyHeight { get; private set; }

        /// <summary>
        /// Sizes the tube for <paramref name="capacity"/> balls. The tap target also covers the lift
        /// area above the rim, so a tap on a lifted run still hits its tube.
        /// </summary>
        /// <param name="isClay">Shows the clay overlay; clay is permanent, so it is set only here.</param>
        public void Setup(int tubeIndex, int capacity, bool isClay, TubeVisualConfig config)
        {
            TubeIndex = tubeIndex;
            _config = config;
            _liftedCount = 0;

            float height = config.TubeHeight(capacity);
            BodyHeight = height;
            float scale = config.ArtScale;

            // The bottom stands on the base and the top hangs from the rim, both at the art's size;
            // the middle stretches over the gap between them.
            float middleBottom = HeightAbovePivot(config.BottomSprite) * scale;
            float middleTop = height - HeightBelowPivot(config.TopSprite) * scale;
            float middleScale = Mathf.Max(0f, middleTop - middleBottom) /
                Mathf.Max(SpriteHeight(config.MiddleSprite), 0.001f);
            ApplyPart(_bottom, config.BottomSprite, config.TubeTint, 0f, new Vector3(scale, scale, 1f));
            ApplyPart(_middle, config.MiddleSprite, config.TubeTint, middleBottom, new Vector3(scale, middleScale, 1f));
            ApplyPart(_top, config.TopSprite, config.TubeTint, height, new Vector3(scale, scale, 1f));

            ApplyOutline(_front, config.FrontSprite, config.FrontTint, scale, height);
            ApplySprite(_cap, config.CapSprite, config.CapTint,
                new Vector2(config.TubeWidth, config.CapHeight), new Vector3(0f, height, 0f));
            SetCapVisible(false);

            // Clay is as wide as the tube is drawn (its rim), so it never reaches the next tube.
            float tubeDrawnWidth = SpriteWidth(config.TopSprite) * scale;
            float clayScale = tubeDrawnWidth / Mathf.Max(SpriteWidth(config.ClaySprite), 0.001f);
            ApplyPart(_clay, config.ClaySprite, config.ClayTint, 0f, new Vector3(clayScale, clayScale, 1f));
            _clay.enabled = isClay && _clay.sprite != null;

            // The flash uses the outline: it is the tube's silhouette.
            _flashTween?.Kill();
            ApplyOutline(_flash, config.OutlineSprite, Color.clear, scale, height);

            // A pooled view may come back from a covered tube.
            SetBallsVisible(true);

            float tapHeight = height + config.LiftHeight;
            _tapTarget.size = new Vector2(config.TubeWidth + config.HorizontalSpacing, tapHeight);
            _tapTarget.offset = new Vector2(0f, tapHeight * 0.5f);
        }

        /// <summary>
        /// Puts <paramref name="ball"/> in the next free slot. Any lifted balls drop back first, so
        /// lifted balls are always the top of the tube.
        /// </summary>
        public void AddBall(BallView ball)
        {
            Lower();
            int slot = _balls.Count;
            ball.transform.SetParent(_ballRoot, false);
            ball.AssignSlot(TubeIndex, slot);
            ball.SetRaised(false, _front.sortingLayerID, _raisedSortingOrder);
            ball.MoveTo(SlotPosition(slot));
            _balls.Add(ball);
        }

        /// <summary>
        /// Removes the top <paramref name="count"/> balls and appends them to
        /// <paramref name="into"/>, topmost first. They stay parented here until added elsewhere.
        /// </summary>
        public void TakeTop(int count, List<BallView> into)
        {
            for (int i = 0; i < count; i++)
            {
                int top = _balls.Count - 1;
                into.Add(_balls[top]);
                _balls.RemoveAt(top);
            }

            _liftedCount = Mathf.Max(0, _liftedCount - count);
        }

        /// <summary>
        /// Raises the top <paramref name="count"/> balls (the selected run) by the lift offset and
        /// draws them above the glass. Lowers any previous lift first.
        /// </summary>
        public void Lift(int count)
        {
            Lower();
            _liftedCount = Mathf.Min(count, _balls.Count);
            var offset = new Vector3(0f, _config.LiftHeight, 0f);
            for (int slot = _balls.Count - _liftedCount; slot < _balls.Count; slot++)
            {
                _balls[slot].SetRaised(true, _front.sortingLayerID, _raisedSortingOrder);
                _balls[slot].MoveTo(SlotPosition(slot) + offset);
            }
        }

        /// <summary>Drops any lifted balls back into their slots.</summary>
        public void Lower()
        {
            for (int slot = _balls.Count - _liftedCount; slot < _balls.Count; slot++)
            {
                _balls[slot].SetRaised(false, _front.sortingLayerID, _raisedSortingOrder);
                _balls[slot].MoveTo(SlotPosition(slot));
            }

            _liftedCount = 0;
        }

        /// <summary>Hides or shows every ball in the tube; driven by <see cref="CoverView.SetOpen"/>.</summary>
        public void SetBallsVisible(bool visible) => _ballRoot.gameObject.SetActive(visible);

        /// <summary>
        /// Washes the tube in <paramref name="color"/>, fading out over <paramref name="duration"/>
        /// seconds. A new flash replaces a running one.
        /// </summary>
        public void Flash(Color color, float duration)
        {
            _flashTween?.Kill();
            _flash.color = color;
            _flashTween = _flash.DOFade(0f, duration).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        /// <summary>Hands every ball back to the caller (for pooling) and empties the tube.</summary>
        public void TakeAllBalls(List<BallView> into)
        {
            Lower();
            into.AddRange(_balls);
            _balls.Clear();
        }

        public void SetCapVisible(bool visible) => _cap.enabled = visible && _cap.sprite != null;

        /// <summary>Gives every ball in the tube <paramref name="expression"/>: laughing once the tube is completed.</summary>
        public void SetBallExpression(BallExpression expression, ColorPalette palette)
        {
            foreach (BallView ball in _balls)
            {
                ball.SetExpression(expression, palette);
            }
        }

        private Vector3 SlotPosition(int slot) => new Vector3(0f, _config.SlotY(slot), 0f);

        /// <summary>A piece drawn as is, scaled and placed by its pivot on the tube's centre line.</summary>
        private static void ApplyPart(SpriteRenderer renderer, Sprite sprite, Color tint, float y, Vector3 scale)
        {
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.transform.localPosition = new Vector3(0f, y, 0f);
            renderer.transform.localScale = scale;
            renderer.enabled = sprite != null;
        }

        /// <summary>
        /// A whole-tube sprite pivoted at the base, 9-sliced so its rim and bottom keep the art's size
        /// while it stretches to <paramref name="height"/>.
        /// </summary>
        private static void ApplyOutline(SpriteRenderer renderer, Sprite sprite, Color tint, float scale, float height)
        {
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.enabled = sprite != null;
            if (sprite != null)
            {
                renderer.size = new Vector2(SpriteWidth(sprite), height / scale);
            }
        }

        private static float SpriteWidth(Sprite sprite) => sprite != null ? sprite.bounds.size.x : 0f;

        private static float SpriteHeight(Sprite sprite) => sprite != null ? sprite.bounds.size.y : 0f;

        private static float HeightAbovePivot(Sprite sprite) =>
            sprite != null ? (sprite.rect.height - sprite.pivot.y) / sprite.pixelsPerUnit : 0f;

        private static float HeightBelowPivot(Sprite sprite) =>
            sprite != null ? sprite.pivot.y / sprite.pixelsPerUnit : 0f;

        private static void ApplySprite(
            SpriteRenderer renderer, Sprite sprite, Color tint, Vector2 size, Vector3 localPosition)
        {
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.transform.localPosition = localPosition;
        }

        private void Awake()
        {
            if (_top == null || _middle == null || _bottom == null || _front == null || _cap == null ||
                _clay == null || _flash == null || _ballRoot == null || _tapTarget == null)
            {
                Debug.LogError(
                    $"{name}: top, middle, bottom, front, cap, clay, flash, ball root and tap target must be assigned.", this);
            }
        }

        private void OnDestroy() => _flashTween?.Kill();
    }
}
