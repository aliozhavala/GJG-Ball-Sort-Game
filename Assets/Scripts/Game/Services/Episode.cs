using System;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// One episode of the run: where it starts, its name, its two backgrounds, its mascot frame and the picture that
    /// announces it. Listed in an
    /// <see cref="EpisodeConfig"/>.
    /// </summary>
    [Serializable]
    public sealed class Episode
    {
        [Tooltip("The episode's first level, 1-based. It runs until the next episode's first level.")]
        [SerializeField, Min(1)] private int _firstLevel = 1;

        [Tooltip("The episode's name, under the title on the Episode Unlocked overlay (\"Beach Lab\").")]
        [SerializeField] private string _name = string.Empty;
        [SerializeField] private Sprite _homeBackground;
        [SerializeField] private Sprite _levelBackground;

        [Tooltip("The scientist in a frame, top left of the Level scene.")]
        [SerializeField] private Sprite _mascotFrame;

        [Tooltip("The scene shown on the Episode Unlocked overlay when the run reaches this episode; empty = the mascot frame.")]
        [SerializeField] private Sprite _unlockedPicture;

        public int FirstLevel => _firstLevel;

        /// <summary>The episode's name; empty for none.</summary>
        public string Name => _name;

        /// <summary>Full-screen background of the Home screen while this episode is current.</summary>
        public Sprite HomeBackground => _homeBackground;

        /// <summary>Full-screen background of the Level scene for this episode's levels.</summary>
        public Sprite LevelBackground => _levelBackground;

        /// <summary>The episode's mascot (the scientist in a frame).</summary>
        public Sprite MascotFrame => _mascotFrame;

        /// <summary>The Episode Unlocked overlay's picture: its own scene, or the mascot frame until it has one.</summary>
        public Sprite UnlockedPicture => _unlockedPicture != null ? _unlockedPicture : _mascotFrame;
    }
}
