using System.Collections.Generic;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The leaderboard's fixed entries below the player (there is no backend): ranked 2, 3, … in list
    /// order, the player always first.
    /// </summary>
    [CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "Ball Sort/Leaderboard Config")]
    public sealed class LeaderboardConfig : ScriptableObject
    {
        [Tooltip("Ranked from 2 down, top to bottom. Names should be family friendly.")]
        [SerializeField] private LeaderboardBot[] _bots = new LeaderboardBot[0];

        public IReadOnlyList<LeaderboardBot> Bots => _bots;

        private void OnValidate()
        {
            foreach (LeaderboardBot bot in _bots)
            {
                if (string.IsNullOrWhiteSpace(bot.Name))
                {
                    Debug.LogWarning($"{name}: every bot needs a name.", this);
                    return;
                }
            }
        }
    }
}
