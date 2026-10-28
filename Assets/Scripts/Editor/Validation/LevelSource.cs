namespace BallSort.Editor.Validation
{
    /// <summary>
    /// One level file as read on the main thread, so it can be checked on a worker thread without
    /// touching Unity objects.
    /// </summary>
    public sealed class LevelSource
    {
        public LevelSource(int number, string name, string assetPath, string json)
        {
            Number = number;
            Name = name;
            AssetPath = assetPath;
            Json = json;
        }

        /// <summary>1-based position in the level database.</summary>
        public int Number { get; }

        public string Name { get; }

        /// <summary>Project-relative path of the JSON file, or null for an empty database slot.</summary>
        public string AssetPath { get; }

        /// <summary>The file's text, or null for an empty database slot.</summary>
        public string Json { get; }
    }
}
