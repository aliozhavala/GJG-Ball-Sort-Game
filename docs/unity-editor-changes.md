# Unity Editor changes made through MCP

A record of every change made in the Unity Editor via the Unity MCP tools (scenes, prefabs, assets,
project settings). Code and text files edited directly are tracked in git and are not listed here.
Newest entries at the bottom.

## 2026-10-27 — Roadmap step 2: level loading + greybox board

### Project settings
- **Sorting layers** (`ProjectSettings/TagManager.asset`): added, in order after `Default`:
  `Background`, `TubeBack`, `Balls`, `TubeFront`, `VFX`.

### Placeholder sprites
- Created folder `Assets/Art/Sprites/Placeholder/`.
- Copied `Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Circle.png`
  → `Assets/Art/Sprites/Placeholder/Ball_Circle.png`.
- Copied `.../Textures/v2/9Sliced.png` → `Assets/Art/Sprites/Placeholder/Tube_9Sliced.png`
  (keeps its 64 px borders on all sides).
- Both: import type Sprite (Single), **Mesh Type = Full Rect** (required for sliced drawing), 256 PPU (1 world unit).
- Why copies: the originals live in a package `Editor/` folder, which is excluded from player builds.

### TextMesh Pro
- Imported **TMP Essential Resources** (`TMP_PackageResourceImporter.ImportResources(essentials: true, examples: false)`).
  Creates `Assets/TextMesh Pro/` (TMP Settings, LiberationSans SDF font, shaders). Needed for the hidden-ball "?" mark.

### ScriptableObject assets
- `Assets/Data/Config/ColorPalette.asset` (`ColorPalette`)
  - Default sprite and hidden sprite: `Ball_Circle`. Hidden tint `(0.32, 0.33, 0.38)`.
  - 12 entries, all sprite `Ball_Circle`, tints by colour id:
    0 `#E53935` red · 1 `#1E88E5` blue · 2 `#FDD835` yellow · 3 `#43A047` green · 4 `#8E24AA` purple ·
    5 `#FB8C00` orange · 6 `#00ACC1` cyan · 7 `#F06292` pink · 8 `#C0CA33` lime · 9 `#6D4C41` brown ·
    10 `#283593` navy · 11 `#F5F5F5` white.
- `Assets/Data/Config/TubeVisualConfig.asset` (`TubeVisualConfig`)
  - Back, front and cap sprites: `Tube_9Sliced`. Cap tint `(0.85, 0.75, 0.55)`, cap height `0.5`.
  - All other values are the script defaults (tube width 1.1, slot height 1, bottom offset 0.55,
    top padding 0.15, ball diameter 0.9, lift height 1, spacing 0.45 / 0.5, back tint white α 0.25,
    front tint white α 0.12).
- `Assets/Data/Config/AnimationConfig.asset` (`AnimationConfig`): script defaults (placeholders).
- `Assets/Data/LevelDatabase.asset` (`LevelDatabase`): `_levels` = `level_001.json` … `level_005.json`, in order.

### Prefabs
- `Assets/Prefabs/Ball.prefab`
  - `Ball` (root): `BallView` (`_body` → Body, `_hiddenMark` → HiddenMark).
  - `Body`: SpriteRenderer, sprite `Ball_Circle`, material `Sprite-Unlit-Default`, sorting layer `Balls`, order 0.
  - `HiddenMark` (inactive by default): `TextMeshPro` "?", bold, size 7, centred, white α 0.9,
    rect 1×1, sorting layer `Balls`, order 1.
- `Assets/Prefabs/Tube.prefab`
  - `Tube` (root): `BoxCollider2D`, `TubeView` (`_back`, `_front`, `_cap`, `_ballRoot`, `_tapTarget` wired to the children/collider below).
  - `Back`: SpriteRenderer, `Tube_9Sliced`, Draw Mode Sliced, `Sprite-Unlit-Default`, layer `TubeBack`, order 0.
  - `Balls`: empty Transform; parent for ball views.
  - `Front`: SpriteRenderer, `Tube_9Sliced`, Sliced, `Sprite-Unlit-Default`, layer `TubeFront`, order 0.
  - `Cap`: SpriteRenderer, `Tube_9Sliced`, Sliced, `Sprite-Unlit-Default`, layer `TubeFront`, order 1, renderer disabled.
  - Sizes, positions and collider bounds are overwritten at runtime by `TubeView.Setup` from `TubeVisualConfig`.

### Scene `Assets/Scenes/Main.unity`
- `Main Camera` (modified): Clear Flags `Solid Color`, background `(0.11, 0.13, 0.18)`. Already orthographic, size 5.
- `GameBootstrap` (new, first root object): `GameBootstrap` with `_levelDatabase` → `LevelDatabase.asset`,
  `_boardView` → Board, `_levelNumber` = 1.
- `Board` (new): `BoardLayout` (`_camera` → Main Camera; margins at script defaults) and `BoardView`
  (`_tubePrefab` → Tube.prefab, `_ballPrefab` → Ball.prefab, `_palette` → ColorPalette, `_tubeConfig` →
  TubeVisualConfig, `_layout` → BoardLayout, `_poolRoot` → Board/Pool).
  - `Board/Pool` (new, **inactive**): holder for pooled tube and ball views.
- Scene saved.

### Verification runs (no persistent changes)
- Ran all EditMode tests via `TestRunnerApi` (161 passed, 0 failed).
- Entered Play mode, rebuilt levels 1–5 and rendered each at 9:16, 9:19.5 and 3:4 into temporary
  RenderTextures via `BoardLayout.FitTo`; exited Play mode. Play-mode changes are discarded on exit.

## 2026-10-27 — Roadmap step 3a: Core rules, player data, scenes, Home UI

### Asset moves and renames (AssetDatabase, so GUIDs and references are kept)
- Moved `Assets/Scripts/Game/Services/LevelJsonParser.cs` → `Assets/Scripts/LevelJson/LevelJsonParser.cs`
  (new `BallSort.LevelJson` assembly).
- Renamed `Assets/Scripts/Game/Bootstrap/GameBootstrap.cs` → `LevelBootstrap.cs` (class renamed to match;
  the scene component kept its script GUID `9cd0c2e8e49447143a71e8c87f853261`).
- Renamed scene `Assets/Scenes/Main.unity` → `Assets/Scenes/Level.unity`.

### Project settings
- **Build settings** (`ProjectSettings/EditorBuildSettings.asset`): scenes are now
  `0 Assets/Scenes/Home.unity`, `1 Assets/Scenes/Level.unity` (Main.unity entry replaced).

### ScriptableObject assets
- `Assets/Data/Config/EconomyConfig.asset` (`EconomyConfig`), script defaults: start coins 1000, lives 5,
  Undo/Shuffle/AddTube 99/99/99, win reward 20, play-on cost 900, added-tube capacity 4.

### Prefabs
- `Assets/Resources/AppRoot.prefab`: `AppRoot` (`_economy` → EconomyConfig, `_levelDatabase` → LevelDatabase.asset).
  Loaded by name `AppRoot` at launch.
- `Assets/Prefabs/UI/Toast.prefab`: anchored bottom-centre (y +380, 860×130), dark rounded `Image`
  (UISprite, no raycast), `CanvasGroup` (alpha 0, no raycasts), `Toast` (`_label`, `_group`); child `Label` (TMP 46).
- `Assets/Prefabs/UI/SettingsPopup.prefab` (root **inactive** = closed): full-screen dim `Image` (black α 0.6,
  raycast target), `Popup` (`_closeButton` → CloseButton), `SettingsPopup` (`_popup`, `_toggles` = Sound/Music/Haptics,
  `_restartButton`, `_homeButton`).
  - `Panel` (820 wide, `VerticalLayoutGroup` + `ContentSizeFitter`, so it shrinks when Restart/Home are hidden):
    `Title`, `SoundRow`/`MusicRow`/`HapticsRow` (TMP label + `Toggle` with `SettingToggle`), `RestartButton`,
    `HomeButton`, `CloseButton` ("×", ignores layout, top-right).

### Scene `Assets/Scenes/Level.unity` (was Main)
- `GameBootstrap` object renamed `LevelBootstrap`; `LevelBootstrap._hud` → LevelCanvas. `_boardView` unchanged;
  the removed `_levelDatabase`/`_levelNumber` fields were dropped on save.
- `EventSystem` (new): `EventSystem` + `InputSystemUIInputModule`.
- `LevelCanvas` (new): Screen Space Overlay, `CanvasScaler` 1080×1920 match 0.5, `GraphicRaycaster`, `LevelHud`
  (`_levelTitle`, `_settingsButton`, `_settingsPopup`, `_toast`).
  - `SafeArea` (`SafeArea`) → `TopBar` (top, 180 high) → `SettingsButton` ("≡", left, 130×130), `LevelTitle` (TMP 72).
  - `SettingsPopup` and `Toast` prefab instances (in that order, so the toast draws above the popup).

### Scene `Assets/Scenes/Home.unity` (new)
- `HomeBootstrap` (`_homeScreen` → HomeCanvas).
- `Main Camera`: orthographic, solid colour `#1B2030`, `UniversalAdditionalCameraData`, `AudioListener`.
- `EventSystem`: `EventSystem` + `InputSystemUIInputModule`.
- `HomeCanvas`: same canvas setup as Level, `HomeScreen` (all references wired; `_livesFormat` =
  `<color=#FF5252>♥</color> {0}`, `_coinsFormat` = `<color=#FFC107>●</color> {0}`).
  - `Background` (full-screen `#1B2030`, no raycast).
  - `SafeArea` → `TopBar` (`AvatarButton` "You", `Lives` pill, `Coins` pill, `SettingsButton` "≡"),
    `LevelButton` (green, 680×230, auto-sized label), `BottomNav` (`HorizontalLayoutGroup`: `LeaderboardButton`,
    `HomeButton` (selected, non-interactable), `ShopButton`).
  - `SettingsPopup` and `Toast` prefab instances.

### Verification runs (no persistent changes)
- EditMode tests via `TestRunnerApi`: 213 passed, 0 failed.
- Play mode from Home: Home → Level → Restart → Home, settings toggle saved and restored after a Play
  restart, "Coming soon" toast. The test save was deleted afterwards (no save existed before).

## 2026-10-27 — Roadmap step 3b: level gameplay UI and flow

### Assets
- `Assets/Data/Levels/level_006.json` (new): placeholder "stuck trap" level, so the stuck / Play on flow can be
  reached in play. Appended to `LevelDatabase.asset` as level 6.
- `AnimationConfig.asset`: new field `_celebrationDuration` (script default 1 s).

### Prefabs
- `Assets/Prefabs/UI/WinPopup.prefab` (new, root inactive): dim `Image`, `Popup`, `WinPopup`; `Panel`
  (820 wide, vertical layout) → `Title`, `Reward`, `ContinueButton`.

### Scene `Assets/Scenes/Level.unity`
- `LevelFlow` (new root): `LevelFlow` + `LevelFeedback` (animation config, celebration label).
- `LevelBootstrap`: `_flow` → LevelFlow (the old `_boardView` / `_hud` fields were removed).
- `Board`: added `TubeInput` (`_camera` → Main Camera).
- `LevelCanvas` → `LevelHud` gains `_boosterBar`, `_stuckPanel`, `_winPopup`.
  - `SafeArea/BoosterBar` (bottom, 190 high): `CanvasGroup`, `BoosterBar`, three `BoosterButton`s (Undo, Shuffle,
    + Tube) with a count badge.
  - `SafeArea/StuckPanel` (inactive): `Banner` ("No available moves", under the top bar) and `Buttons`
    (Give up, Play on) in the booster bar's place.
  - `Celebration` ("Well done!", inactive) after `SafeArea`; `WinPopup` instance between `SettingsPopup` and `Toast`.

### Verification runs (no persistent changes)
- EditMode tests: 234 passed. Play mode on levels 1, 3 and 6 driven with queued Input System mouse events; the
  original save was backed up and restored.

### Step 3b follow-up (review fixes)
- `level_006.json` rewritten with capacity-4 tubes only (the trap: tube 2 → 5, then 1 → 2).
- `Assets/Art/Sprites/Placeholder/Hidden_Mark.png` (new, white "?" rendered from LiberationSans), imported as a
  Sprite; `ColorPalette.asset` → `_hiddenMarkSprite`, `_hiddenMarkSize` 0.55.
- `Ball.prefab`: the `HiddenMark` TextMeshPro child was replaced by a SpriteRenderer (`Sprite-Unlit-Default`,
  Balls layer, order 1); `BallView._hiddenMark` re-wired.
- Level scene: booster buttons 170×130 (bar 150 high at y 20, badge inside the corner); stuck buttons 330/450×130.
  New `SafeArea/BoardArea` (stretch, bottom 190, top −280, no graphic) wired to `BoardLayout._boardArea`.
- Verification: EditMode 241 passed; Play mode on level 3 at 1391×657, board clear of both HUD bars.

## 2026-10-28 — Roadmap 4a: Core mechanics, grid layout and validation

### Assets
- `Assets/Data/Levels/level_001.json`–`level_005.json`: `layout.grid` added (same arrangement as the old rows).
- `Assets/Data/Levels/level_006.json`: content replaced by the veil test level (the file and its GUID are kept, so
  the `LevelDatabase` slot is unchanged). The old stuck trap is now the test fixture `TestLevels.StuckTrap()`.
- `Assets/Data/Levels/level_007.json` (new, lock and key) and `level_008.json` (new, clay + capacity-10 tube spanning
  2 rows): appended to `LevelDatabase.asset` as levels 7 and 8 (through MCP, `SerializedObject`).
- `TubeVisualConfig` lost its `_verticalSpacing` field (rows are now spaced by the grid rule). `TubeVisualConfig.asset`
  still holds the stale `_verticalSpacing: 0.5` key until Unity next re-serializes it; it is ignored.
- `LevelBootstrap` gained `_loadFailedFormat` ("Level {0} could not be loaded"); the scene uses the script default,
  so `Level.unity` was not modified.

### Verification runs (no persistent changes)
- EditMode tests via `TestRunnerApi`: 342 passed, 0 failed.
- Play mode (Game view 1391×657): level 4 rows centred; level 8 shown through `BoardView.Initialize` with a fresh
  session: large tube bottom-aligned, flush with the top row, top row left-aligned with the aligned block; two
  added tubes went to top then bottom, the scale refitted only when the widest row grew.
- Load failure: the play-mode `AppRoot` was pointed at an in-memory database holding one broken file; the Level scene
  logged the validator error and returned to Home with the toast "Level 1 could not be loaded". Nothing was saved.

## 2026-10-28 — Roadmap 4b: solver, Validate Levels, Shuffle

### Assets
- `Assets/Data/Levels/level_001.json`–`level_008.json`: `meta.optimalMoves` written by the new Validate Levels
  writer (run through MCP, same code as the window's button): 10, 16, 13, 16, 16, 6, 8, 10. One line changed per
  file; formatting, key order, LF endings and no BOM kept.
- `BoosterBar._shuffleComingSoon` was replaced by `_noValidShuffle` ("Can't shuffle right now"). `Level.unity` was
  saved from the test runner's "Scene(s) Have Been Modified" prompt: it now serializes `_noValidShuffle` (stale key
  gone) and `LevelBootstrap._loadFailedFormat`; the layout-driven anchor noise was dropped. It still carries the
  earlier uncommitted `_editorLevelOverride: 8`.

### Verification runs (no persistent changes except the save)
- EditMode tests via `TestRunnerApi` (`runSynchronously`): 393 passed, 0 failed.
- Validate Levels logic (`LevelValidationRunner`) over all 8 levels; the window opened from its menu item without
  console errors.
- Play mode, level 8 (the scene's editor override): Shuffle pressed twice through its button — flow went
  Playing → Shuffling → Playing within 2–3 frames, the board view rebuilt in sync with the model, undo history
  cleared. This spent 2 Shuffle boosters from the Editor save (99 → 97).

### 4b follow-up: shuffle limits and "Shuffling…" indicator
- `BoardShuffler.NodeLimit` 20,000 → 2,000 (20 attempts kept). Worst case re-measured (every attempt failing at the
  limit): ~0.59 s on 12 tubes, ~0.97–1.08 s on 18 tubes.
- `Level.unity` (through MCP, saved):
  - New `LevelCanvas/SafeArea/ShufflingIndicator` (sibling after `StuckPanel`, inactive): same rect as
    `StuckPanel/Banner` (top stretch, y −185, height 80); TextMeshProUGUI "Shuffling…", LiberationSans SDF 50,
    white, centred, not a raycast target.
  - `LevelHud._shufflingIndicator` → that label. `LevelFlow._shufflingIndicatorDelay` 0.3 (script default).
  - `LevelBootstrap._editorLevelOverride` 8 → 0.
- Verification: EditMode 393 passed. Play mode on a temporary in-memory 18-tube worst-case level (play-mode
  `AppRoot` only, nothing saved): "Shuffling…" appeared at 0.32 s, the search ended at 0.97 s, the label hid, the
  toast said "Can't shuffle right now", no booster spent. A fast shuffle on the saved current level spent one booster
  from the Editor save (now 96 Shuffle). The synthetic level's 15 colours logged a palette-size error (palette has 12).

## 2026-10-28 — Roadmap 4c: mechanic views (placeholders), editor level jump

### Placeholder sprites
- New, generated through MCP (white fill, dark outline, so a tint colours only the fill), imported as Sprite,
  256 ppu, Full Rect, no mipmaps, Clamp:
  - `Assets/Art/Sprites/Placeholder/Lock_Icon.png` (padlock), `Key_Icon.png` (key), `Veil_Badge.png` (disc).

### Assets
- `Assets/Data/Config/LockPalette.asset` (new `LockPalette`): tints mint (0.55, 1, 0.78), lavender (0.80, 0.64, 1),
  peach (1, 0.74, 0.58), magenta (1, 0.2, 1); unknown-id tint black. Picked by CIELAB distance: each is at least
  ΔE 36 from every ball colour and the hidden tint, and ΔE 63 from the others (gold/silver/gunmetal came within
  ΔE 4–21 of yellow, white and the hidden tint). Magenta is lock 3, so unknown ids use black instead.
- `TubeVisualConfig.asset`: veil sprite `Tube_9Sliced`, tint (0.55, 0.53, 0.62), badge `Veil_Badge` size 0.8;
  door sprite `Tube_9Sliced`, tint (0.22, 0.26, 0.33), lock icon `Lock_Icon` size 1; clay sprite `Tube_9Sliced`,
  tint (0.60, 0.46, 0.34, 0.6), height 1.2.
- `ColorPalette.asset`: key mark `Key_Icon`, size 0.75, offset (0.25, −0.25).
- `AnimationConfig.asset`: invalid flash colour (1, 0.25, 0.2, 0.6), duration 0.3.

### Prefabs
- `Assets/Prefabs/Cover.prefab` (new): root `CoverView`; `Body` SpriteRenderer (`Tube_9Sliced`, Sliced,
  `Sprite-Unlit-Default`, TubeFront order 3); `Icon` SpriteRenderer (TubeFront order 4). `_body`/`_icon` wired.
- `Tube.prefab`: new children `Clay` (SpriteRenderer, Sliced, TubeFront order 2, disabled) and `Flash`
  (SpriteRenderer, Sliced, VFX order 0, colour clear); `TubeView._clay`, `_flash` wired, `_raisedSortingOrder` 10.
  Draw order on TubeFront: glass 0, cap 1, clay 2, cover body 3, cover icon 4, lifted balls 10+.
- `Ball.prefab`: new child `KeyMark` (SpriteRenderer `Key_Icon`, Balls order 2, disabled, at (0.25, −0.25));
  `BallView._keyMark` wired.

### Scenes
- `Level.unity`: `Board` → `BoardView._coverPrefab` = `Cover.prefab`, `_lockPalette` = `LockPalette.asset`. Saved;
  the removed `LevelBootstrap._editorLevelOverride` key dropped out. The scene re-dirtied itself afterwards with
  the usual layout-driven anchor noise; a save-as-copy showed it had returned to the committed values, so it was
  saved again and the diff holds only the two references and the removed key.

### Verification runs (no persistent changes)
- EditMode tests via `TestRunnerApi` (`runSynchronously`, scene clean): 395 passed, 0 failed.
- Play mode through `PlayLevelWindow.Play` and in-play-mode jumps (SessionState + `SceneLoader.LoadLevel`), save's
  current level 4 throughout:
  - Level 7: door across tubes 3–4 (centred between them, width 2.65), their balls hidden, mint lock icon, key mark
    on tube 0 slot 1. Invalid tap on the door flashed (VFX layer). Add tube → bottom row re-centred and the door
    followed (x 0 → −0.78). Move 0→2 → key mark gone, door open, balls shown. Restart → door and key back.
    Shuffle → key stayed, door closed.
  - Level 6: veil over tube 3 with a red badge; completing colour 0 opened it and showed its balls.
  - Level 8: clay overlay only on the capacity-10 tube; clay tube not selectable, flash on tap; moves into it work.
  - Level 3: hidden marks shown; a reveal turned the new top face up.
  - In-memory database (play-mode `AppRoot` only) with two temporary levels: a key on a hidden ball had no mark;
    on the revealing move the views saw `BallRevealed` (mark shown), `KeyFound` (mark gone), `LockOpened` (balls
    shown). A stuck trap with a closed door reached Stuck with the door still closed; Play on added a 7th tube and
    the door followed its tube (x 3.88 → 3.10).
  - Exhaustive move search: levels 3, 6, 7 and 8 can never reach a stuck position (19,877 / 463 / 1,192 / 9,986
    positions).
- The checks spent 900 coins, one Add tube and one Shuffle from the Editor save; restored afterwards through
  `SaveService` (2040 coins, Shuffle 96, Add tube 62, current level 4).

### 4c follow-up: level files outside the run
- New folder `Assets/Data/TestLevels/` with `stuck_lock.json` (written as a text file, imported by an MCP
  `AssetDatabase.Refresh`; Unity generated both `.meta` files). Not in `LevelDatabase.asset`.
- No scene, prefab or config asset changed.
- Verification: EditMode 397 passed (2 new in `TestLevelFileTests`). Play mode through `PlayLevelWindow.PlayFile`:
  HUD "Level stuck_lock"; red tube 1 → 4, yellow tube 0 → 1 got stuck with the door closed (stuck panel shown,
  booster bar hidden, hidden key without a mark); Play on (900 coins) added a 7th tube and the door followed its tube
  (x 3.88 → 3.10). The level was then won with the solver's moves: coins and current level unchanged, Win panel
  "Level stuck_lock complete" with the reward line hidden. The 900 coins were restored through `SaveService`
  (2120 coins, current level 4 — the save had gained 80 coins outside these checks).

## 2026-10-29 — Roadmap 5: level format v2 (variants, tier, episodes)

All changes made through Unity MCP Editor commands; Unity generated every new `.meta` file.

### Placeholder sprites
- New folder `Assets/Art/Sprites/Placeholder/Backgrounds/` with 10 generated 90×160 vertical-gradient PNGs,
  `Episode1_Home.png` … `Episode5_Home.png` and `Episode1_Level.png` … `Episode5_Level.png` (one hue per episode:
  teal, violet, amber, green, blue; Level versions darker). Imported as Sprite (Single), no mipmaps, clamp, bilinear,
  uncompressed, 100 PPU.

### Assets
- New `Assets/Data/Config/EpisodeConfig.asset` (`EpisodeConfig`): 5 episodes, first levels 1, 21, 41, 61, 81, each with
  its Home and Level background sprite above.
- `Assets/Data/Config/EconomyConfig.asset`: re-saved after the code change; `_winReward: 20` became `_regularReward: 20`
  (`FormerlySerializedAs`), plus `_hardReward: 40` and `_superHardReward: 60`.
- Level files `Assets/Data/Levels/level_001.json` … `level_008.json` and `Assets/Data/TestLevels/stuck_lock.json`
  rewritten to format 2 (text files, by a script; V1 = the old content). `meta.optimalMoves` then written for all 40
  shipped variants by the Validate Levels runner and `OptimalMovesWriter`.

### Prefabs
- `Assets/Resources/AppRoot.prefab`: `AppRoot._episodes` → `EpisodeConfig`.

### Scenes
- `Level.unity`: new root object **Background** (after Main Camera) with a `SpriteRenderer` (Sprite-Unlit-Default,
  sorting layer Background, order 0, no sprite until play) and `BackgroundSprite` (`_renderer` → its renderer,
  `_camera` → Main Camera). `LevelBootstrap._background` → it.
- `Home.unity`:
  - `HomeCanvas/Background` (existing full-screen Image): colour dark navy → white, raycast target off; added
    `AspectRatioFitter` (Envelope Parent, 9:16) and `BackgroundImage` (`_image`, `_fitter` → those).
    `HomeBootstrap._background` → it.
  - `SafeArea/LevelButton`: added `LevelTierLook` (`_background` → the button's Image, `_regularTint` = the existing
    green 0.263/0.627/0.278, hard orange, super hard magenta) and a child **TierBadge** (inactive; UISprite sliced,
    dark 0.12/0.12/0.16/0.95, 320×72 centred on the button's top edge) with a TMP child **Label** (LiberationSans,
    44 bold, white, "Hard"). `HomeScreen._levelTierLook` → the `LevelTierLook`.

### Verification runs (no persistent changes)
- EditMode tests via `TestRunnerApi` (`runSynchronously`, scene clean): 523 passed, 0 failed (includes all 40 shipped
  level × variant solves, every `stuck_lock` variant, and the JSON examples in `docs/levels.md` and
  `docs/level-format-guide.md`).
- Validate Levels runner: every variant of levels 1–8 valid and solvable; optima equal to V1's except `level_006` V5
  (hidden ball: 7 instead of 6). Writer: a second write is a no-op; an unknown variant is refused.
- Play mode from Home with the Editor save (version 1, current level 4): the save migrated to version 2 (try 0, no
  variant); Home showed `Episode1_Home` and the hard look (orange, "Hard" badge). Entering level 4 started try 1 on V1
  with `Episode1_Level` covering the camera; Restart ×8 played V2, V3, V4, V5, then V3, V4, V3, V4, each try saved at
  once. Winning paid 40 coins (Win panel "+40"), moved to level 5 and reset the tries. Editor jump to level 6 V5: HUD
  "Level 6 · V5", Restart kept V5, no try counted, the win paid nothing (reward line hidden) and kept level 5.
- The original Editor save string (version 1, level 4, 1220 coins) was written back afterwards.

## 2026-10-29 — Import of the delivered levels 1–100

Asset moves made through Unity MCP Editor commands (`AssetDatabase.MoveAsset`, `AssetDatabase.MoveAssetToTrash`;
`DeleteAsset` is refused for MCP commands); Unity moved or removed every `.meta` file itself.

### Level files
- `Assets/Data/Levels/level_001.json`–`level_003.json` (placeholders, no test used them): moved to the trash.
- `Assets/Data/Levels/level_004.json`–`level_008.json` (placeholders) moved to `Assets/Data/TestLevels/` as
  `uneven_rows.json`, `full_rows.json`, `veil.json`, `lock_and_key.json` and `clay_large_tube.json` (GUIDs kept), then
  their `id` set to 902–906 (text edit).
- `Assets/Data/LevelsIncoming/level_001.json` … `level_100.json` moved to `Assets/Data/Levels/` (their GUIDs kept, so
  `level_001`–`level_008` now have new GUIDs); the empty `Assets/Data/LevelsIncoming/` folder moved to the trash.
- `meta.optimalMoves` written into 64 variants of the delivered files with `OptimalMovesWriter` (text edit, one line
  each; `difficulty` and `notes` untouched), then `AssetDatabase.Refresh`. `level_051` V3 has none (not settled).

### Assets
- `Assets/Data/LevelDatabase.asset` (`LevelDatabase`): `_levels` = `level_001.json` … `level_100.json`, in order
  (slot n = level n; was the 8 placeholders). While the list was replaced, `OnValidate` logged the old list once
  (slots 1–3 empty, slots 4–8 pointing at the moved test levels); nothing after a forced reimport.
- No scene, prefab or config asset changed: `EpisodeConfig` already covers levels 1–100, `ColorPalette` has 12
  colours (the levels use at most 10) and `LockPalette` 4 (the levels use lock ids 0–2).

### Verification runs (no persistent changes)
- EditMode tests via `TestRunnerApi` (`runSynchronously`, scene clean): 1100 passed, 0 failed (all 500 shipped
  level × variant solves, every fixture variant, the mechanic tests moved to `TestLevelFileTests`).
- Shuffle timing through `LevelSession.TryBeginShuffle` / `PendingShuffle.Search` on the main thread: results in
  `docs/mechanics.md` (Shuffle, Limits).

## 2026-10-29 — Lives, tutorials and Home tabs, stage 1: player save moved into testable assemblies

Asset moves made through a Unity MCP Editor command (`AssetDatabase.MoveAsset`); Unity moved every `.meta` file itself,
so the GUIDs are kept. No scene, prefab or config asset changed.

### Scripts
- `Assets/Scripts/Game/Services/PlayerData.cs`, `SaveData.cs` and `SettingType.cs` moved to `Assets/Scripts/Core/Player/`
  (namespace now `BallSort.Core.Player`).
- New: `Assets/Scripts/Core/Player/PlayerRules.cs`, the folder `Assets/Scripts/SaveJson/` with
  `BallSort.SaveJson.asmdef` and `SaveJsonCodec.cs`, and `Assets/Tests/EditMode/SaveJsonCodecTests.cs` (Unity generated
  their `.meta` files).

### Verification runs (no persistent changes)
- EditMode tests via `TestRunnerApi` (`runSynchronously`, scene clean): 1100 passed before the change; 1114 passed after
  (the same 1100 unchanged, plus 14 codec tests), 0 failed. Console clean.
- The Editor's stored save, read through the new `SaveService` and written back in memory (not saved), gave the same
  JSON string.

## 2026-10-29 — Lives, tutorials and Home tabs, stage 2: Core rules and save version 3

No scene, prefab or asset changed. `EconomyConfig.asset` still stores `_startLives: 5`; the script reads it as
`_maxLives` (`FormerlySerializedAs`) and `_lifeRefillMinutes` takes its default of 30 until the asset is next saved.

### Verification runs (no persistent changes)
- EditMode tests via `TestRunnerApi` (`runSynchronously`, scene clean): 1189 passed, 0 failed. Console clean.
- The Editor's stored save (version 2) read through the version 3 code without a warning: level 1, 100 coins, 5/5
  lives, retries 0, name `Player`, no tutorials seen. The stored string was not rewritten (the next Play migrates it).

## 2026-10-29 — Lives, tutorials and Home tabs, stage 3: Level scene

All changes made through Unity MCP Editor commands; Unity generated every new `.meta` file.

### Placeholder sprites (generated, imported as Sprite, single, 100 PPU, no mipmaps, alpha is transparency)
- New folder `Assets/Art/Sprites/Placeholder/UI/` with `Heart.png` (128×128, white fill and dark outline, so a tint
  colours only the fill).
- New folder `Assets/Art/Sprites/Placeholder/Mascots/` with `Episode1_Mascot.png` … `Episode5_Mascot.png` (160×200: a
  scientist with goggles and a lab coat in a gold frame, on the episode's Home background hue).

### Config assets
- `Assets/Data/Config/TutorialConfig.asset` (new, `TutorialConfig`): entries for levels 1, 7, 20, 30, 55 and 80 with
  placeholder text on the mechanic each introduces (basic sorting, hidden balls, clay + large tube, veil, normal-size
  clay, lock and key); no images.
- `Assets/Data/Config/EpisodeConfig.asset`: `_mascotFrame` of episodes 1–5 = `Episode1_Mascot` … `Episode5_Mascot`.

### Prefabs (new, in `Assets/Prefabs/UI/`, built from copies of `WinPopup.prefab`; root inactive)
- `AreYouSurePopup.prefab`: `Popup` + `AreYouSurePopup`; Panel: Title "Are you sure?", LifeCost "You will lose a
  life.", ConfirmButton (red, "Confirm"), CloseButton (copied from `SettingsPopup.prefab`, wired as the popup's close).
- `MoreLivesPopup.prefab`: `Popup` + `MoreLivesPopup`; Panel: Title "More lives", Heart (`Heart.png` tinted #E53935,
  220 high) with the Count label on it, Timer "Next life in 30:00", CloseButton.
- `TutorialPopup.prefab`: `Popup` + `TutorialPopup` + a `Button` on the root (no transition) as the tap-anywhere
  dismiss area; Panel: Picture (inactive, 360 high, preserve aspect), Text, Hint "Tap anywhere to continue".

### Scene `Assets/Scenes/Level.unity`
- `LevelCanvas`: instances of `AreYouSurePopup`, `MoreLivesPopup` and `TutorialPopup` (inactive) between `WinPopup`
  and `Toast`, so they draw over the settings popup and under the toast.
- `LevelCanvas/SafeArea/TopBar/MascotFrame` (new `Image`, anchored top right at -40, 136×170, preserve aspect, not a
  raycast target; sprite `Episode1_Mascot`, replaced at runtime by the level's episode).
- `LevelHud`: `_mascotFrame`, `_areYouSurePopup`, `_moreLivesPopup`, `_tutorialPopup` assigned.
- `LevelBootstrap`: `_tutorials` = `TutorialConfig.asset`.

### Verification runs
- EditMode tests via `TestRunnerApi` (`runSynchronously`, scene clean): 1189 passed, 0 failed. Console clean.
- Play mode on the Editor save (backed up first and restored afterwards), driving the real buttons: level 1 opened
  with its tutorial popup and the episode 1 mascot (the version 2 save migrated); dismissing saved it as seen;
  Restart → Are You Sure → Close kept the settings popup open and spent nothing; Restart → Confirm spent a life,
  counted a retry and played V2; at 0 lives Restart opened More Lives ("0", "Next life in 30:00") over the settings
  popup; Home at 0 lives hid the life line, kept 0 lives and counted the retry (Confirm tapped twice counted once).
  Level 20 won with the solver handed Home "episode 2 unlocked". Stopping Play mode on level 21 and starting again
  charged the interrupted attempt (4 lives, 1 retry, V2).
- Setting `Time.timeScale` during that run made Unity rewrite `ProjectSettings/TimeManager.asset` in its newer format
  (same values); the file was restored from git.

## 2026-10-29 — Lives, tutorials and Home tabs, stage 4: Home

All changes made through Unity MCP Editor commands; Unity generated every new `.meta` file.

### Prefabs (new, in `Assets/Prefabs/UI/`, built from copies of `WinPopup.prefab`; root inactive)
- `EditProfilePopup.prefab`: `Popup` + `EditProfilePopup`; Panel: Title "Edit profile", NameLabel "Name", NameField
  (`TMP_InputField` from `TMP_DefaultControls`, single line, 16 characters, placeholder "Your name", text area padded
  32 px), SaveButton (green, "Save"), CloseButton (copied from `SettingsPopup.prefab`, wired as the popup's close).
- `EpisodeUnlockedOverlay.prefab`: `Popup` + `EpisodeUnlockedOverlay` + a `Button` on the root (no transition) as the
  tap-anywhere area, root dim at 75 %; Panel: Title "Episode 2 unlocked" (gold), Mascot (400 high, preserve aspect),
  Hint "Tap anywhere to continue".

### Scene `Assets/Scenes/Home.unity`
- New root `HomeFlow` (`HomeFlow`: `_homeScreen` = HomeCanvas, `_background` = HomeCanvas/Background,
  `_episodeUnlockedDelay` 0.7). `HomeBootstrap._flow` = it (its old `_homeScreen` and `_background` fields are gone).
- `SafeArea/TopBar/Lives`: `LivesDisplay` added; new `Heart` (`Heart.png` tinted #E53935, 88×88, left) with a `Count`
  label on it; the old `Label` renamed `Timer` (stretched right of the heart, 44 pt, "Full").
- `SafeArea`: new `HomePage` (holds the `LevelButton`, moved from `SafeArea`), `LeaderboardPage` and `ShopPage`
  (inactive placeholders: a panel with a title, "Coming soon" and rows that do nothing), each between the top bar (180)
  and the bottom navigation (200).
- `SafeArea/BottomNav`: `HomeTabs` added (Leaderboard → LeaderboardPage, Home → HomePage, Shop → ShopPage; starts on
  Home). Each tab button: base colour #3A4360 (Home was #5C6BC0), a new `Icon` (built-in Knob sprite, tinted
  gold/blue/green), the label moved to the lower half at 34 pt, and a new `Highlight` bar (#FFC107, 10 px, bottom).
- `HomeCanvas`: new `InputBlocker` (transparent, raycast target, inactive) after `SafeArea`; instances of
  `EditProfilePopup`, `MoreLivesPopup` and `EpisodeUnlockedOverlay` (inactive) between `SettingsPopup` and `Toast`.
- `HomeScreen`: `_lives`, `_tabs`, `_editProfilePopup`, `_moreLivesPopup`, `_episodeUnlockedOverlay`, `_inputBlocker`
  assigned (its old `_livesLabel`, `_leaderboardButton`, `_shopButton`, `_livesFormat`, `_comingSoonText` are gone).

### Verification runs
- EditMode tests via `TestRunnerApi` (`runSynchronously`, scenes clean): 1192 passed, 0 failed (3 new for the level
  jump). Console clean.
- Play mode on the Editor save (backed up first and restored afterwards): Home showed "5" and "Full"; tabs swapped
  pages and highlights; Edit Profile opened with "Player", disabled Save for a blank name, saved "  Ada  " as "Ada",
  and Close discarded an edit; at 0 lives the Level button opened More Lives ("0", "Next life in 01:00") and loaded
  nothing; Home reloaded with "episode 2 unlocked" opened on the episode 1 background with input blocked, and 0.70 s
  later showed the overlay (episode 2 mascot) over the episode 2 background; a tap closed it. The Home and Level
  screens were rendered to 1080×1920 images (temporary camera, Play mode only) to check the layouts.
- `Ball Sort ▸ Player Data…` opened and loaded the save without errors. (After this run it also gained a Current
  level field with a Jump button, backed by `PlayerData.SetCurrentLevel`, so Episode Unlocked can be tested by hand.)

## 2026-10-30 — Home tab fix and the new stuck rule

All changes made through Unity MCP Editor commands.

### Scenes
- `Assets/Scenes/Home.unity`: `SafeArea/BottomNav/HomeButton` set interactable (it was saved as not interactable, the old
  "always selected" look, so real taps on it did nothing once another tab was open) and given the same colour block as
  the other tabs. `HomeTabs` now also makes every tab interactable when it starts.
- `Assets/Scenes/Level.unity`: `StuckPanel._bannerLabel` = `StuckPanel/Banner/Label`; the label's text set to
  "No more useful moves" (`StuckPanel._bannerText` sets it at runtime, the one place to change it).

### Verification runs (no persistent changes)
- Play mode on Home (the Editor save backed up first and restored afterwards): all 9 tab-to-tab combinations tapped
  through the event system (raycast at the tab's centre, then the click sent to whatever handles it there): 9/9 showed
  the right page and highlight, each tap handled by the tab under it.
- `StuckDetector` timing in the Editor: random play on every level and variant (15,307 checks) averaged 0.02 ms, at most
  9 positions explored; on synthetic one-colour 13-tube boards built to keep every move reversible, about 0.1 ms per
  position (limit 8: 0.7–0.8 ms, 16: 1.4–1.6 ms, 24: 2.7–3.0 ms, 32: 3.4–4.4 ms, 48: 6.0–7.4 ms, 64: 9.9–11.2 ms).
  `DefaultPositionLimit` set to 32.

## 2026-10-30 — Step 6a: first art delivery integrated

All changes made through Unity MCP Editor commands (or the new **Ball Sort ▸ Art ▸ Prepare Sprites**, run through MCP).

### Assets moved and renamed (`AssetDatabase.MoveAsset`, `.meta` files kept)
- 82 PNGs from `Assets/Art/Incoming/` to `Assets/Art/Sprites/{Balls, Tubes, Mechanics, UI/Buttons, UI/Displays,
  UI/Home, UI/Shop, Episodes, Backgrounds}` and `Assets/Art/Unused/` (mapping in docs/art-inventory.md); the empty
  `Incoming` folders moved to the trash. Originals committed untouched before trimming.
- New folders: `Assets/Art/Sprites/{Balls, Tubes, Mechanics, UI, UI/Buttons, UI/Displays, UI/Home, UI/Shop, Episodes,
  Backgrounds}`, `Assets/Art/Unused`.

### Prepare Sprites (first run)
- Trimmed in place: 5 buttons, 2 displays, 3 shop pieces (each to its content); 3 bottom bars (shared 928×244);
  5 mascot frames (shared 856×844); tube top/middle/bottom/full, clay and veil (cut from the shared canvas, pivots set).
- Import settings on 72 sprites (`SpriteRules`): Sprite, Single, 100 PPU, no mipmaps, clamp, bilinear, max size and
  Android/iOS ASTC per folder; ball pivots on the body centre; 9-slice borders on the pills, displays, veil and tube
  outline. A second run changed no file (idempotent).

### Configs
- `Assets/Data/Config/ColorPalette.asset`: entries 0–9 → the monsters' four faces (Red, Blue, Yellow, Green, Purple,
  Orange, Turquoise, Pink, Lilac, Brown), sprite tint white, sprite size 256/175; `_color` sampled from each monster's
  body (veil badge); entries 10–11 keep `Ball_Circle` tinted with their colour; hidden sprite `Ball_Hidden`, tint white,
  size 256/195; hidden mark cleared.
- `Assets/Data/Config/TubeVisualConfig.asset`: top/middle/bottom/outline tube sprites, tube tint white, art body width
  145; front empty; veil `Veil_Towel`, tint white, bottom gap 1; clay `Clay_Ice`, tint white. Cap and door unchanged.
- `Assets/Data/Config/EpisodeConfig.asset`: Level backgrounds and mascot frames for episodes 1–5; Home backgrounds
  `Home_Ep2_Summer` for episodes 1–4, `Home_Ep5_Space` for episode 5.

### Prefabs
- `Assets/Prefabs/Tube.prefab`: `Back` renamed `Bottom`; new `Middle` and `Top` (copies of it, TubeBack, order 0, Simple);
  `Clay` moved to TubeBack order 1 (behind the balls), Simple; `TubeView._top/_middle/_bottom` assigned.
- `Assets/Prefabs/UI/AreYouSurePopup.prefab`: ConfirmButton → `Button_Red` (sliced, white bold label); CloseButton →
  `Button_Close` (120×120, label hidden).
- `Assets/Prefabs/UI/EditProfilePopup.prefab`: SaveButton → `Button_Green`; CloseButton → `Button_Close`.
- `Assets/Prefabs/UI/MoreLivesPopup.prefab`, `SettingsPopup.prefab`: CloseButton → `Button_Close`.
- `Assets/Prefabs/UI/WinPopup.prefab`: ContinueButton → `Button_Green`.

### Scenes
- `Assets/Scenes/Home.unity`:
  - `TopBar/AvatarButton` 160×160, `MascotFrame_Ep1_Lab` (preserve aspect), label hidden; `HomeScreen._avatarImage`
    assigned, `_coinsFormat` = "{0}".
  - `TopBar/Lives` 280×110 at x −150, `Display_Lives` sliced; `Heart` stretched over the drawn heart (image disabled),
    count white bold 46; `Timer` over the pill, white bold 40.
  - `TopBar/Coins` 280×110 at x 160, `Display_Coins` sliced; `Label` over the pill, white bold 44.
  - `TopBar/SettingsButton` 140×140, `Button_Settings`, label hidden.
  - `HomePage/LevelButton` → `Button_Green` sliced, white bold label; `LevelTierLook` tints all white.
  - `BottomNav`: layout group removed, `AspectRatioFitter` (width controls height, 3.80), image `BottomBar_Home`;
    children reordered `ShopButton`, `HomeButton`, `LeaderboardButton`, each an invisible tap area over its drawn
    section (anchors 0.010–0.408, 0.408–0.594, 0.594–0.992), icons, labels and highlights removed, transition none;
    `HomeTabs._bar` and `_tabs` (Shop, Home, Leaderboard with their bar sprites), start tab 1.
  - Pages (`ShopPage`, `HomePage`, `LeaderboardPage`) stretched with offsets bottom 290, top 180.
  - `ShopPage`: old `Panel` removed; new `Scroll` (ScrollRect, vertical) → `Viewport` (RectMask2D) → `Content`
    (VerticalLayoutGroup, ContentSizeFitter) with `Header` (`Shop_Header`, "Shop"), `Panel` (`Shop_Panel`) and
    `Bundle1–3` (`Shop_Bundle`, name and price texts), all 920 wide.
- `Assets/Scenes/Level.unity`: `TopBar/SettingsButton` 140×140 `Button_Settings` (label hidden); `TopBar/MascotFrame`
  170×170, preserve aspect; `LevelTitle` bold; `StuckPanel/Buttons/GiveUpButton` → `Button_Red`, `PlayOnButton` →
  `Button_Yellow` (dark label); `StuckPanel._playOnFormat` = "Play on — {0} ●".

### Verification runs (no persistent changes)
- Console clean after every compile. EditMode tests: 1707 passed, 0 failed.
- Play mode on a 1440×3088 Game view (editor test visits, nothing spent): Home, Shop (top and scrolled), Leaderboard
  tab via real taps; levels 51 (hidden, veil, clay, capacity 16), 85 (lock and key, veils, clay, capacity 10) and 12
  (solver moves until two tubes completed: laughing faces, caps). Screenshots in `Screenshots/step6-art/`.
