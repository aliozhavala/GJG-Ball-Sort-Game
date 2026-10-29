using BallSort.Core.Levels;
using BallSort.Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Home screen: lives and coins in the top bar, the Level button in the centre, and the
    /// bottom navigation. Placeholder features answer with a "Coming soon" toast.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] private Button _avatarButton;
        [SerializeField] private TMP_Text _livesLabel;
        [SerializeField] private TMP_Text _coinsLabel;
        [SerializeField] private Button _settingsButton;

        [Header("Centre")]
        [SerializeField] private Button _levelButton;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private LevelTierLook _levelTierLook;

        [Header("Bottom navigation")]
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _shopButton;

        [Header("Shared pieces")]
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private Toast _toast;

        [Header("Text")]
        [SerializeField] private string _livesFormat = "♥ {0}";
        [SerializeField] private string _coinsFormat = "● {0}";
        [SerializeField] private string _levelFormat = "Level {0}";
        [SerializeField] private string _allCompleteText = "All levels complete";
        [SerializeField] private string _comingSoonText = "Coming soon";

        private PlayerData _player;
        private SceneLoader _sceneLoader;

        /// <param name="levelTier">Tier of the level the Level button plays; sets the button's look.</param>
        public void Initialize(PlayerData player, SceneLoader sceneLoader, LevelTier levelTier)
        {
            _player = player;
            _sceneLoader = sceneLoader;

            _settingsPopup.Initialize(player, showLevelActions: false);

            _player.Wallet.Changed += ShowCoins;
            _player.LivesChanged += ShowLives;
            ShowCoins(_player.Wallet.Coins);
            ShowLives(_player.Lives);

            // An all-complete player replays the last level, which CurrentLevel already points at.
            _levelLabel.text = _player.AllLevelsComplete
                ? _allCompleteText
                : string.Format(_levelFormat, _player.CurrentLevel);
            _levelTierLook.Show(levelTier);

            _levelButton.onClick.AddListener(_sceneLoader.LoadLevel);
            _settingsButton.onClick.AddListener(_settingsPopup.Popup.Open);
            _avatarButton.onClick.AddListener(ShowComingSoon);
            _leaderboardButton.onClick.AddListener(ShowComingSoon);
            _shopButton.onClick.AddListener(ShowComingSoon);
        }

        private void ShowCoins(int coins) => _coinsLabel.text = string.Format(_coinsFormat, coins);

        private void ShowLives(int lives) => _livesLabel.text = string.Format(_livesFormat, lives);

        /// <summary>Shows a short message over the Home screen, e.g. a notice from the previous scene.</summary>
        public void ShowToast(string message) => _toast.Show(message);

        private void ShowComingSoon() => _toast.Show(_comingSoonText);

        private void Awake()
        {
            if (_avatarButton == null || _livesLabel == null || _coinsLabel == null || _settingsButton == null ||
                _levelButton == null || _levelLabel == null || _levelTierLook == null || _leaderboardButton == null ||
                _shopButton == null || _settingsPopup == null || _toast == null)
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
                _player.LivesChanged -= ShowLives;
            }
        }
    }
}
