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

Loader validates the format (below) and every completion, cover, clay, large-tube and grid rule in docs/mechanics.md. Solvability is an editor-time check.

- Required fields carry no parser attributes: a missing one arrives as `0`/`null` and the validator reports it (`formatVersion` missing, `id < 1`, `colors < 1`, `capacity < 1`, `balls` missing — use `[]` for an empty tube). Required ints for which 0 is a valid value (`veil.color`, a key's `slot` and `lock`, a lock's `id`) are nullable in the DTOs, so a missing one is reported rather than read as 0.
- `id` is 1-based and matches the file name (`level_007.json` → `7`); an EditMode test checks every shipped file.
- No tube over capacity; every tube's top ball starts revealed.
- Opening position (checked only once the structure is valid): no tube already completed, the level not already won, and at least one legal move.
- All problems are reported at once, each as `<level>: <location>: <problem>`, e.g. `level_003: tubes[2]: top ball is hidden`.
- Split: Core owns the DTOs, `LevelValidator`, `LevelLayout` and `LevelFactory` (no serializer dependency); `LevelJsonParser` lives in its own engine-free assembly, `BallSort.LevelJson` (Core + Newtonsoft), so Game, Editor and Tests share it without depending on each other.

Current files are placeholders until the generator lands: `level_001`–`level_005` are plain, `level_006` (veil),
`level_007` (lock and key) and `level_008` (clay and a capacity-10 tube spanning 2 rows) are hand-made mechanic tests,
each played to a win by a scripted solution in `ShippedLevelTests`. The former stuck trap is the test fixture
`TestLevels.StuckTrap()`.

### Solver and generator
- **Solver** (Core): BFS / A* over board states using the **same `MoveRules` as the game** (single source of truth), with full knowledge of hidden colors but honoring hidden flags for run building.
  - Search on clones (`BoardState.Clone`), never by undo: undo keeps reveals, opened covers and found keys, so it cannot backtrack.
  - State to compare: every tube's balls bottom → top (colour, hidden flag, key) and its cover; capacity and clay are fixed per tube. The grid never affects legality.
  - Canonical state hash: sort tubes that are interchangeable (same capacity, clay flag and cover) before hashing.
  - Pruning: never move out of a completed tube; never move a single-color tube's entire content into an empty tube; never immediately reverse the previous move.
- **Generator** (Editor window): parameters (colors, capacity, empty tubes, mechanics) → random fill → solve → reject unsolvable/trivial → score difficulty → export JSON.
  - Difficulty signals: optimal move count, states explored, dead-end ratio, number of empty tubes, hidden-ball count.
- Product teammate curates the final 100 and hand-authors tutorial levels for each mechanic.
