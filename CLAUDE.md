# Ball Sort — Project Guide for Claude Code

Mobile ball-sort puzzle built as a hiring case study for **Good Job Games**.
Team of 3: game developer (owner of this repo's code), product, art. One-week deadline.
Reviewers will judge **technical quality, level design, art, and player experience** — they will read this code.
Optimize for clarity, clean architecture, and robustness over cleverness.

## Brief (from the case study)
- Player moves balls from the top of one tube to another. A ball may go into an empty tube or onto a ball of the same color. Level is won when every tube holds a single color.
- **100 levels**, all solvable, difficulty increasing across the run.
- **At least two new mechanics** across the levels. Confirmed: **hidden balls**. Others TBD (candidates: variable tube capacity, locked tubes).
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
- **Ball** = color id (`int`, 0..N-1) + `IsHidden` flag.
- **Run** = the contiguous same-color, **revealed** balls at the top of a tube. Hidden balls are never part of a run.
- **Move** source → target is legal when:
  - source ≠ target, source has ≥1 revealed ball on top, source is not completed and not locked;
  - target is not locked, has free space, and is empty **or** its top ball is **revealed** and has the run's color. (Requiring the target's top to be revealed is unreachable in normal play — balls arriving on a tube are always revealed — but it stops corrupt level data from producing a move that stacks onto a colour the player cannot see.)
- **Moved count** = `min(runLength, target.FreeSpace)` (partial moves allowed, water-sort style).
- **Reveal**: after a move, the new top ball of the source tube becomes revealed. At level start, every tube's top ball must be revealed (loader enforces this).
- **Undo** restores ball positions; revealed balls **stay revealed** (information already gained is not taken back).
  - Undo is a **booster with a charge count** (generous, not an economy — this is a case study). Each undo spends one charge.
  - **A move that completes a tube cannot be undone.** The tube is finalized (corked) and never returns to an unfinished state. Because undo is LIFO, such a move **seals the history**: every move at or before it is permanently non-undoable. Moves made *after* it are undoable again, down to that seal.
  - Undo is therefore blocked for exactly three reasons: no moves left, the next move to undo is sealed by a tube completion, or no charges left. The UI should be able to tell them apart.
- **Restart** reloads the level from its source data, **with hidden balls hidden again**. It is *not* "undo everything" — see the undo rules above.
- **Tube completed** = full (`Count == Capacity`) and single color. Completed tubes cannot be a move source.
- **Level won** = every tube is either empty or completed.
- **Input UX**: tap a tube to select (its run lifts); tap it again to deselect; tap another tube to move. If the move is illegal, play invalid-move feedback and clear the selection (never auto-select the tapped tube; the player selects again).

## Architecture
Strict separation: **Core (pure logic) → Game (Unity views/flow) → Editor (tools)**.

- **Core** (`BallSort.Core`, asmdef with `noEngineReferences: true`): plain C#, no `UnityEngine`. Board state, rules, commands, solver, level DTOs. Must be usable from tests, editor tools, and background threads.
- **Game** (`BallSort.Game`): MonoBehaviours — views, input, layout, game flow, UI, audio, haptics, services (save, level loading). Depends on Core.
- **Editor** (`BallSort.Editor`, editor-only): level generator/validator windows. Depends on Core.
- **Tests** (`BallSort.Tests.EditMode`): tests Core.

### Patterns
- **Command**: every move is a `MoveCommand` (`Execute`/`Undo`) kept in a `CommandHistory`, which owns the undo charges and the completion seal. Restart does **not** build on it — restart reloads the level.
- **Observer**: Core raises plain C# events (`MoveApplied`, `BallRevealed`, `TubeCompleted`, `LevelCompleted`, `MoveUndone`). Views, audio, haptics and UI subscribe. Always unsubscribe in `OnDisable`/`OnDestroy`. Avoid Inspector-wired UnityEvent chains for game logic.
- **State machine**: game flow (`Loading → Playing → Won → Transition`) and input selection state.
- **Object pooling**: balls and VFX via `UnityEngine.Pool.ObjectPool<T>`. No `Instantiate`/`Destroy` per move.
- **ScriptableObjects** for config: `ColorPalette` (color id → sprite/tint), `AnimationConfig` (timings, eases), `LevelDatabase` (ordered list of level `TextAsset`s).
- **Composition root**: one `GameBootstrap` creates and wires services. **No singleton sprawl** — no static `Instance` unless explicitly agreed.

### Timing and async
- **Game logic never waits on animation.** The model applies moves instantly and fires events; views animate asynchronously and may be interrupted (a re-selected ball kills/completes its tween first).
- **DOTween** for motion. **Coroutines or `Awaitable`/async** only for orchestration (delays, win sequence, level transitions) — pick one style per feature, don't mix in the same flow.
- **Solver at runtime** (hints) runs on a background thread (`Task.Run`), results applied on the main thread.

## Levels

### Progression
- **Linear only in the game:** no level select screen. The player always continues from their highest unlocked level (saved via a `SaveService`, PlayerPrefs-backed).
- **Editor-only level jump:** a developer tool to pick and play any level (1–100) from the Editor, for testing and tuning. It must not ship in player builds (`#if UNITY_EDITOR` or the Editor assembly).

### File format
One JSON file per level: `Assets/Data/Levels/level_001.json` … `level_100.json`, referenced in order by `LevelDatabase`.
Ball arrays run **bottom → top**. `hidden` is optional and parallel to `balls`. `lock` is optional (only if the locked-tube mechanic is adopted).

```json
{
  "id": 37,
  "colors": 6,
  "tubes": [
    { "capacity": 4, "balls": [2, 0, 1, 2], "hidden": [true, true, true, false] },
    { "capacity": 4, "balls": [1, 1, 0, 2] },
    { "capacity": 4, "balls": [] }
  ],
  "meta": { "optimalMoves": 23, "difficulty": 0.54, "mechanics": ["hidden"], "tutorial": false }
}
```

Loader validates: each color's total ball count fills tubes exactly; no tube over capacity; top balls revealed; level solvable (editor-time check).

### Solver and generator
- **Solver** (Core): BFS / A* over board states using the **same `MoveRules` as the game** (single source of truth), with full knowledge of hidden colors but honoring hidden flags for run building.
  - Canonical state hash: sort tubes that are interchangeable (same capacity and rules) before hashing.
  - Pruning: never move out of a completed tube; never move a single-color tube's entire content into an empty tube; never immediately reverse the previous move.
- **Generator** (Editor window): parameters (colors, capacity, empty tubes, mechanics) → random fill → solve → reject unsolvable/trivial → score difficulty → export JSON.
  - Difficulty signals: optimal move count, states explored, dead-end ratio, number of empty tubes, hidden-ball count.
- Product teammate curates the final 100 and hand-authors tutorial levels for each mechanic.

## Folder structure
Project folders live **directly under `Assets/`**, alongside `Plugins/`. (There is no `_Project/`
wrapper: the only third-party asset is DOTween, and it is already quarantined in `Plugins/`, so the
extra nesting level buys nothing here.)
```
Assets/
  Scripts/
    Core/            BallSort.Core.asmdef  (Model/, Rules/, Commands/, Solver/, Levels/)
    Game/            BallSort.Game.asmdef  (Bootstrap/, Flow/, Views/, Input/, Layout/, UI/, Audio/, Services/)
    Editor/          BallSort.Editor.asmdef (LevelGenerator/, Validation/)
  Tests/EditMode/    BallSort.Tests.EditMode.asmdef
  Art/               Sprites/, Atlases/, Materials/, VFX/
  Audio/
  Data/              Config/ (ScriptableObjects), Levels/ (*.json), LevelDatabase.asset
  Prefabs/
  Scenes/            Main.unity
  Plugins/           third-party (DOTween) — do not modify
  Resources/         DOTweenSettings.asset only — DOTween loads it by name, leave it here
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
- **Do not edit `.unity`, `.prefab`, or `.asset` YAML** unless explicitly asked. Instead, list the Inspector/scene wiring the developer must do.
- Do not touch `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Plugins/`, `ProjectSettings/`, or `Packages/manifest.json` without asking.
- Keep Core free of `UnityEngine` references (the asmdef enforces it — don't work around it).
- Before large refactors or new dependencies, propose the plan first.
- After each change, summarize: files changed, what to wire in the Editor, how to test.
- Keep changes small and commit-sized. Don't rewrite working code for style alone.

## Open decisions (update as they're settled)
- Second (and third) mechanic: variable capacity? locked tubes? other?
  - If **locked tubes** are adopted, each level that uses them must also provide the in-level unlock
    condition that makes it solvable. There are no permanently-locked tubes. The unlock trigger is
    not designed yet, so `Tube.IsLocked` is currently a plain flag with no unlock logic in Core.
- Boosters (extra tube, hint) — not designed. Undo is settled: charge-based, generous count, no economy.
- Should a tube that completes while still holding hidden balls reveal them? (Possible: the balls under
  a revealed top can be hidden *and* the same color, so a push can cork a tube the player can't verify.
  Core currently implements the literal rule — full + single color, hidden or not.)
- Sprite specs from art: pixels-per-unit, ball size, tube slicing (9-slice vs. modular), palette size, colorblind patterns.
- Difficulty curve shape and mechanic introduction levels (product).