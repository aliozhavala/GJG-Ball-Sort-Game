using System;
using BallSort.Core.Player;
using TMPro;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The avatar menu, opened from Home's avatar: the selected avatar in its frame, the player's name
    /// and a pen to edit it in place, and a grid of frames to pick an avatar from.
    /// </summary>
    /// <remarks>
    /// The pen makes the name editable where it is; Enter, or a tap anywhere else, ends the edit and
    /// saves it under the name rules (<see cref="PlayerNameRules"/>; a refused name goes back to the
    /// saved one). The first <see cref="AvatarRules.Count"/> frames hold the avatars and pick one at a
    /// tap, saved at once, the picked one in the selected frame; the rest are empty frames that cannot be
    /// picked. Close (✕) closes.
    /// </remarks>
    [RequireComponent(typeof(Popup))]
    public sealed class AvatarMenuPopup : MonoBehaviour
    {
        [SerializeField] private Popup _popup;
        [Tooltip("The selected avatar, top left.")]
        [SerializeField] private AvatarFrame _selected;
        [Tooltip("The name, read-only until the pen is tapped.")]
        [SerializeField] private TMP_InputField _nameField;
        [Tooltip("On the name field: blocks its taps while it is not being edited, so only the pen starts an edit.")]
        [SerializeField] private CanvasGroup _nameTaps;
        [SerializeField] private UnityEngine.UI.Button _penButton;
        [Tooltip("The grid of frames, in reading order; the first ones hold the avatars.")]
        [SerializeField] private AvatarFrame[] _slots = new AvatarFrame[0];

        private PlayerData _player;
        private AvatarConfig _avatars;
        private int _editEndedFrame = -1;

        public Popup Popup => _popup;

        public void Initialize(PlayerData player, AvatarConfig avatars)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _avatars = avatars != null ? avatars : throw new ArgumentNullException(nameof(avatars));

            _nameField.characterLimit = PlayerNameRules.MaxLength;
            _nameField.lineType = TMP_InputField.LineType.SingleLine;
            _nameField.onEndEdit.AddListener(OnNameEditEnded);
            _penButton.onClick.AddListener(OnPenClicked);
            SetEditing(false);

            for (int i = 0; i < _slots.Length; i++)
            {
                int avatarId = i + 1;
                bool hasAvatar = AvatarRules.IsValid(avatarId);
                _slots[i].Show(hasAvatar ? avatars.GetPicture(avatarId) : null);
                if (_slots[i].Button != null)
                {
                    _slots[i].Button.interactable = hasAvatar;
                    if (hasAvatar)
                    {
                        _slots[i].Button.onClick.AddListener(() => _player.TrySetAvatar(avatarId));
                    }
                }
            }

        }

        public void Open()
        {
            if (_nameField.isFocused)
            {
                _nameField.DeactivateInputField();
            }

            SetEditing(false);
            Refresh();
            _popup.Open();
        }

        private void Refresh()
        {
            _selected.Show(_avatars.GetPicture(_player.AvatarId));
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].ShowFrame(i + 1 == _player.AvatarId ? _avatars.SelectedFrame : _avatars.Frame);
            }

            if (!_nameField.isFocused)
            {
                _nameField.SetTextWithoutNotify(_player.PlayerName);
            }
        }

        private void OnPenClicked()
        {
            // A tap on the pen while editing first ends the edit (the field loses focus); it must
            // not open a new one in the same tap.
            if (_editEndedFrame == Time.frameCount)
            {
                return;
            }

            SetEditing(true);
            _nameField.Select();
            _nameField.ActivateInputField();
            _nameField.MoveTextEnd(false);
        }

        // Raised on Enter and when the field loses focus (a tap elsewhere).
        private void OnNameEditEnded(string name)
        {
            // Shows the saved name: trimmed when accepted, the old one when refused.
            _player.TrySetPlayerName(name);
            _nameField.SetTextWithoutNotify(_player.PlayerName);
            _editEndedFrame = Time.frameCount;
            SetEditing(false);
        }

        // Read-only and deaf to taps, so only the pen starts an edit. Runs inside onEndEdit, which a tap
        // elsewhere raises while the EventSystem is changing the selection: nothing here may select or
        // deselect (making the field non-interactable would, and the EventSystem refuses a nested change).
        private void SetEditing(bool editing)
        {
            _nameField.readOnly = !editing;
            _nameTaps.blocksRaycasts = editing;
        }

        private void Awake()
        {
            if (_popup == null || _selected == null || _nameField == null || _nameTaps == null || _penButton == null ||
                _slots.Length == 0)
            {
                Debug.LogError($"{name}: popup, selected frame, name field and its canvas group, pen and slots must be assigned.", this);
            }

            if (_slots.Length < AvatarRules.Count)
            {
                Debug.LogError($"{name}: {_slots.Length} frames cannot hold {AvatarRules.Count} avatars.", this);
            }
        }

        // Follows the player only while open. A popup that is never opened never runs OnDestroy, so a
        // subscription made at Initialize would outlive the scene (PlayerData does) and throw later.
        private void OnEnable()
        {
            if (_player != null)
            {
                _player.Changed += Refresh;
            }
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.Changed -= Refresh;
            }
        }

        private void OnDestroy()
        {
            if (_nameField != null)
            {
                _nameField.onEndEdit.RemoveListener(OnNameEditEnded);
            }

            if (_penButton != null)
            {
                _penButton.onClick.RemoveListener(OnPenClicked);
            }

            foreach (AvatarFrame slot in _slots)
            {
                if (slot != null && slot.Button != null)
                {
                    slot.Button.onClick.RemoveAllListeners();
                }
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
