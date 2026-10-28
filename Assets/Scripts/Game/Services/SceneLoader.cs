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

        private string _pendingNotice;

        /// <summary>True from a load request until the new scene is active.</summary>
        public bool IsLoading { get; private set; }

        public void LoadHome() => Load(HomeSceneIndex);

        /// <summary>
        /// Loads Home and leaves it <paramref name="notice"/> to show, e.g. why a level could not be played.
        /// </summary>
        public void LoadHome(string notice)
        {
            _pendingNotice = notice;
            Load(HomeSceneIndex);
        }

        /// <summary>Returns the notice left by <see cref="LoadHome(string)"/>, once; null when there is none.</summary>
        public string TakePendingNotice()
        {
            string notice = _pendingNotice;
            _pendingNotice = null;
            return notice;
        }

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
