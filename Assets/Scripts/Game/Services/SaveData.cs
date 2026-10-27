using Newtonsoft.Json;

namespace BallSort.Game.Services
{
    /// <summary>
    /// The on-disk shape of the player's save: one flat, versioned JSON object.
    /// </summary>
    /// <remarks>
    /// Every field is required, so a truncated or hand-edited save fails to parse and is replaced
    /// by defaults instead of silently loading zeros (no coins, every setting off).
    /// Bump <see cref="SaveService.CurrentVersion"/> and add a migration when the shape changes.
    /// </remarks>
    public sealed class SaveData
    {
        [JsonProperty(Required = Required.Always)] public int Version { get; set; }
        [JsonProperty(Required = Required.Always)] public int CurrentLevel { get; set; }
        [JsonProperty(Required = Required.Always)] public bool AllLevelsComplete { get; set; }
        [JsonProperty(Required = Required.Always)] public int Coins { get; set; }
        [JsonProperty(Required = Required.Always)] public int Lives { get; set; }
        [JsonProperty(Required = Required.Always)] public int UndoBoosters { get; set; }
        [JsonProperty(Required = Required.Always)] public int ShuffleBoosters { get; set; }
        [JsonProperty(Required = Required.Always)] public int AddTubeBoosters { get; set; }
        [JsonProperty(Required = Required.Always)] public bool SoundOn { get; set; }
        [JsonProperty(Required = Required.Always)] public bool MusicOn { get; set; }
        [JsonProperty(Required = Required.Always)] public bool HapticsOn { get; set; }

        /// <summary>A brand-new player's save.</summary>
        public static SaveData CreateDefault(EconomyConfig economy) =>
            new SaveData
            {
                Version = SaveService.CurrentVersion,
                CurrentLevel = 1,
                AllLevelsComplete = false,
                Coins = economy.StartCoins,
                Lives = economy.StartLives,
                UndoBoosters = economy.StartUndo,
                ShuffleBoosters = economy.StartShuffle,
                AddTubeBoosters = economy.StartAddTube,
                SoundOn = true,
                MusicOn = true,
                HapticsOn = true,
            };
    }
}
