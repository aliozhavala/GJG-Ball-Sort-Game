using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A full-screen UI background: an image that envelops its parent (the canvas) at any aspect
    /// ratio, cropped rather than stretched. Sits first under the canvas, outside the safe area.
    /// </summary>
    public sealed class BackgroundImage : MonoBehaviour
    {
        [SerializeField] private Image _image;

        [Tooltip("Set to Envelope Parent; its aspect ratio follows the sprite.")]
        [SerializeField] private AspectRatioFitter _fitter;

        /// <summary>Shows <paramref name="sprite"/>; null hides the background.</summary>
        public void Show(Sprite sprite)
        {
            _image.sprite = sprite;
            _image.enabled = sprite != null;
            if (sprite != null)
            {
                _fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
        }

        private void Awake()
        {
            if (_image == null || _fitter == null)
            {
                Debug.LogError($"{name}: image and aspect ratio fitter must be assigned.", this);
            }
        }
    }
}
