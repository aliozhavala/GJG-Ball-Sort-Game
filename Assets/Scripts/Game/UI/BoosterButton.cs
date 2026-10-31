using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// One booster in the booster bar: its button and remaining count. At 0 the count badge gives way to
    /// the buy badge (a green plus), and a tap opens the Buy popup. An unavailable booster is dimmed but
    /// still clickable, so the level flow can tell the player why it is unavailable.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class BoosterButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _countLabel;

        [Tooltip("The count's badge, shown while the player holds at least one.")]
        [SerializeField] private GameObject _countBadge;

        [Tooltip("The green plus (an art slot) shown instead of the count badge at 0: tapping buys.")]
        [SerializeField] private GameObject _buyBadge;

        [SerializeField] private CanvasGroup _group;
        [SerializeField, Range(0f, 1f)] private float _dimmedAlpha = 0.4f;

        private int _shownCount = -1;

        /// <summary>Raised on every tap, available or not.</summary>
        public event Action Clicked;

        /// <param name="count">Boosters held; 0 shows the buy badge.</param>
        /// <param name="dimmed">Whether to dim the button because using it would be refused.</param>
        public void Show(int count, bool dimmed)
        {
            // Text only when it changes: this runs after every move.
            if (count != _shownCount)
            {
                _shownCount = count;
                _countLabel.text = count.ToString();
                _countBadge.SetActive(count > 0);
                _buyBadge.SetActive(count == 0);
            }

            _group.alpha = dimmed ? _dimmedAlpha : 1f;
        }

        private void OnClicked() => Clicked?.Invoke();

        private void Awake()
        {
            if (_button == null || _countLabel == null || _countBadge == null || _buyBadge == null || _group == null)
            {
                Debug.LogError($"{name}: button, count label, count and buy badges and canvas group must be assigned.", this);
                return;
            }

            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnValidate()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
            }
        }
    }
}
