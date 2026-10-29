using System;
using BallSort.Core.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Level screen's UI: settings button and level title on top, the booster bar (or, while
    /// stuck, the stuck panel) at the bottom, the settings and Win popups, and the toast.
    /// </summary>
    /// <remarks>
    /// Presentation only: the level flow decides what to show and reacts to the buttons.
    /// </remarks>
    public sealed class LevelHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelTitle;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private BoosterBar _boosterBar;
        [SerializeField] private StuckPanel _stuckPanel;
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private WinPopup _winPopup;
        [SerializeField] private Toast _toast;
        [Tooltip("\"Shuffling…\" label above the board, shown while a slow shuffle search runs. Not a raycast target.")]
        [SerializeField] private TMP_Text _shufflingIndicator;
        [SerializeField] private string _levelFormat = "Level {0}";

        /// <summary>The player chose Restart in the settings popup.</summary>
        public event Action RestartRequested;

        /// <summary>The player chose Home in the settings popup.</summary>
        public event Action HomeRequested;

        public BoosterBar Boosters => _boosterBar;

        public StuckPanel Stuck => _stuckPanel;

        public WinPopup Win => _winPopup;

        /// <summary>For short messages from the level flow, e.g. booster refusals.</summary>
        public Toast Toast => _toast;

        /// <summary>True while a modal popup covers the board.</summary>
        public bool IsPopupOpen => _settingsPopup.Popup.IsOpen || _winPopup.Popup.IsOpen;

        /// <param name="levelLabel">The level's number, or a level file's name (editor testing).</param>
        public void Initialize(string levelLabel, PlayerData player)
        {
            _levelTitle.text = string.Format(_levelFormat, levelLabel);
            _settingsPopup.Initialize(player, showLevelActions: true);
            _settingsPopup.RestartRequested += OnRestartRequested;
            _settingsPopup.HomeRequested += OnHomeRequested;
            _settingsButton.onClick.AddListener(_settingsPopup.Popup.Open);
            ShowStuck(false);
            ShowShuffling(false);
        }

        /// <summary>Shows or hides the "Shuffling…" label.</summary>
        public void ShowShuffling(bool visible) => _shufflingIndicator.gameObject.SetActive(visible);

        /// <summary>Swaps the booster bar for the stuck panel, or back.</summary>
        public void ShowStuck(bool stuck)
        {
            _stuckPanel.SetVisible(stuck);
            _boosterBar.gameObject.SetActive(!stuck);
        }

        /// <summary>Locks the settings button and the boosters, e.g. for the win sequence.</summary>
        public void SetLocked(bool locked)
        {
            _settingsButton.interactable = !locked;
            _boosterBar.SetInteractable(!locked);
        }

        private void OnRestartRequested() => RestartRequested?.Invoke();

        private void OnHomeRequested() => HomeRequested?.Invoke();

        private void Awake()
        {
            if (_levelTitle == null || _settingsButton == null || _boosterBar == null || _stuckPanel == null ||
                _settingsPopup == null || _winPopup == null || _toast == null || _shufflingIndicator == null)
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

            if (_settingsButton != null && _settingsPopup != null)
            {
                _settingsButton.onClick.RemoveListener(_settingsPopup.Popup.Open);
            }
        }
    }
}
