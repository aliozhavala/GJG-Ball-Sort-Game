using BallSort.Core.Levels;
using BallSort.Game.Flow;
using BallSort.Game.Services;
using BallSort.Game.UI;
using UnityEngine;

namespace BallSort.Game.Bootstrap
{
    /// <summary>
    /// Home scene composition root: hands the persistent services, the current level's tier and what the
    /// Level scene left for Home (<see cref="HomeArrival"/>) to the <see cref="HomeFlow"/>.
    /// </summary>
    public sealed class HomeBootstrap : MonoBehaviour
    {
        [SerializeField] private HomeFlow _flow;
        [SerializeField] private UiButtonSounds _buttonSounds;

        private void Start()
        {
            AppRoot app = AppRoot.Instance;
            if (app == null || _flow == null || _buttonSounds == null)
            {
                Debug.LogError($"{name}: needs a running AppRoot and an assigned Home flow and button sounds.", this);
                return;
            }

            _buttonSounds.Initialize(app.Audio, app.PlayerData);

            _flow.Begin(app.PlayerData, app.SceneLoader, app.Episodes, ReadCurrentTier(app), app.SceneLoader.TakePendingArrival());
        }

        private static LevelTier ReadCurrentTier(AppRoot app)
        {
            int level = Mathf.Clamp(app.PlayerData.CurrentLevel, 1, app.Levels.Count);
            return new LevelLoader().ReadTier(app.Levels.GetLevel(level));
        }
    }
}
