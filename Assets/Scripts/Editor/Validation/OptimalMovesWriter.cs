using System.Text.RegularExpressions;
using BallSort.Core.Levels;
using BallSort.LevelJson;
using Newtonsoft.Json;

namespace BallSort.Editor.Validation
{
    /// <summary>
    /// Writes <c>meta.optimalMoves</c> into a level file's text, keeping everything else exactly as
    /// it was — hand formatting, key order, line endings.
    /// </summary>
    /// <remarks>
    /// Re-serialising through Newtonsoft would lose the hand formatting (one tube per line, inline
    /// ball arrays), so this edits the text instead: it replaces an existing value, or inserts the field
    /// first in <c>meta</c> (the order of <see cref="LevelMetaDto"/>), or adds a one-line
    /// <c>meta</c> as the last top-level field. Every edit is then checked by parsing both versions:
    /// if anything other than <c>meta.optimalMoves</c> differs, the edit is refused.
    /// </remarks>
    public static class OptimalMovesWriter
    {
        private static readonly Regex MetaOpening = new Regex("\"meta\"\\s*:\\s*\\{");
        private static readonly Regex OptimalMovesValue = new Regex("\"optimalMoves\"\\s*:\\s*(null|-?\\d+)");

        /// <param name="json">The level file's text.</param>
        /// <param name="levelName">For error messages, e.g. <c>level_003</c>.</param>
        /// <param name="optimalMoves">The value to write.</param>
        /// <param name="updated">The new text (equal to <paramref name="json"/> when the value was already there).</param>
        /// <param name="error">Why the edit was refused, when this returns false.</param>
        public static bool TryWrite(string json, string levelName, int optimalMoves, out string updated, out string error)
        {
            updated = Splice(json, optimalMoves);
            try
            {
                LevelDto before = LevelJsonParser.Parse(json, levelName);
                LevelDto after = LevelJsonParser.Parse(updated, levelName);
                if (after.Meta?.OptimalMoves != optimalMoves)
                {
                    error = $"{levelName}: could not place meta.optimalMoves in the file.";
                    return false;
                }

                before.Meta = before.Meta ?? new LevelMetaDto();
                before.Meta.OptimalMoves = optimalMoves;
                if (JsonConvert.SerializeObject(before) != JsonConvert.SerializeObject(after))
                {
                    error = $"{levelName}: the edit would change more than meta.optimalMoves.";
                    return false;
                }
            }
            catch (LevelFormatException e)
            {
                error = string.Join("\n", e.Errors);
                return false;
            }

            error = null;
            return true;
        }

        private static string Splice(string json, int optimalMoves)
        {
            Match meta = MetaOpening.Match(json);
            if (!meta.Success)
            {
                return AppendMeta(json, optimalMoves);
            }

            // meta holds only numbers, booleans and arrays, so its first '}' closes it.
            int bodyStart = meta.Index + meta.Length;
            int bodyEnd = json.IndexOf('}', bodyStart);
            if (bodyEnd < 0)
            {
                return json;
            }

            string body = json.Substring(bodyStart, bodyEnd - bodyStart);
            Match existing = OptimalMovesValue.Match(body);
            if (existing.Success)
            {
                Group value = existing.Groups[1];
                body = body.Substring(0, value.Index) + optimalMoves + body.Substring(value.Index + value.Length);
            }
            else if (body.Trim().Length == 0)
            {
                body = $" \"optimalMoves\": {optimalMoves} ";
            }
            else
            {
                int firstKey = body.IndexOf('"');
                body = body.Substring(0, firstKey) + $"\"optimalMoves\": {optimalMoves}, " + body.Substring(firstKey);
            }

            return json.Substring(0, bodyStart) + body + json.Substring(bodyEnd);
        }

        /// <summary>Adds <c>"meta": { "optimalMoves": n }</c> after the last top-level field, indented like the first.</summary>
        private static string AppendMeta(string json, int optimalMoves)
        {
            int close = json.LastIndexOf('}');
            if (close < 0)
            {
                return json;
            }

            int lastValueEnd = close - 1;
            while (lastValueEnd >= 0 && char.IsWhiteSpace(json[lastValueEnd]))
            {
                lastValueEnd--;
            }

            string newline = json.Contains("\r\n") ? "\r\n" : "\n";
            Match firstField = Regex.Match(json, "\\{\\r?\\n([ \\t]*)\"");
            string indent = firstField.Success ? firstField.Groups[1].Value : "  ";
            return json.Substring(0, lastValueEnd + 1) + "," + newline + indent +
                   $"\"meta\": {{ \"optimalMoves\": {optimalMoves} }}" + json.Substring(lastValueEnd + 1);
        }
    }
}
