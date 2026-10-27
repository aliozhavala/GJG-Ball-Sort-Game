namespace BallSort.Core.Model
{
    /// <summary>An empty tube was appended to the board.</summary>
    public readonly struct TubeAddedEventArgs
    {
        /// <summary>Index of the new tube; always the last index on the board.</summary>
        public int TubeIndex { get; }

        /// <summary>Slots in the new tube.</summary>
        public int Capacity { get; }

        public TubeAddedEventArgs(int tubeIndex, int capacity)
        {
            TubeIndex = tubeIndex;
            Capacity = capacity;
        }

        public override string ToString() => $"tube {TubeIndex} added with capacity {Capacity}";
    }
}
