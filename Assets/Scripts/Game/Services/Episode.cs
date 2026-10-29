using System;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>One episode of the run: where it starts and its two backgrounds. Listed in an <see cref="EpisodeConfig"/>.</summary>
    [Serializable]
    public sealed class Episode
    {
        [Tooltip("The episode's first level, 1-based. It runs until the next episode's first level.")]
        [SerializeField, Min(1)] private int _firstLevel = 1;
        [SerializeField] private Sprite _homeBackground;
        [SerializeField] private Sprite _levelBackground;

        public int FirstLevel => _firstLevel;

        /// <summary>Full-screen background of the Home screen while this episode is current.</summary>
        public Sprite HomeBackground => _homeBackground;

        /// <summary>Full-screen background of the Level scene for this episode's levels.</summary>
        public Sprite LevelBackground => _levelBackground;
    }
}
