using System;
using BallSort.Core.Player;

namespace BallSort.Tests.EditMode
{
    /// <summary>A clock the test moves by hand, e.g. to let lives refill while "the app is closed".</summary>
    internal sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; set; }

        public void Advance(TimeSpan time) => UtcNow += time;
    }
}
