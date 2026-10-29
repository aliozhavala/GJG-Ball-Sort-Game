using System;
using BallSort.Core.Levels;
using BallSort.LevelJson;
using Newtonsoft.Json;

namespace BallSort.Editor.Validation
{
    /// <summary>
    /// Writes one variant's <c>meta.optimalMoves</c> into a level file's text, keeping everything else
    /// exactly as it was — hand formatting, key order, line endings.
    /// </summary>
    /// <remarks>
    /// Re-serialising through Newtonsoft would lose the hand formatting (one tube per line, inline
    /// ball arrays), so this edits the text instead. A small scanner that understands strings and
    /// nesting finds <c>variants[k].meta</c>; the value is replaced, or inserted first in <c>meta</c>
    /// (the order of <see cref="LevelMetaDto"/>), or a one-line <c>meta</c> is added as the variant's
    /// last field. Every edit is then checked by parsing both versions: if anything other than that
    /// variant's <c>meta.optimalMoves</c> differs, the edit is refused.
    /// </remarks>
    public static class OptimalMovesWriter
    {
        /// <param name="json">The level file's text.</param>
        /// <param name="levelName">For error messages, e.g. <c>level_003</c>.</param>
        /// <param name="variant">The variant to write, 1-based.</param>
        /// <param name="optimalMoves">The value to write.</param>
        /// <param name="updated">The new text (equal to <paramref name="json"/> when the value was already there).</param>
        /// <param name="error">Why the edit was refused, when this returns false.</param>
        public static bool TryWrite(
            string json, string levelName, int variant, int optimalMoves, out string updated, out string error)
        {
            updated = json;
            try
            {
                LevelDto before = LevelJsonParser.Parse(json, levelName);
                if (before?.Variants == null || variant < 1 || variant > before.Variants.Count || before.Variants[variant - 1] == null)
                {
                    error = $"{levelName}: has no variant {variant}.";
                    return false;
                }

                if (!TrySplice(json, variant - 1, optimalMoves, out updated))
                {
                    updated = json;
                    error = $"{levelName}: could not place variant {variant}'s meta.optimalMoves in the file.";
                    return false;
                }

                LevelDto after = LevelJsonParser.Parse(updated, levelName);
                LevelVariantDto expected = before.Variants[variant - 1];
                expected.Meta ??= new LevelMetaDto();
                expected.Meta.OptimalMoves = optimalMoves;
                if (JsonConvert.SerializeObject(before) != JsonConvert.SerializeObject(after))
                {
                    updated = json;
                    error = $"{levelName}: the edit would change more than variant {variant}'s meta.optimalMoves.";
                    return false;
                }
            }
            catch (LevelFormatException e)
            {
                updated = json;
                error = string.Join("\n", e.Errors);
                return false;
            }

            error = null;
            return true;
        }

        private static bool TrySplice(string json, int variantIndex, int optimalMoves, out string updated)
        {
            updated = json;
            string value = optimalMoves.ToString();

            int root = SkipWhitespace(json, 0);
            if (!IsAt(json, root, '{') ||
                !TryFindMember(json, root, "variants", out int variants, out _) || !IsAt(json, variants, '[') ||
                !TryFindElement(json, variants, variantIndex, out int variantStart, out int variantEnd) ||
                !IsAt(json, variantStart, '{'))
            {
                return false;
            }

            if (!TryFindMember(json, variantStart, "meta", out int metaStart, out int metaEnd))
            {
                // Added as the variant's last field, right after its last value.
                int close = variantEnd - 1;
                int lastValueEnd = close - 1;
                while (lastValueEnd > variantStart && char.IsWhiteSpace(json[lastValueEnd]))
                {
                    lastValueEnd--;
                }

                updated = json.Insert(lastValueEnd + 1, $", \"meta\": {{ \"optimalMoves\": {value} }}");
                return true;
            }

            if (!IsAt(json, metaStart, '{'))
            {
                // "meta": null
                updated = Replace(json, metaStart, metaEnd, $"{{ \"optimalMoves\": {value} }}");
                return true;
            }

            if (TryFindMember(json, metaStart, "optimalMoves", out int valueStart, out int valueEnd))
            {
                updated = Replace(json, valueStart, valueEnd, value);
                return true;
            }

            int firstMember = SkipWhitespace(json, metaStart + 1);
            updated = IsAt(json, firstMember, '}')
                ? Replace(json, metaStart, metaEnd, $"{{ \"optimalMoves\": {value} }}")
                : json.Insert(firstMember, $"\"optimalMoves\": {value}, ");
            return true;
        }

        private static string Replace(string json, int start, int end, string text) =>
            json.Substring(0, start) + text + json.Substring(end);

        private static bool IsAt(string json, int index, char c) => index < json.Length && json[index] == c;

        // --- A minimal scanner over text already known to parse ------------------------------------

        /// <summary>Finds a member of the object starting at <paramref name="objectStart"/>; returns its value's span.</summary>
        private static bool TryFindMember(string json, int objectStart, string name, out int valueStart, out int valueEnd)
        {
            int i = SkipWhitespace(json, objectStart + 1);
            while (i < json.Length && json[i] == '"')
            {
                int keyEnd = SkipString(json, i);
                string key = json.Substring(i + 1, keyEnd - i - 2);
                i = SkipWhitespace(json, keyEnd);
                i = SkipWhitespace(json, i + 1); // the ':'
                valueStart = i;
                valueEnd = SkipValue(json, i);
                if (string.Equals(key, name, StringComparison.Ordinal))
                {
                    return true;
                }

                i = SkipWhitespace(json, valueEnd);
                if (IsAt(json, i, ','))
                {
                    i = SkipWhitespace(json, i + 1);
                }
            }

            valueStart = valueEnd = -1;
            return false;
        }

        /// <summary>Finds element <paramref name="index"/> of the array starting at <paramref name="arrayStart"/>.</summary>
        private static bool TryFindElement(string json, int arrayStart, int index, out int elementStart, out int elementEnd)
        {
            int i = SkipWhitespace(json, arrayStart + 1);
            for (int n = 0; i < json.Length && json[i] != ']'; n++)
            {
                int end = SkipValue(json, i);
                if (n == index)
                {
                    elementStart = i;
                    elementEnd = end;
                    return true;
                }

                i = SkipWhitespace(json, end);
                if (IsAt(json, i, ','))
                {
                    i = SkipWhitespace(json, i + 1);
                }
            }

            elementStart = elementEnd = -1;
            return false;
        }

        /// <returns>The index just past the value starting at <paramref name="start"/>.</returns>
        private static int SkipValue(string json, int start)
        {
            char c = json[start];
            if (c == '"')
            {
                return SkipString(json, start);
            }

            if (c != '{' && c != '[')
            {
                // A number, true, false or null.
                int i = start;
                while (i < json.Length && ",}] \t\r\n".IndexOf(json[i]) < 0)
                {
                    i++;
                }

                return i;
            }

            int depth = 0;
            for (int i = start; i < json.Length; i++)
            {
                char d = json[i];
                if (d == '"')
                {
                    i = SkipString(json, i) - 1;
                }
                else if (d == '{' || d == '[')
                {
                    depth++;
                }
                else if ((d == '}' || d == ']') && --depth == 0)
                {
                    return i + 1;
                }
            }

            return json.Length;
        }

        /// <returns>The index just past the closing quote of the string starting at <paramref name="start"/>.</returns>
        private static int SkipString(string json, int start)
        {
            for (int i = start + 1; i < json.Length; i++)
            {
                if (json[i] == '\\')
                {
                    i++;
                }
                else if (json[i] == '"')
                {
                    return i + 1;
                }
            }

            return json.Length;
        }

        private static int SkipWhitespace(string json, int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i]))
            {
                i++;
            }

            return i;
        }
    }
}
