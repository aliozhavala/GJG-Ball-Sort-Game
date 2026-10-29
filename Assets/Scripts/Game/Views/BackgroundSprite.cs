using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// A full-screen background in world space: a sprite on the Background sorting layer, centred on
    /// the camera and scaled to cover its whole view at any aspect ratio (cropped, never stretched).
    /// </summary>
    public sealed class BackgroundSprite : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Camera _camera;

        private Vector2Int _fittedScreen;
        private float _fittedOrthographicSize;

        /// <summary>Shows <paramref name="sprite"/>; null hides the background.</summary>
        public void Show(Sprite sprite)
        {
            _renderer.sprite = sprite;
            Fit();
        }

        private void Fit()
        {
            _fittedScreen = new Vector2Int(Screen.width, Screen.height);
            _fittedOrthographicSize = _camera.orthographicSize;

            Sprite sprite = _renderer.sprite;
            if (sprite == null)
            {
                return;
            }

            float viewHeight = _camera.orthographicSize * 2f;
            float viewWidth = viewHeight * _camera.aspect;
            Vector2 spriteSize = sprite.bounds.size;
            float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);

            Vector3 cameraPosition = _camera.transform.position;
            transform.position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        // Layout only, like BoardLayout: refit when the screen or the camera's view changes size.
        private void LateUpdate()
        {
            if (Screen.width != _fittedScreen.x || Screen.height != _fittedScreen.y ||
                !Mathf.Approximately(_camera.orthographicSize, _fittedOrthographicSize))
            {
                Fit();
            }
        }

        private void Awake()
        {
            if (_renderer == null || _camera == null)
            {
                Debug.LogError($"{name}: sprite renderer and camera must be assigned.", this);
                enabled = false;
            }
        }
    }
}
