using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>One tube in a level file.</summary>
    /// <remarks>
    /// Every mechanic field is optional, so a level that does not use a mechanic simply omits it.
    /// </remarks>
    public sealed class TubeDto
    {
        /// <summary>Number of slots in the tube.</summary>
        public int Capacity { get; set; }

        /// <summary>Colour ids, bottom to top. Required; an empty tube has an empty list.</summary>
        public List<int> Balls { get; set; }

        /// <summary>Optional hidden flags, parallel to <see cref="Balls"/>. <c>null</c> means all revealed.</summary>
        public List<bool> Hidden { get; set; }

        /// <summary>Optional veil over the tube. A tube has at most one cover (veil or lock).</summary>
        public VeilDto Veil { get; set; }

        /// <summary>True for a clay tube, which is never a move source.</summary>
        public bool Clay { get; set; }

        /// <summary>Optional keys on this tube's balls.</summary>
        public List<KeyDto> Keys { get; set; }
    }
}
