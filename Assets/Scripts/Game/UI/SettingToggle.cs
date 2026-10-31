using BallSort.Core.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>Two-way binding between a uGUI <see cref="Toggle"/> and one saved player setting.</summary>
    [RequireComponent(typeof(Toggle))]
    public sealed class SettingToggle : MonoBehaviour
    {
        [SerializeField] private SettingType _setting;
        [SerializeField] private Toggle _toggle;

        private PlayerData _player;
        private bool _subscribed;

        public void Bind(PlayerData player)
        {
            Unbind();
            _player = player;
            _toggle.SetIsOnWithoutNotify(player.GetSetting(_setting));
            _toggle.onValueChanged.AddListener(OnToggled);
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void OnToggled(bool isOn) => _player.SetSetting(_setting, isOn);

        private void OnSettingChanged(SettingType setting, bool isOn)
        {
            if (setting == _setting)
            {
                _toggle.SetIsOnWithoutNotify(isOn);
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

            _toggle.SetIsOnWithoutNotify(_player.GetSetting(_setting));
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
