using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Keeps a Home page's bottom edge just above the bottom bar, whose height follows the screen's
    /// width (<see cref="BottomBar"/>), so content anchored to the page's bottom (the Level button)
    /// never slides under the bar. Pages that scroll behind the bar use <see cref="ScrollBehindBar"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class KeepAboveBar : MonoBehaviour
    {
        [SerializeField] private BottomBar _bar;
        [Tooltip("Space between the bar's top and this rect's bottom, in canvas units.")]
        [SerializeField] private float _gap = 16f;

        private RectTransform _rect;
        private readonly Vector3[] _corners = new Vector3[4];

        private void Awake()
        {
            _rect = (RectTransform)transform;
            if (_bar == null)
            {
                Debug.LogError($"{name}: the bar must be assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable() => Apply();

        // Layout upkeep, not game logic: the bar and the safe area move without an event (see
        // BottomBar); this writes only when the page is out of place.
        private void LateUpdate() => Apply();

        private void Apply()
        {
            _rect.GetWorldCorners(_corners);
            float scale = _rect.lossyScale.y;
            if (scale <= 0f)
            {
                return;
            }

            float shift = (_bar.TopWorldY + _gap * scale - _corners[0].y) / scale;
            if (Mathf.Abs(shift) > 0.01f)
            {
                _rect.offsetMin += new Vector2(0f, shift);
            }
        }
    }
}
