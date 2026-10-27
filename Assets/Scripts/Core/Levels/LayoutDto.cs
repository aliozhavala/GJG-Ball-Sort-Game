using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>Optional presentation hints for a level.</summary>
    public sealed class LayoutDto
    {
        /// <summary>Tube counts per row, top row first. See <see cref="LevelLayout"/> for the rules.</summary>
        public List<int> Rows { get; set; }
    }
}
