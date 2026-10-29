using System;
using System.Collections;
using BallSort.Core.Player;
using TMPro;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Home's lives: the count on a heart and the time to the next life (<c>mm:ss</c>, or "Full"),
    /// refreshed every second, which also adds the lives that have come back.
    /// </summary>
    public sealed class LivesDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private TMP_Text _timerLabel;
        [SerializeField] private string _fullText = "Full";

        private readonly WaitForSecondsRealtime _oneSecond = new WaitForSecondsRealtime(1f);

        private PlayerData _player;

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
        }

        private void Awake()
        {
            if (_countLabel == null || _timerLabel == null)
            {
                Debug.LogError($"{name}: count and timer labels must be assigned.", this);
            }
        }
    }
}
