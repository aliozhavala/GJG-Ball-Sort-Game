using System;
using System.Collections;
using BallSort.Core.Player;
using TMPro;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// "More lives": shown at 0 lives instead of starting a level (Home's Level button, the Level
    /// scene's Restart). Shows the lives and the wait for the next one, refreshed every second while
    /// open; its close button goes back to whatever is beneath it. Used by both scenes.
    /// </summary>
    [RequireComponent(typeof(Popup))]
    public sealed class MoreLivesPopup : MonoBehaviour
    {
        [SerializeField] private Popup _popup;
        [SerializeField] private TMP_Text _livesLabel;
        [SerializeField] private TMP_Text _timerLabel;
        [SerializeField] private string _timerFormat = "Next life in {0}";
        [SerializeField] private string _fullText = "Lives full";

        private readonly WaitForSecondsRealtime _oneSecond = new WaitForSecondsRealtime(1f);

        private PlayerData _player;

        public Popup Popup => _popup;

        public void Initialize(PlayerData player) => _player = player ?? throw new ArgumentNullException(nameof(player));

        public void Open() => _popup.Open();

        // Opening activates the GameObject, which starts the countdown; closing stops it.
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
                Refresh();
                yield return _oneSecond;
            }
        }

        private void Refresh()
        {
            TimeSpan? untilNext = _player.GetTimeUntilNextLife();
            _livesLabel.text = _player.LifeCount.ToString();
            _timerLabel.text = untilNext.HasValue ? string.Format(_timerFormat, LifeTimerText.Format(untilNext.Value)) : _fullText;
        }

        private void Awake()
        {
            if (_popup == null || _livesLabel == null || _timerLabel == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
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
