using BallSort.Core.Levels;
using BallSort.Game.Flow;
using BallSort.Game.Services;
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

        private void Start()
        {
            AppRoot app = AppRoot.Instance;
            if (app == null || _flow == null)
            {
                Debug.LogError($"{name}: needs a running AppRoot and an assigned Home flow.", this);
                return;
            }

            _flow.Begin(app.PlayerData, app.SceneLoader, app.Episodes, ReadCurrentTier(app), app.SceneLoader.TakePendingArrival());
        }

        private static LevelTier ReadCurrentTier(AppRoot app)
        {
            int level = Mathf.Clamp(app.PlayerData.CurrentLevel, 1, app.Levels.Count);
            return new LevelLoader().ReadTier(app.Levels.GetLevel(level));
        }
    }
}
