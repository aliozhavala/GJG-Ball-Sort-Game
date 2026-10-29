using System;
using System.Collections.Generic;

namespace BallSort.Core.Player
{
    /// <summary>
    /// Everything that persists about the player: progress and attempts, coins, lives, boosters, name,
    /// tutorials seen and settings.
    /// </summary>
    /// <remarks>
    /// <see cref="Changed"/> fires after any change, including ones made directly on
    /// <see cref="Wallet"/> or <see cref="Boosters"/>, so the save service needs only one subscription.
    /// An operation that changes several values (a win, leaving a level) raises it once, after all of
    /// them, so they are saved together.
    /// <para>
    /// Attempts follow docs/screens.md (Lives) and docs/levels.md (Variants and retries): entering a
    /// level begins an attempt and marks it in progress; every exit before a win spends a life and
    /// counts a retry; a win spends nothing; an attempt still marked in progress at launch was cut off
    /// by closing the app and is settled like an exit.
    /// </para>
    /// </remarks>
    public sealed class PlayerData
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly bool[] _settings = new bool[3];
        private readonly LevelProgress _progress;
        private readonly Lives _lives;
        private readonly IClock _clock;

        // Sorted, so the save lists them in a stable order.
        private readonly SortedSet<int> _tutorialsSeen;

        // Set while one operation changes several values, so they are saved once, together.
        private bool _batchingChanges;

        /// <summary>The level the Level scene loads. Advances on win; stays on the last level once all are done.</summary>
        public int CurrentLevel => _progress.CurrentLevel;

        /// <summary>True once the last level has been won.</summary>
        public bool AllLevelsComplete => _progress.AllLevelsComplete;

        /// <summary>Attempts at the current level that cost a life since it was last won.</summary>
        public int RetryCount => _progress.RetryCount;

        /// <summary>The variant the latest attempt at the current level played, 1-based; 0 before the first.</summary>
        public int LastVariant => _progress.LastVariant;

        /// <summary>The level whose attempt has started and not ended; 0 for none.</summary>
        public int LevelInProgress => _progress.LevelInProgress;

        public Wallet Wallet { get; }

        public BoosterInventory Boosters { get; }

        /// <summary>Lives held, as of the latest refresh (see <see cref="RefreshLives"/>).</summary>
        public int LifeCount => _lives.Count;

        /// <summary>Most lives the player can hold.</summary>
        public int MaxLives => _lives.Max;

        /// <summary>The player's name, 1 to <see cref="PlayerNameRules.MaxLength"/> characters.</summary>
        public string PlayerName { get; private set; }

        /// <summary>Raised after any persistent value changes.</summary>
        public event Action Changed;

        /// <summary>Raised after <see cref="LifeCount"/> or the wait for the next life changes, with the life count.</summary>
        public event Action<int> LivesChanged;

        /// <summary>Raised after a setting changes, with the setting and its new value.</summary>
        public event Action<SettingType, bool> SettingChanged;

        /// <param name="save">A parsed, sanitised save (the game reads it with <c>BallSort.SaveJson</c>).</param>
        /// <param name="levelCount">Levels in the game; the saved level must be within it.</param>
        /// <param name="rules">The lives maximum and refill interval.</param>
        /// <param name="clock">The current time, for the lives refill.</param>
        public PlayerData(SaveData save, int levelCount, PlayerRules rules, IClock clock)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _progress = new LevelProgress(
                save.CurrentLevel, save.AllLevelsComplete, levelCount, save.RetryCount, save.LastVariant, save.LevelInProgress);
            _lives = new Lives(
                save.Lives, FromUnixMs(save.LifeTimerStartUnixMs), rules.MaxLives, rules.LifeRefillInterval, clock.UtcNow);
            Wallet = new Wallet(save.Coins);
            Boosters = new BoosterInventory(save.UndoBoosters, save.ShuffleBoosters, save.AddTubeBoosters);
            PlayerName = PlayerNameRules.Validate(save.PlayerName) ?? PlayerNameRules.Default;
            _tutorialsSeen = new SortedSet<int>(save.TutorialsSeen ?? Array.Empty<int>());
            _settings[(int)SettingType.Sound] = save.SoundOn;
            _settings[(int)SettingType.Music] = save.MusicOn;
            _settings[(int)SettingType.Haptics] = save.HapticsOn;

            Wallet.Changed += _ => RaiseChanged();
            Boosters.Changed += (_, _) => RaiseChanged();
            _lives.Changed += OnLivesChanged;
        }

        // --- Attempts ------------------------------------------------------------------------------

        /// <summary>
        /// Begins an attempt at the current level, on entering it: marks it in progress and picks its
        /// variant, saved at once so that closing the app mid-level is noticed on the next launch.
        /// </summary>
        /// <param name="random">Picks the variant after the first five attempts.</param>
        /// <returns>The variant to play, 1-based (see <see cref="BallSort.Core.Levels.VariantSelector"/>).</returns>
        public int BeginAttempt(Random random)
        {
            int variant = _progress.StartAttempt(random);
            RaiseChanged();
            return variant;
        }

        /// <summary>
        /// Ends the attempt by leaving the level before a win (settings → Home, stuck → Give up): one
        /// life spent (none at 0), one retry counted, the mark cleared, all saved together.
        /// </summary>
        public void LeaveAttempt() =>
            ChangeTogether(() =>
            {
                _lives.LoseOne(_clock.UtcNow);
                _progress.RecordRetry();
                _progress.ClearLevelInProgress();
            });

        /// <summary>
        /// Ends the attempt and begins the next one at the same level (Restart): one life spent, one
        /// retry counted and the next variant picked; the level stays in progress. Refused, changing
        /// nothing, at 0 lives.
        /// </summary>
        /// <param name="random">Picks the variant after the first five attempts.</param>
        /// <param name="variant">The variant the new attempt plays; 0 when refused.</param>
        /// <returns>False at 0 lives.</returns>
        public bool TryRestartAttempt(Random random, out int variant)
        {
            variant = 0;
            if (!CanStartLevel())
            {
                return false;
            }

            int picked = 0;
            ChangeTogether(() =>
            {
                _lives.LoseOne(_clock.UtcNow);
                _progress.RecordRetry();
                picked = _progress.StartAttempt(random);
            });
            variant = picked;
            return true;
        }

        /// <summary>
        /// Call once on launch: an attempt still marked in progress was cut off by closing the app (or
        /// the OS ending it), so it costs a life and counts a retry, as leaving would, and the mark is cleared.
        /// </summary>
        /// <returns>True when an attempt was in progress.</returns>
        public bool ResolveInterruptedAttempt()
        {
            if (_progress.LevelInProgress == 0)
            {
                return false;
            }

            ChangeTogether(() =>
            {
                _lives.LoseOne(_clock.UtcNow);
                _progress.ResolveInterruptedAttempt();
            });
            return true;
        }

        /// <summary>
        /// Records a won level: ends the attempt (a win spends no life), advances progress and resets
        /// the retries (see <see cref="LevelProgress.RecordWin"/>) and grants <paramref name="reward"/>
        /// coins, then raises <see cref="Changed"/> once so all of it is saved together, immediately.
        /// </summary>
        /// <param name="wonLevel">The level that was just won, 1-based.</param>
        /// <param name="reward">Coins to grant; replays are rewarded too.</param>
        /// <returns>True when progress moved forward; false for a replay.</returns>
        public bool RecordWin(int wonLevel, int reward)
        {
            bool advanced = false;
            ChangeTogether(() =>
            {
                advanced = _progress.RecordWin(wonLevel);
                Wallet.Add(reward);
            });
            return advanced;
        }

        // --- Lives ---------------------------------------------------------------------------------

        /// <summary>Adds the lives that have come back since the last refresh (and saves them).</summary>
        public void RefreshLives() => _lives.Refresh(_clock.UtcNow);

        /// <summary>True when the player has a life to start a level with, counting those that have come back.</summary>
        public bool CanStartLevel()
        {
            RefreshLives();
            return _lives.Count > 0;
        }

        /// <summary>
        /// Time until the next life comes back; null at the maximum. Refreshes the lives first, so
        /// <see cref="LifeCount"/> matches.
        /// </summary>
        public TimeSpan? GetTimeUntilNextLife()
        {
            RefreshLives();
            return _lives.GetTimeUntilNext(_clock.UtcNow);
        }

        /// <summary>
        /// Sets the lives directly, for testing tools: <paramref name="lives"/> is clamped to
        /// 0..<see cref="MaxLives"/>, and below the maximum the next life comes back after
        /// <paramref name="timeUntilNextLife"/> (a full refill interval when null).
        /// </summary>
        public void SetLives(int lives, TimeSpan? timeUntilNextLife = null) =>
            _lives.Set(Math.Clamp(lives, 0, _lives.Max), timeUntilNextLife ?? _lives.RefillInterval, _clock.UtcNow);

        private void OnLivesChanged()
        {
            LivesChanged?.Invoke(_lives.Count);
            RaiseChanged();
        }

        // --- Name and tutorials --------------------------------------------------------------------

        /// <summary>Renames the player; see <see cref="PlayerNameRules"/>.</summary>
        /// <returns>False, changing nothing, for an empty or too long name.</returns>
        public bool TrySetPlayerName(string name)
        {
            string valid = PlayerNameRules.Validate(name);
            if (valid == null)
            {
                return false;
            }

            if (valid != PlayerName)
            {
                PlayerName = valid;
                RaiseChanged();
            }

            return true;
        }

        /// <summary>True once the tutorial popup of level <paramref name="levelId"/> has been dismissed.</summary>
        public bool HasSeenTutorial(int levelId) => _tutorialsSeen.Contains(levelId);

        /// <summary>Records that the tutorial popup of level <paramref name="levelId"/> has been dismissed.</summary>
        public void MarkTutorialSeen(int levelId)
        {
            if (levelId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelId), levelId, "Level ids start at 1.");
            }

            if (_tutorialsSeen.Add(levelId))
            {
                RaiseChanged();
            }
        }

        /// <summary>Forgets every tutorial seen, for testing tools, so each shows again.</summary>
        public void ClearTutorialsSeen()
        {
            if (_tutorialsSeen.Count == 0)
            {
                return;
            }

            _tutorialsSeen.Clear();
            RaiseChanged();
        }

        // --- Settings ------------------------------------------------------------------------------

        public bool GetSetting(SettingType setting) => _settings[(int)setting];

        public void SetSetting(SettingType setting, bool isOn)
        {
            if (_settings[(int)setting] == isOn)
            {
                return;
            }

            _settings[(int)setting] = isOn;
            SettingChanged?.Invoke(setting, isOn);
            RaiseChanged();
        }

        // --- Saving --------------------------------------------------------------------------------

        /// <summary>A snapshot of the current values, ready to write to disk.</summary>
        public SaveData ToSaveData()
        {
            var tutorialsSeen = new int[_tutorialsSeen.Count];
            _tutorialsSeen.CopyTo(tutorialsSeen);
            return new SaveData
            {
                Version = SaveData.CurrentVersion,
                CurrentLevel = CurrentLevel,
                AllLevelsComplete = AllLevelsComplete,
                Coins = Wallet.Coins,
                Lives = _lives.Count,
                LifeTimerStartUnixMs = ToUnixMs(_lives.TimerStartUtc),
                UndoBoosters = Boosters.GetCount(BoosterType.Undo),
                ShuffleBoosters = Boosters.GetCount(BoosterType.Shuffle),
                AddTubeBoosters = Boosters.GetCount(BoosterType.AddTube),
                RetryCount = RetryCount,
                LastVariant = LastVariant,
                LevelInProgress = LevelInProgress,
                PlayerName = PlayerName,
                TutorialsSeen = tutorialsSeen,
                SoundOn = GetSetting(SettingType.Sound),
                MusicOn = GetSetting(SettingType.Music),
                HapticsOn = GetSetting(SettingType.Haptics),
            };
        }

        private void ChangeTogether(Action change)
        {
            bool outer = _batchingChanges;
            _batchingChanges = true;
            try
            {
                change();
            }
            finally
            {
                _batchingChanges = outer;
            }

            RaiseChanged();
        }

        private void RaiseChanged()
        {
            if (!_batchingChanges)
            {
                Changed?.Invoke();
            }
        }

        private static DateTime? FromUnixMs(long unixMs) =>
            unixMs > 0 ? UnixEpoch.AddTicks(unixMs * TimeSpan.TicksPerMillisecond) : (DateTime?)null;

        private static long ToUnixMs(DateTime? utc) =>
            utc == null ? 0 : (utc.Value.Ticks - UnixEpoch.Ticks) / TimeSpan.TicksPerMillisecond;
    }
}
