# Mechanics & Board Layout

Read this when working on Core rules, level files, validation, the solver, or board views. `docs/levels.md` holds the JSON schema; this file holds the rules the schema expresses. If they disagree, this file wins and `docs/levels.md` must be fixed.

## Completion (all mechanics)
A tube is **completed** when it is full, all its balls are revealed, and it holds **every ball of that color in the level**. So 4 purples in a capacity-4 tube are not a completion when the level has 10 purples — those balls can still be moved out. Completion seals undo history (unchanged).

Validation: for each color, its ball count must equal the capacity of some tube, and for each capacity value, the number of colors with that count must not exceed the number of tubes with that capacity.

## Covers: veil and lock
A **covered** tube is neither a move source nor a target, and its contents are drawn covered (not visible). A tube has at most one cover. Any tube can be covered, large tubes included. When the cover is removed, the tube behaves normally and its balls follow their own `hidden` flags. Removing a cover is permanent (never undone).
- Validation: a covered tube must not already hold a finished set (it would complete the moment its cover opened, and removing a cover never completes a tube).

### Veil
- A veil has one color (`veil.color`, a ball color id). It opens **only** when a tube completes in **that color**, by a move or by an undo. Completing a tube of any other color does nothing to it, and neither does filling a tube with only some of the veil's color (that is not a completion).
- Every veil of the completed color opens at once.
- Validation: the veil color exists in the level.

### Lock and key
- A **lock** covers one or more tubes that are **adjacent in the same grid row** and have the **same capacity**: one door spans them all, so they share a height. A lock may cover large tubes, but never a mix of large and capacity-4 tubes. Locks have an `id`; their color comes from a separate lock palette (never confused with ball colors).
- A **key** sits on one specific ball and opens the lock with the same id.
- A key is **found** when its ball is part of its tube's selectable run (revealed, top run, tube not covered/clay). Check after every move, undo, reveal and cover removal. Found → the lock opens, the key mark disappears from the ball.
- Consequences: a key on a hidden ball can't be found until revealed; a key inside a covered tube can't be found until that tube is uncovered.
- A key on a hidden ball is **invisible** (no key mark) until the ball is revealed. A key's ball is only revealed by becoming the top of the tube a move just left (a shuffle never moves it), and that tube is open and not clay, so revealing the ball also finds the key, in the same move. Its events fire `BallRevealed`, then `KeyFound`, then `LockOpened` (order documented on `BoardState`), and the step 6 animations must play them in that order: the ball flips and shows its key, the key mark leaves, then the door opens.
- Validation: every lock has exactly one key and every key's lock exists; no key is in a selectable run at level start; lock tubes are adjacent in one row and have the same capacity; no key in a clay tube; lock ids are unique; a ball holds at most one key; no key inside a tube covered by its own lock.
- Level design note: put keys on the top ball of a run so the visual matches.

## Clay
- A clay tube is **never a move source**; it is a normal target (empty, or top color matches).
- Starts empty or with balls of **one color only**. No hidden balls and no keys in clay.
- Any capacity can be clay. Usual design (not a rule): a large clay tube with 1 starting ball of the color that will fill it.

## Large tubes
- Normal tubes have capacity 4. Large tubes span grid rows: capacity **5–10 spans 2 rows**, **11–16 spans 3 rows**.
- A large tube is **bottom-aligned** with the bottom row of its span; capacity 10 (2 rows) and 16 (3 rows) also align with the top row exactly.
- Apart from capacity and layout, a large tube is an ordinary tube: it **need not be clay** and may hold hidden balls, keys or a cover like any other.

## Board layout (grid)
- Every level has an explicit `layout.grid`: rows of cells (top row first), each cell a tube index or `null`. Max **3 rows**, max **6 cells per row** in level files (added tubes may extend a row to 8 during play).
- Geometry: row height = a capacity-4 tube; gap between rows = **2 ball slots** (also the lift space), so rows sit 6 slots apart, base line to base line. So 2 rows = 10 slots, 3 rows = 16 slots.
- **Aligned rows** = rows that contain any part of a large tube. Aligned rows share column positions: cell *i* is in the same column in each of them, and the aligned block is centered on screen by its widest row (shorter aligned rows are therefore left-aligned with it, not centered). A large tube appears in the same column in every row it spans, and its span must match its capacity. `null` = an empty gap, allowed only in aligned rows.
- **Free rows** = rows with no large tube. Each is centered on its own by its tube count; columns don't need to line up with other rows. No `null` cells.
- Example: 3 rows with a large tube spanning rows 2–3 → row 1 is free (centered), rows 2–3 are aligned.
- **Board scale** = fit the widest row and the row count to the board area. It is computed at load and only recomputed when an added tube makes the widest row wider (tubes and balls shrink to fit). The number of rows never changes during play.

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
- **Valid result:** it differs from the current arrangement, no tube is completed, no key is findable (no key in a selectable run), and the board is solvable (solver, Fast mode; a result the solver can't confirm within its node limit counts as unsolvable).
- **Limits** (constants in `BoardShuffler`): up to **20** random arrangements (`MaxAttempts`), each solved within **2,000** nodes (`NodeLimit`). Basis (Editor, random solvable boards up to the full 3×6 grid): a shuffle takes 1–6 ms and a solvable candidate needed at most ~130 nodes; the worst case, every attempt failing at the limit, is ~1 s on 18 tubes.
  - Re-measured on the delivered levels (Editor). On the largest, `level_028` (13 tubes, 44 balls the shuffle can move), 400 shuffles, from the start and from 25/50/75% through a solution in each variant, all succeeded: median 0.9 ms, max 3.2 ms. Across all 100 levels (4,000 shuffles): median 0.4 ms, max 35 ms; the 13 that found no valid arrangement gave up within 2.2 ms. The worst case was never seen; timed from a 2,000-node search on the 13-tube levels it would be 1.2–3 s (`level_051`, with its capacity-16 tube, is the slowest per node). The limits are kept.
- **Timing:** the search runs off the main thread with input locked; "Shuffling…" appears only if it takes longer than 0.3 s (docs/screens.md).
- **No valid shuffle found:** nothing changes, the booster isn't spent, and a toast says so.
- **Success:** spends one Shuffle booster and **clears the undo history** (the recorded moves no longer match the board). A shuffle is not undoable.
- Like every booster, unavailable once the level is won or while stuck.