using BallSort.Core.Levels;
using BallSort.Core.Player;
using BallSort.Core.Rules;
using BallSort.Core.Session;
using BallSort.LevelJson;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class LevelJsonParserTests
    {
        /// <summary>The example from docs/levels.md, verbatim: it must stay valid and solvable.</summary>
        private const string DocsExample = @"{
  ""formatVersion"": 1,
  ""id"": 37,
  ""colors"": 3,
  ""layout"": { ""grid"": [[0, 1, 2], [0, 3, 4]] },
  ""tubes"": [
    { ""capacity"": 5, ""balls"": [0], ""clay"": true },
    { ""capacity"": 4, ""balls"": [1, 2, 0, 0], ""hidden"": [true, false, false, false], ""keys"": [{ ""slot"": 1, ""lock"": 0 }] },
    { ""capacity"": 4, ""balls"": [2, 0, 2, 2], ""veil"": { ""color"": 1 } },
    { ""capacity"": 4, ""balls"": [1, 1, 1, 0] },
    { ""capacity"": 4, ""balls"": [] }
  ],
  ""locks"": [{ ""id"": 0, ""tubes"": [3, 4] }],
  ""meta"": { ""mechanics"": [""hidden"", ""veil"", ""lock"", ""clay"", ""large""], ""tutorial"": false }
}";

        [Test]
        public void Parse_ReadsEveryField()
        {
            LevelDto level = LevelJsonParser.Parse(DocsExample, "docs");

            Assert.That(level.FormatVersion, Is.EqualTo(1));
            Assert.That(level.Id, Is.EqualTo(37));
            Assert.That(level.Colors, Is.EqualTo(3));
            Assert.That(level.Layout.Grid, Has.Count.EqualTo(2));
            Assert.That(level.Layout.Grid[1], Is.EqualTo(new int?[] { 0, 3, 4 }));
            Assert.That(level.Tubes, Has.Count.EqualTo(5));
            Assert.That(level.Tubes[0].Clay, Is.True);
            Assert.That(level.Tubes[1].Clay, Is.False);
            Assert.That(level.Tubes[1].Balls, Is.EqualTo(new[] { 1, 2, 0, 0 }));
            Assert.That(level.Tubes[1].Hidden, Is.EqualTo(new[] { true, false, false, false }));
            Assert.That(level.Tubes[1].Keys[0].Slot, Is.EqualTo(1));
            Assert.That(level.Tubes[1].Keys[0].Lock, Is.EqualTo(0));
            Assert.That(level.Tubes[2].Veil.Color, Is.EqualTo(1));
            Assert.That(level.Tubes[3].Hidden, Is.Null);
            Assert.That(level.Tubes[4].Balls, Is.Empty);
            Assert.That(level.Locks[0].Id, Is.EqualTo(0));
            Assert.That(level.Locks[0].Tubes, Is.EqualTo(new[] { 3, 4 }));
            Assert.That(level.Meta.Mechanics, Is.EqualTo(new[] { "hidden", "veil", "lock", "clay", "large" }));
            Assert.That(level.Meta.Tutorial, Is.False);
        }

        [Test]
        public void TheDocsExample_IsValidAndSolvable()
        {
            LoadedLevel loaded = LevelFactory.Load(LevelJsonParser.Parse(DocsExample, "docs"), "docs");
            var session = new LevelSession(loaded, new BoosterInventory(0, 0, 0), addedTubeCapacity: 4);

            int[][] solution =
            {
                new[] { 1, 0 },   // the 0s into clay; the key's ball becomes the top run, lock 0 opens
                new[] { 3, 0 },
                new[] { 1, 4 },   // the 2 off the hidden ball, which is revealed
                new[] { 1, 3 },   // colour 1 completes; the veil opens
                new[] { 2, 4 },
                new[] { 2, 0 },   // the clay tube completes
                new[] { 2, 4 },
            };

            foreach (int[] move in solution)
            {
                Assert.That(session.TryMove(move[0], move[1], out MoveRejection rejection), Is.True, $"{move[0]} -> {move[1]}: {rejection}");
            }

            Assert.That(session.IsWon, Is.True);
        }

        [Test]
        public void Parse_ReadsNullGridCells()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1,
                ""layout"": { ""grid"": [[0, 1], [null, 1]] },
                ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0, 0, 0] }, { ""capacity"": 8, ""balls"": [0, 0, 0, 0] } ] }";

            LevelDto level = LevelJsonParser.Parse(json, "gap");

            Assert.That(level.Layout.Grid[1], Is.EqualTo(new int?[] { null, 1 }));
            Assert.That(LevelValidator.Validate(level, "gap"), Is.Empty);
        }

        [Test]
        public void Parse_OptionalSectionsMayBeOmitted()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1, ""layout"": { ""grid"": [[0, 1]] },
                ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0] }, { ""capacity"": 4, ""balls"": [0, 0] } ] }";

            LevelDto level = LevelJsonParser.Parse(json, "minimal");

            Assert.That(level.Meta, Is.Null);
            Assert.That(level.Locks, Is.Null);
            Assert.That(level.Tubes[0].Veil, Is.Null);
            Assert.That(level.Tubes[0].Keys, Is.Null);
            Assert.That(LevelValidator.Validate(level, "minimal"), Is.Empty);
        }

        [Test]
        public void Parse_WithoutALayout_IsCaughtByValidation()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1,
                ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0] }, { ""capacity"": 4, ""balls"": [0, 0] } ] }";

            LevelDto level = LevelJsonParser.Parse(json, "no-layout");

            Assert.That(LevelValidator.Validate(level, "no-layout"), Has.Exactly(1).Contains("layout is missing"));
        }

        [Test]
        public void Parse_TheOldRowsLayout_Throws()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1, ""layout"": { ""rows"": [2] },
                ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0] }, { ""capacity"": 4, ""balls"": [0, 0] } ] }";

            var e = Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse(json, "old-layout"));
            Assert.That(e.Errors, Has.Exactly(1).Contains("rows"));
        }

        [Test]
        public void Parse_APortal_IsRejectedUntilItIsImplemented()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1, ""layout"": { ""grid"": [[0, 1]] },
                ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0], ""portal"": [] }, { ""capacity"": 4, ""balls"": [0, 0] } ] }";

            var e = Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse(json, "portal"));
            Assert.That(e.Errors, Has.Exactly(1).Contains("portal"));
        }

        [Test]
        public void Parse_UnknownKeyField_Throws()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1, ""layout"": { ""grid"": [[0, 1]] },
                ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0], ""keys"": [{ ""slots"": 0, ""lock"": 0 }] },
                            { ""capacity"": 4, ""balls"": [0, 0] } ] }";

            var e = Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse(json, "typo"));
            Assert.That(e.Errors, Has.Exactly(1).Contains("slots"));
        }

        [Test]
        public void Parse_UnknownTopLevelField_Throws()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colours"": 1, ""tubes"": [] }";

            var e = Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse(json, "typo"));
            Assert.That(e.Errors, Has.Exactly(1).StartsWith("typo: ").And.Contains("colours"));
        }

        [Test]
        public void Parse_UnknownTubeField_Throws()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1,
                ""tubes"": [ { ""capacity"": 2, ""balls"": [0, 0], ""hiden"": [false, false] } ] }";

            var e = Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse(json, "typo"));
            Assert.That(e.Errors, Has.Exactly(1).Contains("hiden"));
        }

        [Test]
        public void Parse_MalformedJson_Throws()
        {
            var e = Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse(@"{ ""id"": 1, ", "broken"));
            Assert.That(e.LevelName, Is.EqualTo("broken"));
        }

        [Test]
        public void Parse_WrongValueType_Throws()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1,
                ""tubes"": [ { ""capacity"": ""four"", ""balls"": [] } ] }";

            Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse(json, "bad-type"));
        }

        [Test]
        public void Parse_EmptyText_Throws()
        {
            Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse("  ", "empty"));
        }

        [Test]
        public void Parse_MissingFormatVersion_IsCaughtByValidation()
        {
            const string json = @"{ ""id"": 1, ""colors"": 1, ""layout"": { ""grid"": [[0, 1]] },
                ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0] }, { ""capacity"": 4, ""balls"": [0, 0] } ] }";

            LevelDto level = LevelJsonParser.Parse(json, "old");

            Assert.That(LevelValidator.Validate(level, "old"), Has.Exactly(1).Contains("formatVersion is missing"));
        }

        [Test]
        public void Parse_UnsupportedFormatVersion_FailsToLoad()
        {
            const string json = @"{ ""formatVersion"": 2, ""id"": 1, ""colors"": 1, ""layout"": { ""grid"": [[0, 1]] },
                ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0] }, { ""capacity"": 4, ""balls"": [0, 0] } ] }";

            LevelDto level = LevelJsonParser.Parse(json, "future");

            var e = Assert.Throws<LevelFormatException>(() => LevelFactory.Load(level, "future"));
            Assert.That(e.Errors, Has.Exactly(1).Contains("formatVersion 2 is not supported"));
        }
    }
}
