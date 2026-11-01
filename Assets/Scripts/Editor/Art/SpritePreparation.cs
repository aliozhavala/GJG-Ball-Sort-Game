using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Art
{
    /// <summary>
    /// Ball Sort ▸ Art ▸ Prepare Sprites: makes delivered art ready to use in one step. Trims the
    /// files in place (each on its own, swap sets to one shared rectangle, the tube cut from its shared
    /// canvas), then applies the import settings of <see cref="SpriteRules"/> to every art folder.
    /// </summary>
    /// <remarks>
    /// For a new delivery: overwrite the files under their names in <c>Assets/Art/Sprites/</c> (the
    /// <c>.meta</c> files and every reference survive), then run this. Already trimmed files are left
    /// as they are, so it can be run any time. Placeholders are not touched.
    /// </remarks>
    internal static class SpritePreparation
    {
        private static readonly string[] MobilePlatforms = { "Android", "iPhone" };

        [MenuItem("Ball Sort/Art/Prepare Sprites")]
        private static void Prepare()
        {
            var report = new StringBuilder("Prepare Sprites\n");
            var pivots = new Dictionary<string, Vector2>();
            var borders = new Dictionary<string, Vector4>();

            MoveDeliveries(report);
            ReplaceDeliveries(report);
            RemoveEmptyIncoming(report);

            foreach (string folder in SpriteRules.TrimEach)
            {
                SpriteTrim.TrimEach(folder, report);
            }

            foreach (string folder in SpriteRules.TrimShared)
            {
                SpriteTrim.TrimShared(folder, report);
            }

            foreach (string folder in SpriteRules.TrimSharedRows)
            {
                SpriteTrim.TrimSharedRows(folder, report);
            }

            foreach (CanvasAnchor rule in SpriteRules.CanvasAnchored)
            {
                SpriteTrim.TrimAnchored(rule, pivots, report);
            }

            TubeArtTrim.Trim(pivots, borders, report);
            int count = ApplyImportSettings(pivots, borders);
            report.AppendLine($"Import settings applied to {count} sprites.");
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Moves each delivered file still in <see cref="SpriteRules.Incoming"/> to its place, through the
        /// AssetDatabase so its <c>.meta</c> and GUID come along. A destination that already exists is
        /// left alone and reported: replacing art goes by overwriting the file (docs/art-inventory.md).
        /// </summary>
        private static void MoveDeliveries(StringBuilder report)
        {
            foreach ((string from, string to) in SpriteRules.Deliveries)
            {
                string source = $"{SpriteRules.Incoming}/{from}";
                if (!File.Exists(source))
                {
                    continue;
                }

                if (File.Exists(to))
                {
                    report.AppendLine($"{source}: {to} already exists; not moved.");
                    continue;
                }

                EnsureFolder(Path.GetDirectoryName(to)?.Replace('\\', '/'));
                string error = AssetDatabase.MoveAsset(source, to);
                report.AppendLine(string.IsNullOrEmpty(error) ? $"Moved {source} → {to}" : $"{source}: {error}");
            }
        }

        /// <summary>
        /// Copies each delivered replacement over the file it replaces (<see cref="SpriteRules.Replacements"/>),
        /// so the file keeps its <c>.meta</c>, GUID and references, then puts the delivered copy in the
        /// trash. A replacement whose target does not exist is reported and left in Incoming.
        /// </summary>
        private static void ReplaceDeliveries(StringBuilder report)
        {
            foreach ((string from, string to) in SpriteRules.Replacements)
            {
                string source = $"{SpriteRules.Incoming}/{from}";
                if (!File.Exists(source))
                {
                    continue;
                }

                if (!File.Exists(to))
                {
                    report.AppendLine($"{source}: {to} does not exist, nothing to replace; left in Incoming.");
                    continue;
                }

                File.Copy(source, to, true);
                AssetDatabase.ImportAsset(to, ImportAssetOptions.ForceUpdate);
                AssetDatabase.MoveAssetToTrash(source);
                report.AppendLine($"Replaced {to} with {source}");
            }
        }

        /// <summary>Removes folders under Incoming (and Incoming itself) that the moves have emptied.</summary>
        private static void RemoveEmptyIncoming(StringBuilder report)
        {
            if (!AssetDatabase.IsValidFolder(SpriteRules.Incoming))
            {
                return;
            }

            foreach (string folder in AssetDatabase.GetSubFolders(SpriteRules.Incoming))
            {
                TrashIfEmpty(folder, report);
            }

            TrashIfEmpty(SpriteRules.Incoming, report);
        }

        private static void TrashIfEmpty(string folder, StringBuilder report)
        {
            foreach (string sub in AssetDatabase.GetSubFolders(folder))
            {
                TrashIfEmpty(sub, report);
            }

            if (Directory.GetFiles(folder).Length == 0 && Directory.GetDirectories(folder).Length == 0)
            {
                AssetDatabase.MoveAssetToTrash(folder);
                report.AppendLine($"Removed the empty folder {folder}");
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <param name="pivots">Pivots worked out while trimming, by asset path; they win over the rules.</param>
        /// <param name="borders">Borders worked out while trimming, by asset path; they win over the rules.</param>
        private static int ApplyImportSettings(
            IReadOnlyDictionary<string, Vector2> pivots, IReadOnlyDictionary<string, Vector4> borders)
        {
            int count = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (FolderImport rule in SpriteRules.Imports)
                {
                    foreach (string path in SpriteTrim.PngsIn(rule.Folder))
                    {
                        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                        {
                            Configure(importer, rule, pivots, borders);
                            importer.SaveAndReimport();
                            count++;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return count;
        }

        private static void Configure(
            TextureImporter importer, FolderImport rule,
            IReadOnlyDictionary<string, Vector2> pivots, IReadOnlyDictionary<string, Vector4> borders)
        {
            string path = importer.assetPath;
            string name = Path.GetFileNameWithoutExtension(path);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = SpriteRules.PixelsPerUnit;
            settings.spriteMeshType = rule.TightMesh ? SpriteMeshType.Tight : SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.mipmapEnabled = false;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.filterMode = FilterMode.Bilinear;
            settings.sRGBTexture = true;
            settings.alphaSource = rule.Opaque ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            settings.alphaIsTransparency = !rule.Opaque;

            // Pivots and borders not named anywhere keep what the importer already has, e.g. the tube
            // pivots written by an earlier run.
            if (pivots.TryGetValue(path, out Vector2 pivot) || TryGetFolderPivot(rule, out pivot))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
            }

            if (borders.TryGetValue(path, out Vector4 border) || SpriteRules.Borders.TryGetValue(name, out border))
            {
                settings.spriteBorder = border;
            }

            importer.SetTextureSettings(settings);
            importer.maxTextureSize = rule.MaxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;

            foreach (string platform in MobilePlatforms)
            {
                TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(platform);
                platformSettings.overridden = true;
                platformSettings.maxTextureSize = rule.MaxSize;
                platformSettings.format = rule.MobileFormat;
                importer.SetPlatformTextureSettings(platformSettings);
            }
        }

        private static bool TryGetFolderPivot(FolderImport rule, out Vector2 pivot)
        {
            pivot = rule.Pivot ?? default;
            return rule.Pivot.HasValue;
        }
    }
}
