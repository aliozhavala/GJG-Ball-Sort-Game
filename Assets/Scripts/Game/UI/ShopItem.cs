using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// One coin bundle in a frame of the Shop panel: its picture (an art slot), the coins and the price
    /// label. A tap anywhere on it raises <see cref="Clicked"/>.
    /// </summary>
    public sealed class ShopItem : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _coinsLabel;
        [SerializeField] private TMP_Text _priceLabel;

        public event Action Clicked;

        /// <param name="coins">Shown in full with thousands separators, e.g. "10,000".</param>
        /// <param name="priceLabel">Shown as written.</param>
        public void Show(int coins, string priceLabel)
        {
            _coinsLabel.text = coins.ToString("N0", CultureInfo.InvariantCulture);
            _priceLabel.text = priceLabel;
        }

        private void OnClicked() => Clicked?.Invoke();

        private void Awake()
        {
            if (_button == null || _coinsLabel == null || _priceLabel == null)
            {
                Debug.LogError($"{name}: button, coins label and price label must be assigned.", this);
                return;
            }

            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }
    }
}
