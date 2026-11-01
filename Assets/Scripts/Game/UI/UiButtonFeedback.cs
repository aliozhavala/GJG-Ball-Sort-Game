using System.Collections.Generic;
using BallSort.Game.Audio;
using BallSort.Game.Services;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The press feedback of every button and settings toggle under this canvas: the button sound and a
    /// light haptic (docs/design/SFX_GUIDE.md §7).
    /// </summary>
    /// <remarks>
    /// One per canvas root, initialised by the screen once its content is built: it gathers the
    /// buttons below it then, popups (inactive) included. A disabled button never clicks, so it stays
    /// silent. Exceptions:
    /// <list type="bullet">
    /// <item>Buttons in <see cref="_ownFeedback"/> get nothing here: their owner plays the sound of what
    /// the press did instead (Home's Level button, Buy, Are You Sure's Confirm).</item>
    /// <item>Popups' close buttons and those in <see cref="_noHaptic"/> click without a haptic.</item>
    /// <item>A toggle plays after its setting is saved, so turning Sound off is silent and turning
    /// Haptics on is felt.</item>
    /// </list>
    /// </remarks>
    public sealed class UiButtonFeedback : MonoBehaviour
    {
        [Tooltip("Buttons whose owner plays the sound of what the press did, instead of the button sound.")]
        [SerializeField] private Button[] _ownFeedback = new Button[0];

        [Tooltip("Buttons that click without a haptic (the SFX guide: edit name). Popups' close buttons never have one.")]
        [SerializeField] private Button[] _noHaptic = new Button[0];

        private readonly List<Button> _buttons = new List<Button>();
        private readonly List<UnityAction> _listeners = new List<UnityAction>();
        private readonly List<SettingToggle> _toggles = new List<SettingToggle>();
        private AudioService _audio;
        private HapticsService _haptics;

        /// <summary>Gives every button and settings toggle under this canvas its press feedback.</summary>
        public void Initialize(AudioService audio, HapticsService haptics)
        {
            Unbind();
            _audio = audio;
            _haptics = haptics;

            var silent = new HashSet<Button>(_ownFeedback);
            var noHaptic = new HashSet<Button>(_noHaptic);
            foreach (Popup popup in GetComponentsInChildren<Popup>(true))
            {
                if (popup.CloseButton != null)
                {
                    noHaptic.Add(popup.CloseButton);
                }
            }

            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (silent.Contains(button))
                {
                    continue;
                }

                UnityAction listener = noHaptic.Contains(button) ? PlayClick : PlayPress;
                button.onClick.AddListener(listener);
                _buttons.Add(button);
                _listeners.Add(listener);
            }

            GetComponentsInChildren(true, _toggles);
            foreach (SettingToggle toggle in _toggles)
            {
                toggle.Toggled += PlayPress;
            }
        }

        /// <summary>The button sound and a light haptic.</summary>
        private void PlayPress()
        {
            _audio.Play(Sfx.UiButton);
            _haptics.Light();
        }

        /// <summary>The button sound alone.</summary>
        private void PlayClick() => _audio.Play(Sfx.UiButton);

        private void Unbind()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null)
                {
                    _buttons[i].onClick.RemoveListener(_listeners[i]);
                }
            }

            foreach (SettingToggle toggle in _toggles)
            {
                if (toggle != null)
                {
                    toggle.Toggled -= PlayPress;
                }
            }

            _buttons.Clear();
            _listeners.Clear();
            _toggles.Clear();
        }

        private void OnDestroy() => Unbind();
    }
}
