namespace BallSort.Core.Rules
{
    /// <summary>The outcome of one <see cref="StuckDetector.Analyze"/>: the verdict and how much was searched.</summary>
    public readonly struct StuckAnalysis
    {
        public StuckAnalysis(StuckVerdict verdict, int positionsExplored)
        {
            Verdict = verdict;
            PositionsExplored = positionsExplored;
        }

        public StuckVerdict Verdict { get; }

        /// <summary>Distinct positions the search recorded, the current one included.</summary>
        public int PositionsExplored { get; }

        /// <summary>True for <see cref="StuckVerdict.NoLegalMove"/> and <see cref="StuckVerdict.Loop"/>.</summary>
        public bool IsStuck => Verdict == StuckVerdict.NoLegalMove || Verdict == StuckVerdict.Loop;

        public override string ToString() => $"{Verdict} ({PositionsExplored} positions)";
    }
}
