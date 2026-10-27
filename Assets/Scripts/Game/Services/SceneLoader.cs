using UnityEngine;
using UnityEngine.SceneManagement;

namespace BallSort.Game.Services
{
    /// <summary>
    /// Switches between the game's two scenes by build index, ignoring requests while a load is
    /// already in flight (a double tap must not load the scene twice).
    /// </summary>
    public sealed class SceneLoader
    {
        /// <summary>Build index of the Home scene.</summary>
        public const int HomeSceneIndex = 0;

        /// <summary>Build index of the Level scene.</summary>
        public const int LevelSceneIndex = 1;

        /// <summary>True from a load request until the new scene is active.</summary>
        public bool IsLoading { get; private set; }

        public void LoadHome() => Load(HomeSceneIndex);

        public void LoadLevel() => Load(LevelSceneIndex);

        private void Load(int buildIndex)
        {
            if (IsLoading)
            {
                return;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex);
            if (operation == null)
            {
                Debug.LogError($"Scene with build index {buildIndex} is not in the build settings.");
                return;
            }

            IsLoading = true;
            operation.completed += _ => IsLoading = false;
        }
    }
}
