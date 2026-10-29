using System;

namespace BallSort.Core.Player
{
    /// <summary>
    /// The player's lives: at most <see cref="Max"/>, never below 0, and one coming back every
    /// <see cref="RefillInterval"/> while below the maximum (docs/screens.md, Lives).
    /// </summary>
    /// <remarks>
    /// The refill is computed from a timestamp, never counted while the app runs, so lives come back
    /// while the app is closed: <see cref="TimerStartUtc"/> is when the current wait began, and each
    /// whole interval since then is one life. Every method takes the current time, so the class has
    /// no clock of its own.
    /// </remarks>
    public sealed class Lives
    {
        /// <param name="count">Lives held, 0 to <paramref name="max"/>.</param>
        /// <param name="timerStartUtc">
        /// When the current wait for a life began; ignored at the maximum. Null below the maximum
        /// (e.g. a save from before lives refilled) starts the wait at <paramref name="nowUtc"/>.
        /// </param>
        /// <param name="max">Most lives a player can hold; at least one.</param>
        /// <param name="refillInterval">Time for one life to come back; positive.</param>
        /// <param name="nowUtc">The current time; lives due since the timer started are added at once.</param>
        public Lives(int count, DateTime? timerStartUtc, int max, TimeSpan refillInterval, DateTime nowUtc)
        {
            if (max < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "A player must be able to hold a life.");
            }

            if (refillInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(refillInterval), refillInterval, "The refill interval must be positive.");
            }

            if (count < 0 || count > max)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, $"Lives must be in 0..{max}.");
            }

            Max = max;
            RefillInterval = refillInterval;
            Count = count;
            TimerStartUtc = count < max ? timerStartUtc ?? nowUtc : (DateTime?)null;
            ApplyRefill(nowUtc);
        }

        /// <summary>Lives held now (after the latest <see cref="Refresh"/>).</summary>
        public int Count { get; private set; }

        /// <summary>Most lives a player can hold.</summary>
        public int Max { get; }

        /// <summary>Time for one life to come back while below <see cref="Max"/>.</summary>
        public TimeSpan RefillInterval { get; }

        /// <summary>True at the maximum: no life is on its way.</summary>
        public bool IsFull => Count >= Max;

        /// <summary>When the current wait for a life began, in UTC; null at the maximum.</summary>
        public DateTime? TimerStartUtc { get; private set; }

        /// <summary>Raised after <see cref="Count"/> or <see cref="TimerStartUtc"/> changes, so the new state can be saved.</summary>
        public event Action Changed;

        /// <summary>Adds every life that has come back since the timer started.</summary>
        /// <returns>True when anything changed.</returns>
        public bool Refresh(DateTime nowUtc)
        {
            if (!ApplyRefill(nowUtc))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Time until the next life comes back, as of <paramref name="nowUtc"/>; null when full by then.
        /// Does not change anything.
        /// </summary>
        public TimeSpan? GetTimeUntilNext(DateTime nowUtc)
        {
            if (TimerStartUtc == null)
            {
                return null;
            }

            TimeSpan elapsed = nowUtc - TimerStartUtc.Value;
            if (elapsed < TimeSpan.Zero)
            {
                // The clock was turned back: the next refresh restarts the wait.
                return RefillInterval;
            }

            long dueLives = elapsed.Ticks / RefillInterval.Ticks;
            if (Count + dueLives >= Max)
            {
                return null;
            }

            return TimeSpan.FromTicks(RefillInterval.Ticks - elapsed.Ticks % RefillInterval.Ticks);
        }

        /// <summary>
        /// Spends one life, after adding those that came back. At the maximum the wait for the next one
        /// starts now; at 0 nothing changes (lives never go below 0).
        /// </summary>
        /// <returns>True when a life was spent; false at 0.</returns>
        public bool LoseOne(DateTime nowUtc)
        {
            bool refilled = ApplyRefill(nowUtc);
            if (Count == 0)
            {
                if (refilled)
                {
                    Changed?.Invoke();
                }

                return false;
            }

            if (IsFull)
            {
                TimerStartUtc = nowUtc;
            }

            Count--;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Sets the lives directly, for testing tools. Below the maximum, the next life comes back
        /// after <paramref name="timeUntilNext"/> (clamped to one interval).
        /// </summary>
        public void Set(int count, TimeSpan timeUntilNext, DateTime nowUtc)
        {
            if (count < 0 || count > Max)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, $"Lives must be in 0..{Max}.");
            }

            if (timeUntilNext <= TimeSpan.Zero || timeUntilNext > RefillInterval)
            {
                timeUntilNext = RefillInterval;
            }

            Count = count;
            TimerStartUtc = count < Max ? nowUtc - (RefillInterval - timeUntilNext) : (DateTime?)null;
            Changed?.Invoke();
        }

        private bool ApplyRefill(DateTime nowUtc)
        {
            if (TimerStartUtc == null)
            {
                return false;
            }

            DateTime start = TimerStartUtc.Value;
            if (nowUtc < start)
            {
                // The clock was turned back: restart the wait rather than wait for the old time to return.
                TimerStartUtc = nowUtc;
                return true;
            }

            long dueLives = (nowUtc - start).Ticks / RefillInterval.Ticks;
            if (dueLives == 0)
            {
                return false;
            }

            int gained = (int)Math.Min(dueLives, Max - Count);
            Count += gained;
            TimerStartUtc = IsFull ? (DateTime?)null : start + TimeSpan.FromTicks(RefillInterval.Ticks * gained);
            return true;
        }
    }
}
