using System;
using System.Collections.Generic;
using BallSort.Core.Levels;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>The resolved grid: cells, aligned and free rows, placements and added tubes (docs/mechanics.md).</summary>
    [TestFixture]
    public sealed class BoardGridTests
    {
        private static BoardGrid Grid(int tubeCount, params int?[][] rows) => new BoardGrid(rows, tubeCount);

        /// <summary>Rows of the given cell counts, filled with normal tubes in order.</summary>
        private static BoardGrid FreeRows(params int[] cellsPerRow)
        {
            var rows = new int?[cellsPerRow.Length][];
            int next = 0;
            for (int r = 0; r < rows.Length; r++)
            {
                rows[r] = new int?[cellsPerRow[r]];
                for (int c = 0; c < cellsPerRow[r]; c++)
                {
                    rows[r][c] = next++;
                }
            }

            return new BoardGrid(rows, next);
        }

        private static int[] CellCounts(BoardGrid grid)
        {
            var counts = new int[grid.RowCount];
            for (int r = 0; r < counts.Length; r++)
            {
                counts[r] = grid.GetCellCount(r);
            }

            return counts;
        }

        private static (int Row, int Cell) Add(BoardGrid grid)
        {
            Assert.That(grid.TryAddTube(out int row, out int cell), Is.True, "test setup: there should be room");
            return (row, cell);
        }

        // --- Resolving -------------------------------------------------------------------------

        [Test]
        public void Placements_PutALargeTubeOnItsBottomRow()
        {
            BoardGrid grid = Grid(4, new int?[] { 0, 1 }, new int?[] { 0, 2, 3 });

            GridPlacement large = grid.GetPlacement(0);
            Assert.That((large.Row, large.Cell, large.RowSpan, large.TopRow), Is.EqualTo((1, 0, 2, 0)));
            Assert.That((grid.GetPlacement(1).Row, grid.GetPlacement(1).Cell), Is.EqualTo((0, 1)));
            Assert.That((grid.GetPlacement(3).Row, grid.GetPlacement(3).Cell), Is.EqualTo((1, 2)));
            Assert.That(grid.GetPlacement(3).RowSpan, Is.EqualTo(1));
        }

        [Test]
        public void Rows_HoldingPartOfALargeTube_AreAligned_TheRestAreFree()
        {
            // The docs example: three rows, a large tube across rows 2–3 (indices 1–2).
            BoardGrid grid = Grid(6, new int?[] { 0, 1 }, new int?[] { 3, 2, null }, new int?[] { 3, 4, 5 });

            Assert.That(grid.IsAligned(0), Is.False);
            Assert.That(grid.IsAligned(1), Is.True);
            Assert.That(grid.IsAligned(2), Is.True);
        }

        [Test]
        public void CellCounts_IncludeGapsAndLargeTubeCells()
        {
            BoardGrid grid = Grid(6, new int?[] { 0, 1 }, new int?[] { 3, 2, null }, new int?[] { 3, 4, 5 });

            Assert.That(CellCounts(grid), Is.EqualTo(new[] { 2, 3, 3 }));
            Assert.That(grid.GetTubeAt(1, 2), Is.EqualTo(BoardGrid.Gap));
            Assert.That(grid.GetTubeAt(2, 0), Is.EqualTo(3));
            Assert.That(grid.WidestRowCellCount, Is.EqualTo(3));
            Assert.That(grid.TubeCount, Is.EqualTo(6));
        }

        [Test]
        public void ATubeMissingFromTheCells_Throws()
        {
            Assert.That(() => Grid(3, new int?[] { 0, 1 }), Throws.ArgumentException);
        }

        [Test]
        public void ACellNamingNoTube_Throws()
        {
            Assert.That(() => Grid(2, new int?[] { 0, 1, 5 }), Throws.ArgumentException);
        }

        // --- Added tubes -----------------------------------------------------------------------

        [Test]
        public void AddedTubes_GoToTheRowWithFewestCells_TiesToTheBottom()
        {
            // Docs example: rows 4, 5 → added to the top → 5, 5 → next added to the bottom → 5, 6.
            BoardGrid grid = FreeRows(4, 5);

            Assert.That(Add(grid), Is.EqualTo((0, 4)));
            Assert.That(CellCounts(grid), Is.EqualTo(new[] { 5, 5 }));

            Assert.That(Add(grid), Is.EqualTo((1, 5)));
            Assert.That(CellCounts(grid), Is.EqualTo(new[] { 5, 6 }));
        }

        [Test]
        public void AddedTubes_BesideALargeTube_GoToTheBottom_AndTheTopRowStaysAligned()
        {
            // Docs example: a large tube across both rows, 4, 4 → added to the bottom → 4, 5.
            BoardGrid grid = Grid(7, new int?[] { 0, 1, 2, 3 }, new int?[] { 0, 4, 5, 6 });

            Assert.That(Add(grid), Is.EqualTo((1, 4)));
            Assert.That(CellCounts(grid), Is.EqualTo(new[] { 4, 5 }));
            Assert.That(grid.IsAligned(0) && grid.IsAligned(1), Is.True, "the top row is not centred on its own");
        }

        [Test]
        public void AnAddedTube_IsPlacedAtTheEndOfItsRowAsANormalTube()
        {
            BoardGrid grid = FreeRows(3);

            Add(grid);

            Assert.That(grid.TubeCount, Is.EqualTo(4));
            Assert.That(grid.GetTubeAt(0, 3), Is.EqualTo(3));
            GridPlacement placement = grid.GetPlacement(3);
            Assert.That((placement.Row, placement.Cell, placement.RowSpan), Is.EqualTo((0, 3, 1)));
        }

        [Test]
        public void AddedTubes_LandAfterAGap_AtTheVeryEndOfTheRow()
        {
            BoardGrid grid = Grid(4, new int?[] { 0, 1, 2 }, new int?[] { 0, 3, null });

            Assert.That(Add(grid), Is.EqualTo((1, 3)), "the tie goes to the bottom row, after its gap");
            Assert.That(grid.GetTubeAt(1, 2), Is.EqualTo(BoardGrid.Gap), "the gap stays");
        }

        [Test]
        public void ARow_GrowsFromSixToEightCells_DuringPlay()
        {
            BoardGrid grid = FreeRows(LevelLayout.MaxCellsPerRow);

            Assert.That(Add(grid), Is.EqualTo((0, 6)));
            Assert.That(Add(grid), Is.EqualTo((0, 7)));

            Assert.That(grid.GetCellCount(0), Is.EqualTo(LevelLayout.MaxCellsPerRowInPlay));
            Assert.That(grid.RowCount, Is.EqualTo(1), "rows are never added");
        }

        [Test]
        public void AFullRow_IsSkippedWhileAnotherRowHasRoom()
        {
            BoardGrid grid = FreeRows(8, 7);

            Assert.That(Add(grid), Is.EqualTo((1, 7)));
            Assert.That(CellCounts(grid), Is.EqualTo(new[] { 8, 8 }));
        }

        [Test]
        public void OnceEveryRowHasEightCells_ThereIsNoRoom()
        {
            BoardGrid grid = FreeRows(6, 6, 6);
            for (int i = 0; i < 6; i++)
            {
                Add(grid);
            }

            Assert.That(CellCounts(grid), Is.EqualTo(new[] { 8, 8, 8 }));
            Assert.That(grid.CanAddTube, Is.False);
            Assert.That(grid.TryAddTube(out int row, out int cell), Is.False);
            Assert.That((row, cell), Is.EqualTo((-1, -1)));
            Assert.That(grid.TubeCount, Is.EqualTo(24), "a refused add changes nothing");
        }

        [Test]
        public void CanAddTube_IsTrueWhileAnyRowHasRoom()
        {
            Assert.That(FreeRows(8, 8, 7).CanAddTube, Is.True);
            Assert.That(FreeRows(8).CanAddTube, Is.False);
        }

        [Test]
        public void TheWidestRow_TracksAddedTubes()
        {
            BoardGrid grid = FreeRows(5, 5);
            Assert.That(grid.WidestRowCellCount, Is.EqualTo(5));

            Add(grid);   // bottom: 5, 6

            Assert.That(grid.WidestRowCellCount, Is.EqualTo(6));

            Add(grid);   // top: 6, 6 — the widest row does not grow

            Assert.That(grid.WidestRowCellCount, Is.EqualTo(6));
        }

        [Test]
        public void NullCells_Throw()
        {
            Assert.That(() => new BoardGrid(null, 1), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new BoardGrid(new List<IReadOnlyList<int?>>(), 0), Throws.ArgumentException);
        }
    }
}
