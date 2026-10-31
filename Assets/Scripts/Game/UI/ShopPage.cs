using System;
using System.Collections.Generic;
using BallSort.Game.Services;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Shop page's coin bundles, top left to bottom right, from <see cref="EconomyConfig.CoinBundles"/>.
    /// There is no real payment: a tap grants the bundle's coins at once (the Home screen does it).
    /// </summary>
    public sealed class ShopPage : MonoBehaviour
    {
        [Tooltip("One per bundle, in the config's order: top left to bottom right.")]
        [SerializeField] private ShopItem[] _items = new ShopItem[0];

        private readonly List<Action> _handlers = new List<Action>();

        /// <summary>Raised with the tapped bundle.</summary>
        public event Action<CoinBundle> BundleClicked;

        public void Initialize(IReadOnlyList<CoinBundle> bundles)
        {
            Unsubscribe();
            if (bundles.Count != _items.Length)
            {
                Debug.LogError($"{name}: {_items.Length} shop items for {bundles.Count} coin bundles; extra items are hidden.", this);
            }

            for (int i = 0; i < _items.Length; i++)
            {
                bool used = i < bundles.Count;
                _items[i].gameObject.SetActive(used);
                if (!used)
                {
                    _handlers.Add(null);
                    continue;
                }

                CoinBundle bundle = bundles[i];
                _items[i].Show(bundle.Coins, bundle.PriceLabel);
                Action handler = () => BundleClicked?.Invoke(bundle);
                _items[i].Clicked += handler;
                _handlers.Add(handler);
            }
        }

        private void Unsubscribe()
        {
            for (int i = 0; i < _handlers.Count && i < _items.Length; i++)
            {
                if (_handlers[i] != null && _items[i] != null)
                {
                    _items[i].Clicked -= _handlers[i];
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
        }

        private void OnDestroy() => Unsubscribe();
    }
}
