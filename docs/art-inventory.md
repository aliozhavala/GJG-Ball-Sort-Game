# Art inventory (step 6, first delivery)

What the artist delivered (82 PNGs, first delivery, 2026-10-30), where each file went, what is wrong with it, and what
art is still missing. Measured on the delivered files: size, alpha, content bounds, and whether the parts can be
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
| `tube/tube_up.png` | `Tubes/Tube_Top.png` | `TubeVisualConfig` top (fixed size) |
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
| `button/button_default.png` | `UI/Buttons/Button_Green.png` | Continue (Win), Home Level button, Confirm (Edit Profile Save) |
| `button/button_red.png` | `UI/Buttons/Button_Red.png` | Give up, Are You Sure Confirm |
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
`frames/mainmenu_frame.png` (empty purple ring) is the Home avatar's placeholder frame, as
`UI/Avatar/Avatar_Frame_Empty.png`, until a real avatar frame arrives.

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
    (`+20 ●`) and the Play on price (`900 ●`), and no standalone heart for More Lives.
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
- **Tube cap / cork** for a completed tube (sits on the rim, ≈ rim width).
- **Lock door**: covers 1–3 adjacent tubes of the same height, so it must stretch in both directions (9-sliceable:
  plain middle, details in the corners/edges). Neutral colour (tinted per lock) or 4 coloured versions.
- **Lock icon** on the door, and **key mark** on a ball carrying a key (both tinted per lock, so light/white art).
- **Veil badge** (optional): the circle in the veil's colour we draw on the towel; ours stays meanwhile.
- **Ball colours 11 and 12** (ids 10–11; optional: the levels use 10 colours).
- **Clay ice at the tube's width**: the delivered ice is drawn 2.1× the tube's body, so the game scales it down to the
  tube's drawn width (rim), which makes it small. Please redraw it no wider than the rim (180 px at the tube canvas's
  scale), keeping its height, ideally with a see-through middle so the bottom ball stays visible (problem 5).
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
- **Booster icons**: Undo, Shuffle, Add tube, and a **count badge** (the buttons show text today).
- **Booster bar background** (bottom strip of the Level scene).
- **Level title plate** behind "Level N" (optional).
- **Stuck banner** ("No more useful moves") background.
- **Win celebration** title ("Well done!").

**Popups (Home and Level)**
- **Popup panel** (9-sliceable): Settings, Win, Are You Sure, More Lives, Tutorial, Edit Profile.
- **Popup title ribbon** (optional).
- **Toggle on/off** (or Sound / Music / Haptics icons with on/off states) for Settings.
- **Text input field** (Edit Profile name).
- **Standalone coin icon** (Win reward, Play on price) and **standalone heart** (More Lives) (problem 12).
- **Episode Unlocked** banner/overlay art.
- **Toast** background.

**Home**
- **Home backgrounds for episodes 1, 3 and 4** (Lab, Ice, Lava).
- **Level button tier looks**: the green button is regular; hard and super hard need their own look (another button
  colour or a badge), since tinting a coloured sprite only darkens it *(code: `LevelTierLook` swaps sprites instead of
  tints)*.
- **Leaderboard page**: title/header and a row (rank, avatar, name, score).
- **Shop item art** for the six frames of the shop panel (e.g. coin packs, booster packs) and a price tag; the bundle
  card's title (or we write it in text).
- **Empty avatar frame** for Home's avatar (top left; the same in every episode, a player picture may go inside later).
  The empty purple `mainmenu_frame` stands in meanwhile; if that is the intended one, just confirm.

**Packaging**
- **App icon** and **splash screen**.

## 5. How the art is prepared (Ball Sort ▸ Art ▸ Prepare Sprites)

One menu item makes delivered art ready; it can be run any time, files already trimmed are left as they are.
- **Trim each** (`UI/Buttons`, `UI/Displays`, `UI/Shop`, `UI/Avatar`): every file cut to its drawn pixels.
- **Trim shared** (`UI/Home`, `Episodes`): a folder is one swap set, cut to one rectangle holding all its files'
  content, so the bottom bar's versions and the five frames line up.
- **Tube** (`Tubes/*`, `Mechanics/Clay_Ice`, `Mechanics/Veil_Towel`, while they still share the delivery canvas): the
  parts are cut into bands that do not overlap (top 100, middle 420, bottom 88 px, all 180 px wide), and every piece
  gets a pivot on the walls' centre line (top part and veil at the rim's top, middle at its bottom, bottom part, outline
  and clay at the base), which is how `TubeView` and `CoverView` place them.
- Sizes are padded to multiples of 4 (block compression), then the import settings are applied per folder
  (`SpriteRules`): Sprite, Single, 100 PPU, no mipmaps, clamp, bilinear; max size 256 (balls), 1024 (tube, mechanics,
  UI, frames), 2048 (backgrounds, no alpha); Android and iOS ASTC 4×4 (balls, tube, mechanics, buttons, displays) or
  6×6 (bar, shop, frames, backgrounds). Ball pivots sit on the body's centre; 9-slice borders for the pills, displays
  and veil are in `SpriteRules.Borders`.

**New delivery:** copy the new PNG over the old file of the same name in `Assets/Art/Sprites/` (the `.meta` and every
reference survive), run Prepare Sprites, check the screens. A new file name or folder needs a line in `SpriteRules`.
