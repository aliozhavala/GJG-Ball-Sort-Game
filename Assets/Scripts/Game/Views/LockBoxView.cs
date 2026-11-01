using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// The lock box drawn across a lock's tubes: closed, a side at each edge, the plain face tiled
    /// between them and the lock's coloured mid in the centre; while it opens, the inside behind the
    /// tubes, the two cover halves sliding apart and the key flying into the slot. Part of a lock's
    /// <see cref="CoverView"/>, which sizes it and drives the unlock (docs/animation-spec.md, 3.3).
    /// </summary>
    /// <remarks>
    /// Every part is drawn at one uniform scale that makes the box as tall as asked, so the parts keep
    /// the art's proportions and line up (they were cut to the same rows, pivots on their bottom
    /// centre). Only the face changes with the width: tiled from each side towards the mid, mirrored
    /// on the right so the box is symmetric.
    /// <para>
    /// Opening, the faces are the door panels and the covers their leading edges: each face runs from
    /// its side to its cover's centre, so it shrinks as the cover slides out. A sprite mask the size of
    /// the box clips the covers at its edges, under the sides, like doors sliding into the frame.
    /// </para>
    /// </remarks>
    public sealed class LockBoxView : MonoBehaviour
    {
        [Header("Closed box")]
        [SerializeField] private SpriteRenderer _sideLeft;
        [SerializeField] private SpriteRenderer _sideRight;
        [SerializeField] private SpriteRenderer _faceLeft;
        [SerializeField] private SpriteRenderer _faceRight;
        [SerializeField] private SpriteRenderer _mid;

        [Header("Opening box")]
        [Tooltip("Behind the tubes (TubeBack, under the tube parts).")]
        [SerializeField] private SpriteRenderer _inside;
        [Tooltip("The left half as drawn; the right half is its mirror. Visible inside the mask only.")]
        [SerializeField] private SpriteRenderer _coverLeft;
        [SerializeField] private SpriteRenderer _coverRight;
        [Tooltip("Clips the sliding covers to the box (a square sprite, stretched to the box).")]
        [SerializeField] private SpriteMask _mask;
        [Tooltip("The lock's key flying into the slot, above the box.")]
        [SerializeField] private SpriteRenderer _key;

        private enum Phase { Closed, Opening, Open }

        private SpriteRenderer[] _closedParts;
        private SpriteRenderer[] _allParts;
        private SpriteRenderer[] _fadingParts;
        private Phase _phase;
        private float _scale = 1f;
        private float _width;
        private float _artHeight = 1f;
        private float _sideWidth;
        private float _coverInner;
        private float _doors;
        private float _fade = 1f;
        private float _shade = 1f;
        private float _keyAlpha;
        private Vector2 _slotCentre;
        private float _slotKeyWidth;
        private float _coverInnerEdge;

        /// <summary>The middle of the key slot, in this box's space.</summary>
        public Vector3 SlotPosition { get; private set; }

        /// <summary>Width of the key seated in the slot, in this box's space.</summary>
        public float SeatedKeyWidth { get; private set; }

        /// <summary>Puts the shared parts and this lock's <paramref name="mid"/> and <paramref name="key"/> on the renderers.</summary>
        public void Setup(TubeVisualConfig config, Sprite mid, Sprite key)
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
            Assign(_key, key, flip: false, SpriteDrawMode.Simple);
            _slotCentre = config.LockSlotCentre;
            _slotKeyWidth = config.LockSlotKeyWidth;
            _coverInnerEdge = config.LockCoverInnerEdge;
            _shade = 1f;
            SetClosed(true);
        }

        /// <summary>
        /// Sizes the box to <paramref name="width"/> × <paramref name="height"/>, its bottom centre at
        /// this object's origin. Narrower than its sides and mid together, it shows no face. An opening
        /// box keeps its progress.
        /// </summary>
        public void Fit(float width, float height)
        {
            Init();
            _artHeight = SpriteHeight(_sideRight.sprite != null ? _sideRight.sprite : _mid.sprite);
            _scale = height / _artHeight;
            _width = width;
            var scaled = new Vector3(_scale, _scale, 1f);
            foreach (SpriteRenderer part in _allParts)
            {
                part.transform.localScale = scaled;
            }

            _sideWidth = SpriteWidth(_sideRight.sprite) * _scale;
            float mid = SpriteWidth(_mid.sprite) * _scale;
            float half = width * 0.5f;
            Place(_sideLeft, -half + _sideWidth * 0.5f);
            Place(_sideRight, half - _sideWidth * 0.5f);
            Place(_mid, 0f);

            // The inside spans the box between the sides' outer halves.
            _inside.size = new Vector2(Mathf.Max(width - _sideWidth, 0f) / _scale, _artHeight);
            Place(_inside, 0f);

            // The covers meet where their bars end, short of the sprite's transparent edge.
            _coverInner = (_coverInnerEdge - 0.5f) * SpriteWidth(_coverLeft.sprite) * _scale;

            Bounds maskBounds = _mask.sprite != null ? _mask.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            var maskScale = new Vector3(width / maskBounds.size.x, height / maskBounds.size.y, 1f);
            _mask.transform.localScale = maskScale;
            _mask.transform.localPosition = new Vector3(
                -maskBounds.center.x * maskScale.x, height * 0.5f - maskBounds.center.y * maskScale.y, 0f);

            float midWidth = mid > 0f ? mid : width;
            SlotPosition = new Vector3((_slotCentre.x - 0.5f) * midWidth, _slotCentre.y * height, 0f);
            SeatedKeyWidth = _slotKeyWidth * midWidth;

            PlaceFacesAndCovers();
            Refresh();
        }

        /// <summary>Shows the closed box, or nothing (open). Ends an opening at once.</summary>
        public void SetClosed(bool closed)
        {
            Init();
            _phase = closed ? Phase.Closed : Phase.Open;
            _doors = 0f;
            _fade = 1f;
            _keyAlpha = 0f;
            SetWobble(0f, 0f);
            PlaceFacesAndCovers();
            Refresh();
        }

        /// <summary>Darkens every part by <paramref name="shade"/> (1 = as drawn).</summary>
        public void SetShade(float shade)
        {
            Init();
            _shade = shade;
            ApplyColours();
        }

        /// <summary>The flying or seated key: its centre and width in this box's space, and its alpha (0 hides it).</summary>
        public void SetKeyPose(Vector3 position, float width, float alpha)
        {
            _key.transform.localPosition = position;
            _key.transform.localScale = Vector3.one * (width / Mathf.Max(SpriteWidth(_key.sprite), 0.0001f));
            _keyAlpha = alpha;
            _key.enabled = alpha > 0f && _key.sprite != null;
            ApplyColours();
        }

        /// <summary>The box's wobble when the key seats: a tilt in degrees and a squash (wider and lower), about its base.</summary>
        public void SetWobble(float angle, float squash)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(1f + squash, 1f - squash, 1f);
        }

        /// <summary>
        /// The mid gives way to the two cover halves meeting in the middle; the inside shows behind the
        /// tubes. The faces and sides stay until the covers have slid over them.
        /// </summary>
        public void BeginOpening()
        {
            _phase = Phase.Opening;
            _doors = 0f;
            _fade = 1f;
            PlaceFacesAndCovers();
            Refresh();
        }

        /// <summary>How far the covers have slid: 0 meeting in the middle, 1 centred on the box's edges.</summary>
        public void SetDoors(float progress)
        {
            _doors = progress;
            PlaceFacesAndCovers();
            Refresh();
        }

        /// <summary>The open box's alpha (inside, covers, sides), 1 → 0 at the end of the opening.</summary>
        public void SetFade(float alpha)
        {
            _fade = alpha;
            ApplyColours();
        }

        private void PlaceFacesAndCovers()
        {
            float half = _width * 0.5f;
            float inner = half - _sideWidth;
            if (_phase != Phase.Opening)
            {
                // Closed: each face runs from its side to the mid.
                float mid = SpriteWidth(_mid.sprite) * _scale;
                PlaceFace(Mathf.Max(0f, inner - mid * 0.5f), half);
                Place(_coverLeft, -_coverInner);
                Place(_coverRight, _coverInner);
                return;
            }

            // The covers' centres slide from meeting in the middle to the box's edges (never inwards).
            float centre = Mathf.Lerp(_coverInner, Mathf.Max(_coverInner, half), _doors);
            Place(_coverLeft, -centre);
            Place(_coverRight, centre);
            PlaceFace(Mathf.Max(0f, inner - centre), half);
        }

        // Each face runs inwards from its side, `width` wide.
        private void PlaceFace(float width, float half)
        {
            var size = new Vector2(width / _scale, _artHeight);
            _faceLeft.size = size;
            _faceRight.size = size;
            Place(_faceLeft, -half + _sideWidth + width * 0.5f);
            Place(_faceRight, half - _sideWidth - width * 0.5f);
        }

        private void Refresh()
        {
            foreach (SpriteRenderer part in _allParts)
            {
                part.enabled = false;
            }

            bool closed = _phase == Phase.Closed;
            bool opening = _phase == Phase.Opening;
            Show(_sideLeft, closed || opening);
            Show(_sideRight, closed || opening);
            Show(_faceLeft, (closed || opening) && _faceLeft.size.x > 0f);
            Show(_faceRight, (closed || opening) && _faceRight.size.x > 0f);
            Show(_mid, closed);
            Show(_inside, opening);
            Show(_coverLeft, opening);
            Show(_coverRight, opening);
            _mask.enabled = opening;
            _key.enabled = _keyAlpha > 0f && _key.sprite != null;
            ApplyColours();
        }

        private static void Show(SpriteRenderer part, bool shown) => part.enabled = shown && part.sprite != null;

        private void ApplyColours()
        {
            var solid = new Color(_shade, _shade, _shade, 1f);
            foreach (SpriteRenderer part in _closedParts)
            {
                part.color = solid;
            }

            // The parts still drawn while the open box fades.
            var faded = new Color(_shade, _shade, _shade, _fade);
            foreach (SpriteRenderer part in _fadingParts)
            {
                part.color = faded;
            }

            _key.color = new Color(_shade, _shade, _shade, _keyAlpha);
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

            _closedParts = new[] { _faceLeft, _faceRight, _mid };
            _fadingParts = new[] { _sideLeft, _sideRight, _inside, _coverLeft, _coverRight };
            _allParts = new[] { _sideLeft, _sideRight, _faceLeft, _faceRight, _mid, _inside, _coverLeft, _coverRight };
        }

        private void Awake()
        {
            if (_sideLeft == null || _sideRight == null || _faceLeft == null || _faceRight == null || _mid == null ||
                _inside == null || _coverLeft == null || _coverRight == null || _mask == null || _key == null)
            {
                Debug.LogError($"{name}: every lock box part, the mask and the key must be assigned.", this);
            }
        }
    }
}
