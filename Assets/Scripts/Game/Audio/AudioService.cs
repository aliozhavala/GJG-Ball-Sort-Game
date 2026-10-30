using BallSort.Core.Player;
using UnityEngine;

namespace BallSort.Game.Audio
{
    /// <summary>
    /// Plays sound effects from a small pool of 2D sources, fire-and-forget: a sound never blocks
    /// anything, and several overlap freely (a run's landings, the chime and the cork).
    /// </summary>
    /// <remarks>
    /// Lives on the persistent <c>AppRoot</c>, so sounds survive scene changes; scenes get it through
    /// their bootstrap. The player's Sound setting mutes everything, including sounds already playing.
    /// Pitch goes through <see cref="AudioSource.pitch"/>, so a higher pitch also plays shorter, as in
    /// the prototype. A slot's start offset skips the silence mp3 encoding adds, so a sound lands on
    /// its frame. When every source is busy, the oldest sound is cut.
    /// </remarks>
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private SfxConfig _config;

        private AudioSource[] _sources = new AudioSource[0];
        private int _next;
        private PlayerData _player;
        private bool _soundOn = true;

        /// <summary>The slots and pitch rules, for callers that compute a pitch (landing, chain).</summary>
        public SfxConfig Config => _config;

        /// <summary>Follows <paramref name="player"/>'s Sound setting from now on.</summary>
        public void Initialize(PlayerData player)
        {
            Unbind();
            _player = player;
            _soundOn = player.GetSetting(SettingType.Sound);
            _player.SettingChanged += OnSettingChanged;
        }

        /// <summary>
        /// Plays <paramref name="sfx"/> at <paramref name="pitch"/> (times its random pitch, if it uses one).
        /// Silent when Sound is off or the slot is empty.
        /// </summary>
        public void Play(Sfx sfx, float pitch = 1f)
        {
            SfxClip slot = _config.Get(sfx);
            if (!_soundOn || slot == null || slot.Clip == null || _sources.Length == 0)
            {
                return;
            }

            if (slot.RandomPitch)
            {
                float range = _config.RandomPitchRange;
                pitch *= Random.Range(1f - range, 1f + range);
            }

            AudioSource source = NextSource();
            source.clip = slot.Clip;
            source.volume = slot.Volume * _config.SfxVolume;
            source.pitch = pitch;
            source.Play();
            if (slot.StartOffset > 0f)
            {
                source.time = Mathf.Min(slot.StartOffset, slot.Clip.length * 0.5f);
            }
        }

        /// <summary>A free source, or the one that started longest ago when all are busy.</summary>
        private AudioSource NextSource()
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                int index = (_next + i) % _sources.Length;
                if (!_sources[index].isPlaying)
                {
                    _next = (index + 1) % _sources.Length;
                    return _sources[index];
                }
            }

            AudioSource oldest = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            return oldest;
        }

        private void OnSettingChanged(SettingType setting, bool isOn)
        {
            if (setting != SettingType.Sound)
            {
                return;
            }

            _soundOn = isOn;
            if (!isOn)
            {
                foreach (AudioSource source in _sources)
                {
                    source.Stop();
                }
            }
        }

        private void Unbind()
        {
            if (_player != null)
            {
                _player.SettingChanged -= OnSettingChanged;
                _player = null;
            }
        }

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"{name}: SFX config must be assigned.", this);
                enabled = false;
                return;
            }

            _sources = new AudioSource[_config.Voices];
            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }
        }

        // The player data outlives this object only if the app is shutting down; unbind anyway.
        private void OnDestroy() => Unbind();
    }
}
