using System.Collections.Generic;
using BallSort.Core.Levels;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// The run's episodes, in order: each one's first level and its Home and Level backgrounds
    /// (docs/screens.md, Episodes). The current episode is derived from a level number, never stored.
    /// </summary>
    [CreateAssetMenu(fileName = "EpisodeConfig", menuName = "Ball Sort/Episode Config")]
    public sealed class EpisodeConfig : ScriptableObject
    {
        [Tooltip("Episodes in order. The first starts at level 1; each runs until the next one's first level.")]
        [SerializeField] private Episode[] _episodes = new Episode[0];

        private int[] _firstLevels;

        public int Count => _episodes.Length;

        /// <summary>
        /// The episode <paramref name="level"/> belongs to (see <see cref="EpisodeMap"/>). A level file
        /// outside the run (level 0) gets the first episode.
        /// </summary>
        public Episode GetEpisodeForLevel(int level)
        {
            if (_episodes.Length == 0)
            {
                Debug.LogError($"{name} has no episodes.", this);
                return new Episode();
            }

            _firstLevels ??= ReadFirstLevels();
            return _episodes[EpisodeMap.Find(_firstLevels, level)];
        }

        private int[] ReadFirstLevels()
        {
            var firstLevels = new int[_episodes.Length];
            for (int i = 0; i < firstLevels.Length; i++)
            {
                firstLevels[i] = _episodes[i]?.FirstLevel ?? 0;
            }

            return firstLevels;
        }

        private void OnValidate()
        {
            _firstLevels = null;

            string problem = EpisodeMap.Validate(ReadFirstLevels());
            if (problem != null)
            {
                Debug.LogError($"{name}: {problem}", this);
            }

            var missing = new List<int>();
            for (int i = 0; i < _episodes.Length; i++)
            {
                if (_episodes[i] == null || _episodes[i].HomeBackground == null || _episodes[i].LevelBackground == null)
                {
                    missing.Add(i + 1);
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning($"{name}: episode(s) {string.Join(", ", missing)} miss a background sprite.", this);
            }
        }
    }
}
