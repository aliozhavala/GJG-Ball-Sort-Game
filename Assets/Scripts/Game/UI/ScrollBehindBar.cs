using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A Home page's scroll view that runs down behind the bottom bar: its bottom is pulled to the
    /// screen's bottom edge, so content scrolls out from behind the bar, and its content gets enough
    /// bottom padding for the last item to scroll fully into view above the bar.
    /// </summary>
    /// <remarks>
    /// Put it on the page's scroll view (the RectTransform holding the viewport), with the content's
    /// layout group. Shop uses it; the Leaderboard page can use the same structure: a scroll view with
    /// this component and the <see cref="BottomBar"/> drawn after the pages.
    /// </remarks>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScrollBehindBar : MonoBehaviour
    {
        [SerializeField] private BottomBar _bar;
        [Tooltip("The scrolled content's layout; its bottom padding is set here.")]
        [SerializeField] private LayoutGroup _content;
        [Tooltip("Space between the last item and the bar's top once scrolled to the end, in canvas units.")]
        [SerializeField, Min(0f)] private float _endSpacing = 40f;

        private RectTransform _rect;
        private readonly Vector3[] _corners = new Vector3[4];

        private void Awake()
        {
            _rect = (RectTransform)transform;
            if (_bar == null || _content == null)
            {
                Debug.LogError($"{name}: bar and content must be assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable() => Apply();

        // Layout upkeep, not game logic: the bar and the safe area move without an event (see
        // BottomBar); this writes only when something is out of place.
        private void LateUpdate() => Apply();

        private void Apply()
        {
            _rect.GetWorldCorners(_corners);
            float screenBottom = _bar.ScreenBottomWorldY;
            float scale = _rect.lossyScale.y;
            if (scale <= 0f)
            {
                return;
            }

            // Reach the screen's bottom edge, whatever inset the parent (the safe area) leaves.
            float shift = (screenBottom - _corners[0].y) / scale;
            if (Mathf.Abs(shift) > 0.01f)
            {
                _rect.offsetMin += new Vector2(0f, shift);
            }

            int padding = Mathf.CeilToInt((_bar.TopWorldY - screenBottom) / _content.transform.lossyScale.y + _endSpacing);
            if (_content.padding.bottom != padding)
            {
                _content.padding.bottom = padding;
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)_content.transform);
            }
        }
    }
}
