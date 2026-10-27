using System;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A modal panel over a dimmed, full-screen background that swallows taps, so nothing beneath
    /// it can be touched while it is open. Content-specific popups sit on the same GameObject and
    /// drive this one.
    /// </summary>
    /// <remarks>
    /// Open and closed is the GameObject's active state, so a closed popup costs nothing. Draw
    /// order is the hierarchy order: keep popups after the screen content and before the toast.
    /// </remarks>
    public sealed class Popup : MonoBehaviour
    {
        [Tooltip("Optional. Closes the popup.")]
        [SerializeField] private Button _closeButton;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Raised after the popup opens.</summary>
        public event Action Opened;

        /// <summary>Raised after the popup closes.</summary>
        public event Action Closed;

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            gameObject.SetActive(true);
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }
        }
    }
}
