using System;
using BallSort.Core.Levels;
using BallSort.Game.Flow;
using BallSort.Game.Services;
using BallSort.Game.UI;
using BallSort.Game.Views;
using UnityEngine;

namespace BallSort.Game.Bootstrap
{
    /// <summary>
    /// Level scene composition root: loads the player's current level, shows its episode's
    /// background and hands the level to the <see cref="LevelFlow"/>, which begins the first attempt. A
    /// level that fails to load sends the player back to Home with a toast instead of an empty board,
    /// costing no life.
    /// </summary>
    /// <remarks>
    /// In the Editor, Ball Sort ▸ Play Level… can choose for one visit any level of the run or a level
    /// file outside it, such as a test level, in any variant. Such a visit is a test: it plays the
    /// chosen variant on every attempt and records no life, retry, reward, progress or tutorial seen.
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

        /// <summary>
        /// SessionState key the editor level jump writes the chosen variant (1-based) to. Read once, with
        /// <see cref="EditorLevelJumpKey"/> or <see cref="EditorLevelFileKey"/>; V1 when absent.
        /// </summary>
        public const string EditorLevelVariantKey = "BallSort.EditorLevelVariant";
#endif

        [SerializeField] private LevelFlow _flow;
        [SerializeField] private BackgroundSprite _background;
        [SerializeField] private TutorialConfig _tutorials;
        [SerializeField] private UiButtonSounds _buttonSounds;

        [Tooltip("Toast shown on Home when a level file cannot be loaded; {0} is the level number or file name.")]
        [SerializeField] private string _loadFailedFormat = "Level {0} could not be loaded";

        private AppRoot _app;

        private void Start()
        {
            _app = AppRoot.Instance;
            if (_app == null || _flow == null || _background == null || _tutorials == null || _buttonSounds == null)
            {
                Debug.LogError($"{name}: needs a running AppRoot, an assigned level flow, background, tutorial config and button sounds.", this);
                return;
            }

            _buttonSounds.Initialize(_app.Audio, _app.PlayerData);

            LevelVisit visit = ChooseVisit(out TextAsset editorFile);
            _background.Show(_app.Episodes.GetEpisodeForLevel(visit.LevelNumber).LevelBackground);
            if (!TryLoad(visit, editorFile, out LoadedLevel level))
            {
                _app.SceneLoader.LoadHome(HomeArrival.WithNotice(string.Format(_loadFailedFormat, visit.Label)));
                return;
            }

            _flow.Begin(level, visit, _app.PlayerData, _app.Economy, _app.Episodes, _tutorials, _app.SceneLoader, _app.Audio);
        }

        /// <param name="editorFile">The level file to play when the visit is from a file; otherwise null.</param>
        private bool TryLoad(LevelVisit visit, TextAsset editorFile, out LoadedLevel level)
        {
            try
            {
                TextAsset file = visit.LevelNumber == 0 ? editorFile : _app.Levels.GetLevel(visit.LevelNumber);
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

        /// <summary>The player's current level, unless an editor tool chose a test visit instead.</summary>
        /// <param name="editorFile">The chosen level file (null if it no longer exists), or null.</param>
        private LevelVisit ChooseVisit(out TextAsset editorFile)
        {
            editorFile = null;
#if UNITY_EDITOR
            // One visit only: Continue → Home → Level, or the next plain Play, goes back to the save.
            string filePath = UnityEditor.SessionState.GetString(EditorLevelFileKey, string.Empty);
            int jump = UnityEditor.SessionState.GetInt(EditorLevelJumpKey, 0);
            int variant = Mathf.Clamp(UnityEditor.SessionState.GetInt(EditorLevelVariantKey, 1), 1, VariantSelector.VariantCount);
            UnityEditor.SessionState.EraseString(EditorLevelFileKey);
            UnityEditor.SessionState.EraseInt(EditorLevelJumpKey);
            UnityEditor.SessionState.EraseInt(EditorLevelVariantKey);

            if (filePath.Length > 0)
            {
                editorFile = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                return LevelVisit.ForEditorFile(System.IO.Path.GetFileNameWithoutExtension(filePath), variant);
            }

            if (jump > 0)
            {
                return LevelVisit.ForEditorLevel(Mathf.Clamp(jump, 1, _app.Levels.Count), variant);
            }
#endif
            return LevelVisit.ForLevel(Mathf.Clamp(_app.PlayerData.CurrentLevel, 1, _app.Levels.Count));
        }
    }
}
