using System;
using System.Collections;
using BallSort.Core.Levels;
using BallSort.Core.Player;
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
    /// switches to the new episode behind it. The overlay then blocks input until tapped.
    /// </remarks>
    public sealed class HomeFlow : MonoBehaviour
    {
        [SerializeField] private HomeScreen _homeScreen;
        [SerializeField] private BackgroundImage _background;

        [Tooltip("Seconds after arriving on Home before the Episode Unlocked overlay appears.")]
        [SerializeField, Min(0f)] private float _episodeUnlockedDelay = 0.7f;

        private bool _begun;

        /// <summary>Sets Home up and plays <paramref name="arrival"/>. Call once, from the scene's bootstrap.</summary>
        /// <param name="levelTier">Tier of the level the Level button plays.</param>
        public void Begin(
            PlayerData player, SceneLoader sceneLoader, EpisodeConfig episodes, LevelTier levelTier, HomeArrival arrival)
        {
            if (_begun)
            {
                throw new InvalidOperationException("The Home flow has already begun.");
            }

            if (player == null || sceneLoader == null || episodes == null)
            {
                throw new ArgumentNullException(player == null ? nameof(player) : sceneLoader == null ? nameof(sceneLoader) : nameof(episodes));
            }

            _begun = true;
            _homeScreen.Initialize(player, sceneLoader, levelTier);

            int unlocked = arrival.UnlockedEpisode;
            if (arrival.HasUnlockedEpisode && unlocked > 0 && unlocked < episodes.Count)
            {
                ShowEpisode(episodes.GetEpisode(unlocked - 1));
                StartCoroutine(ShowEpisodeUnlocked(unlocked, episodes.GetEpisode(unlocked)));
            }
            else
            {
                ShowEpisode(episodes.GetEpisodeForLevel(player.CurrentLevel));
            }

            if (!string.IsNullOrEmpty(arrival.Notice))
            {
                _homeScreen.ShowToast(arrival.Notice);
            }
        }

        private IEnumerator ShowEpisodeUnlocked(int episodeIndex, Episode episode)
        {
            _homeScreen.SetInputBlocked(true);
            yield return new WaitForSeconds(_episodeUnlockedDelay);
            ShowEpisode(episode);
            _homeScreen.EpisodeUnlocked.Show(episodeIndex + 1, episode.MascotFrame);
            _homeScreen.SetInputBlocked(false);
        }

        /// <summary>Home wears <paramref name="episode"/>: its background, and its mascot frame as the avatar.</summary>
        private void ShowEpisode(Episode episode)
        {
            _background.Show(episode.HomeBackground);
            _homeScreen.ShowAvatar(episode.MascotFrame);
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
