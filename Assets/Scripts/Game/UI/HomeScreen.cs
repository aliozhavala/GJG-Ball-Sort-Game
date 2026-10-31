using System.Globalization;
using BallSort.Core.Levels;
using BallSort.Core.Player;
using BallSort.Game.Audio;
using BallSort.Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Home screen: avatar, lives and coins in the top bar, the Level button in the centre, the
    /// bottom tabs and their pages, and the popups (settings, avatar menu, More Lives, Buy, Episode Unlocked).
    /// </summary>
    /// <remarks>
    /// The Level button starts the level only with a life to spend; at 0 lives it opens More Lives.
    /// The plus on the lives (shown below the maximum) opens Buy hearts; the plus on the coins opens
    /// the Shop tab, whose bundles grant their coins at once (docs/screens.md, Buying).
    /// What to show on arriving (an unlocked episode, a notice) is decided by the Home flow.
    /// </remarks>
    public sealed class HomeScreen : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] private Button _avatarButton;
        [Tooltip("The picked avatar in its frame, on the avatar button.")]
        [SerializeField] private AvatarFrame _avatar;
        [SerializeField] private LivesDisplay _lives;
        [SerializeField] private AmountLabel _coins;
        [Tooltip("The green plus on the coins display: opens the Shop tab.")]
        [SerializeField] private Button _coinsBuyButton;
        [SerializeField] private Button _settingsButton;

        [Header("Centre")]
        [SerializeField] private Button _levelButton;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private LevelTierLook _levelTierLook;

        [Header("Bottom navigation")]
        [SerializeField] private HomeTabs _tabs;
        [Tooltip("Index of the Shop tab in the tabs' list.")]
        [SerializeField, Min(0)] private int _shopTab;
        [SerializeField] private ShopPage _shopPage;

        [Header("Popups and shared pieces")]
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private AvatarMenuPopup _avatarMenu;
        [SerializeField] private MoreLivesPopup _moreLivesPopup;
        [SerializeField] private BuyPopup _buyPopup;
        [SerializeField] private EpisodeUnlockedOverlay _episodeUnlockedOverlay;

        [Tooltip("Transparent full-screen blocker, active while Home must not take taps (before Episode Unlocked shows).")]
        [SerializeField] private GameObject _inputBlocker;

        [SerializeField] private Toast _toast;

        [Header("Art")]
        [SerializeField] private AvatarConfig _avatars;

        [Header("Text")]
        [SerializeField] private string _levelFormat = "Level {0}";
        [SerializeField] private string _allCompleteText = "All levels complete";
        [Tooltip("What Buy hearts buys, as in \"Buy hearts\".")]
        [SerializeField] private string _heartsName = "hearts";
        [SerializeField] private string _livesFullText = "Lives are already full";
        [Tooltip("Toast after a coin bundle is taken; {0} is the coins.")]
        [SerializeField] private string _bundleBoughtFormat = "+{0} coins";

        private PlayerData _player;
        private SceneLoader _sceneLoader;
        private EconomyConfig _economy;
        private AudioService _audio;

        public EpisodeUnlockedOverlay EpisodeUnlocked => _episodeUnlockedOverlay;

        /// <param name="levelTier">Tier of the level the Level button plays; sets the button's look.</param>
        /// <param name="economy">Prices of the lives refill and the coin bundles.</param>
        /// <param name="audio">Plays the refusal sound of a purchase.</param>
        public void Initialize(
            PlayerData player, SceneLoader sceneLoader, LevelTier levelTier, EconomyConfig economy, AudioService audio)
        {
            _player = player;
            _sceneLoader = sceneLoader;
            _economy = economy;
            _audio = audio;

            _settingsPopup.Initialize(player, showLevelActions: false);
            _avatarMenu.Initialize(player, _avatars);
            _moreLivesPopup.Initialize(player);
            _lives.Bind(player);
            _tabs.Initialize();
            SetInputBlocked(false);

            _player.Wallet.Changed += ShowCoins;
            ShowCoins(_player.Wallet.Coins);
            _player.Changed += ShowAvatar;
            ShowAvatar();

            // An all-complete player replays the last level, which CurrentLevel already points at.
            _levelLabel.text = _player.AllLevelsComplete
                ? _allCompleteText
                : string.Format(_levelFormat, _player.CurrentLevel);
            _levelTierLook.Show(levelTier);

            _levelButton.onClick.AddListener(OnLevelClicked);
            _settingsButton.onClick.AddListener(_settingsPopup.Popup.Open);
            _avatarButton.onClick.AddListener(_avatarMenu.Open);
            _coinsBuyButton.onClick.AddListener(OnCoinsBuyClicked);
            _lives.BuyClicked += OnBuyLivesClicked;
            _shopPage.Initialize(_economy.CoinBundles);
            _shopPage.BundleClicked += OnBundleClicked;
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

        private void ShowCoins(int coins) => _coins.Show(coins);

        private void OnCoinsBuyClicked() => _tabs.Select(_shopTab);

        private void OnBuyLivesClicked() => _buyPopup.Show(_heartsName, _economy.LivesRefillPrice, TryBuyLives);

        /// <returns>True when the popup should close: bought, or nothing left to buy.</returns>
        private bool TryBuyLives()
        {
            if (_player.TryRefillLives(_economy.LivesRefillPrice, out PurchaseRefusal refusal))
            {
                _lives.RefreshNow();
                return true;
            }

            if (refusal == PurchaseRefusal.LivesFull)
            {
                // Lives came back while the popup was open and filled up.
                _lives.RefreshNow();
                _toast.Show(_livesFullText);
                return true;
            }

            _audio.Play(Sfx.MoveInvalid);
            _toast.Show(_buyPopup.NotEnoughCoinsText);
            return false;
        }

        // No real payment yet: the bundle's coins are granted at once.
        private void OnBundleClicked(CoinBundle bundle)
        {
            _player.AddCoins(bundle.Coins);
            _toast.Show(string.Format(_bundleBoughtFormat, bundle.Coins.ToString("N0", CultureInfo.InvariantCulture)));
        }

        private void ShowAvatar() => _avatar.Show(_avatars.GetPicture(_player.AvatarId));

        private void Awake()
        {
            if (_avatarButton == null || _avatar == null || _avatars == null || _lives == null || _coins == null || _settingsButton == null ||
                _coinsBuyButton == null || _levelButton == null || _levelLabel == null || _levelTierLook == null || _tabs == null ||
                _shopPage == null || _settingsPopup == null || _avatarMenu == null || _moreLivesPopup == null || _buyPopup == null ||
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
                _player.Changed -= ShowAvatar;
            }

            if (_lives != null)
            {
                _lives.BuyClicked -= OnBuyLivesClicked;
            }

            if (_shopPage != null)
            {
                _shopPage.BundleClicked -= OnBundleClicked;
            }

            if (_coinsBuyButton != null)
            {
                _coinsBuyButton.onClick.RemoveListener(OnCoinsBuyClicked);
            }
        }
    }
}
