using System.Globalization;
using BallSort.Core.Player;
using TMPro;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// An amount on a label of fixed size, on one line: shown in full and shrunk to fit, and abbreviated
    /// (125K, 1.2M) only once the full number would need a size below the minimum.
    /// </summary>
    public sealed class AmountLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField, Min(1f)] private float _maxFontSize = 44f;
        [SerializeField, Min(1f)] private float _minFontSize = 30f;

        public void Show(long amount)
        {
            string text = amount.ToString(CultureInfo.InvariantCulture);
            float size = FittingSize(text);
            if (size < _minFontSize)
            {
                text = AmountFormat.Abbreviate(amount);
                size = Mathf.Max(FittingSize(text), _minFontSize);
            }

            _label.fontSize = size;
            _label.text = text;
        }

        /// <summary>The largest size up to the maximum at which <paramref name="text"/> fits the label's width.</summary>
        private float FittingSize(string text)
        {
            _label.fontSize = _maxFontSize;
            float width = _label.GetPreferredValues(text).x;
            float available = _label.rectTransform.rect.width - _label.margin.x - _label.margin.z;
            return width <= available ? _maxFontSize : _maxFontSize * available / width;
        }

        private void Awake()
        {
            if (_label == null)
            {
                Debug.LogError($"{name}: the label must be assigned.", this);
                enabled = false;
                return;
            }

            // The fit is computed here; TMP's own auto-size or wrapping would fight it.
            _label.enableAutoSizing = false;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void OnValidate()
        {
            _minFontSize = Mathf.Min(_minFontSize, _maxFontSize);
        }
    }
}
