# Levels — format, validation, solver and generator

### File format
One JSON file per level: `Assets/Data/Levels/level_001.json` … `level_100.json`, referenced in order by `LevelDatabase`.
Ball arrays run **bottom → top**. This file is the schema; the rules and validation behind each field are in docs/mechanics.md.

- `formatVersion` (int, currently 1) at the top level. Each mechanic (hidden balls, veil, lock and key, clay, large tubes) uses **optional** fields (per level, per tube, or per ball); an absent field means the mechanic is not used. Bump `formatVersion` only for breaking changes.
- Parse strictly: unknown fields are errors (Newtonsoft `MissingMemberHandling.Error`), so typos in hand-edited levels are caught. An unsupported `formatVersion` is an error.
- `layout.grid` (required): rows of cells, top row first; each cell is a tube index or `null`. At most 3 rows and 6 cells per row. A large tube is listed in every row it spans, in the same cell.
- Per tube: `capacity` and `balls` (required); optional `hidden` (parallel to `balls`), `veil: { "color": c }` (a ball colour id), `clay: true`, `keys: [{ "slot": s, "lock": id }]` (`slot` indexes `balls`).
- Top level: optional `locks: [{ "id": id, "tubes": [i, j, …] }]` (`id` an integer ≥ 0; it also picks the lock's colour). `meta` (optional) never affects gameplay; `meta.mechanics` names are `hidden`, `veil`, `lock`, `clay`, `large`.

```json
{
  "formatVersion": 1,
  "id": 37,
  "colors": 3,
  "layout": { "grid": [[0, 1, 2], [0, 3, 4]] },
  "tubes": [
    { "capacity": 5, "balls": [0], "clay": true },
    { "capacity": 4, "balls": [1, 2, 0, 0], "hidden": [true, false, false, false], "keys": [{ "slot": 1, "lock": 0 }] },
    { "capacity": 4, "balls": [2, 0, 2, 2], "veil": { "color": 1 } },
    { "capacity": 4, "balls": [1, 1, 1, 0] },
    { "capacity": 4, "balls": [] }
  ],
  "locks": [{ "id": 0, "tubes": [3, 4] }],
  "meta": { "mechanics": ["hidden", "veil", "lock", "clay", "large"], "tutorial": false }
}
```

Loader validates the format (below) and every completion, cover, clay, large-tube and grid rule in docs/mechanics.md. Solvability is an editor-time check (Validate Levels and `ShippedLevelTests`, below); it also catches what the rules can't, e.g. two locks whose keys sit inside each other.

- Required fields carry no parser attributes: a missing one arrives as `0`/`null` and the validator reports it (`formatVersion` missing, `id < 1`, `colors < 1`, `capacity < 1`, `balls` missing — use `[]` for an empty tube). Required ints for which 0 is a valid value (`veil.color`, a key's `slot` and `lock`, a lock's `id`) are nullable in the DTOs, so a missing one is reported rather than read as 0.
- `id` is 1-based and matches the file name (`level_007.json` → `7`); an EditMode test checks every shipped file.
- No tube over capacity; every tube's top ball starts revealed.
- Opening position (checked only once the structure is valid): no tube already completed, the level not already won, and at least one legal move.
- All problems are reported at once, each as `<level>: <location>: <problem>`, e.g. `level_003: tubes[2]: top ball is hidden`.
- Split: Core owns the DTOs, `LevelValidator`, `LevelLayout` and `LevelFactory` (no serializer dependency); `LevelJsonParser` lives in its own engine-free assembly, `BallSort.LevelJson` (Core + Newtonsoft), so Game, Editor and Tests share it without depending on each other.

Current files are placeholders until the generator lands: `level_001`–`level_005` are plain, `level_006` (veil),
`level_007` (lock and key) and `level_008` (clay and a capacity-10 tube spanning 2 rows) are hand-made mechanic tests.
`ShippedLevelTests` solves every file with the solver and plays the solution to a win; the mechanic levels also keep
a scripted solution that checks their mechanic fires. The former stuck trap is the test fixture `TestLevels.StuckTrap()`.

### Solver
`BoardSolver.Solve(board, mode, nodeLimit, cancellationToken)` in `Core/Solver/`; pure C#, safe on a worker thread (it copies the board on entry).
- **Real rules:** moves come from `MoveRules` and are applied with the board's own `ApplyMove` cascade (reveal, completion, veils, keys, locks) on copies, never by undo (undo keeps reveals, opened covers and found keys, so it cannot backtrack). Full knowledge of hidden colours, but hidden flags still block runs.
- **Modes:** `Optimal` = A* with an admissible, consistent heuristic (the argument is in `SolverHeuristic`: blocks above the bottom of a tube, plus surplus bottom blocks per colour); `Fast` = weighted A* (g + 3h), usually short but not guaranteed shortest. Both prove `Unsolvable` exactly when the search runs out.
- **Result:** `Solved` / `Unsolvable` / `Unknown` (node limit hit), the moves (replayed and checked before returning), whether optimal, a proven lower bound (`≥ n` when Optimal stops at its limit), nodes expanded, largest frontier, elapsed time.
- **Position key** (`BoardStateKey`): a packed byte encoding of every tube (capacity, clay flag, cover, balls with colour, hidden flag and key) plus the tube count. Interchangeable tubes (same capacity, clay flag, cover kind and id) are sorted first. Compared byte for byte, so a hash collision can never hide an unvisited position.
- **Skipped moves** (each only reaches a position already seen): moves out of a completed tube; pouring a tube that is one revealed run into an equivalent empty tube (same capacity, not clay, not covered); moving the previous move's balls straight back, unless that move revealed a ball, found a key or opened a cover.
- **Speed (Editor):** about 15–25 µs per expanded node. The current levels need under 100 nodes in Fast mode; `level_005` (10 colours, 12 tubes) takes 695 nodes / ~11 ms in Optimal mode.

### Validate Levels (Editor)
**Ball Sort ▸ Validate Levels…** runs, for every level in the `LevelDatabase`: the loader's validation, the solver in Fast mode, then in Optimal mode if Fast found a solution (each with an editable node limit). The table shows level, colours / tubes, the mechanics found in the data, solvable, optimal moves (or `≥ n`), nodes and time. Solving runs on a worker thread behind a cancelable progress bar. **Write meta.optimalMoves** writes each proven optimum into its file by editing the text (formatting and key order are kept; the edit is re-parsed and refused if anything else would change).

### Generator
- **Generator** (Editor window): parameters (colors, capacity, empty tubes, mechanics) → random fill → solve → reject unsolvable/trivial → score difficulty → export JSON.
  - Difficulty signals: optimal move count, states explored, dead-end ratio, number of empty tubes, hidden-ball count.
- Product teammate curates the final 100 and hand-authors tutorial levels for each mechanic.
