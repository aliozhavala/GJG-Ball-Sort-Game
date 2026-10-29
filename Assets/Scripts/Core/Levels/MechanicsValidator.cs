using System;
using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// The level-file rules for veils, locks, keys and clay in one variant (see <c>docs/mechanics.md</c>). Part of
    /// <see cref="LevelValidator"/>; split out so each half stays readable.
    /// </summary>
    internal static class MechanicsValidator
    {
        /// <param name="variant">A variant whose tubes are non-empty; individual tubes may still be null or broken.</param>
        /// <param name="grid">The resolved grid, or null when the grid is invalid (lock adjacency is then not checked).</param>
        /// <param name="errors">Receives one unprefixed message per broken rule.</param>
        public static void Validate(LevelVariantDto variant, BoardGrid grid, List<string> errors)
        {
            Dictionary<int, int> lockIndexById = ValidateLocks(variant, grid, errors, out Dictionary<int, int> lockOfTube);

            var keysPerLock = new Dictionary<int, int>();
            for (int t = 0; t < variant.Tubes.Count; t++)
            {
                TubeDto tube = variant.Tubes[t];
                if (tube?.Balls == null)
                {
                    continue;
                }

                bool hasLock = lockOfTube.TryGetValue(t, out int lockId);
                ValidateVeil(tube, t, variant.Colors, hasLock, lockId, errors);
                ValidateClay(tube, t, errors);
                ValidateKeys(tube, t, tube.Veil != null || hasLock, hasLock ? lockId : -1, lockIndexById, keysPerLock, errors);
            }

            foreach (KeyValuePair<int, int> entry in lockIndexById)
            {
                keysPerLock.TryGetValue(entry.Key, out int keys);
                if (keys != 1)
                {
                    errors.Add(keys == 0
                        ? $"locks[{entry.Value}]: lock {entry.Key} has no key."
                        : $"locks[{entry.Value}]: lock {entry.Key} has {keys} keys; expected exactly one.");
                }
            }
        }

        /// <returns>Lock id → index in <c>locks</c>, for every lock with a usable id.</returns>
        private static Dictionary<int, int> ValidateLocks(
            LevelVariantDto variant, BoardGrid grid, List<string> errors, out Dictionary<int, int> lockOfTube)
        {
            var lockIndexById = new Dictionary<int, int>();
            lockOfTube = new Dictionary<int, int>();
            if (variant.Locks == null)
            {
                return lockIndexById;
            }

            for (int l = 0; l < variant.Locks.Count; l++)
            {
                string where = $"locks[{l}]";
                LockDto lockDto = variant.Locks[l];
                if (lockDto == null)
                {
                    errors.Add($"{where} is null.");
                    continue;
                }

                bool idValid = true;
                if (lockDto.Id == null || lockDto.Id < 0)
                {
                    errors.Add(lockDto.Id == null
                        ? $"{where}: id is missing."
                        : $"{where}: id must be 0 or greater (was {lockDto.Id}).");
                    idValid = false;
                }
                else if (lockIndexById.TryGetValue(lockDto.Id.Value, out int first))
                {
                    errors.Add($"{where}: id {lockDto.Id} is already used by locks[{first}].");
                    idValid = false;
                }
                else
                {
                    lockIndexById.Add(lockDto.Id.Value, l);
                }

                if (lockDto.Tubes == null || lockDto.Tubes.Count == 0)
                {
                    errors.Add($"{where}: tubes is missing or empty.");
                    continue;
                }

                bool tubesValid = true;
                for (int i = 0; i < lockDto.Tubes.Count; i++)
                {
                    int tube = lockDto.Tubes[i];
                    if (tube < 0 || tube >= variant.Tubes.Count)
                    {
                        errors.Add($"{where}: tubes[{i}] is {tube}; expected a tube index 0 to {variant.Tubes.Count - 1}.");
                        tubesValid = false;
                    }
                    else if (lockOfTube.TryGetValue(tube, out int otherId))
                    {
                        errors.Add(otherId == (lockDto.Id ?? -1)
                            ? $"{where}: lists tube {tube} twice."
                            : $"tubes[{tube}] has more than one cover (locks {otherId} and {lockDto.Id}).");
                        tubesValid = false;
                    }
                    else if (idValid)
                    {
                        lockOfTube.Add(tube, lockDto.Id.Value);
                    }
                }

                if (tubesValid && grid != null && !AreAdjacentInOneRow(grid, lockDto.Tubes))
                {
                    errors.Add($"{where}: tubes {string.Join(", ", lockDto.Tubes)} must sit next to each other in one grid row.");
                }

                // One door spans every tube of a lock, so they must share a height.
                if (tubesValid && !HaveOneCapacity(variant, lockDto.Tubes, out int capacity, out int otherCapacity))
                {
                    errors.Add($"{where}: tubes {string.Join(", ", lockDto.Tubes)} mix capacities {capacity} and {otherCapacity}; all tubes under one lock must have the same capacity.");
                }
            }

            return lockIndexById;
        }

        /// <summary>
        /// True when every listed tube has the same capacity. Otherwise returns the first capacity and
        /// the first one that differs from it. Null tubes are skipped (reported elsewhere).
        /// </summary>
        private static bool HaveOneCapacity(LevelVariantDto variant, List<int> tubes, out int capacity, out int otherCapacity)
        {
            capacity = -1;
            otherCapacity = -1;
            foreach (int index in tubes)
            {
                TubeDto tube = variant.Tubes[index];
                if (tube == null)
                {
                    continue;
                }

                if (capacity < 0)
                {
                    capacity = tube.Capacity;
                }
                else if (tube.Capacity != capacity)
                {
                    otherCapacity = tube.Capacity;
                    return false;
                }
            }

            return true;
        }

        private static void ValidateVeil(TubeDto tube, int index, int colors, bool hasLock, int lockId, List<string> errors)
        {
            if (tube.Veil == null)
            {
                return;
            }

            string where = $"tubes[{index}]";
            if (tube.Veil.Color == null)
            {
                errors.Add($"{where}: veil.color is missing.");
            }
            else if (tube.Veil.Color < 0 || tube.Veil.Color >= colors)
            {
                errors.Add($"{where}: veil.color is {tube.Veil.Color}; expected a colour 0 to {colors - 1}.");
            }

            if (hasLock)
            {
                errors.Add($"{where} has more than one cover (a veil and lock {lockId}).");
            }
        }

        private static void ValidateClay(TubeDto tube, int index, List<string> errors)
        {
            if (!tube.Clay)
            {
                return;
            }

            string where = $"tubes[{index}]";
            for (int slot = 1; slot < tube.Balls.Count; slot++)
            {
                if (tube.Balls[slot] != tube.Balls[0])
                {
                    errors.Add($"{where}: a clay tube starts empty or with one colour, but holds {tube.Balls[0]} and {tube.Balls[slot]}.");
                    break;
                }
            }

            if (tube.Hidden != null && tube.Hidden.Contains(true))
            {
                errors.Add($"{where}: a clay tube cannot hold hidden balls.");
            }

            if (tube.Keys != null && tube.Keys.Count > 0)
            {
                errors.Add($"{where}: a clay tube cannot hold keys.");
            }
        }

        private static void ValidateKeys(
            TubeDto tube,
            int index,
            bool isCovered,
            int coveringLockId,
            Dictionary<int, int> lockIndexById,
            Dictionary<int, int> keysPerLock,
            List<string> errors)
        {
            if (tube.Keys == null)
            {
                return;
            }

            // A key in the top run of an open tube would be found before the player has done anything.
            int runStart = isCovered || tube.Clay ? tube.Balls.Count : tube.Balls.Count - RunLength(tube);
            var keyedSlots = new HashSet<int>();

            for (int k = 0; k < tube.Keys.Count; k++)
            {
                string where = $"tubes[{index}].keys[{k}]";
                KeyDto key = tube.Keys[k];
                if (key == null)
                {
                    errors.Add($"{where} is null.");
                    continue;
                }

                if (key.Slot == null)
                {
                    errors.Add($"{where}: slot is missing.");
                }
                else if (key.Slot < 0 || key.Slot >= tube.Balls.Count)
                {
                    errors.Add(tube.Balls.Count == 0
                        ? $"{where}: slot {key.Slot} is not a ball; the tube is empty."
                        : $"{where}: slot {key.Slot} is not a ball; expected 0 to {tube.Balls.Count - 1}.");
                }
                else if (!keyedSlots.Add(key.Slot.Value))
                {
                    errors.Add($"{where}: balls[{key.Slot}] already has a key; a ball holds at most one.");
                }
                else if (key.Slot >= runStart)
                {
                    errors.Add($"{where}: balls[{key.Slot}] is in the tube's top run at start, so the key would be found before the first move.");
                }

                if (key.Lock == null)
                {
                    errors.Add($"{where}: lock is missing.");
                    continue;
                }

                if (!lockIndexById.ContainsKey(key.Lock.Value))
                {
                    errors.Add($"{where}: lock {key.Lock} does not exist.");
                    continue;
                }

                keysPerLock.TryGetValue(key.Lock.Value, out int count);
                keysPerLock[key.Lock.Value] = count + 1;

                if (key.Lock == coveringLockId)
                {
                    errors.Add($"{where}: the key for lock {key.Lock} is inside a tube that lock {key.Lock} covers, so it can never be found.");
                }
            }
        }

        /// <summary>Length of the tube's top run (same colour, revealed, contiguous), as the board will compute it.</summary>
        private static int RunLength(TubeDto tube)
        {
            int count = tube.Balls.Count;
            bool IsHidden(int slot) => tube.Hidden != null && slot < tube.Hidden.Count && tube.Hidden[slot];

            if (count == 0 || IsHidden(count - 1))
            {
                return 0;
            }

            int length = 1;
            while (length < count && !IsHidden(count - 1 - length) && tube.Balls[count - 1 - length] == tube.Balls[count - 1])
            {
                length++;
            }

            return length;
        }

        /// <summary>True when some row holds all of <paramref name="tubes"/> in consecutive cells.</summary>
        private static bool AreAdjacentInOneRow(BoardGrid grid, List<int> tubes)
        {
            for (int row = 0; row < grid.RowCount; row++)
            {
                int first = int.MaxValue;
                int last = int.MinValue;
                int found = 0;
                for (int cell = 0; cell < grid.GetCellCount(row); cell++)
                {
                    if (tubes.Contains(grid.GetTubeAt(row, cell)))
                    {
                        first = Math.Min(first, cell);
                        last = Math.Max(last, cell);
                        found++;
                    }
                }

                // Each tube is in a row at most once, so all found and no hole means adjacent.
                if (found == tubes.Count && last - first + 1 == found)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
