namespace BallSort.Core.Levels
{
    /// <summary>A veil over one tube; it opens when any tube completes in <see cref="Color"/>.</summary>
    public sealed class VeilDto
    {
        /// <summary>Ball colour id that opens the veil. Required; nullable so a missing value is reported, not read as colour 0.</summary>
        public int? Color { get; set; }
    }
}
