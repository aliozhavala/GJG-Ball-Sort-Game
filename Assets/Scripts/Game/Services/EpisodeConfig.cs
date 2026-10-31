using System.Collections.Generic;
using BallSort.Core.Levels;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// The run's episodes, in order: each one's first level, its Home and Level backgrounds and its
    /// mascot frame (docs/screens.md, Episodes). The current episode is derived from a level number,
    /// never stored.
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

            return _episodes[GetEpisodeIndex(level)];
        }

        /// <summary>
        /// The 0-based position of the episode <paramref name="level"/> belongs to, e.g. to tell that a
        /// win moved the player into the next episode. 0 when there are no episodes.
        /// </summary>
        public int GetEpisodeIndex(int level)
        {
            if (_episodes.Length == 0)
            {
                return 0;
            }

            _firstLevels ??= ReadFirstLevels();
            return EpisodeMap.Find(_firstLevels, level);
        }

        /// <summary>
        /// The episode winning <paramref name="wonLevel"/> unlocked, now that the player's current level
        /// is <paramref name="nextLevel"/> (see <see cref="EpisodeMap.FindUnlocked"/>); -1 for none.
        /// </summary>
        public int GetUnlockedEpisode(int wonLevel, int nextLevel)
        {
            if (_episodes.Length == 0)
            {
                return -1;
            }

            _firstLevels ??= ReadFirstLevels();
            return EpisodeMap.FindUnlocked(_firstLevels, wonLevel, nextLevel);
        }

        /// <summary>The episode at <paramref name="index"/> (0-based), as <see cref="GetEpisodeIndex"/> returns it.</summary>
        public Episode GetEpisode(int index) => _episodes[index];

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
                if (_episodes[i] == null || _episodes[i].HomeBackground == null || _episodes[i].LevelBackground == null ||
                    _episodes[i].MascotFrame == null)
                {
                    missing.Add(i + 1);
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning($"{name}: episode(s) {string.Join(", ", missing)} miss a background or mascot sprite.", this);
            }
        }
    }
}
