using System.Collections.Generic;
using BallSort.Core.Commands;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using BallSort.Core.Player;
using BallSort.Core.Rules;
using BallSort.Core.Session;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestLevels;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class LevelSessionTests
    {
        private const int AddedCapacity = 4;

        private static LevelSession Start(LevelDto level, int undo = 99, int shuffle = 99, int addTube = 99) =>
            Start(level, new BoosterInventory(undo, shuffle, addTube));

        private static LevelSession Start(LevelDto level, BoosterInventory boosters) =>
            new LevelSession(LevelFactory.Load(level, Name), boosters, AddedCapacity);

        /// <summary>
        /// Three colours in full capacity-2 tubes plus one empty capacity-1 tube. Moving tube 0's
        /// top into tube 3 leaves no legal move at all: tube 0's new top (colour 0) is buried in
        /// tube 1, every other tube is full, and tube 3 is corked.
        /// </summary>
        private static LevelDto OneMoveFromStuck() =>
            Level(3, T(2, 0, 1), T(2, 0, 2), T(2, 2, 1), T(1));

        /// <summary>One colour, one move from winning: tube 0 → tube 1 completes tube 1.</summary>
        private static LevelDto OneMoveFromWin() =>
            Level(1, T(2, 0), T(2, 0));

        // --- Moves -----------------------------------------------------------------------------

        [Test]
        public void TryMove_AppliesALegalMoveAndCountsIt()
        {
            LevelSession session = Start(Valid());

            Assert.That(session.TryMove(0, 2, out MoveRejection rejection), Is.True);

            Assert.That(rejection, Is.EqualTo(MoveRejection.None));
            Assert.That(session.MoveCount, Is.EqualTo(1));
            Assert.That(session.Board[2].Count, Is.EqualTo(1));
        }

        [Test]
        public void TryMove_RefusesAnIllegalMoveWithTheReason()
        {
            LevelSession session = Start(Valid());

            Assert.That(session.TryMove(0, 1, out MoveRejection rejection), Is.False);

            Assert.That(rejection, Is.EqualTo(MoveRejection.TargetFull));
            Assert.That(session.MoveCount, Is.EqualTo(0));
        }

        [Test]
        public void BoardEvents_AreForwardedBySession()
        {
            LevelSession session = Start(OneMoveFromWin());
            var log = new List<string>();
            session.MoveApplied += _ => log.Add("MoveApplied");
            session.TubeCompleted += _ => log.Add("TubeCompleted");
            session.LevelCompleted += () => log.Add("LevelCompleted");

            session.TryMove(0, 1, out _);

            Assert.That(log, Is.EqualTo(new[] { "MoveApplied", "TubeCompleted", "LevelCompleted" }));
            Assert.That(session.IsWon, Is.True);
        }

        [Test]
        public void Changed_FiresOnceAfterAMoveWithTheSealAlreadyRecorded()
        {
            LevelSession session = Start(Level(2, T(2, 0, 1), T(2, 1, 0), T(2), T(1)));
            var log = new List<string>();
            session.MoveApplied += _ => log.Add($"MoveApplied:{session.UndoBlockReason}");
            session.Changed += () => log.Add($"Changed:{session.UndoBlockReason}");

            session.TryMove(0, 3, out _);   // colour 1 corks the capacity-1 tube 3

            Assert.That(log, Is.EqualTo(new[]
            {
                "MoveApplied:NoMovesToUndo",   // mid-operation: the history has not recorded the move yet
                "Changed:SealedByTubeCompletion",
            }));
        }

        [Test]
        public void Changed_FiresAfterUndoAddTubePlayOnAndRestart_ButNotAfterARefusal()
        {
            LevelSession session = Start(OneMoveFromStuck());
            var changes = 0;
            session.Changed += () => changes++;

            session.TryMove(0, 1, out _);                         // refused: tube 1 is full
            Assert.That(changes, Is.EqualTo(0));

            session.TryMove(0, 3, out _);                         // move, now stuck
            session.TryPlayOn(new Wallet(PlayOnPrice), PlayOnPrice, out _);
            session.TryAddTube(out _);
            session.Restart();
            session.TryMove(0, 2, out _);   // tube 2 is full
            session.TryAddTube(out _);
            session.TryMove(0, 4, out _);
            session.TryUndo(out _);

            Assert.That(changes, Is.EqualTo(7));
        }

        // --- Undo ------------------------------------------------------------------------------

        [Test]
        public void Undo_AtStart_HasNothingToUndo()
        {
            LevelSession session = Start(Valid());

            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.NoMovesToUndo));
            Assert.That(session.TryUndo(out UndoBlockReason reason), Is.False);
            Assert.That(reason, Is.EqualTo(UndoBlockReason.NoMovesToUndo));
        }

        [Test]
        public void TryUndo_RevertsTheMoveAndSpendsOneUndoBooster()
        {
            var boosters = new BoosterInventory(undo: 3, shuffle: 0, addTube: 0);
            LevelSession session = Start(Valid(), boosters);
            session.TryMove(0, 2, out _);
            var changes = new List<int>();
            boosters.Changed += (type, count) => changes.Add(count);

            Assert.That(session.TryUndo(out UndoBlockReason reason), Is.True);

            Assert.That(reason, Is.EqualTo(UndoBlockReason.None));
            Assert.That(session.Board[2].IsEmpty, Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(0));
            Assert.That(boosters.GetCount(BoosterType.Undo), Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Undo_WithNoBoostersLeft_IsBlockedAndChangesNothing()
        {
            LevelSession session = Start(Valid(), undo: 0);
            session.TryMove(0, 2, out _);

            Assert.That(session.TryUndo(out UndoBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(UndoBlockReason.NoBoostersLeft));
            Assert.That(session.MoveCount, Is.EqualTo(1));
        }

        [Test]
        public void Undo_SealedByACompletion_OutranksNoBoostersLeft()
        {
            // Telling the player to find more undos when a corked tube is the real obstacle would
            // send them after a booster that cannot help.
            LevelSession session = Start(Level(2, T(2, 1, 0), T(2, 0), T(2, 1)), undo: 0);
            session.TryMove(0, 1, out _);   // completes tube 1

            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion));
        }

        [Test]
        public void Undo_ThatCompletesATube_FiresTubeCompletedAndSeals()
        {
            // Tube 0 is full and single-coloured but hides its bottom ball, so it is not complete.
            LevelSession session = Start(Level(2,
                Hidden(4, new[] { 1, 1, 1, 1 }, new[] { true, false, false, false }),
                T(4, 0, 0, 0),
                T(4, 0),
                T(4),
                T(4)));
            var completed = new List<int>();
            session.TubeCompleted += e => completed.Add(e.TubeIndex);
            session.TryMove(2, 4, out _);   // an earlier, ordinary move
            session.TryMove(0, 3, out _);   // reveals tube 0's bottom ball

            Assert.That(session.TryUndo(out _), Is.True);

            Assert.That(completed, Is.EqualTo(new[] { 0 }));
            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion),
                "the earlier move is now behind the seal");
        }

        [Test]
        public void Undo_AfterAnUndoCorksATube_CannotReopenItAndSpendsNothing()
        {
            var boosters = new BoosterInventory(undo: 5, shuffle: 0, addTube: 0);
            LevelSession session = Start(Level(2,
                Hidden(4, new[] { 1, 1, 1 }, new[] { true, false, false }),   // 0: one ball short
                T(4, 0, 0, 0, 1),                                             // 1: supplies the fourth 1
                T(4, 0),
                T(4)), boosters);
            session.TryMove(1, 0, out _);   // fills tube 0, still hiding a ball
            session.TryMove(0, 3, out _);   // reveals tube 0's bottom ball
            session.TryUndo(out _);         // corks tube 0

            Assert.That(session.TryUndo(out UndoBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion));
            Assert.That(session.Board[0].IsCompleted, Is.True);
            Assert.That(session.Board[0].Count, Is.EqualTo(4));
            Assert.That(boosters.GetCount(BoosterType.Undo), Is.EqualTo(4), "only the successful undo was paid for");
        }

        // --- Add tube --------------------------------------------------------------------------

        [Test]
        public void TryAddTube_AppendsAnEmptyTubeAndSpendsOneBooster()
        {
            var boosters = new BoosterInventory(undo: 0, shuffle: 0, addTube: 2);
            LevelSession session = Start(Valid(), boosters);
            TubeAddedEventArgs received = default;
            session.TubeAdded += e => received = e;

            Assert.That(session.TryAddTube(out AddTubeBlockReason reason), Is.True);

            Assert.That(reason, Is.EqualTo(AddTubeBlockReason.None));
            Assert.That(session.Board.TubeCount, Is.EqualTo(4));
            Assert.That(session.Board[3].IsEmpty, Is.True);
            Assert.That(session.Board[3].Capacity, Is.EqualTo(AddedCapacity));
            Assert.That(received.TubeIndex, Is.EqualTo(3));
            Assert.That(session.Rows, Is.EqualTo(new[] { 4 }));
            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(1));
        }

        [Test]
        public void TubeAdded_FiresAfterRowsAlreadyIncludeTheNewTube()
        {
            LevelSession session = Start(Valid());
            int rowTotalSeen = -1;
            session.TubeAdded += _ =>
            {
                rowTotalSeen = 0;
                foreach (int row in session.Rows)
                {
                    rowTotalSeen += row;
                }
            };

            session.TryAddTube(out _);

            Assert.That(rowTotalSeen, Is.EqualTo(session.Board.TubeCount));
        }

        [Test]
        public void TryAddTube_IsNotUndoableAndDoesNotSeal()
        {
            LevelSession session = Start(Valid());
            session.TryMove(0, 2, out _);

            session.TryAddTube(out _);

            Assert.That(session.MoveCount, Is.EqualTo(1), "adding a tube is not a move");
            Assert.That(session.TryUndo(out _), Is.True, "the earlier move is still undoable");
            Assert.That(session.Board.TubeCount, Is.EqualTo(4), "and undo leaves the added tube alone");
        }

        [Test]
        public void TryAddTube_WithNoBoostersLeft_IsBlocked()
        {
            LevelSession session = Start(Valid(), addTube: 0);

            Assert.That(session.TryAddTube(out AddTubeBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(AddTubeBlockReason.NoBoostersLeft));
            Assert.That(session.Board.TubeCount, Is.EqualTo(3));
        }

        [Test]
        public void TryAddTube_StopsAtEighteenTubesForAnAutomaticLayout()
        {
            LevelSession session = Start(Valid());   // 3 tubes, one row

            int added = 0;
            while (session.TryAddTube(out _))
            {
                added++;
            }

            Assert.That(added, Is.EqualTo(LevelLayout.MaxTubes - 3));
            Assert.That(session.Board.TubeCount, Is.EqualTo(18));
            Assert.That(session.Rows, Is.EqualTo(new[] { 6, 6, 6 }));
            Assert.That(session.AddTubeBlockReason, Is.EqualTo(AddTubeBlockReason.NoRoom));
        }

        [Test]
        public void TryAddTube_StopsWhenAnExplicitLayoutRunsOutOfRows()
        {
            LevelDto level = Valid();
            level.Layout = new LayoutDto { Rows = new List<int> { 2, 1 } };
            LevelSession session = Start(level);

            while (session.TryAddTube(out _))
            {
            }

            Assert.That(session.Rows, Is.EqualTo(new[] { 2, 6, 6 }), "existing rows never reshuffle");
            Assert.That(session.Board.TubeCount, Is.EqualTo(14));
            Assert.That(session.AddTubeBlockReason, Is.EqualTo(AddTubeBlockReason.NoRoom));
        }

        [Test]
        public void NoRoom_OutranksNoBoostersLeft()
        {
            LevelDto level = Valid();
            level.Layout = new LayoutDto { Rows = new List<int> { 1, 1, 1 } };
            var boosters = new BoosterInventory(undo: 0, shuffle: 0, addTube: 5);
            LevelSession session = Start(level, boosters);
            while (session.TryAddTube(out _))
            {
            }

            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(0));
            Assert.That(session.Rows, Is.EqualTo(new[] { 1, 1, 6 }));
            Assert.That(session.AddTubeBlockReason, Is.EqualTo(AddTubeBlockReason.NoRoom));
        }

        [Test]
        public void TryAddTube_AfterTheWin_IsBlocked()
        {
            LevelSession session = Start(OneMoveFromWin());
            session.TryMove(0, 1, out _);

            Assert.That(session.TryAddTube(out AddTubeBlockReason reason), Is.False);
            Assert.That(reason, Is.EqualTo(AddTubeBlockReason.LevelWon));
        }

        // --- Shuffle ---------------------------------------------------------------------------

        [Test]
        public void TryShuffle_IsNotAvailableYetAndSpendsNothing()
        {
            var boosters = new BoosterInventory(undo: 0, shuffle: 3, addTube: 0);
            LevelSession session = Start(Valid(), boosters);

            Assert.That(session.TryShuffle(out ShuffleBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(ShuffleBlockReason.NotAvailable));
            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(3));
        }

        [Test]
        public void TryShuffle_AfterTheWin_IsBlocked()
        {
            LevelSession session = Start(OneMoveFromWin());
            session.TryMove(0, 1, out _);

            Assert.That(session.ShuffleBlockReason, Is.EqualTo(ShuffleBlockReason.LevelWon));
        }

        // --- Stuck -----------------------------------------------------------------------------

        [Test]
        public void AFreshLevel_IsNotStuck()
        {
            Assert.That(Start(OneMoveFromStuck()).IsStuck, Is.False);
        }

        [Test]
        public void AMoveThatLeavesNoLegalMove_MakesTheSessionStuck()
        {
            LevelSession session = Start(OneMoveFromStuck());
            var log = new List<string>();
            session.MoveApplied += _ => log.Add("MoveApplied");
            session.StuckChanged += stuck => log.Add($"StuckChanged({stuck})");

            session.TryMove(0, 3, out _);

            Assert.That(session.IsStuck, Is.True);
            Assert.That(log, Is.EqualTo(new[] { "MoveApplied", "StuckChanged(True)" }),
                "the stuck notice comes after the move that caused it");
        }

        [Test]
        public void AWonLevel_IsNeverStuck()
        {
            LevelSession session = Start(OneMoveFromWin());
            var stuckChanges = 0;
            session.StuckChanged += _ => stuckChanges++;

            session.TryMove(0, 1, out _);

            Assert.That(session.IsStuck, Is.False, "no legal move is left, but the level is won");
            Assert.That(stuckChanges, Is.EqualTo(0));
        }

        [Test]
        public void WhileStuck_EveryBoosterIsBlocked()
        {
            var boosters = new BoosterInventory(undo: 5, shuffle: 5, addTube: 5);
            LevelSession session = Start(OneMoveFromStuck(), boosters);
            session.TryMove(0, 3, out _);

            Assert.That(session.TryUndo(out UndoBlockReason undo), Is.False);
            Assert.That(session.TryAddTube(out AddTubeBlockReason addTube), Is.False);
            Assert.That(session.TryShuffle(out ShuffleBlockReason shuffle), Is.False);

            Assert.That(undo, Is.EqualTo(UndoBlockReason.Stuck));
            Assert.That(addTube, Is.EqualTo(AddTubeBlockReason.Stuck));
            Assert.That(shuffle, Is.EqualTo(ShuffleBlockReason.Stuck));
            Assert.That(boosters.GetCount(BoosterType.Undo), Is.EqualTo(5));
            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(5));
            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(5));
        }

        [Test]
        public void Restart_ClearsTheStuckState()
        {
            LevelSession session = Start(OneMoveFromStuck());
            session.TryMove(0, 3, out _);
            var log = new List<string>();
            session.Restarted += () => log.Add("Restarted");
            session.StuckChanged += stuck => log.Add($"StuckChanged({stuck})");

            session.Restart();

            Assert.That(session.IsStuck, Is.False);
            Assert.That(log, Is.EqualTo(new[] { "Restarted", "StuckChanged(False)" }));
        }

        // --- Play on ---------------------------------------------------------------------------

        private const int PlayOnPrice = 900;

        /// <summary>
        /// <see cref="OneMoveFromStuck"/> padded with fourteen full, mixed, inert tubes to the
        /// 18-tube limit, so once it is stuck there is no room for another tube.
        /// </summary>
        private static LevelDto OneMoveFromStuckWithAFullLayout()
        {
            LevelDto level = OneMoveFromStuck();
            int colors = level.Colors;
            for (int pair = 0; pair < 7; pair++, colors += 2)
            {
                level.Tubes.Add(T(2, colors, colors + 1));
                level.Tubes.Add(T(2, colors + 1, colors));
            }

            level.Colors = colors;
            return level;
        }

        [Test]
        public void TryPlayOn_WhileStuck_ChargesAddsATubeAndClearsStuck()
        {
            LevelSession session = Start(OneMoveFromStuck());
            session.TryMove(0, 3, out _);
            var wallet = new Wallet(1000);
            var log = new List<string>();
            session.TubeAdded += e => log.Add($"TubeAdded({e.TubeIndex})");
            session.StuckChanged += stuck => log.Add($"StuckChanged({stuck})");

            Assert.That(session.TryPlayOn(wallet, PlayOnPrice, out PlayOnBlockReason reason), Is.True);

            Assert.That(reason, Is.EqualTo(PlayOnBlockReason.None));
            Assert.That(wallet.Coins, Is.EqualTo(100));
            Assert.That(session.Board.TubeCount, Is.EqualTo(5));
            Assert.That(session.Board[4].Capacity, Is.EqualTo(AddedCapacity));
            Assert.That(session.Rows, Is.EqualTo(new[] { 5 }));
            Assert.That(session.IsStuck, Is.False);
            Assert.That(log, Is.EqualTo(new[] { "TubeAdded(4)", "StuckChanged(False)" }));
        }

        [Test]
        public void TryPlayOn_IgnoresTheBoosterInventoryAndTheHistory()
        {
            var boosters = new BoosterInventory(undo: 0, shuffle: 0, addTube: 0);
            LevelSession session = Start(OneMoveFromStuck(), boosters);
            session.TryMove(0, 3, out _);

            Assert.That(session.TryPlayOn(new Wallet(PlayOnPrice), PlayOnPrice, out _), Is.True);

            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(0));
            Assert.That(session.MoveCount, Is.EqualTo(1), "Play on is not a move");
        }

        [Test]
        public void TryPlayOn_WhenNotStuck_IsRefusedAndChargesNothing()
        {
            LevelSession session = Start(Valid());
            var wallet = new Wallet(1000);

            Assert.That(session.TryPlayOn(wallet, PlayOnPrice, out PlayOnBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(PlayOnBlockReason.NotStuck));
            Assert.That(wallet.Coins, Is.EqualTo(1000));
            Assert.That(session.Board.TubeCount, Is.EqualTo(3));
        }

        [Test]
        public void TryPlayOn_WithoutEnoughCoins_IsRefusedAndStaysStuck()
        {
            LevelSession session = Start(OneMoveFromStuck());
            session.TryMove(0, 3, out _);
            var wallet = new Wallet(PlayOnPrice - 1);

            Assert.That(session.TryPlayOn(wallet, PlayOnPrice, out PlayOnBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(PlayOnBlockReason.NotEnoughCoins));
            Assert.That(wallet.Coins, Is.EqualTo(PlayOnPrice - 1));
            Assert.That(session.Board.TubeCount, Is.EqualTo(4));
            Assert.That(session.IsStuck, Is.True);
        }

        [Test]
        public void TryPlayOn_WithAFullLayout_IsRefusedAsNoRoom()
        {
            LevelSession session = Start(OneMoveFromStuckWithAFullLayout());
            session.TryMove(0, 3, out _);
            Assert.That(session.IsStuck, Is.True, "test setup: the padded board is stuck");
            Assert.That(session.Board.TubeCount, Is.EqualTo(LevelLayout.MaxTubes), "test setup: the board is full");
            var wallet = new Wallet(0);

            Assert.That(session.TryPlayOn(wallet, PlayOnPrice, out PlayOnBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(PlayOnBlockReason.NoRoom), "no room outranks the coin shortage");
            Assert.That(session.Board.TubeCount, Is.EqualTo(LevelLayout.MaxTubes));
        }

        [Test]
        public void GetPlayOnBlockReason_FollowsTheWallet()
        {
            LevelSession session = Start(OneMoveFromStuck());
            session.TryMove(0, 3, out _);
            var wallet = new Wallet(PlayOnPrice - 1);
            Assert.That(session.GetPlayOnBlockReason(wallet, PlayOnPrice), Is.EqualTo(PlayOnBlockReason.NotEnoughCoins));

            wallet.Add(1);

            Assert.That(session.GetPlayOnBlockReason(wallet, PlayOnPrice), Is.EqualTo(PlayOnBlockReason.None));
        }

        // --- Selection -------------------------------------------------------------------------

        [Test]
        public void CanSelect_OnlyTubesThatCanStartAMove()
        {
            LevelSession session = Start(OneMoveFromWin());
            Assert.That(session.CanSelect(0), Is.True);

            session.TryMove(0, 1, out _);

            Assert.That(session.CanSelect(0), Is.False, "empty");
            Assert.That(session.CanSelect(1), Is.False, "completed");
        }

        // --- Restart ---------------------------------------------------------------------------

        [Test]
        public void Restart_RehidesBallsDropsAddedTubesAndClearsHistory()
        {
            var boosters = new BoosterInventory(undo: 5, shuffle: 0, addTube: 5);
            LevelSession session = Start(
                Level(2, Hidden(2, new[] { 1, 0 }, new[] { true, false }), T(2, 1, 0), T(2)),
                boosters);
            BoardState before = session.Board;
            session.TryMove(0, 2, out _);   // reveals tube 0's bottom ball
            session.TryAddTube(out _);
            Assert.That(session.Board[0][0].IsRevealed, Is.True, "test setup: the ball was revealed");

            session.Restart();

            Assert.That(session.Board, Is.Not.SameAs(before), "restart rebuilds from source data");
            Assert.That(session.Board[0][0].IsHidden, Is.True, "hidden balls are hidden again");
            Assert.That(session.Board.TubeCount, Is.EqualTo(3), "added tubes are gone");
            Assert.That(session.Rows, Is.EqualTo(new[] { 3 }));
            Assert.That(session.MoveCount, Is.EqualTo(0));
            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.NoMovesToUndo));
            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(4), "spent boosters stay spent");
        }

        [Test]
        public void Restart_KeepsForwardingEventsFromTheNewBoard()
        {
            LevelSession session = Start(Valid());
            var moves = 0;
            session.MoveApplied += _ => moves++;

            session.Restart();
            session.TryMove(0, 2, out _);

            Assert.That(moves, Is.EqualTo(1));
        }
    }
}
