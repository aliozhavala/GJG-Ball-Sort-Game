using System;

namespace BallSort.Game.UI
{
    /// <summary>Formats the wait for the next life as <c>mm:ss</c>, shared by Home and the More Lives popup.</summary>
    public static class LifeTimerText
    {
        /// <summary>
        /// <paramref name="time"/> as <c>mm:ss</c>, rounded up so the label never shows 00:00 while a
        /// life is still on its way (minutes go past 59 for refill intervals over an hour).
        /// </summary>
        public static string Format(TimeSpan time)
        {
            int seconds = Math.Max(0, (int)Math.Ceiling(time.TotalSeconds));
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
