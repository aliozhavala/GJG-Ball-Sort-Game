# Mechanics & Board Layout

> **[+P] = product addition** on top of the developer's version (commit `4f77fb8`, 28 Oct 2026). Text without the mark is the developer's, unchanged. Product additions never change a rule above them; they add the design intent and the visuals the rules don't cover yet. They use this file's terms; product / art names appear only in the name table, in visual notes and in player-facing text. Where product and this file disagree, this file wins until we've talked.

Read this when working on Core rules, level files, validation, the solver, or board views. `docs/levels.md` holds the JSON schema; this file holds the rules the schema expresses. If they disagree, this file wins and `docs/levels.md` must be fixed.

## [+P] Product names → code names
Product, art and the prototype use themed names. They map 1:1 onto the mechanics below; nothing new in Core. The rest of this file uses the code names.

| Product / art name | Code | Shape in the level file |
|---|---|---|
| Bottle | tube | — |
| Creature | ball | — |
| Corked bottle | completed tube | — |
| "?" creature | hidden ball | `hidden` |
| **Towel** (badge shows a colour) | veil | `veil: { "color": c }` |
| **Lock box** (wooden crate + coloured padlock) | lock | `locks: [{ id, tubes }]` |
| Key (drawn on a creature) | key | `keys: [{ slot, lock }]` |
| **Small ice bottle** | capacity-4 clay tube, starts empty | `capacity: 4, clay: true` |
| **Big ice bottle** | large clay tube, capacity 8, starts with 2–4 balls of its colour (ice skin) | `capacity: 8, clay: true` |
| **Big bottle** | large clay tube, capacity 8 or 12, starts with 1 ball of its colour (big-bottle skin) | `capacity: 8 / 12, clay: true, skin: "bigBottle"` |

The two large clay forms behave identically in Core (both start with their colour on top, so only that colour can ever go in). They differ only in art, in where the generator may use them (see Clay below) and in the break-on-completion effect. The view needs to know which one to draw; `docs/levels.md` proposes an optional per-tube `skin` field for that. Below, **large-clay colour** means the 8- or 12-ball colour that fills a large clay tube.

Mechanic unlock levels (product, D32): hidden **7** · large clay tube (big-bottle skin) **20** · veil **30** · capacity-4 clay tube **55** (large clay with ice skin from 61) · lock **80**. Per-level plan: `docs/levels.md` → 100-level plan.

## Completion (all mechanics)
A tube is **completed** when it is full, all its balls are revealed, and it holds **every ball of that color in the level**. So 4 purples in a capacity-4 tube are not a completion when the level has 10 purples — those balls can still be moved out. Completion seals undo history (unchanged).

Validation: for each color, its ball count must equal the capacity of some tube, and for each capacity value, the number of colors with that count must not exceed the number of tubes with that capacity.

- **[+P] Colour counts the product levels use:** 4 for every normal colour; one large-clay colour per large clay tube, with as many balls as its capacity (8 or 12). Each of these matches a tube of that capacity, so the validation above already covers them.

## Covers: veil and lock
A **covered** tube is neither a move source nor a target, and its contents are drawn covered (not visible). A tube has at most one cover. When the cover is removed, the tube behaves normally and its balls follow their own `hidden` flags. Removing a cover is permanent (never undone).
- Validation: a covered tube must not already hold a finished set (it would complete the moment its cover opened, and removing a cover never completes a tube).

### Veil
- A veil has one color (`veil.color`, a ball color id). It opens **only** when a tube completes in **that color**, by a move or by an undo. Completing a tube of any other color does nothing to it, and neither does filling a tube with only some of the veil's color (that is not a completion).
- Every veil of the completed color opens at once.
- Validation: the veil color exists in the level.
- **[+P] Product design (veil):**
  - A level has **1–3 veil colours with 1–3 veiled tubes each** (e.g. 1 orange + 2 yellow + 1 red). Intro block: L30 two veils of one colour, L31 two colours, L32 three veils in two colours, L33 three colours.
  - Veiled tubes start **full** and hold only "plain" colours: never a veil colour or a large-clay colour. So every veil colour can always be completed from the open tubes (the solver still checks every level).
  - At least 2 filled tubes stay uncovered (no veil, no lock).
  - Veiled tubes of the same colour take mirrored slots in their row; veils go towards the top rows.
  - **[+P] Visual (step 6), art name "towel":** a towel draped over the whole tube with a round badge in the veil colour. When it opens, the towel **lifts straight off** (no fade first), then the balls underneath are drawn with their own `hidden` flags.

### Lock and key
- A **lock** covers one or more tubes that are **adjacent in the same grid row**. Locks have an `id`; their color comes from a separate lock palette (never confused with ball colors).
- A **key** sits on one specific ball and opens the lock with the same id.
- A key is **found** when its ball is part of its tube's selectable run (revealed, top run, tube not covered/clay). Check after every move, undo, reveal and cover removal. Found → the lock opens, the key mark disappears from the ball.
- Consequences: a key on a hidden ball can't be found until revealed; a key inside a covered tube can't be found until that tube is uncovered.
- Validation: every lock has exactly one key and every key's lock exists; no key is in a selectable run at level start; lock tubes are adjacent in one row; no key in a clay tube; lock ids are unique; a ball holds at most one key; no key inside a tube covered by its own lock.
- Level design note: put keys on the top ball of a run so the visual matches.
- **[+P] Product design (lock):**
  - A lock covers **1–3 filled normal tubes** (never clay, never large). L80–81 use 1-tube locks, then 2-tube locks; L99–100 use 3-tube locks.
  - **1 lock per level**, 2 locks on episode-5 Hard / Super Hard levels.
  - Keys sit on **revealed balls only** (never on a hidden ball) and **never inside a veiled tube** (the veil would hide the key from the player). Both are stricter than the validation above, so they are generator rules, not validation rules.
  - Keys start **just below the top run** of an open tube (1–2 balls under it), never in it, so the player digs the key ball out and the lock opens the moment it reaches the top run. This matches the validation rule above.
  - Lock palette (6, independent of the ball colours): pink, blue, green, yellow, purple, orange. The lock and the key mark use the lock's colour.
  - Locked tubes come first in the layout, so they fill the top row; all tubes of one lock sit next to each other in one row.
  - **[+P] Visual (step 6), art name "lock box":** a wooden crate over the tube(s) with a coloured padlock on the front. On unlock, the key flies from its ball to the padlock, the padlock pops and the crate breaks open. The flight is cosmetic: Core has already opened the lock.

## Clay
- A clay tube is **never a move source**; it is a normal target (empty, or top color matches).
- Starts empty or with balls of **one color only**. No hidden balls and no keys in clay.
- Any capacity can be clay. Usual design (not a rule): a large clay tube with 1 starting ball of the color that will fill it.
- **[+P] Product uses clay in three forms** (art names in the name table above):
  - **Capacity-4 clay tube:** starts empty. 1 per level while new (L55–57), then 2.
  - **Large clay tube, ice skin:** capacity 8, starts with **2–4 balls of its colour** at the bottom, so the player sees which colour belongs there and the top-colour rule keeps every other colour out. The level has one 8-ball colour per such tube. From L61, on every third regular level and on tier levels.
  - **Large clay tube, big-bottle skin:** capacity 8 (episodes 1–2, and when there are two) or 12 (later episodes), starts with **1 ball** of its colour. 1 per level, or 2 side by side on about a third of the regular levels that use it, from L25.
- **[+P] Combination rules** (product D31): a clay tube is never a source, so a colour with more balls than a clay tube holds could be poured in and never complete. The generator therefore refuses:
  - a capacity-4 clay tube in a level that has any large clay tube (the large-clay colour, 8–12 balls, could get stuck in it);
  - both large clay forms (ice skin and big-bottle skin) in the same level;
  - a big-bottle-skin large clay tube on a Hard or Super Hard level (it soaks up a whole colour and makes the board easy; product D23). The ice-skin form is allowed there.
  
  These could also become validation warnings; they are design rules, not Core rules.
- **[+P] Visual (step 6):** clay tubes without the big-bottle skin are drawn as bottles standing in a block of ice (art name "ice"). **The ice breaks only when the tube completes** (cracks ~0.15 s, then shards + shatter SFX); an unfinished one keeps its ice. The big-bottle skin is a tall glass bottle with a crystal base in its colour.

## Large tubes
- Normal tubes have capacity 4. Large tubes span grid rows: capacity **5–10 spans 2 rows**, **11–16 spans 3 rows**.
- A large tube is **bottom-aligned** with the bottom row of its span; capacity 10 (2 rows) and 16 (3 rows) also align with the top row exactly.
- Apart from capacity and layout, a large tube is an ordinary tube: it **need not be clay** and may hold hidden balls, keys or a cover like any other.
- **[+P] Product uses large tubes only as clay** (both skins), capacities 8 and 12. Placement per level: in the **centre** (other tubes left and right of it), or at the **left / right edge**. Two large tubes stand side by side in the centre.

## Board layout (grid)
- Every level has an explicit `layout.grid`: rows of cells (top row first), each cell a tube index or `null`. Max **3 rows**, max **6 cells per row** in level files (added tubes may extend a row to 8 during play).
- Geometry: row height = a capacity-4 tube; gap between rows = **2 ball slots** (also the lift space), so rows sit 6 slots apart, base line to base line. So 2 rows = 10 slots, 3 rows = 16 slots.
- **Aligned rows** = rows that contain any part of a large tube. Aligned rows share column positions: cell *i* is in the same column in each of them, and the aligned block is centered on screen by its widest row (shorter aligned rows are therefore left-aligned with it, not centered). A large tube appears in the same column in every row it spans, and its span must match its capacity. `null` = an empty gap, allowed only in aligned rows.
- **Free rows** = rows with no large tube. Each is centered on its own by its tube count; columns don't need to line up with other rows. No `null` cells.
- Example: 3 rows with a large tube spanning rows 2–3 → row 1 is free (centered), rows 2–3 are aligned.
- **Board scale** = fit the widest row and the row count to the board area. It is computed at load and only recomputed when an added tube makes the widest row wider (tubes and balls shrink to fit). The number of rows never changes during play.
- **[+P] How product wants grids to look.** These are rules for whoever builds `layout.grid` (the generator), within the limits above:
  - **Row shapes** (fixed per level, so retries keep the look): even rows (default, top row longest), **three** rows, **diamond** (middle row longest, e.g. 4-5-3), **pyramid** (rows grow downwards), **flip** (the whole board mirrored vertically: longer row at the bottom, empty and clay tubes on top, veiled tubes at the bottom). The 3-row shapes only on boards of 11+ tubes; flip on boards of 6+.
  - **Symmetric slots:** after the row lengths are set, tubes take slots by type: locks (two locks of the same width take both ends of a row; one lock alone sits centred), then veiled tubes (from the top), then clay and empty tubes (from the bottom), then partly filled tubes, each type in mirrored pairs with a single one in the centre slot. Full tubes fill the rest.
  - **No repeats:** a level never looks the same as either of the two levels before it when it has the same mechanics.
  - **Spacing (view config):** tubes sit close together and big, ~1.42 ball diameters centre to centre, and the board fills the width (Magic Sort look, product D30).

### Added tubes (Add tube booster, Play on)
- An added tube is empty, capacity 4, and is **appended to the end of the row with the fewest cells** (cells include `null`s and large-tube cells). Ties go to the **bottom-most** of the tied rows.
  - Example: rows 4,5 → added to the top → 5,5 → next added to the bottom → 5,6.
  - Example with a large tube spanning both rows, 4,4 → added to the bottom → 4,5; the top row stays aligned with the bottom row, not centered.
- Rows are never added. During play a row can grow to **8 cells** (2 more than the level-design maximum of 6), so adding is almost always possible. When every row has 8 cells there is no room: Add tube shows "No room for more tubes"; Play on is disabled, so Give up is the only choice.
- Positions of existing tubes may shift (re-centering, re-fitting) but tubes never change row or order within their row.

## Win and stuck
- Won = every tube is empty or completed (a covered empty tube counts as empty).
- Stuck = not won and no legal move (covers and clay already restrict moves). Soft-locks with legal moves remaining are not detected (by design).

## Shuffle (booster)
- **Which tubes:** only tubes that are **not covered** (veil or lock), **not clay** and **not completed**. Balls move only between these tubes, and each of them keeps its ball count. Every other tube is untouched.
- **Keys stay put:** a ball that still carries a key keeps its tube and slot.
- **Hidden balls take part:** every ball keeps its hidden flag, except that a hidden ball landing on **top** of a tube is revealed. After a shuffle every tube's top ball is revealed.
- **Valid result:** it differs from the current arrangement, no tube is completed, no key is findable (no key in a selectable run), and the board is solvable (solver, Fast mode; a result the solver can't confirm within its node limit counts as unsolvable). Random arrangements are tried up to N times.
- **No valid shuffle found:** nothing changes, the booster isn't spent, and a toast says so.
- **Success:** spends one Shuffle booster and **clears the undo history** (the recorded moves no longer match the board). A shuffle is not undoable.
- Like every booster, unavailable once the level is won or while stuck.

## [+P] Mechanic visuals and tutorial text (step 6)
State changes are instant in Core; everything here is view-only and must never block input.

| Event (Core) | Visual (art names) | Tutorial toast on the intro level |
|---|---|---|
| `BallRevealed` | The "?" creature flips to its real face (quick flip) | L7: "New: Hidden balls! The colour of a "?" ball shows when the ball above it moves." |
| `TubeCompleted` (large clay, big-bottle skin) | Cork drops on the big bottle | L20: "New: Big bottle! It only takes its own colour. Gather all of that colour here." |
| `TubeUnveiled` | The towel lifts straight off | L30: "New: Towels! Complete the colour on the badge and the towels come off." |
| `TubeCompleted` (clay, ice skin) | The ice cracks, then bursts into shards | L55: "New: Ice bottles! They are frozen to the floor: you can fill them, but never pour out of them." |
| `KeyFound` → `LockOpened` | The key flies to the padlock, the crate breaks open | L80: "New: Lock boxes! Dig out the creature carrying a key and the box with the matching lock opens." |

L1 toast: "Tap a tube, then tap the target. Put same-colour balls together!" Toast timing: shown 0.6 s after the level loads, for 3.6 s, never blocks input.
