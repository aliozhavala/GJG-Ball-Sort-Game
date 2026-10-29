# Level import check

The 100 delivered level files in `Assets/Data/LevelsIncoming/`, as committed ("Raw level delivery from product"), checked on 29 October 2026. **No level file was changed by the check.**

> **Imported since:** all 100 files now live in `Assets/Data/Levels/` and are listed in order in the level database
> (slot n = level n). The only change to them is `meta.optimalMoves`: the 25 values that were too high were corrected
> and the 39 missing ones added (64 lines in all), so every variant now has the checked number except level 51 V3,
> which stays empty. Your `difficulty` and `notes` are untouched.

Every file went through the same checks as **Ball Sort ▸ Validate Levels…**: it must load, follow every rule in `docs/level-format-guide.md` (including "same board in every variant"), and have an `id` that matches its file name. Then every variant was solved twice: once to prove it can be won (the solution was also played move by move through the game's rules to confirm it wins), and once to find its **shortest solution**, the number that goes into `meta.optimalMoves`.

## Summary

- **Files OK: 100 of 100. Files with problems: 0.**
- All 500 variants are valid and can be won.
- Shortest solution found for 499 of 500 variants. Level 51 V3 is between 36 and 38 moves (the search ran out of budget before it could tell which). That is a limit of the checker, not a problem with the level.
- **Highest number of ball colours: 10**, in 11 levels (49, 59, 64, 69, 74, 79, 84, 89, 94, 99, 100). No level uses more than 10, so the artist's 10 ball sprites cover every level. The placeholder colour palette has 12 colours, so no ball shows up magenta.
- Lock colours: the highest lock number used is 2 (level 99); the lock palette has 4 colours (0–3), so every door gets its own colour.
- Warnings (things to look at, not errors) are listed below: 8 levels whose V2 differs a lot from V1, 16 levels with a V3–V5 that may not be easier than V1, one tier to double-check, and a few smaller notes.

## Problems

None. Every file loads, follows every rule, and every variant can be won.

## Warnings

### V2's shortest solution very different from V1's

Tries 1 and 2 play V1 and V2, so a player who fails V1 once meets V2 next. Listed when the shortest solutions differ by 3 moves or more (and by more than 10%).

| Level | V1 moves | V2 moves | Difference |
|---|---|---|---|
| 21 | 24 | 29 | +5 (+21%) |
| 28 | 37 | 32 | -5 (-14%) |
| 70 | 36 | 32 | -4 (-11%) |
| 87 | 34 | 38 | +4 (+12%) |
| 97 | 37 | 33 | -4 (-11%) |
| 39 | 27 | 30 | +3 (+11%) |
| 88 | 25 | 22 | -3 (-12%) |
| 91 | 25 | 28 | +3 (+12%) |

### V3–V5 not easier than V1

From the 6th try on, players only see V3, V4 and V5, so these should be the gentler ones. Two measures were used: your bot's `difficulty` from each variant's `meta`, and the shortest solution.

**Rated as hard as V1 or harder by your own difficulty score:**

| Level | Variant | V1 difficulty | Variant difficulty | V1 moves | Variant moves |
|---|---|---|---|---|---|
| 33 | V4 | 0.21 | 0.32 | 20 | 19 |
| 33 | V5 | 0.21 | 0.33 | 20 | 14 |
| 35 | V4 | 0.22 | 0.22 | 33 | 35 |
| 37 | V5 | 0.31 | 0.40 | 29 | 28 |
| 65 | V4 | 0.38 | 0.40 | 29 | 25 |
| 65 | V5 | 0.38 | 0.44 | 29 | 28 |
| 68 | V5 | 0.44 | 0.44 | 29 | 29 |

**Shortest solution 3 or more moves longer than V1** (your difficulty score rates all of these easier than V1, so they may be fine; worth a playtest):

| Level | Variant | V1 moves | Variant moves |
|---|---|---|---|
| 5 | V4 | 14 | 17 (+3) |
| 17 | V5 | 19 | 23 (+4) |
| 21 | V3 | 24 | 30 (+6) |
| 21 | V4 | 24 | 30 (+6) |
| 21 | V5 | 24 | 30 (+6) |
| 42 | V4 | 20 | 24 (+4) |
| 54 | V5 | 23 | 26 (+3) |
| 64 | V5 | 30 | 33 (+3) |
| 79 | V4 | 33 | 36 (+3) |
| 80 | V3 | 20 | 23 (+3) |
| 80 | V4 | 20 | 23 (+3) |
| 80 | V5 | 20 | 23 (+3) |
| 91 | V3 | 25 | 30 (+5) |
| 91 | V4 | 25 | 30 (+5) |
| 91 | V5 | 25 | 30 (+5) |
| 92 | V5 | 34 | 37 (+3) |
| 94 | V4 | 31 | 35 (+4) |

Another 31 levels have a V3–V5 that is only 1–2 moves longer than V1 while your score rates it easier: 6, 8, 10, 11, 12, 20, 23, 28, 29, 31, 34, 36, 38, 39, 44, 45, 50, 52, 56, 57, 58, 59, 60, 66, 71, 73, 74, 78, 85, 86, 93. Their numbers are in the overview below.

Levels 1–4 are left out above: all five variants are the same puzzle (some with the colours swapped), so V3–V5 cannot be easier. That seems fine for the first levels.

### Tiers and tutorial flags

- **Tiers:** every level has one, and the pattern is clear: regular, with hard on 14, 24, … 94 and super hard on 19, 29, … 99, plus 100. Nothing looks missing.
- **One to check: level 100** is super hard, but your difficulty score rates its V1 at 0.66, lower than every other super hard level from 29 on (0.68–0.93) and lower than hard levels 54, 64, 84, 94. If it is meant as the finale, it may need tightening; otherwise "hard" may fit better.
- **Tutorial flags:** set on 1, 7, 20, 30, 55, 80. Each mechanic's first level is flagged: hidden balls (7), clay and large tubes together (20), veil (30), lock & key (80); level 55 is the first clay tube of normal size. Nothing looks missing.
- Small notes: level 20 introduces two mechanics at once (clay and a large tube). Every large tube in the set is also clay, so players never meet a large tube they can take balls from (allowed, just so you know). Level 51 is the first 3-row tube (capacity 16) and is not flagged as a tutorial; flag it if it should be.

### Other notes

- **`meta.optimalMoves`:** 25 values in the files are higher than the real shortest solution, and 40 are missing. When the levels are imported, every variant gets the checked number (your `difficulty` and `notes` stay as they are). The ones that will change, as level/variant (file → real): 31 V4 (16 → 15), 31 V5 (20 → 19), 32 V1 (19 → 18), 33 V2 (22 → 21), 37 V1 (30 → 29), 37 V2 (30 → 29), 40 V3 (37 → 36), 53 V2 (29 → 28), 64 V2 (32 → 31), 67 V4 (35 → 34), 72 V3 (36 → 35), 75 V3 (37 → 36), 75 V4 (37 → 36), 75 V5 (37 → 36), 78 V4 (24 → 23), 79 V3 (36 → 35), 79 V4 (37 → 36), 88 V1 (26 → 25), 90 V2 (39 → 38), 93 V3 (35 → 34), 93 V4 (35 → 34), 93 V5 (35 → 34), 94 V5 (33 → 31), 95 V3 (37 → 36), 96 V2 (26 → 25).
- **Identical variants:** level 1 (V1, V2 and V3 are identical; V4 and V5 are identical); level 2 (V1 and V4 are identical; V3 and V5 are identical); level 3 (V1 and V4 are identical; V2 and V3 are identical). Their notes say "copy of V1 with new colours", but these copies kept the same colours, so a retrying player gets the very same puzzle. Harmless this early.
- **V3, V4 and V5 are the same puzzle with the colours swapped** in levels 12, 29, 39, 44, 69, 71, 73, 75, 77, 80, 91, 93, 96. From the 6th try on the game rotates between them, so in these levels only the colours change. Fine if intended.
- **Mechanics that differ between variants** (allowed): level 17 has no hidden balls in V1; level 77 has no hidden balls in V2.

## Overview

Tubes / colours are the same in every variant of a level. V1–V5 are the shortest solutions in moves. "36–38" means it can be won in 38 moves and certainly not in fewer than 36; the exact number was not settled.

| Level | Tier | Tutorial | Tubes / colours | Mechanics | V1 | V2 | V3 | V4 | V5 |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Regular | yes | 3 / 2 | – | 4 | 4 | 4 | 4 | 4 |
| 2 | Regular |  | 4 / 3 | – | 8 | 8 | 8 | 8 | 8 |
| 3 | Regular |  | 5 / 3 | – | 9 | 9 | 9 | 9 | 9 |
| 4 | Regular |  | 6 / 4 | – | 12 | 12 | 12 | 12 | 12 |
| 5 | Regular |  | 7 / 5 | – | 14 | 16 | 13 | 17 | 15 |
| 6 | Regular |  | 7 / 5 | – | 15 | 17 | 15 | 16 | 14 |
| 7 | Regular | yes | 7 / 5 | hidden balls | 17 | 17 | 16 | 17 | 16 |
| 8 | Regular |  | 7 / 5 | hidden balls | 16 | 16 | 17 | 15 | 14 |
| 9 | Regular |  | 8 / 6 | hidden balls | 19 | 17 | 18 | 19 | 19 |
| 10 | Regular |  | 7 / 5 | hidden balls | 16 | 16 | 15 | 17 | 17 |
| 11 | Regular |  | 9 / 7 | hidden balls | 22 | 23 | 20 | 23 | 23 |
| 12 | Regular |  | 9 / 7 | hidden balls | 20 | 22 | 21 | 21 | 21 |
| 13 | Regular |  | 8 / 6 | hidden balls | 19 | 19 | 17 | 19 | 17 |
| 14 | Hard |  | 10 / 8 | hidden balls | 28 | 27 | 27 | 25 | 24 |
| 15 | Regular |  | 8 / 6 | hidden balls | 19 | 20 | 18 | 19 | 18 |
| 16 | Regular |  | 9 / 7 | hidden balls | 22 | 20 | 21 | 21 | 20 |
| 17 | Regular |  | 9 / 7 | hidden balls (not in V1) | 19 | 17 | 20 | 21 | 23 |
| 18 | Regular |  | 9 / 7 | hidden balls | 21 | 23 | 21 | 21 | 20 |
| 19 | Super hard |  | 11 / 9 | hidden balls | 29 | 29 | 29 | 28 | 29 |
| 20 | Regular | yes | 10 / 7 | clay, large tube | 27 | 27 | 25 | 28 | 28 |
| 21 | Regular |  | 11 / 8 | clay, large tube | 24 | 29 | 30 | 30 | 30 |
| 22 | Regular |  | 11 / 8 | clay, large tube | 32 | 31 | 29 | 31 | 32 |
| 23 | Regular |  | 9 / 7 | hidden balls | 21 | 22 | 20 | 20 | 23 |
| 24 | Hard |  | 9 / 7 | hidden balls | 23 | 23 | 22 | 21 | 22 |
| 25 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 35 | 36 | 32 | 34 | 35 |
| 26 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 35 | 35 | 33 | 33 | 33 |
| 27 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 33 | 33 | 33 | 31 | 31 |
| 28 | Regular |  | 13 / 9 | clay, large tube | 37 | 32 | 37 | 39 | 37 |
| 29 | Super hard |  | 11 / 9 | hidden balls | 28 | 28 | 29 | 29 | 29 |
| 30 | Regular | yes | 7 / 5 | veil | 16 | 16 | 13 | 13 | 12 |
| 31 | Regular |  | 8 / 6 | veil | 17 | 18 | 18 | 15 | 19 |
| 32 | Regular |  | 8 / 6 | veil | 18 | 16 | 17 | 15 | 17 |
| 33 | Regular |  | 9 / 7 | veil | 20 | 21 | 20 | 19 | 14 |
| 34 | Hard |  | 11 / 9 | hidden balls, veil | 28 | 28 | 29 | 27 | 29 |
| 35 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 33 | 32 | 33 | 35 | 33 |
| 36 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 34 | 33 | 34 | 36 | 35 |
| 37 | Regular |  | 11 / 9 | hidden balls, veil | 29 | 29 | 25 | 29 | 28 |
| 38 | Regular |  | 12 / 8 | hidden balls, veil, clay, large tube | 30 | 32 | 32 | 30 | 31 |
| 39 | Super hard |  | 11 / 9 | hidden balls, veil | 27 | 30 | 28 | 28 | 28 |
| 40 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 37 | 38 | 36 | 37 | 36 |
| 41 | Regular |  | 11 / 7 | hidden balls, veil, clay, large tube | 30 | 28 | 28 | 25 | 28 |
| 42 | Regular |  | 9 / 7 | hidden balls, veil | 20 | 22 | 20 | 24 | 19 |
| 43 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 35 | 35 | 32 | 33 | 35 |
| 44 | Hard |  | 11 / 9 | hidden balls, veil | 28 | 26 | 30 | 30 | 30 |
| 45 | Regular |  | 11 / 9 | hidden balls | 26 | 27 | 27 | 26 | 25 |
| 46 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 36 | 38 | 36 | 32 | 35 |
| 47 | Regular |  | 13 / 9 | hidden balls, veil, clay, large tube | 36 | 34 | 35 | 35 | 34 |
| 48 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 34 | 35 | 34 | 33 | 31 |
| 49 | Super hard |  | 12 / 10 | hidden balls, veil | 32 | 29 | 31 | 31 | 30 |
| 50 | Regular |  | 9 / 7 | hidden balls | 22 | 22 | 20 | 21 | 24 |
| 51 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 35 | 37 | 36–38 | 34 | 32 |
| 52 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 35 | 35 | 34 | 36 | 34 |
| 53 | Regular |  | 11 / 9 | hidden balls, veil | 30 | 28 | 29 | 29 | 29 |
| 54 | Hard |  | 10 / 8 | hidden balls, veil | 23 | 24 | 25 | 22 | 26 |
| 55 | Regular | yes | 9 / 7 | clay | 22 | 20 | 20 | 22 | 21 |
| 56 | Regular |  | 10 / 8 | clay | 24 | 25 | 24 | 26 | 24 |
| 57 | Regular |  | 10 / 8 | clay | 23 | 22 | 22 | 24 | 24 |
| 58 | Regular |  | 11 / 8 | clay | 24 | 24 | 22 | 25 | 22 |
| 59 | Super hard |  | 12 / 10 | hidden balls, clay | 29 | 30 | 30 | 30 | 28 |
| 60 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 37 | 38 | 38 | 38 | 34 |
| 61 | Regular |  | 12 / 9 | hidden balls, clay | 30 | 30 | 29 | 29 | 30 |
| 62 | Regular |  | 12 / 8 | hidden balls, veil, clay, large tube | 37 | 37 | 36 | 37 | 36 |
| 63 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 35 | 34 | 35 | 35 | 35 |
| 64 | Hard |  | 13 / 10 | hidden balls, veil, clay | 30 | 31 | 32 | 31 | 33 |
| 65 | Regular |  | 11 / 9 | hidden balls | 29 | 29 | 28 | 25 | 28 |
| 66 | Regular |  | 12 / 9 | hidden balls, veil, clay | 27 | 27 | 29 | 29 | 27 |
| 67 | Regular |  | 13 / 9 | hidden balls, veil, clay, large tube | 34 | 34 | 33 | 34 | 33 |
| 68 | Regular |  | 12 / 9 | hidden balls, clay | 29 | 29 | 28 | 26 | 29 |
| 69 | Super hard |  | 13 / 10 | hidden balls, veil, clay | 31 | 31 | 29 | 29 | 29 |
| 70 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 36 | 32 | 34 | 34 | 34 |
| 71 | Regular |  | 10 / 8 | hidden balls, veil | 24 | 23 | 26 | 26 | 26 |
| 72 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 36 | 35 | 35 | 35 | 35 |
| 73 | Regular |  | 11 / 9 | hidden balls, clay | 27 | 27 | 29 | 29 | 29 |
| 74 | Hard |  | 13 / 10 | hidden balls, veil, clay | 30 | 31 | 30 | 31 | 30 |
| 75 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 39 | 39 | 36 | 36 | 36 |
| 76 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 36 | 34 | 35 | 34 | 36 |
| 77 | Regular |  | 9 / 7 | hidden balls (not in V2), veil | 23 | 21 | 18 | 18 | 18 |
| 78 | Regular |  | 11 / 9 | hidden balls, veil | 25 | 25 | 26 | 23 | 26 |
| 79 | Super hard |  | 13 / 10 | hidden balls, veil, clay | 33 | 36 | 35 | 36 | 35 |
| 80 | Regular | yes | 9 / 7 | lock & key | 20 | 21 | 23 | 23 | 23 |
| 81 | Regular |  | 10 / 8 | lock & key | 26 | 24 | 24 | 22 | 24 |
| 82 | Regular |  | 9 / 7 | lock & key | 24 | 23 | 24 | 22 | 23 |
| 83 | Regular |  | 11 / 8 | lock & key, clay, large tube | 29 | 29 | 26 | 28 | 27 |
| 84 | Hard |  | 12 / 10 | hidden balls, lock & key | 33 | 33 | 33 | 33 | 33 |
| 85 | Regular |  | 13 / 8 | hidden balls, veil, lock & key, clay, large tube | 36 | 36 | 34 | 38 | 38 |
| 86 | Regular |  | 12 / 9 | hidden balls, lock & key, clay, large tube | 36 | 37 | 36 | 35 | 37 |
| 87 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 34 | 38 | 33 | 33 | 33 |
| 88 | Regular |  | 9 / 7 | hidden balls, lock & key | 25 | 22 | 21 | 23 | 24 |
| 89 | Super hard |  | 12 / 10 | hidden balls, veil, lock & key | 33 | 32 | 32 | 33 | 32 |
| 90 | Regular |  | 13 / 8 | hidden balls, veil, lock & key, clay, large tube | 39 | 38 | 38 | 37 | 36 |
| 91 | Regular |  | 11 / 9 | hidden balls, veil | 25 | 28 | 30 | 30 | 30 |
| 92 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 34 | 35 | 33 | 34 | 37 |
| 93 | Regular |  | 12 / 9 | hidden balls, lock & key, clay, large tube | 33 | 34 | 34 | 34 | 34 |
| 94 | Hard |  | 12 / 10 | hidden balls, veil, lock & key | 31 | 30 | 29 | 35 | 31 |
| 95 | Regular |  | 13 / 8 | hidden balls, veil, lock & key, clay, large tube | 37 | 37 | 36 | 37 | 36 |
| 96 | Regular |  | 11 / 9 | hidden balls, veil | 27 | 25 | 27 | 27 | 27 |
| 97 | Regular |  | 12 / 9 | hidden balls, clay, large tube | 37 | 33 | 35 | 36 | 33 |
| 98 | Regular |  | 13 / 8 | hidden balls, veil, clay, large tube | 38 | 38 | 35 | 34 | 32 |
| 99 | Super hard |  | 12 / 10 | hidden balls, lock & key | 35 | 33 | 34 | 33 | 30 |
| 100 | Super hard |  | 13 / 10 | hidden balls, veil, lock & key, clay | 34 | 33 | 33 | 31 | 34 |

## How this was checked

- The game's own level loader rules and solver (the code behind Validate Levels), run outside Unity on all 500 variants. Search budget: 500,000 positions per variant for the shortest solution (the Validate Levels default). 11 variants needed more and were re-run with 4,000,000; 10 of them were then settled.
- Solving is generous with knowledge: the solver knows what the hidden balls are, so its shortest solution is a best case. A player who has to discover the hidden balls usually needs more moves.
