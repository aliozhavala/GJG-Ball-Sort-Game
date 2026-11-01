using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// The lock box drawn across a lock's tubes: closed, a side at each edge, the plain face tiled
    /// between them and the lock's coloured mid in the centre; open, the inside behind the tubes and
    /// the two cover halves. Part of a lock's <see cref="CoverView"/>, which sizes and shows it.
    /// </summary>
    /// <remarks>
    /// Every part is drawn at one uniform scale that makes the box as tall as asked, so the parts keep
    /// the art's proportions and line up (they were cut to the same rows, pivots on their bottom
    /// centre). Only the face changes with the width: tiled from each side towards the mid, mirrored
    /// on the right so the box is symmetric. The open box's parts are placed now but stay hidden until
    /// the unlock animation shows them (docs/animation-spec.md).
    /// </remarks>
    public sealed class LockBoxView : MonoBehaviour
    {
        [Header("Closed box")]
        [SerializeField] private SpriteRenderer _sideLeft;
        [SerializeField] private SpriteRenderer _sideRight;
        [SerializeField] private SpriteRenderer _faceLeft;
        [SerializeField] private SpriteRenderer _faceRight;
        [SerializeField] private SpriteRenderer _mid;

        [Header("Open box (hidden until the unlock animation)")]
        [Tooltip("Behind the tubes (TubeBack, under the tube parts).")]
        [SerializeField] private SpriteRenderer _inside;
        [Tooltip("The left half as drawn; the right half is its mirror.")]
        [SerializeField] private SpriteRenderer _coverLeft;
        [SerializeField] private SpriteRenderer _coverRight;

        private SpriteRenderer[] _closedParts;
        private SpriteRenderer[] _allParts;
        private bool _closed;

        /// <summary>Puts the shared parts and this lock's <paramref name="mid"/> on the renderers.</summary>
        public void Setup(TubeVisualConfig config, Sprite mid)
        {
            Init();
            Assign(_sideLeft, config.LockSideSprite, flip: true, SpriteDrawMode.Simple);
            Assign(_sideRight, config.LockSideSprite, flip: false, SpriteDrawMode.Simple);
            Assign(_faceLeft, config.LockFaceSprite, flip: false, SpriteDrawMode.Tiled);
            Assign(_faceRight, config.LockFaceSprite, flip: true, SpriteDrawMode.Tiled);
            Assign(_mid, mid, flip: false, SpriteDrawMode.Simple);
            Assign(_inside, config.LockInsideSprite, flip: false, SpriteDrawMode.Sliced);
            Assign(_coverLeft, config.LockCoverSprite, flip: false, SpriteDrawMode.Simple);
            Assign(_coverRight, config.LockCoverSprite, flip: true, SpriteDrawMode.Simple);
            SetShade(1f);
        }

        /// <summary>
        /// Sizes the box to <paramref name="width"/> × <paramref name="height"/>, its bottom centre at
        /// this object's origin. Narrower than its sides and mid together, it shows no face.
        /// </summary>
        public void Fit(float width, float height)
        {
            Init();
            float artHeight = SpriteHeight(_sideRight.sprite != null ? _sideRight.sprite : _mid.sprite);
            float scale = height / artHeight;
            var scaled = new Vector3(scale, scale, 1f);
            foreach (SpriteRenderer part in _allParts)
            {
                part.transform.localScale = scaled;
            }

            float side = SpriteWidth(_sideRight.sprite) * scale;
            float mid = SpriteWidth(_mid.sprite) * scale;
            float half = width * 0.5f;
            Place(_sideLeft, -half + side * 0.5f);
            Place(_sideRight, half - side * 0.5f);
            Place(_mid, 0f);

            float face = Mathf.Max(0f, (width - 2f * side - mid) * 0.5f);
            _faceLeft.size = new Vector2(face / scale, artHeight);
            _faceRight.size = new Vector2(face / scale, artHeight);
            Place(_faceLeft, -half + side + face * 0.5f);
            Place(_faceRight, half - side - face * 0.5f);

            // The inside spans the box between the sides' outer halves; the covers meet in the middle.
            _inside.size = new Vector2(Mathf.Max(width - side, 0f) / scale, artHeight);
            Place(_inside, 0f);
            float cover = SpriteWidth(_coverLeft.sprite) * scale;
            Place(_coverLeft, -cover * 0.5f);
            Place(_coverRight, cover * 0.5f);
            Refresh();
        }

        /// <summary>Shows the closed box, or nothing (the open box's parts stay hidden for now).</summary>
        public void SetClosed(bool closed)
        {
            Init();
            _closed = closed;
            Refresh();
        }

        private void Refresh()
        {
            foreach (SpriteRenderer part in _allParts)
            {
                part.enabled = false;
            }

            foreach (SpriteRenderer part in _closedParts)
            {
                part.enabled = _closed && part.sprite != null;
            }

            // The face shows only where the box is wider than its sides and mid.
            _faceLeft.enabled &= _faceLeft.size.x > 0f;
            _faceRight.enabled &= _faceRight.size.x > 0f;
        }

        /// <summary>Darkens every part by <paramref name="shade"/> (1 = as drawn).</summary>
        public void SetShade(float shade)
        {
            Init();
            var colour = new Color(shade, shade, shade, 1f);
            foreach (SpriteRenderer part in _allParts)
            {
                part.color = colour;
            }
        }

        private static void Assign(SpriteRenderer part, Sprite sprite, bool flip, SpriteDrawMode mode)
        {
            part.sprite = sprite;
            part.flipX = flip;
            part.drawMode = mode;
            if (mode == SpriteDrawMode.Tiled)
            {
                part.tileMode = SpriteTileMode.Continuous;
            }
        }

        private static void Place(SpriteRenderer part, float x) => part.transform.localPosition = new Vector3(x, 0f, 0f);

        private static float SpriteWidth(Sprite sprite) => sprite != null ? sprite.bounds.size.x : 0f;

        private static float SpriteHeight(Sprite sprite) => sprite != null ? sprite.bounds.size.y : 1f;

        private void Init()
        {
            if (_allParts != null)
            {
                return;
            }

            _closedParts = new[] { _sideLeft, _sideRight, _faceLeft, _faceRight, _mid };
            _allParts = new[] { _sideLeft, _sideRight, _faceLeft, _faceRight, _mid, _inside, _coverLeft, _coverRight };
        }

        private void Awake()
        {
            if (_sideLeft == null || _sideRight == null || _faceLeft == null || _faceRight == null || _mid == null ||
                _inside == null || _coverLeft == null || _coverRight == null)
            {
                Debug.LogError($"{name}: every lock box part must be assigned.", this);
            }
        }
    }
}
