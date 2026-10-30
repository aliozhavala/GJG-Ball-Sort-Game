using UnityEngine;

namespace BallSort.Game.Audio
{
    /// <summary>
    /// The sound effects, one named slot each (docs/design/SFX_GUIDE.md), and the rules shared by all
    /// of them: master volume, random pitch, the landing's rising scale and the completion chime's chain.
    /// An empty slot plays nothing, so a missing sound needs no code change.
    /// </summary>
    [CreateAssetMenu(fileName = "SfxConfig", menuName = "Ball Sort/Sfx Config")]
    public sealed class SfxConfig : ScriptableObject
    {
        [Tooltip("Every clip plays into this master volume.")]
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.7f;
        [Tooltip("Random pitch range of the clips that use it: 0.03 = ±3 %.")]
        [SerializeField, Range(0f, 0.2f)] private float _randomPitchRange = 0.03f;
        [Tooltip("Sources played at once; when all are busy the oldest sound is cut.")]
        [SerializeField, Range(1, 32)] private int _voices = 10;

        [Header("Pitch rules")]
        [Tooltip("Landing pitch in semitones by the slot the ball lands in (slot 0 first; higher slots use the last).")]
        [SerializeField] private float[] _landSemitones = { 0f, 1f, 2f, 3.5f, 4.5f, 6f, 7f };
        [Tooltip("Completion chime: semitones added per tube completed before this one in the level.")]
        [SerializeField, Min(0f)] private float _chainSemitones = 1f;
        [Tooltip("Upper limit of the chime's rise, in semitones.")]
        [SerializeField, Min(0f)] private float _chainSemitoneCap = 12f;

        [Header("Gameplay")]
        [SerializeField] private SfxClip _ballSelect = new SfxClip();
        [SerializeField] private SfxClip _ballDeselect = new SfxClip();
        [SerializeField] private SfxClip _ballLand = new SfxClip();
        [SerializeField] private SfxClip _moveInvalid = new SfxClip();
        [SerializeField] private SfxClip _tubeCompleteChime = new SfxClip();
        [SerializeField] private SfxClip _tubeCork = new SfxClip();

        [Header("Mechanics")]
        [SerializeField] private SfxClip _hiddenReveal = new SfxClip();
        [SerializeField] private SfxClip _towelPull = new SfxClip();
        [SerializeField] private SfxClip _lockboxKeyFly = new SfxClip();
        [SerializeField] private SfxClip _lockboxKeyIn = new SfxClip();
        [SerializeField] private SfxClip _lockboxBreak = new SfxClip();
        [SerializeField] private SfxClip _iceBreak = new SfxClip();

        [Header("Boosters")]
        [SerializeField] private SfxClip _boosterUndo = new SfxClip();
        [SerializeField] private SfxClip _boosterShuffle = new SfxClip();
        [SerializeField] private SfxClip _boosterAddTube = new SfxClip();

        [Header("Win and rewards")]
        [SerializeField] private SfxClip _winFanfare = new SfxClip();
        [SerializeField] private SfxClip _rewardJingle = new SfxClip();
        [SerializeField] private SfxClip _winTitleStamp = new SfxClip();
        [SerializeField] private SfxClip _coinGain = new SfxClip();

        [Header("UI")]
        [SerializeField] private SfxClip _uiButton = new SfxClip();
        [SerializeField] private SfxClip _uiPlay = new SfxClip();
        [SerializeField] private SfxClip _lifeLost = new SfxClip();

        public float SfxVolume => _sfxVolume;
        public float RandomPitchRange => _randomPitchRange;
        public int Voices => _voices;

        /// <summary>The slot of <paramref name="sfx"/>.</summary>
        public SfxClip Get(Sfx sfx) => sfx switch
        {
            Sfx.BallSelect => _ballSelect,
            Sfx.BallDeselect => _ballDeselect,
            Sfx.BallLand => _ballLand,
            Sfx.MoveInvalid => _moveInvalid,
            Sfx.TubeCompleteChime => _tubeCompleteChime,
            Sfx.TubeCork => _tubeCork,
            Sfx.HiddenReveal => _hiddenReveal,
            Sfx.TowelPull => _towelPull,
            Sfx.LockboxKeyFly => _lockboxKeyFly,
            Sfx.LockboxKeyIn => _lockboxKeyIn,
            Sfx.LockboxBreak => _lockboxBreak,
            Sfx.IceBreak => _iceBreak,
            Sfx.BoosterUndo => _boosterUndo,
            Sfx.BoosterShuffle => _boosterShuffle,
            Sfx.BoosterAddTube => _boosterAddTube,
            Sfx.WinFanfare => _winFanfare,
            Sfx.RewardJingle => _rewardJingle,
            Sfx.WinTitleStamp => _winTitleStamp,
            Sfx.CoinGain => _coinGain,
            Sfx.UiButton => _uiButton,
            Sfx.UiPlay => _uiPlay,
            Sfx.LifeLost => _lifeLost,
            _ => null,
        };

        /// <summary>Pitch of a ball landing in <paramref name="slot"/> (0 = bottom): a tube filling up plays a rising scale.</summary>
        public float LandPitch(int slot)
        {
            if (_landSemitones == null || _landSemitones.Length == 0)
            {
                return 1f;
            }

            return SemitonesToPitch(_landSemitones[Mathf.Clamp(slot, 0, _landSemitones.Length - 1)]);
        }

        /// <summary>Pitch of the completion chime after <paramref name="chain"/> earlier completions in the level.</summary>
        public float ChainPitch(int chain) => SemitonesToPitch(Mathf.Min(chain * _chainSemitones, _chainSemitoneCap));

        private static float SemitonesToPitch(float semitones) => Mathf.Pow(2f, semitones / 12f);
    }
}
