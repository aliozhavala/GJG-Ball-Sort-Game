using System;
using BallSort.Game.Views;
using DG.Tweening;
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
    /// Shown and hidden by activating this GameObject; it fades in (docs/animation-spec.md, 5.1) and
    /// hides at once. Its root must not be a raycast target, so taps between the banner and the
    /// buttons still reach the board.
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class StuckPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [Tooltip("The fade-in's duration.")]
        [SerializeField] private AnimationConfig _animation;
        [SerializeField] private TMP_Text _bannerLabel;

        [Tooltip("The banner's text: the one place to change it.")]
        [SerializeField] private string _bannerText = "No more useful moves";

        [SerializeField] private Button _giveUpButton;
        [SerializeField] private Button _playOnButton;
        [SerializeField] private TMP_Text _playOnLabel;
        [Tooltip("Play on's label, {0} = the price; the coin comes before it.")]
        [SerializeField] private string _playOnFormat = "Play on <sprite name=\"Icon_Coin\"> {0}";

        private int _shownPrice = -1;
        private Tween _fade;

        public event Action GiveUpClicked;
        public event Action PlayOnClicked;

        /// <summary>Fades the panel in (real time), or hides it at once.</summary>
        public void SetVisible(bool visible)
        {
            _fade?.Kill();
            _fade = null;
            gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            float duration = _animation.StuckFadeDuration;
            _group.alpha = duration > 0f ? 0f : 1f;
            if (duration > 0f)
            {
                _fade = _group.DOFade(1f, duration).SetUpdate(true).SetLink(gameObject);
            }
        }

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
            if (_group == null || _animation == null || _bannerLabel == null || _giveUpButton == null ||
                _playOnButton == null || _playOnLabel == null)
            {
                Debug.LogError(
                    $"{name}: canvas group, animation config, banner label, both buttons and the Play on label must be assigned.", this);
                return;
            }

            _bannerLabel.text = _bannerText;

            _giveUpButton.onClick.AddListener(OnGiveUpClicked);
            _playOnButton.onClick.AddListener(OnPlayOnClicked);
        }

        private void OnValidate()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            _fade?.Kill();

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
