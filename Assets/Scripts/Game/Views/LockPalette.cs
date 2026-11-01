using System;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Maps lock ids to their art: the lock box's coloured mid section (lamp and key slot) and the key
    /// a ball carries. Kept apart from <see cref="ColorPalette"/> so a lock colour is never read as a
    /// ball colour.
    /// </summary>
    /// <remarks>
    /// The rest of the lock box (sides, face, the open box's parts) is the same for every lock and lives
    /// in <see cref="TubeVisualConfig"/>.
    /// </remarks>
    [CreateAssetMenu(fileName = "LockPalette", menuName = "Ball Sort/Lock Palette")]
    public sealed class LockPalette : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [Tooltip("Only for the Inspector and logs, e.g. \"Green\".")]
            [SerializeField] private string _name;
            [Tooltip("The lock box's mid section in this lock's colour, with the key slot.")]
            [SerializeField] private Sprite _mid;
            [Tooltip("The key in this lock's colour, drawn on the ball that carries it.")]
            [SerializeField] private Sprite _key;

            public Sprite Mid => _mid;
            public Sprite Key => _key;
        }

        [Tooltip("Lock id 0, 1, 2, …. Keep each one unlike every other lock.")]
        [SerializeField] private Entry[] _locks = new Entry[0];

        /// <summary>How many lock ids this palette can draw.</summary>
        public int Count => _locks.Length;

        /// <summary>The lock box's mid section for <paramref name="lockId"/>; null for an id beyond the list (the board logs it).</summary>
        public Sprite GetMid(int lockId) => IsInRange(lockId) ? _locks[lockId].Mid : null;

        /// <summary>The key for <paramref name="lockId"/>; null for an id beyond the list or no key.</summary>
        public Sprite GetKey(int lockId) => IsInRange(lockId) ? _locks[lockId].Key : null;

        private bool IsInRange(int lockId) => lockId >= 0 && lockId < _locks.Length;

        private void OnValidate()
        {
            if (_locks.Length == 0)
            {
                Debug.LogWarning($"{name}: no locks defined.", this);
            }

            for (int i = 0; i < _locks.Length; i++)
            {
                if (_locks[i].Mid == null || _locks[i].Key == null)
                {
                    Debug.LogWarning($"{name}: lock {i} needs both a mid and a key sprite.", this);
                }
            }
        }
    }
}
