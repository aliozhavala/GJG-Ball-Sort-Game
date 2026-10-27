using BallSort.Core.Levels;
using BallSort.Game.Services;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class LevelJsonParserTests
    {
        private const string FullLevel = @"{
            ""formatVersion"": 1,
            ""id"": 37,
            ""colors"": 2,
            ""layout"": { ""rows"": [2, 1] },
            ""tubes"": [
                { ""capacity"": 4, ""balls"": [0, 1, 0, 1], ""hidden"": [true, true, false, false] },
                { ""capacity"": 4, ""balls"": [1, 0, 1, 0] },
                { ""capacity"": 4, ""balls"": [] }
            ],
            ""meta"": { ""optimalMoves"": 6, ""difficulty"": 0.25, ""mechanics"": [""hidden""], ""tutorial"": true }
        }";

        [Test]
        public void Parse_ReadsEveryField()
        {
            LevelDto level = LevelJsonParser.Parse(FullLevel, "full");

            Assert.That(level.FormatVersion, Is.EqualTo(1));
            Assert.That(level.Id, Is.EqualTo(37));
            Assert.That(level.Colors, Is.EqualTo(2));
            Assert.That(level.Layout.Rows, Is.EqualTo(new[] { 2, 1 }));
            Assert.That(level.Tubes, Has.Count.EqualTo(3));
            Assert.That(level.Tubes[0].Balls, Is.EqualTo(new[] { 0, 1, 0, 1 }));
            Assert.That(level.Tubes[0].Hidden, Is.EqualTo(new[] { true, true, false, false }));
            Assert.That(level.Tubes[1].Hidden, Is.Null);
            Assert.That(level.Tubes[2].Balls, Is.Empty);
            Assert.That(level.Meta.OptimalMoves, Is.EqualTo(6));
            Assert.That(level.Meta.Difficulty, Is.EqualTo(0.25));
            Assert.That(level.Meta.Mechanics, Is.EqualTo(new[] { "hidden" }));
            Assert.That(level.Meta.Tutorial, Is.True);
        }

        [Test]
        public void Parse_OptionalSectionsMayBeOmitted()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1,
                ""tubes"": [ { ""capacity"": 2, ""balls"": [0, 0] } ] }";

            LevelDto level = LevelJsonParser.Parse(json, "minimal");

            Assert.That(level.Layout, Is.Null);
            Assert.That(level.Meta, Is.Null);
            Assert.That(LevelValidator.Validate(level, "minimal"), Is.Empty);
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
            const string json = @"{ ""id"": 1, ""colors"": 1, ""tubes"": [ { ""capacity"": 2, ""balls"": [0, 0] } ] }";

            LevelDto level = LevelJsonParser.Parse(json, "old");

            Assert.That(LevelValidator.Validate(level, "old"), Has.Exactly(1).Contains("formatVersion is missing"));
        }

        [Test]
        public void Parse_UnsupportedFormatVersion_FailsToLoad()
        {
            const string json = @"{ ""formatVersion"": 2, ""id"": 1, ""colors"": 1,
                ""tubes"": [ { ""capacity"": 2, ""balls"": [0, 0] } ] }";

            LevelDto level = LevelJsonParser.Parse(json, "future");

            var e = Assert.Throws<LevelFormatException>(() => LevelFactory.Load(level, "future"));
            Assert.That(e.Errors, Has.Exactly(1).Contains("formatVersion 2 is not supported"));
        }
    }
}
