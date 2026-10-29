using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// One level file, exactly as it appears on disk (see <c>docs/levels.md</c>): one board and
    /// <see cref="VariantSelector.VariantCount"/> variants of the puzzle on it.
    /// </summary>
    /// <remarks>
    /// A pure data carrier with no parsing attributes, so Core stays free of serializer
    /// dependencies. Required fields that are missing from the JSON arrive as their default value
    /// (<c>0</c> or <c>null</c>) and are reported by <see cref="LevelValidator"/>.
    /// </remarks>
    public sealed class LevelDto
    {
        /// <summary>The level format version this level was written for. Currently <see cref="LevelValidator.SupportedFormatVersion"/>.</summary>
        public int FormatVersion { get; set; }

        /// <summary>1-based level number.</summary>
        public int Id { get; set; }

        /// <summary>The level's tier; sets its win reward and its look on Home. Required; nullable so a missing value is reported.</summary>
        public LevelTier? Tier { get; set; }

        /// <summary>Optional: true for a level that introduces a mechanic. Nothing reads it yet.</summary>
        public bool? Tutorial { get; set; }

        /// <summary>The grid layout, shared by every variant. Required.</summary>
        public LayoutDto Layout { get; set; }

        /// <summary>The variants, V1 first. Required; exactly <see cref="VariantSelector.VariantCount"/>.</summary>
        public List<LevelVariantDto> Variants { get; set; }
    }
}
