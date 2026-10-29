namespace BallSort.Core.Levels
{
    /// <summary>
    /// How hard a level is meant to be. Sets its win reward and its look on Home; never affects the rules.
    /// </summary>
    /// <remarks>Written in level files as <c>"regular"</c>, <c>"hard"</c> or <c>"superHard"</c>.</remarks>
    public enum LevelTier
    {
        Regular,
        Hard,
        SuperHard,
    }
}
