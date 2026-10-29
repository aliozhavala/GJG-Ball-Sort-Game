using System;

namespace BallSort.Core.Player
{
    /// <summary>
    /// The current time. Injected rather than read from <see cref="DateTime.UtcNow"/>, so lives can
    /// refill from saved timestamps and tests can move time forward.
    /// </summary>
    public interface IClock
    {
        /// <summary>The current time, in UTC.</summary>
        DateTime UtcNow { get; }
    }
}
