using System;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// The ordered list of level files. Index 0 is level 1.
    /// </summary>
    /// <remarks>
    /// Only the run's levels belong here. Test and draft levels live in <c>TestLevelsFolder</c> and
    /// are played from the Editor as level files; the database must never list them.
    /// </remarks>
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "Ball Sort/Level Database")]
    public sealed class LevelDatabase : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>Folder of test and draft level files, outside the run (Ball Sort ▸ Play Level… plays them).</summary>
        public const string TestLevelsFolder = "Assets/Data/TestLevels";
#endif

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
#if UNITY_EDITOR
                else if (UnityEditor.AssetDatabase.GetAssetPath(_levels[i]).StartsWith(TestLevelsFolder + "/", StringComparison.Ordinal))
                {
                    Debug.LogError($"{name}: level slot {i + 1} holds the test level {_levels[i].name}; test levels must stay out of the run.", this);
                }
#endif
            }
        }
    }
}
