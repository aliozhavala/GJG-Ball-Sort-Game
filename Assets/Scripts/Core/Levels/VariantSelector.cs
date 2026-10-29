using System;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// Which variant each try at a level plays (docs/levels.md, Variants and tries): tries 1–5 play
    /// V1–V5 in order; from try 6 on, a random variant among V3–V5, never the previous try's.
    /// </summary>
    /// <remarks>Variants are numbered from 1, like V1–V5 in the docs, the save and the tools.</remarks>
    public static class VariantSelector
    {
        /// <summary>Variants in every level file.</summary>
        public const int VariantCount = 5;

        /// <summary>First variant of the random rotation that follows the first <see cref="VariantCount"/> tries.</summary>
        public const int FirstRotatingVariant = 3;

        /// <summary>True for a variant number a level has, 1 to <see cref="VariantCount"/>.</summary>
        public static bool IsVariant(int variant) => variant >= 1 && variant <= VariantCount;

        /// <summary>Picks the variant for one try.</summary>
        /// <param name="tryNumber">The try at this level, from 1.</param>
        /// <param name="previousVariant">The variant the previous try played, or 0 if there was none.</param>
        /// <param name="random">Picks among the rotating variants from try <see cref="VariantCount"/> + 1 on; unused before.</param>
        /// <returns>A variant number, 1 to <see cref="VariantCount"/>.</returns>
        public static int Select(int tryNumber, int previousVariant, Random random)
        {
            if (tryNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(tryNumber), tryNumber, "Tries are counted from 1.");
            }

            if (tryNumber <= VariantCount)
            {
                return tryNumber;
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            // Draw from the rotation minus the previous variant, then step over the previous one, so
            // every other rotating variant is equally likely and a back-to-back repeat is impossible.
            int rotationSize = VariantCount - FirstRotatingVariant + 1;
            bool previousRotates = previousVariant >= FirstRotatingVariant && previousVariant <= VariantCount;
            int variant = FirstRotatingVariant + random.Next(previousRotates ? rotationSize - 1 : rotationSize);
            if (previousRotates && variant >= previousVariant)
            {
                variant++;
            }

            return variant;
        }
    }
}
