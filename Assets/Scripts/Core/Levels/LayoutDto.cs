using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>Where each tube sits on screen. Required in every level file.</summary>
    public sealed class LayoutDto
    {
        /// <summary>
        /// Rows of cells, top row first. Each cell is a tube index, or <c>null</c> for a gap. A large
        /// tube is listed in every row it spans. See <see cref="LevelLayout"/> for the rules.
        /// </summary>
        public List<List<int?>> Grid { get; set; }
    }
}
