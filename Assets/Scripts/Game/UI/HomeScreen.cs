using BallSort.Core.Levels;
using BallSort.Core.Player;
using BallSort.Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Home screen: avatar, lives and coins in the top bar, the Level button in the centre, the
    /// bottom tabs and their pages, and the popups (settings, Edit Profile, More Lives, Episode Unlocked).
    /// </summary>
    /// <remarks>
    /// The Level button starts the level only with a life to spend; at 0 lives it opens More Lives.
    /// What to show on arriving (an unlocked episode, a notice) is decided by the Home flow.
    /// </remarks>
    public sealed class HomeScreen : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] private Button _avatarButton;
        [SerializeField] private LivesDisplay _lives;
        [SerializeField] private TMP_Text _coinsLabel;
        [SerializeField] private Button _settingsButton;

        [Header("Centre")]
        [SerializeField] private Button _levelButton;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private LevelTierLook _levelTierLook;

        [Header("Bottom navigation")]
        [SerializeField] private HomeTabs _tabs;

        [Header("Popups and shared pieces")]
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private EditProfilePopup _editProfilePopup;
        [SerializeField] private MoreLivesPopup _moreLivesPopup;
        [SerializeField] private EpisodeUnlockedOverlay _episodeUnlockedOverlay;

        [Tooltip("Transparent full-screen blocker, active while Home must not take taps (before Episode Unlocked shows).")]
        [SerializeField] private GameObject _inputBlocker;

        [SerializeField] private Toast _toast;

        [Header("Text")]
        [SerializeField] private string _coinsFormat = "● {0}";
        [SerializeField] private string _levelFormat = "Level {0}";
        [SerializeField] private string _allCompleteText = "All levels complete";

        private PlayerData _player;
        private SceneLoader _sceneLoader;

        public EpisodeUnlockedOverlay EpisodeUnlocked => _episodeUnlockedOverlay;

        /// <param name="levelTier">Tier of the level the Level button plays; sets the button's look.</param>
        public void Initialize(PlayerData player, SceneLoader sceneLoader, LevelTier levelTier)
        {
            _player = player;
            _sceneLoader = sceneLoader;

            _settingsPopup.Initialize(player, showLevelActions: false);
            _editProfilePopup.Initialize(player);
            _moreLivesPopup.Initialize(player);
            _lives.Bind(player);
            _tabs.Initialize();
            SetInputBlocked(false);

            _player.Wallet.Changed += ShowCoins;
            ShowCoins(_player.Wallet.Coins);

            // An all-complete player replays the last level, which CurrentLevel already points at.
            _levelLabel.text = _player.AllLevelsComplete
                ? _allCompleteText
                : string.Format(_levelFormat, _player.CurrentLevel);
            _levelTierLook.Show(levelTier);

            _levelButton.onClick.AddListener(OnLevelClicked);
            _settingsButton.onClick.AddListener(_settingsPopup.Popup.Open);
            _avatarButton.onClick.AddListener(_editProfilePopup.Open);
        }

        /// <summary>Shows a short message over the Home screen, e.g. a notice from the previous scene.</summary>
        public void ShowToast(string message) => _toast.Show(message);

        /// <summary>Blocks or allows every tap on Home, popups included.</summary>
        public void SetInputBlocked(bool blocked) => _inputBlocker.SetActive(blocked);

        private void OnLevelClicked()
        {
            if (_player.CanStartLevel())
            {
                _sceneLoader.LoadLevel();
            }
            else
            {
                _moreLivesPopup.Open();
            }
        }

        private void ShowCoins(int coins) => _coinsLabel.text = string.Format(_coinsFormat, coins);

        private void Awake()
        {
            if (_avatarButton == null || _lives == null || _coinsLabel == null || _settingsButton == null ||
                _levelButton == null || _levelLabel == null || _levelTierLook == null || _tabs == null ||
                _settingsPopup == null || _editProfilePopup == null || _moreLivesPopup == null ||
                _episodeUnlockedOverlay == null || _inputBlocker == null || _toast == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
            }
        }

        // PlayerData outlives the scene, so the subscriptions must not outlive this object.
        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.Wallet.Changed -= ShowCoins;
            }
        }
    }
}
