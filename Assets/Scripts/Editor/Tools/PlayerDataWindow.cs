using System;
using BallSort.Core.Player;
using BallSort.Game.Bootstrap;
using BallSort.Game.Services;
using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Tools
{
    /// <summary>
    /// Testing tool: sets the player's coins, lives (and the wait for the next one), booster counts,
    /// name and avatar, moves progress to any level, and forgets the tutorials seen. Outside Play mode it edits the
    /// save on disk; in Play mode it edits the live player, so the HUD updates at once and the change is
    /// saved as usual.
    /// </summary>
    internal sealed class PlayerDataWindow : EditorWindow
    {
        private int _coins;
        private int _lives;
        private int _maxLives;
        private float _minutesToNextLife;
        private string _playerName = string.Empty;
        private int _avatar = AvatarRules.Default;
        private string _status = string.Empty;
        private int _level = 1;
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
            _lives = EditorGUILayout.IntSlider($"Lives (max {_maxLives})", _lives, 0, Mathf.Max(1, _maxLives));
            using (new EditorGUI.DisabledScope(_lives >= _maxLives))
            {
                _minutesToNextLife = Mathf.Max(0.05f, EditorGUILayout.FloatField("Next life in (minutes)", _minutesToNextLife));
            }

            _playerName = EditorGUILayout.TextField("Player name", _playerName);
            _avatar = EditorGUILayout.IntSlider("Avatar", _avatar, 1, AvatarRules.Count);
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_status, EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _level = Mathf.Max(1, EditorGUILayout.IntField("Current level", _level));
                if (GUILayout.Button("Jump", GUILayout.Width(60f)))
                {
                    JumpToLevel();
                }
            }

            if (GUILayout.Button("Clear tutorials seen"))
            {
                ClearTutorialsSeen();
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
            _lives = player.LifeCount;
            _maxLives = player.MaxLives;
            TimeSpan? untilNext = player.GetTimeUntilNextLife();
            _minutesToNextLife = untilNext.HasValue ? (float)untilNext.Value.TotalMinutes : 30f;
            _playerName = player.PlayerName;
            _avatar = player.AvatarId;
            _level = player.CurrentLevel;
            _status = $"Level {player.CurrentLevel}, retries {player.RetryCount}, last variant V{player.LastVariant}, " +
                $"level in progress {(player.LevelInProgress == 0 ? "none" : player.LevelInProgress.ToString())}, " +
                $"tutorials seen {Describe(player.ToSaveData().TutorialsSeen)}.";
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
            player.SetLives(_lives, TimeSpan.FromMinutes(_minutesToNextLife));
            if (!player.TrySetPlayerName(_playerName))
            {
                Debug.LogWarning($"Player Data: \"{_playerName}\" is not a valid name (1–{PlayerNameRules.MaxLength} characters); the name was kept.");
            }

            player.TrySetAvatar(_avatar);
            SetBoosters(player.Boosters, BoosterType.Undo, _undo);
            SetBoosters(player.Boosters, BoosterType.Shuffle, _shuffle);
            SetBoosters(player.Boosters, BoosterType.AddTube, _addTube);

            // The live player saves itself on every change; an offline copy must be written back.
            offlineSave?.Save(player);
            Debug.Log($"Player data set: {_coins} coins, {_lives} lives, boosters {_undo}/{_shuffle}/{_addTube}.");
            Reload();
        }

        // Its own button, so Apply never resets the retries; e.g. jump to level 20 to win it and see Episode Unlocked.
        private void JumpToLevel()
        {
            PlayerData player = GetPlayer(out SaveService offlineSave);
            if (player == null)
            {
                return;
            }

            try
            {
                player.SetCurrentLevel(_level);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Debug.LogWarning($"Player Data: {e.Message}");
                return;
            }

            offlineSave?.Save(player);
            Debug.Log($"Player data: current level set to {_level} (retries and the level in progress cleared).");
            Reload();
        }

        private void ClearTutorialsSeen()
        {
            PlayerData player = GetPlayer(out SaveService offlineSave);
            if (player == null)
            {
                return;
            }

            player.ClearTutorialsSeen();
            offlineSave?.Save(player);
            Debug.Log("Player data: every tutorial will show again.");
            Reload();
        }

        private static string Describe(int[] ids) => ids.Length == 0 ? "none" : string.Join(", ", ids);

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

            offlineSave = new SaveService(prefab.Economy, prefab.Levels.Count, new SystemClock());
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
