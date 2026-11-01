# Ball Sort — Project Guide for Claude Code

Mobile ball-sort puzzle built as a hiring case study for **Good Job Games**.
Team of 3: game developer (owner of this repo's code), product, art. One-week deadline.
Reviewers will judge **technical quality, level design, art, and player experience** — they will read this code.
Optimize for clarity, clean architecture, and robustness over cleverness.

## Brief (from the case study)
- Player moves balls from the top of one tube to another. A ball may go into an empty tube or onto a ball of the same color. Level is won when every tube holds a single color.
- **100 levels**, all solvable, difficulty increasing across the run.
- **At least two new mechanics** across the levels. Final set: **hidden balls**, **veil**, **lock and key**, **clay**, **large tubes** — see docs/mechanics.md.
- Deliverable: project zip without `Library/`.

## Tech stack
- **Unity 6000.2.6** — everyone uses this exact version.
- **URP, Universal 2D template.** Sprites use `Sprite-Unlit-Default` (no 2D lighting dependency).
- **Portrait, mobile (iOS/Android).** `Application.targetFrameRate = 60`.
- **DOTween** for all motion (`Assets/Plugins/Demigiant/`).
- **Newtonsoft JSON** (`com.unity.nuget.newtonsoft-json`) for level files.
- **Input System package** for input.
- **Unity Test Framework** (EditMode) for Core logic.

## Game rules (source of truth — implement exactly)
- **Ball** = color id (`int`, 0..N-1) + `IsHidden` flag + optional key (the id of the lock it opens).
- **Run** = the contiguous same-color, **revealed** balls at the top of a tube. Hidden balls are never part of a run.
- **Move** source → target is legal when:
  - source ≠ target, source has ≥1 revealed ball on top, source is not completed, not covered (veil/lock) and not clay;
  - target is not covered, has free space, and is empty **or** its top ball is **revealed** and has the run's color. (Requiring the target's top to be revealed is unreachable in normal play — balls arriving on a tube are always revealed — but it stops corrupt level data from producing a move that stacks onto a colour the player cannot see.)
- **Moved count** = `min(runLength, target.FreeSpace)` (partial moves allowed, water-sort style).
- **Reveal**: after a move, the new top ball of the source tube becomes revealed. At level start, every tube's top ball must be revealed (loader enforces this).
- **Undo** restores ball positions; revealed balls **stay revealed**, opened covers stay open and found keys stay found (information and progress already gained are not taken back).
  - Undo is a booster: each undo spends one Undo from the player's `BoosterInventory`.
  - **A move that completes a tube cannot be undone.** The tube is finalized (corked) and never returns to an unfinished state. Because undo is LIFO, such a move **seals the history**: every move at or before it is permanently non-undoable. Moves made *after* it are undoable again, down to that seal.
  - Because reveals survive undo, an **undo can complete a tube** (putting a run back onto a ball it uncovered). It corks the tube and seals the history the same way, and can win the level.
  - Undo is therefore blocked for exactly three reasons: no moves left, the next move to undo is sealed by a tube completion, or no Undo boosters left. The UI should be able to tell them apart.
- **Boosters** (Undo, Shuffle, AddTube) are global player inventory (`BoosterInventory`), spent one per use, in any level. No per-level charges. At 0 a booster's count badge becomes a green plus and a tap opens the Buy popup (coins; prices in `EconomyConfig`, docs/screens.md, Buying).
- **Add tube** appends an empty tube (capacity from config, currently 4) at the end of the board, so tube indices and history stay valid. Not undoable, doesn't seal. It goes to the end of the grid row with the fewest cells (ties → bottom-most); rows grow to 8 cells during play, rows are never added; refused once every row has 8 cells.
- **Play on** (only while stuck): costs coins (`EconomyConfig`), appends a tube exactly like Add tube (same layout limit), ignores the booster inventory, not undoable, doesn't seal.
- **Shuffle** rearranges **one tube the player picks**: tapping Shuffle highlights the eligible tubes (not covered, clay or completed, holding two balls that *look* different: "?" and key marks count as looks, hidden colours are never read) and dims the rest; the next tap on an eligible tube shuffles it into an order that looks different and spends one Shuffle, any other tap cancels for free. Hidden balls keep their flag unless they land on top (revealed); keys move with their balls and a key landing in the top run is found. Clears the undo history; no solvability check. Full rules in docs/mechanics.md.
- **Restart** ends the attempt like any exit before a win (it costs a life and counts a retry; at 0 lives it opens More Lives instead, docs/screens.md) and reloads the level from its source data, in the next variant (docs/levels.md): hidden balls hidden again, added tubes gone, history cleared; boosters already spent stay spent. It is *not* "undo everything".
- **Tube capacity**: 4 for normal tubes; large tubes (5–16) span 2–3 grid rows. Large tubes need not be clay.
- **Tube completed** = full, every ball revealed, and holds **every ball of its color in the level** (details in docs/mechanics.md). Completed tubes cannot be a move source.
- **Level won** = every tube is either empty or completed.
- **Stuck** = not won, and no legal move or only moves that loop back (every reachable position can return here and none is a win); any one-way move means not stuck. `StuckDetector`, rule in docs/mechanics.md. Checked after every move, undo, added tube, Play on and shuffle. While stuck, and after a win, the whole booster bar is disabled, buying included.
- **Input UX**: tap a tube to select (its top ball lifts; the rest of the run follows when it moves); tap it again to deselect; tap another tube to move. If the move is illegal, play invalid-move feedback and clear the selection (never auto-select the tapped tube; the player selects again).

Mechanics, completion and board layout rules: read docs/mechanics.md when working on Core rules, levels or board views.

## Architecture
Strict separation: **Core (pure logic) → Game (Unity views/flow) → Editor (tools)**.

- **Core** (`BallSort.Core`, asmdef with `noEngineReferences: true`): plain C#, no `UnityEngine`. Board state, rules, commands, solver, level DTOs, `LevelSession`, `BoosterInventory`, `Wallet`, `PlayerData` (progress and attempts, `Lives`, name, avatar, tutorials seen, settings; time through `IClock`) and its save shape `SaveData`. Must be usable from tests, editor tools, and background threads.
  - **`LevelSession`** is the single gameplay entry point: `TryMove`, `TryUndo`, `TryAddTube`, `TryShuffleTube` (with `CanShuffleTube` and `ShuffleBlockReason` for the pick), `Restart`, `IsStuck`, booster spending, and events. It wraps `BoardState` + `CommandHistory` + `BoosterInventory`. **UI and input talk only to `LevelSession`**, never to the board or history directly.
  - Completion is a board query (`BoardState.IsCompleted`): it needs the level's colour totals, counted once per board and shared by its clones. The grid (`BoardGrid`) lives on the session, not the board; it never affects move legality.
- **Game** (`BallSort.Game`): MonoBehaviours — views, input, layout, game flow, UI, audio, haptics, services (save, level loading). Depends on Core.
- **Editor** (`BallSort.Editor`, editor-only): level validation and play-testing windows. Depends on Core.
- **LevelJson** (`BallSort.LevelJson`, no engine refs): the Newtonsoft level parser, shared by Game, Editor and Tests.
- **SaveJson** (`BallSort.SaveJson`, no engine refs): the save's JSON codec (`SaveJsonCodec`: version check, migrations, required fields, clamping), shared by Game and Tests; `SaveService` (Game) only keeps its text in PlayerPrefs.
- **Tests** (`BallSort.Tests.EditMode`): tests Core, LevelJson and SaveJson; must not reference Game.

### Patterns
- **Command**: every move is a `MoveCommand` (`Execute`/`Undo`) kept in a `CommandHistory`, which owns only the completion seal (booster counts live in `BoosterInventory`). Restart does **not** build on it — restart reloads the level.
- **Observer**: Core raises plain C# events (`MoveApplied`, `BallRevealed`, `TubeCompleted`, `TubeUnveiled`, `KeyFound`, `LockOpened`, `LevelCompleted`, `MoveUndone`, `TubeAdded` with its grid row and cell, `Shuffled` with the tube, its new order and where each ball came from, `StuckChanged`, plus change events on `Wallet` and `BoosterInventory`), in the order documented on `LevelSession`. The board finishes every mutation, including the cover cascade, before its first event fires, but board events still arrive before the session has recorded the move in its history; anything showing derived state (booster availability) refreshes on `LevelSession.Changed`, which fires once the operation is complete. Views, audio, haptics and UI subscribe. Always unsubscribe in `OnDisable`/`OnDestroy`; an object that starts inactive (a closed popup) never runs `OnDestroy`, so it subscribes to persistent data (`PlayerData`) in `OnEnable` and unsubscribes in `OnDisable`. Avoid Inspector-wired UnityEvent chains for game logic.
- **State machine**: level flow (`LevelFlow`: `Loading → Playing ⇄ Stuck → Won`, plus `Playing → ShufflePicking → Playing` while the Shuffle booster waits for a tube) and input selection state.
- **Object pooling**: balls and VFX via `UnityEngine.Pool.ObjectPool<T>`. No `Instantiate`/`Destroy` per move.
- **ScriptableObjects** for config: `ColorPalette` (color id → sprite/tint/cork), `AvatarConfig` (avatar pictures), `AnimationConfig` (timings, eases), `SfxConfig` (sound slots, volume, pitch rules), `LevelDatabase` (ordered list of level `TextAsset`s), `EconomyConfig` (rewards per tier, lives, Play on, booster and heart prices, shop coin bundles and packs), `EpisodeConfig` (episodes, their backgrounds and mascot frames), `TutorialConfig` (tutorial popup text per level id), `LeaderboardConfig` (the leaderboard's fixed bots).
- **Composition**: a persistent **`AppRoot`** (prefab in `Resources/`, auto-created via `RuntimeInitializeOnLoadMethod`, `DontDestroyOnLoad`) owns player data, saving, scene loading, sound (`AudioService`: pooled 2D sources, one named slot per sound in `SfxConfig`, muted by the Sound setting) and haptics (`HapticsService`: one method per kind, off with the Haptics setting; calls in place, no vibration yet). It is the **one agreed singleton-style exception**. Each scene has its own bootstrap that takes services from `AppRoot` and hands them to the scene's flow (`LevelFlow`, `HomeFlow`), which wires the views; what Level leaves for Home (a notice, an unlocked episode) travels in memory as a `HomeArrival` through `SceneLoader`. **No other singletons** — no static `Instance`.
- **Scenes**: `Home` (build index 0) and `Level` (build index 1). Pressing Play in either scene must work.

### Timing and async
- **Game logic never waits on animation.** The model applies moves instantly and fires events; views animate asynchronously and may be interrupted (a re-selected ball kills/completes its tween first). Board animations run on the `BoardClock` (DOTween manual update, `AnimationConfig.Speed` and the hit-stop; never `Time.timeScale`); later beats of a move wait on its flights, not on timers (docs/animation-spec.md). Touching a tube finishes its pending beats at once; a beat finished that way snaps to its end and drops the sounds it had not played yet (`TubeView.IsFinishing`). The win's panel is the one skippable sequence: a tap jumps to its next step.
- **DOTween** for motion. **Coroutines or `Awaitable`/async** only for orchestration (delays, win sequence, level transitions) — pick one style per feature, don't mix in the same flow.
- **Solver at runtime** (hints, later) must run on a background thread (`Task.Run`) against a board copy, results applied on the main thread. Nothing uses it at runtime today: Shuffle no longer checks solvability.

## Presentation
- Art is swappable: sprites only via ScriptableObjects (`ColorPalette`, `TubeVisualConfig`, `EpisodeConfig`) or prefab/scene fields. Final art lives in `Art/Sprites/` (inventory, problems and missing art: docs/art-inventory.md); **Ball Sort ▸ Art ▸ Prepare Sprites** moves new files from `Art/Incoming/` to their place (`SpriteRules.Deliveries`; replacements are copied over the old file, `SpriteRules.Replacements`), trims delivered files in place (swap sets to one shared rectangle) and applies the import settings (`SpriteRules`: one PPU of 100, no mipmaps, ASTC on mobile). Placeholders for slots without art: built-in Circle and 9-slice sprites, tinted, and generated ones (the heart on Home's lives pill), in `Art/Sprites/Placeholder/`.
- Board: world-space SpriteRenderers, orthographic camera. `BoardLayout` places tubes on the level's grid (free and aligned rows, large tubes spanning rows) and scales the board root to fit the widest row and the row count between the HUD pieces it keeps clear of (`BoardLayout._keepClear`: title box, mascot frame, settings, booster box, stuck bar; `_keepClearWhenShown`: the tier label), read from their rects so it holds at any aspect ratio (falls back to top/bottom fractions of the safe area), with a scale cap. The top row's lift gap may reach past the top HUD's line (`_topLiftOverlap`); height the board does not need is shared out above and below it (`_spareAbove`). Tubes keep a home position from the layout (`TubeView.Place`); bump and shake are offsets on top, and the tube-complete punch multiplies the fitted scale (`BoardLayout.SetPunch`), so a re-layout mid-animation stays right.
- Tube: rim back + stretched middle behind the balls, bottom part and rim front over them (drawn at one art scale, placed by pivots on the walls' centre line), optional glass front; `BoxCollider2D` tap target covering tube and lift area.
- Sorting layers (bottom→top): Background, TubeBack, Balls, TubeFront, VFX. On TubeBack: the completed-tube glow, then the tube middle and rim back. Balls draw in slot order (upper balls in front). On TubeFront: glass and the tube's bottom part (over resting balls), clay, cork, rim front, covers, then raised balls and their trail (`TubeView._raisedSortingOrder`); particle effects are on VFX. A ball's depth follows its position (`BallView.SetMouth`): inside its tube's mouth it is on Balls (between the rim halves, under the bottom part, clay and cork); once its bottom clears the mouth it is raised above every tube, through the whole arc, until it enters the target's mouth.
- Tube geometry and ball layout are separate (`TubeVisualConfig`): tube heights, the row pitch and large-tube spans follow from the geometry (`_bottomOffset`, `_slotHeight`, `_ballDiameter`, `_topPadding`); balls are drawn `_ballDiameter × _ballScale` and rest at y = bottomOffset + index × `_ballSpacing`, which never changes a tube. Editing them in Play mode re-lays the board out. Every ball sprite (monsters and hidden ball) shares one canvas, body size and centre: one `ColorPalette._artSpriteSize`, no per-colour or hidden corrections. Each colour has five faces (`BallExpression`: open, half-open, closed, laughing, shock; shock falls back to closed until its art exists), changed only through `BallView.SetExpression` (a resting face, or one held for a while) and the blink the board gives one ball at a time (`BlinkScheduler`, open faces only); balls in a completed tube laugh. The hidden-ball art carries its own "?" (the separate mark slot is empty); reveal = quick flip.
- Shuffle pick: eligible tubes get the glow in `TubeVisualConfig`'s highlight tint, the rest are darkened with their balls and covers (`TubeView.SetHighlight`, `BallView.SetShade`, `CoverView.SetShade`); the shuffled tube's balls hop into their new slots (docs/animation-spec.md, 4).
- Mechanic views: a `CoverView` per veil (hangs from the rim at art scale, shortened so the bottom ball's lower half shows, a sticker of the veil colour's open-eyes ball on it) and one per lock with a `LockBoxView` across its capacity-4 tubes (sides, tiled face, the lock's coloured mid; hides every ball; refitted after every layout change; opens through `CoverView.PlayUnlock`: the key flies into the slot, the covers slide apart, docs/animation-spec.md 3.3), the clay (ice) art at the base of clay tubes over their balls, scaled to the tube's drawn width × `_clayScale`, cracking and bursting when the tube completes (`TubeView.BreakIce`, docs/animation-spec.md 3.4; otherwise `TubeView.SetClayShown`), the lock's key in the hands of a keyed ball once revealed. Cover rules for the drawing: docs/mechanics.md. Sprites: veil, lock box parts, clay in `TubeVisualConfig`; key size and offset in `ColorPalette`. Lock art (the box's mid, the key) comes from `LockPalette`, never the ball palette.
- A ball's root carries its position (moved by `BallView.MoveTo`, plus bounce offsets); its `Shape` → `Face` children carry squash and stretch, the face turned back upright.
- Every ball position change goes through `BallView.MoveTo`, every cover change through `CoverView.SetOpen` (a snap) or an opening (`CoverView.PlayVeilLift`, `CoverView.PlayUnlock`); they hide/show the covered balls, so cover art may be translucent; every key-mark change through `BallView.UpdateKeyMark`. Ball moves are animated (select, flight, landing, invalid shake, tube complete, reveal flip, veil lift and sway, lock box, ice break, shuffle, blink, win celebration: docs/animation-spec.md; the Win panel enters animated, skippable by a tap), timed after the flights. Feedback lives in `LevelFeedback` (invalid move, landings, tube completed, reveal, veil, lock box, ice, boosters, stuck, celebration), driven by `BoardView`'s presentation events and the session's booster events. The stuck bar shows only once the board has settled (5.1). VFX are pooled `VfxBurst` prefabs in `Prefabs/VFX/`.
- Button feedback: `UiButtonFeedback` on each canvas root gives every button and settings toggle `sfx_ui_button` and a Light haptic; buttons whose result has its own sound (Home's Level button, Buy, Are You Sure's Confirm) are on its `_ownFeedback` list and their owner plays it. Purchases punch what they filled (`UiPunch`).
- UI: uGUI + TextMeshPro, Canvas Scaler 1080×1920, match 0.5, `SafeArea` on root panel. Labels never wrap where space is fixed: they shrink (TMP auto-size; coins via `AmountLabel`, abbreviating with Core's `AmountFormat`, e.g. 125K, only below the minimum size), and must fit their button, panel or field at any aspect ratio (fit rules in docs/screens.md, Shared UI pieces). Font: Baloo 2 Bold everywhere (ExtraBold read too heavy), a static TMP atlas in `Art/Fonts/` styled through seven material presets (outline and shadow by background; docs/art-inventory.md, section 9); typed names fall back to the device's system fonts at runtime (`SystemFontFallback`), never to a dynamic atlas saved in the project. Home's bottom bar is Shop · Home · Leaderboard (the art's order), full screen width (`BottomBar`), its tap areas above the bottom safe-area inset; pages keep above it (`KeepAboveBar`) or scroll behind it (`ScrollBehindBar`). Home's avatar shows the picked avatar in its frame (avatar menu); mascot frames are Level-scene only (top left; settings top right).

Screens, flow, lives, tutorials, boosters and economy: read docs/screens.md when working on UI or flow.

## Levels

### Progression
- **Linear only in the game:** no level select screen. The player always continues from their highest unlocked level (saved via a `SaveService`, PlayerPrefs-backed).
- **Editor-only level jump:** **Ball Sort ▸ Play Level…** (or Play on a Validate Levels row) picks and plays any level and variant from the Editor, for testing and tuning, for one visit (SessionState, read by `LevelBootstrap`). It can also play a level file outside the run: test and draft levels live in `Assets/Data/TestLevels/`, never in `LevelDatabase`. Every jump is a test visit: winning pays no reward and moves no progress, lives are neither checked nor spent (no retry, no level-in-progress mark), the tutorial shows on every visit without being saved as seen, and Restart replays the picked variant. It must not ship in player builds (`#if UNITY_EDITOR` or the Editor assembly).

Level JSON schema, variants, validation and solver details: read docs/levels.md when working on levels.

### Solvability
- Every variant of every shipped level must be solvable: `ShippedLevelTests` solves each variant (Fast mode) and plays the solution to a win. After editing levels, run **Ball Sort ▸ Validate Levels…** and write `meta.optimalMoves` back.
- `LevelDatabase` slot n holds `level_n`; a level held back (e.g. invalid) leaves its slot empty rather than shifting the rest.
- Tests never depend on a shipped level's content: they use fixtures in `Assets/Data/TestLevels/`, so product can retune levels freely.

## Folder structure
Project folders live **directly under `Assets/`**, alongside `Plugins/`. (There is no `_Project/`
wrapper: the only third-party asset is DOTween, and it is already quarantined in `Plugins/`, so the
extra nesting level buys nothing here.)
```
Assets/
  Scripts/
    Core/            BallSort.Core.asmdef  (Model/, Rules/, Commands/, Session/, Player/, Solver/, Levels/)
    LevelJson/       BallSort.LevelJson.asmdef — level JSON parser (Core + Newtonsoft, no engine refs)
    SaveJson/        BallSort.SaveJson.asmdef — save JSON codec (Core + Newtonsoft, no engine refs)
    Game/            BallSort.Game.asmdef  (Bootstrap/, Flow/, Views/, Input/, Layout/, UI/, Audio/, Services/)
    Editor/          BallSort.Editor.asmdef (Validation/, Tools/, Art/, Audio/)
  Tests/EditMode/    BallSort.Tests.EditMode.asmdef
  Art/               Sprites/ (Balls/, Tubes/, Mechanics/, UI/, Episodes/, Backgrounds/, Placeholder/), Fonts/, Unused/, Atlases/, Materials/, VFX/
  Audio/             SFX/ (gameplay/, mechanics/, boosters/, win_rewards/, ui/; mobile import settings from `SfxImportRules`)
  Data/              Config/ (ScriptableObjects), Levels/ (*.json), TestLevels/ (test/draft *.json, never in the database), LevelDatabase.asset
  Prefabs/
  Scenes/            Home.unity (index 0), Level.unity (index 1)
  Plugins/           third-party (DOTween) — do not modify
  Resources/         DOTweenSettings.asset (DOTween loads it by name, leave it here), AppRoot.prefab
  TextMesh Pro/      TMP Essential Resources (imported by TMP, leave it here)
  Settings/          URP render pipeline assets (from the 2D template)
```

## Coding conventions
- Namespaces mirror assemblies: `BallSort.Core`, `BallSort.Game.Views`, etc. One public type per file; file name = type name.
- `PascalCase` types/methods/properties, `_camelCase` private fields, `camelCase` locals/params.
- Inspector fields: `[SerializeField] private`, never public fields. Validate required references in `Awake` / `OnValidate`.
- No `FindObjectOfType`/`GameObject.Find`, no `SendMessage`, no string-based lookups. Pass dependencies explicitly.
- No game logic in `Update`; use events. Avoid allocations in per-move code paths (no LINQ there, cache `WaitForSeconds`).
- Prefer small, focused classes. Comments explain *why*, not *what*. XML docs on public Core APIs.
- Every Core rule change comes with an EditMode test.

## Rules for Claude Code
- **Never create, edit, or delete `.meta` files.** Unity generates them. If a file must be moved/renamed, say so and let the developer do it in the Editor.
- Scene, prefab and asset changes only through Unity MCP Editor tools, never by editing `.unity`/`.prefab`/`.asset` YAML. Afterwards, list everything created or modified, and append it to `docs/unity-editor-changes.md`.
- After code changes, check the Unity console via MCP; run EditMode tests via MCP after Core changes.
- Keep CLAUDE.md lean: rules and decisions only. Put detailed specs in `docs/` and reference them. No session notes or changelogs here.
- `docs/design/` holds product design documents; read them only when explicitly asked.
- Do not touch `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Plugins/`, `ProjectSettings/`, or `Packages/manifest.json` without asking.
- Keep Core free of `UnityEngine` references (the asmdef enforces it — don't work around it).
- Before large refactors or new dependencies, propose the plan first.
- After each change, summarize: files changed, what to wire in the Editor, how to test.
- Keep changes small and commit-sized. Don't rewrite working code for style alone.

## Roadmap
1 ✅ Core · 2 ✅ Level loading + greybox board/layout · 3a ✅ Core rules, player data, scenes, Home UI · 3b ✅ Level gameplay UI: input, HUD, boosters, win/stuck flows · 4a ✅ Core mechanics, grid layout and validation · 4b ✅ Solver, solvability checks, Validate Levels, Shuffle · 4c ✅ Mechanic views (placeholders) and editor level jump · 5 ✅ Level format v2: variants, tier, episodes; level import and validation · 6 Art/audio/VFX polish (handoff: docs/step6-handoff.md; 6a ✅ first art delivery integrated, docs/art-inventory.md; 6b ✅ board animations 2.2–2.6 and their sounds, docs/animation-spec.md; 6c ✅ second art delivery: rim halves, per-colour corks, avatars and the avatar menu, full-width bottom bar, shop scrolling behind it; 6d ✅ third art delivery: icons, popup panels, settings switches, tier button with multiplier, leaderboard page; 6e ✅ second animation batch: UI, booster and purchase sounds, haptic calls (no vibration yet), stuck bar timing, blinking, ice break, shuffle; 6f ✅ fourth art delivery: reworked balls, lock box and keys, booster and hard buttons, win picture; 6g ✅ third animation batch: reveal flip, veil lift and sway, lock box opening (key flight, doors), level win (board celebration, animated Win panel)) · 7a ✅ Level import: the delivered levels 1–100 checked and shipped (docs/reports/level-import.md) · 7c ✅ Player save in Core + SaveJson; lives, retry-based variants, Are You Sure / More Lives, tutorials, episode mascots and Episode Unlocked, Home tabs and Edit Profile; stuck = no useful moves (`StuckDetector`) · 7d ✅ Economy: buying boosters and hearts, shop coin bundles, per-tube Shuffle pick, rewards 20/60/100, episodes 1–19 / 20–39 / 40–59 / 60–79 / 80–100 · 7b Level curation, playtests, README, packaging

## Open decisions (update as they're settled)
- Sprite specs from art: settled for the first delivery (100 PPU, 256 px monsters, tube in three parts; docs/art-inventory.md). Still open: palette size (12 colour ids; 10 have monster art and corks, 10–11 keep tinted placeholders and the plain cork; the delivered levels use at most 10), lock palette (5 colours from the lock box art, ids 0–4; the delivered levels use 0–2; `Key_Red` is with the keys but no lock uses it until a red mid is confirmed), colorblind patterns.
- Second delivery compromises (docs/art-inventory.md §7): corks sit where drawn and cover the top ball's head (offset or redraw?); the full-width bottom bar crops 6 % of the art per side because the raised tabs have rounded ends.
- Booster count badge art (the booster buttons and the hard tier button arrived; docs/art-inventory.md, section 11). Tube parts found with the lock art are in `Art/Unused/`: possibly a redrawn tube, to confirm with the artist.
- Tutorials (product): `TutorialConfig` holds product's final lines for levels 1, 7, 20, 30, 55 and 80; the New Item card (name, header, picture) and its images are not built.
- Stuck search limit (32 positions, measured in the Editor, docs/mechanics.md): re-measure on a device; the level validator still only requires a legal move at the start, while `ShippedLevelTests` checks every start is not stuck.
- Difficulty curve shape and mechanic introduction levels (product): first delivery imported; its warnings (V2 vs V1, V3–V5 not easier) are in docs/reports/level-import.md. Level 100 is regular (decided 2026-11-01).