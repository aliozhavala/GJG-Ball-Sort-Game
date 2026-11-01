using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// "Episode N unlocked" with the new episode's mascot frame, shown on Home after a win that moved
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
        [SerializeField] private Image _mascot;
        [SerializeField] private string _titleFormat = "Episode {0} unlocked";
        [Tooltip("Fireworks behind the title and picture; their big bursts land on the fanfare's beats.")]
        [SerializeField] private FireworksView _fireworks;

        public Popup Popup => _popup;

        /// <param name="episodeNumber">The new episode, 1-based.</param>
        /// <param name="mascotFrame">Its mascot frame; null hides the picture.</param>
        /// <param name="fireworkBeats">Seconds from now when the big fireworks burst (the fanfare's beats as heard).</param>
        public void Show(int episodeNumber, Sprite mascotFrame, IReadOnlyList<float> fireworkBeats)
        {
            _title.text = string.Format(_titleFormat, episodeNumber);
            _mascot.sprite = mascotFrame;
            _mascot.gameObject.SetActive(mascotFrame != null);
            _popup.Open();
            _fireworks.Play(fireworkBeats);
        }

        private void OnDismissClicked() => _popup.Close();

        private void Awake()
        {
            if (_popup == null || _dismissArea == null || _title == null || _mascot == null || _fireworks == null)
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
