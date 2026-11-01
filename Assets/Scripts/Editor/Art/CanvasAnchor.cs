using UnityEngine;

namespace BallSort.Editor.Art
{
    /// <summary>
    /// A set of art drawn in place on one shared canvas, whose sprites are pivoted on one point of that
    /// canvas (<see cref="SpriteTrim.TrimAnchored"/>).
    /// </summary>
    internal readonly struct CanvasAnchor
    {
        public CanvasAnchor(string folder, Vector2Int canvas, Vector2 anchor)
        {
            Folder = folder;
            Canvas = canvas;
            Anchor = anchor;
        }

        public string Folder { get; }

        /// <summary>The delivered canvas size; files of any other size are not part of a fresh delivery.</summary>
        public Vector2Int Canvas { get; }

        /// <summary>The pivot point in canvas pixels, rows counted up from the bottom.</summary>
        public Vector2 Anchor { get; }
    }
}
