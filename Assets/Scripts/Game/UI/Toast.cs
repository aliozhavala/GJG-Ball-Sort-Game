using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A short message that fades in, waits, and fades out. Never blocks input. A new message
    /// replaces the one on screen instead of queueing behind it.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class Toast : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private CanvasGroup _group;
        [SerializeField, Min(0.1f)] private float _visibleSeconds = 1.6f;
        [SerializeField, Min(0f)] private float _fadeSeconds = 0.15f;

        private Sequence _sequence;

        public void Show(string message)
        {
            _sequence?.Kill();
            _label.text = message;
            _group.alpha = 0f;

            _sequence = DOTween.Sequence()
                .Append(_group.DOFade(1f, _fadeSeconds))
                .AppendInterval(_visibleSeconds)
                .Append(_group.DOFade(0f, _fadeSeconds))
                .SetLink(gameObject);
        }

        private void Awake()
        {
            if (_label == null || _group == null)
            {
                Debug.LogError($"{name}: label and canvas group must be assigned.", this);
                enabled = false;
                return;
            }

            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void OnDestroy() => _sequence?.Kill();

        private void OnValidate()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
            }
        }
    }
}
