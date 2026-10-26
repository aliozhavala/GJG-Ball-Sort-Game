namespace BallSort.Core.Commands
{
    /// <summary>
    /// Why undo is unavailable. Distinct reasons so the UI can give the player a straight answer
    /// instead of just greying the button out.
    /// </summary>
    public enum UndoBlockReason
    {
        /// <summary>Undo is available.</summary>
        None = 0,

        /// <summary>No moves have been made yet, or every undoable move has been undone.</summary>
        NoMovesToUndo,

        /// <summary>
        /// The next move to undo completed a tube. That tube is corked for good, which seals the
        /// history at that point — no earlier move can be reached either.
        /// </summary>
        SealedByTubeCompletion,

        /// <summary>The player has spent every undo charge.</summary>
        NoChargesLeft,
    }
}
