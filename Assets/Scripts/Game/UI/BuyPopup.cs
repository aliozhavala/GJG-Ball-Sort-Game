using System;
using BallSort.Core.Player;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A title ("Buy Undo", "Refill hearts"), a picture of what is bought, and a yellow button showing
    /// only the price and a coin icon (docs/screens.md, Buying). One popup serves every purchase in its
    /// scene: boosters in Level, hearts on Home. The Level scene also shows the coin balance in it.
    /// </summary>
    /// <remarks>
    /// The caller passes what a tap on the price does. When it buys, the popup closes; when it is
    /// refused (not enough coins), the caller gives the feedback and the popup stays open. The ✕ closes it.
    /// </remarks>
    [RequireComponent(typeof(Popup))]
    public sealed class BuyPopup : MonoBehaviour
    {
        [SerializeField] private Popup _popup;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _buyButton;

        [Tooltip("What is bought (booster or heart art); hidden when the caller passes none.")]
        [SerializeField] private Image _picture;

        [Tooltip("Optional coin balance, shown only once ShowCoins is called (the Level scene; Home's top bar shows it).")]
        [SerializeField] private CoinsDisplay _coins;

        [Tooltip("The price on the buy button, next to the coin icon (an art slot on the button).")]
        [SerializeField] private AmountLabel _price;

        [SerializeField] private string _titleFormat = "Buy {0}";

        [Tooltip("Toast when the player cannot afford the price; the popup stays open.")]
        [SerializeField] private string _notEnoughCoins = "Not enough coins";

        private Func<bool> _tryBuy;

        public Popup Popup => _popup;

        /// <summary>The toast for a purchase refused for lack of coins.</summary>
        public string NotEnoughCoinsText => _notEnoughCoins;

        /// <summary>"Buy {item}", the usual title, e.g. "Buy Undo".</summary>
        public string BuyTitle(string itemName) => string.Format(_titleFormat, itemName);

        /// <summary>Shows the coin balance of <paramref name="wallet"/> in the popup, kept up to date.</summary>
        public void ShowCoins(Wallet wallet)
        {
            if (_coins == null)
            {
                Debug.LogError($"{name}: no coins display to show the balance in.", this);
                return;
            }

            _coins.Bind(wallet);
            _coins.gameObject.SetActive(true);
        }

        /// <param name="title">The whole title, e.g. <see cref="BuyTitle"/> or "Refill hearts".</param>
        /// <param name="picture">What is bought; null hides the picture.</param>
        /// <param name="price">Coins shown on the button.</param>
        /// <param name="tryBuy">Runs on a tap on the price; returns true when bought, which closes the popup.</param>
        public void Show(string title, Sprite picture, int price, Func<bool> tryBuy)
        {
            _tryBuy = tryBuy ?? throw new ArgumentNullException(nameof(tryBuy));
            _title.text = title;
            if (_picture != null)
            {
                _picture.sprite = picture;
                _picture.gameObject.SetActive(picture != null);
            }

            _popup.Open();

            // After opening: the label fits its text to its laid-out width.
            _price.Show(price);
        }

        private void OnBuyClicked()
        {
            if (_tryBuy != null && _tryBuy())
            {
                _popup.Close();
            }
        }

        private void OnClosed() => _tryBuy = null;

        private void Awake()
        {
            if (_popup == null || _title == null || _buyButton == null || _price == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
                return;
            }

            _buyButton.onClick.AddListener(OnBuyClicked);
            _popup.Closed += OnClosed;
        }

        private void OnDestroy()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(OnBuyClicked);
            }

            if (_popup != null)
            {
                _popup.Closed -= OnClosed;
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
