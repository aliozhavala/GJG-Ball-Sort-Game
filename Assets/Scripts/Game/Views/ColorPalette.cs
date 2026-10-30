using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Maps ball colour ids to their sprites (one per <see cref="BallExpression"/>), sprite tint and
    /// colour. The single place art swaps ball visuals.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "Ball Sort/Color Palette")]
    public sealed class ColorPalette : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [Tooltip("Eyes open: the normal look. Also stands in for any expression left empty.")]
            [FormerlySerializedAs("_sprite")]
            [SerializeField] private Sprite _open;
            [Tooltip("Eyes half closed (blink frame).")]
            [SerializeField] private Sprite _halfOpen;
            [Tooltip("Eyes closed (blink frame).")]
            [SerializeField] private Sprite _closed;
            [Tooltip("Laughing: balls in a completed tube.")]
            [SerializeField] private Sprite _laugh;
            [Tooltip("Multiplies the sprite: white for coloured art, the colour itself for a white placeholder.")]
            [SerializeField] private Color _spriteTint;
            [Tooltip("The colour where it is shown without a ball, e.g. the veil's badge.")]
            [FormerlySerializedAs("_tint")]
            [SerializeField] private Color _color;
            [Tooltip("Sprite width in ball diameters: how much wider the art is than the ball's round body. 0 = 1.")]
            [SerializeField, Min(0f)] private float _spriteSize;

            public Color SpriteTint => _spriteTint;
            public Color Color => _color;
            public float SpriteSize => _spriteSize > 0f ? _spriteSize : 1f;

            public Sprite GetSprite(BallExpression expression)
            {
                Sprite sprite = expression switch
                {
                    BallExpression.HalfOpen => _halfOpen,
                    BallExpression.Closed => _closed,
                    BallExpression.Laugh => _laugh,
                    _ => _open,
                };
                return sprite != null ? sprite : _open;
            }
        }

        [Tooltip("Used for any entry that has no sprite of its own (drawn at sprite size 1).")]
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private Entry[] _entries = new Entry[0];

        [Header("Hidden balls")]
        [SerializeField] private Sprite _hiddenSprite;
        [SerializeField] private Color _hiddenTint = Color.white;
        [Tooltip("Sprite width in ball diameters, as for a colour; lets hidden art match the revealed balls' size.")]
        [SerializeField, Min(0.1f)] private float _hiddenSpriteSize = 1f;
        [Tooltip("Drawn over a hidden ball (the \"?\"). Leave empty when the hidden sprite shows one itself.")]
        [SerializeField] private Sprite _hiddenMarkSprite;
        [Tooltip("Width of the hidden mark as a fraction of the ball's diameter.")]
        [SerializeField, Range(0.1f, 1f)] private float _hiddenMarkSize = 0.55f;

        [Header("Keys")]
        [Tooltip("Drawn on a revealed ball that carries a key, tinted from the LockPalette.")]
        [SerializeField] private Sprite _keyMarkSprite;
        [Tooltip("Width of the key mark as a fraction of the ball's diameter.")]
        [SerializeField, Range(0.1f, 1f)] private float _keyMarkSize = 0.75f;
        [Tooltip("Centre of the key mark relative to the ball's centre, in ball diameters.")]
        [SerializeField] private Vector2 _keyMarkOffset = new Vector2(0.25f, -0.25f);

        /// <summary>How many colour ids this palette can draw.</summary>
        public int Count => _entries.Length;

        public Sprite HiddenSprite => _hiddenSprite != null ? _hiddenSprite : _defaultSprite;

        public Color HiddenTint => _hiddenTint;

        public float HiddenSpriteSize => _hiddenSprite != null ? _hiddenSpriteSize : 1f;

        public Sprite HiddenMarkSprite => _hiddenMarkSprite;

        public float HiddenMarkSize => _hiddenMarkSize;

        public Sprite KeyMarkSprite => _keyMarkSprite;

        public float KeyMarkSize => _keyMarkSize;

        public Vector2 KeyMarkOffset => _keyMarkOffset;

        /// <summary>The ball sprite of <paramref name="colorId"/> wearing <paramref name="expression"/>.</summary>
        public Sprite GetSprite(int colorId, BallExpression expression)
        {
            Sprite sprite = IsInRange(colorId) ? _entries[colorId].GetSprite(expression) : null;
            return sprite != null ? sprite : _defaultSprite;
        }

        /// <remarks>Out-of-range ids render magenta so a palette that is too short is obvious on screen.</remarks>
        public Color GetSpriteTint(int colorId) => IsInRange(colorId) ? _entries[colorId].SpriteTint : Color.magenta;

        /// <summary>The colour itself, for things that show it without a ball (the veil's badge).</summary>
        public Color GetColor(int colorId) => IsInRange(colorId) ? _entries[colorId].Color : Color.magenta;

        /// <summary>Width of <paramref name="colorId"/>'s sprite in ball diameters.</summary>
        public float GetSpriteSize(int colorId) =>
            IsInRange(colorId) && _entries[colorId].GetSprite(BallExpression.Open) != null
                ? _entries[colorId].SpriteSize
                : 1f;

        private bool IsInRange(int colorId) => colorId >= 0 && colorId < _entries.Length;

        private void OnValidate()
        {
            if (_defaultSprite == null)
            {
                Debug.LogWarning($"{name}: default sprite is not assigned.", this);
            }
        }
    }
}
