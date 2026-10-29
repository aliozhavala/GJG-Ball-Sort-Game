using System;
using BallSort.Core.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Level screen's UI: settings button, level title and the episode's mascot frame on top, the
    /// booster bar (or, while stuck, the stuck panel) at the bottom, the popups (settings, Win, Are You
    /// Sure, More Lives, tutorial), and the toast.
    /// </summary>
    /// <remarks>
    /// Presentation only: the level flow decides what to show and reacts to the buttons.
    /// </remarks>
    public sealed class LevelHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelTitle;
        [SerializeField] private Button _settingsButton;

        [Tooltip("Top right: the episode's mascot frame. Display only; hidden when the episode has none.")]
        [SerializeField] private Image _mascotFrame;

        [SerializeField] private BoosterBar _boosterBar;
        [SerializeField] private StuckPanel _stuckPanel;
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private WinPopup _winPopup;
        [SerializeField] private AreYouSurePopup _areYouSurePopup;
        [SerializeField] private MoreLivesPopup _moreLivesPopup;
        [SerializeField] private TutorialPopup _tutorialPopup;
        [SerializeField] private Toast _toast;
        [Tooltip("\"Shuffling…\" label above the board, shown while a slow shuffle search runs. Not a raycast target.")]
        [SerializeField] private TMP_Text _shufflingIndicator;
        [SerializeField] private string _levelFormat = "Level {0}";

        /// <summary>The player chose Restart in the settings popup, which stays open.</summary>
        public event Action RestartRequested;

        /// <summary>The player chose Home in the settings popup, which stays open.</summary>
        public event Action HomeRequested;

        public BoosterBar Boosters => _boosterBar;

        public StuckPanel Stuck => _stuckPanel;

        public WinPopup Win => _winPopup;

        public AreYouSurePopup AreYouSure => _areYouSurePopup;

        public MoreLivesPopup MoreLives => _moreLivesPopup;

        public TutorialPopup Tutorial => _tutorialPopup;

        /// <summary>For short messages from the level flow, e.g. booster refusals.</summary>
        public Toast Toast => _toast;

        /// <summary>True while a modal popup covers the board.</summary>
        public bool IsPopupOpen =>
            _settingsPopup.Popup.IsOpen || _winPopup.Popup.IsOpen || _areYouSurePopup.Popup.IsOpen ||
            _moreLivesPopup.Popup.IsOpen || _tutorialPopup.Popup.IsOpen;

        /// <param name="levelLabel">The level's number, or a level file's name (editor testing).</param>
        /// <param name="mascotFrame">The episode's mascot frame; null hides it.</param>
        public void Initialize(string levelLabel, PlayerData player, Sprite mascotFrame)
        {
            _levelTitle.text = string.Format(_levelFormat, levelLabel);
            _mascotFrame.sprite = mascotFrame;
            _mascotFrame.gameObject.SetActive(mascotFrame != null);
            _settingsPopup.Initialize(player, showLevelActions: true);
            _settingsPopup.RestartRequested += OnRestartRequested;
            _settingsPopup.HomeRequested += OnHomeRequested;
            _settingsButton.onClick.AddListener(_settingsPopup.Popup.Open);
            _moreLivesPopup.Initialize(player);
            ShowStuck(false);
            ShowShuffling(false);
        }

        /// <summary>Closes the settings popup, once the player has confirmed Restart or Home.</summary>
        public void CloseSettings() => _settingsPopup.Popup.Close();

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
            if (_levelTitle == null || _settingsButton == null || _mascotFrame == null || _boosterBar == null ||
                _stuckPanel == null || _settingsPopup == null || _winPopup == null || _areYouSurePopup == null ||
                _moreLivesPopup == null || _tutorialPopup == null || _toast == null || _shufflingIndicator == null)
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
