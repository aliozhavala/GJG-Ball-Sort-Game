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
        /// A tube was completed at this point in the history (by a move, or by an undo). That tube
        /// is corked for good, which seals the history there — no earlier move can be reached.
        /// </summary>
        SealedByTubeCompletion,

        /// <summary>The player has no Undo boosters left in their inventory.</summary>
        NoBoostersLeft,

        /// <summary>The board is stuck; boosters are disabled until the player plays on or gives up.</summary>
        Stuck,
    }
}
