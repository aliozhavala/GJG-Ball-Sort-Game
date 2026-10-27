# Levels — format, validation, solver and generator

### File format
One JSON file per level: `Assets/Data/Levels/level_001.json` … `level_100.json`, referenced in order by `LevelDatabase`.
Ball arrays run **bottom → top**. `hidden` is optional and parallel to `balls`. `lock` is optional (only if the locked-tube mechanic is adopted; until then strict parsing rejects it).

- `formatVersion` (int, currently 1) at the top level. The format will grow: about 3 more mechanics are planned on top of hidden balls. New mechanics add **optional** fields (per level, per tube, or per ball); an absent field means the mechanic is not used. Bump `formatVersion` only for breaking changes.
- Parse strictly: unknown fields are errors (Newtonsoft `MissingMemberHandling.Error`), so typos in hand-edited levels are caught. An unsupported `formatVersion` is an error.
- `layout.rows` (optional): tube counts per row, top to bottom, e.g. `[4, 3]`. Each row holds 1–6 tubes, rows sum to the tube count, at most 3 rows. If omitted: use the fewest rows with ≤ 6 tubes each, split as evenly as possible, extra tube in the top row.

```json
{
  "formatVersion": 1,
  "id": 37,
  "colors": 6,
  "layout": { "rows": [2, 1] },
  "tubes": [
    { "capacity": 4, "balls": [2, 0, 1, 2], "hidden": [true, true, true, false] },
    { "capacity": 4, "balls": [1, 1, 0, 2] },
    { "capacity": 4, "balls": [] }
  ],
  "meta": { "optimalMoves": 23, "difficulty": 0.54, "mechanics": ["hidden"], "tutorial": false }
}
```

Loader validates: each color's total ball count fills tubes exactly; no tube over capacity; top balls revealed; level solvable (editor-time check).

- "Fills tubes exactly", made precise for variable capacity: every colour `0..colors-1` appears, and a colour with `k` balls needs its own tube of capacity exactly `k` (per capacity `k`: colours with `k` balls ≤ tubes of capacity `k`).
- Required fields carry no parser attributes: a missing one arrives as `0`/`null` and the validator reports it (`formatVersion` missing, `id < 1`, `colors < 1`, `capacity < 1`, `balls` missing — use `[]` for an empty tube).
- `id` is 1-based and matches the file name (`level_007.json` → `7`); an EditMode test checks every shipped file.
- At most 18 tubes (3 rows × 6).
- Opening position (checked only once the structure is valid): no tube already completed, the level not already won, and at least one legal move.
- All problems are reported at once, each as `<level>: <location>: <problem>`, e.g. `level_003: tubes[2]: top ball is hidden`.
- Split: Core owns the DTOs, `LevelValidator`, `LevelLayout` and `LevelFactory` (no serializer dependency); `LevelJsonParser` lives in its own engine-free assembly, `BallSort.LevelJson` (Core + Newtonsoft), so Game, Editor and Tests share it without depending on each other.

### Solver and generator
- **Solver** (Core): BFS / A* over board states using the **same `MoveRules` as the game** (single source of truth), with full knowledge of hidden colors but honoring hidden flags for run building.
  - Canonical state hash: sort tubes that are interchangeable (same capacity and rules) before hashing.
  - Pruning: never move out of a completed tube; never move a single-color tube's entire content into an empty tube; never immediately reverse the previous move.
- **Generator** (Editor window): parameters (colors, capacity, empty tubes, mechanics) → random fill → solve → reject unsolvable/trivial → score difficulty → export JSON.
  - Difficulty signals: optimal move count, states explored, dead-end ratio, number of empty tubes, hidden-ball count.
- Product teammate curates the final 100 and hand-authors tutorial levels for each mechanic.
