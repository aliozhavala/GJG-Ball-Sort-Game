using System.Collections.Generic;

namespace BallSort.Core.Model
{
    /// <summary>The Shuffle booster rearranged the balls of one tube.</summary>
    public readonly struct TubeShuffledEventArgs
    {
        /// <summary>The shuffled tube.</summary>
        public int TubeIndex { get; }

        /// <summary>
        /// The tube's balls bottom → top as the shuffle placed them, the top one already revealed. Keys
        /// are shown where they landed: a key the shuffle made findable is still on its ball here, and
        /// its <c>KeyFound</c> event follows.
        /// </summary>
        public IReadOnlyList<Ball> Balls { get; }

        /// <summary>
        /// Where each ball came from: <c>FromSlots[slot]</c> is the slot (before the shuffle) of the ball
        /// now in <c>slot</c>, so a view can move each ball it shows to its new place.
        /// </summary>
        public IReadOnlyList<int> FromSlots { get; }

        public TubeShuffledEventArgs(int tubeIndex, IReadOnlyList<Ball> balls, IReadOnlyList<int> fromSlots)
        {
            TubeIndex = tubeIndex;
            Balls = balls;
            FromSlots = fromSlots;
        }

        public override string ToString() => $"tube {TubeIndex} shuffled to [{string.Join(", ", Balls)}]";
    }
}
