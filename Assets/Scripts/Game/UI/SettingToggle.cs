using BallSort.Game.Services;
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

        public void Bind(PlayerData player)
        {
            Unbind();
            _player = player;
            _toggle.SetIsOnWithoutNotify(player.GetSetting(_setting));
            _toggle.onValueChanged.AddListener(OnToggled);
            _player.SettingChanged += OnSettingChanged;
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
            _player.SettingChanged -= OnSettingChanged;
            _player = null;
        }

        // PlayerData outlives the scene, so the subscription must not outlive this object.
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
