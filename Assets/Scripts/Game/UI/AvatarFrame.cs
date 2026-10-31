using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// An avatar in its frame: the picture, masked to the frame's opening, or the empty frame. Used by
    /// Home's avatar button and the avatar menu.
    /// </summary>
    public sealed class AvatarFrame : MonoBehaviour
    {
        [Tooltip("The frame, drawn under the picture; its sprite swaps when the avatar is selected.")]
        [SerializeField] private Image _frame;
        [Tooltip("The avatar picture, under a mask shaped like the frame's opening.")]
        [SerializeField] private Image _picture;
        [Tooltip("Optional. Picks this frame's avatar.")]
        [SerializeField] private Button _button;

        public Button Button => _button;

        /// <summary>Shows <paramref name="picture"/> in the frame; null leaves the frame empty.</summary>
        public void Show(Sprite picture)
        {
            _picture.sprite = picture;
            _picture.enabled = picture != null;
        }

        /// <summary>Draws the frame with <paramref name="frame"/>: the plain or the selected one (<see cref="AvatarConfig"/>).</summary>
        public void ShowFrame(Sprite frame) => _frame.sprite = frame;

        private void Awake()
        {
            if (_frame == null || _picture == null)
            {
                Debug.LogError($"{name}: the frame and the picture must be assigned.", this);
            }
        }
    }
}
