using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// Design metadata written by the generator and curation tools. Gameplay never reads it.
    /// </summary>
    public sealed class LevelMetaDto
    {
        /// <summary>Length of the shortest known solution, if computed.</summary>
        public int? OptimalMoves { get; set; }

        /// <summary>Normalised difficulty score in <c>0 .. 1</c>, if computed.</summary>
        public double? Difficulty { get; set; }

        /// <summary>Names of the mechanics this level uses, e.g. <c>"hidden"</c>.</summary>
        public List<string> Mechanics { get; set; }

        /// <summary>True for hand-authored levels that introduce a mechanic.</summary>
        public bool? Tutorial { get; set; }
    }
}
