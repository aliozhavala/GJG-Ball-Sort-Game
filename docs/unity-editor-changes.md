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

## 2026-10-30 — Step 6a visual tweaks

All changes made through Unity MCP Editor commands.

### Assets
- `Assets/Art/Unused/Frame_Purple_Empty.png` moved to `Assets/Art/Sprites/UI/Avatar/Avatar_Frame_Empty.png` (new
  folder `UI/Avatar`); Prepare Sprites trimmed it (772×792) and applied the `UI/Avatar` import rule.
- `Assets/Data/Config/TubeVisualConfig.asset`: `_slotHeight` 1 → 0.91 (balls 1/10 of a ball closer per slot),
  `_bottomOffset` 0.55 → 0.59 (bottom ball ≈5 px higher at the reference resolution).

### Scenes
- `Assets/Scenes/Home.unity`: `TopBar/AvatarButton` image → `Avatar_Frame_Empty` (no longer set per episode);
  `TopBar/Coins/Label`: `AmountLabel` added (max 44 pt, min 30 pt), TMP auto-size off, no wrapping;
  `HomeScreen._coins` → it (`_coinsLabel`, `_coinsFormat`, `_avatarImage` are gone); `HomePage/LevelButton` anchored to
  the bottom centre of the page, 24 above it (just over the bottom bar).
- `Assets/Scenes/Level.unity`: `TopBar/SettingsButton` anchored top right (x −40); `TopBar/MascotFrame` top left,
  255×255, pivot top-left at (30, −6); `TopBar/LevelTitle` stretched with 311 margins left and right, auto-size 36–72,
  no wrapping; `StuckPanel/Banner` and `ShufflingIndicator` inset 300 left and right, auto-size 30–50, no wrapping.

### Verification runs (no persistent changes)
- Console clean. EditMode tests: 1724 passed, 0 failed (17 new, `AmountFormatTests`).
- Play mode (1080×1920 Game view, editor test visits): coin label shown with 12345 → 44 pt, 123456 → 38.9 pt,
  1234567 → 33.3 pt, 12345678 → "12.3M", 123456789 → "123M", 1234567890 → "1.2B", always one line (display only, the
  wallet untouched). Slot height changed to 1.2 on the asset in Play mode: the board re-laid out on the next frame
  (slot 1 at 1.79), then restored to 0.91 (1.50). Screenshots in `Screenshots/step6-art-2/`.

## 2026-10-30 — Step 6b board animations (docs/animation-spec.md 2.2–2.6)

All changes made through Unity MCP Editor commands.

### Assets
- New placeholder sprites (generated, white, Sprite, 100 PPU, no mipmaps, clamp):
  `Assets/Art/Sprites/Placeholder/Tube_Glow.png` (128×128, 9-slice border 48, pivot bottom centre),
  `Vfx_Sparkle.png` (64×64), `Vfx_Ring.png` (128×128).
- New folder `Assets/Prefabs/VFX/` with `Vfx_TubeSparkles`, `Vfx_TubeRing`, `Vfx_Confetti`, `Vfx_Droplets` (one
  `ParticleSystem` + `VfxBurst` each; Sprite-Unlit-Default material, VFX sorting layer, local simulation, hierarchy
  scaling, texture sheet in Sprites mode with the placeholders above / `Ball_Circle`).
- `Assets/Prefabs/Ball.prefab`: new children `Shape` → `Face`; `Body`, `HiddenMark`, `KeyMark` moved under `Face`
  (local transforms kept); `TrailRenderer` on the root (not emitting, 0.15 s, Sprite-Unlit-Default, TubeFront);
  `BallView._shape`, `_face`, `_trail` assigned.
- `Assets/Prefabs/Tube.prefab`: `Flash` renamed `Glow`, sorting layer VFX → TubeBack, order −1, renderer disabled;
  `TubeView._flash` → `_glow` (`FormerlySerializedAs`).
- `Assets/Data/Config/TubeVisualConfig.asset`: `_glowSprite` → `Tube_Glow` (`_liftHeight` removed from the code; the
  lift now lives in `AnimationConfig`).
- `Assets/Data/Config/AnimationConfig.asset`: new fields at their defaults; kept-name fields reset to the new values
  (`_liftDuration` 0.12 → 0.23, `_liftEase` OutQuad → OutCubic, `_dropDuration` 0.15 → 0.12, `_dropEase` OutQuad →
  InQuad, `_invalidShakeDuration` 0.25 → 0.4); `_corkDropHeight` 1.2.

### Scenes
- `Assets/Scenes/Level.unity`: `Board` gained `BoardClock` (`_animation` → AnimationConfig); `BoardView._animation` →
  AnimationConfig; `LevelFlow/LevelFeedback._clock` → the board's `BoardClock`, `_sparkles` / `_ring` / `_confetti` /
  `_droplets` → the four VFX prefabs.

### Verification runs (no persistent changes)
- Console clean after every compile. EditMode tests: 1724 passed, 0 failed.
- Play mode, editor test visits: level 5 V1 solution played with selections and overlapping flights (won, every ball
  view back in its slot after settling, tubes at home, board scale back after the punch); `veil.json` and
  `lock_and_key.json` with invalid taps on every tube, a mid-flight undo, selecting tubes while balls land (won; ball
  faces, slots and doors match the model). Frames of a completion checked (lift, stretch and trail, glow, cork, VFX).
- The undo spent by the check was restored in the save (Undo 99).

## 2026-10-30 — Step 6b sounds (2.2–2.6)

All changes made through Unity MCP Editor commands, except the copy of the sound files.

### Assets
- `Assets/Audio/SFX/{boosters,gameplay,mechanics,ui,win_rewards}/`: the 22 files of `Reference/sfx/` copied as is
  (file copy, Unity generated the `.meta` files); imported by `SfxImportRules` (short clips Decompress On Load + ADPCM,
  `sfx_win_fanfare` and `sfx_hidden_reveal` Compressed In Memory + Vorbis 0.7, all preloaded, sample rate preserved).
- New `Assets/Data/Config/SfxConfig.asset`: every slot filled, volume 1; random pitch on the mp3s and
  `sfx_ball_deselect`; `_startOffset` 0.05 on the mp3s; master 0.7, 10 voices.
- `Assets/Resources/AppRoot.prefab`: `AudioService` added (`_config` → SfxConfig); `AppRoot._audio` → it.

### Verification runs (no persistent changes)
- Leading silence measured on every clip (first sample above 5 % of the peak): mp3s 52–116 ms, WAVs 7–8 ms. The two
  compressed clips were switched to Decompress On Load for the measurement and switched back.
- Console clean. EditMode tests: 1724 passed, 0 failed.
- Play mode: the service played land / chime / invalid at the expected pitch, offset and volume, and Sound off stopped
  and blocked them; level 5 V1 (test visit) traced sound by sound: select on lift, deselect only on a real drop-back,
  invalid with the deselect, one land per ball 0.08 s apart rising with the slot, no sound on undo flights, chime with
  the last landing rising with the chain, cork 0.16–0.18 s later.
- Undos spent from the save were restored to 99.

## 2026-10-31 — Step 6c second art delivery: rims, corks, avatar menu, bottom bar, shop scrolling

All changes made through Unity MCP Editor commands.

### Assets
- Moved (`AssetDatabase.MoveAsset`, `.meta` kept) from `Assets/Art/Incoming/` (folder then trashed):
  `Tubes/Tube_RimBack.png`, `Tubes/Tube_RimFront.png`, `Tubes/Corks/Cork_{Plain,Red,Blue,Yellow,Green,Purple,Orange,
  Turquoise,Pink,Lilac,Brown}.png` (new folder `Tubes/Corks`), `UI/Buttons/Button_Pen.png`,
  `UI/Avatar/AvatarMenu_Panel.png`, `UI/Avatars/Avatar_01…05.png` (new folder `UI/Avatars`).
  `Tubes/Tube_Top.png` moved to `Assets/Art/Unused/`.
- **Ball Sort ▸ Art ▸ Prepare Sprites** run: rim halves cut to 180×100 and corks to 140×152 with pivots at the rim's
  top; panel and pen trimmed; import settings applied (corks max 256 ASTC 4×4, avatars max 512 ASTC 6×6 without alpha).
- `Assets/Prefabs/Tube.prefab`: `Top` renamed `RimBack` (TubeBack 0); new `RimFront` (copy of it, TubeFront 3);
  `Clay` TubeBack 1 → TubeFront 1; `Cap` order 1 → 2, draw mode Simple; `TubeView._rimBack` / `_rimFront` assigned.
- `Assets/Prefabs/Cover.prefab`: `Body` order 3 → 4, `Icon` 4 → 5 (above the rim front).
- `Assets/Data/Config/TubeVisualConfig.asset`: `_rimBackSprite` → `Tube_RimBack`, `_rimFrontSprite` → `Tube_RimFront`,
  `_capSprite` → `Cork_Plain`, `_capTint` white, `_clayScale` 1.1, `_veilHeightScale` 0.92 (hem at the bottom ball's
  centre on a capacity-4 tube).
- `Assets/Data/Config/ColorPalette.asset`: `_entries[0..9]._cork` → `Cork_<Colour>` (ids 10–11 left empty).
- New `Assets/Data/Config/AvatarConfig.asset` (`_pictures` = `Avatar_01…05`).
- `Assets/Prefabs/UI/SettingsPopup.prefab`: `RestartButton` and `HomeButton` → `Button_Red` (Sliced, ×2.34 pixels
  per unit), white tint, Are You Sure Confirm's colours, label and height (140).
- New `Assets/Prefabs/UI/AvatarMenuPopup.prefab`, built from `EditProfilePopup.prefab`: root dim + `Popup` (close
  button kept) + `AvatarMenuPopup`; `Panel` = `AvatarMenu_Panel` (820×1340, no layout group); `Header` (over the art's
  top bar) with `SelectedAvatar` (`AvatarFrame`: `Frame`, `Mask` circle → `Picture`), the old `NameField` (transparent,
  no transition, auto-size 26–46, placeholder off) over the art's name field and `PenButton`; `Grid`
  (`GridLayoutGroup` 3 columns, 180×185 cells) with `Slot1…9` (`AvatarFrame` + `Button`, `Highlight` gold
  `Ball_Circle` behind the frame); `CloseButton` last.
- Trashed `Assets/Prefabs/UI/EditProfilePopup.prefab` and `Assets/Scripts/Game/UI/EditProfilePopup.cs`.

### Scenes
- `Assets/Scenes/Home.unity`: `EditProfilePopup` instance replaced by an `AvatarMenuPopup` instance (same place in
  the draw order); `SafeArea/TopBar/AvatarButton` gained `AvatarFrame` and `Mask` (circle, 13–87 % × 14.4–88.4 %) →
  `Picture`; `BottomNav` moved out of `SafeArea` to right after it under `HomeCanvas`, `AspectRatioFitter` removed,
  `BottomBar` added (`_art` = its image, `_tapAreas` = new child `TapAreas` holding the three tab buttons);
  `HomePage` and `LeaderboardPage` gained `KeepAboveBar` (gap 8); `ShopPage/Scroll` gained `ScrollBehindBar`
  (content = `Viewport/Content`'s layout group); `HomeScreen._avatarMenu`, `_avatar`, `_avatars` assigned.

### Verification runs (no persistent changes)
- Console clean after every compile and after Play mode. EditMode tests: 1732 passed, 0 failed.
- Play mode, 1080×1920: real taps opened the avatar menu, picked avatar 3 (Home's avatar followed), an empty frame
  ignored its tap; a tap on the name did not start an edit, the pen did; ending the edit saved "Lab Rat" trimmed, and
  an empty name went back to it. Shop: a drag scrolled the content behind the bar; at the end the last bundle sits
  above it. Editor test visits: level 34 V1 (veils), level 20 V1 (clay, then two completions with their corks, then a
  flight from the top row across the board, the leading ball switching to inside the target's mouth at its clear
  height).
- The Editor save's avatar and name were restored (1, `Player`); lives and progress were not touched (test visits).

## 2026-10-31 — Avatar menu: selected frame, name edit fix

All changes made through Unity MCP Editor commands, except restoring the plain frame's original file.

### Assets
- `Assets/Art/Sprites/UI/Avatar/Avatar_Frame_Empty.png` overwritten with its untrimmed original (git `5884c0f`,
  `Art/Unused/Frame_Purple_Empty.png`; GUID kept) and moved to the new swap-set folder `UI/AvatarFrames/`, with
  `Assets/Art/Incoming/mainmenu_frame_selected.png` moved there as `Avatar_Frame_Selected.png` (`Incoming` trashed).
  Prepare Sprites cut both to 772×792.
- `Assets/Data/Config/AvatarConfig.asset`: `_frame` → `Avatar_Frame_Empty`, `_selectedFrame` → `Avatar_Frame_Selected`.
- `Assets/Prefabs/UI/AvatarMenuPopup.prefab`: every `AvatarFrame._frame` → its `Frame` image; the nine `Highlight`
  children removed; `NameField` gained a `CanvasGroup` (blocks raycasts off), left interactable;
  `AvatarMenuPopup._nameTaps` → it.

### Scenes
- `Assets/Scenes/Home.unity`: `SafeArea/TopBar/AvatarButton`'s `AvatarFrame._frame` → its own image.

### Verification runs
- Play mode (Home): editing ended by deselecting to nothing and by selecting another object, no error, the name saved
  trimmed; a tap on the read-only name reaches the panel behind it; picking avatar 4 showed the green frame on it only.
  EditMode tests: 1732 passed. Console clean.
- Stopping the user's Play session (it had lost `AppRoot` to a recompile) left a level-in-progress mark; clearing it
  through `PlayerData.LeaveAttempt` also spent a life and counted a retry, so the save was written back to its values
  before (lives 5, no timer, retries 3, no mark).

## 2026-10-31 — Buying, shop bundles, per-tube Shuffle, episode split

All changes made through Unity MCP Editor commands; Unity generated every new `.meta` file.

### Assets
- `Assets/Scripts/Core/Session/BoardShuffler.cs` and `PendingShuffle.cs` moved to the trash (`AssetDatabase.MoveAssetToTrash`).
- New placeholder sprites in `Assets/Art/Sprites/Placeholder/UI/` (generated, 128×128, imported like `Heart.png`: Sprite,
  single, 100 PPU, no mipmaps, alpha is transparency): `Plus_Green.png` (green disc, dark rim, white plus) and
  `Coin.png` (gold coin with a darker rim).
- `Assets/Data/Config/EconomyConfig.asset`: `_hardReward` 40 → 60, `_superHardReward` 60 → 100; new `_undoPrice` 300,
  `_shufflePrice` 300, `_addTubePrice` 900, `_boostersPerPurchase` 1, `_livesRefillPrice` 900, `_coinBundles` (1,000 /
  5,000 / 10,000 / 25,000 / 50,000 / 100,000 coins, "99,99 TL" … "4999,99 TL"). Saving also wrote `_maxLives: 5` and
  `_lifeRefillMinutes: 30` in place of the stale `_startLives: 5` (same values).
- `Assets/Data/Config/EpisodeConfig.asset`: first levels 1, 21, 41, 61, 81 → 1, 20, 40, 60, 80 (sprites unchanged).
- New `Assets/Prefabs/UI/BuyPopup.prefab`, a copy of `AreYouSurePopup.prefab`: `AreYouSurePopup` replaced by
  `BuyPopup` (popup, title, buy button and price wired); `LifeCost` removed; `ConfirmButton` renamed `BuyButton`, sprite
  `Button_Yellow`, its label replaced by `Content` (`HorizontalLayoutGroup`, centred, spacing 16) holding `CoinIcon`
  (96×96, `Coin.png`) and `Price` (160×110, TMP bold 60, `AmountLabel` 60 → 36); title "Buy Undo".

### Scenes
- `Assets/Scenes/Home.unity`:
  - `BuyPopup` prefab instance under `HomeCanvas`, just before `Toast`, inactive; `HomeScreen._buyPopup` → it.
  - `SafeArea/TopBar/Lives`: new `BuyButton` (56×56, `Plus_Green.png`, right-middle, x −4), inactive;
    `LivesDisplay._buyButton` → it. `Lives/Timer` right offset −26 → −38 to make room.
  - `SafeArea/TopBar/Coins`: new `BuyButton` (same); `HomeScreen._coinsBuyButton` → it. `Coins/Label` right offset
    −26 → −38.
  - `SafeArea/ShopPage`: `ShopPage` added, `_items` → six new children of `Scroll/Viewport/Content/Panel`,
    `Bundle1` … `Bundle6`, anchored on the panel art's frames (columns 2.6–32.6 / 35.2–65.2 / 67.8–97.9 %, rows
    46.9–88 / 2.8–43.9 %). Each: transparent `Image` + `Button` + `ShopItem`; `Picture` (`Coin.png`, preserve aspect),
    `Coins` (TMP bold, auto-size 28–52), `PriceTag` (`Button_Yellow`, sliced) → `Price` (TMP bold, auto-size 18–36).
    `Bundle1`–`Bundle3` (the static cards below the panel) are untouched.
  - `HomeScreen._shopPage` → `ShopPage`, `_shopTab` = 0 (the tab whose page is `ShopPage`).
- `Assets/Scenes/Level.unity`:
  - `BuyPopup` prefab instance under `LevelCanvas`, just before `Toast`, inactive; `LevelHud._buyPopup` → it.
  - `SafeArea/BoosterBar/{Undo,Shuffle,AddTube}Button`: new `BuyBadge` (60×60, `Plus_Green.png`, at `Badge`'s anchor
    and position, not a raycast target), inactive; `BoosterButton._countBadge` → `Badge`, `_buyBadge` → `BuyBadge`.
  - `SafeArea/ShufflingIndicator` removed (`LevelHud._shufflingIndicator` is gone).

### Verification runs (no persistent changes)
- EditMode tests: 1774 passed, 0 failed. Console clean after every compile and after Play mode.
- Play mode, Home (real UI taps): the lives plus shows only below the maximum; Buy hearts at 100 coins kept the popup
  open with "Not enough coins"; with 1,000 it refilled to 5 and closed; the coins plus opened the Shop tab; Bundle 3
  added 10,000 with "+10,000 coins".
- Play mode, Level 6 V1 (editor test visit): Undo at 0 showed the green plus, undimmed; Buy Undo at 100 coins stayed open
  with the toast, at 1,100 it bought one (badge back to "1", dimmed: nothing to undo). Shuffle entered the pick: the five
  mixed tubes glowed, the two empty ones were dimmed, the bar locked. Tube taps could not be driven from a command
  (queued Input System events never reached the game), so the pick-and-shuffle tap is on the manual checklist; three
  direct `TryShuffleTube` calls showed the tube view following the board each time.
- Found and fixed during the run: `AvatarMenuPopup` and `SettingToggle` subscribed to `PlayerData` at `Initialize`, and a
  popup never opened never runs `OnDestroy`, so after leaving Home any player-data change (spending or buying a booster)
  threw `MissingReferenceException`. They now subscribe in `OnEnable` and unsubscribe in `OnDisable`.
- The Editor save was backed up before Play mode and written back afterwards (level 20, 1,020 coins, 5 lives, Undo 0,
  Shuffle 99, Add tube 99).

## 2026-10-31 — Third art delivery: icons, popup panels, settings switches, tier button, leaderboard

All through Unity MCP Editor commands (prefabs via `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset`).

### Assets
- **Prepare Sprites** (extended with `SpriteRules.Deliveries`) moved the 24 files of `Assets/Art/Incoming/` with
  `AssetDatabase.MoveAsset` (GUIDs kept), trimmed and imported them: `UI/Buttons/Button_SuperHard.png`,
  `UI/Settings/Toggle_Off.png`, `Toggle_On.png`, `UI/Icons/` (`Icon_Coin`, `Icon_CoinPouch`, `Icon_CoinChest`,
  `Icon_CoinChestFull`, `Icon_CoinTank`, `Icon_CoinTankFull`, `Icon_Heart`, `Icon_HeartBroken`, `Icon_NoAds`,
  `Icon_Plus`), `UI/Leaderboard/` (`Leaderboard_Header`, `Leaderboard_Panel`, `Leaderboard_Row`, `Leaderboard_RowMe`),
  `UI/Popups/` (`Popup_Panel`, `Settings_Panel`), and to `Art/Unused/`: `Icon_Clock`, `Popup_Panel_Header_Cream`,
  `Popup_Panel_Header`, `Popup_Panel_Wide_Cream`, `Popup_Panel_Cream`. 9-slice borders: `Button_SuperHard`
  150/0/320/0, `Popup_Panel` 100/110/100/100, `Leaderboard_Panel` 0/130/0/470. The empty `Assets/Art/Incoming/`
  folders were moved to the trash.
- New generated placeholders `Art/Sprites/Placeholder/UI/Booster_Undo.png`, `Booster_Shuffle.png`, `Booster_AddTube.png`
  (256², import settings copied from `Plus_Green.png`, max 256).
- New `Art/Atlases/Icon_Coin_TMP.asset`: TMP sprite asset made from `Icon_Coin` (menu Create ▸ TextMeshPro ▸ Sprite
  Asset, moved here); its glyph metrics set to bearing x 0, bearing y 420, advance 540 so it follows the text.
- New `Data/Config/LeaderboardConfig.asset`: 10 bots (Bubbles 1, Captain Fizz 2, Luna Star 5, Pip Pepper 5, Mochi 5,
  Ziggy Zap 1, Sunny Sprout 4, Professor Plum 5, Nova 4, Tilly Tumble 2; avatar ids from `System.Random(2026)`).
- `Prefabs/UI/WinPopup`, `TutorialPopup`, `EpisodeUnlockedOverlay`, `AreYouSurePopup`, `MoreLivesPopup`, `BuyPopup`:
  `Panel` image → `Popup_Panel` (sliced, white, ppum 1), layout padding 80/80/80/90.
  - `WinPopup`: `Panel/Reward` sprite asset → `Icon_Coin_TMP`; `WinPopup._rewardFormat` → `+{0} <sprite name="Icon_Coin">`.
  - `AreYouSurePopup`: new `Panel/BrokenHeart` (`Icon_HeartBroken`, preserve aspect, preferred height 200) between
    `Title` and `LifeCost`; `AreYouSurePopup._lifeCostIcon` → it.
  - `MoreLivesPopup`: `Panel/Heart` → `Icon_Heart`, white, preferred height 240; `Heart/Count` offset y 10.
  - `BuyPopup`: new `Panel/Coins` (inactive; preferred height 96; `CoinsDisplay`) → `Pill` (250×96, `Display_Coins`
    sliced, ppum 3.958) → `Label` (TMP bold 38, `AmountLabel` 38–26, insets 96 / 22); new `Panel/Picture` (preserve
    aspect, preferred height 220); `BuyButton/Content/CoinIcon` → `Icon_Coin`; `BuyPopup._picture` / `_coins` → them.
- `Prefabs/UI/SettingsPopup`: `Panel` → `Settings_Panel` (simple, 820×1146), its `VerticalLayoutGroup`,
  `ContentSizeFitter` and the children's `LayoutElement`s removed; children anchored on the art: `Title` (header lip,
  auto-size 30–56), `SoundRow` / `MusicRow` / `HapticsRow` on the three bars (x 0.137–0.863), each `Label` left-aligned
  at x 0.20–0.64 (auto-size 30–52), each toggle on the track (x 0.709–1.0): transparent target graphic, `graphic` none,
  transition none, `Checkmark` renamed `Knob` (94×96, left-middle anchor), new `SettingSwitch` (off x 52, on x 121,
  `Toggle_Off` / `Toggle_On`), `SettingToggle._switch` → it; `RestartButton` / `HomeButton` side by side on the bottom
  strip (x 0.15–0.49 / 0.51–0.85, y 0.108–0.203, ppum 2.7, labels auto-size 28–46); `CloseButton` at (−10, −10).

### Scenes
- `Assets/Scenes/Home.unity`:
  - `SafeArea/HomePage/LevelButton`: `TierBadge` removed; new first child `TierArt` (inactive; `Button_SuperHard`
    sliced, ppum 1.861, offsets −1.07/−1.07 to +30.6/+26.9 so its pill covers the button) → `Multiplier` (TMP bold
    auto 20–40, white, 140×46 at (−80, −60) from the top right, rotated −25°). `LevelTierLook`: `_tierArt`,
    `_multiplierLabel`, `_hardSprite` and `_superHardSprite` (both `Button_SuperHard`).
  - `SafeArea/TopBar/Lives/BuyButton`, `Coins/BuyButton` → `Icon_Plus`.
  - `ShopPage/.../Panel/Bundle1–6/Picture` → `Icon_Coin`, `Icon_CoinPouch`, `Icon_CoinChest`, `Icon_CoinChestFull`,
    `Icon_CoinTank`, `Icon_CoinTankFull`; their `PriceTag/Price` black, auto-size 12–36, margins 14/4/14/6.
  - `ShopPage/.../Content/Bundle1–3` (cards): `Name` at x 0.67–0.975 (auto-size 24–46); new `Coin` (x 0.465–0.565,
    `Icon_Coin` / `Icon_CoinChest` / `Icon_CoinTank`) and `NoAds` (x 0.57–0.66, `Icon_NoAds`); `Price` black, auto-size
    20–56.
  - `SafeArea/LeaderboardPage`: placeholder `Panel` removed; `Scroll` (copy of the Shop's scroll view, with
    `ScrollBehindBar`) → `Viewport/Content` → `Header` (920×513, `Leaderboard_Header`, `Title` "Leaderboard") and
    `Panel` (`Leaderboard_Panel` sliced, ppum 0.696, width 920, `VerticalLayoutGroup` padding 86/86/676/173, spacing 26)
    holding `Podium1–3` (masked avatar pictures, 132², ignore layout, on the tube discs) and the inactive `RowTemplate`
    (748×182, `LeaderboardRow`: `Background`, `Rank`, masked `Avatar` 104², `Name`). New `LeaderboardPage` on the page
    (`LeaderboardConfig`, the row template, both row sprites, the three podium pictures).
  - `HomeScreen`: `_leaderboardPage`, `_heartPicture` (`Icon_Heart`); `_heartsName` replaced by `_refillHeartsTitle`
    ("Refill hearts").
- `Assets/Scenes/Level.unity`: `SafeArea/BoosterBar/{Undo,Shuffle,AddTube}Button/BuyBadge` → `Icon_Plus`; `BoosterBar`
  `_undoPicture` / `_shufflePicture` / `_addTubePicture` → the three placeholders; `StuckPanel._playOnFormat` →
  `Play on — {0} <sprite name="Icon_Coin">`, its label's sprite asset → `Icon_Coin_TMP`.

### Verification runs (no persistent changes)
- EditMode tests: 1774 passed, 0 failed. Console clean after every compile and after Play mode.
- Play mode, Home: each tier on the Level button (x3 / x5 on the tag); Shop and Leaderboard scrolled top and bottom;
  real taps on Settings and on each switch (knob slid from 121 to 52 and swapped sprites halfway); the lives plus
  opened "Refill hearts" with the heart; More Lives.
- Play mode, Level 20: Undo at 0 opened "Buy Undo" with the balance (10,720) and the placeholder picture; buying closed
  it and spent 300 (the popup's balance read 10,420); Settings → Restart opened Are You Sure with the broken heart;
  the Win reward and Play on show the coin inline.
- The Editor save was backed up before Play mode and written back afterwards.

## 2026-10-31 — Fonts, text changes and text fit (stage 1)

### Fonts
- Created `Assets/Art/Fonts/`; copied `Reference/Fonts/Baloo2-ExtraBold.ttf` and `Baloo2-Bold.ttf` into it (default
  font import settings).
- Created `Assets/Art/Fonts/Baloo2-ExtraBold SDF.asset` and `Baloo2-Bold SDF.asset` (`TMP_FontAsset.CreateFontAsset`,
  then saved with their atlas texture and material as sub-assets and switched to **Static**): SDFAA, 1024 × 1024,
  sampling 80, padding 11, 208 glyphs (character list in docs/art-inventory.md, section 9, and in each asset's creation
  settings, custom characters). Shader `TextMeshPro/Mobile/Distance Field`.
- `Assets/TextMesh Pro/Resources/TMP Settings.asset`: default font asset → `Baloo2-ExtraBold SDF` (Unity also rewrote
  `m_ActiveFontFeatures` as `kern` and unwrapped two long lines; same values).
- Every `TMP_Text` in `Home.unity`, `Level.unity` and `Prefabs/UI/*.prefab` → `Baloo2-ExtraBold SDF` and its material,
  except `Toast.prefab`'s `Label` → `Baloo2-Bold SDF`. Scene instances of the popup prefabs inherit the prefab's font
  (any font override reverted). `AvatarMenuPopup` `NameField` (`TMP_InputField`) global font asset → ExtraBold.
- `Assets/Resources/AppRoot.prefab`: added `SystemFontFallback` (default family list, style `Regular`) and assigned it to
  `AppRoot._fontFallback`.

### Text changes (docs/design/TEXT_CHANGES.md, section 2)
- `AreYouSurePopup.prefab`: `LifeCost` "You will lose 1 life!"; `ConfirmButton/Label` "Quit"; `AreYouSurePopup`
  `_confirmLabel` → that label, `_quitText` "Quit", `_restartText` "Restart".
- `MoreLivesPopup.prefab`: `_timerFormat` "Time to next life\n<size=140%>{0}</size>", `_fullText` "Your lives are
  full!", `Timer` placeholder text to match, `Timer` `LayoutElement` preferred height 90 → 170.
- `EpisodeUnlockedOverlay.prefab`, `TutorialPopup.prefab`: `Hint` "Tap to Continue".
- `Level.unity` `StuckPanel._playOnFormat` → `Play on <sprite name="Icon_Coin"> {0}` (label placeholder text to match).
  `_bannerText` kept ("No more useful moves").
- `Assets/Data/Config/TutorialConfig.asset`: the six texts → product's lines (level 1 one line; 7, 20, 30, 55, 80 main
  line with the key word in `<color=#FFD23A>`, then the hint in `<size=75%>`; non-breaking spaces keep each key word
  with the word before it).

### Text fit (auto-size min–max, wrap off unless noted)
- Prefabs: `AreYouSurePopup` Title 40–64, LifeCost 32–52, ConfirmButton/Label 32–56; `BuyPopup` Title 40–64;
  `MoreLivesPopup` Title 40–64, Heart/Count 48–88, Timer 28–52; `WinPopup` Title 40–64, Reward 48–80,
  ContinueButton/Label 32–56; `TutorialPopup` Hint 24–36; `EpisodeUnlockedOverlay` Title 40–64, Hint 24–36; `Toast`
  Label 28–46 (wraps); `AvatarMenuPopup` name field Text and Placeholder 22–46.
- `Home.unity`: `TopBar/Lives/Timer` 24–40, `Lives/Heart/Count` 28–46, `HomePage/LevelButton/Label` 36–80 (no wrap),
  Shop header `Title` 60–110.
- `Level.unity`: booster `Label`s 22–34, booster `Badge/Count` 14–26, `StuckPanel` `GiveUpButton/Label` and
  `PlayOnButton/Label` 28–44, banner `Label` minimum 30 → 24, `Celebration` 60–120.
- Side margins (TMP margin left/right): 6 on the popup `Title`s (Are You Sure, Buy, More Lives, Win, Episode
  Unlocked), the Level HUD `LevelTitle` and the Leaderboard header `Title`; 4 on the Shop card `Name`s and the stuck
  banner.
- Button labels raised off the art's bottom lip (rect `offsetMin.y` = 10 % of the button height): Home
  `LevelButton/Label` 23; `AreYouSurePopup` ConfirmButton/Label 14, `WinPopup` ContinueButton/Label 14, `BuyPopup`
  BuyButton/Content 14; `SettingsPopup` RestartButton/Label and HomeButton/Label 11; Level `GiveUpButton/Label` and
  `PlayOnButton/Label` 13.
- `AvatarMenuPopup` `NameField/Text Area` right offset −12 → −29: the pen button covers the field's last 17 units, so a
  long name ran under it.

### Verification runs (no persistent changes)
- Fit check in Play mode at 1080×1920, 1080×2400, 1080×2520 and 1536×2048 (`PlayModeWindow.SetCustomRenderingResolution`),
  Home (main, both tiers, Shop, Leaderboard, Settings, avatar menu, More Lives, Refill hearts, Episode Unlocked, toast)
  and Level (editor test visit to level 7: all six tutorial texts, HUD, buy badges, Stuck, Settings, Are You Sure ×3,
  More Lives, Buy booster, Win, celebration, toast), with the longest values. After the fixes nothing leaves its rect,
  container or the screen. The only remaining flag, the editor-only Win title "Level 100 · V5 complete" overhanging by
  2 units per side, is fixed by the title margins added afterwards.
- EditMode tests: 1774 passed, 0 failed. Console: no errors or warnings.
- The Game view was put back to 1080×1920 afterwards. Test visits spend nothing (no lives, coins or boosters).

## 2026-11-01 — Text look: outline and shadow (stage 2)

### Font atlas
- `Assets/Art/Fonts/Baloo2-ExtraBold SDF.asset` rebuilt in place (same GUID, references unchanged): sampling 80 → 64 pt,
  padding 11 → 15 (0.23 em, room for the outline plus the shadow), same 208 characters, still 1024² and static;
  material `_GradientScale` 16. `Baloo2-Bold SDF` unchanged (the toast stays plain).

### Material presets (new, `Assets/Art/Fonts/`)
- `Baloo2-ExtraBold SDF Title.mat`, `Body.mat`, `Green.mat`, `Red.mat`, `Yellow.mat`: copies of the font material
  (TextMeshPro/Mobile/Distance Field) with `OUTLINE_ON` and `UNDERLAY_ON`; face dilate = outline width (0.19, Body
  0.12), underlay dilate = 2 × width, underlay offset Y −0.40 (Body −0.25), softness 0; outline and underlay colour
  `#24106E` (Title, Body), `#1F5C14` (Green), `#7A0C22` (Red), `#8A4A0A` (Yellow).

### Labels
- Prefabs: `AreYouSurePopup` Title → Title, LifeCost → Body, ConfirmButton/Label → Red; `BuyPopup` Title → Title,
  Coins/Pill/Label → Green, BuyButton/Content/Price → Yellow; `MoreLivesPopup` Title → Title, Heart/Count → Red, Timer →
  Body; `WinPopup` Title and Reward → Title, ContinueButton/Label → Green; `TutorialPopup` Text and Hint → Body;
  `EpisodeUnlockedOverlay` Title → Title, Hint → Body; `SettingsPopup` Title → Title, Sound/Music/Haptics labels → Body,
  Restart/Home labels → Red. Scene instances inherit (material and colour overrides reverted).
- `Home.unity`: Lives/Timer and Coins/Label → Green, Lives/Heart/Count → Red, LevelButton/Label → Green,
  TierArt/Multiplier → Red, Shop and Leaderboard header Titles → Title (fill `#2E334C` → white), leaderboard
  RowTemplate Rank/Name, Shop Panel/Bundle1–6/Coins and card Bundle1–3/Name → Body. `LevelTierLook`: `_label` →
  LevelButton/Label, `_regularLabelMaterial` → Green, `_tierLabelMaterial` → Red. Side margins: Lives/Timer 5,
  Leaderboard header Title 6 → 12.
- `Level.unity`: LevelTitle → Title (side margins 6 → 10), booster Labels → Body, booster Badge/Count → Green (side
  margins 4), StuckPanel Banner/Label → Title (keeps its fill), GiveUp → Red, PlayOn → Yellow (fill `#5C3300` → white),
  Celebration → Title, fill white with vertex gradient `#FFF6C2` → `#FF9500`.

### Verification runs (no persistent changes)
- Fit check counting each material's outline and shadow, at 1080×1920, 1080×2400, 1080×2520 and 1536×2048, Home and
  Level (editor test visit to level 7), all screens and popups with the longest values: nothing leaves its button,
  panel, field or the screen. The one remaining flag, the Buy price "900" whose outline reaches 2 units left of its
  left-aligned rect, is still well inside the button.
- EditMode tests: 1774 passed, 0 failed. Console: no errors or warnings. Game view put back to 1080×1920.

## 2026-11-01 — Every label to Baloo 2 Bold, lighter outline

- `Assets/Art/Fonts/Baloo2-Bold SDF.asset` rebuilt in place (same GUID): sampling 80 → 64 pt, padding 11 → 15, same
  208 characters, 1024², static; material `_GradientScale` 16.
- Material presets renamed (`AssetDatabase.MoveAsset`, GUIDs kept) `Baloo2-ExtraBold SDF {Title,Body,Green,Red,Yellow}.mat`
  → `Baloo2-Bold SDF {…}.mat`, pointed at the Bold atlas (`_MainTex`, texture size, gradient scale 16), and lightened:
  Title / Green / Red / Yellow face dilate 0.19 → 0.06, outline 0.19 → 0.22, underlay dilate 0.38 → 0.30 (offset
  −0.40 kept); Body dilate 0.12 → 0.03, outline 0.12 → 0.16, underlay dilate 0.24 → 0.20 (offset −0.25 kept).
- Every `TMP_Text` on `Baloo2-ExtraBold SDF` in `Home.unity`, `Level.unity` and `Prefabs/UI/*.prefab` → `Baloo2-Bold SDF`
  (presets kept; labels on the plain ExtraBold material → the Bold material): 29 in Home, 12 in Level, 29 in the
  prefabs. `AvatarMenuPopup` name field's global font → Bold. TMP Settings default font → `Baloo2-Bold SDF`.
  `Baloo2-ExtraBold SDF` and its `.ttf` are now unreferenced (left in place).
- Verified: fit check (outline and shadow counted) and screenshots of every screen and popup at 1080×1920 and
  1080×2400, Home and Level (editor test visit to level 7): nothing leaves its button, panel, field or the screen
  (Bold is narrower than ExtraBold and the effects reach less far, so the 9:21 and 3:4 results only improve).
  EditMode tests 1774/1774, console clean.

## 2026-11-01 — Second animation batch, stage 1: button, booster and purchase sounds, haptic calls

### Prefabs
- `Assets/Resources/AppRoot.prefab`: added `HapticsService` (`_minLandingInterval` 0.05, `_logInEditor` off);
  `AppRoot._haptics` → it.

### Level.unity
- `LevelCanvas`: added `UiButtonFeedback`; `_ownFeedback` = `AreYouSurePopup/Panel/ConfirmButton`,
  `BuyPopup/Panel/BuyButton`; `_noHaptic` empty. `LevelHud._buttonFeedback` → it.
- `SafeArea/BoosterBar/{Undo,Shuffle,AddTube}Button/Badge`: added `UiPunch` (`_animation` → `Data/Config/AnimationConfig`,
  `_target` → the badge); each `BoosterButton._purchasePunch` → its badge's `UiPunch`.

### Home.unity
- `HomeCanvas`: added `UiButtonFeedback`; `_ownFeedback` = `SafeArea/HomePage/LevelButton`, `BuyPopup/Panel/BuyButton`;
  `_noHaptic` = `AvatarMenuPopup/Panel/Header/PenButton`. `HomeScreen._buttonFeedback` → it.
- `SafeArea/TopBar/Lives/Heart` and `SafeArea/TopBar/Coins`: added `UiPunch` (`_animation` → `AnimationConfig`, `_target`
  → itself); `HomeScreen._livesPunch` / `_coinsPunch` → them.

### Assets
- `Data/Config/AnimationConfig.asset`: new fields keep their code defaults (`_purchasePunchScale` 0.25,
  `_purchasePunchDuration` 0.35, `_purchasePunchVibrato` 6); not edited.

### Verification
- Play mode (Level, real taps): Settings, the Music toggle twice (setting unchanged) and the settings ✕ each play
  `sfx_ui_button`; Settings and the toggles log a Light haptic (with `_logInEditor` set on the runtime instance only),
  the ✕ none. The level-in-progress mark this run set in the Editor save (level 24) was cleared afterwards.
- EditMode tests: 1774 passed, 0 failed. Console: no errors or warnings.

## 2026-11-01 — Second animation batch, stage 2: stuck bar timing

### Level.unity
- `LevelCanvas/SafeArea/StuckPanel`: added a `CanvasGroup` (alpha 1, blocks raycasts: the root has no graphic, so taps
  between the banner and the buttons still reach the board); `StuckPanel._group` → it, `_animation` →
  `Data/Config/AnimationConfig`.

### Assets
- New test level `Assets/Data/TestLevels/stuck_after_move.json` (id 902, the same board in all five variants): not stuck at
  the start; moving tube 3's top ball onto tube 4 leaves it stuck (found by a search in the Editor and checked with
  `LevelSession.IsStuck`). Play it with **Ball Sort ▸ Play Level…** to see the stuck bar. Unity created its `.meta`.
- `Data/Config/AnimationConfig.asset`: new fields keep their code defaults (`_stuckDelay` 0.4, `_stuckFadeDuration`
  0.25); not edited.

### Verification
- Play mode (editor test visit to `stuck_after_move`, stepped frame by frame): after the move the state was Stuck at
  once and the booster bar locked; the board settled at 0.46 s, the stuck bar appeared 0.42 s later (one frame past
  0.4) and finished fading in 0.26 s after that; `sfx_move_invalid` played with it.
- EditMode tests: 1774 passed, 0 failed. Console: no errors; one warning from the verification probe itself (reading
  `AudioSource.time` on an empty source), none from the game. Level.unity's dirty flag after Play held only
  layout-driven anchors; the scene was reloaded from disk, not saved.

## 2026-11-01 — Second animation batch, stage 3: blinking

### Assets
- `Data/Config/AnimationConfig.asset`: new fields keep their code defaults (`_blink` on, `_boardBlinkIntervalMin` 2,
  `_boardBlinkIntervalMax` 5, `_blinkCloseDuration` 0.08, `_blinkClosedDuration` 0.16, `_blinkOpenDuration` 0.08); not
  edited. No scene or prefab changes.

### Verification
- Play mode (editor test visit to `stuck_after_move`, stepped at 0.02 s): each blink went Open → HalfOpen (0.10 s) →
  Closed (0.18 s) → HalfOpen (0.10 s) → Open (the configured times rounded up to the step). A first version gave every
  ball its own 2–7 s clock, far too busy on a full board; it now blinks one ball every 2–5 s board-wide (a
  `BlinkScheduler` driven from a command: 5 blinks in 10 s), confirmed by hand in Play mode. An invalid tap on tube 1:
  its balls held the shock face (the closed sprite until shock art exists) for 0.7 s with no blink frame, then went
  back to their open faces and blinked on.
- EditMode tests: 1774 passed, 0 failed. Console: no errors or warnings. Level.unity's dirty flag after Play was
  layout-only; reloaded from disk, not saved.

## 2026-11-01 — Second animation batch, stage 4: ice break

### Assets
- New placeholder sprite `Assets/Art/Sprites/Placeholder/Vfx_IceShard.png` (generated, 32×48, white splinter with a
  darker facet, soft edge; Sprite, single, 100 PPU, no mipmaps, clamp, alpha is transparency).
- New prefab `Assets/Prefabs/VFX/Vfx_IceBreak.prefab`, a copy of `Vfx_Droplets` (`AssetDatabase.CopyAsset`), retuned:
  root system = shards (burst 22, lifetime 0.8–1.1, speed 2–4.5, size 0.12–0.24, random start rotation, rotation
  over lifetime ±6, gravity 2.5, cone 50° radius 0.35, start colour random between `#D1F2FF` and `#8CD1FF`, fade out
  over the last 30 %, size over lifetime off, texture sheet → `Vfx_IceShard`); new child `Puff` (8 particles,
  lifetime 0.45–0.6, speed 0.3–0.8, size 0.45–0.75 growing ×0.7 → ×1.4, white at 55 % fading out, circle radius 0.3,
  local simulation, hierarchy scaling, texture sheet → `Ball_Circle`, same material and sorting layer, order −1).
  `VfxBurst`: `_root` → shards, `_tinted` / `_mixed` empty, `_chainScaled` none (colours stay the prefab's).
- `Data/Config/AnimationConfig.asset`: new fields keep their code defaults (`_iceCrackTime` 0.16, `_iceShiverAmount`
  0.04, `_iceShiverFrequency` 70, `_iceCrackBrighten` 0.5); not edited.

### Level.unity
- `LevelFlow/LevelFeedback._iceBreak` → `Vfx_IceBreak`; `_iceRingColour` keeps its default (`#BFEBFF`).

### Verification
- Play mode (editor test visit to `clay_large_tube` V1): four moves filled the clay tube. The ice shivered (up to
  0.035 board units) and was gone 0.80 s after the last move's tap (its flight, then 0.16 s); `sfx_ice_break` and
  `sfx_tube_cork` both played, `Vfx_IceBreak` and a second ring were playing.
- EditMode tests: 1774 passed, 0 failed. Console: no errors; one expected warning from the test level itself
  (`clay_large_tube` is marked as a tutorial level without a `TutorialConfig` entry). Level.unity's dirty flag after
  Play was reloaded from disk (the scene was saved before Play).

## 2026-11-01 — Second animation batch, stage 5: single-tube shuffle

- No scene, prefab or asset edits. `Data/Config/AnimationConfig.asset`: new fields keep their code defaults
  (`_shuffleSpeed` 1.7, `_shuffleStagger` 0.05, `_shuffleRiseHeight` 0.4, `_shuffleArcHeight` 0.3, `_shuffleWiggleAngle`
  8, `_shuffleWiggleDuration` 0.4, `_shuffleWiggleFrequency` 30, `_shuffleWiggleDamping` 8).
- Verification: the animation checked by hand in Play mode; EditMode tests 1775 passed, 0 failed; console: no errors.

## 2026-11-01 — Fourth art delivery: reworked balls, lock box and keys, booster and hard buttons, win picture

### Assets (Ball Sort ▸ Art ▸ Prepare Sprites)
- 41 replaced in place (pixels copied over, `.meta`/GUID kept; import settings re-applied, pivot now (128, 139) of
  256): `Art/Sprites/Balls/Ball_<Colour>_{Open,HalfOpen,Closed,Laugh}.png` (10 colours) and `Ball_Hidden.png`.
- 19 new: `Art/Sprites/UI/Buttons/Button_Hard.png`; `Art/Sprites/UI/Boosters/Booster_{Undo,Shuffle,AddTube}.png`;
  `Art/Sprites/UI/Win/Win_Picture.png`; `Art/Sprites/Mechanics/LockBox/LockBox_{Side,Face,Inside,Cover}.png` and
  `LockBox_Mid_{Blue,Green,Pink,Purple,Yellow}.png`; `Art/Sprites/Mechanics/Keys/Key_{Blue,Green,Pink,Purple,Yellow}.png`.
- 16 to `Art/Unused/`: `Key_Red`, `LockBox_Mid_Plain`, `LockBox_Mid_Inside`, `LockBox_Side_Inside`, `LockBox_Single`,
  `LockBox_Single_{Blue,Green,Pink,Purple,Yellow}`, `LockBox_Double`, `LockBox_Double_Inside`,
  `Tube_{Full,Middle,RimBack,RimFront}_Redraw`.
- `Assets/Art/Incoming/` (emptied) removed by the tool.

### Config assets
- `Data/Config/TubeVisualConfig.asset`: `_lockSideSprite` → `LockBox_Side`, `_lockFaceSprite` → `LockBox_Face`,
  `_lockInsideSprite` → `LockBox_Inside`, `_lockCoverSprite` → `LockBox_Cover`; new fields at their code defaults:
  `_ballScale` 0.9, `_ballSpacing` 0.855, `_veilStickerSize` 0.55, `_lockBoxPadding` (0.08, 0.1). Removed fields
  (`_veilBadgeSprite`/`Size`, `_doorSprite`/`Tint`, `_lockIconSprite`/`Size`) dropped on save. Tube geometry unchanged.
- `Data/Config/ColorPalette.asset`: `_artSpriteSize` 1.4628571 (replaces the per-entry `_spriteSize` and
  `_hiddenSpriteSize`, dropped on save); `_keyMarkSize` 0.75 → 0.5, `_keyMarkOffset` (0.25, −0.25) → (0, −0.3);
  `_keyMarkSprite` removed.
- `Data/Config/LockPalette.asset`: `_tints` replaced by `_locks` (5): Green, Purple, Yellow, Pink, Blue, each with its
  `LockBox_Mid_<Colour>` and `Key_<Colour>`.

### Prefabs
- `Prefabs/Cover.prefab`: new child `LockBox` (`LockBoxView`) with SpriteRenderers `SideLeft`, `SideRight`, `FaceLeft`,
  `FaceRight` (TubeFront 4), `Mid` (TubeFront 5), `Inside` (TubeBack −2), `CoverLeft`, `CoverRight` (TubeFront 6), all
  Sprite-Unlit-Default, disabled; `CoverView._lockBox` → it.
- `Prefabs/UI/WinPopup.prefab`: `Panel` Image disabled (no panel art), its layout padding 60/60/0/0, spacing 24; new
  child `Picture` (Image `Win_Picture`, preserve aspect, no raycast, LayoutElement preferred height 700) between `Title`
  and `Reward`; `Title` font size 80 (max 80), preferred height 120.

### Level.unity
- `LevelCanvas/.../BoosterBar/{Undo,Shuffle,AddTube}Button`: Image → `Booster_Undo` / `Booster_Shuffle` /
  `Booster_AddTube`, white, Simple, preserve aspect, size 150×152; `Label` children deactivated.
- `BoosterBar._undoPicture` / `_shufflePicture` / `_addTubePicture` → the same three sprites.

### Home.unity
- `LevelTierLook._hardSprite` → `Button_Hard` (was `Button_SuperHard`).

### Verification
- EditMode tests: 1776 passed, 0 failed. Validate Levels (runner, fast solve): 100 levels, 500 variants, 0 invalid,
  0 unsolved. Console: no errors or warnings.
- Screenshots in Play mode (editor test visits, save restored afterwards): level 84 (two 3-tube lock boxes, keys),
  31 (veils with stickers), 9 (hidden balls), the booster bar, the Buy popup (Shuffle), the Win panel, Home on a hard
  level (14).

## 2026-11-01 — Review fixes: red key, tube bottom over balls, level 100 tier

- `Art/Unused/Key_Red.png` → `Art/Sprites/Mechanics/Keys/Key_Red.png` (`AssetDatabase.MoveAsset`), cut to the keys'
  shared 152×144 rectangle (same offset as the others, checked against `Key_Blue`'s alpha); Prepare Sprites applied the
  Keys import settings. Not referenced (no red lock). `SpriteRules.Deliveries` now sends it to `Keys`.
- `Prefabs/Tube.prefab`: `Bottom` SpriteRenderer sorting layer TubeBack 0 → TubeFront 0 (over resting balls, under
  clay, cork and rim front; raised balls stay above it).
- `Data/Levels/level_100.json`: `tier` `superHard` → `regular` (text edit).

## 2026-11-01 — Animations stage 1: lock box opening (docs/animation-spec.md 3.3)

- New `Art/Sprites/Placeholder/Mask_Square.png` (generated: 100×100 white, Sprite, PPU 100, centre pivot, full rect, no
  mipmaps): the shape of the lock box's `SpriteMask`.
- `Prefabs/Cover.prefab`, under `LockBox`:
  - new child `Mask` (`SpriteMask`, sprite `Mask_Square`, disabled; `LockBoxView` sizes it to the box and enables it
    while the box opens) → `LockBoxView._mask`;
  - new child `Key` (SpriteRenderer, Sprite-Unlit-Default, TubeFront 8, disabled) → `LockBoxView._key`;
  - `CoverLeft`, `CoverRight`: Mask Interaction → Visible Inside Mask;
  - `SideLeft`, `SideRight`: sorting order 4 → 7 (over the sliding covers).
- `AnimationConfig.asset`, `TubeVisualConfig.asset`: new fields only (code defaults; nothing edited).

### Verification
- EditMode tests: 1776 passed, 0 failed. Console: no errors; one warning from the test fixture (level 905 has no
  tutorial text).
- Play mode, editor test visit of `TestLevels/lock_and_key.json` V1 (move tube 0 → 2 frees the key): screenshots of
  the key's flight, the seat and wobble, the covers sliding, the fade and the open box; a one-tube box checked by
  refitting the box to one tube's width (covers start clipped, end clipped at the edges).

## 2026-11-01 — Animations stage 2: veil lift and hidden reveal (docs/animation-spec.md 3.1, 3.2)

- New `Prefabs/VFX/Vfx_RevealPuff.prefab`: `Vfx_IceBreak`'s `Puff` system on its own with a `VfxBurst` (`_root` = it):
  10 particles, lifetime 0.45–0.6, speed 0.3–0.7, size 0.3–0.55, shape radius 0.25, start colour lilac
  (0.78, 0.64, 1, 0.6); renderer sorting layer Balls −1 (behind the balls).
- New `Prefabs/VFX/Vfx_TowelPuff.prefab`: the same puff, 12 particles, lifetime 0.5–0.75, speed 0.3–0.9, size
  0.5–0.85, radius 0.7, pink (1, 0.64, 0.86, 0.45); VFX −1.
- New `Prefabs/VFX/Vfx_TowelSparks.prefab`: copy of `Vfx_TubeSparkles`, burst 10, start colour gold (1, 0.82, 0.3);
  `VfxBurst._mixed` emptied and `_chainScaled` cleared (no colour mix, no chain growth).
- `Scenes/Level.unity`, `LevelFlow` object's `LevelFeedback`: `_revealPuff`, `_towelPuff`, `_towelSparks` → the three
  prefabs.
- `AnimationConfig.asset`: new fields only (code defaults).

### Verification
- EditMode tests: 1776 passed, 0 failed. Console: no errors; the test fixture's tutorial warning (level 904).
- Play mode, editor test visit of `TestLevels/veil.json` V5: moves 0 → 2, 1 → 0 (red completes, the veil lifts:
  screenshots of the start with smoke and sparks, mid-lift with the tilt, the end), then 1 → 4, 1 → 2 (the hidden ball
  turns: screenshots mid-flip and after).

## 2026-11-01 — Animations stage 3: level win (docs/animation-spec.md 5.2)

- `Prefabs/UI/WinPopup.prefab`, root: new `WinPanelAnimation` (`_animation` → `AnimationConfig`, `_dim` → the root's
  Image, `_picture` → `Panel/Picture`, `_title` → `Panel/Title`, `_reward` → `Panel/Reward`, `_continue` →
  `Panel/ContinueButton`); `WinPopup._entrance` → it.
- New `Prefabs/VFX/Vfx_WinConfetti.prefab`: copy of `Vfx_Confetti`, burst 40, start speed 7–11.
- `Scenes/Level.unity`: `LevelCanvas/Celebration` (the "Well done!" placeholder label) deleted; `LevelFeedback`:
  `_celebration` gone (field removed), `_winConfetti` → `Vfx_WinConfetti`, `_winConfettiInset` (0.04, 0.04),
  `_winConfettiAngle` 30 (defaults).
- `AnimationConfig.asset`: `_celebrationDuration` removed, the win fields added (code defaults).

### Verification
- EditMode tests: 1776 passed, 0 failed. Console: no errors; the test fixture's tutorial warning (level 905).
- Play mode, editor test visit of `TestLevels/lock_and_key.json` V1 solved by the solver: screenshots (UI included) of
  the board celebration and corner confetti, the dim and picture pop, the title's letters, the panel at rest with
  Continue. Skip checked by calling `WinPanelAnimation.Skip` three times: Card, then Reward raised once each at the
  skip, the stamp dropped, Continue live at the end.

## 2026-11-01 — Shop: coin packs in TL and clickable

- `Scenes/Home.unity`, `ShopPage/Scroll/Viewport/Content/Bundle1–3` (the pack cards): Image raycast target on; new
  `Button` (Color Tint on the card's Image, the panel bundles' colours, navigation None) and `ShopItem` (`_button`,
  `_priceLabel` → `Price`, no coins label); `Price` text 259,99 TL / 899,99 TL / 3099,99 TL (was $4.99 / $9.99 /
  $19.99; the text comes from `EconomyConfig` at runtime), anchors (0.63, 0.227)–(0.947, 0.52) = the yellow button's
  fill, margins 18/2/18/2, auto-size 20–56 unchanged. `ShopPage._packs` → the three cards.
- `ShopPage/.../Panel/Bundle1–6/PriceTag/Price`: margins 14/4/14/6 → 26/6/26/8, so "4999,99 TL" stays inside the tag's
  yellow.
- `EconomyConfig.asset`: new `_coinPacks` (code defaults: 1,000 / 10,000 / 50,000 coins).

### Verification
- Console clean. Play mode on Home: Shop tab screenshots (panel and packs); real taps on the three cards granted
  1,000, 10,000 and 50,000 coins (the 61,000 taken back from the Editor save afterwards).

## 2026-11-01 — Win fireworks on the fanfare's beats (docs/animation-spec.md 5.2)

- New script `Scripts/Game/UI/FireworksView.cs` (uGUI mesh, the prototype's fireworks); `SfxClip` gets `_beats`,
  `AnimationConfig` gets `_fireworks` (default on), `LevelFeedback` gets `_fireworks` and starts it with the fanfare.
- `Prefabs/UI/WinPopup.prefab`: new child `Fireworks` (first child: above the root's dim, under `Panel`), stretched
  to the popup (anchors 0–1, offsets 0), `FireworksView` with raycast target off and code defaults.
- `Scenes/Level.unity`: `LevelFeedback._fireworks` → `LevelCanvas/.../WinPopup/Fireworks`.
- `SfxConfig.asset`: `_winFanfare._beats` = 1.75, 1.95, 2.09, 2.61, 2.75, 2.89 (the prototype's marks for this clip,
  same file as `ms_win_fanfare`); every other slot `_beats` empty.

### Verification
- Console: no errors after compiling. Play mode, `WinPopup.Show("12", 20)` called directly: screenshots in
  `Screenshots/fireworks/` (taken with the Editor in the background, about 8 fps): bursts high on the screen over the
  dim and behind the picture, title and reward, the first big one at ~1.7 s, the last sparks gone by ~4.5 s.
  The fanfare's sync was not checked by ear.

## 2026-11-01 — Episode Unlocked: fanfare, jingle and fireworks (docs/screens.md, Episodes)

- Code: `EpisodeUnlockedOverlay` gets `_fireworks` and `Show(…, fireworkBeats)`; `HomeFlow` plays the fanfare, a Win
  haptic and the fireworks as the overlay opens, then the reward jingle and a Success haptic after
  `_episodeJingleDelay` 0.6 s (skipped if the overlay was closed); `SfxClip.HeardBeats()` (the beats minus the start
  offset) is shared with `LevelFeedback`.
- `Prefabs/UI/EpisodeUnlockedOverlay.prefab`: new child `Fireworks`, last child (drawn over `Panel`, as in the
  prototype), stretched to the overlay, `FireworksView` raycast target off; `EpisodeUnlockedOverlay._fireworks` → it.

### Verification
- Console: no errors. Play mode, `SceneLoader.LoadHome(HomeArrival.WithUnlockedEpisode(1))`: screenshots
  `Screenshots/fireworks/episode_*.png` (Episode 2 card with bursts over it); `sfx_win_fanfare` was playing on the
  AppRoot sources through the bursts. The jingle (0.55 s) fell between samples and was not caught.
- Side effect on the Editor save: the test left level 4 mid-attempt; lives were set back to 5, but the attempt mark
  stays (the next Play costs one life and a retry, as stopping mid-level always does).

## 2026-11-02 — Fifth art delivery, stage 1: Level HUD boxes, board placement, corks, keys, stands

- **Prepare Sprites** (code: `SpriteRules` Deliveries/Replacements, new `CanvasAnchored` rule with `CanvasAnchor` and
  `SpriteTrim.TrimAnchored`; the cork cut moved out of `TubeArtTrim`): moved the HUD boxes to `Art/Sprites/UI/Hud/` and
  the stands to `Art/Sprites/Tubes/Stands/`; replaced in place `UI/Win/Win_Picture`, `Episodes/MascotFrame_Ep2_Summer`,
  the 10 coloured corks and the 6 keys (docs/art-inventory.md, section 12). The other four mascot frames were restored to
  their delivered canvas first (from git) so the set is cut together. Stage 2's 12 files stay in `Assets/Art/Incoming/`.
- `Prefabs/Tube.prefab`: new child `Stand` (SpriteRenderer, TubeBack −2, off), `TubeView._stand` → it.
- `ColorPalette.asset`: `_entries[0–9]._stand` → `Stand_<Colour>`; `_keyMarkSize` 0.5 → 0.38, `_keyMarkOffset`
  (0, −0.3) → (0.33, 0.30).
- `TubeVisualConfig.asset`: new `_capRaise` 25.
- `Scenes/Level.unity`:
  - `LevelCanvas/SafeArea/TopBar/TitleBox` (new; Image `Hud_LevelBox`, sliced, PPU multiplier 2.5, no raycast;
    centre, 420×120) now holds `LevelTitle` (stretched, 44/6 inset, margins 0, auto-size max 72 → 64).
  - `TopBar/TierBox` (new, off; Image `Hud_TierBox_Hard`, sliced, PPU multiplier 3.5, no raycast; anchored to the
    bar's bottom centre, pivot top, 24 up, 220×70) with `Label` (TMP, Baloo 2 Bold, Title material, auto-size 24–42).
  - `LevelHud`: `_titleBox`, `_tierBox`, `_tierLabel`, `_hardBox`, `_superHardBox` assigned.
  - `SafeArea/BoosterBar`: anchored bottom centre, 460×196, 16 up; new Image `Hud_BoosterBox` (Simple, preserve
    aspect); layout spacing 50 → 34; the three buttons 150×152 → 112×114 (layout elements too), badges 52 → 39 and buy
    badges 60 → 45 at (−4.5, −4.5), badge count auto-size max 20.
  - `BoardLayout`: `_keepClear` = TitleBox, MascotFrame, SettingsButton, BoosterBar, StuckPanel/Banner,
    StuckPanel/Buttons; `_keepClearWhenShown` = TierBox; `_hudClearance` 0.3; `_topLiftOverlap` 1 and `_spareAbove` 0.4
    (defaults). `_boardArea` removed (field gone) and `SafeArea/BoardArea` deleted.

### Verification
- EditMode tests: 1776 passed, 0 failed. Prepare Sprites rerun: nothing left to move or cut.
- Console: no errors. Screenshots in `Screenshots/stage1/` (editor test visits, 1080×1920 unless named): level 95
  (largest: 3 rows, two capacity-10 tubes, lock boxes) at 9:16, 9:19.5 and 3:4 with no tube on any HUD piece; a ball
  lifted from its top row (below the title box); hard level 94 and super hard level 99 with their labels; level 20
  (large tube on its stand, no ice; corks forced on to check their seat); level 55 (capacity-4 clay keeps its ice);
  lock level 80 (key over the eye's top right); the Win panel with the fixed picture. `before_L95_9x16.png` is the
  layout before.

## 2026-11-02 — Fifth art delivery, stage 2: Home backgrounds, Episode Unlocked, cream popups, tutorials, Shop, Settings, Leaderboard

- **Prepare Sprites** (code: `SpriteRules`): moved `Home_Ep1_Lab`, `Home_Ep3_Ice`, `Home_Ep4_Lava`, the four
  `Episodes/Unlocked/EpisodeUnlocked_Ep<N>_<Theme>` (new folder rule: 1024, opaque) and `UI/Popups/Popup_Panel_Cream`
  (borders 130); replaced in place `Home_Ep2_Summer` (same pixels) and `Home_Ep5_Space`. `Assets/Art/Unused/
  Popup_Panel_Cream.png` (identical, unreferenced) moved to the trash. `Assets/Art/Incoming/` is empty and gone.
- **New tool** `Editor/Art/TutorialPictureBuilder.cs` (**Ball Sort ▸ Art ▸ Build Tutorial Pictures**, Play mode):
  rendered `Art/Sprites/UI/Tutorials/Tutorial_Hidden, _BigTube, _Veil, _Ice, _LockBox.png` (new folder rule: trimmed
  each, 512, ASTC 4×4).
- Code: `Episode._unlockedPicture` / `UnlockedPicture`; `EpisodeUnlockedOverlay._mascot` → `_picture`
  (`FormerlySerializedAs`); `HomeFlow` shows the picture; `SettingsPopup._termsButton`.
- `EpisodeConfig.asset`: Home backgrounds 1–5 = `Home_Ep<N>_<Theme>`; `_unlockedPicture` of episodes 2–5.
- `TutorialConfig.asset`: `_image` of levels 7, 20, 30, 55, 80 = the five pictures; texts of levels 20 and 55 (screens.md,
  Tutorials).
- `Prefabs/UI/TutorialPopup.prefab`, `BuyPopup.prefab`, `EpisodeUnlockedOverlay.prefab`: `Panel` Image →
  `Popup_Panel_Cream` (sliced, PPU multiplier 1); `Hint` colour `#2E2B6EB3`; tutorial `Text` colour `#2E2B6E`;
  Episode Unlocked picture (`Mascot`) preferred height 400 → 460.
- `Prefabs/UI/SettingsPopup.prefab`: new `Panel/TermsButton` (off; TMP "Terms and Privacy", Body material,
  underlined, auto-size 28–46, white; Button, navigation none; anchors 0.15–0.85 × 0.11–0.20, the Restart/Home strip);
  `SettingsPopup._termsButton` → it.
- New material `Art/Fonts/Baloo2-Bold SDF Black Outline.mat` (a copy of Title with a black outline and a 50 % black
  shadow).
- `Scenes/Home.unity`: the nine Shop `Price` labels (6 bundles, 3 pack cards) → Black Outline material, white;
  `LeaderboardPage/.../Panel/Rank1–3` (new TMP "1", "2", "3", Title material, 60, 80×80, layout ignored) on the tubes'
  windows at (464, −336), (198, −382), (737, −382).

### Verification
- EditMode tests: 1776 passed, 0 failed. Console: no errors. Prepare Sprites rerun: nothing left to move or cut.
- Screenshots in `Screenshots/stage2/`: Home on each episode's background, Episode
  Unlocked for episodes 2–5 (picture on the cream panel, fireworks), each tutorial popup (levels 1, 7, 20, 30, 55, 80),
  the Buy popup, the Shop (white prices with black outlines), Home settings (Terms and Privacy), the Leaderboard (1, 2,
  3 on the tubes).
- Editor save: the Level visits of stage 1 left it at 3 lives; set back to 5.

## 2026-11-02 — Tutorial popup as the New Item card; key on the belly

- Code: `TutorialEntry` gets `_name` and `_hint` (`_text` is the main line); `TutorialPopup.Show(name, text, hint,
  image)` with the card's parts and their entrance (name and picture pop, OutBack); new `UI/NewItemGlow.cs` (rays,
  glow and sparkles as a uGUI mesh); `LevelFlow` passes the entry's fields.
- `Prefabs/UI/TutorialPopup.prefab` rebuilt: root dim `#0A051A` at 95 %; new `Content` (vertical layout, 1000 wide,
  centred) holding `Name` (TMP, Yellow material, vertex gradient, auto-size 70–128), `NewItemLabel` (TMP "New Item
  Unlocked!", Title material, 64), `PictureArea` (560 high: `Glow` 900×760 `NewItemGlow`, `Picture` 620×500, aspect
  kept), `Panel` (the cream card, PPU multiplier 1.8, padding 80/80/62/66, 860 wide: `Text` 52 navy and new `HintLine`
  38 `#7A6E9E`, both Plain material) and `Hint` ("Tap to Continue", Title material, white, 60).
- New material `Art/Fonts/Baloo2-Bold SDF Plain.mat` (Body without outline, shadow or dilate).
- `TutorialConfig.asset`: names, lines (highlight `#9B3BEB`) and hints of levels 1, 7, 20, 30, 55, 80 from
  docs/design/TEXT_CHANGES.md, section 3 (level 20's hint back to "It only takes its own color.").
- `ColorPalette.asset`: `_keyMarkSize` 0.38 → 0.46, `_keyMarkOffset` (0.33, 0.30) → (0.06, −0.30).
- `Tutorial_LockBox.png` re-rendered with the key on the belly (Build Tutorial Pictures, Prepare Sprites).

### Verification
- EditMode tests: 1776 passed. Console: no errors. Screenshots `Screenshots/stage2/newitem_level*.png` (all six) and
  `key_on_belly_L80.png`.

## 2026-11-02 — Titles: "Level N Complete!", "Episode N Unlocked!" and the episode's name

- Code: `WinPopup._titleFormat` and `EpisodeUnlockedOverlay._titleFormat` defaults; `Episode._name`;
  `EpisodeUnlockedOverlay._episodeName` and `Show(number, name, picture, beats)`; `HomeFlow` passes the name.
- `Prefabs/UI/WinPopup.prefab`: `_titleFormat` "Level {0} complete" → "Level {0} Complete!".
- `Prefabs/UI/EpisodeUnlockedOverlay.prefab`: `_titleFormat` → "Episode {0} Unlocked!"; new `Panel/EpisodeName` under
  `Title` (TMP, Plain material, navy `#2A1B5E`, auto-size 32–52, 64 high), `_episodeName` → it.
- `EpisodeConfig.asset`: `_name` = Classic Lab, Beach Lab, Arctic Lab, Lava Lab, Space Lab (the prototype's names on
  our themes; docs/design/TEXT_CHANGES.md 4.6 said no names, product asked for them on 2026-11-02).
- Verified: screenshots `episode_unlocked_named.png`, `win_title.png`; no scene overrides of either format.

## 2026-11-02 — Large tubes' stand a little lower

- Code: `TubeVisualConfig._standDrop` (art pixels; `StandDrop` in world units) and `TubeView.Setup` places the stand
  that far below the base.
- `TubeVisualConfig.asset`: `_standDrop` 24 (8 first; too little to see).
- Verified: level 20 screenshot `Screenshots/stage2/stand_drop_L20.png` (only the stand moves; the tube's round bottom now sits on the
  stand's top face); console clean.
