using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BallSort.Editor.Art
{
    /// <summary>
    /// Cuts the tube art delivered on one shared canvas into pieces that fit together without that
    /// canvas: the top, middle and bottom parts, the full outline, the clay and the veil.
    /// </summary>
    /// <remarks>
    /// The delivered parts overlap (the same wall pixels sit in two files), which would draw the
    /// semi-transparent walls twice at the joins. So the middle keeps only the rows between the top
    /// part and the bottom part. Every piece is then anchored by its pivot to a point of the tube, on
    /// the walls' centre line: the top part and the veil to the rim's top, the middle to its own
    /// bottom, the bottom part, outline and clay to the base. <c>TubeView</c> places them from there.
    /// <para>
    /// Runs only while all six files still share one canvas size (a fresh delivery); afterwards the
    /// pivots it wrote stay in the import settings.
    /// </para>
    /// </remarks>
    internal static class TubeArtTrim
    {
        private static readonly string[] Paths =
        {
            SpriteRules.TubeTop, SpriteRules.TubeMiddle, SpriteRules.TubeBottom,
            SpriteRules.TubeFull, SpriteRules.Clay, SpriteRules.Veil,
        };

        public static void Trim(Dictionary<string, Vector2> pivots, Dictionary<string, Vector4> borders, StringBuilder report)
        {
            var images = new Dictionary<string, PixelImage>();
            foreach (string path in Paths)
            {
                if (!System.IO.File.Exists(path))
                {
                    report.AppendLine($"Tube: {path} is missing; tube art not trimmed.");
                    return;
                }

                images[path] = PixelImage.Load(path);
            }

            PixelImage first = images[SpriteRules.TubeTop];
            foreach (PixelImage image in images.Values)
            {
                if (image.Width != first.Width || image.Height != first.Height)
                {
                    report.AppendLine("Tube: already trimmed (the pieces no longer share one canvas); skipped.");
                    return;
                }
            }

            RectInt top = images[SpriteRules.TubeTop].ContentBounds();
            RectInt middle = images[SpriteRules.TubeMiddle].ContentBounds();
            RectInt bottom = images[SpriteRules.TubeBottom].ContentBounds();
            RectInt full = images[SpriteRules.TubeFull].ContentBounds();
            if (middle.yMin > bottom.yMax || middle.yMax < top.yMin)
            {
                report.AppendLine("Tube: the middle part does not reach the top and bottom parts; tube art not trimmed.");
                return;
            }

            // Rows count up from the bottom of the canvas.
            float centreX = middle.center.x;
            int baseY = bottom.yMin;
            int rimY = top.yMax;

            // One column range for the parts and the outline, so they stay centred on each other.
            int xMin = Mathf.Min(Mathf.Min(top.xMin, middle.xMin), Mathf.Min(bottom.xMin, full.xMin));
            int xMax = Mathf.Max(Mathf.Max(top.xMax, middle.xMax), Mathf.Max(bottom.xMax, full.xMax));
            int widthPadding = SpriteTrim.PaddingToMultipleOf4(xMax - xMin);
            int x = xMin - widthPadding / 2;
            int width = xMax - xMin + widthPadding;

            // Padding goes where it touches no other part: under the bottom part, over the top part.
            // The middle is stretched to fill its gap, so it is shortened instead.
            int bottomPadding = SpriteTrim.PaddingToMultipleOf4(bottom.height);
            var bottomRect = new RectInt(x, bottom.yMin - bottomPadding, width, bottom.height + bottomPadding);
            int middleHeight = top.yMin - bottom.yMax;
            var middleRect = new RectInt(x, bottom.yMax, width, middleHeight - middleHeight % 4);
            var topRect = new RectInt(x, top.yMin, width, top.height + SpriteTrim.PaddingToMultipleOf4(top.height));
            var fullRect = new RectInt(x, full.yMin, width, full.height + SpriteTrim.PaddingToMultipleOf4(full.height));
            RectInt clayRect = SpriteTrim.PadToMultipleOf4(images[SpriteRules.Clay].ContentBounds());
            RectInt veilRect = SpriteTrim.PadToMultipleOf4(images[SpriteRules.Veil].ContentBounds());

            Save(SpriteRules.TubeTop, topRect, rimY);
            Save(SpriteRules.TubeMiddle, middleRect, middleRect.y);
            Save(SpriteRules.TubeBottom, bottomRect, baseY);
            Save(SpriteRules.TubeFull, fullRect, baseY);
            Save(SpriteRules.Clay, clayRect, baseY);
            Save(SpriteRules.Veil, veilRect, rimY);

            // The outline is sliced at the joins, so its rim and rounded bottom keep their shape.
            borders[SpriteRules.TubeFull] = new Vector4(0f, bottom.yMax - fullRect.y, 0f, fullRect.yMax - top.yMin);
            report.AppendLine($"Tube: cut into top {topRect.height}, middle {middleRect.height} and bottom " +
                $"{bottomRect.height} px, {width} px wide; body centre x {centreX}, clay and veil anchored.");

            void Save(string path, RectInt rect, float anchorY)
            {
                images[path].SaveCropped(path, rect);
                pivots[path] = SpriteTrim.NormalisedPivot(new Vector2(centreX, anchorY), rect);
            }
        }
    }
}
