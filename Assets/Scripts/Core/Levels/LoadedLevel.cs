using System;
using BallSort.Core.Model;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// A validated level ready to play: its source data, from which each try builds a fresh board in
    /// one of its variants, and the grid they all share.
    /// </summary>
    public sealed class LoadedLevel
    {
        /// <summary>Display name, usually the file name; used in logs and error messages.</summary>
        public string Name { get; }

        /// <summary>The validated source data. Every try and restart rebuilds its board from it.</summary>
        public LevelDto Source { get; }

        /// <summary>The level's id, 1-based; matches its number in the run for shipped levels.</summary>
        public int Id => Source.Id;

        public LevelTier Tier => Source.Tier.Value;

        /// <summary>True for a level that introduces a mechanic. Nothing reads it yet.</summary>
        public bool IsTutorial => Source.Tutorial == true;

        /// <summary>Number of variants, always <see cref="VariantSelector.VariantCount"/> once validated.</summary>
        public int VariantCount => Source.Variants.Count;

        internal LoadedLevel(string name, LevelDto source)
        {
            Name = name;
            Source = source;
        }

        /// <summary>The source data of one variant.</summary>
        /// <param name="variant">1 to <see cref="VariantCount"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the level has no such variant.</exception>
        public LevelVariantDto GetVariant(int variant)
        {
            if (variant < 1 || variant > VariantCount)
            {
                throw new ArgumentOutOfRangeException(nameof(variant), variant, $"{Name} has variants 1 to {VariantCount}.");
            }

            return Source.Variants[variant - 1];
        }

        /// <summary>A fresh board in <paramref name="variant"/>: every hidden ball hidden, every cover and key in place.</summary>
        /// <param name="variant">1 to <see cref="VariantCount"/>.</param>
        public BoardState CreateBoard(int variant) => LevelFactory.CreateBoard(GetVariant(variant));

        /// <summary>A fresh grid without added tubes; the same for every variant.</summary>
        public BoardGrid CreateGrid() => LevelFactory.CreateGrid(Source);
    }
}
