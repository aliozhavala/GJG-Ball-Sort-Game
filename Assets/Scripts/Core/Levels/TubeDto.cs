using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>One tube in a level file.</summary>
    /// <remarks>
    /// Per-ball mechanics are stored as optional arrays parallel to <see cref="Balls"/>, as
    /// <see cref="Hidden"/> is, so a level that does not use a mechanic simply omits its array.
    /// </remarks>
    public sealed class TubeDto
    {
        /// <summary>Number of slots in the tube.</summary>
        public int Capacity { get; set; }

        /// <summary>Colour ids, bottom to top. Required; an empty tube has an empty list.</summary>
        public List<int> Balls { get; set; }

        /// <summary>Optional hidden flags, parallel to <see cref="Balls"/>. <c>null</c> means all revealed.</summary>
        public List<bool> Hidden { get; set; }
    }
}
