using System;
using BallSort.Core.Model;
using BallSort.Core.Rules;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class MoveRulesTests
    {
        private static MoveRejection RejectionFor(BoardState board, int from, int to)
        {
            MoveRules.TryGetMove(board, from, to, out _, out MoveRejection rejection);
            return rejection;
        }

        private static Tube Covered(CoverKind kind, int capacity, params Ball[] balls) =>
            kind == CoverKind.Veil ? MakeVeiledTube(0, capacity, balls) : MakeLockedTube(0, capacity, balls);

        // --- Legal moves -----------------------------------------------------------------------

        [Test]
        public void MoveOntoEmptyTube_IsLegal()
        {
            BoardState board = MakeBoard(MakeTube(4, R(2), R(1)), MakeEmptyTube(4));

            Assert.That(MoveRules.TryGetMove(board, 0, 1, out MoveInfo move, out MoveRejection rejection), Is.True);
            Assert.That(rejection, Is.EqualTo(MoveRejection.None));
            Assert.That(move.Count, Is.EqualTo(1));
            Assert.That(move.ColorId, Is.EqualTo(1));
            Assert.That(move.IsPartial, Is.False);
        }

        [Test]
        public void MoveOntoMatchingTopBall_IsLegal()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1), R(1)), MakeTube(4, R(1)));

            Assert.That(MoveRules.TryGetMove(board, 0, 1, out MoveInfo move), Is.True);
            Assert.That(move.Count, Is.EqualTo(2));
            Assert.That(move.ColorId, Is.EqualTo(1));
        }

        [Test]
        public void MovingAnEntireSingleColourTubeIntoAnEmptyTube_IsLegal()
        {
            // Pointless, and the solver prunes it, but the rules themselves permit it.
            BoardState board = MakeBoard(MakeTube(4, R(1), R(1)), MakeEmptyTube(4));

            Assert.That(MoveRules.IsLegal(board, 0, 1), Is.True);
        }

        // --- Partial moves ---------------------------------------------------------------------

        [Test]
        public void MovedCount_IsRunLengthCappedByTargetFreeSpace()
        {
            // Run of three, but only two slots free in the target.
            BoardState board = MakeBoard(
                MakeTube(4, R(1), R(1), R(1)),
                MakeTube(4, R(1), R(1)));

            Assert.That(MoveRules.TryGetMove(board, 0, 1, out MoveInfo move), Is.True);
            Assert.That(move.Count, Is.EqualTo(2));
            Assert.That(move.IsPartial, Is.True, "one ball of the run stays behind");
        }

        [Test]
        public void MovedCount_IsNotPartialWhenTheWholeRunFits()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1), R(1)), MakeTube(4, R(1)));

            Assert.That(MoveRules.TryGetMove(board, 0, 1, out MoveInfo move), Is.True);
            Assert.That(move.Count, Is.EqualTo(2));
            Assert.That(move.IsPartial, Is.False);
        }

        [Test]
        public void MovedCount_ExcludesHiddenBallsOfTheSameColour()
        {
            // Three colour-1 balls, but the bottom one is hidden, so only two can move.
            BoardState board = MakeBoard(MakeTube(4, H(1), R(1), R(1)), MakeEmptyTube(4));

            Assert.That(MoveRules.TryGetMove(board, 0, 1, out MoveInfo move), Is.True);
            Assert.That(move.Count, Is.EqualTo(2));
        }

        // --- Rejections ------------------------------------------------------------------------

        [Test]
        public void SourceEqualToTarget_IsRejected()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));

            Assert.That(RejectionFor(board, 0, 0), Is.EqualTo(MoveRejection.SameTube));
        }

        [Test]
        public void EmptySource_IsRejected()
        {
            BoardState board = MakeBoard(MakeEmptyTube(4), MakeTube(4, R(1)));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.SourceEmpty));
        }

        [Test]
        public void HiddenTopBallOnSource_IsRejected()
        {
            // Unreachable in normal play; guards against corrupt level data.
            BoardState board = MakeBoard(MakeTube(4, R(1), H(2)), MakeEmptyTube(4));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.SourceTopHidden));
        }

        [Test]
        public void CompletedSource_IsRejected()
        {
            BoardState board = MakeBoard(MakeTube(2, R(1), R(1)), MakeEmptyTube(4));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.SourceCompleted));
        }

        [Test]
        public void FullSingleColourSource_ThatIsNotTheWholeColour_CanStillMove()
        {
            // Four of five colour-1 balls fill tube 0, but the fifth is elsewhere, so it is not completed.
            BoardState board = MakeBoard(MakeTube(4, R(1), R(1), R(1), R(1)), MakeTube(4, R(1)));

            Assert.That(MoveRules.TryGetMove(board, 0, 1, out MoveInfo move), Is.True);
            Assert.That(move.Count, Is.EqualTo(3), "as many as fit");
        }

        [TestCase(CoverKind.Veil)]
        [TestCase(CoverKind.Lock)]
        public void CoveredSource_IsRejected(CoverKind kind)
        {
            BoardState board = MakeBoard(Covered(kind, 4, R(1)), MakeEmptyTube(4));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.SourceCovered));
        }

        [TestCase(CoverKind.Veil)]
        [TestCase(CoverKind.Lock)]
        public void CoveredTarget_IsRejected(CoverKind kind)
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), Covered(kind, 4, R(1)));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.TargetCovered));
        }

        [Test]
        public void CoveredEmptyTarget_IsRejected()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeLockedTube(0, 4));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.TargetCovered));
        }

        [Test]
        public void CoveredEmptySource_ReportsTheCoverFirst()
        {
            // The cover is what the player sees, so it is the reason given.
            BoardState board = MakeBoard(MakeVeiledTube(0, 4), MakeEmptyTube(4));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.SourceCovered));
        }

        [Test]
        public void ClaySource_IsRejected()
        {
            BoardState board = MakeBoard(MakeClayTube(4, R(1), R(1)), MakeEmptyTube(4));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.SourceClay));
        }

        [Test]
        public void ClayTarget_TakesAMatchingRun()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1), R(1)), MakeClayTube(5, R(1)));

            Assert.That(MoveRules.TryGetMove(board, 0, 1, out MoveInfo move), Is.True);
            Assert.That(move.Count, Is.EqualTo(2));
        }

        [Test]
        public void EmptyClayTarget_TakesAnyRun()
        {
            BoardState board = MakeBoard(MakeTube(4, R(2)), MakeClayTube(4));

            Assert.That(MoveRules.IsLegal(board, 0, 1), Is.True);
        }

        [Test]
        public void ClayTarget_StillRejectsAMismatchedColour()
        {
            BoardState board = MakeBoard(MakeTube(4, R(2)), MakeClayTube(4, R(1)));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.ColorMismatch));
        }

        [Test]
        public void FullTarget_IsRejected()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(2, R(1), R(1)));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.TargetFull));
        }

        [Test]
        public void CompletedTarget_IsRejectedAsFull()
        {
            // A completed tube is by definition full, so fullness is the reason reported. Documents
            // that completed tubes can never be a target either.
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(3, R(1), R(1), R(1)));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.TargetFull));
        }

        [Test]
        public void MismatchedTargetColour_IsRejected()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(4, R(2)));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.ColorMismatch));
        }

        [Test]
        public void HiddenTargetTopBall_IsRejectedEvenWhenTheConcealedColourMatches()
        {
            // Only reachable via corrupt data, but asserts we never stack onto a colour the player
            // cannot see — the hidden ball here is colour 1, the same as the incoming run.
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(4, R(1), H(1)));

            Assert.That(RejectionFor(board, 0, 1), Is.EqualTo(MoveRejection.TargetTopHidden));
        }

        [Test]
        public void OutOfRangeIndices_Throw()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));

            Assert.That(
                () => MoveRules.TryGetMove(board, 5, 0, out _, out _),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => MoveRules.TryGetMove(board, 0, -1, out _, out _),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NullBoard_Throws()
        {
            Assert.That(
                () => MoveRules.TryGetMove(null, 0, 1, out _, out _),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TryGetMove_LeavesTheBoardUnchanged()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1), R(1)), MakeEmptyTube(4));

            MoveRules.TryGetMove(board, 0, 1, out _);

            Assert.That(board[0].Count, Is.EqualTo(2));
            Assert.That(board[1].Count, Is.EqualTo(0));
        }

        // --- Selection and dead ends -----------------------------------------------------------

        [Test]
        public void CanSelect_RejectsEmptyCompletedCoveredClayAndHiddenTopTubes()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(1)),              // 0: selectable
                MakeEmptyTube(4),               // 1: empty
                MakeTube(2, R(2), R(2)),        // 2: completed
                MakeLockedTube(0, 4, R(3)),     // 3: locked
                MakeTube(4, R(4), H(4)),        // 4: hidden top
                MakeVeiledTube(1, 4, R(5)),     // 5: veiled
                MakeClayTube(4, R(6)));         // 6: clay

            Assert.That(MoveRules.CanSelect(board, 0), Is.True);
            for (int tube = 1; tube <= 6; tube++)
            {
                Assert.That(MoveRules.CanSelect(board, tube), Is.False, $"tube {tube}");
            }
        }

        [Test]
        public void HasAnyLegalMove_IsTrueWhenAMoveExists()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));

            Assert.That(MoveRules.HasAnyLegalMove(board), Is.True);
        }

        [Test]
        public void HasAnyLegalMove_IsFalseOnADeadEnd()
        {
            // Both tubes are full with mismatched tops, so nothing can move anywhere.
            BoardState board = MakeBoard(
                MakeTube(2, R(1), R(2)),
                MakeTube(2, R(2), R(1)));

            Assert.That(MoveRules.HasAnyLegalMove(board), Is.False);
        }

        [Test]
        public void HasAnyLegalMove_IsFalseWhenEveryTubeIsEmptyOrCompleted()
        {
            BoardState board = MakeBoard(MakeTube(2, R(1), R(1)), MakeEmptyTube(2));

            Assert.That(MoveRules.HasAnyLegalMove(board), Is.False);
        }
    }
}
