using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// One booster in the booster bar: its button and remaining count. An unavailable booster is
    /// dimmed but still clickable, so the level flow can tell the player why it is unavailable.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class BoosterButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private CanvasGroup _group;
        [SerializeField, Range(0f, 1f)] private float _dimmedAlpha = 0.4f;

        private int _shownCount = -1;

        /// <summary>Raised on every tap, available or not.</summary>
        public event Action Clicked;

        public void Show(int count, bool available)
        {
            // Text only when it changes: this runs after every move.
            if (count != _shownCount)
            {
                _shownCount = count;
                _countLabel.text = count.ToString();
            }

            _group.alpha = available ? 1f : _dimmedAlpha;
        }

        private void OnClicked() => Clicked?.Invoke();

        private void Awake()
        {
            if (_button == null || _countLabel == null || _group == null)
            {
                Debug.LogError($"{name}: button, count label and canvas group must be assigned.", this);
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
