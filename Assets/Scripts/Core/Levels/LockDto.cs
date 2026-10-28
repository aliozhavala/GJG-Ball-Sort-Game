using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>A lock covering one or more tubes that sit next to each other in one grid row.</summary>
    public sealed class LockDto
    {
        /// <summary>
        /// The lock's id, matched by exactly one key; also selects its colour from the lock palette.
        /// Required; nullable so a missing value is reported, not read as id 0.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>Indices of the tubes the lock covers.</summary>
        public List<int> Tubes { get; set; }
    }
}
