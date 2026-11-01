using System;
using BallSort.Core.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Two-way binding between a uGUI <see cref="Toggle"/> and one saved player setting, drawn by an
    /// optional <see cref="SettingSwitch"/> (it slides on a tap and snaps when the setting is synced).
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public sealed class SettingToggle : MonoBehaviour
    {
        [SerializeField] private SettingType _setting;
        [SerializeField] private Toggle _toggle;

        [Tooltip("Optional: the switch art that shows the toggle's state.")]
        [SerializeField] private SettingSwitch _switch;

        private PlayerData _player;
        private bool _subscribed;

        /// <summary>
        /// The player tapped the toggle, and the setting is already saved: turning Sound on can be heard
        /// confirming itself, turning it off cannot.
        /// </summary>
        public event Action Toggled;

        public void Bind(PlayerData player)
        {
            Unbind();
            _player = player;
            ShowWithoutNotify(player.GetSetting(_setting));
            _toggle.onValueChanged.AddListener(OnToggled);
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void OnToggled(bool isOn)
        {
            if (_switch != null)
            {
                _switch.Slide(isOn);
            }

            _player.SetSetting(_setting, isOn);
            Toggled?.Invoke();
        }

        private void ShowWithoutNotify(bool isOn)
        {
            _toggle.SetIsOnWithoutNotify(isOn);
            if (_switch != null)
            {
                _switch.Snap(isOn);
            }
        }

        private void OnSettingChanged(SettingType setting, bool isOn)
        {
            // The setting already changed by this toggle's own tap is already shown (and sliding).
            if (setting == _setting && _toggle.isOn != isOn)
            {
                ShowWithoutNotify(isOn);
            }
        }

        private void Unbind()
        {
            if (_player == null)
            {
                return;
            }

            _toggle.onValueChanged.RemoveListener(OnToggled);
            Unsubscribe();
            _player = null;
        }

        private void Subscribe()
        {
            if (_player == null || _subscribed)
            {
                return;
            }

            ShowWithoutNotify(_player.GetSetting(_setting));
            _player.SettingChanged += OnSettingChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_player != null && _subscribed)
            {
                _player.SettingChanged -= OnSettingChanged;
            }

            _subscribed = false;
        }

        // PlayerData outlives the scene, so the subscription lasts only while the toggle is shown: a
        // toggle in a popup that is never opened never runs OnDestroy.
        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unbind();

        private void OnValidate()
        {
            if (_toggle == null)
            {
                _toggle = GetComponent<Toggle>();
            }
        }
    }
}
