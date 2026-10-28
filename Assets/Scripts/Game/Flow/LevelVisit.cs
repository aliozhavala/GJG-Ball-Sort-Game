namespace BallSort.Game.Flow
{
    /// <summary>
    /// What one visit to the Level scene plays: a level of the run, or (Editor only) a level file
    /// outside it, such as a test or draft level.
    /// </summary>
    /// <remarks>
    /// Only levels of the run record wins. A level file never pays a reward or moves progress: it is
    /// not part of the run, so winning it says nothing about the player's place in it.
    /// </remarks>
    public readonly struct LevelVisit
    {
        private LevelVisit(int levelNumber, string label, bool isFromFile)
        {
            LevelNumber = levelNumber;
            Label = label;
            IsFromFile = isFromFile;
        }

        /// <summary>1-based number in the level database; 0 for a level file.</summary>
        public int LevelNumber { get; }

        /// <summary>What the HUD and the Win panel call the level: its number, or the file's name.</summary>
        public string Label { get; }

        /// <summary>True for a level file: winning it pays nothing and moves no progress.</summary>
        public bool IsFromFile { get; }

        /// <summary>A level of the run. Winning it is recorded (a replay pays the reward but keeps progress).</summary>
        public static LevelVisit ForLevel(int levelNumber) => new LevelVisit(levelNumber, levelNumber.ToString(), false);

        /// <summary>A level file outside the run, named by <paramref name="fileName"/>.</summary>
        public static LevelVisit ForFile(string fileName) => new LevelVisit(0, fileName, true);
    }
}
