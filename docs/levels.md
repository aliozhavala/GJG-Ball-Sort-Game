# Levels — format, variants, validation and solver

### File format (version 2)
One JSON file per level: `Assets/Data/Levels/level_001.json` … `level_100.json`, referenced in order by `LevelDatabase`.
Ball arrays run **bottom → top**. This file is the schema; the rules and validation behind each field are in docs/mechanics.md.

A level is one board (`layout.grid`) and **five variants**: five puzzles on that board. Each try at the level plays one
variant (Variants and tries, below), so a player who retries does not face the same puzzle every time.

- Top level, all required: `formatVersion` (int, currently **2**), `id`, `tier`, `layout` and `variants`.
  - `tier`: `"regular"`, `"hard"` or `"superHard"`, as a string (a number is rejected). It sets the win reward and the Home Level button's look (docs/screens.md).
  - `layout.grid`: rows of cells, top row first; each cell is a tube index or `null`. At most 3 rows and 6 cells per row. A large tube is listed in every row it spans, in the same cell. Shared by all variants.
  - `variants`: exactly 5, played as V1–V5 in array order.
- Top level, optional: `tutorial` (`true` for a level that introduces a mechanic; per level, not per variant). Nothing reads it yet; tutorial levels will get their own UI later.
- Per variant: `colors` and `tubes` (required), `locks` and `meta` (optional).
  - Per tube: `capacity` and `balls` (required); optional `hidden` (parallel to `balls`), `veil: { "color": c }` (a ball colour id), `clay: true`, `keys: [{ "slot": s, "lock": id }]` (`slot` indexes `balls`).
  - `locks: [{ "id": id, "tubes": [i, j, …] }]` (`id` an integer ≥ 0; it also picks the lock's colour from `LockPalette`, which has 4 colours today, so use ids 0–3: a larger id draws black and logs an error, the validator does not catch it).
  - `meta` never affects gameplay: `optimalMoves` (written by Validate Levels), `difficulty` (0–1) and `notes` (free text), each optional.
- Each mechanic (hidden balls, veil, lock and key, clay, large tubes) uses **optional** fields (per tube, per ball, or `locks`); an absent field means the variant does not use it.
- Parse strictly: unknown fields are errors (Newtonsoft `MissingMemberHandling.Error`), so typos in hand-edited levels are caught.
- `formatVersion` is checked before anything else, and any value but 2 rejects the file with that one error: version 1 (one puzzle per file, no variants) gets "formatVersion 1 is no longer supported: convert the file to version 2", others "formatVersion N is not supported (expected 2)". Bump `formatVersion` only for breaking changes.

**Variant consistency.** The variants share one board, so the grid, the tube views and the lock doors fit all five:
every variant has the same tube count and, tube by tube, the same `capacity`, `clay` flag and cover kind (none, veil
or lock), and the same lock tube sets (each lock covers the same tubes). Everything else may differ: ball colours and
`colors`, hidden flags, key positions, veil colours, lock ids (door colours) and `meta`. Each variant is checked against
V1, e.g. `level_006: variant 3: tubes[2]: capacity is 5, but variant 1 has 4`.

Compact example, `level_006.json`: 5 tubes in two rows with a veil. V2–V4 swap colours and move the full tubes
around; V5 also hides a ball. (`LevelJsonParserTests` checks that every complete JSON example in the docs is valid and
every variant solvable.)

```json
{
  "formatVersion": 2,
  "id": 6,
  "tier": "regular",
  "tutorial": true,
  "layout": { "grid": [[0, 1, 2], [3, 4]] },
  "variants": [
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [0, 0, 0, 1] },
        { "capacity": 4, "balls": [1, 1, 2, 0] },
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [2, 2, 1, 2], "veil": { "color": 0 } },
        { "capacity": 4, "balls": [] }
      ], "meta": { "optimalMoves": 6, "notes": "Tutorial order: finish red to lift the veil." } },
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [1, 1, 1, 0] },
        { "capacity": 4, "balls": [0, 0, 2, 1] },
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [2, 2, 0, 2], "veil": { "color": 1 } },
        { "capacity": 4, "balls": [] }
      ], "meta": { "optimalMoves": 6 } },
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [0, 0, 1, 2] },
        { "capacity": 4, "balls": [2, 2, 2, 0] },
        { "capacity": 4, "balls": [1, 1, 0, 1], "veil": { "color": 2 } },
        { "capacity": 4, "balls": [] }
      ], "meta": { "optimalMoves": 6 } },
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [1, 1, 1, 2] },
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [2, 2, 0, 1] },
        { "capacity": 4, "balls": [0, 0, 2, 0], "veil": { "color": 1 } },
        { "capacity": 4, "balls": [] }
      ], "meta": { "optimalMoves": 6 } },
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [0, 0, 0, 2] },
        { "capacity": 4, "balls": [2, 2, 1, 0], "hidden": [true, false, false, false] },
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [1, 1, 2, 1], "veil": { "color": 0 } },
        { "capacity": 4, "balls": [] }
      ], "meta": { "optimalMoves": 7, "notes": "Tube 1 fills with yellow but cannot complete until its bottom ball is revealed." } }
  ]
}
```

Loader validates the format (below), then each variant against every completion, cover, clay, large-tube and grid rule in docs/mechanics.md, then the variant consistency rule. Solvability is an editor-time check on every variant (Validate Levels and `ShippedLevelTests`, below); it also catches what the rules can't, e.g. two locks whose keys sit inside each other.

- Required fields carry no parser attributes: a missing one arrives as `0`/`null` and the validator reports it (`id < 1`, `tier` missing, `layout` missing, `variants` missing or not exactly 5, `colors < 1`, `capacity < 1`, `balls` missing — use `[]` for an empty tube). Required ints for which 0 is a valid value (`veil.color`, a key's `slot` and `lock`, a lock's `id`) are nullable in the DTOs, so a missing one is reported rather than read as 0.
- `id` is 1-based and matches the file name (`level_007.json` → `7`); an EditMode test checks every shipped file.
- No tube over capacity; every tube's top ball starts revealed.
- Opening position, per variant (checked only once its structure is valid): no tube already completed, the variant not already won, and at least one legal move.
- All problems are reported at once, each as `<level>: <location>: <problem>`; inside a variant the location starts with it, 1-based like V1–V5, e.g. `level_003: variant 2: tubes[2]: top ball is hidden`.
- Split: Core owns the DTOs (`LevelDto` for the file, `LevelVariantDto` per variant, `LevelTier`), `LevelValidator`
  (`Validate` for a file; `ValidateVariant` checks one variant on a layout), `VariantSelector`, `LevelLayout` and
  `LevelFactory` (no serializer dependency); `LoadedLevel` holds a validated file and builds each try's board with
  `CreateBoard(variant)`; `LevelJsonParser` lives in its own engine-free assembly, `BallSort.LevelJson` (Core + Newtonsoft), so Game, Editor and Tests share it without depending on each other.

Current files are placeholders until the delivered levels arrive: `level_001`–`level_005` are plain, `level_006`
(veil), `level_007` (lock and key) and `level_008` (clay and a capacity-10 tube spanning 2 rows) are hand-made mechanic
tests. In each, V1 is the original puzzle and V2–V5 permute its colours and swap equivalent open tubes (same capacity,
no cover, not clay), so every variant is as solvable as V1; V5 of `level_006` also hides a ball. Placeholder tiers:
`level_004` hard and `level_005` super hard, so Home shows every look; the rest regular. `level_003` and
`level_006`–`level_008` are marked `tutorial`. `ShippedLevelTests` solves every variant of every file with the solver
and plays the solution to a win; the mechanic levels also keep a scripted solution on V1 that checks their mechanic
fires. The former stuck trap is the test fixture `TestLevels.StuckTrap()`.

### Variants and tries
- Tries 1–5 at a level play V1–V5 in order. From try 6 on, the variant is picked at random among V3–V5, never the
  same one as the previous try.
- A new try starts on **entering the level** and on **Restart**. **Play on** continues the same try. A **win** resets
  the count, so the next level starts at try 1 (V1).
- The try count and the last variant played are saved in player data (docs/screens.md) the moment a try starts, so
  leaving the level or closing the app mid-try still counts it, and the next entry is the next try.
- Replays (the last level again, after all levels are complete) use the same rules.

### Test and draft levels
Level files outside the run live in `Assets/Data/TestLevels/` (ids from 901, file names free). The `LevelDatabase` must
never list them (its `OnValidate` logs an error if it does), and `ShippedLevelTests` does not read that folder. Play one
with **Ball Sort ▸ Play Level… ▸ Level file**, in the variant picked there: like every editor jump it is a test
visit (no reward, no progress, no try counted). Test levels use the same format as shipped levels, five variants included.
- `stuck_lock.json`: the former stuck trap plus a locked empty tube 5 whose key sits on a hidden ball at the bottom of
  tube 2. In V1, red from tube 1 into the empty tube 4, then yellow from tube 0 onto tube 1, gets stuck with the door
  closed; Play on adds a tube to the same row, so the row re-centres and the door must follow its tube. Its variants
  only permute colours, so the same moves set the same trap in each. `TestLevelFileTests` keeps every variant valid,
  solvable and stuck-able that way.

### Solver
`BoardSolver.Solve(board, mode, nodeLimit, cancellationToken)` in `Core/Solver/`; pure C#, safe on a worker thread (it copies the board on entry).
- **Real rules:** moves come from `MoveRules` and are applied with the board's own `ApplyMove` cascade (reveal, completion, veils, keys, locks) on copies, never by undo (undo keeps reveals, opened covers and found keys, so it cannot backtrack). Full knowledge of hidden colours, but hidden flags still block runs.
- **Modes:** `Optimal` = A* with an admissible, consistent heuristic (the argument is in `SolverHeuristic`: blocks above the bottom of a tube, plus surplus bottom blocks per colour); `Fast` = weighted A* (g + 3h), usually short but not guaranteed shortest. Both prove `Unsolvable` exactly when the search runs out.
- **Result:** `Solved` / `Unsolvable` / `Unknown` (node limit hit), the moves (replayed and checked before returning), whether optimal, a proven lower bound (`≥ n` when Optimal stops at its limit), nodes expanded, largest frontier, elapsed time.
- **Position key** (`BoardStateKey`): a packed byte encoding of every tube (capacity, clay flag, cover, balls with colour, hidden flag and key) plus the tube count. Interchangeable tubes (same capacity, clay flag, cover kind and id) are sorted first. Compared byte for byte, so a hash collision can never hide an unvisited position.
- **Skipped moves** (each only reaches a position already seen): moves out of a completed tube; pouring a tube that is one revealed run into an equivalent empty tube (same capacity, not clay, not covered); moving the previous move's balls straight back, unless that move revealed a ball, found a key or opened a cover.
- **Speed (Editor):** about 15–25 µs per expanded node. The current levels need under 100 nodes in Fast mode; `level_005` (10 colours, 12 tubes) takes 695 nodes / ~11 ms in Optimal mode.

### Validate Levels (Editor)
**Ball Sort ▸ Validate Levels…** runs, for every level in the `LevelDatabase`: the loader's validation, then for each variant the solver in Fast mode, then in Optimal mode if Fast found a solution (each with an editable node limit). The table has **one row per variant**: level, variant, colours / tubes, the mechanics found in the data, solvable, optimal moves (or `≥ n`), nodes and time, with that variant's errors under it; a level with problems outside its variants (unreadable file, header, grid) gets an extra `file` row. A variant with no problems of its own is still solved when another part of the file is broken. Solving runs on a worker thread behind a cancelable progress bar. **Write meta.optimalMoves** writes each proven optimum into its variant's `meta` by editing the text (formatting and key order are kept; the edit is re-parsed and refused if anything other than that variant's `meta.optimalMoves` would change). Each row's **Play** button opens that level in that variant through the editor level jump (**Ball Sort ▸ Play Level…**); it is enabled only when the whole file is valid, since only a valid file loads.

### Play Level (Editor)
**Ball Sort ▸ Play Level…** opens the Level scene in Play mode on a level of the database or on a level file, in the variant picked with its **Variant** slider (V1–V5). Every such visit is a test: each try (Restart included) plays the picked variant, and winning pays no reward, moves no progress and counts no try. The HUD and the Win panel name the variant, e.g. "Level 6 · V5".
