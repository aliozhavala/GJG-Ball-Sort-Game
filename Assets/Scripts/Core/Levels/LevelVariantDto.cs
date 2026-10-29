using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// One variant of a level: a complete puzzle on the level's shared grid.
    /// </summary>
    /// <remarks>
    /// The variants of a level share their structure (tube count, capacities, clay flags, cover
    /// kinds and lock tube sets) so one grid and one set of tube views fits them all; ball colours,
    /// hidden flags, keys, veil colours and lock ids are free. New mechanics add optional members
    /// here or on <see cref="TubeDto"/>; <c>null</c> means the variant does not use the mechanic.
    /// </remarks>
    public sealed class LevelVariantDto
    {
        /// <summary>Number of distinct colours; ball colour ids run <c>0 .. Colors - 1</c>.</summary>
        public int Colors { get; set; }

        /// <summary>The variant's tubes in board order.</summary>
        public List<TubeDto> Tubes { get; set; }

        /// <summary>Optional locks; each covers adjacent tubes in one grid row and is opened by one key.</summary>
        public List<LockDto> Locks { get; set; }

        /// <summary>Optional design metadata; never affects gameplay.</summary>
        public LevelMetaDto Meta { get; set; }
    }
}
