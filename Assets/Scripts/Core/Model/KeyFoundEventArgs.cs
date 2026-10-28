namespace BallSort.Core.Model
{
    /// <summary>
    /// A key has been found: its ball became part of a selectable run. The key mark is already gone
    /// from the ball, and <see cref="BoardState.LockOpened"/> follows for its lock.
    /// </summary>
    public readonly struct KeyFoundEventArgs
    {
        /// <summary>Index of the tube holding the ball.</summary>
        public int TubeIndex { get; }

        /// <summary>Slot of the ball within the tube, <c>0</c> = bottom.</summary>
        public int SlotIndex { get; }

        /// <summary>Id of the lock the key opens.</summary>
        public int LockId { get; }

        public KeyFoundEventArgs(int tubeIndex, int slotIndex, int lockId)
        {
            TubeIndex = tubeIndex;
            SlotIndex = slotIndex;
            LockId = lockId;
        }

        public override string ToString() => $"key for lock {LockId} found at tube {TubeIndex} slot {SlotIndex}";
    }
}
