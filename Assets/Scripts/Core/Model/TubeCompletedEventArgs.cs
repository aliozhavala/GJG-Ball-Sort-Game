namespace BallSort.Core.Model
{
    /// <summary>A tube just became full with a single colour, and is now finished.</summary>
    public readonly struct TubeCompletedEventArgs
    {
        /// <summary>Index of the completed tube.</summary>
        public int TubeIndex { get; }

        /// <summary>The single colour filling the tube.</summary>
        public int ColorId { get; }

        public TubeCompletedEventArgs(int tubeIndex, int colorId)
        {
            TubeIndex = tubeIndex;
            ColorId = colorId;
        }

        public override string ToString() => $"tube {TubeIndex} completed with color {ColorId}";
    }
}
