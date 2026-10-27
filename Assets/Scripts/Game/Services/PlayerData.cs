using System;
using BallSort.Core.Player;

namespace BallSort.Game.Services
{
    /// <summary>
    /// Everything that persists about the player: progress, coins, lives, boosters and settings.
    /// </summary>
    /// <remarks>
    /// <see cref="Changed"/> fires after any change, including ones made directly on
    /// <see cref="Wallet"/> or <see cref="Boosters"/>, so the save service needs only one subscription.
    /// </remarks>
    public sealed class PlayerData
    {
        private readonly bool[] _settings = new bool[3];
        private readonly LevelProgress _progress;

        // Set while one operation changes several values, so they are saved once, together.
        private bool _batchingChanges;

        /// <summary>The level the Level scene loads. Advances on win; stays on the last level once all are done.</summary>
        public int CurrentLevel => _progress.CurrentLevel;

        /// <summary>True once the last level has been won.</summary>
        public bool AllLevelsComplete => _progress.AllLevelsComplete;

        public Wallet Wallet { get; }

        public BoosterInventory Boosters { get; }

        /// <summary>Display only for now.</summary>
        public int Lives { get; private set; }

        /// <summary>Raised after any persistent value changes.</summary>
        public event Action Changed;

        /// <summary>Raised after <see cref="Lives"/> changes, with the new value.</summary>
        public event Action<int> LivesChanged;

        /// <summary>Raised after a setting changes, with the setting and its new value.</summary>
        public event Action<SettingType, bool> SettingChanged;

        /// <param name="save">A parsed, sanitised save; see <see cref="SaveService"/>.</param>
        /// <param name="levelCount">Levels in the game; the saved level must be within it.</param>
        public PlayerData(SaveData save, int levelCount)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            _progress = new LevelProgress(save.CurrentLevel, save.AllLevelsComplete, levelCount);
            Lives = save.Lives;
            Wallet = new Wallet(save.Coins);
            Boosters = new BoosterInventory(save.UndoBoosters, save.ShuffleBoosters, save.AddTubeBoosters);
            _settings[(int)SettingType.Sound] = save.SoundOn;
            _settings[(int)SettingType.Music] = save.MusicOn;
            _settings[(int)SettingType.Haptics] = save.HapticsOn;

            Wallet.Changed += _ => RaiseChanged();
            Boosters.Changed += (_, _) => RaiseChanged();
        }

        /// <summary>
        /// Records a won level: advances progress (see <see cref="LevelProgress.RecordWin"/>) and
        /// grants <paramref name="reward"/> coins, then raises <see cref="Changed"/> once so both
        /// are saved together, immediately.
        /// </summary>
        /// <param name="wonLevel">The level that was just won, 1-based.</param>
        /// <param name="reward">Coins to grant; replays are rewarded too.</param>
        public void RecordWin(int wonLevel, int reward)
        {
            _batchingChanges = true;
            try
            {
                _progress.RecordWin(wonLevel);
                Wallet.Add(reward);
            }
            finally
            {
                _batchingChanges = false;
            }

            Changed?.Invoke();
        }

        /// <summary>Sets the life count. Nothing spends or refills lives yet; the HUD already listens.</summary>
        public void SetLives(int lives)
        {
            lives = Math.Max(0, lives);
            if (lives == Lives)
            {
                return;
            }

            Lives = lives;
            LivesChanged?.Invoke(lives);
            RaiseChanged();
        }

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

        private void RaiseChanged()
        {
            if (!_batchingChanges)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>A snapshot of the current values, ready to write to disk.</summary>
        public SaveData ToSaveData() =>
            new SaveData
            {
                Version = SaveService.CurrentVersion,
                CurrentLevel = CurrentLevel,
                AllLevelsComplete = AllLevelsComplete,
                Coins = Wallet.Coins,
                Lives = Lives,
                UndoBoosters = Boosters.GetCount(BoosterType.Undo),
                ShuffleBoosters = Boosters.GetCount(BoosterType.Shuffle),
                AddTubeBoosters = Boosters.GetCount(BoosterType.AddTube),
                SoundOn = GetSetting(SettingType.Sound),
                MusicOn = GetSetting(SettingType.Music),
                HapticsOn = GetSetting(SettingType.Haptics),
            };
    }
}
