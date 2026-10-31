using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Tube art and geometry, in world units of an unscaled board. <see cref="Layout.BoardLayout"/>
    /// scales the whole board afterwards, so these only need to be right relative to each other.
    /// </summary>
    /// <remarks>
    /// The tube art is drawn at one scale, <see cref="ArtScale"/>, that makes its body
    /// <see cref="TubeWidth"/> wide; the clay and veil art share it, so they keep their size relative
    /// to the tube. Each piece is placed by its pivot, which Ball Sort ▸ Art ▸ Prepare Sprites sets on
    /// the walls' centre line: the rim's two halves, the corks and the veil at the rim's top, the
    /// middle part at its bottom, the bottom part, the outline and the clay at the base.
    /// <para>
    /// The rim is drawn in two halves so a ball inside the tube sits in its mouth: the back behind
    /// the balls, the front over them and over the cork.
    /// </para>
    /// <para>
    /// Ball spacing is <see cref="BottomOffset"/> and <see cref="SlotHeight"/>. Tube heights and the row
    /// pitch (<c>LevelLayout.RowPitchSlots</c> slots) both follow from them, so capacity-10 and -16 tubes
    /// still reach the top row of their span whatever the values. Changing them in the Inspector during
    /// Play mode re-lays the board out (<see cref="Changed"/>).
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "TubeVisualConfig", menuName = "Ball Sort/Tube Visual Config")]
    public sealed class TubeVisualConfig : ScriptableObject
    {
        [Header("Tube (rim and bottom keep their size, the middle stretches to the tube's height)")]
        [Tooltip("The rim's back half, behind the balls. Shares its rectangle and pivot with the front half.")]
        [FormerlySerializedAs("_topSprite")]
        [SerializeField] private Sprite _rimBackSprite;
        [Tooltip("The rim's front half (with the neck), over the balls in the tube and the cork.")]
        [SerializeField] private Sprite _rimFrontSprite;
        [SerializeField] private Sprite _middleSprite;
        [SerializeField] private Sprite _bottomSprite;
        [SerializeField] private Color _tubeTint = Color.white;
        [Tooltip("Width of the tube's body (outer edges of the walls) in the tube art, in pixels. Scales the art to Tube Width.")]
        [SerializeField, Min(1f)] private float _artBodyWidth = 145f;
        [Tooltip("The whole tube in one sprite, 9-sliced at the rim and the bottom; the invalid-tap flash uses it.")]
        [SerializeField] private Sprite _outlineSprite;

        [Header("Glass front (optional; shaped like the outline, drawn over the balls)")]
        [SerializeField] private Sprite _frontSprite;
        [SerializeField] private Color _frontTint = new Color(1f, 1f, 1f, 0.12f);

        [Header("Cork on a completed tube: the colour's own (ColorPalette), else this plain one, tinted")]
        [Tooltip("Drawn at the tube art's scale, its pivot at the rim's top.")]
        [SerializeField] private Sprite _capSprite;
        [Tooltip("Multiplies the tube's colour on the plain cork; its alpha is every cork's.")]
        [SerializeField] private Color _capTint = Color.white;

        [Header("Glow around a completed tube (9-sliced, pivot at the bottom centre, tinted in the tube's colour)")]
        [SerializeField] private Sprite _glowSprite;
        [Tooltip("Multiplies the tube's colour.")]
        [SerializeField] private Color _glowTint = Color.white;
        [Tooltip("How far the glow reaches past the tube's body on every side.")]
        [SerializeField, Min(0f)] private float _glowPadding = 0.12f;

        [Header("Geometry")]
        [Tooltip("Width of the tube body.")]
        [SerializeField, Min(0.1f)] private float _tubeWidth = 1.1f;
        [Tooltip("Vertical distance between ball centres. Tube heights and row spacing follow from it.")]
        [SerializeField, Min(0.1f)] private float _slotHeight = 0.91f;
        [Tooltip("Height of the bottom ball's centre above the tube's base.")]
        [SerializeField, Min(0f)] private float _bottomOffset = 0.59f;
        [Tooltip("Space between the top slot's ball edge and the tube's rim.")]
        [SerializeField, Min(0f)] private float _topPadding = 0.15f;
        [SerializeField, Min(0.1f)] private float _ballDiameter = 0.9f;

        [Header("Spacing")]
        [Tooltip("Gap between neighbouring tubes. Rows are spaced by the grid rule (two ball slots), not here.")]
        [SerializeField, Min(0f)] private float _horizontalSpacing = 0.45f;

        [Header("Veil (9-sliced; may be translucent, covered balls are hidden separately)")]
        [Tooltip("Hangs from the rim at the art's scale; its middle stretches for taller tubes.")]
        [SerializeField] private Sprite _veilSprite;
        [SerializeField] private Color _veilTint = Color.white;
        [Tooltip("Vertical scale of the veil art: shortens it so its hem crosses the bottom ball, whose lower half " +
            "shows. Taller tubes add their extra height to the stripes.")]
        [SerializeField, Range(0.3f, 1f)] private float _veilHeightScale = 0.85f;
        [Tooltip("Drawn in the veil's centre, tinted with the ball colour that opens it.")]
        [SerializeField] private Sprite _veilBadgeSprite;
        [SerializeField, Min(0.05f)] private float _veilBadgeSize = 0.8f;

        [Header("Door (9-sliced; one per lock, across all of its tubes)")]
        [SerializeField] private Sprite _doorSprite;
        [SerializeField] private Color _doorTint = new Color(0.22f, 0.26f, 0.33f);
        [Tooltip("Drawn in the door's centre, tinted from the LockPalette.")]
        [SerializeField] private Sprite _lockIconSprite;
        [SerializeField, Min(0.05f)] private float _lockIconSize = 1f;

        [Header("Clay (ice at the base of a clay tube: over the balls in it, under balls in flight)")]
        [Tooltip("Scaled to the tube's drawn width (its rim) times Clay Scale, keeping its proportions.")]
        [SerializeField] private Sprite _claySprite;
        [SerializeField] private Color _clayTint = Color.white;
        [Tooltip("Size of the ice relative to the tube's drawn width.")]
        [SerializeField, Range(0.5f, 2f)] private float _clayScale = 1.1f;

        [Header("Shuffle pick (tubes the Shuffle booster can and cannot pick)")]
        [Tooltip("The glow's colour around a tube the Shuffle booster can pick.")]
        [SerializeField] private Color _shuffleHighlightTint = new Color(1f, 0.95f, 0.55f, 0.9f);
        [Tooltip("Brightness of the tubes it cannot pick, their balls and covers (1 = as drawn).")]
        [SerializeField, Range(0f, 1f)] private float _shuffleDimShade = 0.45f;

        /// <summary>
        /// Editor only: raised from <c>OnValidate</c> when a value changes in the Inspector, so a board on
        /// screen can lay itself out again. Listeners must not build or destroy objects in the handler
        /// (Unity forbids it during OnValidate); they note the change and act on their next frame. Never
        /// raised in a build.
        /// </summary>
        public event Action Changed;

        public Sprite RimBackSprite => _rimBackSprite;
        public Sprite RimFrontSprite => _rimFrontSprite;
        public Sprite MiddleSprite => _middleSprite;
        public Sprite BottomSprite => _bottomSprite;
        public Color TubeTint => _tubeTint;
        public Sprite OutlineSprite => _outlineSprite;
        public Sprite FrontSprite => _frontSprite;
        public Color FrontTint => _frontTint;
        public Sprite CapSprite => _capSprite;
        public Color CapTint => _capTint;
        public Sprite GlowSprite => _glowSprite;
        public Color GlowTint => _glowTint;
        public float GlowPadding => _glowPadding;
        public float TubeWidth => _tubeWidth;
        public float SlotHeight => _slotHeight;
        public float BottomOffset => _bottomOffset;
        public float BallDiameter => _ballDiameter;
        public float HorizontalSpacing => _horizontalSpacing;
        public Sprite VeilSprite => _veilSprite;
        public Color VeilTint => _veilTint;
        public float VeilHeightScale => _veilHeightScale;
        public Sprite VeilBadgeSprite => _veilBadgeSprite;
        public float VeilBadgeSize => _veilBadgeSize;
        public Sprite DoorSprite => _doorSprite;
        public Color DoorTint => _doorTint;
        public Sprite LockIconSprite => _lockIconSprite;
        public float LockIconSize => _lockIconSize;
        public Sprite ClaySprite => _claySprite;
        public Color ClayTint => _clayTint;
        public float ClayScale => _clayScale;
        public Color ShuffleHighlightTint => _shuffleHighlightTint;
        public float ShuffleDimShade => _shuffleDimShade;

        /// <summary>Scale that makes the tube art's body <see cref="TubeWidth"/> wide.</summary>
        public float ArtScale =>
            _bottomSprite != null ? _tubeWidth * _bottomSprite.pixelsPerUnit / _artBodyWidth : 1f;

        /// <summary>Local y of a ball's centre in <paramref name="slotIndex"/>, measured from the tube base.</summary>
        public float SlotY(int slotIndex) => _bottomOffset + slotIndex * _slotHeight;

        /// <summary>Body height of a tube that holds <paramref name="capacity"/> balls.</summary>
        public float TubeHeight(int capacity) => SlotY(capacity - 1) + _ballDiameter * 0.5f + _topPadding;

        private void OnValidate()
        {
            if (_rimBackSprite == null || _rimFrontSprite == null || _middleSprite == null || _bottomSprite == null ||
                _outlineSprite == null)
            {
                Debug.LogWarning($"{name}: rim back and front, middle, bottom and outline sprites must be assigned.", this);
            }

            // Covered balls are hidden either way, so a missing cover sprite would show an empty tube.
            if (_veilSprite == null || _doorSprite == null || _claySprite == null)
            {
                Debug.LogWarning($"{name}: veil, door and clay sprites must be assigned.", this);
            }

            Changed?.Invoke();
        }
    }
}
