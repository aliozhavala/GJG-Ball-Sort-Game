namespace BallSort.Core.Levels
{
    /// <summary>Where one tube sits on the <see cref="BoardGrid"/>.</summary>
    public readonly struct GridPlacement
    {
        /// <summary>The bottom row of the tube's span (<c>0</c> = top row); the tube stands on this row's base line.</summary>
        public int Row { get; }

        /// <summary>The tube's cell in each row it spans; large tubes keep one column.</summary>
        public int Cell { get; }

        /// <summary>How many rows the tube spans: 1 for a normal tube, 2 or 3 for a large one.</summary>
        public int RowSpan { get; }

        /// <summary>The top row of the tube's span.</summary>
        public int TopRow => Row - RowSpan + 1;

        public GridPlacement(int row, int cell, int rowSpan)
        {
            Row = row;
            Cell = cell;
            RowSpan = rowSpan;
        }

        public override string ToString() => $"row {Row} cell {Cell} (spans {RowSpan})";
    }
}
