using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BallSort.Editor.Art
{
    /// <summary>
    /// Trims PNGs to their drawn pixels. Sizes are padded to multiples of 4 with transparent pixels,
    /// which block compression needs. Trimming a trimmed file changes nothing, so it is safe to rerun.
    /// </summary>
    internal static class SpriteTrim
    {
        /// <summary>Trims every PNG directly inside <paramref name="folder"/> to its own content.</summary>
        public static void TrimEach(string folder, StringBuilder report)
        {
            foreach (string path in PngsIn(folder))
            {
                PixelImage image = PixelImage.Load(path);
                Apply(path, image, PadToMultipleOf4(image.ContentBounds()), report);
            }
        }

        /// <summary>
        /// Trims every PNG directly inside <paramref name="folder"/> to one rectangle holding all their
        /// content, so files of one swap set keep lining up. They must share one canvas size.
        /// </summary>
        public static void TrimShared(string folder, StringBuilder report)
        {
            List<string> paths = PngsIn(folder);
            var images = new List<PixelImage>();
            RectInt union = default;
            foreach (string path in paths)
            {
                PixelImage image = PixelImage.Load(path);
                if (images.Count > 0 && (image.Width != images[0].Width || image.Height != images[0].Height))
                {
                    report.AppendLine($"{folder}: files differ in size, so they cannot share one rectangle; skipped.");
                    return;
                }

                RectInt bounds = image.ContentBounds();
                union = images.Count == 0 ? bounds : Union(union, bounds);
                images.Add(image);
            }

            RectInt rect = PadToMultipleOf4(union);
            for (int i = 0; i < paths.Count; i++)
            {
                Apply(paths[i], images[i], rect, report);
            }
        }

        /// <summary>
        /// Trims every PNG directly inside <paramref name="folder"/> to its own columns and to the rows
        /// holding all their content, so parts drawn in place on one canvas keep one height and line up
        /// top and bottom when laid side by side. They must share one canvas height.
        /// </summary>
        public static void TrimSharedRows(string folder, StringBuilder report)
        {
            List<string> paths = PngsIn(folder);
            var images = new List<PixelImage>();
            var columns = new List<RectInt>();
            int yMin = int.MaxValue;
            int yMax = int.MinValue;
            foreach (string path in paths)
            {
                PixelImage image = PixelImage.Load(path);
                if (images.Count > 0 && image.Height != images[0].Height)
                {
                    report.AppendLine($"{folder}: files differ in height, so they cannot share their rows; skipped.");
                    return;
                }

                RectInt bounds = image.ContentBounds();
                yMin = Mathf.Min(yMin, bounds.yMin);
                yMax = Mathf.Max(yMax, bounds.yMax);
                images.Add(image);
                columns.Add(bounds);
            }

            for (int i = 0; i < paths.Count; i++)
            {
                var rect = new RectInt(columns[i].x, yMin, columns[i].width, yMax - yMin);
                Apply(paths[i], images[i], PadToMultipleOf4(rect), report);
            }
        }

        /// <summary>
        /// Cuts the files of <paramref name="rule"/>'s folder still on its canvas to one rectangle holding
        /// all their content, each pivoted on the rule's anchor, so a set drawn in place stays in place.
        /// </summary>
        public static void TrimAnchored(CanvasAnchor rule, Dictionary<string, Vector2> pivots, StringBuilder report)
        {
            var paths = new List<string>();
            var images = new List<PixelImage>();
            RectInt union = default;
            foreach (string path in PngsIn(rule.Folder))
            {
                PixelImage image = PixelImage.Load(path);
                if (image.Width != rule.Canvas.x || image.Height != rule.Canvas.y)
                {
                    continue;
                }

                RectInt bounds = image.ContentBounds();
                union = images.Count == 0 ? bounds : Union(union, bounds);
                paths.Add(path);
                images.Add(image);
            }

            if (paths.Count == 0)
            {
                return;
            }

            RectInt rect = PadToMultipleOf4(union);
            for (int i = 0; i < paths.Count; i++)
            {
                images[i].SaveCropped(paths[i], rect);
                pivots[paths[i]] = NormalisedPivot(rule.Anchor, rect);
            }

            report.AppendLine($"{rule.Folder}: {paths.Count} files cut to {rect.width}×{rect.height}, anchored at {rule.Anchor}.");
        }

        public static int PaddingToMultipleOf4(int size) => (4 - size % 4) % 4;

        /// <summary>Grows <paramref name="rect"/> evenly on both sides to whole multiples of 4.</summary>
        public static RectInt PadToMultipleOf4(RectInt rect)
        {
            int padX = PaddingToMultipleOf4(rect.width);
            int padY = PaddingToMultipleOf4(rect.height);
            return new RectInt(rect.x - padX / 2, rect.y - padY / 2, rect.width + padX, rect.height + padY);
        }

        /// <summary><paramref name="point"/>, in source pixels, as a pivot of a sprite cut out at <paramref name="rect"/>.</summary>
        public static Vector2 NormalisedPivot(Vector2 point, RectInt rect) =>
            new Vector2((point.x - rect.x) / rect.width, (point.y - rect.y) / rect.height);

        /// <summary>PNG asset paths directly inside <paramref name="folder"/>, sorted.</summary>
        public static List<string> PngsIn(string folder)
        {
            var paths = new List<string>();
            if (!Directory.Exists(folder))
            {
                return paths;
            }

            foreach (string file in Directory.GetFiles(folder, "*.png"))
            {
                paths.Add(file.Replace('\\', '/'));
            }

            paths.Sort(System.StringComparer.Ordinal);
            return paths;
        }

        private static RectInt Union(RectInt a, RectInt b)
        {
            int xMin = Mathf.Min(a.xMin, b.xMin);
            int yMin = Mathf.Min(a.yMin, b.yMin);
            return new RectInt(xMin, yMin, Mathf.Max(a.xMax, b.xMax) - xMin, Mathf.Max(a.yMax, b.yMax) - yMin);
        }

        private static void Apply(string path, PixelImage image, RectInt rect, StringBuilder report)
        {
            if (rect.Equals(image.Bounds))
            {
                return;
            }

            image.SaveCropped(path, rect);
            report.AppendLine($"{Path.GetFileName(path)}: {image.Width}×{image.Height} → {rect.width}×{rect.height}");
        }
    }
}
