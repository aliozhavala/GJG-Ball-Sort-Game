using System;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// One coin bundle on the Shop page: the coins it grants and the price it shows. The price is
    /// display text only; there is no real payment, so a tap grants the coins at once.
    /// </summary>
    [Serializable]
    public sealed class CoinBundle
    {
        [SerializeField, Min(1)] private int _coins = 1000;

        [Tooltip("Shown on the bundle as written, e.g. \"99,99 TL\".")]
        [SerializeField] private string _priceLabel = "";

        public CoinBundle()
        {
        }

        public CoinBundle(int coins, string priceLabel)
        {
            _coins = coins;
            _priceLabel = priceLabel;
        }

        public int Coins => _coins;

        public string PriceLabel => _priceLabel;
    }
}
