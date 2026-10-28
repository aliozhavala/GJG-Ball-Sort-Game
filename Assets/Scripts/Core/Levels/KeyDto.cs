namespace BallSort.Core.Levels
{
    /// <summary>A key on one ball of a tube; finding it opens the lock with id <see cref="Lock"/>.</summary>
    public sealed class KeyDto
    {
        /// <summary>Index into the tube's <c>balls</c>, <c>0</c> = bottom. Required; nullable so a missing value is reported.</summary>
        public int? Slot { get; set; }

        /// <summary>Id of the lock this key opens. Required; nullable so a missing value is reported.</summary>
        public int? Lock { get; set; }
    }
}
