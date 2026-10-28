namespace BallSort.Core.Model
{
    /// <summary>What, if anything, covers a tube. A covered tube is neither a move source nor a target.</summary>
    public enum CoverKind
    {
        /// <summary>The tube is uncovered and plays normally.</summary>
        None = 0,

        /// <summary>Opens only when a tube completes in the veil's own colour; other colours leave it closed.</summary>
        Veil,

        /// <summary>Opens when the key for this lock is found.</summary>
        Lock,
    }
}
