using BallSort.Core.Player;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A coin balance kept up to date: the coins pill (art) with its count. Used inside the Buy popup,
    /// so the player sees what they can afford.
    /// </summary>
    /// <remarks>
    /// The wallet outlives the scene and the popup starts inactive (it never runs <c>OnDestroy</c>), so the
    /// subscription lasts only while the display is shown (<c>OnEnable</c> / <c>OnDisable</c>).
    /// </remarks>
    public sealed class CoinsDisplay : MonoBehaviour
    {
        [SerializeField] private AmountLabel _label;

        private Wallet _wallet;
        private bool _subscribed;

        public void Bind(Wallet wallet)
        {
            Unsubscribe();
            _wallet = wallet;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Show(int coins) => _label.Show(coins);

        private void Subscribe()
        {
            if (_wallet == null || _subscribed)
            {
                return;
            }

            _wallet.Changed += Show;
            _subscribed = true;
            Show(_wallet.Coins);
        }

        private void Unsubscribe()
        {
            if (_wallet != null && _subscribed)
            {
                _wallet.Changed -= Show;
            }

            _subscribed = false;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void Awake()
        {
            if (_label == null)
            {
                Debug.LogError($"{name}: the amount label must be assigned.", this);
            }
        }
    }
}
