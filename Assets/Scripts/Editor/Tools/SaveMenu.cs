using BallSort.Game.Services;
using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Tools
{
    /// <summary>Editor menu for wiping the player's save while testing.</summary>
    internal static class SaveMenu
    {
        private const string ResetPath = "Ball Sort/Reset Save";

        [MenuItem(ResetPath)]
        private static void ResetSave()
        {
            SaveService.Delete();
            Debug.Log("Ball Sort save deleted. The next Play starts a new player.");
        }

        // In Play mode the live AppRoot would write its in-memory data straight back on the next
        // change or on exit, so the reset would not stick.
        [MenuItem(ResetPath, true)]
        private static bool CanResetSave() => !EditorApplication.isPlaying;
    }
}
