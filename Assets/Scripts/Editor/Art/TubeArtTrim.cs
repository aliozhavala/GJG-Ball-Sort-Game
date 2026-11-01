using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BallSort.Editor.Art
{
    /// <summary>
    /// Cuts the tube art delivered on one shared canvas into pieces that fit together without that
    /// canvas: the rim (back and front), middle and bottom parts, the full outline, the clay and the
    /// veil. The corks and stands are cut by <see cref="SpriteTrim.TrimAnchored"/>.
    /// </summary>
    /// <remarks>
    /// The delivered parts overlap (the same wall pixels sit in two files), which would draw the
    /// semi-transparent walls twice at the joins. So the middle keeps only the rows between the rim
    /// and the bottom part. Every piece is then anchored by its pivot to a point of the tube, on the
    /// walls' centre line: the rim and the veil to the rim's top, the middle to its own
    /// bottom, the bottom part, outline and clay to the base. <c>TubeView</c> places them from there.
    /// <para>
    /// The rim's two halves are cut to one rectangle (back and front must line up). They may arrive on
    /// their own, on the canvas of a tube cut earlier (the second delivery did): then only they are cut.
    /// It runs only while the files still share one canvas size (a fresh delivery); afterwards the
    /// pivots it wrote stay in the import settings.
    /// </para>
    /// </remarks>
    internal static class TubeArtTrim
    {
        private static readonly string[] BodyPaths =
        {
            SpriteRules.TubeMiddle, SpriteRules.TubeBottom, SpriteRules.TubeFull, SpriteRules.Clay, SpriteRules.Veil,
        };

        public static void Trim(Dictionary<string, Vector2> pivots, Dictionary<string, Vector4> borders, StringBuilder report)
        {
            var rimPaths = new List<string> { SpriteRules.TubeRimBack, SpriteRules.TubeRimFront };
            var images = new Dictionary<string, PixelImage>();
            foreach (string path in rimPaths)
            {
                if (!System.IO.File.Exists(path))
                {
                    report.AppendLine($"Tube: {path} is missing; tube art not trimmed.");
                    return;
                }

                images[path] = PixelImage.Load(path);
            }

            PixelImage canvas = images[SpriteRules.TubeRimBack];
            if (!ShareCanvas(images.Values, canvas))
            {
                report.AppendLine("Tube: already trimmed (the rim's halves no longer share one canvas); skipped.");
                return;
            }

            bool withBody = true;
            foreach (string path in BodyPaths)
            {
                if (!System.IO.File.Exists(path))
                {
                    withBody = false;
                    break;
                }

                PixelImage image = PixelImage.Load(path);
                withBody &= image.Width == canvas.Width && image.Height == canvas.Height;
                images[path] = image;
            }

            // Rows count up from the bottom of the canvas.
            RectInt rim = Union(images[SpriteRules.TubeRimBack].ContentBounds(), images[SpriteRules.TubeRimFront].ContentBounds());
            int rimY = rim.yMax;
            float centreX = withBody ? images[SpriteRules.TubeMiddle].ContentBounds().center.x : rim.center.x;

            RectInt rimRect = SpriteTrim.PadToMultipleOf4(rim);
            if (withBody)
            {
                CutBody(images, rim, centreX, rimY, pivots, borders, report, out rimRect);
            }

            Save(images, SpriteRules.TubeRimBack, rimRect, centreX, rimY, pivots);
            Save(images, SpriteRules.TubeRimFront, rimRect, centreX, rimY, pivots);

            report.AppendLine($"Tube: rim cut to {rimRect.width}×{rimRect.height} px (back and front)" +
                $"{(withBody ? ", body cut too" : "")}; centre x {centreX}.");
        }

        /// <summary>The full set on one canvas: the middle stops where the rim starts.</summary>
        private static void CutBody(
            Dictionary<string, PixelImage> images, RectInt rim, float centreX, int rimY,
            Dictionary<string, Vector2> pivots, Dictionary<string, Vector4> borders, StringBuilder report, out RectInt rimRect)
        {
            RectInt middle = images[SpriteRules.TubeMiddle].ContentBounds();
            RectInt bottom = images[SpriteRules.TubeBottom].ContentBounds();
            RectInt full = images[SpriteRules.TubeFull].ContentBounds();

            // One column range for the parts and the outline, so they stay centred on each other.
            int xMin = Mathf.Min(Mathf.Min(rim.xMin, middle.xMin), Mathf.Min(bottom.xMin, full.xMin));
            int xMax = Mathf.Max(Mathf.Max(rim.xMax, middle.xMax), Mathf.Max(bottom.xMax, full.xMax));
            int widthPadding = SpriteTrim.PaddingToMultipleOf4(xMax - xMin);
            int x = xMin - widthPadding / 2;
            int width = xMax - xMin + widthPadding;

            // Padding goes where it touches no other part: under the bottom part, over the rim.
            // The middle is stretched to fill its gap, so it is shortened instead.
            int bottomPadding = SpriteTrim.PaddingToMultipleOf4(bottom.height);
            var bottomRect = new RectInt(x, bottom.yMin - bottomPadding, width, bottom.height + bottomPadding);
            int middleHeight = rim.yMin - bottom.yMax;
            var middleRect = new RectInt(x, bottom.yMax, width, middleHeight - middleHeight % 4);
            rimRect = new RectInt(x, rim.yMin, width, rim.height + SpriteTrim.PaddingToMultipleOf4(rim.height));
            var fullRect = new RectInt(x, full.yMin, width, full.height + SpriteTrim.PaddingToMultipleOf4(full.height));

            int baseY = bottom.yMin;
            Save(images, SpriteRules.TubeMiddle, middleRect, centreX, middleRect.y, pivots);
            Save(images, SpriteRules.TubeBottom, bottomRect, centreX, baseY, pivots);
            Save(images, SpriteRules.TubeFull, fullRect, centreX, baseY, pivots);
            Save(images, SpriteRules.Clay, SpriteTrim.PadToMultipleOf4(images[SpriteRules.Clay].ContentBounds()), centreX, baseY, pivots);
            Save(images, SpriteRules.Veil, SpriteTrim.PadToMultipleOf4(images[SpriteRules.Veil].ContentBounds()), centreX, rimY, pivots);

            // The outline is sliced at the joins, so its rim and rounded bottom keep their shape.
            borders[SpriteRules.TubeFull] = new Vector4(0f, bottom.yMax - fullRect.y, 0f, fullRect.yMax - rim.yMin);
            report.AppendLine($"Tube: cut into rim {rimRect.height}, middle {middleRect.height} and bottom " +
                $"{bottomRect.height} px, {width} px wide; clay and veil anchored.");
        }

        private static void Save(
            Dictionary<string, PixelImage> images, string path, RectInt rect, float centreX, float anchorY,
            Dictionary<string, Vector2> pivots)
        {
            images[path].SaveCropped(path, rect);
            pivots[path] = SpriteTrim.NormalisedPivot(new Vector2(centreX, anchorY), rect);
        }

        private static bool ShareCanvas(IEnumerable<PixelImage> images, PixelImage canvas)
        {
            foreach (PixelImage image in images)
            {
                if (image.Width != canvas.Width || image.Height != canvas.Height)
                {
                    return false;
                }
            }

            return true;
        }

        private static RectInt Union(RectInt a, RectInt b)
        {
            int xMin = Mathf.Min(a.xMin, b.xMin);
            int yMin = Mathf.Min(a.yMin, b.yMin);
            return new RectInt(xMin, yMin, Mathf.Max(a.xMax, b.xMax) - xMin, Mathf.Max(a.yMax, b.yMax) - yMin);
        }
    }
}
