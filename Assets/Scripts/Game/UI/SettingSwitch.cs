using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The on/off switch drawn for a setting: a knob in the track, on the left with the off sprite
    /// when off, on the right with the on sprite when on. A change slides the knob across and swaps
    /// the sprite halfway, so only one knob is ever visible.
    /// </summary>
    /// <remarks>Driven by <see cref="SettingToggle"/>: <see cref="Snap"/> to sync, <see cref="Slide"/> on a tap.</remarks>
    public sealed class SettingSwitch : MonoBehaviour
    {
        [SerializeField] private RectTransform _knob;
        [SerializeField] private Image _knobImage;
        [SerializeField] private Sprite _offSprite;
        [SerializeField] private Sprite _onSprite;

        [Tooltip("The knob's anchored x when off (left end of the track) and when on (right end).")]
        [SerializeField] private float _offX;
        [SerializeField] private float _onX;

        [SerializeField, Min(0f)] private float _slideDuration = 0.18f;
        [SerializeField] private Ease _slideEase = Ease.OutCubic;

        private Tween _slide;

        /// <summary>Shows <paramref name="isOn"/> at once, ending any slide.</summary>
        public void Snap(bool isOn)
        {
            _slide?.Kill();
            _slide = null;
            SetKnob(isOn, isOn ? _onX : _offX);
        }

        /// <summary>Slides the knob to <paramref name="isOn"/>'s side; the sprite swaps halfway.</summary>
        public void Slide(bool isOn)
        {
            _slide?.Kill();
            float target = isOn ? _onX : _offX;
            float from = _knob.anchoredPosition.x;
            float halfway = (from + target) * 0.5f;
            bool swapped = false;

            // Unscaled: popups run while gameplay may be paused, and the setting is UI, not the board.
            _slide = DOTween.To(() => _knob.anchoredPosition.x, x =>
                {
                    _knob.anchoredPosition = new Vector2(x, _knob.anchoredPosition.y);
                    if (!swapped && (x - halfway) * (target - halfway) >= 0f)
                    {
                        swapped = true;
                        _knobImage.sprite = isOn ? _onSprite : _offSprite;
                    }
                }, target, _slideDuration)
                .SetEase(_slideEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => SetKnob(isOn, target));
        }

        private void SetKnob(bool isOn, float x)
        {
            _knob.anchoredPosition = new Vector2(x, _knob.anchoredPosition.y);
            _knobImage.sprite = isOn ? _onSprite : _offSprite;
        }

        // A popup closed mid-slide reopens with the knob where the setting is.
        private void OnDisable()
        {
            if (_slide != null && _slide.IsActive())
            {
                _slide.Complete();
            }

            _slide = null;
        }

        private void Awake()
        {
            if (_knob == null || _knobImage == null || _offSprite == null || _onSprite == null)
            {
                Debug.LogError($"{name}: knob, knob image and both sprites must be assigned.", this);
            }
        }
    }
}
