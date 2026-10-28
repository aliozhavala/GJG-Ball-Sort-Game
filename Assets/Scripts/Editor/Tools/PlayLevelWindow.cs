using System;
using BallSort.Game.Bootstrap;
using BallSort.Game.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BallSort.Editor.Tools
{
    /// <summary>
    /// Ball Sort ▸ Play Level…: the editor level jump. Plays any level of a <see cref="LevelDatabase"/>,
    /// or a level file outside the run (test and draft levels in <see cref="LevelDatabase.TestLevelsFolder"/>),
    /// for testing and tuning. The Validate Levels window uses the same <see cref="Play"/>.
    /// </summary>
    /// <remarks>
    /// The choice is stored in <see cref="SessionState"/>, which survives the domain reload of
    /// entering Play mode, and <see cref="LevelBootstrap"/> reads it once. Lives in the Editor
    /// assembly, so it never ships. Winning a database level other than the current one is a replay:
    /// the reward is paid, progress does not move. Winning a level file pays nothing and moves nothing.
    /// </remarks>
    public sealed class PlayLevelWindow : EditorWindow
    {
        [SerializeField] private LevelDatabase _database;
        [SerializeField] private int _levelNumber = 1;
        [SerializeField] private TextAsset _levelFile;

        [MenuItem("Ball Sort/Play Level…")]
        public static void Open() => GetWindow<PlayLevelWindow>("Play Level");

        /// <summary>
        /// Offers to save modified scenes, opens the Level scene and enters Play mode on
        /// <paramref name="levelNumber"/>. Call it outside OnGUI (e.g. through
        /// <see cref="EditorApplication.delayCall"/>): it may open a dialog and change scenes.
        /// </summary>
        /// <returns>False when already in Play mode, the save prompt was cancelled, or the scene is missing.</returns>
        public static bool Play(int levelNumber) =>
            EnterLevelScene(() => SessionState.SetInt(LevelBootstrap.EditorLevelJumpKey, levelNumber));

        /// <summary>
        /// Like <see cref="Play"/>, for a level file outside the run. Its win pays no reward and moves
        /// no progress.
        /// </summary>
        /// <returns>False when <paramref name="levelFile"/> is not a project asset, or as for <see cref="Play"/>.</returns>
        public static bool PlayFile(TextAsset levelFile)
        {
            string path = AssetDatabase.GetAssetPath(levelFile);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Play Level: pick a level file from the project.");
                return false;
            }

            return EnterLevelScene(() => SessionState.SetString(LevelBootstrap.EditorLevelFileKey, path));
        }

        /// <param name="storeChoice">Writes this visit's choice to <see cref="SessionState"/>.</param>
        private static bool EnterLevelScene(Action storeChoice)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Play Level: leave Play mode first.");
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            string scenePath = SceneUtility.GetScenePathByBuildIndex(SceneLoader.LevelSceneIndex);
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError($"Play Level: no scene at build index {SceneLoader.LevelSceneIndex}.");
                return false;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // A choice left over from an attempt that never reached Play mode must not win over this one.
            SessionState.EraseInt(LevelBootstrap.EditorLevelJumpKey);
            SessionState.EraseString(LevelBootstrap.EditorLevelFileKey);
            storeChoice();
            EditorApplication.EnterPlaymode();
            return true;
        }

        private void OnEnable()
        {
            if (_database == null)
            {
                _database = LevelDatabaseFinder.FindFirst();
            }

            if (_levelFile == null)
            {
                _levelFile = FindFirstTestLevel();
            }

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        private void OnPlayModeChanged(PlayModeStateChange _) => Repaint();

        private void OnGUI()
        {
            bool canPlay = !EditorApplication.isPlayingOrWillChangePlaymode;

            EditorGUILayout.LabelField("Level of the run", EditorStyles.boldLabel);
            DrawDatabaseLevel(canPlay);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level file (test or draft, outside the run)", EditorStyles.boldLabel);
            DrawLevelFile(canPlay);
        }

        private void DrawDatabaseLevel(bool canPlay)
        {
            _database = (LevelDatabase)EditorGUILayout.ObjectField("Level database", _database, typeof(LevelDatabase), false);
            if (_database == null || _database.Count == 0)
            {
                EditorGUILayout.HelpBox("Pick a level database with at least one level.", MessageType.Info);
                return;
            }

            _levelNumber = Mathf.Clamp(_levelNumber, 1, _database.Count);
            _levelNumber = EditorGUILayout.IntSlider("Level", _levelNumber, 1, _database.Count);
            TextAsset file = _database.GetLevel(_levelNumber);
            EditorGUILayout.LabelField("File", file != null ? file.name : "(empty slot)");

            using (new EditorGUI.DisabledScope(!canPlay || file == null))
            {
                if (GUILayout.Button($"Play level {_levelNumber}"))
                {
                    int levelNumber = _levelNumber;
                    EditorApplication.delayCall += () => Play(levelNumber);
                }
            }

            EditorGUILayout.HelpBox(
                "Winning a level other than the current one pays the reward but does not move progress.",
                MessageType.None);
        }

        private void DrawLevelFile(bool canPlay)
        {
            _levelFile = (TextAsset)EditorGUILayout.ObjectField("Level file", _levelFile, typeof(TextAsset), false);

            using (new EditorGUI.DisabledScope(!canPlay || _levelFile == null))
            {
                if (GUILayout.Button(_levelFile != null ? $"Play {_levelFile.name}" : "Play file"))
                {
                    TextAsset levelFile = _levelFile;
                    EditorApplication.delayCall += () => PlayFile(levelFile);
                }
            }

            EditorGUILayout.HelpBox(
                $"Winning a level file pays no reward and does not move progress. Keep test and draft levels in " +
                $"{LevelDatabase.TestLevelsFolder}/; the level database must never list them.",
                MessageType.None);
        }

        private static TextAsset FindFirstTestLevel()
        {
            if (!AssetDatabase.IsValidFolder(LevelDatabase.TestLevelsFolder))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { LevelDatabase.TestLevelsFolder });
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
