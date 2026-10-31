using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// What the Level screen shows while stuck: a banner above the board ("No more useful moves") and
    /// Give up / Play on buttons in the booster bar's place. Never covers the tubes.
    /// </summary>
    /// <remarks>
    /// Shown and hidden by activating this GameObject; its root must not be a raycast target, so
    /// taps between the banner and the buttons still reach the board.
    /// </remarks>
    public sealed class StuckPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _bannerLabel;

        [Tooltip("The banner's text: the one place to change it.")]
        [SerializeField] private string _bannerText = "No more useful moves";

        [SerializeField] private Button _giveUpButton;
        [SerializeField] private Button _playOnButton;
        [SerializeField] private TMP_Text _playOnLabel;
        [Tooltip("Play on's label, {0} = the price; the coin comes before it.")]
        [SerializeField] private string _playOnFormat = "Play on <sprite name=\"Icon_Coin\"> {0}";

        private int _shownPrice = -1;

        public event Action GiveUpClicked;
        public event Action PlayOnClicked;

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        /// <summary>
        /// Updates Play on's price and whether it can be bought. Unavailable, it stays visible
        /// with its price but cannot be pressed; Give up is then the only choice.
        /// </summary>
        public void ShowPlayOn(int price, bool available)
        {
            if (price != _shownPrice)
            {
                _shownPrice = price;
                _playOnLabel.text = string.Format(_playOnFormat, price);
            }

            _playOnButton.interactable = available;
        }

        private void OnGiveUpClicked() => GiveUpClicked?.Invoke();

        private void OnPlayOnClicked() => PlayOnClicked?.Invoke();

        private void Awake()
        {
            if (_bannerLabel == null || _giveUpButton == null || _playOnButton == null || _playOnLabel == null)
            {
                Debug.LogError($"{name}: the banner label, both buttons and the Play on label must be assigned.", this);
                return;
            }

            _bannerLabel.text = _bannerText;

            _giveUpButton.onClick.AddListener(OnGiveUpClicked);
            _playOnButton.onClick.AddListener(OnPlayOnClicked);
        }

        private void OnDestroy()
        {
            if (_giveUpButton != null)
            {
                _giveUpButton.onClick.RemoveListener(OnGiveUpClicked);
            }

            if (_playOnButton != null)
            {
                _playOnButton.onClick.RemoveListener(OnPlayOnClicked);
            }
        }
    }
}
