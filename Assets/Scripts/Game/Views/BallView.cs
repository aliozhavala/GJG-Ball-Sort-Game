using BallSort.Core.Model;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One ball on screen. Pooled by <see cref="BoardView"/>; never shows a hidden ball's colour or key.
    /// </summary>
    /// <remarks>
    /// The root is scaled to the ball diameter; the body and the marks are normalised to it, so any
    /// sprite size works. All are SpriteRenderers so they scale and sort together.
    /// <para>
    /// Every change of position goes through <see cref="MoveTo"/>, and every change of the key mark
    /// through <see cref="UpdateKeyMark"/>, so the animation pass can swap each instant snap for a
    /// tween in one place.
    /// </para>
    /// <para>
    /// Its face goes through <see cref="SetExpression"/> alone, so blinking and laughing can be
    /// animated there. Balls of one tube draw in slot order, upper balls in front, so appendages
    /// reaching into the slot above tuck behind that ball.
    /// </para>
    /// <para>
    /// The ball keeps its own copy of its key: by the time a reveal is announced the board has
    /// already found the key (docs/mechanics.md), so the mark must show on the reveal and only go
    /// when <see cref="RemoveKey"/> follows.
    /// </para>
    /// </remarks>
    public sealed class BallView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("The \"?\" drawn over the body while the ball is hidden.")]
        [SerializeField] private SpriteRenderer _hiddenMark;
        [Tooltip("The key drawn on a revealed ball that carries one.")]
        [SerializeField] private SpriteRenderer _keyMark;

        private int _bodySortingLayer;
        private int _bodySortingOrder;
        private int _markSortingLayer;
        private int _markSortingOrder;
        private int _keySortingLayer;
        private int _keySortingOrder;
        private int _ordersPerBall = 1;

        private bool _isHidden;
        private int _colorId;
        private BallExpression _expression;
        private int _keyLockId = Ball.NoKey;

        /// <summary>Board index of the tube holding this ball.</summary>
        public int TubeIndex { get; private set; } = -1;

        /// <summary>Slot within that tube, 0 = bottom.</summary>
        public int Slot { get; private set; } = -1;

        /// <summary>True while drawn above the tube's glass front (selected or in flight).</summary>
        public bool IsRaised { get; private set; }

        /// <summary>Shows <paramref name="ball"/> as the board holds it: colour or hidden, with or without its key.</summary>
        public void Show(Ball ball, ColorPalette palette, LockPalette locks, float diameter)
        {
            _hiddenMark.sprite = palette.HiddenMarkSprite;
            _hiddenMark.transform.localScale = Vector3.one * (palette.HiddenMarkSize / SpriteWidth(palette.HiddenMarkSprite));

            _keyMark.sprite = palette.KeyMarkSprite;
            _keyMark.transform.localScale = Vector3.one * (palette.KeyMarkSize / SpriteWidth(palette.KeyMarkSprite));
            _keyMark.transform.localPosition = palette.KeyMarkOffset;
            _keyMark.color = locks.GetTint(ball.KeyLockId);
            _keyLockId = ball.KeyLockId;

            _expression = BallExpression.Open;
            ShowColour(ball.ColorId, ball.IsHidden, palette);
            transform.localScale = Vector3.one * diameter;
        }

        /// <summary>Turns a hidden ball face up. A key it carries stays, now visible, until <see cref="RemoveKey"/>.</summary>
        public void Reveal(int colorId, ColorPalette palette) => ShowColour(colorId, false, palette);

        /// <summary>
        /// The single entry point for the ball's face. Snaps for now. A hidden ball keeps its hidden
        /// look and shows the expression once revealed.
        /// </summary>
        public void SetExpression(BallExpression expression, ColorPalette palette)
        {
            _expression = expression;
            if (!_isHidden)
            {
                _body.sprite = palette.GetSprite(_colorId, expression);
            }
        }

        /// <summary>The ball's key has been found; its mark goes.</summary>
        public void RemoveKey()
        {
            _keyLockId = Ball.NoKey;
            UpdateKeyMark();
        }

        /// <summary>Records which tube and slot this ball now belongs to.</summary>
        public void AssignSlot(int tubeIndex, int slot)
        {
            TubeIndex = tubeIndex;
            Slot = slot;
        }

        /// <summary>The single entry point for moving a ball, in its parent's space. Snaps for now.</summary>
        public void MoveTo(Vector3 localPosition) => transform.localPosition = localPosition;

        /// <summary>
        /// Draws the ball above (<paramref name="raised"/>) or back inside its tube. A raised ball's
        /// lowest part sorts at <paramref name="sortingOrder"/> on <paramref name="sortingLayer"/>.
        /// </summary>
        public void SetRaised(bool raised, int sortingLayer, int sortingOrder)
        {
            IsRaised = raised;
            int slotOrder = Mathf.Max(Slot, 0) * _ordersPerBall;
            if (raised)
            {
                // Keep the parts' order: body, then the marks on top of it.
                int baseOrder = sortingOrder - Mathf.Min(_bodySortingOrder, Mathf.Min(_markSortingOrder, _keySortingOrder)) + slotOrder;
                Apply(_body, sortingLayer, baseOrder + _bodySortingOrder);
                Apply(_hiddenMark, sortingLayer, baseOrder + _markSortingOrder);
                Apply(_keyMark, sortingLayer, baseOrder + _keySortingOrder);
            }
            else
            {
                Apply(_body, _bodySortingLayer, _bodySortingOrder + slotOrder);
                Apply(_hiddenMark, _markSortingLayer, _markSortingOrder + slotOrder);
                Apply(_keyMark, _keySortingLayer, _keySortingOrder + slotOrder);
            }
        }

        private void ShowColour(int colorId, bool hidden, ColorPalette palette)
        {
            _isHidden = hidden;
            _colorId = colorId;
            Sprite sprite = hidden ? palette.HiddenSprite : palette.GetSprite(colorId, _expression);
            _body.sprite = sprite;
            _body.color = hidden ? palette.HiddenTint : palette.GetSpriteTint(colorId);
            _hiddenMark.enabled = hidden && _hiddenMark.sprite != null;
            float size = hidden ? palette.HiddenSpriteSize : palette.GetSpriteSize(colorId);
            _body.transform.localScale = Vector3.one * (size / SpriteWidth(sprite));
            UpdateKeyMark();
        }

        /// <summary>
        /// The single entry point for the key mark: drawn while the ball is revealed and still carries
        /// its key, so a key on a hidden ball stays invisible. Snaps for now.
        /// </summary>
        private void UpdateKeyMark() =>
            _keyMark.enabled = _keyLockId != Ball.NoKey && !_isHidden && _keyMark.sprite != null;

        private static float SpriteWidth(Sprite sprite) => sprite != null ? sprite.bounds.size.x : 1f;

        private static void Apply(Renderer renderer, int layer, int order)
        {
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
        }

        private void Awake()
        {
            if (_body == null || _hiddenMark == null || _keyMark == null)
            {
                Debug.LogError($"{name}: body, hidden mark and key mark must be assigned.", this);
                return;
            }

            _bodySortingLayer = _body.sortingLayerID;
            _bodySortingOrder = _body.sortingOrder;
            _markSortingLayer = _hiddenMark.sortingLayerID;
            _markSortingOrder = _hiddenMark.sortingOrder;
            _keySortingLayer = _keyMark.sortingLayerID;
            _keySortingOrder = _keyMark.sortingOrder;
            _ordersPerBall = Mathf.Max(_bodySortingOrder, Mathf.Max(_markSortingOrder, _keySortingOrder)) -
                Mathf.Min(_bodySortingOrder, Mathf.Min(_markSortingOrder, _keySortingOrder)) + 1;
        }
    }
}
