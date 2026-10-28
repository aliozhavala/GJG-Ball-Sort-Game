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
