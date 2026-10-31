using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Home's bottom bar, fitted to the screen: the art spans the full width (a little wider, so its
    /// rounded ends are off-screen in every version) and stands on the screen's bottom edge (a little
    /// below, hiding its rounded bottom corners), reaching into the bottom safe-area inset. The tap
    /// areas stay above that inset, clear of the home gesture.
    /// </summary>
    /// <remarks>
    /// Sits directly under the canvas (outside the safe area), after the pages so it draws over them,
    /// anchored to the bottom centre. The art is scaled uniformly, so the tabs keep their proportions
    /// and the tap areas (children of <see cref="_tapAreas"/>, anchored to their sections) follow.
    /// Pages read its top from <see cref="TopWorldY"/>: <see cref="KeepAboveBar"/> keeps a page above
    /// it, <see cref="ScrollBehindBar"/> lets one scroll behind it.
    /// </remarks>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BottomBar : MonoBehaviour
    {
        [Tooltip("The bar art, stretched over this rect; its sprite's proportions set the bar's height.")]
        [SerializeField] private Image _art;
        [Tooltip("Parent of the tab buttons, which are anchored to their sections of the bar.")]
        [SerializeField] private RectTransform _tapAreas;
        [Tooltip("How much wider than the screen the art is drawn, as a fraction of the screen's width: enough " +
            "to put every version's rounded ends off-screen (the raised Shop and Leaderboard tabs round in " +
            "furthest, 5.7% of the art's width per side).")]
        [SerializeField, Range(0f, 0.3f)] private float _overflow = 0.12f;
        [Tooltip("How far the art sits below the screen's bottom edge, as a fraction of its height, so its rounded " +
            "bottom corners and transparent bottom rows are off-screen.")]
        [SerializeField, Range(0f, 0.2f)] private float _sink = 0.035f;

        private RectTransform _rect;
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _screen;
        private Vector2 _appliedScreenSize;
        private Rect _appliedSafeArea;
        private Sprite _appliedSprite;

        // Read straight from the transforms: a page may ask before this bar's Awake has run.

        /// <summary>The top of the bar's art, in world space.</summary>
        public float TopWorldY
        {
            get
            {
                ((RectTransform)transform).GetWorldCorners(_corners);
                return _corners[1].y;
            }
        }

        /// <summary>The bottom of the screen, in world space.</summary>
        public float ScreenBottomWorldY
        {
            get
            {
                ((RectTransform)transform.parent).GetWorldCorners(_corners);
                return _corners[0].y;
            }
        }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _screen = transform.parent as RectTransform;
            if (_art == null || _tapAreas == null || _screen == null)
            {
                Debug.LogError($"{name}: art and tap areas must be assigned, and the bar must sit under the canvas.", this);
                enabled = false;
                return;
            }

            Apply();
        }

        // Layout upkeep, not game logic: after SafeArea's Update, since the screen and the safe area
        // change without an event (rotation, the device simulator). Comparing a few values is free.
        private void LateUpdate()
        {
            if (_screen.rect.size != _appliedScreenSize || Screen.safeArea != _appliedSafeArea || _art.sprite != _appliedSprite)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _appliedScreenSize = _screen.rect.size;
            _appliedSafeArea = Screen.safeArea;
            _appliedSprite = _art.sprite;
            Sprite sprite = _art.sprite;
            if (sprite == null || _appliedScreenSize.x <= 0f || Screen.height <= 0)
            {
                return;
            }

            float width = _appliedScreenSize.x * (1f + _overflow);
            float height = width * sprite.rect.height / sprite.rect.width;
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0f);
            _rect.pivot = new Vector2(0.5f, 0f);
            float sink = _sink * height;
            _rect.anchoredPosition = new Vector2(0f, -sink);
            _rect.sizeDelta = new Vector2(width, height);

            // The tap areas start above the home-gesture inset (and the part sunk below the screen);
            // the art may reach into the inset.
            float inset = _appliedScreenSize.y * _appliedSafeArea.yMin / Screen.height;
            _tapAreas.anchorMin = Vector2.zero;
            _tapAreas.anchorMax = Vector2.one;
            _tapAreas.offsetMin = new Vector2(0f, Mathf.Clamp(inset + sink, 0f, height * 0.5f));
            _tapAreas.offsetMax = Vector2.zero;
        }
    }
}
