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

        public TubeShuffledEventArgs(int tubeIndex, IReadOnlyList<Ball> balls)
        {
            TubeIndex = tubeIndex;
            Balls = balls;
        }

        public override string ToString() => $"tube {TubeIndex} shuffled to [{string.Join(", ", Balls)}]";
    }
}
