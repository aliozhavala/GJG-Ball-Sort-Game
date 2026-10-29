# Level format guide

A practical guide to writing level files by hand: the format, one complete example, the rules every file must follow,
and how to check your work in the Editor. The developer reference is `docs/levels.md`; the mechanics are explained in
`docs/mechanics.md`.

## What a level file is

One level = one JSON file. It describes **one board** (where the tubes stand) and **five variants**: five puzzles on
that board. A player's first attempt at a level plays V1. Every attempt that ends without a win (leaving, restarting or
closing the app mid-level, each of which costs a life) moves them on to the next: V2, and so on up to V5. From the sixth
attempt on, the game picks V3, V4 or V5 at random, never the same one twice in a row. Winning resets the count, so every
level starts at V1.
So V1 is what everyone sees first, and V3–V5 are what players who keep retrying see again and again.

- Shipped levels: `Assets/Data/Levels/level_001.json` … `level_100.json`. The number in the file name must equal the
  file's `id`, and the file must be listed, in order, in `Assets/Data/LevelDatabase.asset`.
- Drafts and experiments: `Assets/Data/TestLevels/` (any file name, ids from 901). They never ship and are never in the
  database.

Balls are listed **bottom to top**. Tubes are numbered from 0, in the order they are listed.

## The format

### Top level

| Field | Required | Meaning |
|---|---|---|
| `formatVersion` | yes | Always `2`. |
| `id` | yes | The level number, 1–100 (matches the file name). |
| `tier` | yes | `"regular"`, `"hard"` or `"superHard"`. Sets the win reward (20 / 40 / 60 coins) and the Level button's look on Home. |
| `tutorial` | no | `true` for a level that introduces a mechanic. The first time a player enters it, a popup shows its tutorial text, written in `Assets/Data/Config/TutorialConfig.asset` under the level's id. |
| `layout` | yes | `{ "grid": [...] }`: where each tube stands. Shared by all five variants. |
| `variants` | yes | Exactly 5 variants, V1 first. |

### Layout grid

Rows top to bottom, each row a list of tube numbers: `[[0, 1, 2], [3, 4]]` puts tubes 0–2 in the top row and 3–4 below.
At most 3 rows and 6 tubes per row. A large tube (capacity 5–10 spans 2 rows, 11–16 spans 3) is listed in every row it
covers, in the same position; `null` leaves a gap, allowed only in rows that contain a large tube.

### Each variant

| Field | Required | Meaning |
|---|---|---|
| `colors` | yes | Number of ball colours in this variant; colours are numbered 0 to `colors - 1`. |
| `tubes` | yes | The tubes, in order (see below). |
| `locks` | no | `[{ "id": 0, "tubes": [3, 4] }]`: one door over the listed tubes. |
| `meta` | no | Notes for the team; the game never reads it: `optimalMoves` (Validate Levels fills it in), `difficulty` (0–1), `notes` (free text). |

### Each tube

| Field | Required | Meaning |
|---|---|---|
| `capacity` | yes | 4 for a normal tube, 5–16 for a large tube. |
| `balls` | yes | Colour numbers, bottom to top. `[]` for an empty tube. |
| `hidden` | no | One `true`/`false` per ball: `true` shows the ball as "?" until a move makes it the top ball. |
| `veil` | no | `{ "color": 2 }`: covered until a tube of that colour is completed. |
| `clay` | no | `true`: balls can go in, never come out. |
| `keys` | no | `[{ "slot": 1, "lock": 0 }]`: a key on ball number `slot` (0 = bottom) that opens lock `lock`. |

### Colour and lock numbers

Ball colours (placeholder art): 0 red, 1 blue, 2 yellow, 3 green, 4 purple, 5 orange, 6 teal, 7 pink, 8 lime,
9 brown, 10 navy, 11 white. At most 12 colours per variant.

Lock numbers also choose the door colour: 0 mint, 1 lavender, 2 peach, 3 magenta. Use only 0–3.

## A complete example

A hard lock-and-key level. The door covers tubes 3 and 4 in every variant; its key sits under the two balls on top of
tube 0. V2 uses another door colour, V3 moves the key and hides a ball, V4 and V5 move the tubes around.

```json
{
  "formatVersion": 2,
  "id": 12,
  "tier": "hard",
  "layout": { "grid": [[0, 1, 2], [3, 4]] },
  "variants": [
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [1, 2, 0, 0], "keys": [{ "slot": 1, "lock": 0 }] },
        { "capacity": 4, "balls": [2, 1, 0, 0] },
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [2, 1, 2, 1] },
        { "capacity": 4, "balls": [] }
      ], "locks": [{ "id": 0, "tubes": [3, 4] }], "meta": { "optimalMoves": 8, "notes": "Move the two reds off tube 0 to find the key." } },
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [2, 0, 1, 1], "keys": [{ "slot": 1, "lock": 2 }] },
        { "capacity": 4, "balls": [0, 2, 1, 1] },
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [0, 2, 0, 2] },
        { "capacity": 4, "balls": [] }
      ], "locks": [{ "id": 2, "tubes": [3, 4] }], "meta": { "optimalMoves": 8, "notes": "Same door, peach this time (lock 2)." } },
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [1, 0, 2, 2], "hidden": [true, false, false, false] },
        { "capacity": 4, "balls": [0, 1, 2, 2], "keys": [{ "slot": 1, "lock": 0 }] },
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [1, 0, 1, 0] },
        { "capacity": 4, "balls": [] }
      ], "locks": [{ "id": 0, "tubes": [3, 4] }], "meta": { "notes": "The key moved to tube 1; tube 0 hides its bottom ball." } },
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [1, 2, 0, 0] },
        { "capacity": 4, "balls": [2, 1, 0, 0], "keys": [{ "slot": 1, "lock": 0 }] },
        { "capacity": 4, "balls": [1, 2, 1, 2] },
        { "capacity": 4, "balls": [] }
      ], "locks": [{ "id": 0, "tubes": [3, 4] }], "meta": { "optimalMoves": 8 } },
    { "colors": 3, "tubes": [
        { "capacity": 4, "balls": [1, 0, 2, 2], "keys": [{ "slot": 1, "lock": 0 }] },
        { "capacity": 4, "balls": [] },
        { "capacity": 4, "balls": [0, 1, 2, 2] },
        { "capacity": 4, "balls": [0, 1, 0, 1] },
        { "capacity": 4, "balls": [] }
      ], "locks": [{ "id": 0, "tubes": [3, 4] }], "meta": { "optimalMoves": 8 } }
  ]
}
```

Tip: write V1, copy it four times, then change the copies. One tube per line keeps files readable and easy to compare.

## Rules

The game refuses a file that breaks any of these, and lists every problem it finds.

### The file
- `formatVersion` is `2`, `id` matches the file number, `tier` is one of the three tiers, and there are exactly
  **5 variants**.
- Old files (`formatVersion` 1, one puzzle per file) are refused with a message to convert them.
- Field names must be spelled exactly as above; an unknown field (a typo such as `"colours"` or `"hiden"`) is an error.

### Same board in every variant
All five variants are played on the same board, so these must be identical in every variant:
- the number of tubes, and for each tube: its `capacity`, whether it is `clay`, and what covers it (nothing, a veil or
  a lock);
- **the tubes each lock covers.** A lock over tubes 3 and 4 in V1 must cover exactly tubes 3 and 4 in V2–V5 (not
  3 alone, not 4 and 5, not two separate locks).

Everything else may change from one variant to the next: the balls and their colours, the number of colours, which
balls are hidden, where the keys are, the veil colours, and **lock ids, which set the door colour**. In the example, V2
uses lock 2 (peach) where the others use lock 0 (mint), and its key says `"lock": 2` to match.

### Each variant
- Every colour from 0 to `colors - 1` is used, and each colour has exactly as many balls as some tube's capacity (4 for
  normal tubes; 10 balls of one colour need a capacity-10 tube), with enough tubes of that capacity for all such colours.
- No tube holds more balls than its capacity, and every tube's top ball is visible (`hidden` is `false` for the top).
- `hidden`, when present, has one entry per ball.
- The puzzle is not finished at the start (no tube already complete) and has at least one legal move.
- **It is solvable.** The game does not check this when loading; Validate Levels and the automated tests do (below).

### Mechanics (per variant)
- **Veil:** its colour exists in the variant. A tube has at most one cover (a veil or a lock, not both). A covered tube
  must not already hold a finished set.
- **Locks:** a lock's tubes stand next to each other in one row and have the same capacity. Every lock has exactly one
  key, and every key's lock exists. A key is not on one of the top balls that could be picked up at the start (it would
  open the door immediately), not in a clay tube, and not inside a tube its own lock covers. A ball holds at most one key.
- **Clay:** starts empty or with one colour only; no hidden balls and no keys in clay.
- **Large tubes:** capacity 5–10 spans 2 rows of the grid, 11–16 spans 3, and the grid must list them that way.

## Checking your levels

### Ball Sort ▸ Validate Levels…
Checks every level in the level database.
1. Open **Ball Sort ▸ Validate Levels…** and press **Validate all levels**.
2. Each **variant** gets its own row: level, variant (V1–V5), colours / tubes, mechanics, **Solvable** (`yes`, `NO`,
   `unknown` or `invalid`), the shortest solution (**Optimal**), and solver statistics. Problems appear in red under the
   row they belong to; a row marked `file` holds problems of the file as a whole (a typo, a broken grid, a missing tier).
3. Every row should say **Solvable: yes**. `NO` means that variant can never be won; `unknown` means the solver gave
   up, so raise the node limits at the top and run it again.
4. Press **Write meta.optimalMoves** to store each variant's shortest solution in its `meta`. Only that number changes;
   the rest of the file stays exactly as you wrote it.

Error messages name the level, the variant and the tube, for example:
- `level_012: variant 3: tubes[1]: top ball is hidden; every tube's top ball must start revealed.`
- `level_012: variant 2: tubes[3]: has no cover, but variant 1 has a lock.`
- `level_012: variant 4: locks[0] covers tubes 4, but no lock in variant 1 covers exactly those tubes.`

### Ball Sort ▸ Play Level…
Plays any level in the Editor.
1. Open **Ball Sort ▸ Play Level…** and pick a **Variant** (V1–V5) with the slider.
2. Either pick a level of the database and press **Play level N, Vk**, or drop a file (for example a draft from
   `Assets/Data/TestLevels/`) into **Level file** and press **Play**.
3. The game opens on that level and variant. Restart replays the same variant. These visits are tests: winning pays
   no coins and moves no progress, and leaving costs no life. Every row in Validate Levels also has a **Play** button for its variant.

Drafts in `TestLevels/` are not in the database, so Validate Levels does not list them. Play them with **Level file**:
a file with problems sends the game back to Home with "Level … could not be loaded", and the Console lists every problem.
