using System;
using BallSort.Core.Player;

namespace BallSort.Game.Services
{
    /// <summary>The device clock, in UTC, so a change of time zone neither grants nor takes lives.</summary>
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
