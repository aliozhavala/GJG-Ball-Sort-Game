using System.Collections.Generic;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// The tutorial popups, keyed by level id (docs/screens.md, Tutorials). A level whose file sets
    /// <c>"tutorial": true</c> shows its entry the first time it is entered. Product edits the text here,
    /// with no code change.
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "Ball Sort/Tutorial Config")]
    public sealed class TutorialConfig : ScriptableObject
    {
        [SerializeField] private TutorialEntry[] _entries = new TutorialEntry[0];

        /// <summary>The popup of level <paramref name="levelId"/>, if it has one.</summary>
        public bool TryGetEntry(int levelId, out TutorialEntry entry)
        {
            foreach (TutorialEntry candidate in _entries)
            {
                if (candidate != null && candidate.LevelId == levelId)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        private void OnValidate()
        {
            var seen = new HashSet<int>();
            foreach (TutorialEntry entry in _entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (!seen.Add(entry.LevelId))
                {
                    Debug.LogWarning($"{name}: level {entry.LevelId} has more than one entry; only the first is shown.", this);
                }

                if (string.IsNullOrWhiteSpace(entry.Text))
                {
                    Debug.LogWarning($"{name}: the entry of level {entry.LevelId} has no text.", this);
                }
            }
        }
    }
}
