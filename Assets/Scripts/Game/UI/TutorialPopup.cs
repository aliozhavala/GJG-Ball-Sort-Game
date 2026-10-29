using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A tutorial level's popup: its text and an optional image. A tap anywhere dismisses it; until then
    /// it covers the screen, so nothing else can be touched.
    /// </summary>
    [RequireComponent(typeof(Popup))]
    public sealed class TutorialPopup : MonoBehaviour
    {
        [SerializeField] private Popup _popup;

        [Tooltip("Full-screen button on the popup's root: a tap anywhere, the panel included, dismisses it.")]
        [SerializeField] private Button _dismissArea;

        [SerializeField] private TMP_Text _text;

        [Tooltip("Shows the entry's image; hidden when the entry has none.")]
        [SerializeField] private Image _image;

        /// <summary>The player dismissed the popup. It has already closed.</summary>
        public event Action Dismissed;

        public Popup Popup => _popup;

        /// <param name="image">Optional; null hides the image.</param>
        public void Show(string text, Sprite image)
        {
            _text.text = text;
            _image.sprite = image;
            _image.gameObject.SetActive(image != null);
            _popup.Open();
        }

        private void OnDismissClicked()
        {
            _popup.Close();
            Dismissed?.Invoke();
        }

        private void Awake()
        {
            if (_popup == null || _dismissArea == null || _text == null || _image == null)
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
