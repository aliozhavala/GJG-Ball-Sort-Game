using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Tube art and geometry, in world units of an unscaled board. <see cref="Layout.BoardLayout"/>
    /// scales the whole board afterwards, so these only need to be right relative to each other.
    /// </summary>
    [CreateAssetMenu(fileName = "TubeVisualConfig", menuName = "Ball Sort/Tube Visual Config")]
    public sealed class TubeVisualConfig : ScriptableObject
    {
        [Header("Sprites (9-sliced: Mesh Type = Full Rect, borders set)")]
        [SerializeField] private Sprite _backSprite;
        [SerializeField] private Sprite _frontSprite;
        [SerializeField] private Sprite _capSprite;
        [SerializeField] private Color _backTint = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color _frontTint = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color _capTint = Color.white;

        [Header("Geometry")]
        [Tooltip("Width of the tube body.")]
        [SerializeField, Min(0.1f)] private float _tubeWidth = 1.1f;
        [Tooltip("Vertical distance between ball centres.")]
        [SerializeField, Min(0.1f)] private float _slotHeight = 1f;
        [Tooltip("Height of the bottom ball's centre above the tube's base.")]
        [SerializeField, Min(0f)] private float _bottomOffset = 0.55f;
        [Tooltip("Space between the top slot's ball edge and the tube's rim.")]
        [SerializeField, Min(0f)] private float _topPadding = 0.15f;
        [SerializeField, Min(0.1f)] private float _ballDiameter = 0.9f;
        [SerializeField, Min(0.05f)] private float _capHeight = 0.5f;
        [Tooltip("How far a selected run rises; stays inside the two-slot gap between rows. Part of the tap target.")]
        [SerializeField, Min(0f)] private float _liftHeight = 1f;

        [Header("Spacing")]
        [Tooltip("Gap between neighbouring tubes. Rows are spaced by the grid rule (two ball slots), not here.")]
        [SerializeField, Min(0f)] private float _horizontalSpacing = 0.45f;

        public Sprite BackSprite => _backSprite;
        public Sprite FrontSprite => _frontSprite;
        public Sprite CapSprite => _capSprite;
        public Color BackTint => _backTint;
        public Color FrontTint => _frontTint;
        public Color CapTint => _capTint;
        public float TubeWidth => _tubeWidth;
        public float SlotHeight => _slotHeight;
        public float BottomOffset => _bottomOffset;
        public float BallDiameter => _ballDiameter;
        public float CapHeight => _capHeight;
        public float LiftHeight => _liftHeight;
        public float HorizontalSpacing => _horizontalSpacing;

        /// <summary>Local y of a ball's centre in <paramref name="slotIndex"/>, measured from the tube base.</summary>
        public float SlotY(int slotIndex) => _bottomOffset + slotIndex * _slotHeight;

        /// <summary>Body height of a tube that holds <paramref name="capacity"/> balls.</summary>
        public float TubeHeight(int capacity) => SlotY(capacity - 1) + _ballDiameter * 0.5f + _topPadding;

        private void OnValidate()
        {
            if (_backSprite == null || _frontSprite == null)
            {
                Debug.LogWarning($"{name}: back and front sprites must be assigned.", this);
            }
        }
    }
}
