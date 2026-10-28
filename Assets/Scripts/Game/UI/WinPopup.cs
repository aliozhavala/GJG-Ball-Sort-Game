using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>The Win panel: "Level N complete", the coin reward (if any), and Continue.</summary>
    /// <remarks>
    /// Shown only after progress and reward have been saved, so it is purely informational:
    /// closing the app here loses nothing. It has no close button; Continue is the way out.
    /// </remarks>
    [RequireComponent(typeof(Popup))]
    public sealed class WinPopup : MonoBehaviour
    {
        [SerializeField] private Popup _popup;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _reward;
        [SerializeField] private Button _continueButton;
        [SerializeField] private string _titleFormat = "Level {0} complete";
        [SerializeField] private string _rewardFormat = "+{0} <color=#FFC107>●</color>";

        public event Action ContinueClicked;

        public Popup Popup => _popup;

        /// <param name="levelLabel">The level's number, or a level file's name (editor testing).</param>
        /// <param name="reward">Coins paid for the win; 0 hides the reward line (a level file pays nothing).</param>
        public void Show(string levelLabel, int reward)
        {
            _title.text = string.Format(_titleFormat, levelLabel);
            _reward.text = string.Format(_rewardFormat, reward);
            _reward.enabled = reward > 0;
            _popup.Open();
        }

        private void OnContinueClicked() => ContinueClicked?.Invoke();

        private void Awake()
        {
            if (_popup == null || _title == null || _reward == null || _continueButton == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
                return;
            }

            _continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDestroy()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(OnContinueClicked);
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
