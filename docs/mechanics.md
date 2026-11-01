# Mechanics & Board Layout

Read this when working on Core rules, level files, validation, the solver, or board views. `docs/levels.md` holds the JSON schema; this file holds the rules the schema expresses. If they disagree, this file wins and `docs/levels.md` must be fixed.

## Completion (all mechanics)
A tube is **completed** when it is full, all its balls are revealed, and it holds **every ball of that color in the level**. So 4 purples in a capacity-4 tube are not a completion when the level has 10 purples — those balls can still be moved out. Completion seals undo history (unchanged).

Validation: for each color, its ball count must equal the capacity of some tube, and for each capacity value, the number of colors with that count must not exceed the number of tubes with that capacity.

## Covers: veil and lock
A **covered** tube is neither a move source nor a target, and its contents are drawn covered (not visible). A tube has at most one cover. A veil may cover any tube, large tubes included; a lock covers only capacity-4 tubes (Lock and key, below). When the cover is removed, the tube behaves normally and its balls follow their own `hidden` flags. Removing a cover is permanent (never undone).
- Validation: a covered tube must not already hold a finished set (it would complete the moment its cover opened, and removing a cover never completes a tube).

### Cover visuals
- **Veil:** the towel hangs from the rim over the tube, shortened by scaling it vertically (`TubeVisualConfig._veilHeightScale`, 0.92) so its hem crosses the bottom ball: that ball's lower half shows below the hem, drawn as it would be uncovered ("?" if hidden, its key if revealed and keyed). Every other ball in the tube is hidden (not drawn, so the towel may be translucent). On taller tubes the hem stays at the same height and the stripes stretch. In the veil's middle sits a sticker: the open-eyes ball of the colour that opens it (`ColorPalette`, body `TubeVisualConfig._veilStickerSize` wide).
- **Lock:** one lock box across all the lock's tubes hides every ball in them (`LockBoxView`, part of the lock's `CoverView`). Closed: `LockBox_Side` at both edges (the left one mirrored), `LockBox_Face` tiled from each side towards the centre (mirrored on the right) and the lock's coloured `LockBox_Mid_<Colour>` (lamp and triangular key slot) in the centre. Every part is drawn at one scale, so the box is as tall as a capacity-4 tube from below its base to above its rim, plus `TubeVisualConfig._lockBoxPadding`, and as wide as the tubes' outer rims plus that padding; only the face changes with the width. Opening (docs/animation-spec.md 3.3), the key flies into the mid's slot, the mid gives way to two `LockBox_Cover` halves that slide apart over `LockBox_Inside` (behind the tubes), and the box fades.
- Opening a cover shows all the tube's balls (`CoverView.SetOpen`, the single entry point). Showing the bottom ball changes no rule: a veiled tube is still neither a source nor a target.

### Veil
- A veil has one color (`veil.color`, a ball color id). It opens **only** when a tube completes in **that color**, by a move or by an undo. Completing a tube of any other color does nothing to it, and neither does filling a tube with only some of the veil's color (that is not a completion).
- Every veil of the completed color opens at once.
- Validation: the veil color exists in the level.

### Lock and key
- A **lock** covers one or more tubes that are **adjacent in the same grid row** and all have **capacity 4**: one lock box spans them all, and its art is drawn one row high, so a lock never covers a large tube. Locks have an `id`; their colour (the box's mid section and the key) comes from a separate lock palette (`LockPalette`, never confused with ball colours).
- A **key** sits on one specific ball and opens the lock with the same id.
- A key is **found** when its ball is part of its tube's selectable run (revealed, top run, tube not covered/clay). Check after every move, undo, reveal and cover removal. Found → the lock opens, the key disappears from the ball.
- Consequences: a key on a hidden ball can't be found until revealed; a key inside a covered tube can't be found until that tube is uncovered.
- A key is drawn in the monster's hands on the ball carrying it: the lock's key sprite (`LockPalette`), untinted, `ColorPalette._keyMarkSize` wide at `_keyMarkOffset` (below the eye, between the arms).
- A key on a hidden ball is **invisible** (no key drawn) until the ball is revealed. A key's ball is only revealed by becoming the top of a tube: the tube a move just left, or a tube the Shuffle booster rearranged. Either tube is open and not clay, so revealing the ball also finds the key, in the same operation. Its events fire `BallRevealed`, then `KeyFound`, then `LockOpened` (order documented on `BoardState`), and the step 6 animations must play them in that order: the ball flips and shows its key, the key leaves, then the lock box opens.
- Validation: every lock has exactly one key and every key's lock exists; no key is in a selectable run at level start; lock tubes are adjacent in one row and each has capacity 4 (every other capacity is reported per tube); the delivered levels 1–100 break none of it (checked 2026-11-01: 105 locks over 1–5 tubes, all capacity 4); no key in a clay tube; lock ids are unique; a ball holds at most one key; no key inside a tube covered by its own lock.
- Level design note: put keys on the top ball of a run so the visual matches.

## Clay
- A clay tube is **never a move source**; it is a normal target (empty, or top color matches).
- Starts empty or with balls of **one color only**. No hidden balls and no keys in clay.
- Drawn as ice at the tube's base, over the balls inside it and under balls in flight, 1.1× the rim's width (`TubeVisualConfig._clayScale`). It disappears when the tube completes (`TubeView.SetClayShown`; a snap until its animation exists).
- Any capacity can be clay. Usual design (not a rule): a large clay tube with 1 starting ball of the color that will fill it.

## Large tubes
- Normal tubes have capacity 4. Large tubes span grid rows: capacity **5–10 spans 2 rows**, **11–16 spans 3 rows**.
- A large tube is **bottom-aligned** with the bottom row of its span; capacity 10 (2 rows) and 16 (3 rows) also align with the top row exactly.
- Apart from capacity and layout, a large tube is an ordinary tube: it **need not be clay** and may hold hidden balls, keys or a veil like any other. It cannot be locked (Lock and key).

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
- Stuck = not won, and either no legal move exists, or both: (a) no position reachable from the current one is a win, and (b) every reachable position can get back to the current one (the reachable set is one strongly connected loop). Any one-way move still available (a reveal, a completion, a key found, a cover opened, a run lifted off a different colour, or simply a position with no way back) means not stuck, even if it leads to a dead end: the player may foresee it and use a booster first.
- `StuckDetector` (Core) decides it with the real rules and `BoardStateKey`. Hidden balls, covered tubes, unfound keys, completed tubes and balls sitting on a different colour (or on a hidden ball) only change one way, so a move that changes any of them ends the search as one-way. The other moves are explored up to `StuckDetector.DefaultPositionLimit` positions (hitting the limit means not stuck); then a backward search from the current position must reach every recorded one. Evaluated after every move, undo, added tube (booster or Play on), shuffle and restart.
- Cost, measured in the Editor: random play on every delivered level and variant (15,307 checks) averaged 0.02 ms per check, and no check recorded more than 9 positions. The search costs about 0.1 ms per position at 13 tubes (the largest delivered levels), so the limit is 32 positions: about 3.5–4.5 ms when it is reached (measured on synthetic one-colour 13-tube boards built to keep every move reversible). A few hundred positions would cost 30 ms or more.

## Shuffle (booster)
The Shuffle booster rearranges **one tube the player picks** (the pick itself: docs/screens.md, Boosters). Rules in
`ShuffleRules`, applied by `LevelSession.TryShuffleTube`.
- **What the player sees decides, never hidden colours.** A ball's *look* is "?" when hidden, otherwise its colour plus
  its key if it carries one (each lock's key has its own colour). A hidden ball's key is invisible, so it does
  not count. Nothing here reads the colour of a hidden ball, so neither the pick nor the result can leak it.
- **Eligible tube:** not covered (veil or lock), not clay, not completed, and holding at least two balls that look
  different. With none eligible the booster is refused ("No tube to shuffle") and nothing is spent.
- **The new order:** the tube's own balls, so it keeps its count and colours, in an order whose look differs from the
  current one in at least one slot (compared with the new top already revealed). It is a random order (Fisher–Yates),
  tried up to 8 times; if every try looks the same, the top is swapped with a ball that looks different, which always
  shows. Every other tube is untouched.
- **Hidden balls** keep their hidden flag, except a hidden ball that lands on **top**, which is revealed.
- **Keys move with their balls.** A key that ends up in the tube's selectable run is found and opens its lock through
  the normal cascade, as after a move.
- **Completion:** a full tube of one colour with a hidden ball in it is eligible ("?" looks different); if the shuffle
  puts the hidden ball on top, revealing it completes the tube, which corks, opens the veils of its colour and may win
  the level, as after a move.
- **No solvability check:** the shuffle may leave the level unwinnable or immediately stuck. Stuck is evaluated
  afterwards, as after every operation.
- **Cost:** spends one Shuffle booster and **clears the undo history** (the recorded moves no longer match the board).
  Not undoable. Like every booster, refused (nothing spent) once the level is won, while stuck, or with no boosters left;
  an ineligible tube is refused too (`ShuffleBlockReason.TubeNotEligible`).
- **Events:** `Shuffled` (the tube and its new order, bottom → top, the top revealed and keys where they landed), then
  as after a move `BallRevealed` (the new top, if it was hidden), `TubeCompleted`, each `TubeUnveiled`, each `KeyFound`
  and its `LockOpened`, `LevelCompleted`; then `StuckChanged` (if it changed) and `Changed`.
- The view snaps the tube's balls into the new order (`BoardView`); its animation is not made yet.