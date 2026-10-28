using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Maps lock ids to their colour, used by the lock icon on a door and the key mark on a ball.
    /// Kept apart from <see cref="ColorPalette"/> so a lock colour is never read as a ball colour.
    /// </summary>
    /// <remarks>
    /// Colours only: the lock and key sprites live in <see cref="TubeVisualConfig"/> and
    /// <see cref="ColorPalette"/>, like every other board sprite.
    /// </remarks>
    [CreateAssetMenu(fileName = "LockPalette", menuName = "Ball Sort/Lock Palette")]
    public sealed class LockPalette : ScriptableObject
    {
        [Tooltip("Colour of lock id 0, 1, 2, …. Keep each one unlike every ball colour and every other lock.")]
        [SerializeField] private Color[] _tints = new Color[0];
        [Tooltip("Used for a lock id beyond the list (the board logs an error). Keep it unlike every entry.")]
        [SerializeField] private Color _unknownTint = Color.black;

        /// <summary>How many lock ids this palette can colour.</summary>
        public int Count => _tints.Length;

        public Color GetTint(int lockId) => lockId >= 0 && lockId < _tints.Length ? _tints[lockId] : _unknownTint;

        private void OnValidate()
        {
            if (_tints.Length == 0)
            {
                Debug.LogWarning($"{name}: no lock colours defined.", this);
            }
        }
    }
}
