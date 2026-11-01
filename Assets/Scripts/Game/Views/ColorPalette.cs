using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Maps ball colour ids to their sprites (one per <see cref="BallExpression"/>), sprite tint,
    /// colour and the cork of a tube completed in it. The single place art swaps ball visuals.
    /// </summary>
    /// <remarks>
    /// Every delivered ball sprite (the monsters and the hidden ball) shares one canvas, body size and
    /// body centre, so all are drawn at one <see cref="ArtSpriteSize"/>; only the default (placeholder)
    /// sprite is drawn at size 1. The key a ball carries is drawn from the <see cref="LockPalette"/>.
    /// </remarks>
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
            [Tooltip("Shocked: an invalid tap on the ball's tube. Empty = the closed-eyes face.")]
            [SerializeField] private Sprite _shock;
            [Tooltip("Multiplies the sprite: white for coloured art, the colour itself for a white placeholder.")]
            [SerializeField] private Color _spriteTint;
            [Tooltip("The colour where it is shown without a ball: the glow, the trail, droplets, the plain cork.")]
            [FormerlySerializedAs("_tint")]
            [SerializeField] private Color _color;
            [Tooltip("Cork of a tube completed in this colour, cut on the tube art's canvas (pivot at the rim's top). " +
                "Empty = TubeVisualConfig's plain cork, tinted.")]
            [SerializeField] private Sprite _cork;
            [Tooltip("Stand drawn under a large tube that starts with this colour, cut on the tube art's canvas " +
                "(pivot at the tube's base). Empty = no stand.")]
            [SerializeField] private Sprite _stand;

            public Color SpriteTint => _spriteTint;
            public Sprite Cork => _cork;
            public Sprite Stand => _stand;
            public Color Color => _color;

            public Sprite GetSprite(BallExpression expression)
            {
                Sprite sprite = expression switch
                {
                    BallExpression.HalfOpen => _halfOpen,
                    BallExpression.Closed => _closed,
                    BallExpression.Laugh => _laugh,
                    BallExpression.Shock => _shock != null ? _shock : _closed,
                    _ => _open,
                };
                return sprite != null ? sprite : _open;
            }
        }

        [Tooltip("Used for any entry that has no sprite of its own (drawn at sprite size 1).")]
        [SerializeField] private Sprite _defaultSprite;
        [Tooltip("Width of the ball art's canvas in ball diameters (how much wider it is than the round body). " +
            "One value for every ball sprite and the hidden ball; the default sprite is drawn at 1.")]
        [SerializeField, Min(0.1f)] private float _artSpriteSize = 1.4628571f;
        [SerializeField] private Entry[] _entries = new Entry[0];

        [Header("Hidden balls")]
        [SerializeField] private Sprite _hiddenSprite;
        [SerializeField] private Color _hiddenTint = Color.white;
        [Tooltip("Drawn over a hidden ball (the \"?\"). Leave empty when the hidden sprite shows one itself.")]
        [SerializeField] private Sprite _hiddenMarkSprite;
        [Tooltip("Width of the hidden mark as a fraction of the ball's diameter.")]
        [SerializeField, Range(0.1f, 1f)] private float _hiddenMarkSize = 0.55f;

        [Header("Keys (the lock's key sprite from the LockPalette, held in the monster's hands)")]
        [Tooltip("Width of the key as a fraction of the ball's diameter.")]
        [SerializeField, Range(0.1f, 1f)] private float _keyMarkSize = 0.5f;
        [Tooltip("Centre of the key relative to the ball's centre, in ball diameters.")]
        [SerializeField] private Vector2 _keyMarkOffset = new Vector2(0f, -0.3f);

        /// <summary>How many colour ids this palette can draw.</summary>
        public int Count => _entries.Length;

        public Sprite HiddenSprite => _hiddenSprite != null ? _hiddenSprite : _defaultSprite;

        public Color HiddenTint => _hiddenTint;

        public float HiddenSpriteSize => _hiddenSprite != null ? _artSpriteSize : 1f;

        /// <summary>Width of every delivered ball sprite in ball diameters.</summary>
        public float ArtSpriteSize => _artSpriteSize;

        public Sprite HiddenMarkSprite => _hiddenMarkSprite;

        public float HiddenMarkSize => _hiddenMarkSize;

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

        /// <summary>The colour itself, for things that show it without a ball (glow, trail, droplets).</summary>
        public Color GetColor(int colorId) => IsInRange(colorId) ? _entries[colorId].Color : Color.magenta;

        /// <summary>The cork drawn on a tube completed in <paramref name="colorId"/>; null when the colour has none.</summary>
        public Sprite GetCork(int colorId) => IsInRange(colorId) ? _entries[colorId].Cork : null;

        /// <summary>The stand under a large tube that starts with <paramref name="colorId"/>; null for none.</summary>
        public Sprite GetStand(int colorId) => IsInRange(colorId) ? _entries[colorId].Stand : null;

        /// <summary>Width of <paramref name="colorId"/>'s sprite in ball diameters: the art's, or 1 for the default sprite.</summary>
        public float GetSpriteSize(int colorId)
        {
            Sprite sprite = GetSprite(colorId, BallExpression.Open);
            return sprite != null && sprite != _defaultSprite ? _artSpriteSize : 1f;
        }

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
