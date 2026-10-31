using System;
using System.Collections;
using BallSort.Core.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Home's lives: the count on a heart and the time to the next life (<c>mm:ss</c>, or "Full"),
    /// refreshed every second, which also adds the lives that have come back. Below the maximum a green
    /// plus shows; tapping it raises <see cref="BuyClicked"/> (Buy hearts).
    /// </summary>
    public sealed class LivesDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private TMP_Text _timerLabel;
        [SerializeField] private string _fullText = "Full";

        [Tooltip("The green plus (an art slot), shown only while lives are below the maximum.")]
        [SerializeField] private Button _buyButton;

        private readonly WaitForSecondsRealtime _oneSecond = new WaitForSecondsRealtime(1f);

        private PlayerData _player;

        /// <summary>The plus was tapped.</summary>
        public event Action BuyClicked;

        public void Bind(PlayerData player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            Refresh();
            if (isActiveAndEnabled)
            {
                StopAllCoroutines();
                StartCoroutine(RefreshEverySecond());
            }
        }

        private void OnEnable()
        {
            if (_player != null)
            {
                StartCoroutine(RefreshEverySecond());
            }
        }

        private IEnumerator RefreshEverySecond()
        {
            while (true)
            {
                yield return _oneSecond;
                Refresh();
            }
        }

        private void Refresh()
        {
            TimeSpan? untilNext = _player.GetTimeUntilNextLife();
            _countLabel.text = _player.LifeCount.ToString();
            _timerLabel.text = untilNext.HasValue ? LifeTimerText.Format(untilNext.Value) : _fullText;
            _buyButton.gameObject.SetActive(_player.LifeCount < _player.MaxLives);
        }

        /// <summary>Shows the new lives at once, e.g. after a refill was bought, instead of on the next tick.</summary>
        public void RefreshNow()
        {
            if (_player != null)
            {
                Refresh();
            }
        }

        private void OnBuyClicked() => BuyClicked?.Invoke();

        private void Awake()
        {
            if (_countLabel == null || _timerLabel == null || _buyButton == null)
            {
                Debug.LogError($"{name}: count and timer labels and the buy button must be assigned.", this);
                return;
            }

            _buyButton.onClick.AddListener(OnBuyClicked);
        }

        private void OnDestroy()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(OnBuyClicked);
            }
        }
    }
}
