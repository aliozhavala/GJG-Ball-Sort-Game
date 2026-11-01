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
        [Tooltip("Seconds into the clip where it has a bang that visuals land on (the fanfare's firework beats). Belongs to the clip: replace them with it.")]
        [SerializeField] private float[] _beats = Array.Empty<float>();

        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public bool RandomPitch => _randomPitch;
        public float StartOffset => _startOffset;
        public float[] Beats => _beats;

        /// <summary>
        /// When each beat is heard, in seconds after the clip is played: the source starts <see cref="StartOffset"/>
        /// in, so every bang comes that much sooner.
        /// </summary>
        public float[] HeardBeats()
        {
            float[] beats = _beats ?? Array.Empty<float>();
            var heard = new float[beats.Length];
            for (int i = 0; i < beats.Length; i++)
            {
                heard[i] = Mathf.Max(0f, beats[i] - _startOffset);
            }

            return heard;
        }
    }
}
