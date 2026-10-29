namespace BallSort.Core.Levels
{
    /// <summary>
    /// Design metadata of one variant, written by the level tools and the designer. Gameplay never reads it.
    /// </summary>
    public sealed class LevelMetaDto
    {
        /// <summary>Length of the shortest known solution, if computed (Validate Levels writes it).</summary>
        public int? OptimalMoves { get; set; }

        /// <summary>Normalised difficulty score in <c>0 .. 1</c>, if set.</summary>
        public double? Difficulty { get; set; }

        /// <summary>Free-text design notes.</summary>
        public string Notes { get; set; }
    }
}
