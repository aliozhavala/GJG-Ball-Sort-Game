using System;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>One made-up player on the leaderboard: a name and an avatar id (<see cref="AvatarConfig"/>).</summary>
    [Serializable]
    public struct LeaderboardBot
    {
        [SerializeField] private string _name;
        [SerializeField, Min(1)] private int _avatarId;

        public LeaderboardBot(string name, int avatarId)
        {
            _name = name;
            _avatarId = avatarId;
        }

        public string Name => _name;

        public int AvatarId => _avatarId;
    }
}
