using System.Collections.Generic;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One tube on screen: a sliced back layer, the balls, a sliced glass front layer, and a cap
    /// shown once the tube is completed. The transform's origin is the tube's base centre.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TubeView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _back;
        [SerializeField] private SpriteRenderer _front;
        [SerializeField] private SpriteRenderer _cap;
        [Tooltip("Parent for this tube's balls.")]
        [SerializeField] private Transform _ballRoot;
        [SerializeField] private BoxCollider2D _tapTarget;

        private readonly List<BallView> _balls = new List<BallView>();
        private TubeVisualConfig _config;

        /// <summary>Board index of the tube this view shows.</summary>
        public int TubeIndex { get; private set; }

        /// <summary>Balls currently parented to this tube, bottom first.</summary>
        public IReadOnlyList<BallView> Balls => _balls;

        /// <summary>
        /// Sizes the tube for <paramref name="capacity"/> balls. The tap target also covers the lift
        /// area above the rim, so a tap on a lifted run still hits its tube.
        /// </summary>
        public void Setup(int tubeIndex, int capacity, TubeVisualConfig config)
        {
            TubeIndex = tubeIndex;
            _config = config;

            float height = config.TubeHeight(capacity);
            var bodySize = new Vector2(config.TubeWidth, height);
            var bodyCentre = new Vector3(0f, height * 0.5f, 0f);

            ApplySprite(_back, config.BackSprite, config.BackTint, bodySize, bodyCentre);
            ApplySprite(_front, config.FrontSprite, config.FrontTint, bodySize, bodyCentre);
            ApplySprite(_cap, config.CapSprite, config.CapTint,
                new Vector2(config.TubeWidth, config.CapHeight), new Vector3(0f, height, 0f));
            SetCapVisible(false);

            float tapHeight = height + config.LiftHeight;
            _tapTarget.size = new Vector2(config.TubeWidth + config.HorizontalSpacing, tapHeight);
            _tapTarget.offset = new Vector2(0f, tapHeight * 0.5f);
        }

        /// <summary>Places <paramref name="ball"/> in the next free slot.</summary>
        public void AddBall(BallView ball)
        {
            ball.transform.SetParent(_ballRoot, false);
            ball.transform.localPosition = new Vector3(0f, _config.SlotY(_balls.Count), 0f);
            _balls.Add(ball);
        }

        /// <summary>Hands every ball back to the caller (for pooling) and empties the tube.</summary>
        public void TakeAllBalls(List<BallView> into)
        {
            into.AddRange(_balls);
            _balls.Clear();
        }

        public void SetCapVisible(bool visible) => _cap.enabled = visible && _cap.sprite != null;

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
            if (_back == null || _front == null || _cap == null || _ballRoot == null || _tapTarget == null)
            {
                Debug.LogError($"{name}: back, front, cap, ball root and tap target must be assigned.", this);
            }
        }
    }
}
