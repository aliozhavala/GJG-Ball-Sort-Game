using BallSort.Core.Player;
using BallSort.Game.Services;
using UnityEngine;

namespace BallSort.Game.Bootstrap
{
    /// <summary>
    /// The persistent composition root: owns the player's data, saving and scene loading for the
    /// whole session, across scene changes.
    /// </summary>
    /// <remarks>
    /// The one agreed singleton-style exception (see CLAUDE.md). It is created from a prefab in
    /// <c>Resources/</c> before the first scene loads, so pressing Play in any scene works. Scene
    /// bootstraps read their services from <see cref="Instance"/> once, in <c>Awake</c>, and pass
    /// them on explicitly; nothing else should touch <see cref="Instance"/>.
    /// </remarks>
    public sealed class AppRoot : MonoBehaviour
    {
        /// <summary>Path of the prefab under <c>Resources/</c>.</summary>
        private const string ResourcePath = "AppRoot";

        [SerializeField] private EconomyConfig _economy;
        [SerializeField] private LevelDatabase _levelDatabase;
        [SerializeField] private EpisodeConfig _episodes;

        private SaveService _saveService;

        /// <summary>The live instance; null only before launch or after a failed launch.</summary>
        public static AppRoot Instance { get; private set; }

        public PlayerData PlayerData { get; private set; }

        public SceneLoader SceneLoader { get; private set; }

        public EconomyConfig Economy => _economy;

        public LevelDatabase Levels => _levelDatabase;

        /// <summary>The run's episodes; both scenes show the current one's background.</summary>
        public EpisodeConfig Episodes => _episodes;

        // With domain reload disabled, statics survive between Play sessions; clear them explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnLaunch()
        {
            if (Instance != null)
            {
                return;
            }

            var prefab = Resources.Load<AppRoot>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"AppRoot prefab not found at Resources/{ResourcePath}.");
                return;
            }

            AppRoot root = Instantiate(prefab);
            root.name = prefab.name;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (_economy == null || _levelDatabase == null || _episodes == null)
            {
                Debug.LogError($"{name}: economy config, level database and episode config must be assigned.", this);
                enabled = false;
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;

            _saveService = new SaveService(_economy, _levelDatabase.Count, new SystemClock());
            PlayerData = _saveService.Load();
            PlayerData.Changed += Save;

            // A level still marked in progress was cut off by closing the app: settle it (a life and a
            // retry) before any scene reads the player.
            PlayerData.ResolveInterruptedAttempt();
            SceneLoader = new SceneLoader();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
            }
        }

        private void OnApplicationQuit() => Save();

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            PlayerData.Changed -= Save;
            Instance = null;
        }

        private void Save()
        {
            if (PlayerData != null)
            {
                _saveService.Save(PlayerData);
            }
        }
    }
}
