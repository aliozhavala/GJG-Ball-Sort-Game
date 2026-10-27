using System;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Maps ball colour ids to their sprite and tint. The single place art swaps ball visuals.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "Ball Sort/Color Palette")]
    public sealed class ColorPalette : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private Sprite _sprite;
            [SerializeField] private Color _tint;

            public Sprite Sprite => _sprite;
            public Color Tint => _tint;
        }

        [Tooltip("Used for any entry that has no sprite of its own.")]
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private Entry[] _entries = new Entry[0];

        [Header("Hidden balls")]
        [SerializeField] private Sprite _hiddenSprite;
        [SerializeField] private Color _hiddenTint = new Color(0.35f, 0.35f, 0.4f);

        /// <summary>How many colour ids this palette can draw.</summary>
        public int Count => _entries.Length;

        public Sprite HiddenSprite => _hiddenSprite != null ? _hiddenSprite : _defaultSprite;

        public Color HiddenTint => _hiddenTint;

        public Sprite GetSprite(int colorId)
        {
            Sprite sprite = IsInRange(colorId) ? _entries[colorId].Sprite : null;
            return sprite != null ? sprite : _defaultSprite;
        }

        /// <remarks>Out-of-range ids render magenta so a palette that is too short is obvious on screen.</remarks>
        public Color GetTint(int colorId) => IsInRange(colorId) ? _entries[colorId].Tint : Color.magenta;

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
