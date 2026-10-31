using BallSort.Core.Player;
using BallSort.Game.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// A screen's button sounds (docs/design/SFX_GUIDE.md §7): every button and toggle under this
    /// object plays the UI click; Home's Play button plays its own sound when it starts the level and
    /// the click when it opens More Lives instead.
    /// </summary>
    /// <remarks>
    /// The buttons are collected once, when the scene's bootstrap initializes this, inactive popups
    /// included; the screens' own click handlers are not touched. A button whose result has its own
    /// sound goes in the silent list.
    /// </remarks>
    public sealed class UiButtonSounds : MonoBehaviour
    {
        [Tooltip("Home's Play button. Empty in the Level scene.")]
        [SerializeField] private Button _playButton;
        [Tooltip("Buttons that play no click, because what they do has its own sound.")]
        [SerializeField] private Button[] _silent = new Button[0];

        private AudioService _audio;
        private PlayerData _player;
        private Button[] _buttons = new Button[0];
        private Toggle[] _toggles = new Toggle[0];
        private UnityAction _onClick;
        private UnityAction _onPlayClicked;
        private UnityAction<bool> _onToggled;

        /// <summary>
        /// Starts playing the sounds. Call it before the screens add their own handlers, so the Play
        /// button's sound is chosen before the press spends anything.
        /// </summary>
        public void Initialize(AudioService audio, PlayerData player)
        {
            Unbind();
            _audio = audio;
            _player = player;
            _buttons = GetComponentsInChildren<Button>(true);
            _toggles = GetComponentsInChildren<Toggle>(true);

            foreach (Button button in _buttons)
            {
                if (button == _playButton)
                {
                    button.onClick.AddListener(_onPlayClicked);
                }
                else if (System.Array.IndexOf(_silent, button) < 0)
                {
                    button.onClick.AddListener(_onClick);
                }
            }

            foreach (Toggle toggle in _toggles)
            {
                toggle.onValueChanged.AddListener(_onToggled);
            }
        }

        private void OnClick() => _audio.Play(Sfx.UiButton);

        // At 0 lives the Play button opens More Lives, an ordinary button press.
        private void OnPlayClicked() => _audio.Play(_player.CanStartLevel() ? Sfx.UiPlay : Sfx.UiButton);

        private void OnToggled(bool isOn) => OnClick();

        private void Unbind()
        {
            foreach (Button button in _buttons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(_onClick);
                    button.onClick.RemoveListener(_onPlayClicked);
                }
            }

            foreach (Toggle toggle in _toggles)
            {
                if (toggle != null)
                {
                    toggle.onValueChanged.RemoveListener(_onToggled);
                }
            }
        }

        private void Awake()
        {
            _onClick = OnClick;
            _onPlayClicked = OnPlayClicked;
            _onToggled = OnToggled;
        }

        private void OnDestroy() => Unbind();
    }
}
