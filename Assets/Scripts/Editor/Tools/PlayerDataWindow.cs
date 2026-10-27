using BallSort.Core.Player;
using BallSort.Game.Bootstrap;
using BallSort.Game.Services;
using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Tools
{
    /// <summary>
    /// Testing tool: sets the player's coins, lives and booster counts. Outside Play mode it edits
    /// the save on disk; in Play mode it edits the live player, so the HUD updates at once and the
    /// change is saved as usual.
    /// </summary>
    internal sealed class PlayerDataWindow : EditorWindow
    {
        private int _coins;
        private int _lives;
        private int _undo;
        private int _shuffle;
        private int _addTube;

        [MenuItem("Ball Sort/Player Data...")]
        private static void Open() => GetWindow<PlayerDataWindow>("Player Data");

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Reload();
        }

        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode || change == PlayModeStateChange.EnteredEditMode)
            {
                Reload();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                IsLive
                    ? "Play mode: editing the live player. Changes show and save immediately."
                    : "Edit mode: editing the save on disk. Changes apply on the next Play.",
                MessageType.Info);

            _coins = Mathf.Max(0, EditorGUILayout.IntField("Coins", _coins));
            _lives = Mathf.Max(0, EditorGUILayout.IntField("Lives", _lives));
            EditorGUILayout.Space();
            _undo = Mathf.Max(0, EditorGUILayout.IntField("Undo", _undo));
            _shuffle = Mathf.Max(0, EditorGUILayout.IntField("Shuffle", _shuffle));
            _addTube = Mathf.Max(0, EditorGUILayout.IntField("Add tube", _addTube));
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply"))
                {
                    Apply();
                }

                if (GUILayout.Button("Reload"))
                {
                    Reload();
                }
            }
        }

        // An editor tool may read the live AppRoot directly; game code receives it from its bootstrap.
        private static bool IsLive => EditorApplication.isPlaying && AppRoot.Instance != null;

        private void Reload()
        {
            PlayerData player = GetPlayer(out _);
            if (player == null)
            {
                return;
            }

            _coins = player.Wallet.Coins;
            _lives = player.Lives;
            _undo = player.Boosters.GetCount(BoosterType.Undo);
            _shuffle = player.Boosters.GetCount(BoosterType.Shuffle);
            _addTube = player.Boosters.GetCount(BoosterType.AddTube);
            Repaint();
        }

        private void Apply()
        {
            PlayerData player = GetPlayer(out SaveService offlineSave);
            if (player == null)
            {
                return;
            }

            SetCoins(player.Wallet, _coins);
            player.SetLives(_lives);
            SetBoosters(player.Boosters, BoosterType.Undo, _undo);
            SetBoosters(player.Boosters, BoosterType.Shuffle, _shuffle);
            SetBoosters(player.Boosters, BoosterType.AddTube, _addTube);

            // The live player saves itself on every change; an offline copy must be written back.
            offlineSave?.Save(player);
            Debug.Log($"Player data set: {_coins} coins, {_lives} lives, boosters {_undo}/{_shuffle}/{_addTube}.");
        }

        /// <summary>The live player in Play mode, otherwise a copy loaded from the save.</summary>
        /// <param name="offlineSave">The service to write the copy back with; null for the live player.</param>
        private static PlayerData GetPlayer(out SaveService offlineSave)
        {
            offlineSave = null;
            if (IsLive)
            {
                return AppRoot.Instance.PlayerData;
            }

            var prefab = Resources.Load<AppRoot>("AppRoot");
            if (prefab == null || prefab.Economy == null || prefab.Levels == null)
            {
                Debug.LogError("Player Data: the AppRoot prefab, its economy config and level database are needed.");
                return null;
            }

            offlineSave = new SaveService(prefab.Economy, prefab.Levels.Count);
            return offlineSave.Load();
        }

        private static void SetCoins(Wallet wallet, int target)
        {
            if (target > wallet.Coins)
            {
                wallet.Add(target - wallet.Coins);
            }
            else
            {
                wallet.TrySpend(wallet.Coins - target);
            }
        }

        private static void SetBoosters(BoosterInventory boosters, BoosterType type, int target)
        {
            int count = boosters.GetCount(type);
            if (target > count)
            {
                boosters.Add(type, target - count);
            }

            for (; count > target; count--)
            {
                boosters.TrySpend(type);
            }
        }
    }
}
