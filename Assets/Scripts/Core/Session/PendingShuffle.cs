using System;
using System.Threading;
using BallSort.Core.Model;

namespace BallSort.Core.Session
{
    /// <summary>
    /// A shuffle being searched for: a private copy of the board taken when it started, and the
    /// arrangement once <see cref="Search"/> finds one.
    /// </summary>
    /// <remarks>
    /// Lets the slow part run off the main thread: start it with
    /// <see cref="LevelSession.TryBeginShuffle"/> on the main thread, call <see cref="Search"/>
    /// anywhere (it touches only its own copy), then hand it back to
    /// <see cref="LevelSession.TryApplyShuffle"/> on the main thread.
    /// </remarks>
    public sealed class PendingShuffle
    {
        private readonly BoardState _snapshot;
        private int _searched;

        internal PendingShuffle(LevelSession session, BoardState snapshot, int revision)
        {
            Session = session;
            _snapshot = snapshot;
            Revision = revision;
        }

        /// <summary>True once <see cref="Search"/> has found a valid arrangement.</summary>
        public bool IsFound => Result != null;

        internal LevelSession Session { get; }

        /// <summary>The session's revision when the snapshot was taken; any later change invalidates it.</summary>
        internal int Revision { get; }

        /// <summary>The snapshot with the new arrangement, or null.</summary>
        internal BoardState Result { get; private set; }

        /// <summary>
        /// Searches for a valid arrangement of the snapshot. Safe on any thread; call it once.
        /// </summary>
        /// <param name="random">Source of the arrangements; used only during this call.</param>
        /// <param name="cancellationToken">Cancels the search, e.g. when the level is left.</param>
        /// <returns>True when an arrangement was found.</returns>
        /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled.</exception>
        public bool Search(Random random, CancellationToken cancellationToken = default)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (Interlocked.Exchange(ref _searched, 1) != 0)
            {
                throw new InvalidOperationException("This shuffle has already been searched.");
            }

            Result = BoardShuffler.Find(_snapshot, random, cancellationToken);
            return IsFound;
        }
    }
}
