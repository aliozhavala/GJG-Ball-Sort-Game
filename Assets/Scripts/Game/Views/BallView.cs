using BallSort.Core.Model;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One ball on screen. Pooled by <see cref="BoardView"/>; never shows a hidden ball's colour.
    /// </summary>
    /// <remarks>
    /// The root is scaled to the ball diameter; the body and the hidden mark are normalised to it,
    /// so any sprite size works. Both are SpriteRenderers so they scale and sort together.
    /// <para>
    /// Every change of position goes through <see cref="MoveTo"/>, so the animation pass can swap
    /// the instant snap for a tween in one place.
    /// </para>
    /// </remarks>
    public sealed class BallView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("The \"?\" drawn over the body while the ball is hidden.")]
        [SerializeField] private SpriteRenderer _hiddenMark;

        private int _bodySortingLayer;
        private int _bodySortingOrder;
        private int _markSortingLayer;
        private int _markSortingOrder;

        /// <summary>Board index of the tube holding this ball.</summary>
        public int TubeIndex { get; private set; } = -1;

        /// <summary>Slot within that tube, 0 = bottom.</summary>
        public int Slot { get; private set; } = -1;

        /// <summary>True while drawn above the tube's glass front (selected or in flight).</summary>
        public bool IsRaised { get; private set; }

        public void Show(Ball ball, ColorPalette palette, float diameter)
        {
            _hiddenMark.sprite = palette.HiddenMarkSprite;
            _hiddenMark.transform.localScale = Vector3.one * (palette.HiddenMarkSize / SpriteWidth(palette.HiddenMarkSprite));
            ShowColour(ball, palette);
            transform.localScale = Vector3.one * diameter;
        }

        /// <summary>Turns a hidden ball face up.</summary>
        public void Reveal(int colorId, ColorPalette palette) => ShowColour(new Ball(colorId), palette);

        /// <summary>Records which tube and slot this ball now belongs to.</summary>
        public void AssignSlot(int tubeIndex, int slot)
        {
            TubeIndex = tubeIndex;
            Slot = slot;
        }

        /// <summary>The single entry point for moving a ball, in its parent's space. Snaps for now.</summary>
        public void MoveTo(Vector3 localPosition) => transform.localPosition = localPosition;

        /// <summary>
        /// Draws the ball above (<paramref name="raised"/>) or back inside its tube. Raised balls use
        /// the glass front's sorting layer, just above <paramref name="frontSortingOrder"/>.
        /// </summary>
        public void SetRaised(bool raised, int frontSortingLayer, int frontSortingOrder)
        {
            IsRaised = raised;
            if (raised)
            {
                // Keep the body/mark order: the mark stays on top of the body.
                int baseOrder = frontSortingOrder + 1 - Mathf.Min(_bodySortingOrder, _markSortingOrder);
                Apply(_body, frontSortingLayer, baseOrder + _bodySortingOrder);
                Apply(_hiddenMark, frontSortingLayer, baseOrder + _markSortingOrder);
            }
            else
            {
                Apply(_body, _bodySortingLayer, _bodySortingOrder);
                Apply(_hiddenMark, _markSortingLayer, _markSortingOrder);
            }
        }

        private void ShowColour(Ball ball, ColorPalette palette)
        {
            Sprite sprite = ball.IsHidden ? palette.HiddenSprite : palette.GetSprite(ball.ColorId);
            _body.sprite = sprite;
            _body.color = ball.IsHidden ? palette.HiddenTint : palette.GetTint(ball.ColorId);
            _hiddenMark.enabled = ball.IsHidden && _hiddenMark.sprite != null;
            _body.transform.localScale = Vector3.one / SpriteWidth(sprite);
        }

        private static float SpriteWidth(Sprite sprite) => sprite != null ? sprite.bounds.size.x : 1f;

        private static void Apply(Renderer renderer, int layer, int order)
        {
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
        }

        private void Awake()
        {
            if (_body == null || _hiddenMark == null)
            {
                Debug.LogError($"{name}: body and hidden mark must be assigned.", this);
                return;
            }

            _bodySortingLayer = _body.sortingLayerID;
            _bodySortingOrder = _body.sortingOrder;
            _markSortingLayer = _hiddenMark.sortingLayerID;
            _markSortingOrder = _hiddenMark.sortingOrder;
        }
    }
}
