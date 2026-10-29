using System;
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
        private const int PlayOnPrice = 900;

        private static LevelSession Start(TestLevel level, int undo = 99, int shuffle = 99, int addTube = 99) =>
            Start(level, new BoosterInventory(undo, shuffle, addTube));

        private static LevelSession Start(TestLevel level, BoosterInventory boosters) =>
            new LevelSession(Load(level), 1, boosters, AddedCapacity);

        /// <summary>
        /// Five tubes in one row. Moving tube 0's top (colour 2) onto tube 3 fills tube 3 and leaves
        /// no legal move: only tube 0 (now topped by 1s) and tube 4 (a lone 0) have free slots, and no
        /// other tube shows either colour on top.
        /// </summary>
        private static TestLevel OneMoveFromStuck() =>
            Level(4, T(4, 0, 1, 1, 2), T(4, 3, 3, 3, 2), T(4, 1, 0, 0, 3), T(4, 1, 2, 2), T(4, 0));

        /// <summary>One colour, one move from winning: tube 0 → tube 1 completes tube 1.</summary>
        private static TestLevel OneMoveFromWin() =>
            Level(1, T(4, 0, 0), T(4, 0, 0));

        /// <summary>Tube 0 → tube 1 completes colour 1 in tube 1 (and seals the history).</summary>
        private static TestLevel OneMoveFromACompletion() =>
            Level(2, T(4, 0, 1), T(4, 1, 1, 1), T(4, 0, 0, 0));

        /// <summary>Tube 0 → tube 1 exposes the key for lock 0, which covers tubes 2 and 3.</summary>
        private static TestLevel OneMoveFromALock() =>
            Level(3, T(4, 1, 2, 0, 0).WithKey(1, 0), T(4), T(4, 2, 1, 2, 1), T(4, 0, 0, 1, 2), T(4))
                .WithLock(0, 2, 3);

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
        public void TryMove_FromAClayTube_IsRefused()
        {
            LevelSession session = Start(StuckBehindCovers());

            Assert.That(session.CanSelect(1), Is.False);
            Assert.That(session.TryMove(1, 4, out MoveRejection rejection), Is.False);
            Assert.That(rejection, Is.EqualTo(MoveRejection.SourceClay));
        }

        [Test]
        public void TryMove_OntoACoveredTube_IsRefused()
        {
            LevelSession session = Start(StuckBehindCovers());

            Assert.That(session.CanSelect(0), Is.False);
            Assert.That(session.TryMove(2, 0, out MoveRejection rejection), Is.False);
            Assert.That(rejection, Is.EqualTo(MoveRejection.TargetCovered));
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
        public void CoverEvents_AreForwarded_BeforeStuckChangedAndChanged()
        {
            LevelSession session = Start(OneMoveFromALock());
            var log = new List<string>();
            session.MoveApplied += _ => log.Add("MoveApplied");
            session.KeyFound += e => log.Add($"KeyFound({e.TubeIndex},{e.SlotIndex})");
            session.LockOpened += e => log.Add($"LockOpened({string.Join(",", e.TubeIndices)})");
            session.Changed += () => log.Add("Changed");

            session.TryMove(0, 1, out _);

            Assert.That(log, Is.EqualTo(new[] { "MoveApplied", "KeyFound(0,1)", "LockOpened(2,3)", "Changed" }));
            Assert.That(session.Board[2].IsCovered || session.Board[3].IsCovered, Is.False);
        }

        [Test]
        public void TubeUnveiled_IsForwarded()
        {
            LevelSession session = Start(Level(2,
                T(4, 0, 0, 0), T(4, 1, 0), T(4, 1, 1, 1).WithVeil(0), T(4)));
            var unveiled = new List<int>();
            session.TubeUnveiled += e => unveiled.Add(e.TubeIndex);

            session.TryMove(1, 0, out _);   // completes colour 0

            Assert.That(unveiled, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Changed_FiresOnceAfterAMoveWithTheSealAlreadyRecorded()
        {
            LevelSession session = Start(OneMoveFromACompletion());
            var log = new List<string>();
            session.MoveApplied += _ => log.Add($"MoveApplied:{session.UndoBlockReason}");
            session.Changed += () => log.Add($"Changed:{session.UndoBlockReason}");

            session.TryMove(0, 1, out _);

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
            session.Restart(1);
            session.TryMove(0, 2, out _);                         // refused: tube 2 is full
            session.TryAddTube(out _);
            session.TryMove(0, 5, out _);                         // into the added tube
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
            LevelSession session = Start(OneMoveFromACompletion(), undo: 0);
            session.TryMove(0, 1, out _);

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
            Assert.That(session.Board.IsCompleted(0), Is.True);
            Assert.That(session.Board[0].Count, Is.EqualTo(4));
            Assert.That(boosters.GetCount(BoosterType.Undo), Is.EqualTo(4), "only the successful undo was paid for");
        }

        [Test]
        public void Undo_AfterALockOpened_KeepsItOpen()
        {
            LevelSession session = Start(OneMoveFromALock());
            session.TryMove(0, 1, out _);

            Assert.That(session.TryUndo(out _), Is.True);

            Assert.That(session.Board[0].Top.ColorId, Is.EqualTo(0), "the run is back on the key's ball");
            Assert.That(session.Board[0][1].HasKey, Is.False);
            Assert.That(session.Board[2].IsCovered || session.Board[3].IsCovered, Is.False);
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
            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(1));
            Assert.That((received.TubeIndex, received.Capacity, received.Row, received.Cell), Is.EqualTo((3, AddedCapacity, 0, 3)));
            Assert.That(session.Grid.GetCellCount(0), Is.EqualTo(4));
            Assert.That(session.Grid.GetTubeAt(0, 3), Is.EqualTo(3));
        }

        [Test]
        public void TubeAdded_FiresWithTheBoardAndGridAlreadyIncludingTheTube()
        {
            LevelSession session = Start(Valid());
            int boardTubes = -1;
            int gridTubes = -1;
            session.TubeAdded += _ =>
            {
                boardTubes = session.Board.TubeCount;
                gridTubes = session.Grid.TubeCount;
            };

            session.TryAddTube(out _);

            Assert.That(boardTubes, Is.EqualTo(4));
            Assert.That(gridTubes, Is.EqualTo(4));
        }

        [Test]
        public void TubeAdded_ReportsTheRowWithTheFewestCells_TiesGoingToTheBottom()
        {
            // Rows of 1 and 2 cells: top (fewer), then bottom (tie), then top again.
            LevelSession session = Start(Valid().WithGrid(new int?[] { 0 }, new int?[] { 1, 2 }));
            var placements = new List<(int Row, int Cell)>();
            session.TubeAdded += e => placements.Add((e.Row, e.Cell));

            session.TryAddTube(out _);
            session.TryAddTube(out _);
            session.TryAddTube(out _);

            Assert.That(placements, Is.EqualTo(new[] { (0, 1), (1, 2), (0, 2) }));
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
        public void TryAddTube_GrowsARowToEightCells_ThenHasNoRoom()
        {
            LevelSession session = Start(Valid());   // 3 tubes, one row

            int added = 0;
            while (session.TryAddTube(out _))
            {
                added++;
            }

            Assert.That(added, Is.EqualTo(LevelLayout.MaxCellsPerRowInPlay - 3));
            Assert.That(session.Grid.RowCount, Is.EqualTo(1), "rows are never added");
            Assert.That(session.Grid.GetCellCount(0), Is.EqualTo(8));
            Assert.That(session.AddTubeBlockReason, Is.EqualTo(AddTubeBlockReason.NoRoom));
        }

        [Test]
        public void NoRoom_OutranksNoBoostersLeft()
        {
            // Three one-cell rows take exactly 21 tubes to fill to eight cells each.
            var boosters = new BoosterInventory(undo: 0, shuffle: 0, addTube: 21);
            LevelSession session = Start(Valid().WithGrid(new int?[] { 0 }, new int?[] { 1 }, new int?[] { 2 }), boosters);
            while (session.TryAddTube(out _))
            {
            }

            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(0));
            for (int row = 0; row < 3; row++)
            {
                Assert.That(session.Grid.GetCellCount(row), Is.EqualTo(8), $"row {row}");
            }

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

        // --- Shuffle (the rules themselves are in ShuffleTests) ------------------------------------

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
        public void TheStuckTrapFixture_IsStuckAfterTwoMoves()
        {
            // The former shipped level 6, kept as a fixture for the stuck / Play on flow.
            LevelSession session = Start(StuckTrap());

            session.TryMove(1, 4, out _);
            Assert.That(session.IsStuck, Is.False);
            session.TryMove(0, 1, out _);

            Assert.That(session.IsStuck, Is.True);
        }

        [Test]
        public void Stuck_CountsCoveredAndClayTubesAsUnmovable_EvenWithEmptyTubesLeft()
        {
            LevelSession session = Start(StuckBehindCovers());

            session.TryMove(3, 2, out _);   // completes colour 0

            Assert.That(session.Board[4].IsEmpty && session.Board[5].IsEmpty, Is.True);
            Assert.That(session.IsWon, Is.False);
            Assert.That(session.IsStuck, Is.True, "nothing can start a move");
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
        public void ACompletionThatWins_AlsoOpensTheVeilsOfItsColour()
        {
            // Every colour completes on the way to a win, so no veil is left closed at the end.
            LevelSession session = Start(Level(1, T(4, 0, 0), T(4, 0, 0), T(4).WithVeil(0)));

            session.TryMove(0, 1, out _);

            Assert.That(session.IsWon, Is.True);
            Assert.That(session.Board[2].IsCovered, Is.False);
            Assert.That(session.IsStuck, Is.False);
        }

        [Test]
        public void WhileStuck_EveryBoosterIsBlocked()
        {
            var boosters = new BoosterInventory(undo: 5, shuffle: 5, addTube: 5);
            LevelSession session = Start(OneMoveFromStuck(), boosters);
            session.TryMove(0, 3, out _);

            Assert.That(session.TryUndo(out UndoBlockReason undo), Is.False);
            Assert.That(session.TryAddTube(out AddTubeBlockReason addTube), Is.False);
            Assert.That(session.TryShuffle(new System.Random(1), out ShuffleBlockReason shuffle), Is.False);

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

            session.Restart(1);

            Assert.That(session.IsStuck, Is.False);
            Assert.That(log, Is.EqualTo(new[] { "Restarted", "StuckChanged(False)" }));
        }

        // --- Play on ---------------------------------------------------------------------------

        [Test]
        public void TryPlayOn_WhileStuck_ChargesAddsATubeAndClearsStuck()
        {
            LevelSession session = Start(OneMoveFromStuck());
            session.TryMove(0, 3, out _);
            var wallet = new Wallet(1000);
            var log = new List<string>();
            session.TubeAdded += e => log.Add($"TubeAdded({e.TubeIndex}@{e.Row},{e.Cell})");
            session.StuckChanged += stuck => log.Add($"StuckChanged({stuck})");

            Assert.That(session.TryPlayOn(wallet, PlayOnPrice, out PlayOnBlockReason reason), Is.True);

            Assert.That(reason, Is.EqualTo(PlayOnBlockReason.None));
            Assert.That(wallet.Coins, Is.EqualTo(100));
            Assert.That(session.Board.TubeCount, Is.EqualTo(6));
            Assert.That(session.Board[5].Capacity, Is.EqualTo(AddedCapacity));
            Assert.That(session.Grid.GetCellCount(0), Is.EqualTo(6));
            Assert.That(session.IsStuck, Is.False);
            Assert.That(log, Is.EqualTo(new[] { "TubeAdded(5@0,5)", "StuckChanged(False)" }));
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
            Assert.That(session.Board.TubeCount, Is.EqualTo(5));
            Assert.That(session.IsStuck, Is.True);
        }

        [Test]
        public void TryPlayOn_WhenEveryRowHasEightCells_IsRefusedAsNoRoom()
        {
            LevelSession session = Start(StuckBehindCovers());   // one row of six
            session.TryAddTube(out _);
            session.TryAddTube(out _);
            session.TryMove(3, 2, out _);
            Assert.That(session.IsStuck, Is.True, "test setup: the board is stuck");
            Assert.That(session.Grid.GetCellCount(0), Is.EqualTo(8), "test setup: the row is full");
            var wallet = new Wallet(0);

            Assert.That(session.TryPlayOn(wallet, PlayOnPrice, out PlayOnBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(PlayOnBlockReason.NoRoom), "no room outranks the coin shortage");
            Assert.That(session.Board.TubeCount, Is.EqualTo(8));
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
                Level(2, Hidden(4, new[] { 1, 0 }, new[] { true, false }), T(4, 1, 1, 1, 0), T(4, 0, 0)),
                boosters);
            BoardState before = session.Board;
            BoardGrid gridBefore = session.Grid;
            session.TryMove(0, 2, out _);   // reveals tube 0's bottom ball
            session.TryAddTube(out _);
            Assert.That(session.Board[0][0].IsRevealed, Is.True, "test setup: the ball was revealed");

            session.Restart(1);

            Assert.That(session.Board, Is.Not.SameAs(before), "restart rebuilds from source data");
            Assert.That(session.Grid, Is.Not.SameAs(gridBefore));
            Assert.That(session.Board[0][0].IsHidden, Is.True, "hidden balls are hidden again");
            Assert.That(session.Board.TubeCount, Is.EqualTo(3), "added tubes are gone");
            Assert.That(session.Grid.GetCellCount(0), Is.EqualTo(3));
            Assert.That(session.MoveCount, Is.EqualTo(0));
            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.NoMovesToUndo));
            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(4), "spent boosters stay spent");
        }

        [Test]
        public void Restart_PutsCoversAndKeysBack()
        {
            LevelSession session = Start(OneMoveFromALock());
            session.TryMove(0, 1, out _);
            Assert.That(session.Board[2].IsCovered, Is.False, "test setup: the lock opened");

            session.Restart(1);

            Assert.That(session.Board[2].Cover, Is.EqualTo(TubeCover.Lock(0)));
            Assert.That(session.Board[3].Cover, Is.EqualTo(TubeCover.Lock(0)));
            Assert.That(session.Board[0][1].HasKey, Is.True);
        }

        [Test]
        public void Restart_KeepsForwardingEventsFromTheNewBoard()
        {
            LevelSession session = Start(Valid());
            var moves = 0;
            session.MoveApplied += _ => moves++;

            session.Restart(1);
            session.TryMove(0, 2, out _);

            Assert.That(moves, Is.EqualTo(1));
        }

        // --- Variants --------------------------------------------------------------------------

        /// <summary><see cref="TestLevels.Valid"/> with V3's two full tubes swapped, so V1 and V3 differ.</summary>
        private static LoadedLevel TwoDifferentVariants()
        {
            LevelDto file = Valid().ToFile();
            List<int> first = file.Variants[2].Tubes[0].Balls;
            file.Variants[2].Tubes[0].Balls = file.Variants[2].Tubes[1].Balls;
            file.Variants[2].Tubes[1].Balls = first;
            return LevelFactory.Load(file, Name);
        }

        [Test]
        public void Constructor_PlaysTheGivenVariant()
        {
            var session = new LevelSession(TwoDifferentVariants(), 3, new BoosterInventory(0, 0, 0), AddedCapacity);

            Assert.That(session.Variant, Is.EqualTo(3));
            Assert.That(session.Board[0][0].ColorId, Is.EqualTo(1), "V3 starts with tube 1's balls in tube 0");
        }

        [TestCase(0)]
        [TestCase(6)]
        public void Constructor_RejectsAVariantTheLevelDoesNotHave(int variant)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LevelSession(TwoDifferentVariants(), variant, new BoosterInventory(0, 0, 0), AddedCapacity));
        }

        [Test]
        public void Restart_IntoAnotherVariant_SwapsInItsBoard_OnTheSameGrid()
        {
            var session = new LevelSession(TwoDifferentVariants(), 1, new BoosterInventory(0, 0, 0), AddedCapacity);
            var restarted = 0;
            session.Restarted += () => restarted++;
            session.TryMove(0, 2, out _);
            session.TryAddTube(out _);

            session.Restart(3);

            Assert.That(session.Variant, Is.EqualTo(3));
            Assert.That(restarted, Is.EqualTo(1));
            Assert.That(session.Board[0][0].ColorId, Is.EqualTo(1));
            Assert.That(session.Board.TubeCount, Is.EqualTo(3), "added tubes are gone");
            Assert.That(session.Grid.GetCellCount(0), Is.EqualTo(3));
            Assert.That(session.MoveCount, Is.EqualTo(0));
        }

        [Test]
        public void Restart_IntoAVariantTheLevelDoesNotHave_Throws_AndKeepsPlaying()
        {
            LevelSession session = Start(Valid());
            BoardState before = session.Board;

            Assert.Throws<ArgumentOutOfRangeException>(() => session.Restart(9));

            Assert.That(session.Board, Is.SameAs(before));
            Assert.That(session.Variant, Is.EqualTo(1));
            Assert.That(session.TryMove(0, 2, out _), Is.True, "the session still plays its board");
        }
    }
}
