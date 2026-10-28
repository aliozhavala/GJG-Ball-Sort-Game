using BallSort.Game.Services;
using UnityEditor;

namespace BallSort.Editor.Tools
{
    /// <summary>Default <see cref="LevelDatabase"/> for editor windows that have not been given one.</summary>
    internal static class LevelDatabaseFinder
    {
        /// <summary>The first level database in the project, or null if there is none.</summary>
        public static LevelDatabase FindFirst()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LevelDatabase)}");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<LevelDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
