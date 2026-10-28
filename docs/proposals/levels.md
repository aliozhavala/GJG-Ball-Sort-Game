# Levels — format, validation, solver and generator

> **[+P] = product addition** on top of the developer's version (commit `4f77fb8`, 28 Oct 2026). Text without the mark is the developer's, unchanged. Product additions only add: optional fields, the variant/retry system, generator details and the 100-level plan. Everything here uses this repo's terms. The two large clay forms are told apart by their skin: **ice skin** (capacity 8, starts with 2–4 balls of its colour) and **big-bottle skin** (capacity 8 or 12, starts with 1 ball). A **large-clay colour** is the 8- or 12-ball colour that fills one. Art / prototype names are mapped in `docs/mechanics.md` → "Product names → code names".

### File format
One JSON file per level: `Assets/Data/Levels/level_001.json` … `level_100.json`, referenced in order by `LevelDatabase`.
Ball arrays run **bottom → top**. This file is the schema; the rules and validation behind each field are in docs/mechanics.md.

- `formatVersion` (int, currently 1) at the top level. Each mechanic (hidden balls, veil, lock and key, clay, large tubes) uses **optional** fields (per level, per tube, or per ball); an absent field means the mechanic is not used. Bump `formatVersion` only for breaking changes.
- Parse strictly: unknown fields are errors (Newtonsoft `MissingMemberHandling.Error`), so typos in hand-edited levels are caught. An unsupported `formatVersion` is an error.
- `layout.grid` (required): rows of cells, top row first; each cell is a tube index or `null`. At most 3 rows and 6 cells per row. A large tube is listed in every row it spans, in the same cell.
- Per tube: `capacity` and `balls` (required); optional `hidden` (parallel to `balls`), `veil: { "color": c }` (a ball colour id), `clay: true`, `keys: [{ "slot": s, "lock": id }]` (`slot` indexes `balls`).
- Top level: optional `locks: [{ "id": id, "tubes": [i, j, …] }]` (`id` an integer ≥ 0; it also picks the lock's colour). `meta` (optional) never affects gameplay; `meta.mechanics` names are `hidden`, `veil`, `lock`, `clay`, `large`.
- **[+P] Proposed optional fields.** All optional, so by the rule above no `formatVersion` bump is needed. Strict parsing means the DTOs have to list them.
  - Top level `tier`: `"hard"` or `"superHard"`; absent = regular. It drives the Play-button tag, the win reward and DLB (`docs/screens.md`). Rule: **Hard = levels ending in 4** (14, 24 … 94), **Super Hard = levels ending in 9** (19, 29 … 99) **and 100**. It's stored rather than computed so that product can move one.
  - Per tube `skin`: `"bigBottle"` on a large clay tube that should be drawn with the big-bottle skin. Absent on a clay tube → ice skin. View-only.
  - `meta.solution`: `[[from, to], …]`, a solver solution as tube-index moves. Used by the CI replay, the editor auto-play and QA.
  - `meta.optimalMoves` (int), `meta.botWinRate` (0–1), `meta.bucket` (1–7), `meta.seed` (int), `meta.variant` (`"v1"` … `"v5"`, `"dlb1"` … `"dlb3"`). Written by the generator, read only by tools and the Win screen's grade (see `docs/screens.md`).

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

**[+P]** The same shape with the proposed fields (a regular level with a big-bottle-skin large clay tube; a Hard level would add `"tier": "hard"`; meta values illustrative, solution shortened):

```json
{
  "formatVersion": 1,
  "id": 45,
  "colors": 3,
  "layout": { "grid": [[1, 0, 2], [3, 0, 4]] },
  "tubes": [
    { "capacity": 8, "balls": [0], "clay": true, "skin": "bigBottle" },
    { "capacity": 4, "balls": [1, 0, 2, 0] },
    { "capacity": 4, "balls": [0, 2, 1, 0], "hidden": [true, true, false, false] },
    { "capacity": 4, "balls": [2, 0, 1, 0] },
    { "capacity": 4, "balls": [1, 2, 0] }
  ],
  "meta": {
    "mechanics": ["hidden", "clay", "large"], "tutorial": false,
    "variant": "v1", "seed": 45017, "optimalMoves": 14, "botWinRate": 0.88, "bucket": 2,
    "solution": [[1, 0], [2, 0]]
  }
}
```

Loader validates the format (below) and every completion, cover, clay, large-tube and grid rule in docs/mechanics.md. Solvability is an editor-time check.

- Required fields carry no parser attributes: a missing one arrives as `0`/`null` and the validator reports it (`formatVersion` missing, `id < 1`, `colors < 1`, `capacity < 1`, `balls` missing — use `[]` for an empty tube). Required ints for which 0 is a valid value (`veil.color`, a key's `slot` and `lock`, a lock's `id`) are nullable in the DTOs, so a missing one is reported rather than read as 0.
- `id` is 1-based and matches the file name (`level_007.json` → `7`); an EditMode test checks every shipped file.
- No tube over capacity; every tube's top ball starts revealed.
- Opening position (checked only once the structure is valid): no tube already completed, the level not already won, and at least one legal move.
- All problems are reported at once, each as `<level>: <location>: <problem>`, e.g. `level_003: tubes[2]: top ball is hidden`.
- Split: Core owns the DTOs, `LevelValidator`, `LevelLayout` and `LevelFactory` (no serializer dependency); `LevelJsonParser` lives in its own engine-free assembly, `BallSort.LevelJson` (Core + Newtonsoft), so Game, Editor and Tests share it without depending on each other.
- **[+P] Checks for the proposed fields:** `tier` is one of the two values; `skin` only on a clay tube; if `meta.solution` is present, replaying it with the real `MoveRules` wins the level (CI test).
- **[+P] Product design rules worth a warning** (editor validator, not the loader; the generator already refuses them): a capacity-4 clay tube together with a large clay tube; both large clay skins in one level; a big-bottle-skin tube on a `tier` level; a key on a hidden ball or inside a veiled tube; a veiled tube holding a veil colour or a large-clay colour; fewer than 2 filled tubes without a cover.

Current files are placeholders until the generator lands: `level_001`–`level_005` are plain, `level_006` (veil),
`level_007` (lock and key) and `level_008` (clay and a capacity-10 tube spanning 2 rows) are hand-made mechanic tests,
each played to a win by a scripted solution in `ShippedLevelTests`. The former stuck trap is the test fixture
`TestLevels.StuckTrap()`.

### [+P] Variants, retries and DLB
Each level ships several solver-verified **variants** of the same level, so a retry feels fresh (the Magic Sort pattern; product D18, D25).

- **Same structure, new deal.** All variants of a level share the tube count, capacities, `layout.grid`, mechanics, cover/lock/clay placement and `tier`. Ball colours and the shuffle differ.
- **How many:**
  - L1–4: 1 variant.
  - Every other level: **5 variants**.
  - Hard / Super Hard levels: also **3 DLB variants** (much easier, same structure).
- **Files (proposal):** `level_044.json` stays variant 1, so nothing changes for a level without variants. Extra variants go next to it as `level_044_v2.json` … `level_044_v5.json` and `level_044_dlb1.json` … `level_044_dlb3.json`, each with `"id": 44`. `LevelDatabase` keeps one entry per level holding its variant list, its DLB list and `dlbCap`. The id-vs-file-name test reads the number before the suffix.
- **Difficulty of each slot** (target = the level's bot win rate from the plan below):

  | Slot | Played on | Difficulty |
  |---|---|---|
  | v1 | first attempt | target ± 5 points |
  | v2 | retry 1 | target ± 5 points |
  | v3–v5 | retry 2, 3, 4 | **eased**: target + 8 points (bot wins more; product tunes 5–10) |
  | dlb1–3 | Hard / Super Hard after `dlbCap` losses | target + 25 points |

- **Runtime selection** (save `retryCount[level]`: +1 every time the player restarts or gives up after at least one move; reset to 0 on a win):
  ```
  if (level has DLB && retry >= dlbCap)  use dlb[retry - dlbCap], or a random DLB variant != last played once the list runs out
  else if (retry < variants.Count)      use variants[retry]
  else                                  use a random eased variant (v3–v5) != last played
  ```
- `dlbCap` by episode: 2, 2, 3, 4, 4 (episode 1 … 5). Tune after playtests.
- The Level scene still "always loads `PlayerData.CurrentLevel`"; the variant pick happens inside that load.
- CI: every variant and every DLB variant is a shipped level, so each one is validated and solver-checked: about 100 × 5 + 18 × 3 ≈ 550 files.

### Solver and generator
- **Solver** (Core): BFS / A* over board states using the **same `MoveRules` as the game** (single source of truth), with full knowledge of hidden colors but honoring hidden flags for run building.
  - Search on clones (`BoardState.Clone`), never by undo: undo keeps reveals, opened covers and found keys, so it cannot backtrack.
  - State to compare: every tube's balls bottom → top (colour, hidden flag, key) and its cover; capacity and clay are fixed per tube. The grid never affects legality.
  - Canonical state hash: sort tubes that are interchangeable (same capacity, clay flag and cover) before hashing.
  - Pruning: never move out of a completed tube; never move a single-color tube's entire content into an empty tube; never immediately reverse the previous move.
  - **[+P] Clay exception to the second pruning rule:** pouring a single-colour tube into an **empty clay** tube is a real move, not a mirror of the board (the balls can never come back out), so don't prune it.
  - **[+P] What worked in the prototype's A\*** (ported from octo/watersort, the cleanest reference to port to C#):
    - Admissible heuristic `h = Σ_tubes (runs − 1) + Σ_colours (tubes with that colour at the bottom − 1)`, plus 1 per covered tube.
    - Move ordering: moves that complete a tube first, then moves onto a same-colour top, then partial moves, and moves into an empty tube last.
    - When several empty tubes are interchangeable, try only the first.
    - Treat "node limit hit" as **unknown**, and reject the deal in the generator (never ship an unknown).
    - Cost: the heaviest product boards (veils + locks + a large clay tube, 15 tubes) took up to ~100 s per solvable deal in the JS prototype. Run the generator off the main thread.
- **Generator** (Editor window): parameters (colors, capacity, empty tubes, mechanics) → random fill → solve → reject unsolvable/trivial → score difficulty → export JSON.
  - Difficulty signals: optimal move count, states explored, dead-end ratio, number of empty tubes, hidden-ball count.
  - **[+P] Recipe parameters** (one recipe per level; the values for all 100 are in the plan table below):

    | Param | Meaning |
    |---|---|
    | `colors` | number of colours (large-clay colours count here too) |
    | `empty` | free room, in empty tubes' worth (`empty × 4` free slots) |
    | `fill` | `full` (full tubes + `empty` empty tubes), `spread` (no empty tube; the free slots are 1–2 missing balls spread over the tubes), `mixed` (1 empty tube, the rest spread) |
    | `hidden` | share of non-top balls that start hidden, **capped at 65%** (product D26) |
    | `veils`, `veilColours` | number of veiled tubes, number of veil colours (1–3) |
    | `locks`, `lockWidth` | number of locks, tubes per lock (1–3) |
    | `clayTubes` | capacity-4 clay tubes that start empty |
    | `iceLargeClay` | large clay tubes, ice skin: capacity 8, start with 2–4 balls of their colour |
    | `bottleLargeClay`, `bottleCapacity` | 0–2 large clay tubes, big-bottle skin: capacity 8 or 12, start with 1 ball of their colour |
    | `rowShape`, `tallSide` | grid look: auto / three / diamond / pyramid / flip; tall tubes centre / left / right (`docs/mechanics.md` → Board layout) |
    | `tier`, `target` | regular / hard / superHard; target bot win rate |

  - **[+P] Deal, step by step** (the prototype's `generate()`, which has been verified on thousands of deals):
    1. Pick the colours. Each large clay tube gets its own large-clay colour with as many balls as its capacity (8 or 12); the rest get 4. Shuffle all balls.
    2. Take the starting fill out: 2–4 balls of the colour for each ice-skin tube, 1 ball for each big-bottle-skin tube.
    3. **Veils:** the first `veils` tubes are filled only with plain colours (no veil colour, no large-clay colour), so every veil colour stays completable from the open tubes. Veil colours are handed out round-robin (4 veils, 3 colours → 2 + 1 + 1).
    4. Fill the rest of the tubes per `fill`. Spread never takes balls from veiled tubes. Re-deal if any tube starts completed.
    5. **Locks:** the next `locks × lockWidth` filled tubes get the locks. Each key goes on a ball of an open tube (not locked, not veiled, not clay) **below that tube's opening top run**, never in it (validation: no key in a selectable run at start), weighted by depth under the run: 3 / 2 / 1.
    6. Add empty tubes, capacity-4 clay tubes, then the large clay tubes with their starting fill.
    7. **Hidden:** each non-top ball becomes hidden with probability `hidden`, except key balls and anything in clay.
    8. Build `layout.grid` from `rowShape` / `tallSide` and the symmetric-slot rules (`docs/mechanics.md` → Board layout), within the 3-row / 6-cell limit.
    9. Solve. Unsolvable or unknown → next seed (40 tries; then retry with one more empty tube).
  - **[+P] Refuse impossible recipes with a reason** instead of looping:
    - Veils + lock tubes would leave fewer than 2 open filled tubes.
    - Not enough plain colours to fill the veiled tubes.
    - Large-clay colours + veil colours need more colours than the level has.
    - A capacity-4 clay tube with any large clay tube; both large clay skins in one level (`docs/mechanics.md` → Clay).
  - **[+P] Difficulty = a bot that plays like a casual player.** This replaced the composite score in practice: it is the best proxy for felt difficulty with hidden balls and mechanics.
    - It sees only revealed balls (a hidden ball is unknown, and scoring a move never peeks at the ball it uncovers). No undo, no boosters.
    - Each turn it scores every legal move one step ahead and picks the best plus Gumbel noise. Bonus for a move that digs towards a visible key; −6 for pouring into an empty clay tube, and an empty clay tube doesn't count as free room.
    - Three skills, 20 plays each, and the win rate is the mean:

      | Skill | Noise | Lookahead |
      |---|---|---|
      | careless | 7 | none |
      | casual | 4 | avoids immediate dead ends |
      | careful | 2 | 2-ply |
    - Win rate → bucket 1–7 (for reports): ≥90% → 1, ≥75% → 2, ≥60% → 3, ≥45% → 4, ≥30% → 5, ≥15% → 6, else 7.
    - Write the result into `meta.botWinRate` / `meta.bucket`.
  - **[+P] Hitting the target.** Product draws a **target bot win rate per level** (product D24; the default curve and the saved values are in the plan table). A candidate is accepted within **±5 points**.
    - Search loop: generate 12 candidates per round, up to 6 rounds. If they're too easy, step the recipe harder: more colours first (up to 10), then more hidden (up to the 65% cap), then one empty tube fewer. With a big-bottle-skin tube on the board, drop an empty tube first (it soaks up a whole colour). Too hard → the reverse.
    - **No move cap** (product D27): long levels are fine.
    - Tier targets (product D23): Hard ≈ bot wins 30–45% (D5), Super Hard ≈ 15–30% (D6), L19 (the first Super Hard) at D5. D7 (bot almost never wins) feels like a wall, so don't go there.
  - **[+P] Variants from the candidates:** v1 keeps the generated colours; every other variant is recoloured from the first `colors + 3` palette slots. Variants must be distinct under the structure-aware canonical key (a pure colour swap of another variant counts as the same layout; it's only allowed as an eased copy when no other candidate fits).
- Product teammate curates the final 100 and hand-authors tutorial levels for each mechanic.
- **[+P] Where product is now** (prototype `prototype/potion-sort-lab.html` → Level Design tab, and `tools/levelgen/` in Node; same engine as the game, so they can't disagree on rules):
  - The plan for all 100 levels is set (table below).
  - **Layout approved** for L1–23 and L28; the others are still under review.
  - **Final variants** (5, plus 3 DLB on tier levels) are built for 17 levels: 1, 3, 4, 6, 7, 11, 13, 14, 16, 17, 19, 23, 26, 27, 44, 49, 51. The other levels carry 1 verified stop-gap variant.
  - The prototype follows this repo's rules: keys open when they reach the selectable run, a tube with a hidden ball isn't completed, and grids stay within 3 rows × 6 cells. Every stored solution was re-solved under these rules. A few variants now rate outside their ±5 band and will be regenerated before export.
  - Every layout in the prototype passes its verifier (counts per colour, solution replay, replay in the real game logic with no false "stuck").
  - Product can export these levels to this JSON format (grid included) if that saves generator time: ask.

### [+P] The 100-level plan
**Episodes** (20 levels each; new background per episode): Classic Lab 1–19 · Lava Lab 20–39 · Beach Lab 40–59 · Arctic Lab 60–79 · Space Lab 80–100.

**Mechanic unlocks** (product D32): hidden **7** · large clay, big-bottle skin **20** · veil **30** · capacity-4 clay **55** · lock **80**. Arctic Lab has no new mechanic (large clay with ice skin from 61, clay + veil combos from 66).

**Rhythm:**
- **Hard** on every level ending in 4, **Super Hard** on every level ending in 9, plus L100 (the finale, everything combined). So each episode ends on its peak (19, 39, 59, 79, 99).
- Levels ending in 0 are breathers.
- **Intro block:** a new mechanic is shown alone, without hidden balls, for 4 levels (big-bottle-skin large clay 3: 20–22; veil 30–33; clay 55–58; lock 80–83). Hidden comes back on the next tier level (34, 59, 84).
- First 2-mechanic combos about 11 levels after an intro (veil + big-bottle-skin large clay at 41, clay + veil at 66). 3-mechanic combos only on tier levels and the finale.
- Never a big-bottle-skin tube on a tier level; never a capacity-4 clay tube or an ice-skin tube together with a big-bottle-skin tube.
- Fill styles: full up to L14 and on intro / breather levels, then partial (spread or mixed) on ~30% of episode-2 levels and ~50% later.
- A level never looks the same as either of the two before it when it has the same mechanics.

**Coverage** (levels containing each): hidden 77 · large clay, big-bottle skin 22 · veil 35 · other clay 27 (capacity-4 in 16, ice-skin large in 11) · lock 17 · basic 6. Mechanics per level (hidden aside): 0 → 27 levels, 1 → 48, 2 → 22, 3 → 3.

How to read the table:
- ★ marks a level with a tutorial toast (text in `docs/mechanics.md`).
- "Hidden" is the share of non-top balls.
- **Target** is the bot win rate the variants are tuned to: **bold** = product has saved this value; plain = the default curve, product may still change it.
- "Variants ready" counts the verified layouts in the prototype.

| # | Ep | Tier | Colours | Empty | Hidden | Mechanics (dev terms) | Fill | Row shape | Tall side | Target: bot wins % | Variants ready | Layout reviewed |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 ★ | 1 Classic | Regular | 2 | 1 | – | basic | full | auto | – | **97** | 1 | ✓ |
| 2 | 1 Classic | Regular | 3 | 1 | – | basic | full | flip | – | 97 | 1 (stop-gap) | ✓ |
| 3 | 1 Classic | Regular | 3 | 2 | – | basic | full | auto | – | **97** | 1 | ✓ |
| 4 | 1 Classic | Regular | 4 | 2 | – | basic | full | auto | – | **97** | 1 | ✓ |
| 5 | 1 Classic | Regular | 4 | 2 | – | basic | full | flip | – | 94 | 1 (stop-gap) | ✓ |
| 6 | 1 Classic | Regular | 5 | 2 | – | basic | full | auto | – | **94** | 5 | ✓ |
| 7 ★ | 1 Classic | Regular | 4 | 2 | 35% | hidden | full | auto | – | **93** | 5 | ✓ |
| 8 | 1 Classic | Regular | 4 | 2 | 36% | hidden | full | flip | – | 93 | 1 (stop-gap) | ✓ |
| 9 | 1 Classic | Regular | 5 | 2 | 38% | hidden | full | three | – | 93 | 1 (stop-gap) | ✓ |
| 10 | 1 Classic | Regular | 5 | 2 | 39% | hidden | full | flip | – | 97 | 1 (stop-gap) | ✓ |
| 11 | 1 Classic | Regular | 5 | 2 | 40% | hidden | full | auto | – | **92** | 5 | ✓ |
| 12 | 1 Classic | Regular | 5 | 2 | 41% | hidden | full | flip | – | 92 | 1 (stop-gap) | ✓ |
| 13 | 1 Classic | Regular | 6 | 2 | 43% | hidden | full | auto | – | **90** | 5 | ✓ |
| 14 | 1 Classic | **Hard** | 7 | 2 | 65% | hidden | full | diamond | – | **56** | 5 + 3 DLB | ✓ |
| 15 | 1 Classic | Regular | 6 | 2 | 45% | hidden | full | flip | – | 91 | 1 (stop-gap) | ✓ |
| 16 | 1 Classic | Regular | 6 | 2 | 46% | hidden | spread | auto | – | **91** | 5 | ✓ |
| 17 | 1 Classic | Regular | 7 | 2 | 48% | hidden | full | auto | – | **90** | 5 | ✓ |
| 18 | 1 Classic | Regular | 7 | 2 | 49% | hidden | full | flip | – | 91 | 1 (stop-gap) | ✓ |
| 19 | 1 Classic | **Super Hard** | 9 | 2 | 65% | hidden | mixed | pyramid | – | **42** | 5 + 3 DLB | ✓ |
| 20 ★ | 2 Lava | Regular | 4 | 2 | – | 1× large clay 8 (big-bottle skin) | full | pyramid | left | 96 | 1 (stop-gap) | ✓ |
| 21 | 2 Lava | Regular | 4 | 2 | – | 1× large clay 8 (big-bottle skin) | full | flip | centre | 96 | 1 (stop-gap) | ✓ |
| 22 | 2 Lava | Regular | 4 | 2 | – | 1× large clay 8 (big-bottle skin) | full | auto | right | 96 | 1 (stop-gap) | ✓ |
| 23 | 2 Lava | Regular | 5 | 2 | 45% | hidden | full | three | – | **89** | 5 | ✓ |
| 24 | 2 Lava | **Hard** | 7 | 2 | 65% | hidden | spread | flip | – | 42 | 1 (stop-gap) |  |
| 25 | 2 Lava | Regular | 6 | 2 | 45% | hidden | full | flip | – | 89 | 1 (stop-gap) |  |
| 26 | 2 Lava | Regular | 7 | 1 | 65% | hidden, 1× large clay 8 (big-bottle skin) | full | auto | centre | **89** | 5 |  |
| 27 | 2 Lava | Regular | 6 | 2 | 45% | hidden | full | auto | – | **88** | 5 |  |
| 28 | 2 Lava | Regular | 6 | 2 | – | 2× large clay 8 (big-bottle skin) | mixed | flip | centre | 88 | 1 (stop-gap) | ✓ |
| 29 | 2 Lava | **Super Hard** | 8 | 2 | 65% | hidden | full | flip | – | 27 | 1 (stop-gap) |  |
| 30 ★ | 2 Lava | Regular | 5 | 2 | – | 2 veils / 1 colour | full | pyramid | – | 94 | 1 (stop-gap) |  |
| 31 | 2 Lava | Regular | 6 | 2 | – | 2 veils / 2 colours | full | auto | – | 93 | 1 (stop-gap) |  |
| 32 | 2 Lava | Regular | 6 | 2 | – | 3 veils / 2 colours | full | auto | – | 93 | 1 (stop-gap) |  |
| 33 | 2 Lava | Regular | 6 | 2 | – | 3 veils / 3 colours | full | flip | – | 93 | 1 (stop-gap) |  |
| 34 | 2 Lava | **Hard** | 8 | 2 | 60% | hidden, 5 veils / 3 colours | full | auto | – | 41 | 1 (stop-gap) |  |
| 35 | 2 Lava | Regular | 7 | 2 | 45% | hidden, 2 veils / 1 colour | mixed | pyramid | – | 86 | 1 (stop-gap) |  |
| 36 | 2 Lava | Regular | 8 | 2 | 45% | hidden, 1× large clay 8 (big-bottle skin) | spread | pyramid | left | 86 | 1 (stop-gap) |  |
| 37 | 2 Lava | Regular | 8 | 2 | 45% | hidden, 4 veils / 2 colours | mixed | auto | – | 86 | 1 (stop-gap) |  |
| 38 | 2 Lava | Regular | 8 | 2 | 45% | hidden, 5 veils / 3 colours | spread | auto | – | 86 | 1 (stop-gap) |  |
| 39 | 2 Lava | **Super Hard** | 10 | 2 | 65% | hidden, 3 veils / 1 colour | full | flip | – | 26 | 1 (stop-gap) |  |
| 40 | 3 Beach | Regular | 6 | 2 | 50% | hidden, 2× large clay 8 (big-bottle skin) | full | pyramid | centre | 91 | 1 (stop-gap) |  |
| 41 | 3 Beach | Regular | 6 | 2 | 50% | hidden, 1× large clay 12 (big-bottle skin), 3 veils / 2 colours | mixed | three | left | 85 | 1 (stop-gap) |  |
| 42 | 3 Beach | Regular | 6 | 2 | 50% | hidden, 3 veils / 3 colours | spread | flip | – | 85 | 1 (stop-gap) |  |
| 43 | 3 Beach | Regular | 6 | 2 | 50% | hidden, 1× large clay 12 (big-bottle skin) | full | three | centre | 84 | 1 (stop-gap) |  |
| 44 | 3 Beach | **Hard** | 8 | 2 | 65% | hidden, 3 veils / 1 colour | full | auto | – | **41** | 5 + 3 DLB |  |
| 45 | 3 Beach | Regular | 7 | 2 | – | 1× large clay 12 (big-bottle skin) | spread | auto | centre | 84 | 1 (stop-gap) |  |
| 46 | 3 Beach | Regular | 7 | 2 | 50% | hidden, 4 veils / 3 colours | mixed | flip | – | 84 | 1 (stop-gap) |  |
| 47 | 3 Beach | Regular | 7 | 2 | 50% | hidden, 1× large clay 12 (big-bottle skin), 3 veils / 3 colours | full | pyramid | centre | 83 | 1 (stop-gap) |  |
| 48 | 3 Beach | Regular | 7 | 2 | 50% | hidden, 4 veils / 2 colours | mixed | auto | – | 83 | 1 (stop-gap) |  |
| 49 | 3 Beach | **Super Hard** | 9 | 2 | 65% | hidden, 3 veils / 1 colour | full | auto | – | **26** | 5 + 3 DLB |  |
| 50 | 3 Beach | Regular | 8 | 2 | 50% | hidden | full | pyramid | – | 89 | 1 (stop-gap) |  |
| 51 | 3 Beach | Regular | 10 | 2 | 65% | hidden, 1× large clay 12 (big-bottle skin), 2 veils / 1 colour | spread | pyramid | centre | **85** | 5 |  |
| 52 | 3 Beach | Regular | 7 | 2 | 38% | hidden, 3 veils / 2 colours | full | auto | – | 82 | 1 (stop-gap) |  |
| 53 | 3 Beach | Regular | 10 | 1 | 65% | hidden, 2× large clay 8 (big-bottle skin) | full | pyramid | centre | 82 | 1 (stop-gap) |  |
| 54 | 3 Beach | **Hard** | 9 | 2 | 53% | hidden, 2 veils / 1 colour | spread | auto | – | 39 | 1 (stop-gap) |  |
| 55 ★ | 3 Beach | Regular | 6 | 2 | – | 1× clay 4 | full | three | – | 87 | 1 (stop-gap) |  |
| 56 | 3 Beach | Regular | 8 | 2 | – | 1× clay 4 | full | pyramid | – | 87 | 1 (stop-gap) |  |
| 57 | 3 Beach | Regular | 8 | 2 | – | 1× clay 4 | full | diamond | – | 87 | 1 (stop-gap) |  |
| 58 | 3 Beach | Regular | 8 | 2 | – | 2× clay 4 | full | flip | – | 87 | 1 (stop-gap) |  |
| 59 | 3 Beach | **Super Hard** | 10 | 2 | 65% | hidden, 2× clay 4 | full | auto | – | 24 | 1 (stop-gap) |  |
| 60 | 4 Arctic | Regular | 7 | 2 | 55% | hidden, 2× clay 4 | full | flip | – | 86 | 1 (stop-gap) |  |
| 61 | 4 Arctic | Regular | 7 | 2 | 55% | hidden, 2× clay 4 | full | three | – | 80 | 1 (stop-gap) |  |
| 62 | 4 Arctic | Regular | 7 | 2 | 55% | hidden, 2× clay 4 | full | auto | – | 80 | 1 (stop-gap) |  |
| 63 | 4 Arctic | Regular | 7 | 2 | 55% | hidden, 1× large clay 12 (big-bottle skin) | full | auto | centre | 79 | 1 (stop-gap) |  |
| 64 | 4 Arctic | **Hard** | 8 | 2 | 65% | hidden, 1× large clay 8 (ice skin) | mixed | diamond | centre | 37 | 1 (stop-gap) |  |
| 65 | 4 Arctic | Regular | 8 | 2 | 55% | hidden, 2× clay 4 | mixed | flip | – | 79 | 1 (stop-gap) |  |
| 66 | 4 Arctic | Regular | 8 | 2 | 55% | hidden, 4 veils / 3 colours, 1× large clay 8 (ice skin) | mixed | pyramid | left | 79 | 1 (stop-gap) |  |
| 67 | 4 Arctic | Regular | 7 | 2 | 43% | hidden, 4 veils / 3 colours | spread | auto | – | 78 | 1 (stop-gap) |  |
| 68 | 4 Arctic | Regular | 8 | 2 | 55% | hidden, 2× clay 4 | spread | pyramid | – | 78 | 1 (stop-gap) |  |
| 69 | 4 Arctic | **Super Hard** | 10 | 2 | 65% | hidden, 3 veils / 1 colour, 1× large clay 8 (ice skin) | spread | auto | centre | 23 | 1 (stop-gap) |  |
| 70 | 4 Arctic | Regular | 8 | 2 | 55% | hidden | full | flip | – | 84 | 1 (stop-gap) |  |
| 71 | 4 Arctic | Regular | 8 | 2 | 55% | hidden, 2 veils / 1 colour, 2× clay 4 | spread | pyramid | – | 77 | 1 (stop-gap) |  |
| 72 | 4 Arctic | Regular | 8 | 2 | 55% | hidden, 1× large clay 12 (big-bottle skin) | spread | auto | centre | 77 | 1 (stop-gap) |  |
| 73 | 4 Arctic | Regular | 8 | 2 | 55% | hidden, 2× clay 4 | full | pyramid | – | 77 | 1 (stop-gap) |  |
| 74 | 4 Arctic | **Hard** | 9 | 2 | 65% | hidden, 2 veils / 1 colour, 1× large clay 8 (ice skin) | full | flip | right | 36 | 1 (stop-gap) |  |
| 75 | 4 Arctic | Regular | 9 | 2 | 55% | hidden, 2× large clay 8 (big-bottle skin) | full | auto | left | 76 | 1 (stop-gap) |  |
| 76 | 4 Arctic | Regular | 9 | 2 | 55% | hidden, 1 veil / 1 colour, 2× clay 4 | mixed | auto | – | 76 | 1 (stop-gap) |  |
| 77 | 4 Arctic | Regular | 10 | 1 | 65% | hidden, 1× large clay 12 (big-bottle skin), 2 veils / 2 colours | full | diamond | right | 76 | 1 (stop-gap) |  |
| 78 | 4 Arctic | Regular | 9 | 2 | 55% | hidden, 1× large clay 8 (ice skin) | full | auto | centre | 76 | 1 (stop-gap) |  |
| 79 | 4 Arctic | **Super Hard** | 10 | 2 | 65% | hidden, 5 veils / 2 colours, 1× large clay 8 (ice skin) | full | auto | left | 22 | 1 (stop-gap) |  |
| 80 ★ | 5 Space | Regular | 6 | 2 | – | 1 lock × 1 tube | full | auto | – | 81 | 1 (stop-gap) |  |
| 81 | 5 Space | Regular | 7 | 2 | – | 1 lock × 1 tube | full | flip | – | 81 | 1 (stop-gap) |  |
| 82 | 5 Space | Regular | 7 | 2 | – | 1 lock × 2 tubes | full | pyramid | – | 81 | 1 (stop-gap) |  |
| 83 | 5 Space | Regular | 7 | 2 | – | 1× large clay 12 (big-bottle skin), 1 lock × 2 tubes | full | flip | left | 80 | 1 (stop-gap) |  |
| 84 | 5 Space | **Hard** | 9 | 2 | 65% | hidden, 2 locks × 2 tubes | full | auto | – | 35 | 1 (stop-gap) |  |
| 85 | 5 Space | Regular | 9 | 2 | 60% | hidden, 2× large clay 8 (big-bottle skin), 1 lock × 2 tubes | mixed | three | centre | 74 | 1 (stop-gap) |  |
| 86 | 5 Space | Regular | 9 | 2 | 60% | hidden, 5 veils / 3 colours, 2× clay 4 | mixed | diamond | – | 74 | 1 (stop-gap) |  |
| 87 | 5 Space | Regular | 9 | 2 | 60% | hidden, 1× large clay 12 (big-bottle skin), 1 lock × 2 tubes | full | flip | left | 73 | 1 (stop-gap) |  |
| 88 | 5 Space | Regular | 9 | 2 | 60% | hidden, 1 veil / 1 colour, 1 lock × 2 tubes | full | auto | – | 73 | 1 (stop-gap) |  |
| 89 | 5 Space | **Super Hard** | 10 | 2 | 65% | hidden, 1× large clay 8 (ice skin), 2 locks × 2 tubes | full | diamond | left | 21 | 1 (stop-gap) |  |
| 90 | 5 Space | Regular | 7 | 2 | 36% | hidden | full | diamond | – | 79 | 1 (stop-gap) |  |
| 91 | 5 Space | Regular | 9 | 2 | 60% | hidden, 2× clay 4, 1 lock × 2 tubes | full | auto | – | 72 | 1 (stop-gap) |  |
| 92 | 5 Space | Regular | 9 | 2 | 60% | hidden, 2× large clay 8 (big-bottle skin), 1 lock × 2 tubes | full | three | right | 72 | 1 (stop-gap) |  |
| 93 | 5 Space | Regular | 9 | 2 | 60% | hidden, 2 veils / 2 colours, 1 lock × 2 tubes | spread | auto | – | 72 | 1 (stop-gap) |  |
| 94 | 5 Space | **Hard** | 10 | 2 | 65% | hidden, 5 veils / 3 colours, 1× large clay 8 (ice skin), 2 locks × 2 tubes | full | auto | left | 34 | 1 (stop-gap) |  |
| 95 | 5 Space | Regular | 10 | 2 | 60% | hidden, 1× large clay 12 (big-bottle skin) | spread | diamond | left | 71 | 1 (stop-gap) |  |
| 96 | 5 Space | Regular | 10 | 2 | 60% | hidden, 1× large clay 8 (ice skin), 1 lock × 2 tubes | full | flip | right | 71 | 1 (stop-gap) |  |
| 97 | 5 Space | Regular | 10 | 2 | 60% | hidden, 3 veils / 2 colours, 2× clay 4 | full | auto | – | 71 | 1 (stop-gap) |  |
| 98 | 5 Space | Regular | 10 | 2 | 60% | hidden, 2 veils / 1 colour, 1 lock × 2 tubes | mixed | auto | – | 71 | 1 (stop-gap) |  |
| 99 | 5 Space | **Super Hard** | 10 | 2 | 65% | hidden, 3 veils / 2 colours, 1× large clay 8 (ice skin), 2 locks × 3 tubes | full | diamond | right | 20 | 1 (stop-gap) |  |
| 100 | 5 Space | **Super Hard** | 10 | 2 | 65% | hidden, 2 veils / 1 colour, 1× large clay 8 (ice skin), 2 locks × 3 tubes | full | pyramid | right | 20 | 1 (stop-gap) |  |
