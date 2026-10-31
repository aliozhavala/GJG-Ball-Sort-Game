using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BallSort.Game.Services
{
    /// <summary>
    /// Lets text the player types (their name) show characters the game's fonts lack, by adding the
    /// device's own fonts to TextMesh Pro's global fallback list while the app runs.
    /// </summary>
    /// <remarks>
    /// The game's font assets are static atlases covering only what the game itself writes (ASCII,
    /// Latin-1, Turkish, a few symbols). A name may hold anything else, so each family listed here that
    /// the device has becomes a dynamic font asset built from the system font file. They are created at
    /// runtime and never saved, so glyphs they add never touch a project file; the fallback list itself
    /// is put back on <see cref="Detach"/>, so the TMP Settings asset is left as it was (in the Editor
    /// it outlives Play mode). Families missing on a platform are skipped, so one list serves all.
    /// Colour emoji cannot be drawn by SDF text and show TMP's missing-glyph box.
    /// </remarks>
    public sealed class SystemFontFallback : MonoBehaviour
    {
        [Tooltip("System font families to fall back on, in order; ones the device lacks are skipped. " +
                 "Cover Android, iOS and the Editor's Windows/macOS.")]
        [SerializeField] private string[] _familyNames =
        {
            "Roboto", "Noto Sans CJK JP", "Noto Sans",
            "Helvetica Neue", "PingFang SC", "Apple SD Gothic Neo",
            "Segoe UI", "Microsoft YaHei", "Malgun Gothic",
        };

        [Tooltip("Face of each family to use.")]
        [SerializeField] private string _styleName = "Regular";

        private readonly List<TMP_FontAsset> _added = new List<TMP_FontAsset>();
        private List<TMP_FontAsset> _originalList;
        private bool _attached;

        /// <summary>Adds the device's fonts to TMP's global fallback list. Call once, at launch.</summary>
        public void Attach()
        {
            if (_attached || TMP_Settings.instance == null)
            {
                return;
            }

            _attached = true;
            _originalList = TMP_Settings.fallbackFontAssets;
            var list = _originalList != null ? new List<TMP_FontAsset>(_originalList) : new List<TMP_FontAsset>();

            // FontEngine names faces "Family - Style", the same table CreateFontAsset searches; checking
            // first keeps it from logging every family this platform lacks.
            var installed = new HashSet<string>(FontEngine.GetSystemFontNames());
            foreach (string family in _familyNames)
            {
                if (string.IsNullOrEmpty(family) || !installed.Contains($"{family} - {_styleName}"))
                {
                    continue;
                }

                TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(family, _styleName);
                if (font != null)
                {
                    font.name = $"{family} (system fallback)";
                    _added.Add(font);
                    list.Add(font);
                }
            }

            // A new list rather than adding to the asset's own, so Detach can put the original back.
            TMP_Settings.fallbackFontAssets = list;
        }

        /// <summary>Puts TMP's fallback list back as it was and frees the fonts made by <see cref="Attach"/>.</summary>
        public void Detach()
        {
            if (!_attached)
            {
                return;
            }

            _attached = false;
            if (TMP_Settings.instance != null)
            {
                TMP_Settings.fallbackFontAssets = _originalList;
            }

            foreach (TMP_FontAsset font in _added)
            {
                if (font != null)
                {
                    foreach (Texture2D atlas in font.atlasTextures)
                    {
                        Destroy(atlas);
                    }

                    Destroy(font.material);
                    Destroy(font);
                }
            }

            _added.Clear();
            _originalList = null;
        }
    }
}
