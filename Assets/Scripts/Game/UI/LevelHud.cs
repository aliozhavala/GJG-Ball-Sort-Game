using System;
using BallSort.Core.Player;
using BallSort.Game.Audio;
using BallSort.Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Level screen's UI: settings button, level title and the episode's mascot frame on top, the
    /// booster bar (or, while stuck, the stuck panel) at the bottom, the popups (settings, Win, Are You
    /// Sure, More Lives, Buy, tutorial), and the toast.
    /// </summary>
    /// <remarks>
    /// Presentation only: the level flow decides what to show and reacts to the buttons.
    /// </remarks>
    public sealed class LevelHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelTitle;
        [SerializeField] private Button _settingsButton;

        [Tooltip("Top left: the episode's mascot frame. Display only; hidden when the episode has none.")]
        [SerializeField] private Image _mascotFrame;

        [SerializeField] private BoosterBar _boosterBar;
        [SerializeField] private StuckPanel _stuckPanel;
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private WinPopup _winPopup;
        [SerializeField] private AreYouSurePopup _areYouSurePopup;
        [SerializeField] private MoreLivesPopup _moreLivesPopup;
        [SerializeField] private TutorialPopup _tutorialPopup;
        [SerializeField] private BuyPopup _buyPopup;
        [SerializeField] private Toast _toast;

        [Tooltip("On the canvas root: every button's and toggle's press sound and haptic.")]
        [SerializeField] private UiButtonFeedback _buttonFeedback;

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

        /// <summary>Buys a booster the player has none of.</summary>
        public BuyPopup Buy => _buyPopup;

        /// <summary>For short messages from the level flow, e.g. booster refusals.</summary>
        public Toast Toast => _toast;

        /// <summary>True while a modal popup covers the board.</summary>
        public bool IsPopupOpen =>
            _settingsPopup.Popup.IsOpen || _winPopup.Popup.IsOpen || _areYouSurePopup.Popup.IsOpen ||
            _moreLivesPopup.Popup.IsOpen || _tutorialPopup.Popup.IsOpen || _buyPopup.Popup.IsOpen;

        /// <param name="levelLabel">The level's number, or a level file's name (editor testing).</param>
        /// <param name="mascotFrame">The episode's mascot frame; null hides it.</param>
        /// <param name="audio">Plays the buttons' press sound.</param>
        /// <param name="haptics">Plays the buttons' press haptic.</param>
        public void Initialize(
            string levelLabel, PlayerData player, Sprite mascotFrame, AudioService audio, HapticsService haptics)
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
            _buttonFeedback.Initialize(audio, haptics);
        }

        /// <summary>Closes the settings popup, once the player has confirmed Restart or Home.</summary>
        public void CloseSettings() => _settingsPopup.Popup.Close();


        /// <summary>
        /// Swaps the booster bar for the stuck panel (fading in), or back at once. The level flow shows it
        /// only once the board has settled; until then the bar stays, locked.
        /// </summary>
        public void ShowStuck(bool stuck)
        {
            _stuckPanel.SetVisible(stuck);
            _boosterBar.gameObject.SetActive(!stuck);
        }

        /// <summary>
        /// Locks the settings button (the win sequence, a shuffle pick) and the boosters (also while
        /// stuck). Locked buttons still catch taps, so nothing beneath them is touched; they just do nothing.
        /// </summary>
        public void SetLocked(bool settingsLocked, bool boostersLocked)
        {
            _settingsButton.interactable = !settingsLocked;
            _boosterBar.SetInteractable(!boostersLocked);
        }

        /// <summary>
        /// While the Shuffle booster waits for a tube, the settings button and the boosters let taps through
        /// to the pick, which ends (and unlocks them) on the press. A button that caught that press would
        /// click on its release, once unlocked, so the tap that cancels the pick would also use a booster.
        /// </summary>
        public void SetTapsPassThrough(bool passThrough)
        {
            _settingsButton.targetGraphic.raycastTarget = !passThrough;
            _boosterBar.SetCatchesTaps(!passThrough);
        }

        private void OnRestartRequested() => RestartRequested?.Invoke();

        private void OnHomeRequested() => HomeRequested?.Invoke();

        private void Awake()
        {
            if (_levelTitle == null || _settingsButton == null || _mascotFrame == null || _boosterBar == null ||
                _stuckPanel == null || _settingsPopup == null || _winPopup == null || _areYouSurePopup == null ||
                _moreLivesPopup == null || _tutorialPopup == null || _buyPopup == null || _toast == null ||
                _buttonFeedback == null)
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
