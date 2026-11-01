using System;
using BallSort.Core.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The settings popup shared by both scenes: Sound, Music and Haptics toggles, plus Restart
    /// and Home buttons that only the Level scene shows.
    /// </summary>
    [RequireComponent(typeof(Popup))]
    public sealed class SettingsPopup : MonoBehaviour
    {
        [SerializeField] private Popup _popup;
        [SerializeField] private SettingToggle[] _toggles = new SettingToggle[0];
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton;
        [Tooltip("\"Terms and Privacy\" on Home, where the Level scene has Restart and Home: a button (it plays the " +
            "button sound) that opens nothing yet.")]
        [SerializeField] private Button _termsButton;

        /// <summary>
        /// The player tapped Restart. The popup stays open beneath the confirmation (Are You Sure or
        /// More Lives), so closing that comes back here; the level flow closes it once confirmed.
        /// </summary>
        public event Action RestartRequested;

        /// <summary>The player tapped Home. The popup stays open, as for <see cref="RestartRequested"/>.</summary>
        public event Action HomeRequested;

        public Popup Popup => _popup;

        /// <param name="player">Whose settings the toggles show and change.</param>
        /// <param name="showLevelActions">True in the Level scene, to show Restart and Home; false shows Terms and Privacy.</param>
        public void Initialize(PlayerData player, bool showLevelActions)
        {
            foreach (SettingToggle toggle in _toggles)
            {
                toggle.Bind(player);
            }

            _restartButton.gameObject.SetActive(showLevelActions);
            _homeButton.gameObject.SetActive(showLevelActions);
            _termsButton.gameObject.SetActive(!showLevelActions);

            _restartButton.onClick.RemoveListener(OnRestartClicked);
            _homeButton.onClick.RemoveListener(OnHomeClicked);
            _restartButton.onClick.AddListener(OnRestartClicked);
            _homeButton.onClick.AddListener(OnHomeClicked);
        }

        private void OnRestartClicked() => RestartRequested?.Invoke();

        private void OnHomeClicked() => HomeRequested?.Invoke();

        private void OnDestroy()
        {
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(OnHomeClicked);
            }
        }

        private void OnValidate()
        {
            if (_popup == null)
            {
                _popup = GetComponent<Popup>();
            }
        }
    }
}
