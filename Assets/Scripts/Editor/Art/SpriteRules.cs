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

        /// <summary>The lock box's parts, delivered on one canvas: they share their rows (<see cref="TrimSharedRows"/>).</summary>
        public const string LockBox = Root + "/Mechanics/LockBox";

        /// <summary>One key per lock colour, drawn on the ball that carries it.</summary>
        public const string Keys = Root + "/Mechanics/Keys";

        /// <summary>
        /// Delivered files (relative to <see cref="Incoming"/>) and where they belong, renamed to the
        /// project's names. <see cref="SpritePreparation"/> moves each one that is still in Incoming,
        /// keeping its <c>.meta</c> and GUID, before trimming; entries already moved are skipped.
        /// </summary>
        public static readonly (string From, string To)[] Deliveries =
        {
            // Fifth delivery, stage 2 (2026-11-02): Home backgrounds of episodes 1, 3 and 4, the Episode
            // Unlocked pictures and the cream popup panel (the same pixels as Unused/Popup_Panel_Cream).
            ("mainmenu_lab.png", Root + "/Backgrounds/Home_Ep1_Lab.png"),
            ("mainmenu_ice.png", Root + "/Backgrounds/Home_Ep3_Ice.png"),
            ("mainmenu_lava.png", Root + "/Backgrounds/Home_Ep4_Lava.png"),
            ("winmenu_summer.png", Root + "/Episodes/Unlocked/EpisodeUnlocked_Ep2_Summer.png"),
            ("winmenu_ice.png", Root + "/Episodes/Unlocked/EpisodeUnlocked_Ep3_Ice.png"),
            ("winmenu_lava.png", Root + "/Episodes/Unlocked/EpisodeUnlocked_Ep4_Lava.png"),
            ("winmenu_space.png", Root + "/Episodes/Unlocked/EpisodeUnlocked_Ep5_Space.png"),
            ("ingame5_menu.png", Root + "/UI/Popups/Popup_Panel_Cream.png"),
            // Fifth delivery (2026-11-01): Level HUD boxes and the large tubes' stands.
            ("ingame6_menu(boosterbox).png", Root + "/UI/Hud/Hud_BoosterBox.png"),
            ("ingame7_menu(level).png", Root + "/UI/Hud/Hud_LevelBox.png"),
            ("ingame7_menu(level)PURPLE.png", Root + "/UI/Hud/Hud_TierBox_Hard.png"),
            ("ingame7_menu(level)RED.png", Root + "/UI/Hud/Hud_TierBox_SuperHard.png"),
            ("tube_tabla_blue.png", Stands + "/Stand_Blue.png"),
            ("tube_tabla_brown.png", Stands + "/Stand_Brown.png"),
            ("tube_tabla_green.png", Stands + "/Stand_Green.png"),
            ("tube_tabla_lilac.png", Stands + "/Stand_Lilac.png"),
            ("tube_tabla_orange.png", Stands + "/Stand_Orange.png"),
            ("tube_tabla_pink.png", Stands + "/Stand_Pink.png"),
            ("tube_tabla_purple.png", Stands + "/Stand_Purple.png"),
            ("tube_tabla_red.png", Stands + "/Stand_Red.png"),
            ("tube_tabla_turq.png", Stands + "/Stand_Turquoise.png"),
            ("tube_tabla_yellow.png", Stands + "/Stand_Yellow.png"),
            // Fourth delivery (2026-11-01): hard tier button, booster buttons, win picture, lock box, keys.
            ("buttons/button_hard.png", Root + "/UI/Buttons/Button_Hard.png"),
            ("buttons/button_undo.png", Root + "/UI/Boosters/Booster_Undo.png"),
            ("buttons/button_shuffle.png", Root + "/UI/Boosters/Booster_Shuffle.png"),
            ("buttons/button_tube.png", Root + "/UI/Boosters/Booster_AddTube.png"),
            ("win pop-up/ingame_win.png", Root + "/UI/Win/Win_Picture.png"),
            ("lock and key stuff/tube_lock_default_side.png", LockBox + "/LockBox_Side.png"),
            ("lock and key stuff/tube_lock_default_face.png", LockBox + "/LockBox_Face.png"),
            ("lock and key stuff/tube_lock_default_inside.png", LockBox + "/LockBox_Inside.png"),
            ("lock and key stuff/tube_lock_default_cover.png", LockBox + "/LockBox_Cover.png"),
            ("lock and key stuff/tube_lock_default_mid_blue.png", LockBox + "/LockBox_Mid_Blue.png"),
            ("lock and key stuff/tube_lock_default_mid_green.png", LockBox + "/LockBox_Mid_Green.png"),
            ("lock and key stuff/tube_lock_default_mid_pink.png", LockBox + "/LockBox_Mid_Pink.png"),
            ("lock and key stuff/tube_lock_default_mid_purple.png", LockBox + "/LockBox_Mid_Purple.png"),
            ("lock and key stuff/tube_lock_default_mid_yellow.png", LockBox + "/LockBox_Mid_Yellow.png"),
            ("lock and key stuff/tube_lock_key_blue.png", Keys + "/Key_Blue.png"),
            ("lock and key stuff/tube_lock_key_green.png", Keys + "/Key_Green.png"),
            ("lock and key stuff/tube_lock_key_pink.png", Keys + "/Key_Pink.png"),
            ("lock and key stuff/tube_lock_key_purple.png", Keys + "/Key_Purple.png"),
            ("lock and key stuff/tube_lock_key_yellow.png", Keys + "/Key_Yellow.png"),
            // The red key has no named mid section yet, so no lock uses it (the plain mid has a red lamp).
            ("lock and key stuff/tube_lock_key_red.png", Keys + "/Key_Red.png"),
            // Kept for later, untouched.
            ("lock and key stuff/tube_lock_default_mid.png", Unused + "/LockBox_Mid_Plain.png"),
            ("lock and key stuff/tube_lock_default_mid_inside.png", Unused + "/LockBox_Mid_Inside.png"),
            ("lock and key stuff/tube_lock_default_side_inside.png", Unused + "/LockBox_Side_Inside.png"),
            ("lock and key stuff/tube_lock_default.png", Unused + "/LockBox_Single.png"),
            ("lock and key stuff/tube_lock_default_2bottle.png", Unused + "/LockBox_Double.png"),
            ("lock and key stuff/tube_lock_default_2bottle_inside.png", Unused + "/LockBox_Double_Inside.png"),
            ("lock and key stuff/tube_lock_default_blue.png", Unused + "/LockBox_Single_Blue.png"),
            ("lock and key stuff/tube_lock_default_green.png", Unused + "/LockBox_Single_Green.png"),
            ("lock and key stuff/tube_lock_default_pink.png", Unused + "/LockBox_Single_Pink.png"),
            ("lock and key stuff/tube_lock_default_purple.png", Unused + "/LockBox_Single_Purple.png"),
            ("lock and key stuff/tube_lock_default_yellow.png", Unused + "/LockBox_Single_Yellow.png"),
            // Tube parts found with the lock art: possibly a redrawn tube (docs/art-inventory.md, section 11).
            ("lock and key stuff/tube_full_default.png", Unused + "/Tube_Full_Redraw.png"),
            ("lock and key stuff/tube_mid.png", Unused + "/Tube_Middle_Redraw.png"),
            ("lock and key stuff/tube_up_back.png", Unused + "/Tube_RimBack_Redraw.png"),
            ("lock and key stuff/tube_up_front.png", Unused + "/Tube_RimFront_Redraw.png"),
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

        /// <summary>
        /// Delivered files that replace art already in place, under the same project names. The new
        /// pixels are copied over the old file, so its <c>.meta</c>, GUID and every reference stay; the
        /// delivered copy then goes to the trash (the old pixels stay in git).
        /// </summary>
        public static readonly (string From, string To)[] Replacements =
        {
            // Fifth delivery: fixed win picture, episode 2's mascot frame, corks and keys. The corks come on
            // the tube canvas (CanvasAnchored); the keys and the frames on their full canvas, all of a set
            // together, so each set is cut to one rectangle again.
            ("ingame_win.png", Root + "/UI/Win/Win_Picture.png"),
            // Stage 2: the space Home background, redrawn (the summer one came again unchanged).
            ("mainmenu_space.png", Root + "/Backgrounds/Home_Ep5_Space.png"),
            ("mainmenu_summer.png", Root + "/Backgrounds/Home_Ep2_Summer.png"),
            ("ingame_frame_summer_wcharacter.png", Root + "/Episodes/MascotFrame_Ep2_Summer.png"),
            ("tube_cork_stopper_blue.png", Corks + "/Cork_Blue.png"),
            ("tube_cork_stopper_brown.png", Corks + "/Cork_Brown.png"),
            ("tube_cork_stopper_green.png", Corks + "/Cork_Green.png"),
            ("tube_cork_stopper_lilac.png", Corks + "/Cork_Lilac.png"),
            ("tube_cork_stopper_orange.png", Corks + "/Cork_Orange.png"),
            ("tube_cork_stopper_pink.png", Corks + "/Cork_Pink.png"),
            ("tube_cork_stopper_purple.png", Corks + "/Cork_Purple.png"),
            ("tube_cork_stopper_red.png", Corks + "/Cork_Red.png"),
            ("tube_cork_stopper_turq.png", Corks + "/Cork_Turquoise.png"),
            ("tube_cork_stopper_yellow.png", Corks + "/Cork_Yellow.png"),
            ("tube_lock_key_blue.png", Keys + "/Key_Blue.png"),
            ("tube_lock_key_green.png", Keys + "/Key_Green.png"),
            ("tube_lock_key_pink.png", Keys + "/Key_Pink.png"),
            ("tube_lock_key_purple.png", Keys + "/Key_Purple.png"),
            ("tube_lock_key_red.png", Keys + "/Key_Red.png"),
            ("tube_lock_key_yellow.png", Keys + "/Key_Yellow.png"),
            // Fourth delivery: reworked monsters (one body size and centre) and the hidden ball at their size.
            ("balls reworked/monster_blue_01.png", Root + "/Balls/Ball_Blue_Open.png"),
            ("balls reworked/monster_blue_02.png", Root + "/Balls/Ball_Blue_HalfOpen.png"),
            ("balls reworked/monster_blue_03.png", Root + "/Balls/Ball_Blue_Closed.png"),
            ("balls reworked/monster_blue_04.png", Root + "/Balls/Ball_Blue_Laugh.png"),
            ("balls reworked/monster_brown_01.png", Root + "/Balls/Ball_Brown_Open.png"),
            ("balls reworked/monster_brown_02.png", Root + "/Balls/Ball_Brown_HalfOpen.png"),
            ("balls reworked/monster_brown_03.png", Root + "/Balls/Ball_Brown_Closed.png"),
            ("balls reworked/monster_brown_04.png", Root + "/Balls/Ball_Brown_Laugh.png"),
            ("balls reworked/monster_green_01.png", Root + "/Balls/Ball_Green_Open.png"),
            ("balls reworked/monster_green_02.png", Root + "/Balls/Ball_Green_HalfOpen.png"),
            ("balls reworked/monster_green_03.png", Root + "/Balls/Ball_Green_Closed.png"),
            ("balls reworked/monster_green_04.png", Root + "/Balls/Ball_Green_Laugh.png"),
            ("balls reworked/monster_lilac_01.png", Root + "/Balls/Ball_Lilac_Open.png"),
            ("balls reworked/monster_lilac_02.png", Root + "/Balls/Ball_Lilac_HalfOpen.png"),
            ("balls reworked/monster_lilac_03.png", Root + "/Balls/Ball_Lilac_Closed.png"),
            ("balls reworked/monster_lilac_04.png", Root + "/Balls/Ball_Lilac_Laugh.png"),
            ("balls reworked/monster_orange_01.png", Root + "/Balls/Ball_Orange_Open.png"),
            ("balls reworked/monster_orange_02.png", Root + "/Balls/Ball_Orange_HalfOpen.png"),
            ("balls reworked/monster_orange_03.png", Root + "/Balls/Ball_Orange_Closed.png"),
            ("balls reworked/monster_orange_04.png", Root + "/Balls/Ball_Orange_Laugh.png"),
            ("balls reworked/monster_pink_01.png", Root + "/Balls/Ball_Pink_Open.png"),
            ("balls reworked/monster_pink_02.png", Root + "/Balls/Ball_Pink_HalfOpen.png"),
            ("balls reworked/monster_pink_03.png", Root + "/Balls/Ball_Pink_Closed.png"),
            ("balls reworked/monster_pink_04.png", Root + "/Balls/Ball_Pink_Laugh.png"),
            ("balls reworked/monster_purple_01.png", Root + "/Balls/Ball_Purple_Open.png"),
            ("balls reworked/monster_purple_02.png", Root + "/Balls/Ball_Purple_HalfOpen.png"),
            ("balls reworked/monster_purple_03.png", Root + "/Balls/Ball_Purple_Closed.png"),
            ("balls reworked/monster_purple_04.png", Root + "/Balls/Ball_Purple_Laugh.png"),
            ("balls reworked/monster_red_01.png", Root + "/Balls/Ball_Red_Open.png"),
            ("balls reworked/monster_red_02.png", Root + "/Balls/Ball_Red_HalfOpen.png"),
            ("balls reworked/monster_red_03.png", Root + "/Balls/Ball_Red_Closed.png"),
            ("balls reworked/monster_red_04.png", Root + "/Balls/Ball_Red_Laugh.png"),
            ("balls reworked/monster_turq_01.png", Root + "/Balls/Ball_Turquoise_Open.png"),
            ("balls reworked/monster_turq_02.png", Root + "/Balls/Ball_Turquoise_HalfOpen.png"),
            ("balls reworked/monster_turq_03.png", Root + "/Balls/Ball_Turquoise_Closed.png"),
            ("balls reworked/monster_turq_04.png", Root + "/Balls/Ball_Turquoise_Laugh.png"),
            ("balls reworked/monster_yellow_01.png", Root + "/Balls/Ball_Yellow_Open.png"),
            ("balls reworked/monster_yellow_02.png", Root + "/Balls/Ball_Yellow_HalfOpen.png"),
            ("balls reworked/monster_yellow_03.png", Root + "/Balls/Ball_Yellow_Closed.png"),
            ("balls reworked/monster_yellow_04.png", Root + "/Balls/Ball_Yellow_Laugh.png"),
            ("balls reworked/monster_black_01.png", Root + "/Balls/Ball_Hidden.png"),
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
            Root + "/UI/Boosters",
            Root + "/UI/Win",
            Root + "/UI/Hud",
            Root + "/UI/Tutorials",
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
            Keys,
        };

        /// <summary>
        /// Folders of parts drawn in place on one canvas and laid side by side in the game: each file is
        /// cut to its own columns but to the rows of all of them, so every part has the same height and
        /// the parts line up top and bottom.
        /// </summary>
        public static readonly string[] TrimSharedRows =
        {
            LockBox,
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

        /// <summary>One elliptic stand per ball colour (<c>Stand_&lt;Colour&gt;</c>), drawn under a large tube.</summary>
        public const string Stands = Root + "/Tubes/Stands";

        /// <summary>
        /// Sets delivered in place on the tube canvas (320×640): the files of that size in each folder are
        /// cut to one rectangle, pivot on the tube point they belong to (<see cref="SpriteTrim.TrimAnchored"/>).
        /// Files of another size (already cut, or the plain cork) are left alone.
        /// </summary>
        public static readonly CanvasAnchor[] CanvasAnchored =
        {
            // The rim's top on the centre line, where the second delivery drew the rim (x 76–256, top at
            // row 77 from the top): the corks of the fifth delivery are drawn on that canvas.
            new CanvasAnchor(Corks, new Vector2Int(320, 640), new Vector2(166f, 640f - 77f)),
            // The tube's base on the centre line of the redrawn tube (x 77–247, base at row 623 from the
            // top), the canvas the stands are drawn on.
            new CanvasAnchor(Stands, new Vector2Int(320, 640), new Vector2(162f, 640f - 623f)),
        };

        public static readonly FolderImport[] Imports =
        {
            // Monsters and the hidden ball: the pivot is the round body's centre, the same in every file
            // of the reworked art (128, 139 from the top left of 256), so appendages do not shift a ball
            // in its slot.
            new FolderImport(Root + "/Balls", 256, TextureImporterFormat.ASTC_4x4,
                tightMesh: true, pivot: new Vector2(128f / 256f, 1f - 139f / 256f)),
            new FolderImport(Root + "/Tubes", 1024, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Corks, 256, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Stands, 256, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Root + "/Mechanics", 1024, TextureImporterFormat.ASTC_4x4),
            // Lock box parts stand on the box's base line, placed from their centres (CoverView).
            new FolderImport(LockBox, 1024, TextureImporterFormat.ASTC_4x4, pivot: new Vector2(0.5f, 0f)),
            new FolderImport(Keys, 256, TextureImporterFormat.ASTC_4x4, tightMesh: true, pivot: new Vector2(0.5f, 0.5f)),
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
            // Booster buttons are shown ≈ 200–330 px (the bar and the Buy popup).
            new FolderImport(Root + "/UI/Boosters", 512, TextureImporterFormat.ASTC_4x4),
            new FolderImport(Root + "/UI/Win", 1024, TextureImporterFormat.ASTC_6x6),
            // The Level HUD's boxes, 9-sliced to their content (Borders).
            new FolderImport(Root + "/UI/Hud", 1024, TextureImporterFormat.ASTC_6x6),
            // Tutorial pictures, rendered from the board's own art (Ball Sort ▸ Art ▸ Build Tutorial Pictures).
            new FolderImport(Root + "/UI/Tutorials", 512, TextureImporterFormat.ASTC_4x4),
            // Player pictures: opaque squares shown at most ≈300 px (masked to a circle in their frame).
            new FolderImport(Root + "/UI/Avatars", 512, TextureImporterFormat.ASTC_6x6, opaque: true),
            new FolderImport(Root + "/Episodes", 1024, TextureImporterFormat.ASTC_6x6),
            // Episode Unlocked pictures: opaque 1200×900 scenes, shown ≈ 700 px wide.
            new FolderImport(Root + "/Episodes/Unlocked", 1024, TextureImporterFormat.ASTC_6x6, opaque: true),
            new FolderImport(Root + "/Backgrounds", 2048, TextureImporterFormat.ASTC_6x6, opaque: true),
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
            // The hard button is drawn on the super hard one's canvas, pill and tag in the same place.
            ["Button_Hard"] = new Vector4(150f, 0f, 320f, 0f),
            // The coin or heart is part of the left end.
            ["Display_Coins"] = new Vector4(380f, 150f, 120f, 150f),
            ["Display_Lives"] = new Vector4(400f, 140f, 120f, 140f),
            // The towel's domed top and its hem stay; the straight stripes stretch for large tubes.
            ["Veil_Towel"] = new Vector4(0f, 110f, 0f, 70f),
            // Popup panel: the rounded frame and the inner panel's corners stay; the plain blue stretches
            // both ways (rows and columns 60–760 of the trimmed art are flat).
            ["Popup_Panel"] = new Vector4(100f, 110f, 100f, 100f),
            // Cream panel: the navy frame and the cream field's rounded corners stay; the flat cream stretches.
            ["Popup_Panel_Cream"] = new Vector4(130f, 130f, 130f, 130f),
            // Leaderboard panel: the tubes, the top frame and its orange corners (rows 0–470) and the bottom
            // frame stay; the box's plain middle (rows 470–800) stretches to fit the rows. It is never
            // stretched sideways (scaled uniformly to its width), so it has no side borders.
            ["Leaderboard_Panel"] = new Vector4(0f, 130f, 0f, 470f),
            // Open lock box's back wall: its rounded frame ends stay, the plain middle stretches across the box.
            ["LockBox_Inside"] = new Vector4(32f, 0f, 32f, 0f),
            // Level HUD boxes: the bevelled frame and the inner panel's rounded corners stay, the flat
            // middle stretches (the title box to its text, the booster box to the bar).
            ["Hud_LevelBox"] = new Vector4(64f, 64f, 64f, 64f),
            ["Hud_TierBox_Hard"] = new Vector4(64f, 64f, 64f, 64f),
            ["Hud_TierBox_SuperHard"] = new Vector4(64f, 64f, 64f, 64f),
            ["Hud_BoosterBox"] = new Vector4(130f, 110f, 130f, 110f),
        };
    }
}
