using System.IO;
using UnityEngine;

namespace BallSort.Editor.Art
{
    /// <summary>
    /// A PNG's pixels read straight from the file, bottom row first (Unity's texture order), so they
    /// can be measured and cropped whatever the texture's import settings are.
    /// </summary>
    internal sealed class PixelImage
    {
        private PixelImage(Color32[] pixels, int width, int height)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
        }

        public Color32[] Pixels { get; }

        public int Width { get; }

        public int Height { get; }

        public RectInt Bounds => new RectInt(0, 0, Width, Height);

        public static PixelImage Load(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));
                return new PixelImage(texture.GetPixels32(), texture.width, texture.height);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// The smallest rectangle holding every pixel that is not fully transparent; the whole image if
        /// it is empty.
        /// </summary>
        public RectInt ContentBounds()
        {
            int xMin = Width, yMin = Height, xMax = -1, yMax = -1;
            for (int y = 0; y < Height; y++)
            {
                int row = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    if (Pixels[row + x].a != 0)
                    {
                        xMin = Mathf.Min(xMin, x);
                        xMax = Mathf.Max(xMax, x);
                        yMin = Mathf.Min(yMin, y);
                        yMax = Mathf.Max(yMax, y);
                    }
                }
            }

            return xMax < 0 ? Bounds : new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        /// <summary>
        /// Writes the part of the image inside <paramref name="rect"/> to <paramref name="path"/> as a
        /// PNG. The rectangle may reach past the image's edges; the extra pixels are transparent.
        /// </summary>
        public void SaveCropped(string path, RectInt rect)
        {
            var cropped = new Color32[rect.width * rect.height];
            for (int y = 0; y < rect.height; y++)
            {
                int sourceY = rect.y + y;
                if (sourceY < 0 || sourceY >= Height)
                {
                    continue;
                }

                for (int x = 0; x < rect.width; x++)
                {
                    int sourceX = rect.x + x;
                    if (sourceX >= 0 && sourceX < Width)
                    {
                        cropped[y * rect.width + x] = Pixels[sourceY * Width + sourceX];
                    }
                }
            }

            var texture = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(cropped);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
