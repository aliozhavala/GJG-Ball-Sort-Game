namespace BallSort.Game.Flow
{
    /// <summary>
    /// What one visit to the Level scene plays: a level of the run, or (Editor only) a test visit to
    /// any level or level file in a chosen variant.
    /// </summary>
    /// <remarks>
    /// Only visits of the run count: each try picks its variant by the try rules, and a win pays the
    /// tier's reward and moves progress. An editor test visit plays its chosen variant on every try
    /// and records nothing: no try, no reward, no progress.
    /// </remarks>
    public readonly struct LevelVisit
    {
        private LevelVisit(int levelNumber, string label, int variant)
        {
            LevelNumber = levelNumber;
            Label = label;
            Variant = variant;
        }

        /// <summary>1-based number in the level database; 0 for a level file.</summary>
        public int LevelNumber { get; }

        /// <summary>What the HUD and the Win panel call the level: its number or file name, plus the variant for a test visit.</summary>
        public string Label { get; }

        /// <summary>The variant a test visit plays, 1-based; 0 for a visit of the run, whose tries pick their own.</summary>
        public int Variant { get; }

        /// <summary>True for an editor test visit: it plays <see cref="Variant"/> and records nothing.</summary>
        public bool IsEditorTest => Variant > 0;

        /// <summary>A level of the run. Tries pick the variant; a win is recorded (a replay pays the reward but keeps progress).</summary>
        public static LevelVisit ForLevel(int levelNumber) => new LevelVisit(levelNumber, levelNumber.ToString(), 0);

        /// <summary>An editor test visit to a level of the run, in <paramref name="variant"/>.</summary>
        public static LevelVisit ForEditorLevel(int levelNumber, int variant) =>
            new LevelVisit(levelNumber, $"{levelNumber} · V{variant}", variant);

        /// <summary>An editor test visit to a level file outside the run, named by <paramref name="fileName"/>, in <paramref name="variant"/>.</summary>
        public static LevelVisit ForEditorFile(string fileName, int variant) =>
            new LevelVisit(0, $"{fileName} · V{variant}", variant);
    }
}
