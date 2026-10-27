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

        /// <summary>The level the Level scene loads. Advances on win; stays on the last level once all are done.</summary>
        public int CurrentLevel { get; private set; }

        /// <summary>True once the last level has been won.</summary>
        public bool AllLevelsComplete { get; private set; }

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
        public PlayerData(SaveData save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            CurrentLevel = save.CurrentLevel;
            AllLevelsComplete = save.AllLevelsComplete;
            Lives = save.Lives;
            Wallet = new Wallet(save.Coins);
            Boosters = new BoosterInventory(save.UndoBoosters, save.ShuffleBoosters, save.AddTubeBoosters);
            _settings[(int)SettingType.Sound] = save.SoundOn;
            _settings[(int)SettingType.Music] = save.MusicOn;
            _settings[(int)SettingType.Haptics] = save.HapticsOn;

            Wallet.Changed += _ => Changed?.Invoke();
            Boosters.Changed += (_, _) => Changed?.Invoke();
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
            Changed?.Invoke();
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
            Changed?.Invoke();
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
