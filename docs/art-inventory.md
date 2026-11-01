# Art inventory (step 6)

What the artist delivered (82 PNGs in the first delivery, 2026-10-30; 20 in the second, 2026-10-31, section 7; 24 in
the third, the same day, section 8), where
each file went, what is wrong with it, and what art is still missing. Measured on the delivered files: size, alpha, content bounds, and whether the parts can be
stretched or tiled. The **Missing art** section is the list for the artist. The untouched originals are in git (commit
"Art: first delivery from the artist…"); the files in `Assets/Art/Sprites/` are trimmed (section 5).

Episodes, by the names in the files: 1 Lab, 2 Summer, 3 Ice, 4 Lava, 5 Space.

## 1. Files → new names → slots

Everything moves to `Assets/Art/Sprites/` through `AssetDatabase.MoveAsset` (the `.meta` files and GUIDs survive).
Names: `Kind_Variant[_State].png`, PascalCase, no spaces, hyphens or brackets.

### Board (Level scene, world space)
| Incoming | New path | Slot |
|---|---|---|
| `balls/monster_<colour>_01.png` | `Balls/Ball_<Colour>_Open.png` | `ColorPalette` entry: eyes open (normal look) |
| `balls/monster_<colour>_02.png` | `Balls/Ball_<Colour>_HalfOpen.png` | `ColorPalette` entry: blink frame (stored, used later) |
| `balls/monster_<colour>_03.png` | `Balls/Ball_<Colour>_Closed.png` | `ColorPalette` entry: blink frame (stored, used later) |
| `balls/monster_<colour>_04.png` | `Balls/Ball_<Colour>_Laugh.png` | `ColorPalette` entry: balls in a completed tube |
| `balls/monster_hidden.png` | `Balls/Ball_Hidden.png` | `ColorPalette._hiddenSprite` (already shows the "?") |
| `tube/tube_up.png` | `Tubes/Tube_Top.png` → `Art/Unused/` | Was the top part; replaced by the rim's two halves (section 7) |
| `tube/tube_mid.png` | `Tubes/Tube_Middle.png` | `TubeVisualConfig` middle (stretched to the tube's height) |
| `tube/tube_down.png` | `Tubes/Tube_Bottom.png` | `TubeVisualConfig` bottom (fixed size) |
| `tube/tube_full_default.png` | `Tubes/Tube_Full.png` | Invalid-tap flash silhouette (9-sliced) |
| `tube/tube_down_ice(clay).png` | `Mechanics/Clay_Ice.png` | `TubeVisualConfig._claySprite` |
| `tube/tube_towel(cover-veil).png` | `Mechanics/Veil_Towel.png` | `TubeVisualConfig._veilSprite` |

Colour ids (the placeholder tints decide the match; id 8 was lime, the closest delivered colour left is lilac):

| Id | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Art | Red | Blue | Yellow | Green | Purple | Orange | Turquoise (`turq`) | Pink | Lilac | Brown | placeholder | placeholder |

### Level HUD and Home
| Incoming | New path | Slot |
|---|---|---|
| `button/button_default.png` | `UI/Buttons/Button_Green.png` | Continue (Win), Home Level button |
| `button/button_red.png` | `UI/Buttons/Button_Red.png` | Give up, Are You Sure Confirm, Settings' Restart and Home |
| `button/button_yellow.png` | `UI/Buttons/Button_Yellow.png` | Play on, shop item buttons |
| `button/button_x.png` | `UI/Buttons/Button_Close.png` | Close (✕) on every popup |
| `button/button_settings.png` | `UI/Buttons/Button_Settings.png` | Settings on Home and in the Level scene |
| `button/button_coin.png` | `UI/Displays/Display_Coins.png` | Home coins display (count on the green part) |
| `button/button_lives.png` | `UI/Displays/Display_Lives.png` | Home lives display (count on the heart, timer on the green part) |
| `menus/downmenu_default_wbuttons.png` | `UI/Home/BottomBar_Home.png` | Bottom bar, Home selected |
| `menus/downmenu_shop_wbuttons.png` | `UI/Home/BottomBar_Shop.png` | Bottom bar, Shop selected |
| `menus/downmenu_leader_wbuttons.png` | `UI/Home/BottomBar_Leaderboard.png` | Bottom bar, Leaderboard selected |
| `shop/shop_title.png` | `UI/Shop/Shop_Header.png` | Shop page header ("Shop" text on top) |
| `shop/shop_menu.png` | `UI/Shop/Shop_Panel.png` | Shop page panel (6 empty item frames) |
| `shop/shop_bundle.png` | `UI/Shop/Shop_Bundle.png` | Shop bundle card (static, price text on its yellow button) |

### Episodes
| Incoming | New path | Slot |
|---|---|---|
| `frames/ingame_frame_ep<N>_wcharacter.png` | `Episodes/MascotFrame_Ep<N>_<Theme>.png` | `EpisodeConfig` mascot frame: Level scene top left, Episode Unlocked overlay |
| `illustrations/ingame-background_<theme>_ep<N>.png` | `Backgrounds/Level_Ep<N>_<Theme>.png` | `EpisodeConfig` Level background, episodes 1–5 |
| `illustrations/mainmenu-background_summer_ep2.png` | `Backgrounds/Home_Ep2_Summer.png` | `EpisodeConfig` Home background, episode 2 |
| `illustrations/mainmenu-background_space_ep5.png` | `Backgrounds/Home_Ep5_Space.png` | `EpisodeConfig` Home background, episode 5 |

### Not used (no slot)
`frames/mainmenu_frame.png` (purple ring around a filled disc) is the avatar frame, as
`UI/AvatarFrames/Avatar_Frame_Empty.png`: Home's avatar and every frame of the avatar menu (the picture is masked
onto its disc). Its green version arrived later (section 7).

`frames/ingame_frame_{lab,summer,ice,lava,space}.png` (empty episode rings), `menus/downmenu_{default,leader,shop}.png` (bars without icons), `tube/tube_full_ice.png` (tube + ice in one
image; we draw them separately). They are in `Assets/Art/Unused/` (not referenced, so not built) until the artist says
what they are for.

Home backgrounds: episodes 1, 3 and 4 use episode 2's (`Home_Ep2_Summer`) until their own arrive.

## 2. Measurements

| Group | Canvas | Content | Notes |
|---|---|---|---|
| Balls (41) | 256×256 | body ≈175 px round, centred at (130, 148) from the top left in every colour | Appendages (antennae, horns) reach up to 6–61 px from the top; arms make them ≈199 px wide. The four expressions of a colour have identical bounds. |
| Hidden ball | 256×256 | ≈195×200, centre (128, 144) | Bigger than the monsters (see problems). |
| Tube parts (4) | 320×640, shared | full tube x 74–253, y 18–623 | Body (walls) x 93–237 = 145 px, rim 180 px, inside 127 px, walls 8–10 px. Top + middle + bottom composite exactly into `tube_full_default`. Inside is fully transparent. |
| Clay (ice) | 320×640, shared with the tube | 307×178 at x 9–315, y 459–636 | Drawn for the bottom of a capacity-4 tube; reaches 13 px below the tube's base. |
| Veil (towel) | 320×640, shared with the tube | 238×474 at x 49–286, y 17–490 | Covers the top 78 % of a capacity-4 tube; the tube's bottom shows under the hem. |
| Buttons (5) | 1000×1000 | pills ≈624×330, round buttons ≈644×644 | 70–80 % of each file is empty. |
| Coin / lives displays | 1000×1000 | 844×380 / 860×348 | Coin or heart drawn into the left end of a green pill. |
| Bottom bars (3) | 1000×1000 | 928×244 (shared rect) | Sections at x ≈ 44–414 / 414–586 / 586–956: 41 % / 19 % / 41 %. |
| Shop (3) | 1000×1000 | header 864×440, panel 844×900, bundle 900×256 | |
| Mascot frames (5) | 1000×1000 | 856×844 (shared rect) | Ring ≈780 px; ep1's flask and ep4's hand stick out of the ring. |
| Backgrounds (7) | 1440×2560 (9:16) | full | RGBA but fully opaque; 3.8–4.5 MB each as PNG. |

Stretch and tile tests:
- **Tube middle** stretches cleanly: neighbouring rows differ by less than 2/255 on average, so a 4.6× stretch (capacity
  16) only lengthens the wall; tiling also works (top and bottom rows match within 8/255). Stretching is chosen: no seams.
- **Red and yellow buttons** have a 150–170 px band of identical columns in the middle: 9-slice stretches them to any
  width. **Green** has a slight left-to-right gradient (43 px identical band); stretching keeps the gradient smooth.
- **Coin and lives displays** stretch in the green part (left border must include the coin or heart).
- **Veil** has vertical stripes, so its middle stretches vertically for large tubes.
- **Bottom bar, shop header, panel, bundle, frames, round buttons**: fixed art, scaled uniformly, not sliced.

## 3. Problems found

**Size and padding**
1. **Every UI file sits on a 1000×1000 canvas with 20–80 % empty margin** (buttons, displays, bars, shop, frames). As
   delivered they would waste texture memory, make every Image's rect and tap area far bigger than what is drawn, and
   9-slice borders would have to start inside the empty margin. They need trimming (we can do it; future exports
   should be trimmed).
2. **The tube parts, ice and veil share one 320×640 canvas** with the parts in place. That is useful (they line up),
   but each needs trimming, and **the tube is 5 px right of the canvas centre** (body centre x 165, canvas 160): all
   tube parts must be cut with the same column range, and the ice and veil positioned from the same canvas.
3. **The tube parts overlap**: top and middle share 5 rows (113–117), middle and bottom 13 rows (539–551), with
   identical pixels in both. Drawn as delivered, the semi-transparent wall pixels at the joins would be drawn twice
   (visible darker bands, worse when the middle stretches). Fix: cut non-overlapping bands from the full tube:
   top rows 18–117, middle 118–538, bottom 539–623.
4. **Files in a swap set have different bounds**: the three bottom bars (the Shop version starts 8 px further left)
   and the five frames (rings differ slightly in size and place). Each set must be cut to one shared rectangle, or the
   art jumps when it swaps.

**Board**
5. **The ice (clay) is 307 px wide, 2.1× the tube's body**: at our tube spacing it reaches over the neighbouring
   tubes' bottoms. It is opaque, so **drawn in front it hides the bottom ball** almost completely (only the horns
   show); drawn behind, the ball stays readable but sits in front of the ice. A narrower version, or one with a
   see-through middle, would fix both.
6. **The hidden ball is ≈12 % bigger than the monsters** (≈195×200 vs ≈175 px body): a revealed ball visibly shrinks.
   We can scale it down in config; the artist may prefer to redraw it at the monsters' size.
7. **Monster appendages reach into the slot above** (horns, antennae up to the canvas top) and arms are wider than the
   tube's inside (≈199 vs 127 px at art scale). Fine with no glass front (the tube is behind the balls), but upper
   balls must draw over lower ones so horns tuck behind the ball above.
8. **The tube's inside is fully transparent and its walls are thin** (8–10 px at art scale, ≈3–5 px on a phone).
   Readability on the dark Lab and Space backgrounds and on the bright Summer sand needs checking in screenshots; a
   faint glass fill behind the balls may be needed.
9. **Purple (id 4) and lilac (id 8) are close in hue**; both appear together in the ten-colour levels (49, 59, 64, …).
   Their silhouettes differ (one horn vs a tentacled blob), which helps, but a clearly different hue for id 8 (the
   placeholder was lime) would be safer, also for colour-blind players.
10. **Episode 5's scientist is upside down** in his frame. Probably the zero-gravity joke; please confirm.

**UI**
11. **The bottom bar's order is Shop · Home · Leaderboard** (basket left, door centre, test-tube chart right), the
    reverse of the old Leaderboard · Home · Shop (the game now follows the art). Its **sections are 41 / 19 / 41 %**, not thirds.
12. **The coin and heart are drawn into the display pills**, so there is no standalone coin for the Win reward
    (`+20 ●`) and the Play on price (`900 ●`), and no standalone heart for More Lives. *Solved by the third delivery's
    icons (section 8).*
13. **The shop does not fit one screen**: header 440 + panel 900 + bundle 256 px at art scale is ≈1,600 px tall at
    1080 wide, with the top bar and the bottom bar still to fit in 1920. The panel's six frames are empty (no item
    art), and the header and bundle have no text (we add it).
14. **Inconsistent file names**: hyphens and underscores mixed (`ingame-background_lab_ep1`), brackets in
    `tube_down_ice(clay)` / `tube_towel(cover-veil)`, the abbreviation `turq`, theme and episode in different orders.
15. **Backgrounds are RGBA with no transparency**: imported without alpha. 9:16 art cropped to fill: on 19.5:9 phones
    ≈130 px goes from each side, on 3:4 tablets ≈320 px from top and bottom. Nothing important sits in those margins
    in the delivered art.

Fine as delivered: every sprite PNG has a transparent background (all four corners fully transparent), every dimension is a multiple of 4, the 41 ball files are consistent with each other, the four expressions of each
colour line up exactly, and the tube parts recompose the full tube exactly.

## 4. Missing art (for the artist)

Every slot below has a placeholder today, so each drops in without code changes unless marked *(code)*. Please export
trimmed to the content (no empty canvas), PNG with transparency, and keep swap sets (states of one element) on one
shared canvas size.

**Board (Level scene)**
- **Lock door**: covers 1–3 adjacent tubes of the same height, so it must stretch in both directions (9-sliceable:
  plain middle, details in the corners/edges). Neutral colour (tinted per lock) or 4 coloured versions.
- **Lock icon** on the door, and **key mark** on a ball carrying a key (both tinted per lock, so light/white art).
- **Veil badge** (optional): the circle in the veil's colour we draw on the towel; ours stays meanwhile.
- **Ball colours 11 and 12** (ids 10–11; optional: the levels use 10 colours).
- **Clay ice at the tube's width**: the delivered ice is drawn 2.1× the tube's body, so the game scales it to 1.1× the
  tube's drawn width (rim, `_clayScale`), and draws it over the balls in the clay tube (they sit in the ice). Please
  redraw it about the rim's width (180 px at the tube canvas's scale), keeping its height, ideally with a see-through
  middle so the bottom ball stays readable (problem 5). Its melt on completion needs art or VFX too.
- **Monster body sizes**: every PNG is 256×256, but the round bodies (without arms, feet, horns or antennae) differ
  between colours, roughly 170–190 px wide (lilac smallest, orange and turquoise largest), so some monsters look bigger
  in the tubes. Please draw every body at one size (e.g. 175 px) on the same centre; the game does not correct it.
- **Hidden ball size**: please redraw it at the monsters' size (≈175 px body on the 256 canvas, same centre); the game
  scales it down meanwhile (`ColorPalette._hiddenSpriteSize`) (problem 6). Blink frames optional.
- **Glass front layer for the tubes**: a translucent glass/highlight layer drawn over the balls, on the same canvas and
  in the same three parts (top, middle, bottom) as the tube, so it stretches the same way (problem 8).
- **Confirm episode 5's scientist upside down** in his frame: intended (zero gravity) or a mistake? (problem 10)
- **VFX** (step 6 animations): tube-complete sparkle, win celebration, invalid-move effect.

**Level HUD**
- **Booster icons**: Undo, Shuffle, Add tube, and a **count badge** (the buttons show text today). The same icons
  (or bigger pictures) fill the Buy popup's picture: `BoosterBar._undoPicture` / `_shufflePicture` / `_addTubePicture`,
  generated placeholders `Placeholder/UI/Booster_*.png` today. The buy badge at 0 is `Icon_Plus` (section 8).
- **Booster bar background** (bottom strip of the Level scene).
- **Level title plate** behind "Level N" (optional).
- **Stuck banner** ("No more useful moves") background.
- **Win celebration** title ("Well done!").

**Popups (Home and Level)**
- ~~Popup panel, toggles, standalone coin and heart, green plus~~: arrived in the third delivery (section 8).
- **Popup title ribbon** (optional).
- **Episode Unlocked** banner/overlay art (it uses the popup panel today).
- **Toast** background.

**Home**
- **Home backgrounds for episodes 1, 3 and 4** (Lab, Ice, Lava).
- **Hard tier button**: only the super hard button arrived (section 8), so hard levels use it too, with "x3" on its
  tag. A hard version (e.g. orange or yellow, same canvas and tag) drops into `LevelTierLook._hardSprite`.
- ~~Leaderboard page, coin bundle art~~: arrived in the third delivery (section 8). The three bundle cards below the
  shop panel still have no title art (we write it in text).
- **Avatar frame opening**: the frame's middle is a filled disc, so the picture is masked to a circle over it (74 % of
  the frame's width, measured). A frame with a transparent opening, or a mask shape, would fit exactly.

**Packaging**
- **App icon** and **splash screen**.

## 5. How the art is prepared (Ball Sort ▸ Art ▸ Prepare Sprites)

One menu item makes delivered art ready; it can be run any time, files already trimmed are left as they are.
- **Move deliveries** (`SpriteRules.Deliveries`): each delivered file still in `Assets/Art/Incoming/` is moved to its
  place under its project name through `AssetDatabase.MoveAsset` (`.meta` and GUID kept), folders created as needed. A
  destination that already exists is not overwritten (reported instead). Files kept for later go to `Assets/Art/Unused/`,
  which no rule touches.
- **Trim each** (`UI/Buttons`, `UI/Displays`, `UI/Shop`, `UI/Avatar`, `UI/Icons`, `UI/Popups`, `UI/Leaderboard`): every
  file cut to its drawn pixels. The avatar
  pictures (`UI/Avatars`) are opaque squares and are not trimmed; they import at most 512 px, without alpha.
- **Trim shared** (`UI/Home`, `UI/AvatarFrames`, `UI/Settings`, `Episodes`): a folder is one swap set, cut to one rectangle holding
  all its files' content, so the bottom bar's versions, the plain and selected avatar frames and the five mascot frames
  line up.
- **Tube** (`Tubes/*`, `Tubes/Corks/*`, `Mechanics/Clay_Ice`, `Mechanics/Veil_Towel`, while they still share the
  delivery canvas): the parts are cut into bands that do not overlap (rim 100, middle 420, bottom 88 px, all 180 px
  wide), and every piece gets a pivot on the walls' centre line (rim halves, veil and corks at the rim's top, middle at
  its bottom, bottom part, outline and clay at the base), which is how `TubeView` and `CoverView` place them. The rim's
  two halves are cut to one rectangle, and so are the corks. When only the rim and corks arrive on the tube canvas (the
  second delivery), only they are cut, anchored to the rim drawn in them.
- Sizes are padded to multiples of 4 (block compression), then the import settings are applied per folder
  (`SpriteRules`): Sprite, Single, 100 PPU, no mipmaps, clamp, bilinear; max size 256 (balls), 1024 (tube, mechanics,
  UI, frames), 2048 (backgrounds, no alpha); Android and iOS ASTC 4×4 (balls, tube, mechanics, buttons, displays) or
  6×6 (bar, shop, frames, backgrounds). Ball pivots sit on the body's centre; 9-slice borders for the pills, displays
  and veil are in `SpriteRules.Borders`.

**New delivery:** a replacement: copy the new PNG over the old file of the same name in `Assets/Art/Sprites/` (the
`.meta` and every reference survive), run Prepare Sprites, check the screens. New files: drop them in
`Assets/Art/Incoming/`, add a line per file to `SpriteRules.Deliveries` (and a folder rule or borders if new), run
Prepare Sprites.

## 6. Pending for animations (docs/animation-spec.md, 2.2–2.6)

Slots and placeholders already in place; final art drops in without code changes.

**Sprites**
| Needed | Slot | Placeholder now | Notes for the artist |
|---|---|---|---|
| Glow around a completed tube | `TubeVisualConfig._glowSprite` (9-sliced, pivot bottom centre, tinted in the tube's colour; `_glowPadding` past the tube on each side) | `Placeholder/Tube_Glow` (generated soft rounded outline) | White, soft halo following the tube's outline; drawn behind the tube, pulses in alpha. |
| Shock face, one per colour (10) | `ColorPalette._entries[n]._shock` (`BallExpression.Shock`) | the colour's `Closed` face | Same canvas and centre as the other four faces; shown for 0.7 s on an invalid tap. |
| Sparkle | `Prefabs/VFX/Vfx_TubeSparkles` particle texture sheet (Sprites mode) | `Placeholder/Vfx_Sparkle` (generated star) | White; tinted per particle (white, tube colour, gold). |
| Shockwave ring | `Prefabs/VFX/Vfx_TubeRing` particle texture sheet | `Placeholder/Vfx_Ring` (generated) | White thin ring, soft edge; grows to ≈2.2 ball diameters. |
| Confetti piece | `Prefabs/VFX/Vfx_Confetti` particle texture sheet | `Placeholder/Ball_Circle` (squashed) | White or pre-coloured; colours are set on the prefab. |
| Droplet | `Prefabs/VFX/Vfx_Droplets` particle texture sheet | `Placeholder/Ball_Circle` | White; tinted with the ball's colour. |

The corks arrived in the second delivery (section 7).

Effects on existing sprites (no new art): balls squash and stretch, the glow fades and pulses, the
flight trail is drawn in the ball's colour (no texture).

**Sounds**: none missing; all 22 delivered files are in `Assets/Audio/SFX/` with a slot each in `SfxConfig`. For audio:
the mp3s carry about 50 ms of encoder padding before the first sound (skipped in the game with `_startOffset`);
trimmed WAV exports would remove the need.

## 7. Second delivery (2026-10-31)

20 PNGs, moved from `Assets/Art/Incoming/` with `AssetDatabase.MoveAsset` (`.meta` files and GUIDs kept) and prepared by
**Prepare Sprites** (the rim and corks by `TubeArtTrim`, the panel and pen by Trim each).

| Incoming | New path | Slot |
|---|---|---|
| `tube top/tube_up_back.png` | `Tubes/Tube_RimBack.png` | `TubeVisualConfig._rimBackSprite`: the rim's back lip, behind the balls (TubeBack) |
| `tube top/tube_up_front.png` | `Tubes/Tube_RimFront.png` | `TubeVisualConfig._rimFrontSprite`: the rim and neck glass, over the balls and the cork (TubeFront 3) |
| `corks/tube_cork_stopper_<colour>.png` | `Tubes/Corks/Cork_<Colour>.png` | `ColorPalette._entries[n]._cork`, ids 0–9 (`turq` → `Turquoise`); drawn untinted |
| `corks/tube_cork_stopper.png` | `Tubes/Corks/Cork_Plain.png` | `TubeVisualConfig._capSprite`: the plain cork, tinted in the colour, for ids 10–11 |
| `avatar/avatar_01…05.png` | `UI/Avatars/Avatar_01…05.png` | `AvatarConfig._pictures` (avatar ids 1–5) |
| `avatar/avatar_menu.png` | `UI/Avatar/AvatarMenu_Panel.png` | `AvatarMenuPopup/Panel` |
| `button/button_pen.png` | `UI/Buttons/Button_Pen.png` | `AvatarMenuPopup/Panel/Header/PenButton` |
| `mainmenu_frame_selected.png` (later the same day) | `UI/AvatarFrames/Avatar_Frame_Selected.png` | `AvatarConfig._selectedFrame`: the green frame of the picked avatar in the avatar menu, swapped for the plain frame (`AvatarConfig._frame`) |

**Measurements**

| Group | Canvas | Content | Notes |
|---|---|---|---|
| Rim halves (2) | 320×640, the tube canvas | back x 80–255, y 77–102; front x 76–256, y 101–177 (from the top) | Cut together to 180×100, pivot at the rim's top. The rim sits 59 px lower on the canvas than the first delivery's top part; placement is by pivot, so it does not matter. The front's middle is translucent glass (alpha 0–90), its walls opaque. |
| Corks (11) | 320×640, the tube canvas | 139×150 at x 96–235, y 5–155, identical bounds in every colour | Cut to 140×152 with the rim's pivot, so each sits where it was drawn: 72 px above the rim's top, 78 px into the neck. |
| Avatars (5) | 1000×1000 | full, opaque | Imported at 512 (shown ≈ 120–150 px). |
| Avatar menu panel | 1000×1000 | 577×947 → 580×948 | Top glass bar (x 42–537, y 96–231 of the trimmed art) with a lighter name field (x 214–487, y 138–194); big glass (x 52–529, y 296–884) holds the 3 × 3 grid. |
| Pen button | 1000×1000 | 634×632 → 636×632 | Round, like the close and settings buttons. |
| Selected frame | 1000×1000 | 770×770 at the plain frame's place (the plain one has 20 more rows of lip below) | The plain frame's untouched original was put back on its canvas and both cut to one rectangle, 772×792, the plain frame's size before, so nothing else moved. |

**Problems found**
1. **The cork covers the top ball's head**: drawn where the artist placed it, it reaches 78 px (≈ 0.6 ball) into the
   neck, over the top ball of a full tube (the cork draws over the balls, under the rim front, as asked). The eyes of
   the top monster stay partly visible (screenshot of a completed tube). A cork sitting higher, or a config offset, would
   fix it; left as delivered for now.
2. **The avatars are 1000×1000 opaque squares** with the subject anywhere in the picture; masked to a circle, some crop
   tightly (the scientist's raised hand). Square art framed for a circle, around 256 px, would be lighter and safer.
3. ~~No selection highlight~~: the green selected frame arrived the same day.
4. **The bottom bar's ends differ per version**: the raised Shop and Leaderboard tabs round in 53 px (5.7 %) from the
   art's edge, the Home version 12 px, so a full-width bar is drawn 112 % of the screen's width and crops a little of
   every version's ends (docs/screens.md, Bottom nav). Bars drawn edge to edge (square ends) would avoid the crop.

## 8. Third delivery (2026-10-31)

24 PNGs, moved out of `Assets/Art/Incoming/` by **Prepare Sprites** itself (`SpriteRules.Deliveries`, section 5) and
trimmed each (the two toggle knobs as one swap set). `Assets/Art/Incoming/` was empty afterwards and was removed.

| Incoming | New path | Slot |
|---|---|---|
| `button/button_superhard.png` | `UI/Buttons/Button_SuperHard.png` | `LevelTierLook._superHardSprite` **and** `_hardSprite` (no hard version yet), on `LevelButton/TierArt`; "x5" / "x3" on its tag |
| `button/button_settings_off.png` / `_on.png` | `UI/Settings/Toggle_Off.png` / `Toggle_On.png` | `SettingSwitch._offSprite` / `_onSprite`: the knob of each setting's switch |
| `icons/icon_coin.png` | `UI/Icons/Icon_Coin.png` | Shop bundle 1 and card 1; the Buy popup's price button; inline in the Win reward and the Play on price (TMP sprite asset `Art/Atlases/Icon_Coin_TMP.asset`, `<sprite name="Icon_Coin">`) |
| `icons/icon_coin2…6.png` | `UI/Icons/Icon_CoinPouch`, `Icon_CoinChest`, `Icon_CoinChestFull`, `Icon_CoinTank`, `Icon_CoinTankFull` | Shop bundles 2–6 (`Panel/Bundle<N>/Picture`); cards 2 and 3 use `Icon_CoinChest` and `Icon_CoinTank` |
| `icons/icon_heart.png` | `UI/Icons/Icon_Heart.png` | More Lives (count on it), Refill hearts popup (`HomeScreen._heartPicture`) |
| `icons/icon_heart_broken.png` | `UI/Icons/Icon_HeartBroken.png` | Are You Sure, above "You will lose a life." (`AreYouSurePopup._lifeCostIcon`) |
| `icons/icon_noads.png` | `UI/Icons/Icon_NoAds.png` | Left of each shop card's name |
| `icons/icon_plus.png` | `UI/Icons/Icon_Plus.png` | Buy badge on the lives and coins pills (Home) and on a booster at 0 |
| `leaderboard/leaderboard_title.png` | `UI/Leaderboard/Leaderboard_Header.png` | Leaderboard page header ("Leaderboard" written on the plate) |
| `leaderboard/leaderboard_menu.png` | `UI/Leaderboard/Leaderboard_Panel.png` | Leaderboard panel, 9-sliced vertically; top 3 avatars on the tubes' discs |
| `leaderboard/leaderboard_player_default.png` / `_me.png` | `UI/Leaderboard/Leaderboard_Row.png` / `Leaderboard_RowMe.png` | `LeaderboardPage._otherRowSprite` / `_playerRowSprite` |
| `pop-up menus/ingame3_menu.png` | `UI/Popups/Popup_Panel.png` | Panel of Win, Are You Sure, More Lives, Tutorial, Buy, Episode Unlocked (9-sliced) |
| `pop-up menus/settings_menu.png` | `UI/Popups/Settings_Panel.png` | Settings popup panel (both scenes), fixed art |
| `icons/icon_clock.png` | `Art/Unused/Icon_Clock.png` | For later |
| `pop-up menus/ingame_menu.png`, `ingame2_menu.png`, `ingame4_menu.png`, `ingame5_menu.png` | `Art/Unused/Popup_Panel_Header_Cream.png`, `Popup_Panel_Header.png`, `Popup_Panel_Wide_Cream.png`, `Popup_Panel_Cream.png` | For later (headed and cream-filled panel variants) |

**Measurements** (trimmed sizes; every file came on a 1000×1000 canvas)

| File | Trimmed | Notes |
|---|---|---|
| Button_SuperHard | 884×480 | Pill x 2–827, y 50–478 (from the top); tag at the top right, tilted ≈ 25° clockwise, centre (735, 112). 9-slice borders 150 / 0 / 320 / 0: only the plain middle (columns 150–564) stretches, so the pill takes the green button's width; drawn at 230 / 428 scale so the pill matches the button's height, the tag reaching past its top right. |
| Toggle_Off / Toggle_On | 772×792 (shared) | Round knobs; drawn 94×96 in the art's tracks. |
| Icons | 568–616 × 552–824 | Imported at most 512. The coin is 584×560, so it imports at 512×491. |
| Popup_Panel | 812×848 | Rows and columns 60–760 are flat: borders 100 / 110 / 100 / 100. |
| Settings_Panel | 644×900 | Three bars at rows 160–265, 350–455, 540–645 (x 88–395, icon to x 175), tracks x 420–556; empty strip at rows 710–810 (Restart and Home in the Level scene); header lip rows 0–88 (the title). |
| Leaderboard_Header | 860×480 | No text in the art: we write "Leaderboard" on the plate. |
| Leaderboard_Panel | 640×940 | Tubes and the top frame in rows 0–470, plain box rows 470–800: borders 0 / 130 / 0 / 470, drawn at 920 / 640. Disc centres (321, 61) middle, (137, 100) left, (511, 136) right, ≈ 118 px wide. |
| Leaderboard_Row / RowMe | 756×184 / 780×240 | Rank circle centre ≈ x 80 / 90, avatar ring ≈ x 220 / 235 (opening ≈ 112 px). The player's row has slime drips below the bar (≈ 50 px). |

**Problems found**
1. **No hard tier button**: hard levels use the super hard art with "x3" (section 4).
2. **The popup panel, leaderboard header and shop cards have no text**: titles are TMP text over them, as before.
3. **The bundle icons differ in shape** (the coin is round and wide, the tanks tall), so at one frame size the coin
   reads bigger than the tanks. Fine for now; a common canvas per set would make them line up.
4. **The two leaderboard rows differ in shape** (the player's drips): the row's background hangs from the row's top
   and takes its height from the art (`LeaderboardRow`), so the drips hang into the gap below; the next row covers
   their tips.

## 9. Fonts (2026-10-31)

One family, **Baloo 2** (SIL Open Font License, free to ship; docs/design/FONTS.md), two static cuts from the design
team (`Reference/Fonts/`), copied to `Assets/Art/Fonts/`:

| File | TMP asset | Used for |
|---|---|---|
| `Baloo2-Bold.ttf` | `Baloo2-Bold SDF` (the TMP default) | every label |
| `Baloo2-ExtraBold.ttf` | `Baloo2-ExtraBold SDF` | nothing (kept, unreferenced) |

**Deviation from FONTS.md:** FONTS.md makes ExtraBold the default and Bold a few secondary lines. In the game,
ExtraBold under an outline read too heavy (the holes of e, a and o closed up), so every label uses Bold (decided
2026-11-01). Unreferenced assets are left out of builds, and the `.ttf` files are only the source for the atlases: a
static font asset doesn't reference them in a build.

**Atlases.** Static, so text never writes to them and they never change in git: SDFAA, 1024 × 1024, 208 glyphs each,
sampled at 64 pt with padding 15 (0.23 em: an outline and a drop shadow can reach only as far as the padding, and TMP
silently shrinks any effect that would go further). Characters:
- ASCII 0x20–0x7E (letters, digits, `TL`, punctuation);
- Latin-1 0xA0–0xFF (non-breaking space, `·`, `×`, `Ç ç Ö ö Ü ü`, `â î û` and Western European accents);
- the other Turkish letters `Ğ ğ İ ı Ş ş`;
- `– — ‘ ’ “ ” … • € ₺`, and the zero-width space the input field writes.

Baloo 2 has no `≡ ● ♥ ✕`. They appear only in leftover labels that are switched off (the old "×" close and "≡"
settings labels, replaced by art). The coin in text is the `Icon_Coin` sprite (`Icon_Coin_TMP`), not a glyph. The
character list is saved in each asset's creation settings: to add a character, open **Window ▸ TextMeshPro ▸ Font Asset
Creator** on the asset, append it to the custom characters and regenerate at the same size and padding. There is
little room left in 1024²: Latin Extended-A alone would need a 2048 atlas.

**Look (FONTS.md): material presets.** White fill, a solid outline and a solid shadow straight down in the same colour,
as TMP material presets next to the font (`Baloo2-Bold SDF <Name>.mat`, all on the mobile SDF shader, sharing the
atlas). TMP centres the outline on the glyph's (dilated) edge, so half of it eats into the fill and half shows outside;
face dilate thickens the fill. Keep the dilate small: dilate = outline width (the first try) fattened the letters until
the holes of e, a and o filled up. The shadow is the outlined shape (underlay dilate ≈ dilate + outline), offset down,
softness 0.

| Preset | Colour | Dilate / outline / underlay dilate | Shadow offset | Used on |
|---|---|---|---|---|
| Title | `#24106E` deep purple | 0.06 / 0.22 / 0.30 | −0.40 | popup titles, Win reward, Level HUD title, Shop / Leaderboard headers, stuck banner, celebration |
| Body | `#24106E` | 0.03 / 0.16 / 0.20 | −0.25 | popup body lines and hints, tutorial text, Settings rows, booster labels, leaderboard rows, shop card text |
| Green | `#1F5C14` | 0.06 / 0.22 / 0.30 | −0.40 | green buttons (Continue, regular Level button), green pills (lives timer, coins), booster badges |
| Red | `#7A0C22` | 0.06 / 0.22 / 0.30 | −0.40 | red buttons (Quit / Restart, Settings Restart / Home, Give up, hard Level button), hearts' counts, the x3/x5 tag |
| Yellow | `#8A4A0A` brown | 0.06 / 0.22 / 0.30 | −0.40 | yellow buttons (Buy price, Play on) |

Units are TMP's (fractions of the 15 px padding at 64 pt; the shadow offset of the gradient scale, 16 px), so −0.40 ≈
0.1 em down.

The colours are the art's own darker edges (the buttons' lips and outlines). Labels that were dark on light (the Shop
and Leaderboard plate titles, Play on) are white now; the hint lines keep their muted blue-grey (`#9EA8C7`) under a Body
outline, a step below the main text. The Level button's label swaps Green / Red with its art (`LevelTierLook`). The
celebration ("Well done!") also has FONTS.md's gradient, `#FFF6C2` → `#FF9500` top to bottom (TMP vertex gradient;
two stops, so the middle yellow is approximated). Toast and the avatar menu's name field stay plain (dark backgrounds
of their own).

**Typed names: system font fallback.** A name can hold any character. `SystemFontFallback` (on `AppRoot`) builds TMP
**dynamic** font assets at launch from the device's own fonts (a serialized family list covering Android, iOS and the
Editor: Roboto, Noto Sans CJK JP, Noto Sans, Helvetica Neue, PingFang SC, Apple SD Gothic Neo, Segoe UI, Microsoft
YaHei, Malgun Gothic; families the device lacks are skipped). It puts them on TMP's global fallback list for the session
and restores the list on quit. They live only in memory, so the glyphs they add never touch a project file, and the
TMP Settings asset is left as it was. Checked in the Editor with "Łukasz 日本 Ağaç": Ł from Segoe UI, 日本 from
Microsoft YaHei, ğ and ç from Baloo. Colour emoji can't be drawn as SDF and show TMP's missing-glyph box. On a device
the fallback glyphs are a different typeface from Baloo (thinner); that is the price of not shipping a font for every
script.

## 10. Pending for the second animation batch (2026-11-01)

What the second batch (docs/animation-spec.md: UI sounds, stuck timing, blinking, ice break, shuffle) still needs.
Filled in stage by stage; every slot has a placeholder.

**Sounds:** none missing: every sound this batch plays is in `Assets/Audio/SFX/` with its slot in `SfxConfig`.

**Stage 1 (button, booster and purchase sounds, haptic calls):** no new art. The purchase punch scales the existing
booster count badges, the lives heart and the coins pill. Haptics need no assets (the vibration is code, not built yet).

**Stage 2 (stuck bar timing):** no new art. The stuck banner's background is still missing (section 4, Level HUD).

**Stage 3 (blinking):** no new art: the half-open and closed faces of colours 0–9 are the blink frames. Missing, and
optional: blink frames for colours 10–11 (ids without art; they keep their placeholder and do not blink) and for the
hidden ball (it never blinks: its colour is unknown, and its art has no eyes to close).

**Stage 4 (ice break, docs/animation-spec.md 3.4):**
| Needed | Slot | Placeholder now | Notes for the artist |
|---|---|---|---|
| Ice cracks (3 cracks growing over 0.16 s, or 2–3 frames) | none yet *(code: an overlay or frames on `TubeView`'s clay)* | the ice shivers and whitens | Drawn on the ice's own canvas (`Mechanics/Clay_Ice`), so it lines up. |
| Ice shard | `Prefabs/VFX/Vfx_IceBreak` texture sheet (Sprites mode) | `Placeholder/Vfx_IceShard` (generated white splinter, 32×48) | White or pale blue; tinted per particle on the prefab. 2–3 shapes welcome (the sheet takes several sprites). |
| Smoke puff | `Vfx_IceBreak/Puff` texture sheet | `Placeholder/Ball_Circle` | Soft white cloud, transparent edges. |
