using BallSort.Core.Model;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One ball on screen. Pooled by <see cref="BoardView"/>; never shows a hidden ball's colour.
    /// </summary>
    /// <remarks>
    /// The root is scaled to the ball diameter; the body is normalised to one unit so any sprite
    /// size works, and the hidden mark is authored for a one-unit ball.
    /// </remarks>
    public sealed class BallView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("Shown instead of the colour while the ball is hidden.")]
        [SerializeField] private GameObject _hiddenMark;

        public void Show(Ball ball, ColorPalette palette, float diameter)
        {
            Sprite sprite = ball.IsHidden ? palette.HiddenSprite : palette.GetSprite(ball.ColorId);
            _body.sprite = sprite;
            _body.color = ball.IsHidden ? palette.HiddenTint : palette.GetTint(ball.ColorId);
            _hiddenMark.SetActive(ball.IsHidden);

            float spriteWidth = sprite != null ? sprite.bounds.size.x : 1f;
            _body.transform.localScale = Vector3.one / spriteWidth;
            transform.localScale = Vector3.one * diameter;
        }

        private void Awake()
        {
            if (_body == null || _hiddenMark == null)
            {
                Debug.LogError($"{name}: body and hidden mark must be assigned.", this);
            }
        }
    }
}
