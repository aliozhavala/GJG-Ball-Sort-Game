using BallSort.Core.Player;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// The game's haptics, one method per kind the SFX guide uses (docs/design/SFX_GUIDE.md §2), called
    /// wherever the guide pairs a haptic with a sound. Silent while the player's Haptics setting is off.
    /// </summary>
    /// <remarks>
    /// Placeholder: every call reaches <see cref="Fire"/>, but no device vibrates yet. The platform
    /// call (iOS feedback generators, Android <c>VibrationEffect</c>) goes there alone, so the callers
    /// need no change. Lives on the persistent <c>AppRoot</c>; scenes get it through their bootstrap.
    /// </remarks>
    public sealed class HapticsService : MonoBehaviour
    {
        [Tooltip("At least this many real seconds between two landing haptics (a run's balls land 80 ms apart).")]
        [SerializeField, Min(0f)] private float _minLandingInterval = 0.05f;

        [Tooltip("Editor only: log every haptic that would play, to check the calls until vibration exists.")]
        [SerializeField] private bool _logInEditor;

        private PlayerData _player;
        private bool _hapticsOn = true;
        private float _lastLanding = float.NegativeInfinity;

        /// <summary>Follows <paramref name="player"/>'s Haptics setting from now on.</summary>
        public void Initialize(PlayerData player)
        {
            Unbind();
            _player = player;
            _hapticsOn = player.GetSetting(SettingType.Haptics);
            _player.SettingChanged += OnSettingChanged;
        }

        /// <summary>A ball is lifted.</summary>
        public void Selection() => Fire(HapticKind.Selection);

        /// <summary>A button press or a booster used.</summary>
        public void Light() => Fire(HapticKind.Light);

        /// <summary>An impact, e.g. a key entering its padlock.</summary>
        public void Medium() => Fire(HapticKind.Medium);

        /// <summary>A ball lands: <see cref="Medium"/>, skipped if the last landing's was too recent.</summary>
        public void Landing()
        {
            float now = Time.unscaledTime;
            if (now - _lastLanding < _minLandingInterval)
            {
                return;
            }

            _lastLanding = now;
            Fire(HapticKind.Medium);
        }

        /// <summary>Something refused (an invalid move, stuck) or lost (a life).</summary>
        public void Warning() => Fire(HapticKind.Warning);

        /// <summary>A reward: a cork seats, a purchase goes through.</summary>
        public void Success() => Fire(HapticKind.Success);

        /// <summary>A big break: ice, towel, lock box.</summary>
        public void Heavy() => Fire(HapticKind.Heavy);

        /// <summary>The level is won.</summary>
        public void Win() => Fire(HapticKind.Win);

        private void Fire(HapticKind kind)
        {
            if (!_hapticsOn)
            {
                return;
            }

#if UNITY_EDITOR
            if (_logInEditor)
            {
                Debug.Log($"Haptic: {kind}", this);
            }
#endif
            // The device vibration goes here (not implemented yet).
        }

        private void OnSettingChanged(SettingType setting, bool isOn)
        {
            if (setting == SettingType.Haptics)
            {
                _hapticsOn = isOn;
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

        // The player data outlives this object only if the app is shutting down; unbind anyway.
        private void OnDestroy() => Unbind();
    }
}
