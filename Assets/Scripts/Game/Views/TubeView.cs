using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One tube on screen: a sliced back layer, the balls, a sliced glass front layer, a cap shown
    /// once the tube is completed, a clay overlay on clay tubes and a flash for refused taps. The
    /// transform's origin is the tube's base centre.
    /// </summary>
    /// <remarks>
    /// Covers are not part of the tube: a <see cref="CoverView"/> may span several tubes, and hides
    /// their balls through <see cref="SetBallsVisible"/>.
    /// </remarks>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TubeView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _back;
        [SerializeField] private SpriteRenderer _front;
        [SerializeField] private SpriteRenderer _cap;
        [Tooltip("Translucent overlay on the bottom of a clay tube.")]
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
            var bodySize = new Vector2(config.TubeWidth, height);
            var bodyCentre = new Vector3(0f, height * 0.5f, 0f);

            ApplySprite(_back, config.BackSprite, config.BackTint, bodySize, bodyCentre);
            ApplySprite(_front, config.FrontSprite, config.FrontTint, bodySize, bodyCentre);
            ApplySprite(_cap, config.CapSprite, config.CapTint,
                new Vector2(config.TubeWidth, config.CapHeight), new Vector3(0f, height, 0f));
            SetCapVisible(false);

            float clayHeight = Mathf.Min(config.ClayHeight, height);
            ApplySprite(_clay, config.ClaySprite, config.ClayTint,
                new Vector2(config.TubeWidth, clayHeight), new Vector3(0f, clayHeight * 0.5f, 0f));
            _clay.enabled = isClay && _clay.sprite != null;

            // The flash uses the back sprite: it is the tube's silhouette.
            _flashTween?.Kill();
            ApplySprite(_flash, config.BackSprite, Color.clear, bodySize, bodyCentre);

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

        private Vector3 SlotPosition(int slot) => new Vector3(0f, _config.SlotY(slot), 0f);

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
            if (_back == null || _front == null || _cap == null || _clay == null || _flash == null ||
                _ballRoot == null || _tapTarget == null)
            {
                Debug.LogError($"{name}: back, front, cap, clay, flash, ball root and tap target must be assigned.", this);
            }
        }

        private void OnDestroy() => _flashTween?.Kill();
    }
}
