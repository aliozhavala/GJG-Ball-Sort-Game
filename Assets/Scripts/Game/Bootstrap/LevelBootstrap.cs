using BallSort.Core.Levels;
using BallSort.Core.Session;
using BallSort.Game.Services;
using BallSort.Game.UI;
using BallSort.Game.Views;
using UnityEngine;

namespace BallSort.Game.Bootstrap
{
    /// <summary>
    /// Level scene composition root: loads the player's current level, starts a
    /// <see cref="LevelSession"/> for it, and wires the session to the board view and HUD.
    /// </summary>
    public sealed class LevelBootstrap : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private LevelHud _hud;

        // TEMPORARY: replaced by the editor level jump (roadmap step 4). Ignored in player builds.
        [Tooltip("Editor only, temporary: play this level instead of the saved one. 0 = use the save.")]
        [SerializeField, Min(0)] private int _editorLevelOverride;

        private AppRoot _app;
        private LevelSession _session;

        private void Start()
        {
            _app = AppRoot.Instance;
            if (_app == null || _boardView == null || _hud == null)
            {
                Debug.LogError($"{name}: needs a running AppRoot and assigned board view and HUD.", this);
                return;
            }

            int levelNumber = ChooseLevel();
            LoadedLevel level;
            try
            {
                level = new LevelLoader().Load(_app.Levels.GetLevel(levelNumber));
            }
            catch (LevelFormatException e)
            {
                Debug.LogError(e.Message, this);
                return;
            }

            _session = new LevelSession(level, _app.PlayerData.Boosters, _app.Economy.AddedTubeCapacity);
            _session.Restarted += OnRestarted;
            _boardView.Build(_session);

            _hud.Initialize(levelNumber, _app.PlayerData);
            _hud.RestartRequested += _session.Restart;
            _hud.HomeRequested += _app.SceneLoader.LoadHome;
        }

        private int ChooseLevel()
        {
            int levelNumber = _app.PlayerData.CurrentLevel;
#if UNITY_EDITOR
            if (_editorLevelOverride > 0)
            {
                levelNumber = _editorLevelOverride;
            }
#endif
            return Mathf.Clamp(levelNumber, 1, _app.Levels.Count);
        }

        private void OnRestarted() => _boardView.Build(_session);

        private void OnDestroy()
        {
            if (_session == null)
            {
                return;
            }

            _session.Restarted -= OnRestarted;
            _hud.RestartRequested -= _session.Restart;
            _hud.HomeRequested -= _app.SceneLoader.LoadHome;
        }
    }
}
