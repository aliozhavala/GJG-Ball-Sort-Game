using BallSort.Core.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Home Level button's look for the level's tier: a tint, and a badge naming hard and super
    /// hard levels. Placeholder until final art (docs/screens.md).
    /// </summary>
    public sealed class LevelTierLook : MonoBehaviour
    {
        [SerializeField] private Image _background;

        [Tooltip("Shown for hard and super hard levels only.")]
        [SerializeField] private GameObject _badge;
        [SerializeField] private TMP_Text _badgeLabel;

        [Header("Regular")]
        [SerializeField] private Color _regularTint = Color.white;

        [Header("Hard")]
        [SerializeField] private Color _hardTint = new Color(1f, 0.6f, 0.15f);
        [SerializeField] private string _hardLabel = "Hard";

        [Header("Super hard")]
        [SerializeField] private Color _superHardTint = new Color(0.85f, 0.2f, 0.4f);
        [SerializeField] private string _superHardLabel = "Super hard";

        public void Show(LevelTier tier)
        {
            _background.color = tier switch
            {
                LevelTier.Hard => _hardTint,
                LevelTier.SuperHard => _superHardTint,
                _ => _regularTint,
            };

            string label = tier switch
            {
                LevelTier.Hard => _hardLabel,
                LevelTier.SuperHard => _superHardLabel,
                _ => string.Empty,
            };

            _badgeLabel.text = label;
            _badge.SetActive(label.Length > 0);
        }

        private void Awake()
        {
            if (_background == null || _badge == null || _badgeLabel == null)
            {
                Debug.LogError($"{name}: background image, badge and badge label must be assigned.", this);
            }
        }
    }
}
