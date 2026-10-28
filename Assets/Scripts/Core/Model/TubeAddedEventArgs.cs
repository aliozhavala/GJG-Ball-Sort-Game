namespace BallSort.Core.Model
{
    /// <summary>An empty tube was appended to the board and placed on the grid.</summary>
    public readonly struct TubeAddedEventArgs
    {
        /// <summary>Index of the new tube; always the last index on the board.</summary>
        public int TubeIndex { get; }

        /// <summary>Slots in the new tube.</summary>
        public int Capacity { get; }

        /// <summary>Grid row the tube joined, <c>0</c> = top row.</summary>
        public int Row { get; }

        /// <summary>Cell the tube occupies in that row; always the row's last cell.</summary>
        public int Cell { get; }

        public TubeAddedEventArgs(int tubeIndex, int capacity, int row, int cell)
        {
            TubeIndex = tubeIndex;
            Capacity = capacity;
            Row = row;
            Cell = cell;
        }

        public override string ToString() =>
            $"tube {TubeIndex} added with capacity {Capacity} at row {Row} cell {Cell}";
    }
}
