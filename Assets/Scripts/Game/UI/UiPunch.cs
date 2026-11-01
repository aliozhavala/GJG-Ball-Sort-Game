using BallSort.Game.Views;
using DG.Tweening;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A springy scale punch on a UI element, e.g. on what a purchase just filled (a booster's count,
    /// the lives heart, the coins pill). Timings in <see cref="AnimationConfig"/>; real time, so it
    /// plays whatever the board is doing.
    /// </summary>
    public sealed class UiPunch : MonoBehaviour
    {
        [SerializeField] private AnimationConfig _animation;

        [Tooltip("What scales; this object's own transform when empty.")]
        [SerializeField] private Transform _target;

        private Vector3 _restScale = Vector3.one;
        private Tween _punch;

        /// <summary>Plays the punch from the element's resting size; a running one restarts. Nothing while hidden.</summary>
        public void Play()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            _punch?.Kill();
            _target.localScale = _restScale;
            _punch = _target
                .DOPunchScale(_restScale * _animation.PurchasePunchScale, _animation.PurchasePunchDuration,
                    _animation.PurchasePunchVibrato, 0.5f)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnKill(Rest);
        }

        private void Rest()
        {
            if (_target != null)
            {
                _target.localScale = _restScale;
            }
        }

        private void Awake()
        {
            if (_target == null)
            {
                _target = transform;
            }

            _restScale = _target.localScale;
            if (_animation == null)
            {
                Debug.LogError($"{name}: animation config must be assigned.", this);
                enabled = false;
            }
        }

        // A punch cut off by hiding the element leaves it at its resting size.
        private void OnDisable() => _punch?.Kill();
    }
}
