using BallSort.Core.Levels;
using BallSort.Core.Session;
using BallSort.Game.Flow;
using BallSort.Game.Services;
using UnityEngine;

namespace BallSort.Game.Bootstrap
{
    /// <summary>
    /// Level scene composition root: loads the player's current level, starts a
    /// <see cref="LevelSession"/> for it, and hands both to the <see cref="LevelFlow"/>.
    /// </summary>
    public sealed class LevelBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelFlow _flow;

        // TEMPORARY: replaced by the editor level jump (roadmap step 4). Ignored in player builds.
        [Tooltip("Editor only, temporary: play this level instead of the saved one. 0 = use the save.")]
        [SerializeField, Min(0)] private int _editorLevelOverride;

        private AppRoot _app;

        private void Start()
        {
            _app = AppRoot.Instance;
            if (_app == null || _flow == null)
            {
                Debug.LogError($"{name}: needs a running AppRoot and an assigned level flow.", this);
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

            var session = new LevelSession(level, _app.PlayerData.Boosters, _app.Economy.AddedTubeCapacity);
            _flow.Begin(session, levelNumber, _app.PlayerData, _app.Economy, _app.SceneLoader);
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
    }
}
