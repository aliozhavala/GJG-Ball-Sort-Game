using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// One leaderboard row: the rank in the circle on the left, the avatar in the ring, then the name.
    /// The row art differs for the player's own row.
    /// </summary>
    public sealed class LeaderboardRow : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _rankLabel;
        [Tooltip("The avatar picture, under a circular mask inside the row's ring.")]
        [SerializeField] private Image _picture;
        [SerializeField] private TMP_Text _nameLabel;

        /// <param name="rank">1 for the top row.</param>
        /// <param name="background">The row art: the player's own or everyone else's.</param>
        public void Show(int rank, string playerName, Sprite avatar, Sprite background)
        {
            _rankLabel.text = rank.ToString(CultureInfo.InvariantCulture);
            _nameLabel.text = playerName;
            _picture.sprite = avatar;
            _picture.enabled = avatar != null;
            _background.sprite = background;
            FitBackground(background);
        }

        // The two row arts differ in shape (the player's drips hang below the bar), so the background
        // spans the row's width, hangs from its top and takes its height from the art.
        private void FitBackground(Sprite background)
        {
            if (background == null)
            {
                return;
            }

            RectTransform rect = _background.rectTransform;
            float width = ((RectTransform)transform).rect.width;
            float height = width * background.rect.height / background.rect.width;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        }

        private void Awake()
        {
            if (_background == null || _rankLabel == null || _picture == null || _nameLabel == null)
            {
                Debug.LogError($"{name}: background, rank, picture and name must be assigned.", this);
            }
        }
    }
}
