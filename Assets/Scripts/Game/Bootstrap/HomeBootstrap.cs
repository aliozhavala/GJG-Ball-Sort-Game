using BallSort.Game.UI;
using UnityEngine;

namespace BallSort.Game.Bootstrap
{
    /// <summary>Home scene composition root: hands the persistent services to the Home screen.</summary>
    public sealed class HomeBootstrap : MonoBehaviour
    {
        [SerializeField] private HomeScreen _homeScreen;

        private void Start()
        {
            AppRoot app = AppRoot.Instance;
            if (app == null || _homeScreen == null)
            {
                Debug.LogError($"{name}: needs a running AppRoot and an assigned home screen.", this);
                return;
            }

            _homeScreen.Initialize(app.PlayerData, app.SceneLoader);
        }
    }
}
