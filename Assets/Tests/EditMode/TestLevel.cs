using System.Collections.Generic;
using BallSort.Core.Levels;
using Newtonsoft.Json;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// One variant on its layout: the unit the per-variant rules and the board apply to. Tests build
    /// one with <see cref="TestLevels"/>, break one thing, and check it with
    /// <see cref="LevelValidator.ValidateVariant"/>; <see cref="ToFile"/> makes it a whole level file.
    /// </summary>
    public sealed class TestLevel
    {
        public LayoutDto Layout { get; set; }

        public LevelVariantDto Variant { get; } = new LevelVariantDto();

        public int Colors
        {
            get => Variant.Colors;
            set => Variant.Colors = value;
        }

        public List<TubeDto> Tubes
        {
            get => Variant.Tubes;
            set => Variant.Tubes = value;
        }

        public List<LockDto> Locks
        {
            get => Variant.Locks;
            set => Variant.Locks = value;
        }

        /// <summary>A level file whose <see cref="VariantSelector.VariantCount"/> variants are copies of this one.</summary>
        public LevelDto ToFile(LevelTier tier = LevelTier.Regular)
        {
            var variants = new List<LevelVariantDto>();
            for (int v = 0; v < VariantSelector.VariantCount; v++)
            {
                variants.Add(CopyVariant());
            }

            return new LevelDto
            {
                FormatVersion = LevelValidator.SupportedFormatVersion,
                Id = 1,
                Tier = tier,
                Layout = Layout,
                Variants = variants,
            };
        }

        /// <summary>A deep copy of <see cref="Variant"/>, so each variant of a file can be changed on its own.</summary>
        public LevelVariantDto CopyVariant() =>
            JsonConvert.DeserializeObject<LevelVariantDto>(JsonConvert.SerializeObject(Variant));
    }
}
