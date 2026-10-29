using System;
using BallSort.Core.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Edit Profile, opened from the avatar: the player's name, prefilled. Save stores it (trimmed,
    /// 1–16 characters, so Save is disabled while the field is empty) and closes; Close discards the edit.
    /// </summary>
    [RequireComponent(typeof(Popup))]
    public sealed class EditProfilePopup : MonoBehaviour
    {
        [SerializeField] private Popup _popup;
        [SerializeField] private TMP_InputField _nameField;
        [SerializeField] private Button _saveButton;

        private PlayerData _player;

        public Popup Popup => _popup;

        public void Initialize(PlayerData player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _nameField.characterLimit = PlayerNameRules.MaxLength;
            _nameField.onValueChanged.AddListener(OnNameEdited);
            _saveButton.onClick.AddListener(OnSaveClicked);
        }

        public void Open()
        {
            _nameField.SetTextWithoutNotify(_player.PlayerName);
            OnNameEdited(_nameField.text);
            _popup.Open();
        }

        private void OnNameEdited(string name) => _saveButton.interactable = PlayerNameRules.Validate(name) != null;

        private void OnSaveClicked()
        {
            if (_player.TrySetPlayerName(_nameField.text))
            {
                _popup.Close();
            }
        }

        private void Awake()
        {
            if (_popup == null || _nameField == null || _saveButton == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
            }
        }

        private void OnDestroy()
        {
            if (_nameField != null)
            {
                _nameField.onValueChanged.RemoveListener(OnNameEdited);
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveListener(OnSaveClicked);
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
