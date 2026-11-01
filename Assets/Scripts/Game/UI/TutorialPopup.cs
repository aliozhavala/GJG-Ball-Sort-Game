using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A tutorial level's popup. A mechanic's first level shows the New Item card (docs/screens.md,
    /// Tutorials): its name, "New Item Unlocked!", its picture in a turning light, then the main line and
    /// the hint on a cream card. Level 1 shows only the card with its line. A tap anywhere dismisses it;
    /// until then it covers the screen, so nothing else can be touched.
    /// </summary>
    [RequireComponent(typeof(Popup))]
    public sealed class TutorialPopup : MonoBehaviour
    {
        [SerializeField] private Popup _popup;

        [Tooltip("Full-screen button on the popup's root: a tap anywhere, the panel included, dismisses it.")]
        [SerializeField] private Button _dismissArea;

        [Tooltip("The main line, on the cream card.")]
        [SerializeField] private TMP_Text _text;

        [Tooltip("The hint under the main line; hidden when the entry has none.")]
        [SerializeField] private TMP_Text _hint;

        [Header("New Item card (hidden without a name)")]
        [Tooltip("The mechanic's name, the card's title.")]
        [SerializeField] private TMP_Text _name;

        [Tooltip("\"New Item Unlocked!\" under the name.")]
        [SerializeField] private GameObject _newItemLabel;

        [Tooltip("The picture and the light behind it; hidden when the entry has no picture.")]
        [SerializeField] private GameObject _pictureArea;

        [Tooltip("Shows the entry's image.")]
        [SerializeField] private Image _image;

        [Tooltip("The name and the picture pop in (OutBack) this long after the popup opens, the picture a little later.")]
        [SerializeField, Min(0.01f)] private float _popDuration = 0.45f;
        [SerializeField, Min(0f)] private float _pictureDelay = 0.15f;

        private Sequence _entrance;

        /// <summary>The player dismissed the popup. It has already closed.</summary>
        public event Action Dismissed;

        public Popup Popup => _popup;

        /// <param name="itemName">The mechanic's name; empty shows the plain popup (no title, no picture).</param>
        /// <param name="text">The main line.</param>
        /// <param name="hint">The hint line; empty for none.</param>
        /// <param name="image">Optional; null hides the picture.</param>
        public void Show(string itemName, string text, string hint, Sprite image)
        {
            bool card = !string.IsNullOrEmpty(itemName);
            _name.text = itemName;
            _name.gameObject.SetActive(card);
            _newItemLabel.SetActive(card);
            _text.text = text;
            _hint.text = hint;
            _hint.gameObject.SetActive(!string.IsNullOrEmpty(hint));
            _image.sprite = image;
            _pictureArea.SetActive(card && image != null);
            _popup.Open();
            PlayEntrance(card);
        }

        private void PlayEntrance(bool card)
        {
            KillEntrance();
            if (!card)
            {
                return;
            }

            Transform title = _name.transform;
            Transform picture = _image.transform;
            title.localScale = Vector3.zero;
            picture.localScale = Vector3.zero;
            _entrance = DOTween.Sequence()
                .Insert(0f, title.DOScale(1f, _popDuration).SetEase(Ease.OutBack))
                .Insert(_pictureDelay, picture.DOScale(1f, _popDuration).SetEase(Ease.OutBack, 1.8f))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillEntrance()
        {
            if (_entrance != null && _entrance.IsActive())
            {
                _entrance.Kill(complete: true);
            }

            _entrance = null;
        }

        private void OnDismissClicked()
        {
            _popup.Close();
            Dismissed?.Invoke();
        }

        private void Awake()
        {
            if (_popup == null || _dismissArea == null || _text == null || _hint == null || _name == null ||
                _newItemLabel == null || _pictureArea == null || _image == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
                return;
            }

            _dismissArea.onClick.AddListener(OnDismissClicked);
        }

        private void OnDisable() => KillEntrance();

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
