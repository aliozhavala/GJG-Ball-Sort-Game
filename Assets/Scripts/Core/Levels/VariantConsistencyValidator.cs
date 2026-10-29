using System.Collections.Generic;
using BallSort.Core.Model;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// The rule that a level's variants share one board (see <c>docs/levels.md</c>, Variant consistency):
    /// the same tube count and, tube by tube, the same capacity, clay flag and cover kind, and locks
    /// covering the same tube sets. Every variant is compared with V1. Part of <see cref="LevelValidator"/>.
    /// </summary>
    /// <remarks>
    /// Ball colours, hidden flags, keys, veil colours and lock ids may differ: the grid, the tube views
    /// and the lock doors only depend on what is checked here.
    /// </remarks>
    internal static class VariantConsistencyValidator
    {
        /// <param name="variants">The level's variants; null variants and tubes are skipped (reported elsewhere).</param>
        /// <param name="errors">Receives one message per difference, prefixed with its variant.</param>
        public static void Validate(List<LevelVariantDto> variants, List<string> errors)
        {
            LevelVariantDto reference = variants[0];
            if (reference?.Tubes == null)
            {
                return;
            }

            CoverKind[] referenceCovers = GetCoverKinds(reference);
            for (int v = 1; v < variants.Count; v++)
            {
                LevelVariantDto variant = variants[v];
                if (variant?.Tubes == null)
                {
                    continue;
                }

                string where = $"variant {v + 1}";
                if (variant.Tubes.Count != reference.Tubes.Count)
                {
                    // Tubes are matched by index, so comparing them one by one would only add noise.
                    errors.Add($"{where}: has {variant.Tubes.Count} tubes, but variant 1 has {reference.Tubes.Count}.");
                    continue;
                }

                CoverKind[] covers = GetCoverKinds(variant);
                bool coversMatch = true;
                for (int t = 0; t < variant.Tubes.Count; t++)
                {
                    TubeDto tube = variant.Tubes[t];
                    TubeDto referenceTube = reference.Tubes[t];
                    if (tube == null || referenceTube == null)
                    {
                        continue;
                    }

                    string tubeWhere = $"{where}: tubes[{t}]";
                    if (tube.Capacity != referenceTube.Capacity)
                    {
                        errors.Add($"{tubeWhere}: capacity is {tube.Capacity}, but variant 1 has {referenceTube.Capacity}.");
                    }

                    if (tube.Clay != referenceTube.Clay)
                    {
                        errors.Add(tube.Clay
                            ? $"{tubeWhere}: is clay, but not in variant 1."
                            : $"{tubeWhere}: is not clay, but is in variant 1.");
                    }

                    if (covers[t] != referenceCovers[t])
                    {
                        errors.Add($"{tubeWhere}: has {Describe(covers[t])}, but variant 1 has {Describe(referenceCovers[t])}.");
                        coversMatch = false;
                    }
                }

                // A lock over other tubes already shows up as a cover difference above; this catches the
                // same locked tubes grouped into different locks (one door versus two).
                if (coversMatch)
                {
                    CompareLockTubeSets(variant, reference, where, errors);
                }
            }
        }

        private static void CompareLockTubeSets(
            LevelVariantDto variant, LevelVariantDto reference, string where, List<string> errors)
        {
            List<string> referenceSets = GetLockTubeSets(reference, out _);
            List<string> sets = GetLockTubeSets(variant, out List<int> lockIndices);

            for (int i = 0; i < sets.Count; i++)
            {
                if (!referenceSets.Contains(sets[i]))
                {
                    errors.Add($"{where}: locks[{lockIndices[i]}] covers tubes {sets[i]}, but no lock in variant 1 covers exactly those tubes.");
                }
            }

            foreach (string referenceSet in referenceSets)
            {
                if (!sets.Contains(referenceSet))
                {
                    errors.Add($"{where}: no lock covers tubes {referenceSet}, as one lock does in variant 1.");
                }
            }
        }

        /// <summary>Each lock's tubes, sorted and joined (e.g. <c>"3, 4"</c>), for locks whose tube list is usable.</summary>
        /// <param name="lockIndices">The index in <c>locks</c> of each returned set.</param>
        private static List<string> GetLockTubeSets(LevelVariantDto variant, out List<int> lockIndices)
        {
            var sets = new List<string>();
            lockIndices = new List<int>();
            if (variant.Locks == null)
            {
                return sets;
            }

            for (int l = 0; l < variant.Locks.Count; l++)
            {
                List<int> tubes = variant.Locks[l]?.Tubes;
                if (tubes == null || tubes.Count == 0)
                {
                    continue;
                }

                var sorted = new List<int>(tubes);
                sorted.Sort();
                sets.Add(string.Join(", ", sorted));
                lockIndices.Add(l);
            }

            return sets;
        }

        private static CoverKind[] GetCoverKinds(LevelVariantDto variant)
        {
            var covers = new CoverKind[variant.Tubes.Count];
            if (variant.Locks != null)
            {
                foreach (LockDto lockDto in variant.Locks)
                {
                    if (lockDto?.Tubes == null)
                    {
                        continue;
                    }

                    foreach (int tube in lockDto.Tubes)
                    {
                        if (tube >= 0 && tube < covers.Length)
                        {
                            covers[tube] = CoverKind.Lock;
                        }
                    }
                }
            }

            // A tube with both is invalid anyway (one cover per tube); call it a veil.
            for (int t = 0; t < covers.Length; t++)
            {
                if (variant.Tubes[t]?.Veil != null)
                {
                    covers[t] = CoverKind.Veil;
                }
            }

            return covers;
        }

        private static string Describe(CoverKind cover) =>
            cover == CoverKind.Veil ? "a veil" : cover == CoverKind.Lock ? "a lock" : "no cover";
    }
}
