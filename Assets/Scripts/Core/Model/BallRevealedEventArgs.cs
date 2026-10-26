namespace BallSort.Core.Model
{
    /// <summary>A previously hidden ball has become visible to the player.</summary>
    public readonly struct BallRevealedEventArgs
    {
        /// <summary>Index of the tube holding the ball.</summary>
        public int TubeIndex { get; }

        /// <summary>Slot within the tube, <c>0</c> = bottom. Always the tube's top slot today.</summary>
        public int SlotIndex { get; }

        /// <summary>The colour the player may now be shown.</summary>
        public int ColorId { get; }

        public BallRevealedEventArgs(int tubeIndex, int slotIndex, int colorId)
        {
            TubeIndex = tubeIndex;
            SlotIndex = slotIndex;
            ColorId = colorId;
        }

        public override string ToString() => $"tube {TubeIndex} slot {SlotIndex} is color {ColorId}";
    }
}
