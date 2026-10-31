using BallSort.Core.Player;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The avatar pictures, by avatar id (<see cref="AvatarRules"/>), and the frame they sit in, plain and selected.
    /// The single place avatar art swaps.
    /// </summary>
    [CreateAssetMenu(fileName = "AvatarConfig", menuName = "Ball Sort/Avatar Config")]
    public sealed class AvatarConfig : ScriptableObject
    {
        [Tooltip("Element 0 is avatar 1. One picture per avatar id.")]
        [SerializeField] private Sprite[] _pictures = new Sprite[0];
        [Tooltip("The frame around an avatar, and the empty frames of the avatar menu.")]
        [SerializeField] private Sprite _frame;
        [Tooltip("The frame of the selected avatar in the avatar menu (same rectangle as the plain frame).")]
        [SerializeField] private Sprite _selectedFrame;

        public Sprite Frame => _frame;

        public Sprite SelectedFrame => _selectedFrame;

        /// <summary>The picture of <paramref name="avatarId"/>; null for an id without one.</summary>
        public Sprite GetPicture(int avatarId) =>
            avatarId >= 1 && avatarId <= _pictures.Length ? _pictures[avatarId - 1] : null;

        private void OnValidate()
        {
            if (_frame == null || _selectedFrame == null)
            {
                Debug.LogWarning($"{name}: the plain and selected frames must be assigned.", this);
            }

            if (_pictures.Length != AvatarRules.Count)
            {
                Debug.LogWarning($"{name}: expected {AvatarRules.Count} pictures (AvatarRules.Count), found {_pictures.Length}.", this);
            }

            foreach (Sprite picture in _pictures)
            {
                if (picture == null)
                {
                    Debug.LogWarning($"{name}: every avatar needs a picture.", this);
                    return;
                }
            }
        }
    }
}
