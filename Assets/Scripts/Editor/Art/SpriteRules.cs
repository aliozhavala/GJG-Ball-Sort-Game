using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Art
{
    /// <summary>
    /// How each art folder is trimmed and imported by <see cref="SpritePreparation"/>. A new delivery
    /// that replaces files under the same names needs no change here; a new folder or a new sliced
    /// sprite does (docs/art-inventory.md).
    /// </summary>
    internal static class SpriteRules
    {
        public const string Root = "Assets/Art/Sprites";

        /// <summary>One value for every sprite, so sizes in world units compare like the art does.</summary>
        public const float PixelsPerUnit = 100f;

        /// <summary>Folders whose files are each trimmed to their own drawn pixels.</summary>
        public static readonly string[] TrimEach =
        {
            Root + "/UI/Buttons",
            Root + "/UI/Displays",
            Root + "/UI/Shop",
            Root + "/UI/Avatar",
        };

        /// <summary>
        /// Folders holding one swap set (states of one element): their files are trimmed to one shared
        /// rectangle, so the art does not jump when one replaces another.
        /// </summary>
        public static readonly string[] TrimShared =
        {
            Root + "/UI/Home",
            Root + "/UI/AvatarFrames",
            Root + "/Episodes",
        };

        /// <summary>
        /// The tube's parts, its full outline, the clay, the veil and the corks, delivered on one shared
        /// canvas with each piece in place (<see cref="TubeArtTrim"/>). The rim is two halves: the back
        /// draws behind the balls, the front over them.
        /// </summary>
        public const string TubeRimBack = Root + "/Tubes/Tube_RimBack.png";
        public const string TubeRimFront = Root + "/Tubes/Tube_RimFront.png";
        public const string TubeMiddle = Root + "/Tubes/Tube_Middle.png";
        public const string TubeBottom = Root + "/Tubes/Tube_Bottom.png";
        public const string TubeFull = Root + "/Tubes/Tube_Full.png";
        public const string Clay = Root + "/Mechanics/Clay_Ice.png";
        public const string Veil = Root + "/Mechanics/Veil_Towel.png";

        /// <summary>One cork per ball colour (<c>Cork_&lt;Colour&gt;</c>) and a plain one for colours without art.</summary>
        public const string Corks = Root + "/Tubes/Corks";

        public static readonly FolderImport[] Imports =
        {
            // Monsters: the pivot is the round body's centre, measured on the delivered art (130, 148
            // from the top left of 256), so appendages do not shift a ball in its slot.
            new FolderImport(Root + "/Balls", 256, TextureImporterFormat.ASTC_4x4,
                tightMesh: true, pivot: new Vector2(130f / 256f, 1f - 148f / 256f)),
            new FolderImport(Root + "/Tubes", 1024, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Corks, 256, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Root + "/Mechanics", 1024, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Root + "/UI/Buttons", 1024, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Root + "/UI/Displays", 1024, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Root + "/UI/Home", 1024, TextureImporterFormat.ASTC_6x6),
            new FolderImport(Root + "/UI/Shop", 1024, TextureImporterFormat.ASTC_6x6),
            new FolderImport(Root + "/UI/Avatar", 1024, TextureImporterFormat.ASTC_6x6),
            new FolderImport(Root + "/UI/AvatarFrames", 1024, TextureImporterFormat.ASTC_6x6),
            // Player pictures: opaque squares shown at most ≈300 px (masked to a circle in their frame).
            new FolderImport(Root + "/UI/Avatars", 512, TextureImporterFormat.ASTC_6x6, opaque: true),
            new FolderImport(Root + "/Episodes", 1024, TextureImporterFormat.ASTC_6x6),
            new FolderImport(Root + "/Backgrounds", 2048, TextureImporterFormat.ASTC_6x6, opaque: true),
        };

        /// <summary>Pivots that differ from their folder's, by file name.</summary>
        public static readonly Dictionary<string, Vector2> PivotOverrides = new Dictionary<string, Vector2>
        {
            // The hidden ball is drawn bigger, centred at (128, 144).
            ["Ball_Hidden"] = new Vector2(0.5f, 1f - 144f / 256f),
        };

        /// <summary>
        /// 9-slice borders in pixels (left, bottom, right, top) by file name, measured on the trimmed
        /// art. <see cref="TubeArtTrim"/> sets the tube outline's own.
        /// </summary>
        public static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            // Pills: the rounded ends and their highlights stay; the plain middle stretches.
            ["Button_Green"] = new Vector4(140f, 150f, 130f, 140f),
            ["Button_Red"] = new Vector4(140f, 140f, 130f, 140f),
            ["Button_Yellow"] = new Vector4(140f, 140f, 130f, 140f),
            // The coin or heart is part of the left end.
            ["Display_Coins"] = new Vector4(380f, 150f, 120f, 150f),
            ["Display_Lives"] = new Vector4(400f, 140f, 120f, 140f),
            // The towel's domed top and its hem stay; the straight stripes stretch for large tubes.
            ["Veil_Towel"] = new Vector4(0f, 110f, 0f, 70f),
        };
    }
}
