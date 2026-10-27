using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Fits this RectTransform to the device's safe area, keeping UI clear of notches and the
    /// home indicator. Put it on a full-screen panel directly under the canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _appliedArea;
        private Vector2Int _appliedScreen;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        // Layout upkeep, not game logic: the safe area changes on rotation and in the device
        // simulator, and Unity raises no event for it. Comparing two rects per frame is free.
        private void Update()
        {
            if (Screen.safeArea != _appliedArea || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _appliedArea = Screen.safeArea;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            _rect.anchorMin = new Vector2(_appliedArea.xMin / Screen.width, _appliedArea.yMin / Screen.height);
            _rect.anchorMax = new Vector2(_appliedArea.xMax / Screen.width, _appliedArea.yMax / Screen.height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
