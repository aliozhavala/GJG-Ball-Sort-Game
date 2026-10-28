using System.Collections.Generic;

namespace BallSort.Core.Model
{
    /// <summary>A lock has opened: every tube it covered is now uncovered.</summary>
    public readonly struct LockOpenedEventArgs
    {
        /// <summary>Id of the lock.</summary>
        public int LockId { get; }

        /// <summary>The tubes the lock covered, in ascending index order.</summary>
        public IReadOnlyList<int> TubeIndices { get; }

        public LockOpenedEventArgs(int lockId, IReadOnlyList<int> tubeIndices)
        {
            LockId = lockId;
            TubeIndices = tubeIndices;
        }

        public override string ToString() => $"lock {LockId} opened ({TubeIndices.Count} tube(s))";
    }
}
