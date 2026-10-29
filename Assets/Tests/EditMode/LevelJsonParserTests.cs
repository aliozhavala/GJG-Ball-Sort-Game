using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BallSort.Core.Levels;
using BallSort.Core.Player;
using BallSort.Core.Rules;
using BallSort.Core.Session;
using BallSort.Core.Solver;
using BallSort.LevelJson;
using NUnit.Framework;
using UnityEngine;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class LevelJsonParserTests
    {
        /// <summary>One variant using every field: a large clay tube, hidden balls, a key, a veil and a lock.</summary>
        private const string EveryFieldVariant = @"{ ""colors"": 3, ""tubes"": [
            { ""capacity"": 5, ""balls"": [0], ""clay"": true },
            { ""capacity"": 4, ""balls"": [1, 2, 0, 0], ""hidden"": [true, false, false, false], ""keys"": [{ ""slot"": 1, ""lock"": 0 }] },
            { ""capacity"": 4, ""balls"": [2, 0, 2, 2], ""veil"": { ""color"": 1 } },
            { ""capacity"": 4, ""balls"": [1, 1, 1, 0] },
            { ""capacity"": 4, ""balls"": [] }
          ], ""locks"": [{ ""id"": 0, ""tubes"": [3, 4] }],
          ""meta"": { ""optimalMoves"": 7, ""difficulty"": 0.25, ""notes"": ""Every mechanic {at once}."" } }";

        private const string EveryFieldGrid = @"{ ""grid"": [[0, 1, 2], [0, 3, 4]] }";

        /// <summary>A minimal valid variant on a <c>[[0, 1]]</c> grid.</summary>
        private const string MinimalVariant = @"{ ""colors"": 1, ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0] }, { ""capacity"": 4, ""balls"": [0, 0] } ] }";

        private const string MinimalGrid = @"{ ""grid"": [[0, 1]] }";

        /// <summary>A version 2 file whose five variants are all <paramref name="variant"/>.</summary>
        private static string FileOf(string variant, string layout = MinimalGrid, string tier = @"""regular""", string extra = "") =>
            @"{ ""formatVersion"": 2, ""id"": 37, ""tier"": " + tier + extra + @", ""layout"": " + layout +
            @", ""variants"": [" + string.Join(", ", variant, variant, variant, variant, variant) + "] }";

        private static string[] ParseErrors(string json)
        {
            var e = Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse(json, "bad"));
            return new List<string>(e.Errors).ToArray();
        }

        [Test]
        public void Parse_ReadsEveryField()
        {
            LevelDto level = LevelJsonParser.Parse(FileOf(EveryFieldVariant, EveryFieldGrid, @"""superHard""", @", ""tutorial"": true"), "every");

            Assert.That(level.FormatVersion, Is.EqualTo(2));
            Assert.That(level.Id, Is.EqualTo(37));
            Assert.That(level.Tier, Is.EqualTo(LevelTier.SuperHard));
            Assert.That(level.Tutorial, Is.True);
            Assert.That(level.Layout.Grid, Has.Count.EqualTo(2));
            Assert.That(level.Layout.Grid[1], Is.EqualTo(new int?[] { 0, 3, 4 }));
            Assert.That(level.Variants, Has.Count.EqualTo(5));

            LevelVariantDto variant = level.Variants[0];
            Assert.That(variant.Colors, Is.EqualTo(3));
            Assert.That(variant.Tubes, Has.Count.EqualTo(5));
            Assert.That(variant.Tubes[0].Clay, Is.True);
            Assert.That(variant.Tubes[1].Clay, Is.False);
            Assert.That(variant.Tubes[1].Balls, Is.EqualTo(new[] { 1, 2, 0, 0 }));
            Assert.That(variant.Tubes[1].Hidden, Is.EqualTo(new[] { true, false, false, false }));
            Assert.That(variant.Tubes[1].Keys[0].Slot, Is.EqualTo(1));
            Assert.That(variant.Tubes[1].Keys[0].Lock, Is.EqualTo(0));
            Assert.That(variant.Tubes[2].Veil.Color, Is.EqualTo(1));
            Assert.That(variant.Tubes[3].Hidden, Is.Null);
            Assert.That(variant.Tubes[4].Balls, Is.Empty);
            Assert.That(variant.Locks[0].Id, Is.EqualTo(0));
            Assert.That(variant.Locks[0].Tubes, Is.EqualTo(new[] { 3, 4 }));
            Assert.That(variant.Meta.OptimalMoves, Is.EqualTo(7));
            Assert.That(variant.Meta.Difficulty, Is.EqualTo(0.25));
            Assert.That(variant.Meta.Notes, Is.EqualTo("Every mechanic {at once}."));
        }

        [Test]
        public void TheEveryFieldVariant_IsValid_AndItsMechanicsChainToAWin()
        {
            LoadedLevel loaded = LevelFactory.Load(LevelJsonParser.Parse(FileOf(EveryFieldVariant, EveryFieldGrid), "every"), "every");
            var session = new LevelSession(loaded, 1, new BoosterInventory(0, 0, 0), addedTubeCapacity: 4);

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

        [TestCase("regular", LevelTier.Regular)]
        [TestCase("hard", LevelTier.Hard)]
        [TestCase("superHard", LevelTier.SuperHard)]
        public void Parse_ReadsEachTier(string text, LevelTier tier)
        {
            Assert.That(LevelJsonParser.Parse(FileOf(MinimalVariant, tier: $"\"{text}\""), "tier").Tier, Is.EqualTo(tier));
        }

        [Test]
        public void Parse_ANumericTier_Throws()
        {
            Assert.That(ParseErrors(FileOf(MinimalVariant, tier: "1")), Has.Exactly(1).Contains("tier"));
        }

        [Test]
        public void Parse_AnUnknownTier_Throws()
        {
            Assert.That(ParseErrors(FileOf(MinimalVariant, tier: @"""medium""")), Has.Exactly(1).Contains("medium").And.Contains("tier"));
        }

        [Test]
        public void Parse_WithoutATier_IsCaughtByValidation()
        {
            string json = FileOf(MinimalVariant).Replace(@"""tier"": ""regular"", ", string.Empty);

            LevelDto level = LevelJsonParser.Parse(json, "no-tier");

            Assert.That(LevelValidator.Validate(level, "no-tier"), Has.Exactly(1).Contains("tier is missing"));
        }

        [Test]
        public void Parse_ReadsNullGridCells()
        {
            const string variant = @"{ ""colors"": 1, ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0, 0, 0] }, { ""capacity"": 8, ""balls"": [0, 0, 0, 0] } ] }";

            LevelDto level = LevelJsonParser.Parse(FileOf(variant, @"{ ""grid"": [[0, 1], [null, 1]] }"), "gap");

            Assert.That(level.Layout.Grid[1], Is.EqualTo(new int?[] { null, 1 }));
            Assert.That(LevelValidator.Validate(level, "gap"), Is.Empty);
        }

        [Test]
        public void Parse_OptionalSectionsMayBeOmitted()
        {
            LevelDto level = LevelJsonParser.Parse(FileOf(MinimalVariant), "minimal");

            Assert.That(level.Tutorial, Is.Null);
            Assert.That(level.Variants[0].Meta, Is.Null);
            Assert.That(level.Variants[0].Locks, Is.Null);
            Assert.That(level.Variants[0].Tubes[0].Veil, Is.Null);
            Assert.That(level.Variants[0].Tubes[0].Keys, Is.Null);
            Assert.That(LevelValidator.Validate(level, "minimal"), Is.Empty);
        }

        [Test]
        public void Parse_WithoutALayout_IsCaughtByValidation()
        {
            string json = FileOf(MinimalVariant).Replace(@", ""layout"": " + MinimalGrid, string.Empty);

            LevelDto level = LevelJsonParser.Parse(json, "no-layout");

            Assert.That(LevelValidator.Validate(level, "no-layout"), Has.Exactly(1).Contains("layout is missing"));
        }

        [Test]
        public void Parse_TheOldRowsLayout_Throws()
        {
            Assert.That(ParseErrors(FileOf(MinimalVariant, @"{ ""rows"": [2] }")), Has.Exactly(1).Contains("rows"));
        }

        [Test]
        public void Parse_UnknownTopLevelField_Throws()
        {
            Assert.That(ParseErrors(FileOf(MinimalVariant, extra: @", ""colours"": 1")), Has.Exactly(1).StartsWith("bad: ").And.Contains("colours"));
        }

        [Test]
        public void Parse_UnknownVariantField_Throws()
        {
            string variant = MinimalVariant.Replace(@"""colors""", @"""colours""");

            Assert.That(ParseErrors(FileOf(variant)), Has.Exactly(1).Contains("colours"));
        }

        [Test]
        public void Parse_UnknownTubeField_Throws()
        {
            string variant = MinimalVariant.Replace(@"""balls"": [0, 0] }, {", @"""balls"": [0, 0], ""hiden"": [false, false] }, {");

            Assert.That(ParseErrors(FileOf(variant)), Has.Exactly(1).Contains("hiden"));
        }

        [Test]
        public void Parse_UnknownKeyField_Throws()
        {
            string variant = MinimalVariant.Replace(@"""balls"": [0, 0] }, {", @"""balls"": [0, 0], ""keys"": [{ ""slots"": 0, ""lock"": 0 }] }, {");

            Assert.That(ParseErrors(FileOf(variant)), Has.Exactly(1).Contains("slots"));
        }

        [TestCase("mechanics", @"[""veil""]")]
        [TestCase("tutorial", "true")]
        public void Parse_FieldsThatLeftMeta_Throw(string field, string value)
        {
            // meta.mechanics is derived from the data now; tutorial moved to the top level.
            string variant = MinimalVariant.Replace(" ] }", $" ], \"meta\": {{ \"{field}\": {value} }} }}");

            Assert.That(ParseErrors(FileOf(variant)), Has.Exactly(1).Contains(field));
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
            string variant = MinimalVariant.Replace(@"""capacity"": 4", @"""capacity"": ""four""");

            Assert.That(ParseErrors(FileOf(variant)), Has.Length.EqualTo(1));
        }

        [Test]
        public void Parse_EmptyText_Throws()
        {
            Assert.Throws<LevelFormatException>(() => LevelJsonParser.Parse("  ", "empty"));
        }

        // --- Format version --------------------------------------------------------------------

        [Test]
        public void Parse_AVersion1File_IsRejectedWithOnlyAConversionMessage()
        {
            const string json = @"{ ""formatVersion"": 1, ""id"": 1, ""colors"": 1, ""layout"": { ""grid"": [[0, 1]] },
                ""tubes"": [ { ""capacity"": 4, ""balls"": [0, 0] }, { ""capacity"": 4, ""balls"": [0, 0] } ],
                ""meta"": { ""mechanics"": [], ""tutorial"": false } }";

            Assert.That(ParseErrors(json), Is.EqualTo(new[]
            {
                "bad: formatVersion 1 is no longer supported: convert the file to version 2 (one grid, 5 variants; see docs/level-format-guide.md).",
            }));
        }

        [Test]
        public void Parse_WithoutAFormatVersion_IsRejected()
        {
            string json = FileOf(MinimalVariant).Replace(@"""formatVersion"": 2, ", string.Empty);

            Assert.That(ParseErrors(json), Is.EqualTo(new[] { "bad: formatVersion is missing (expected 2)." }));
        }

        [Test]
        public void Parse_AFutureFormatVersion_IsRejected()
        {
            string json = FileOf(MinimalVariant).Replace(@"""formatVersion"": 2", @"""formatVersion"": 3");

            Assert.That(ParseErrors(json), Is.EqualTo(new[] { "bad: formatVersion 3 is not supported (expected 2)." }));
        }

        // --- The docs ---------------------------------------------------------------------------

        private static IEnumerable<TestCaseData> DocsExamples()
        {
            string docs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs"));
            foreach (string file in new[] { "levels.md", "level-format-guide.md" })
            {
                string path = Path.Combine(docs, file);
                if (!File.Exists(path))
                {
                    continue;
                }

                // Every complete level file in a ```json block; fragments without formatVersion are skipped.
                int n = 0;
                foreach (Match block in Regex.Matches(File.ReadAllText(path), "```json\\s*\\n(.*?)```", RegexOptions.Singleline))
                {
                    if (block.Groups[1].Value.Contains("\"formatVersion\""))
                    {
                        yield return new TestCaseData(file, block.Groups[1].Value).SetName($"DocsExample_IsValidAndEveryVariantSolvable({file} #{++n})");
                    }
                }
            }
        }

        [TestCaseSource(nameof(DocsExamples))]
        public void DocsExample_IsValidAndEveryVariantSolvable(string file, string json)
        {
            LoadedLevel level = LevelFactory.Load(LevelJsonParser.Parse(json, file), file);

            for (int variant = 1; variant <= level.VariantCount; variant++)
            {
                SolverResult result = BoardSolver.Solve(level.CreateBoard(variant), SolverMode.Fast, 200000);
                Assert.That(result.Outcome, Is.EqualTo(SolverOutcome.Solved), $"{file} V{variant}: {result}");
            }
        }
    }
}
