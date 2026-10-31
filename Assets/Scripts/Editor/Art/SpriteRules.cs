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

        /// <summary>Where the artist's files are dropped before <see cref="Deliveries"/> moves them.</summary>
        public const string Incoming = "Assets/Art/Incoming";

        /// <summary>Art kept in the project for later, outside every rule (not trimmed, not referenced).</summary>
        public const string Unused = "Assets/Art/Unused";

        /// <summary>
        /// Delivered files (relative to <see cref="Incoming"/>) and where they belong, renamed to the
        /// project's names. <see cref="SpritePreparation"/> moves each one that is still in Incoming,
        /// keeping its <c>.meta</c> and GUID, before trimming; entries already moved are skipped.
        /// </summary>
        public static readonly (string From, string To)[] Deliveries =
        {
            // Third delivery (2026-10-31): icons, popups, settings, leaderboard, super hard button.
            ("button/button_superhard.png", Root + "/UI/Buttons/Button_SuperHard.png"),
            ("button/button_settings_off.png", Root + "/UI/Settings/Toggle_Off.png"),
            ("button/button_settings_on.png", Root + "/UI/Settings/Toggle_On.png"),
            ("icons/icon_coin.png", Root + "/UI/Icons/Icon_Coin.png"),
            ("icons/icon_coin2.png", Root + "/UI/Icons/Icon_CoinPouch.png"),
            ("icons/icon_coin3.png", Root + "/UI/Icons/Icon_CoinChest.png"),
            ("icons/icon_coin4.png", Root + "/UI/Icons/Icon_CoinChestFull.png"),
            ("icons/icon_coin5.png", Root + "/UI/Icons/Icon_CoinTank.png"),
            ("icons/icon_coin6.png", Root + "/UI/Icons/Icon_CoinTankFull.png"),
            ("icons/icon_heart.png", Root + "/UI/Icons/Icon_Heart.png"),
            ("icons/icon_heart_broken.png", Root + "/UI/Icons/Icon_HeartBroken.png"),
            ("icons/icon_noads.png", Root + "/UI/Icons/Icon_NoAds.png"),
            ("icons/icon_plus.png", Root + "/UI/Icons/Icon_Plus.png"),
            ("leaderboard/leaderboard_title.png", Root + "/UI/Leaderboard/Leaderboard_Header.png"),
            ("leaderboard/leaderboard_menu.png", Root + "/UI/Leaderboard/Leaderboard_Panel.png"),
            ("leaderboard/leaderboard_player_default.png", Root + "/UI/Leaderboard/Leaderboard_Row.png"),
            ("leaderboard/leaderboard_player_me.png", Root + "/UI/Leaderboard/Leaderboard_RowMe.png"),
            ("pop-up menus/ingame3_menu.png", Root + "/UI/Popups/Popup_Panel.png"),
            ("pop-up menus/settings_menu.png", Root + "/UI/Popups/Settings_Panel.png"),
            // Kept for later, untouched.
            ("icons/icon_clock.png", Unused + "/Icon_Clock.png"),
            ("pop-up menus/ingame_menu.png", Unused + "/Popup_Panel_Header_Cream.png"),
            ("pop-up menus/ingame2_menu.png", Unused + "/Popup_Panel_Header.png"),
            ("pop-up menus/ingame4_menu.png", Unused + "/Popup_Panel_Wide_Cream.png"),
            ("pop-up menus/ingame5_menu.png", Unused + "/Popup_Panel_Cream.png"),
        };

        /// <summary>One value for every sprite, so sizes in world units compare like the art does.</summary>
        public const float PixelsPerUnit = 100f;

        /// <summary>Folders whose files are each trimmed to their own drawn pixels.</summary>
        public static readonly string[] TrimEach =
        {
            Root + "/UI/Buttons",
            Root + "/UI/Displays",
            Root + "/UI/Shop",
            Root + "/UI/Avatar",
            Root + "/UI/Icons",
            Root + "/UI/Popups",
            Root + "/UI/Leaderboard",
        };

        /// <summary>
        /// Folders holding one swap set (states of one element): their files are trimmed to one shared
        /// rectangle, so the art does not jump when one replaces another.
        /// </summary>
        public static readonly string[] TrimShared =
        {
            Root + "/UI/Home",
            Root + "/UI/AvatarFrames",
            Root + "/UI/Settings",
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
            // Icons are shown at most ≈300 px (the shop bundles); 512 keeps them sharp on large phones.
            new FolderImport(Root + "/UI/Icons", 512, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Root + "/UI/Popups", 1024, TextureImporterFormat.ASTC_6x6),
            new FolderImport(Root + "/UI/Settings", 256, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Root + "/UI/Leaderboard", 1024, TextureImporterFormat.ASTC_6x6),
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
            // Hard tiers' button: only the plain red middle (columns 150–564) stretches sideways, so the
            // pill matches the green button's width; the right end keeps the tilted tag. No top or bottom
            // border: it is drawn at its own aspect's height (LevelTierLook).
            ["Button_SuperHard"] = new Vector4(150f, 0f, 320f, 0f),
            // The coin or heart is part of the left end.
            ["Display_Coins"] = new Vector4(380f, 150f, 120f, 150f),
            ["Display_Lives"] = new Vector4(400f, 140f, 120f, 140f),
            // The towel's domed top and its hem stay; the straight stripes stretch for large tubes.
            ["Veil_Towel"] = new Vector4(0f, 110f, 0f, 70f),
            // Popup panel: the rounded frame and the inner panel's corners stay; the plain blue stretches
            // both ways (rows and columns 60–760 of the trimmed art are flat).
            ["Popup_Panel"] = new Vector4(100f, 110f, 100f, 100f),
            // Leaderboard panel: the tubes, the top frame and its orange corners (rows 0–470) and the bottom
            // frame stay; the box's plain middle (rows 470–800) stretches to fit the rows. It is never
            // stretched sideways (scaled uniformly to its width), so it has no side borders.
            ["Leaderboard_Panel"] = new Vector4(0f, 130f, 0f, 470f),
        };
    }
}
