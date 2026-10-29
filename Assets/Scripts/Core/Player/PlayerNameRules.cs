namespace BallSort.Core.Player
{
    /// <summary>What a player name may be: 1 to <see cref="MaxLength"/> characters once trimmed.</summary>
    public static class PlayerNameRules
    {
        /// <summary>Longest name, in characters, after trimming.</summary>
        public const int MaxLength = 16;

        /// <summary>A new player's name, and the replacement for an unusable saved one.</summary>
        public const string Default = "Player";

        /// <summary>The name trimmed, or null when it is empty or too long once trimmed.</summary>
        public static string Validate(string name)
        {
            string trimmed = name?.Trim();
            return string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength ? null : trimmed;
        }
    }
}
