using System;
using UnityEngine;

namespace BallSort.Game.Audio
{
    /// <summary>One sound effect slot: its clip (empty = silent), volume and whether its pitch varies a little.</summary>
    [Serializable]
    public sealed class SfxClip
    {
        [SerializeField] private AudioClip _clip;
        [Tooltip("Into the master volume; 1 plays the clip as delivered.")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [Tooltip("Plays at a slightly random pitch each time (the mp3 clips and the deselect, per the SFX guide).")]
        [SerializeField] private bool _randomPitch;
        [Tooltip("Seconds skipped at the start: the silence mp3 encoding adds (0.05 s on the delivered mp3s, which all start at 52 ms or later).")]
        [SerializeField, Min(0f)] private float _startOffset;

        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public bool RandomPitch => _randomPitch;
        public float StartOffset => _startOffset;
    }
}
