using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// "Are you sure?" before leaving a level unwon (settings → Home, settings → Restart, stuck → Give
    /// up), with the life it costs. Confirm closes it and raises <see cref="Confirmed"/>; Close (the
    /// popup's close button) goes back to whatever is beneath it, with nothing spent.
    /// </summary>
    /// <remarks>It knows nothing about the exit asked about: the level flow remembers that.</remarks>
    [RequireComponent(typeof(Popup))]
    public sealed class AreYouSurePopup : MonoBehaviour
    {
        [SerializeField] private Popup _popup;

        [Tooltip("\"You will lose a life.\" Hidden when no life would be spent (at 0 lives).")]
        [SerializeField] private TMP_Text _lifeCostLine;

        [Tooltip("The broken heart above the life line; shown and hidden with it.")]
        [SerializeField] private GameObject _lifeCostIcon;

        [SerializeField] private Button _confirmButton;

        /// <summary>The player confirmed. The popup has already closed.</summary>
        public event Action Confirmed;

        public Popup Popup => _popup;

        /// <param name="costsALife">Whether confirming spends a life; false hides the life line.</param>
        public void Ask(bool costsALife)
        {
            _lifeCostLine.gameObject.SetActive(costsALife);
            if (_lifeCostIcon != null)
            {
                _lifeCostIcon.SetActive(costsALife);
            }
            _popup.Open();
        }

        private void OnConfirmClicked()
        {
            _popup.Close();
            Confirmed?.Invoke();
        }

        private void Awake()
        {
            if (_popup == null || _lifeCostLine == null || _confirmButton == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
                return;
            }

            _confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(OnConfirmClicked);
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
