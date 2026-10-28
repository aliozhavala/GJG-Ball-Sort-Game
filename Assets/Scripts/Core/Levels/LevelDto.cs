using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// One level file, exactly as it appears on disk (see <c>docs/levels.md</c>).
    /// </summary>
    /// <remarks>
    /// A pure data carrier with no parsing attributes, so Core stays free of serializer
    /// dependencies. Required fields that are missing from the JSON arrive as their default value
    /// (<c>0</c> or <c>null</c>) and are reported by <see cref="LevelValidator"/>.
    /// <para>
    /// New mechanics add optional members here or on <see cref="TubeDto"/>; <c>null</c> means the
    /// level does not use the mechanic, so existing level files stay valid.
    /// </para>
    /// </remarks>
    public sealed class LevelDto
    {
        /// <summary>The level format version this level was written for. Currently <see cref="LevelValidator.SupportedFormatVersion"/>.</summary>
        public int FormatVersion { get; set; }

        /// <summary>1-based level number.</summary>
        public int Id { get; set; }

        /// <summary>Number of distinct colours; ball colour ids run <c>0 .. Colors - 1</c>.</summary>
        public int Colors { get; set; }

        /// <summary>The grid layout. Required.</summary>
        public LayoutDto Layout { get; set; }

        /// <summary>The level's tubes in board order.</summary>
        public List<TubeDto> Tubes { get; set; }

        /// <summary>Optional locks; each covers adjacent tubes in one grid row and is opened by one key.</summary>
        public List<LockDto> Locks { get; set; }

        /// <summary>Optional design metadata; never affects gameplay.</summary>
        public LevelMetaDto Meta { get; set; }
    }
}
