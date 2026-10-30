using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Art
{
    /// <summary>Import settings shared by every PNG directly inside one art folder.</summary>
    internal readonly struct FolderImport
    {
        public FolderImport(
            string folder, int maxSize, TextureImporterFormat mobileFormat,
            bool opaque = false, bool tightMesh = false, Vector2? pivot = null)
        {
            Folder = folder;
            MaxSize = maxSize;
            MobileFormat = mobileFormat;
            Opaque = opaque;
            TightMesh = tightMesh;
            Pivot = pivot;
        }

        public string Folder { get; }

        /// <summary>Largest texture side; bigger art is scaled down on import.</summary>
        public int MaxSize { get; }

        /// <summary>Compression on Android and iOS.</summary>
        public TextureImporterFormat MobileFormat { get; }

        /// <summary>Full-screen art with no transparency: imported without an alpha channel.</summary>
        public bool Opaque { get; }

        /// <summary>
        /// A mesh hugging the drawn pixels (less overdraw) instead of the full rectangle. Only for
        /// sprites drawn Simple: sliced sprites need the full rectangle.
        /// </summary>
        public bool TightMesh { get; }

        /// <summary>Normalised pivot for the whole folder; null keeps each sprite's own.</summary>
        public Vector2? Pivot { get; }
    }
}
