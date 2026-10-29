using System;
using BallSort.Core.Levels;
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
    /// in a chosen variant, for testing and tuning. The Validate Levels window uses the same <see cref="Play"/>.
    /// </summary>
    /// <remarks>
    /// The choice is stored in <see cref="SessionState"/>, which survives the domain reload of
    /// entering Play mode, and <see cref="LevelBootstrap"/> reads it once. Lives in the Editor
    /// assembly, so it never ships. Every jump is a test visit: it plays the chosen variant on every
    /// try (Restart included), and winning pays no reward, moves no progress and counts no try.
    /// </remarks>
    public sealed class PlayLevelWindow : EditorWindow
    {
        [SerializeField] private LevelDatabase _database;
        [SerializeField] private int _levelNumber = 1;
        [SerializeField] private TextAsset _levelFile;
        [SerializeField] private int _variant = 1;

        [MenuItem("Ball Sort/Play Level…")]
        public static void Open() => GetWindow<PlayLevelWindow>("Play Level");

        /// <summary>
        /// Offers to save modified scenes, opens the Level scene and enters Play mode on
        /// <paramref name="levelNumber"/> in <paramref name="variant"/>, as a test visit. Call it outside
        /// OnGUI (e.g. through <see cref="EditorApplication.delayCall"/>): it may open a dialog and change scenes.
        /// </summary>
        /// <param name="levelNumber">1-based number in the level database.</param>
        /// <param name="variant">1 to <see cref="VariantSelector.VariantCount"/>.</param>
        /// <returns>False when already in Play mode, the save prompt was cancelled, or the scene is missing.</returns>
        public static bool Play(int levelNumber, int variant) =>
            EnterLevelScene(variant, () => SessionState.SetInt(LevelBootstrap.EditorLevelJumpKey, levelNumber));

        /// <summary>
        /// Like <see cref="Play"/>, for a level file outside the run.
        /// </summary>
        /// <returns>False when <paramref name="levelFile"/> is not a project asset, or as for <see cref="Play"/>.</returns>
        public static bool PlayFile(TextAsset levelFile, int variant)
        {
            string path = AssetDatabase.GetAssetPath(levelFile);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Play Level: pick a level file from the project.");
                return false;
            }

            return EnterLevelScene(variant, () => SessionState.SetString(LevelBootstrap.EditorLevelFileKey, path));
        }

        /// <param name="storeChoice">Writes this visit's level or file to <see cref="SessionState"/>.</param>
        private static bool EnterLevelScene(int variant, Action storeChoice)
        {
            if (!VariantSelector.IsVariant(variant))
            {
                Debug.LogError($"Play Level: variant {variant} does not exist; levels have variants 1 to {VariantSelector.VariantCount}.");
                return false;
            }

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
            SessionState.SetInt(LevelBootstrap.EditorLevelVariantKey, variant);
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

            _variant = Mathf.Clamp(_variant, 1, VariantSelector.VariantCount);
            _variant = EditorGUILayout.IntSlider("Variant", _variant, 1, VariantSelector.VariantCount);
            EditorGUILayout.HelpBox(
                "A test visit: it plays this variant on every try, Restart included. Winning pays no reward, " +
                "moves no progress and counts no try.",
                MessageType.None);

            EditorGUILayout.Space();
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
                if (GUILayout.Button($"Play level {_levelNumber}, V{_variant}"))
                {
                    int levelNumber = _levelNumber;
                    int variant = _variant;
                    EditorApplication.delayCall += () => Play(levelNumber, variant);
                }
            }
        }

        private void DrawLevelFile(bool canPlay)
        {
            _levelFile = (TextAsset)EditorGUILayout.ObjectField("Level file", _levelFile, typeof(TextAsset), false);

            using (new EditorGUI.DisabledScope(!canPlay || _levelFile == null))
            {
                if (GUILayout.Button(_levelFile != null ? $"Play {_levelFile.name}, V{_variant}" : "Play file"))
                {
                    TextAsset levelFile = _levelFile;
                    int variant = _variant;
                    EditorApplication.delayCall += () => PlayFile(levelFile, variant);
                }
            }

            EditorGUILayout.HelpBox(
                $"Keep test and draft levels in {LevelDatabase.TestLevelsFolder}/; the level database must never list them.",
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
