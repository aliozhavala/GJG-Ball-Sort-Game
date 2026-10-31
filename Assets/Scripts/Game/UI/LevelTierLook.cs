using BallSort.Core.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Home Level button's look for the level's tier: the regular button, or the tier's own art
    /// (a red button with a tilted tag at its top right) with the reward multiplier written on the tag.
    /// The label's outline and shadow follow the art (a dark green or a dark red material preset).
    /// </summary>
    /// <remarks>
    /// The tier art reaches past the button (the tag sticks out at the top right), so it is a separate
    /// image laid over the button rather than a sprite swap on it: the button keeps its rect, label
    /// and tap area, and only its regular art is hidden.
    /// </remarks>
    public sealed class LevelTierLook : MonoBehaviour
    {
        [Tooltip("The regular button art; stays the tap target, hidden (transparent) for hard tiers.")]
        [SerializeField] private Image _background;

        [Tooltip("Drawn over the button for hard and super hard levels; its pill lines up with the button.")]
        [SerializeField] private Image _tierArt;

        [Tooltip("On the tier art's tag, rotated to its angle.")]
        [SerializeField] private TMP_Text _multiplierLabel;

        [Tooltip("The button's label (\"Level N\"); its outline follows the art under it.")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("The label's material preset on the regular (green) art.")]
        [SerializeField] private Material _regularLabelMaterial;

        [Tooltip("The label's material preset on the tier (red) art.")]
        [SerializeField] private Material _tierLabelMaterial;

        [Header("Hard")]
        [SerializeField] private Sprite _hardSprite;
        [SerializeField] private string _hardMultiplier = "x3";

        [Header("Super hard")]
        [SerializeField] private Sprite _superHardSprite;
        [SerializeField] private string _superHardMultiplier = "x5";

        public void Show(LevelTier tier)
        {
            Sprite art = tier switch
            {
                LevelTier.Hard => _hardSprite,
                LevelTier.SuperHard => _superHardSprite,
                _ => null,
            };

            bool special = art != null;
            _tierArt.sprite = art;
            _tierArt.gameObject.SetActive(special);
            _multiplierLabel.text = tier == LevelTier.SuperHard ? _superHardMultiplier : _hardMultiplier;
            _label.fontSharedMaterial = special ? _tierLabelMaterial : _regularLabelMaterial;

            // Transparent, not disabled: the regular art stays the button's raycast target.
            Color color = _background.color;
            color.a = special ? 0f : 1f;
            _background.color = color;
        }

        private void Awake()
        {
            if (_background == null || _tierArt == null || _multiplierLabel == null || _label == null ||
                _regularLabelMaterial == null || _tierLabelMaterial == null)
            {
                Debug.LogError($"{name}: background, tier art, both labels and both label materials must be assigned.", this);
            }

            if (_hardSprite == null || _superHardSprite == null)
            {
                Debug.LogWarning($"{name}: a tier without art shows the regular button.", this);
            }
        }
    }
}
