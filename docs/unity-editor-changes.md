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
