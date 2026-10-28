namespace BallSort.Core.Model
{
    /// <summary>A tube's veil has opened because a tube completed in the veil's colour.</summary>
    public readonly struct TubeUnveiledEventArgs
    {
        /// <summary>Index of the tube that is now uncovered.</summary>
        public int TubeIndex { get; }

        /// <summary>The veil's colour: the colour whose completion opened it.</summary>
        public int ColorId { get; }

        public TubeUnveiledEventArgs(int tubeIndex, int colorId)
        {
            TubeIndex = tubeIndex;
            ColorId = colorId;
        }

        public override string ToString() => $"tube {TubeIndex} unveiled by color {ColorId}";
    }
}
