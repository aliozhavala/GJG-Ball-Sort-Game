# Names and art

## Product names → code names
Product, art and the prototype use themed names. They map 1:1 onto Core; nothing new in Core.

| Product / art name | Code | In the level file |
|---|---|---|
| Bottle | tube | — |
| Creature | ball | — |
| Corked bottle | completed tube | — |
| "?" creature | hidden ball | `hidden` |
| Towel (badge shows a colour) | veil | `veil: { "color": c }` |
| Lock box (wooden crate + coloured padlock) | lock | `locks: [{ id, tubes }]` |
| Key (drawn on a creature) | key | `keys: [{ slot, lock }]` |
| Small ice bottle | capacity-4 clay tube, starts empty | `capacity: 4, clay: true` |
| Big ice bottle | large clay tube, capacity 8, starts with 2–4 balls of its colour (**ice skin**) | `capacity: 8, clay: true` |
| Big bottle | large clay tube, capacity 8 or 12, starts with 1 ball of its colour (**big-bottle skin**) | `capacity: 8 / 12, clay: true, skin: "bigBottle"` (proposed field) |

- The two large clay forms behave the same in Core: both start with their colour on top, so only that colour can go in. They differ in art, in where levels use them (level-plan.md) and in the completion effect. The view needs to know which one to draw, hence the proposed per-tube `skin`.
- **Large-clay colour** = the 8- or 12-ball colour that fills a large clay tube.
- **Home avatar** = our main character in a frame. It is not a player profile picture (a profile feature was proposed and dropped, see meta-ideas.md).

## Mechanic looks
State changes are instant in Core. Everything here is view-only and never blocks input. Timings are in feel.md.

| Core | Look |
|---|---|
| Hidden ball (`BallRevealed`) | The "?" creature flips to its real face. |
| Completed tube (`TubeCompleted`) | A wooden cork tinted in the tube's colour drops in. |
| Veil (`TubeUnveiled`) | A towel draped over the whole tube, with a round badge in the veil colour. When it opens, the towel lifts straight off (no fade first); then the balls show with their own hidden flags. |
| Lock (`KeyFound` → `LockOpened`) | A wooden crate over the locked tube(s) with a coloured padlock on the front. On unlock the key flies from its ball to the padlock, the padlock pops and the crate breaks open. The flight is cosmetic: Core has already opened the lock. |
| Clay, ice skin (small and big ice bottle) | A bottle standing in a block of ice. The ice breaks only when the tube completes (cracks, then shards); an unfinished one keeps its ice. |
| Clay, big-bottle skin | A tall glass bottle with a crystal base in its colour; the cork drops on completion. |

## Palettes
**Ball colours**, in colour id order. They go into `ColorPalette`; the artist's creature sprites replace the tints later.

| Id | Colour | Hex |
|---|---|---|
| 0 | red | `#E62E3A` |
| 1 | yellow | `#FFD23F` |
| 2 | blue | `#2F6BFF` |
| 3 | green | `#4FD13A` |
| 4 | purple | `#7B4DFF` |
| 5 | orange | `#FF7A1A` |
| 6 | teal | `#2EC4B6` |
| 7 | pink | `#FF8FD0` |
| 8 | brown | `#8B5A2B` |
| 9 | white | `#F2F2F7` |
| 10 | grey | `#8A93A8` |
| 11 | dark green | `#1B7A44` |

- 12 colours, the current palette size. The prototype named this palette after a film character; here it is the **Lab palette**.
- **Colour-blind symbols:** a small symbol per colour drawn on the creature, off by default, with a Settings toggle later. The symbol sprite lives in `ColorPalette`.

> **Open issue:** green / dark green and white / grey are close. Check them with the symbols on.

**Lock colours** (6, separate from the ball colours; a lock and its key mark share one): pink, blue, green, yellow, purple, orange.

> **Open issue:** all six hues are also ball colours, so a player may read a blue padlock as "opened by blue balls". Options: hues no ball uses (e.g. metallic), or a shape or icon per lock. Team and art decide.

## Creatures
- The creature design is the artist's call; the view only swaps sprites.
- Faces: idle (one per colour), a blink loop, **shock** when lifted or on an invalid move, **happy** when its tube completes and on the win.

## UI kit
- One **popup kit** prefab: card, title tab, close X, inner panel, button row; colour variants purple and red (art brief §B9 in the product repo).
- Buttons in four colours: green (confirm / buy), magenta (ad), purple (neutral), red (destructive). Each has normal / pressed / disabled states and is 9-sliced.
- A pill prefab for coins and lives.
- The look values (colours, sprites, punch scale, ease) are Inspector fields.

## Scenes and board
- One background per episode (level-plan.md → Episodes), on Home and in the level.
- Home centre: the current episode's background with creatures bobbing over a cauldron (idle loop and blink).
- Episode card art for the Episode Unlocked overlay (meta-ideas.md).
- Board: tubes close together and big, about **1.42 ball diameters** centre to centre, the board filling the width (product D30). Today's view config gives about 1.72 ((1.1 tube + 0.45 gap) / 0.9 ball), and `BoardLayout`'s scale cap may stop it filling the width.

## References (product repo)
- Screens: `docs/art/shots/` (`01_home.jpg`, `21_gameplay_level1.jpg`, `41_win_logo.jpg`, …).
- Asset list: `docs/art/ART_ASSET_BRIEF.md`.
