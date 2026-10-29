namespace BallSort.Game.Services
{
    /// <summary>
    /// What Home shows on arriving from the Level scene: a notice (e.g. why a level could not be
    /// loaded) or a newly unlocked episode. Handed over in memory by <see cref="SceneLoader"/>, never saved.
    /// </summary>
    public readonly struct HomeArrival
    {
        private HomeArrival(string notice, int unlockedEpisode)
        {
            Notice = notice;
            UnlockedEpisode = unlockedEpisode;
        }

        /// <summary>A plain arrival: nothing to show.</summary>
        public static HomeArrival None => new HomeArrival(null, -1);

        /// <summary>A short message for Home's toast; null for none.</summary>
        public string Notice { get; }

        /// <summary>0-based index of the episode the last win moved into; -1 for none.</summary>
        public int UnlockedEpisode { get; }

        public bool HasUnlockedEpisode => UnlockedEpisode >= 0;

        public static HomeArrival WithNotice(string notice) => new HomeArrival(notice, -1);

        /// <param name="episodeIndex">0-based index of the episode just unlocked.</param>
        public static HomeArrival WithUnlockedEpisode(int episodeIndex) => new HomeArrival(null, episodeIndex);
    }
}
