# Level plan

Product's plan for the 100 levels. The rules the code enforces are in docs/mechanics.md and docs/levels.md. Where this file is stricter, it is a generator or curation rule, not validation. Which levels are approved or have variants built: decisions.md.

## Episodes
A new background per episode (names-and-art.md).

| Episode | Levels | Count |
|---|---|---|
| 1 Classic Lab | 1–19 | 19 |
| 2 Lava Lab | 20–39 | 20 |
| 3 Beach Lab | 40–59 | 20 |
| 4 Arctic Lab | 60–79 | 20 |
| 5 Space Lab | 80–100 | 21 |

Each episode ends on a Super Hard level (19, 39, 59, 79, 100).

## Tiers and rhythm
- **Hard** on every level ending in 4 (14, 24 … 94). **Super Hard** on every level ending in 9 (19, 29 … 99) and on L100, the finale with everything combined. That's 19 tier levels.
- Tier wins pay more: Hard +60, Super Hard +100 (regular +20), shown as "x3" / "x5" on the Level button (copy.md).
- Levels ending in 0 are breathers.
- The tier is stored per level (proposed `tier` field) rather than computed, so product can move one.
- Fill styles: full up to L14 and on intro and breather levels; partial (spread or mixed) on about 30% of episode-2 levels and about 50% after that.

## Mechanic unlocks (product D32)
| Mechanic | First level | Intro block (shown alone, no hidden balls) |
|---|---|---|
| Hidden balls | 7 | — |
| Large clay, big-bottle skin | 20 | 20–22 |
| Veil | 30 | 30–33 |
| Capacity-4 clay | 55 | 55–58 |
| Large clay, ice skin | 61 | — |
| Lock and key | 80 | 80–83 |

- Hidden balls come back on the first tier level after an intro (34, 59, 84).
- Episode 4 has no new mechanic: ice-skin large clay, then clay + veil combos from L66.
- The first 2-mechanic combos come about 11 levels after an intro (veil + big bottle at L41, clay + veil at L66). 3-mechanic combos only on tier levels and the finale.
- A tutorial toast on the first level of each mechanic: L1, 7, 20, 30, 55, 80 (copy.md).

> **Open issues:**
> - The table's first ice-skin level is L64, not L61 (L61 has capacity-4 clay).
> - L83 adds a big bottle to the lock intro, so the lock isn't shown alone for 4 levels, and that combo comes 3 levels after the intro, not about 11.

## How each mechanic is used
Design choices on top of the rules in docs/mechanics.md.

**Colour counts:** 4 balls for every normal colour. Each large clay tube gets its own large-clay colour with as many balls as its capacity (8 or 12). Each count matches a tube of that capacity, so the existing completion validation covers them.

**Veil**
- 1–3 veil colours per level, 1–3 veiled tubes each (e.g. 1 orange + 2 yellow + 1 red). Intro: L30 two veils of one colour, L31 two colours, L32 three veils in two colours, L33 three colours.
- Veiled tubes start **full** and hold only plain colours, never a veil colour or a large-clay colour, so every veil colour can be completed from the open tubes. The solver still checks every level.
- At least 2 filled tubes stay uncovered (no veil, no lock).
- Veiled tubes of the same colour take mirrored slots in their row; veils go towards the top rows.

**Lock and key**
- A lock covers 1–3 filled normal tubes (never clay, never large). L80–81 use 1-tube locks, then 2-tube locks; L99–100 use 3-tube locks.
- 1 lock per level; 2 locks on episode-5 Hard and Super Hard levels.
- Keys sit on revealed balls only, and never inside a veiled tube (the veil would hide the key).
- A key starts just below the top run of an open tube (1–2 balls under it), never in it. The player digs the key ball out, and the lock opens when it reaches the top run.
- Locked tubes come first in the layout, so they fill the top row; all tubes of one lock sit next to each other.

**Clay**
- Capacity-4 clay tube: starts empty. 1 per level while new (L55–57), then 2.
- Large clay, ice skin: capacity 8, starts with 2–4 balls of its colour at the bottom, so the player sees which colour belongs there. From L61, on every third regular level and on tier levels.
- Large clay, big-bottle skin: capacity 8 (episodes 1–2, and whenever there are two) or 12 (later episodes), starts with 1 ball of its colour. 1 per level, or 2 side by side on about a third of the regular levels that use it, from L25.

**Large tubes:** used only as clay (both skins), capacities 8 and 12. Placed in the centre (other tubes left and right) or at the left or right edge; two large tubes stand side by side in the centre.

> **Open issues:**
> - L94 (10 filled tubes − 5 veiled − 4 locked) and L99 (10 − 3 − 6) seem to leave only 1 uncovered filled tube, against the 2-tube rule.
> - Key depth is "1–2 balls" here, but the generator weights depths 3 / 2 / 1 (3 balls).
> - "Locked tubes come first" vs the slot rule below, where a single lock sits centred.
> - Ice-skin cadence: the only regular levels with it are 66, 78 and 96, and L84 (Hard) has none.

## Combination rules (product D31, D23)
A clay tube is never a source, so a colour with more balls than a clay tube holds could be poured in and never complete. The generator refuses:
- a capacity-4 clay tube in a level with any large clay tube: the 8–12-ball large-clay colour could get stuck in it (a real solvability trap);
- both large clay skins in one level (a design choice);
- a big-bottle tube on a Hard or Super Hard level: it soaks up a whole colour and makes the board easy (D23). The ice skin is allowed there.

## Board look
Rules for building `layout.grid`, within the 3-row, 6-cell limit.
- **Row shapes**, fixed per level so retries keep the look: even (the default, top row longest), **three** rows, **diamond** (middle row longest, e.g. 4-5-3), **pyramid** (rows grow downwards), **flip** (the whole board mirrored vertically: longer row at the bottom, empty and clay tubes on top, veiled tubes at the bottom). 3-row shapes only on boards of 11+ tubes; flip on boards of 6+.
- **Symmetric slots:** once the row lengths are set, tubes take slots by type. Locks first: two locks of the same width take both ends of a row, a single lock sits centred. Then veiled tubes (from the top), then clay and empty tubes (from the bottom), then partly filled tubes. Each type goes in mirrored pairs, with a single one in the centre slot. Full tubes fill the rest.
- **Tall side:** where the large tubes go (centre, left or right).
- **No repeats:** a level never looks the same as either of the two before it when it has the same mechanics.

> **Open issues:**
> - The table breaks the shape limits: L2 is flip with 4 tubes; L9 and L23 are "three" with 7 tubes; L55 is "three" with 9; L14 and L90 are diamond with 9. Does the rule or the table win?
> - A capacity-12 tube spans 3 rows in our grid (capacities 11–16 span 3 rows), so every level with one needs a 3-row board.

## Difficulty
The difficulty measure is **a bot that plays like a casual player**, the best proxy product found for felt difficulty with hidden balls and mechanics.
- It sees only revealed balls (a hidden ball is unknown, and scoring a move never peeks at the ball it uncovers) and doesn't see inside covered tubes. No undo, no boosters.
- Each turn it scores every legal move one step ahead and picks the best plus Gumbel noise. Bonus for a move that digs towards a visible key; −6 for pouring into an empty clay tube, and an empty clay tube doesn't count as free room.
- Three skills, 20 plays each; the win rate is the mean.

  | Skill | Noise | Lookahead |
  |---|---|---|
  | careless | 7 | none |
  | casual | 4 | avoids immediate dead ends |
  | careful | 2 | 2-ply |

- Win rate → **bucket** 1–7 (for reports): ≥90% → 1, ≥75% → 2, ≥60% → 3, ≥45% → 4, ≥30% → 5, ≥15% → 6, else 7.
- **Targets:** each level has a target bot win rate (product D24; table below). A candidate is accepted within ±5 points.
  - Hard ≈ bucket 5 (bot wins 30–45%), Super Hard ≈ bucket 6 (15–30%); L19, the first Super Hard, at bucket 5. Bucket 7 (the bot almost never wins) feels like a wall: don't go there.
  - No move cap (D27): long levels are fine.
- **Search:** 12 candidates per round, up to 6 rounds. Too easy → make the recipe harder: more colours first (up to 10), then more hidden balls (up to the 65% cap, D26), then one empty tube fewer. With a big-bottle tube on the board, drop an empty tube first (it soaks up a whole colour). Too hard → the reverse.

> **Open issue:** L14 (Hard) targets 56%, which is bucket 4, outside the Hard band.

## Prototype generator
How product's prototype (`tools/levelgen/`, Node) builds a level. Useful whichever tool ends up generating the levels (decisions.md → export offer).

**Recipe** (one per level; the values are in the table below):

| Param | Meaning |
|---|---|
| `colors` | number of colours (large-clay colours count too) |
| `empty` | free room, in empty tubes' worth (`empty × 4` free slots) |
| `fill` | `full` (full tubes + `empty` empty tubes), `spread` (no empty tube; the free slots are 1–2 missing balls spread over the tubes), `mixed` (1 empty tube, the rest spread) |
| `hidden` | share of non-top balls that start hidden, capped at 65% (D26) |
| `veils`, `veilColours` | number of veiled tubes, number of veil colours (1–3) |
| `locks`, `lockWidth` | number of locks, tubes per lock (1–3) |
| `clayTubes` | capacity-4 clay tubes that start empty |
| `iceLargeClay` | ice-skin large clay tubes: capacity 8, start with 2–4 balls of their colour |
| `bottleLargeClay`, `bottleCapacity` | 0–2 big-bottle large clay tubes: capacity 8 or 12, start with 1 ball of their colour |
| `rowShape`, `tallSide` | grid look: auto / three / diamond / pyramid / flip; large tubes centre / left / right |
| `tier`, `target` | regular / hard / superHard; target bot win rate |

**Deal, step by step** (the prototype's `generate()`, verified on thousands of deals):
1. Pick the colours. Each large clay tube gets its own large-clay colour with as many balls as its capacity; the rest get 4. Shuffle all balls.
2. Take out the starting fill: 2–4 balls of its colour for each ice-skin tube, 1 ball for each big-bottle tube.
3. **Veils:** the first `veils` tubes are filled only with plain colours (no veil colour, no large-clay colour), so every veil colour stays completable from the open tubes. Veil colours are handed out round-robin (4 veils, 3 colours → 2 + 1 + 1).
4. Fill the rest of the tubes per `fill`. Spread never takes balls from veiled tubes. Re-deal if any tube starts completed.
5. **Locks:** the next `locks × lockWidth` filled tubes get the locks. Each key goes on a ball of an open tube (not locked, veiled or clay), below that tube's opening top run and never in it, weighted by depth under the run: 3 / 2 / 1.
6. Add the empty tubes, the capacity-4 clay tubes, then the large clay tubes with their starting fill.
7. **Hidden:** each non-top ball becomes hidden with probability `hidden`, except key balls and anything in clay.
8. Build `layout.grid` from `rowShape` and `tallSide` and the symmetric-slot rules, within the 3-row, 6-cell limit.
9. Solve. Unsolvable or unknown → next seed (40 tries, then retry with one more empty tube).

**Refuse impossible recipes** with a reason instead of looping:
- veils and lock tubes would leave fewer than 2 open filled tubes;
- not enough plain colours to fill the veiled tubes;
- large-clay colours plus veil colours need more colours than the level has;
- a capacity-4 clay tube with any large clay tube, or both large clay skins in one level.

**Prototype solver:** a node-limit hit counts as unknown, and an unknown deal is rejected (never shipped). The heaviest boards (veils + locks + a large clay tube, 15 tubes) took up to ~100 s per solvable deal in the JS prototype. Its other tricks (heuristic, move ordering) were reviewed and not adopted: our solver already covers them (docs/levels.md → Solver).

> **Open issue:** `fill: full` can't mean all-full tubes when a big bottle starts with 1 ball. L20 has 3 × 4 + 7 = 19 balls outside the bottle, and every "full" big-bottle level has the same remainder. Define what "full" means there.

## Coverage
Levels containing each mechanic (checked against the table): hidden 77 · large clay, big-bottle skin 22 · veil 35 · other clay 27 (capacity-4 in 16, ice-skin large in 11) · lock 17 · basic 6. Mechanics per level, hidden aside: 0 → 27 levels, 1 → 48, 2 → 22, 3 → 3.

## The 100 levels
- ★ = a tutorial toast (copy.md).
- Ep = episode. Empty = free room in empty tubes' worth. Hidden = share of non-top balls that start hidden.
- Target = the bot win rate the level is tuned to. **Bold** = product has saved this value; plain = the default curve, which product may still change.

| # | Ep | Tier | Colours | Empty | Hidden | Mechanics | Fill | Row shape | Tall side | Target: bot wins % |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 ★ | 1 | Regular | 2 | 1 | – | basic | full | auto | – | **97** |
| 2 | 1 | Regular | 3 | 1 | – | basic | full | flip | – | 97 |
| 3 | 1 | Regular | 3 | 2 | – | basic | full | auto | – | **97** |
| 4 | 1 | Regular | 4 | 2 | – | basic | full | auto | – | **97** |
| 5 | 1 | Regular | 4 | 2 | – | basic | full | flip | – | 94 |
| 6 | 1 | Regular | 5 | 2 | – | basic | full | auto | – | **94** |
| 7 ★ | 1 | Regular | 4 | 2 | 35% | hidden | full | auto | – | **93** |
| 8 | 1 | Regular | 4 | 2 | 36% | hidden | full | flip | – | 93 |
| 9 | 1 | Regular | 5 | 2 | 38% | hidden | full | three | – | 93 |
| 10 | 1 | Regular | 5 | 2 | 39% | hidden | full | flip | – | 97 |
| 11 | 1 | Regular | 5 | 2 | 40% | hidden | full | auto | – | **92** |
| 12 | 1 | Regular | 5 | 2 | 41% | hidden | full | flip | – | 92 |
| 13 | 1 | Regular | 6 | 2 | 43% | hidden | full | auto | – | **90** |
| 14 | 1 | **Hard** | 7 | 2 | 65% | hidden | full | diamond | – | **56** |
| 15 | 1 | Regular | 6 | 2 | 45% | hidden | full | flip | – | 91 |
| 16 | 1 | Regular | 6 | 2 | 46% | hidden | spread | auto | – | **91** |
| 17 | 1 | Regular | 7 | 2 | 48% | hidden | full | auto | – | **90** |
| 18 | 1 | Regular | 7 | 2 | 49% | hidden | full | flip | – | 91 |
| 19 | 1 | **Super Hard** | 9 | 2 | 65% | hidden | mixed | pyramid | – | **42** |
| 20 ★ | 2 | Regular | 4 | 2 | – | 1× large clay 8 (big-bottle skin) | full | pyramid | left | 96 |
| 21 | 2 | Regular | 4 | 2 | – | 1× large clay 8 (big-bottle skin) | full | flip | centre | 96 |
| 22 | 2 | Regular | 4 | 2 | – | 1× large clay 8 (big-bottle skin) | full | auto | right | 96 |
| 23 | 2 | Regular | 5 | 2 | 45% | hidden | full | three | – | **89** |
| 24 | 2 | **Hard** | 7 | 2 | 65% | hidden | spread | flip | – | 42 |
| 25 | 2 | Regular | 6 | 2 | 45% | hidden | full | flip | – | 89 |
| 26 | 2 | Regular | 7 | 1 | 65% | hidden, 1× large clay 8 (big-bottle skin) | full | auto | centre | **89** |
| 27 | 2 | Regular | 6 | 2 | 45% | hidden | full | auto | – | **88** |
| 28 | 2 | Regular | 6 | 2 | – | 2× large clay 8 (big-bottle skin) | mixed | flip | centre | 88 |
| 29 | 2 | **Super Hard** | 8 | 2 | 65% | hidden | full | flip | – | 27 |
| 30 ★ | 2 | Regular | 5 | 2 | – | 2 veils / 1 colour | full | pyramid | – | 94 |
| 31 | 2 | Regular | 6 | 2 | – | 2 veils / 2 colours | full | auto | – | 93 |
| 32 | 2 | Regular | 6 | 2 | – | 3 veils / 2 colours | full | auto | – | 93 |
| 33 | 2 | Regular | 6 | 2 | – | 3 veils / 3 colours | full | flip | – | 93 |
| 34 | 2 | **Hard** | 8 | 2 | 60% | hidden, 5 veils / 3 colours | full | auto | – | 41 |
| 35 | 2 | Regular | 7 | 2 | 45% | hidden, 2 veils / 1 colour | mixed | pyramid | – | 86 |
| 36 | 2 | Regular | 8 | 2 | 45% | hidden, 1× large clay 8 (big-bottle skin) | spread | pyramid | left | 86 |
| 37 | 2 | Regular | 8 | 2 | 45% | hidden, 4 veils / 2 colours | mixed | auto | – | 86 |
| 38 | 2 | Regular | 8 | 2 | 45% | hidden, 5 veils / 3 colours | spread | auto | – | 86 |
| 39 | 2 | **Super Hard** | 10 | 2 | 65% | hidden, 3 veils / 1 colour | full | flip | – | 26 |
| 40 | 3 | Regular | 6 | 2 | 50% | hidden, 2× large clay 8 (big-bottle skin) | full | pyramid | centre | 91 |
| 41 | 3 | Regular | 6 | 2 | 50% | hidden, 1× large clay 12 (big-bottle skin), 3 veils / 2 colours | mixed | three | left | 85 |
| 42 | 3 | Regular | 6 | 2 | 50% | hidden, 3 veils / 3 colours | spread | flip | – | 85 |
| 43 | 3 | Regular | 6 | 2 | 50% | hidden, 1× large clay 12 (big-bottle skin) | full | three | centre | 84 |
| 44 | 3 | **Hard** | 8 | 2 | 65% | hidden, 3 veils / 1 colour | full | auto | – | **41** |
| 45 | 3 | Regular | 7 | 2 | – | 1× large clay 12 (big-bottle skin) | spread | auto | centre | 84 |
| 46 | 3 | Regular | 7 | 2 | 50% | hidden, 4 veils / 3 colours | mixed | flip | – | 84 |
| 47 | 3 | Regular | 7 | 2 | 50% | hidden, 1× large clay 12 (big-bottle skin), 3 veils / 3 colours | full | pyramid | centre | 83 |
| 48 | 3 | Regular | 7 | 2 | 50% | hidden, 4 veils / 2 colours | mixed | auto | – | 83 |
| 49 | 3 | **Super Hard** | 9 | 2 | 65% | hidden, 3 veils / 1 colour | full | auto | – | **26** |
| 50 | 3 | Regular | 8 | 2 | 50% | hidden | full | pyramid | – | 89 |
| 51 | 3 | Regular | 10 | 2 | 65% | hidden, 1× large clay 12 (big-bottle skin), 2 veils / 1 colour | spread | pyramid | centre | **85** |
| 52 | 3 | Regular | 7 | 2 | 38% | hidden, 3 veils / 2 colours | full | auto | – | 82 |
| 53 | 3 | Regular | 10 | 1 | 65% | hidden, 2× large clay 8 (big-bottle skin) | full | pyramid | centre | 82 |
| 54 | 3 | **Hard** | 9 | 2 | 53% | hidden, 2 veils / 1 colour | spread | auto | – | 39 |
| 55 ★ | 3 | Regular | 6 | 2 | – | 1× clay 4 | full | three | – | 87 |
| 56 | 3 | Regular | 8 | 2 | – | 1× clay 4 | full | pyramid | – | 87 |
| 57 | 3 | Regular | 8 | 2 | – | 1× clay 4 | full | diamond | – | 87 |
| 58 | 3 | Regular | 8 | 2 | – | 2× clay 4 | full | flip | – | 87 |
| 59 | 3 | **Super Hard** | 10 | 2 | 65% | hidden, 2× clay 4 | full | auto | – | 24 |
| 60 | 4 | Regular | 7 | 2 | 55% | hidden, 2× clay 4 | full | flip | – | 86 |
| 61 | 4 | Regular | 7 | 2 | 55% | hidden, 2× clay 4 | full | three | – | 80 |
| 62 | 4 | Regular | 7 | 2 | 55% | hidden, 2× clay 4 | full | auto | – | 80 |
| 63 | 4 | Regular | 7 | 2 | 55% | hidden, 1× large clay 12 (big-bottle skin) | full | auto | centre | 79 |
| 64 | 4 | **Hard** | 8 | 2 | 65% | hidden, 1× large clay 8 (ice skin) | mixed | diamond | centre | 37 |
| 65 | 4 | Regular | 8 | 2 | 55% | hidden, 2× clay 4 | mixed | flip | – | 79 |
| 66 | 4 | Regular | 8 | 2 | 55% | hidden, 4 veils / 3 colours, 1× large clay 8 (ice skin) | mixed | pyramid | left | 79 |
| 67 | 4 | Regular | 7 | 2 | 43% | hidden, 4 veils / 3 colours | spread | auto | – | 78 |
| 68 | 4 | Regular | 8 | 2 | 55% | hidden, 2× clay 4 | spread | pyramid | – | 78 |
| 69 | 4 | **Super Hard** | 10 | 2 | 65% | hidden, 3 veils / 1 colour, 1× large clay 8 (ice skin) | spread | auto | centre | 23 |
| 70 | 4 | Regular | 8 | 2 | 55% | hidden | full | flip | – | 84 |
| 71 | 4 | Regular | 8 | 2 | 55% | hidden, 2 veils / 1 colour, 2× clay 4 | spread | pyramid | – | 77 |
| 72 | 4 | Regular | 8 | 2 | 55% | hidden, 1× large clay 12 (big-bottle skin) | spread | auto | centre | 77 |
| 73 | 4 | Regular | 8 | 2 | 55% | hidden, 2× clay 4 | full | pyramid | – | 77 |
| 74 | 4 | **Hard** | 9 | 2 | 65% | hidden, 2 veils / 1 colour, 1× large clay 8 (ice skin) | full | flip | right | 36 |
| 75 | 4 | Regular | 9 | 2 | 55% | hidden, 2× large clay 8 (big-bottle skin) | full | auto | left | 76 |
| 76 | 4 | Regular | 9 | 2 | 55% | hidden, 1 veil / 1 colour, 2× clay 4 | mixed | auto | – | 76 |
| 77 | 4 | Regular | 10 | 1 | 65% | hidden, 1× large clay 12 (big-bottle skin), 2 veils / 2 colours | full | diamond | right | 76 |
| 78 | 4 | Regular | 9 | 2 | 55% | hidden, 1× large clay 8 (ice skin) | full | auto | centre | 76 |
| 79 | 4 | **Super Hard** | 10 | 2 | 65% | hidden, 5 veils / 2 colours, 1× large clay 8 (ice skin) | full | auto | left | 22 |
| 80 ★ | 5 | Regular | 6 | 2 | – | 1 lock × 1 tube | full | auto | – | 81 |
| 81 | 5 | Regular | 7 | 2 | – | 1 lock × 1 tube | full | flip | – | 81 |
| 82 | 5 | Regular | 7 | 2 | – | 1 lock × 2 tubes | full | pyramid | – | 81 |
| 83 | 5 | Regular | 7 | 2 | – | 1× large clay 12 (big-bottle skin), 1 lock × 2 tubes | full | flip | left | 80 |
| 84 | 5 | **Hard** | 9 | 2 | 65% | hidden, 2 locks × 2 tubes | full | auto | – | 35 |
| 85 | 5 | Regular | 9 | 2 | 60% | hidden, 2× large clay 8 (big-bottle skin), 1 lock × 2 tubes | mixed | three | centre | 74 |
| 86 | 5 | Regular | 9 | 2 | 60% | hidden, 5 veils / 3 colours, 2× clay 4 | mixed | diamond | – | 74 |
| 87 | 5 | Regular | 9 | 2 | 60% | hidden, 1× large clay 12 (big-bottle skin), 1 lock × 2 tubes | full | flip | left | 73 |
| 88 | 5 | Regular | 9 | 2 | 60% | hidden, 1 veil / 1 colour, 1 lock × 2 tubes | full | auto | – | 73 |
| 89 | 5 | **Super Hard** | 10 | 2 | 65% | hidden, 1× large clay 8 (ice skin), 2 locks × 2 tubes | full | diamond | left | 21 |
| 90 | 5 | Regular | 7 | 2 | 36% | hidden | full | diamond | – | 79 |
| 91 | 5 | Regular | 9 | 2 | 60% | hidden, 2× clay 4, 1 lock × 2 tubes | full | auto | – | 72 |
| 92 | 5 | Regular | 9 | 2 | 60% | hidden, 2× large clay 8 (big-bottle skin), 1 lock × 2 tubes | full | three | right | 72 |
| 93 | 5 | Regular | 9 | 2 | 60% | hidden, 2 veils / 2 colours, 1 lock × 2 tubes | spread | auto | – | 72 |
| 94 | 5 | **Hard** | 10 | 2 | 65% | hidden, 5 veils / 3 colours, 1× large clay 8 (ice skin), 2 locks × 2 tubes | full | auto | left | 34 |
| 95 | 5 | Regular | 10 | 2 | 60% | hidden, 1× large clay 12 (big-bottle skin) | spread | diamond | left | 71 |
| 96 | 5 | Regular | 10 | 2 | 60% | hidden, 1× large clay 8 (ice skin), 1 lock × 2 tubes | full | flip | right | 71 |
| 97 | 5 | Regular | 10 | 2 | 60% | hidden, 3 veils / 2 colours, 2× clay 4 | full | auto | – | 71 |
| 98 | 5 | Regular | 10 | 2 | 60% | hidden, 2 veils / 1 colour, 1 lock × 2 tubes | mixed | auto | – | 71 |
| 99 | 5 | **Super Hard** | 10 | 2 | 65% | hidden, 3 veils / 2 colours, 1× large clay 8 (ice skin), 2 locks × 3 tubes | full | diamond | right | 20 |
| 100 | 5 | **Super Hard** | 10 | 2 | 65% | hidden, 2 veils / 1 colour, 1× large clay 8 (ice skin), 2 locks × 3 tubes | full | pyramid | right | 20 |
