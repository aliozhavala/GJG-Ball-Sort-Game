using System;
using BallSort.Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Level screen's HUD: settings button and level title. The booster bar and the stuck and
    /// win panels join it in roadmap step 3b.
    /// </summary>
    public sealed class LevelHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelTitle;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private Toast _toast;
        [SerializeField] private string _levelFormat = "Level {0}";

        /// <summary>The player chose Restart in the settings popup.</summary>
        public event Action RestartRequested;

        /// <summary>The player chose Home in the settings popup.</summary>
        public event Action HomeRequested;

        /// <summary>For short messages from the level flow, e.g. booster refusals.</summary>
        public Toast Toast => _toast;

        public void Initialize(int levelNumber, PlayerData player)
        {
            _levelTitle.text = string.Format(_levelFormat, levelNumber);
            _settingsPopup.Initialize(player, showLevelActions: true);
            _settingsPopup.RestartRequested += OnRestartRequested;
            _settingsPopup.HomeRequested += OnHomeRequested;
            _settingsButton.onClick.AddListener(_settingsPopup.Popup.Open);
        }

        private void OnRestartRequested() => RestartRequested?.Invoke();

        private void OnHomeRequested() => HomeRequested?.Invoke();

        private void Awake()
        {
            if (_levelTitle == null || _settingsButton == null || _settingsPopup == null || _toast == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
            }
        }

        private void OnDestroy()
        {
            if (_settingsPopup != null)
            {
                _settingsPopup.RestartRequested -= OnRestartRequested;
                _settingsPopup.HomeRequested -= OnHomeRequested;
            }
        }
    }
}
