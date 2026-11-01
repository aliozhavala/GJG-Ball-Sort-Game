using System;
using System.Collections;
using BallSort.Core.Levels;
using BallSort.Core.Player;
using BallSort.Game.Audio;
using BallSort.Game.Services;
using BallSort.Game.UI;
using UnityEngine;

namespace BallSort.Game.Flow
{
    /// <summary>
    /// Runs one visit to Home: shows the current episode's background, sets up the Home screen, and
    /// plays what the Level scene handed over (<see cref="HomeArrival"/>): a notice, or the Episode
    /// Unlocked sequence.
    /// </summary>
    /// <remarks>
    /// Episode Unlocked (docs/screens.md, Episodes): Home opens on the previous episode's background with
    /// input blocked; after <see cref="_episodeUnlockedDelay"/> the overlay appears and the background
    /// switches to the new episode behind it, with the win fanfare, a Win haptic and fireworks on the
    /// fanfare's beats, and the reward jingle <see cref="_episodeJingleDelay"/> later (SFX guide 6.2). The
    /// overlay then blocks input until tapped.
    /// </remarks>
    public sealed class HomeFlow : MonoBehaviour
    {
        [SerializeField] private HomeScreen _homeScreen;
        [SerializeField] private BackgroundImage _background;

        [Tooltip("Seconds after arriving on Home before the Episode Unlocked overlay appears.")]
        [SerializeField, Min(0f)] private float _episodeUnlockedDelay = 0.7f;
        [Tooltip("Seconds after the Episode Unlocked overlay appears before the reward jingle (not if it was closed by then).")]
        [SerializeField, Min(0f)] private float _episodeJingleDelay = 0.6f;

        private bool _begun;

        /// <summary>Sets Home up and plays <paramref name="arrival"/>. Call once, from the scene's bootstrap.</summary>
        /// <param name="levelTier">Tier of the level the Level button plays.</param>
        /// <param name="economy">Prices on Home: the lives refill and the coin bundles.</param>
        /// <param name="audio">Plays Home's sounds.</param>
        /// <param name="haptics">Plays the haptics paired with them.</param>
        public void Begin(
            PlayerData player, SceneLoader sceneLoader, EpisodeConfig episodes, LevelTier levelTier, HomeArrival arrival,
            EconomyConfig economy, AudioService audio, HapticsService haptics)
        {
            if (_begun)
            {
                throw new InvalidOperationException("The Home flow has already begun.");
            }

            if (player == null || sceneLoader == null || episodes == null || economy == null || audio == null ||
                haptics == null)
            {
                throw new ArgumentNullException(
                    player == null ? nameof(player) : sceneLoader == null ? nameof(sceneLoader)
                    : episodes == null ? nameof(episodes) : economy == null ? nameof(economy)
                    : audio == null ? nameof(audio) : nameof(haptics));
            }

            _begun = true;
            _homeScreen.Initialize(player, sceneLoader, levelTier, economy, audio, haptics);

            int unlocked = arrival.UnlockedEpisode;
            if (arrival.HasUnlockedEpisode && unlocked > 0 && unlocked < episodes.Count)
            {
                _background.Show(episodes.GetEpisode(unlocked - 1).HomeBackground);
                StartCoroutine(ShowEpisodeUnlocked(unlocked, episodes.GetEpisode(unlocked), audio, haptics));
            }
            else
            {
                _background.Show(episodes.GetEpisodeForLevel(player.CurrentLevel).HomeBackground);
            }

            if (!string.IsNullOrEmpty(arrival.Notice))
            {
                _homeScreen.ShowToast(arrival.Notice);
            }
        }

        private IEnumerator ShowEpisodeUnlocked(int episodeIndex, Episode episode, AudioService audio, HapticsService haptics)
        {
            _homeScreen.SetInputBlocked(true);
            yield return new WaitForSeconds(_episodeUnlockedDelay);
            _background.Show(episode.HomeBackground);
            EpisodeUnlockedOverlay overlay = _homeScreen.EpisodeUnlocked;
            overlay.Show(episodeIndex + 1, episode.Name, episode.UnlockedPicture, audio.Config.Get(Sfx.WinFanfare).HeardBeats());
            audio.Play(Sfx.WinFanfare);
            haptics.Win();
            _homeScreen.SetInputBlocked(false);
            yield return new WaitForSeconds(_episodeJingleDelay);
            if (overlay.Popup.IsOpen)
            {
                audio.Play(Sfx.RewardJingle);
                haptics.Success();
            }
        }

        private void Awake()
        {
            if (_homeScreen == null || _background == null)
            {
                Debug.LogError($"{name}: home screen and background must be assigned.", this);
                enabled = false;
            }
        }
    }
}
