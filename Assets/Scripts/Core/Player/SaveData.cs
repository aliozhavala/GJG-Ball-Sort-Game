namespace BallSort.Core.Player
{
    /// <summary>
    /// The on-disk shape of the player's save: one flat, versioned object, written as JSON by
    /// <c>BallSort.SaveJson</c>.
    /// </summary>
    /// <remarks>
    /// Every field is required (the JSON reader enforces it), so a truncated or hand-edited save fails
    /// to parse and is replaced by defaults instead of silently loading zeros (no coins, every setting
    /// off). Bump <see cref="CurrentVersion"/> and add a migration to the reader when the shape changes.
    /// </remarks>
    public sealed class SaveData
    {
        /// <summary>The save format this build writes. Older versions are migrated on load.</summary>
        public const int CurrentVersion = 3;

        public int Version { get; set; }
        public int CurrentLevel { get; set; }
        public bool AllLevelsComplete { get; set; }
        public int Coins { get; set; }
        public int Lives { get; set; }

        /// <summary>When the current wait for a life began, in Unix milliseconds (UTC); 0 for none (full).</summary>
        public long LifeTimerStartUnixMs { get; set; }

        public int UndoBoosters { get; set; }
        public int ShuffleBoosters { get; set; }
        public int AddTubeBoosters { get; set; }
        public int RetryCount { get; set; }
        public int LastVariant { get; set; }

        /// <summary>The level whose attempt has started and not ended; 0 for none.</summary>
        public int LevelInProgress { get; set; }

        public string PlayerName { get; set; }

        /// <summary>Ids of the levels whose tutorial popup has been dismissed, ascending.</summary>
        public int[] TutorialsSeen { get; set; }

        public bool SoundOn { get; set; }
        public bool MusicOn { get; set; }
        public bool HapticsOn { get; set; }

        /// <summary>A brand-new player's save: full lives, nothing in progress, no tutorial seen.</summary>
        public static SaveData CreateDefault(PlayerRules rules) =>
            new SaveData
            {
                Version = CurrentVersion,
                CurrentLevel = 1,
                AllLevelsComplete = false,
                Coins = rules.StartCoins,
                Lives = rules.MaxLives,
                LifeTimerStartUnixMs = 0,
                UndoBoosters = rules.StartUndo,
                ShuffleBoosters = rules.StartShuffle,
                AddTubeBoosters = rules.StartAddTube,
                RetryCount = 0,
                LastVariant = 0,
                LevelInProgress = 0,
                PlayerName = PlayerNameRules.Default,
                TutorialsSeen = new int[0],
                SoundOn = true,
                MusicOn = true,
                HapticsOn = true,
            };
    }
}
