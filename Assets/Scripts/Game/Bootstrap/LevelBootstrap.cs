using System;
using BallSort.Core.Levels;
using BallSort.Core.Session;
using BallSort.Game.Flow;
using BallSort.Game.Services;
using UnityEngine;

namespace BallSort.Game.Bootstrap
{
    /// <summary>
    /// Level scene composition root: loads the player's current level, starts a
    /// <see cref="LevelSession"/> for it, and hands both to the <see cref="LevelFlow"/>. A level that
    /// fails to load sends the player back to Home with a toast instead of an empty board.
    /// </summary>
    /// <remarks>
    /// In the Editor, Ball Sort ▸ Play Level… can choose for one visit either another level of the
    /// run (winning it is a replay unless it is the current level: the reward is paid, progress
    /// stays) or a level file outside the run, such as a test level (its win pays nothing and moves
    /// no progress).
    /// </remarks>
    public sealed class LevelBootstrap : MonoBehaviour
    {
#if UNITY_EDITOR
        /// <summary>
        /// SessionState key the editor level jump writes a level number to. Read once, by the next
        /// Level scene to start, then erased.
        /// </summary>
        public const string EditorLevelJumpKey = "BallSort.EditorLevelJump";

        /// <summary>
        /// SessionState key the editor level jump writes a level file's asset path to. Read once, like
        /// <see cref="EditorLevelJumpKey"/>, and takes precedence over it.
        /// </summary>
        public const string EditorLevelFileKey = "BallSort.EditorLevelFile";
#endif

        [SerializeField] private LevelFlow _flow;

        [Tooltip("Toast shown on Home when a level file cannot be loaded; {0} is the level number or file name.")]
        [SerializeField] private string _loadFailedFormat = "Level {0} could not be loaded";

        private AppRoot _app;

        private void Start()
        {
            _app = AppRoot.Instance;
            if (_app == null || _flow == null)
            {
                Debug.LogError($"{name}: needs a running AppRoot and an assigned level flow.", this);
                return;
            }

            LevelVisit visit = ChooseVisit(out TextAsset editorFile);
            if (!TryLoad(visit, editorFile, out LoadedLevel level))
            {
                _app.SceneLoader.LoadHome(string.Format(_loadFailedFormat, visit.Label));
                return;
            }

            var session = new LevelSession(level, _app.PlayerData.Boosters, _app.Economy.AddedTubeCapacity);
            _flow.Begin(session, visit, _app.PlayerData, _app.Economy, _app.SceneLoader);
        }

        /// <param name="editorFile">The level file to play when the visit is from a file; otherwise null.</param>
        private bool TryLoad(LevelVisit visit, TextAsset editorFile, out LoadedLevel level)
        {
            try
            {
                TextAsset file = visit.IsFromFile ? editorFile : _app.Levels.GetLevel(visit.LevelNumber);
                level = new LevelLoader().Load(file);
                return true;
            }
            catch (LevelFormatException e)
            {
                Debug.LogError(e.Message, this);
            }
            catch (ArgumentException e)
            {
                // A missing database slot or level file.
                Debug.LogError($"Level {visit.Label}: {e.Message}", this);
            }

            level = null;
            return false;
        }

        /// <summary>The player's current level, unless an editor tool chose something else for this visit.</summary>
        /// <param name="editorFile">The chosen level file (null if it no longer exists), or null.</param>
        private LevelVisit ChooseVisit(out TextAsset editorFile)
        {
            editorFile = null;
            int levelNumber = _app.PlayerData.CurrentLevel;
#if UNITY_EDITOR
            // One visit only: Continue → Home → Level, or the next plain Play, goes back to the save.
            string filePath = UnityEditor.SessionState.GetString(EditorLevelFileKey, string.Empty);
            int jump = UnityEditor.SessionState.GetInt(EditorLevelJumpKey, 0);
            UnityEditor.SessionState.EraseString(EditorLevelFileKey);
            UnityEditor.SessionState.EraseInt(EditorLevelJumpKey);

            if (filePath.Length > 0)
            {
                editorFile = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                return LevelVisit.ForFile(System.IO.Path.GetFileNameWithoutExtension(filePath));
            }

            if (jump > 0)
            {
                levelNumber = jump;
            }
#endif
            return LevelVisit.ForLevel(Mathf.Clamp(levelNumber, 1, _app.Levels.Count));
        }
    }
}
