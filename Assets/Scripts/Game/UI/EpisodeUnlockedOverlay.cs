using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// "Episode N Unlocked!" with the new episode's name and picture, shown on Home after a win that moved
    /// the player into the next episode, with fireworks behind it. A tap anywhere closes it (and clears
    /// the fireworks).
    /// </summary>
    [RequireComponent(typeof(Popup))]
    public sealed class EpisodeUnlockedOverlay : MonoBehaviour
    {
        [SerializeField] private Popup _popup;

        [Tooltip("Full-screen button on the overlay's root: a tap anywhere closes it.")]
        [SerializeField] private Button _dismissArea;

        [SerializeField] private TMP_Text _title;
        [Tooltip("The new episode's picture (Episode.UnlockedPicture).")]
        [FormerlySerializedAs("_mascot")]
        [SerializeField] private Image _picture;
        [SerializeField] private string _titleFormat = "Episode {0} Unlocked!";
        [Tooltip("The episode's name under the title; hidden when it has none.")]
        [SerializeField] private TMP_Text _episodeName;
        [Tooltip("Fireworks behind the title and picture; their big bursts land on the fanfare's beats.")]
        [SerializeField] private FireworksView _fireworks;

        public Popup Popup => _popup;

        /// <param name="episodeNumber">The new episode, 1-based.</param>
        /// <param name="episodeName">Its name; empty hides the line.</param>
        /// <param name="picture">Its picture; null hides it.</param>
        /// <param name="fireworkBeats">Seconds from now when the big fireworks burst (the fanfare's beats as heard).</param>
        public void Show(int episodeNumber, string episodeName, Sprite picture, IReadOnlyList<float> fireworkBeats)
        {
            _title.text = string.Format(_titleFormat, episodeNumber);
            _episodeName.text = episodeName;
            _episodeName.gameObject.SetActive(!string.IsNullOrEmpty(episodeName));
            _picture.sprite = picture;
            _picture.gameObject.SetActive(picture != null);
            _popup.Open();
            _fireworks.Play(fireworkBeats);
        }

        private void OnDismissClicked() => _popup.Close();

        private void Awake()
        {
            if (_popup == null || _dismissArea == null || _title == null || _picture == null || _fireworks == null || _episodeName == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
                return;
            }

            _dismissArea.onClick.AddListener(OnDismissClicked);
        }

        private void OnDestroy()
        {
            if (_dismissArea != null)
            {
                _dismissArea.onClick.RemoveListener(OnDismissClicked);
            }
        }

        private void OnValidate()
        {
            if (_popup == null)
            {
                _popup = GetComponent<Popup>();
            }
        }
    }
}
