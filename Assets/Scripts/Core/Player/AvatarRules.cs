namespace BallSort.Core.Player
{
    /// <summary>Which avatars a player can pick: ids 1 to <see cref="Count"/>.</summary>
    public static class AvatarRules
    {
        /// <summary>Avatars with art; the menu's other frames stay empty.</summary>
        public const int Count = 5;

        /// <summary>A new player's avatar, and the replacement for an unusable saved one.</summary>
        public const int Default = 1;

        /// <summary>True for an avatar the player can pick.</summary>
        public static bool IsValid(int avatarId) => avatarId >= 1 && avatarId <= Count;
    }
}
