using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Audio
{
    /// <summary>
    /// Mobile import settings for sound effects under <c>Assets/Audio/SFX/</c>, applied the first time a
    /// file is imported (later Inspector changes stay; a file copied over an existing one keeps its
    /// settings). Short clips decompress on load as ADPCM, so playing one costs no decoding; the long
    /// ones (fanfare, reveal) stay Vorbis-compressed in memory. All preload, so the first play never
    /// stalls. Levels are left as delivered: the quieter WAVs are on purpose (docs/design/SFX_GUIDE.md).
    /// </summary>
    internal sealed class SfxImportRules : AssetPostprocessor
    {
        private const string Folder = "Assets/Audio/SFX/";

        // Clips long enough that keeping them compressed saves more memory than decoding costs.
        private static readonly string[] LongClips = { "sfx_win_fanfare", "sfx_hidden_reveal" };

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            importer.loadInBackground = false;
            importer.forceToMono = false;

            bool isLong = false;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            foreach (string longClip in LongClips)
            {
                isLong |= name == longClip;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.preloadAudioData = true;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.loadType = isLong ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = isLong ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = 0.7f;
            importer.defaultSampleSettings = settings;
        }
    }
}
