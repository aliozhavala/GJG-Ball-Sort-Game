using System;
using System.Collections.Generic;
using BallSort.Game.Services;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Shop page's coin bundles: the six in the panel's frames, top left to bottom right, from
    /// <see cref="EconomyConfig.CoinBundles"/>, and the three pack cards below it, top to bottom, from
    /// <see cref="EconomyConfig.CoinPacks"/>. There is no real payment: a tap grants the coins at once (the
    /// Home screen does it).
    /// </summary>
    public sealed class ShopPage : MonoBehaviour
    {
        [Tooltip("One per bundle, in the config's order: top left to bottom right.")]
        [SerializeField] private ShopItem[] _items = new ShopItem[0];
        [Tooltip("One per pack card, in the config's order: top to bottom.")]
        [SerializeField] private ShopItem[] _packs = new ShopItem[0];

        private readonly List<(ShopItem Item, Action Handler)> _handlers = new List<(ShopItem, Action)>();

        /// <summary>Raised with the tapped bundle or pack.</summary>
        public event Action<CoinBundle> BundleClicked;

        public void Initialize(IReadOnlyList<CoinBundle> bundles, IReadOnlyList<CoinBundle> packs)
        {
            Unsubscribe();
            Bind(_items, bundles, "coin bundles");
            Bind(_packs, packs, "coin packs");
        }

        private void Bind(ShopItem[] items, IReadOnlyList<CoinBundle> bundles, string kind)
        {
            if (bundles.Count != items.Length)
            {
                Debug.LogError($"{name}: {items.Length} shop items for {bundles.Count} {kind}; extra items are hidden.", this);
            }

            for (int i = 0; i < items.Length; i++)
            {
                bool used = i < bundles.Count;
                items[i].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                CoinBundle bundle = bundles[i];
                items[i].Show(bundle.Coins, bundle.PriceLabel);
                Action handler = () => BundleClicked?.Invoke(bundle);
                items[i].Clicked += handler;
                _handlers.Add((items[i], handler));
            }
        }

        private void Unsubscribe()
        {
            foreach ((ShopItem item, Action handler) in _handlers)
            {
                if (item != null)
                {
                    item.Clicked -= handler;
                }
            }

            _handlers.Clear();
        }

        private void Awake()
        {
            foreach (ShopItem item in _items)
            {
                if (item == null)
                {
                    Debug.LogError($"{name}: every shop item must be assigned.", this);
                    return;
                }
            }

            foreach (ShopItem pack in _packs)
            {
                if (pack == null)
                {
                    Debug.LogError($"{name}: every pack card must be assigned.", this);
                    return;
                }
            }
        }

        private void OnDestroy() => Unsubscribe();
    }
}
