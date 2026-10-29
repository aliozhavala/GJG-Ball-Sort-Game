using BallSort.Core.Levels;
using BallSort.Game.Services;
using BallSort.Game.UI;
using UnityEngine;

namespace BallSort.Game.Bootstrap
{
    /// <summary>
    /// Home scene composition root: shows the current episode's background and hands the persistent
    /// services, and the current level's tier, to the Home screen.
    /// </summary>
    public sealed class HomeBootstrap : MonoBehaviour
    {
        [SerializeField] private HomeScreen _homeScreen;
        [SerializeField] private BackgroundImage _background;

        private void Start()
        {
            AppRoot app = AppRoot.Instance;
            if (app == null || _homeScreen == null || _background == null)
            {
                Debug.LogError($"{name}: needs a running AppRoot, an assigned home screen and a background.", this);
                return;
            }

            PlayerData player = app.PlayerData;
            _background.Show(app.Episodes.GetEpisodeForLevel(player.CurrentLevel).HomeBackground);
            _homeScreen.Initialize(player, app.SceneLoader, ReadCurrentTier(app));

            string notice = app.SceneLoader.TakePendingNotice();
            if (!string.IsNullOrEmpty(notice))
            {
                _homeScreen.ShowToast(notice);
            }
        }

        private static LevelTier ReadCurrentTier(AppRoot app)
        {
            int level = Mathf.Clamp(app.PlayerData.CurrentLevel, 1, app.Levels.Count);
            return new LevelLoader().ReadTier(app.Levels.GetLevel(level));
        }
    }
}
