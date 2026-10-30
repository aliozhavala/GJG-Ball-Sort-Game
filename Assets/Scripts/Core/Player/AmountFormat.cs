using System;
using System.Globalization;

namespace BallSort.Core.Player
{
    /// <summary>Short forms of large amounts (coins) for labels with little room: 999, 1.2K, 125K, 1.2M.</summary>
    public static class AmountFormat
    {
        private static readonly string[] Suffixes = { "K", "M", "B", "T" };

        /// <summary>
        /// <paramref name="amount"/> as is below 1,000, otherwise with a K, M, B or T suffix: one decimal
        /// below 100 of the unit (12.5K), none from 100 (125K), and no ".0" (1K). Digits are cut, never
        /// rounded up, so the short form never shows more than there is (999,999 is 999K, not 1000K).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="amount"/> is negative.</exception>
        public static string Abbreviate(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amounts are never negative.");
            }

            if (amount < 1000)
            {
                return amount.ToString(CultureInfo.InvariantCulture);
            }

            long unit = 1000;
            int suffix = 0;
            while (suffix < Suffixes.Length - 1 && amount / 1000 >= unit)
            {
                unit *= 1000;
                suffix++;
            }

            long whole = amount / unit;
            long tenths = amount % unit / (unit / 10);
            string number = whole >= 100 || tenths == 0
                ? whole.ToString(CultureInfo.InvariantCulture)
                : whole.ToString(CultureInfo.InvariantCulture) + "." + tenths.ToString(CultureInfo.InvariantCulture);
            return number + Suffixes[suffix];
        }
    }
}
