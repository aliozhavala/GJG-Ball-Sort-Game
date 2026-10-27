using System;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// The ordered list of level files. Index 0 is level 1.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "Ball Sort/Level Database")]
    public sealed class LevelDatabase : ScriptableObject
    {
        [SerializeField] private TextAsset[] _levels = new TextAsset[0];

        public int Count => _levels.Length;

        /// <summary>The level file for a 1-based level number.</summary>
        public TextAsset GetLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > _levels.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levelNumber), levelNumber, $"Database holds levels 1 to {_levels.Length}.");
            }

            return _levels[levelNumber - 1];
        }

        private void OnValidate()
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                if (_levels[i] == null)
                {
                    Debug.LogWarning($"{name}: level slot {i + 1} is empty.", this);
                }
            }
        }
    }
}
