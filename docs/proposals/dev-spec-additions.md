# Proposed additions to the dev specs

Written by the developer side on 28 Oct 2026, after reviewing product's proposals (`levels.md`, `mechanics.md`, `screens.md` in this folder). **Nothing here is applied.** It lists only the additions the review found fair, good or necessary for CLAUDE.md and `docs/*.md`. Left out:
- items that need a team decision (listed in `docs/design/decisions.md`);
- rejected items;
- pure design intent, which now lives in `docs/design/`.

Each item gives the target file and section, the text to paste, the reason, the code it needs, and the matching row of the review table. Apply an item that needs code together with that code and its EditMode tests (CLAUDE.md), not before, so the docs keep describing the real system.

## CLAUDE.md

### A1 · Add · Pointer to docs/design/ · no code
**Where:** after "Screens, flow, boosters and economy: read docs/screens.md when working on UI or flow."
```md
Product design intent (art names, the 100-level plan, feel, copy, unscheduled ideas) is in docs/design/. Don't read it by default, only when a task needs product intent (level curation, step 6 feel, copy). Where it disagrees with this file or docs/*.md, those win.
```
**Why:** the folder exists now. Without this line Claude Code either never finds it or treats it as a spec.

### A2 · Edit · Open decisions · no code
**Where:** Open decisions.
- "Difficulty curve shape and mechanic introduction levels (product)." becomes:
  ```md
  - Difficulty curve shape and mechanic introduction levels (product): proposal in docs/design/level-plan.md, with open issues.
  ```
- In the sprite-specs line, "colorblind patterns" becomes:
  ```md
  colorblind patterns (product: an optional symbol per colour in `ColorPalette`, off by default)
  ```

**Why:** product has partly answered both. Review rows M4, L28, S26.

## docs/mechanics.md

### A3 · Add · Clay looks · code with A7
**Where:** Clay, after "Any capacity can be clay. Usual design (not a rule): …".
```md
- Clay tubes have two looks: the default, and `skin: "bigBottle"` (docs/levels.md). Core ignores the skin; only the view reads it.
```
**Why:** product uses both looks, and the view must know which one to draw. Review row M3.

### A4 · Add · Mechanic views · code in step 4c
**Where:** a new section at the end of the file.
```md
## Mechanic views
Views only react to Core events: the state has already changed, and input is never blocked.
- Hidden ball: `BallRevealed` → the ball flips to its colour.
- Veil: drawn as a cover showing the veil colour. `TubeUnveiled` → the cover lifts off, and the balls show with their own hidden flags.
- Lock: drawn as one cover over its tubes, in the lock's colour; its key is a mark in the same colour on the key's ball. `KeyFound` → the mark leaves the ball (it may fly to the lock); `LockOpened` → the cover opens.
- Clay: drawn per `skin`. `TubeCompleted` on a clay tube plays its completion effect; an unfinished clay tube keeps its look.
Art and timings: docs/design/names-and-art.md and docs/design/feel.md.
```
**Why:** step 4c builds these views. This fixes which event drives each one, and keeps the art details in the design doc. Review rows M9, M15, M18, M24.

### A5 · Add · Shuffle limits on heavy boards · no code
**Where:** Shuffle → Limits, after "Re-measure in step 5 with generated levels."
```md
Start with the heaviest planned boards (veils + locks + a large clay tube, ~15 tubes): the product prototype's JS solver needed up to ~100 s per solvable deal there.
```
**Why:** 2,000 nodes may be far too few on those boards, and Shuffle would then mostly fail ("Can't shuffle right now") on late levels. Review row L18.

## docs/levels.md

### A6 · Add · `tier` field · needs code
**Where:** File format, after the "Top level: optional `locks` …" bullet.
```md
- Top level: optional `tier`: `"hard"` or `"superHard"` (absent = regular). It is stored rather than computed from the level number, so product can move one; the plan is in docs/design/level-plan.md. It sets the win reward and the tier tags (docs/screens.md).
```
**Code:** `LevelDto.Tier` (strict parsing needs it), validation (A9), an EditMode test; the generated tier levels carry it.
**Why:** the plan has 19 Hard and Super Hard levels with their own rewards and tags. Review row L2.

### A7 · Add · `skin` field · needs code
**Where:** File format, the "Per tube" bullet; add to its list of optional fields:
```md
`skin: "bigBottle"` (view-only: which clay look to draw; clay tubes only)
```
**Code:** `TubeDto.Skin`, validation (A9), a test; read by the clay view in step 4c.
**Why:** see A3. Review row L3.

### A8 · Add · `meta.solution` and `meta.seed` · needs code
**Where 1:** File format, the "Top level" bullet, after "`meta.mechanics` names are `hidden`, `veil`, `lock`, `clay`, `large`."
```md
`meta.solution` is a winning move list `[[from, to], …]` (tube indices; the moved count follows from the rules) and `meta.seed` is the generator seed. Tools write both.
```
**Where 2:** the "Current files are placeholders …" paragraph, after "`ShippedLevelTests` solves every file with the solver and plays the solution to a win".
```md
When a file has `meta.solution`, `ShippedLevelTests` replays it to a win with the real rules instead of solving, which keeps the suite fast as levels get bigger.
```
**Code:** `LevelMetaDto` fields; Validate Levels writes `solution` next to `optimalMoves`; a `ShippedLevelTests` change.
**Why:** product's levels come with solutions, and replaying is instant on the heavy boards where solving is slow. Review rows L4, L5, L13.

### A9 · Add · Validation of the new fields · code with A6–A7
**Where:** the validation bullets, after "No tube over capacity; every tube's top ball starts revealed."
```md
- `tier` is `"hard"` or `"superHard"`; `skin` is `"bigBottle"` and only on a clay tube.
```
**Why:** keeps the "all problems reported at once" contract for the new fields. Review row L7.

### A10 · Add · Generator rules · code in step 5
**Where:** Generator, after the "Difficulty signals" bullet.
```md
- **Rules** (generator only; stricter than the loader, so they are not validation). The generator:
  - never ships a deal the solver reports as `Unknown`; it counts as unsolvable;
  - never puts a capacity-4 clay tube in a level that has a large clay tube: a large-clay colour (8–12 balls) poured into it could never complete;
  - puts keys only on revealed balls outside veiled tubes, below the opening run of their tube (never in it);
  - fills veiled tubes completely, with colours that are neither a veil colour nor a large-clay colour, so every veil colour can be completed from the open tubes, and leaves at least 2 filled tubes uncovered;
  - never makes a level look like either of the two before it when they use the same mechanics;
  - refuses a recipe it can't meet (e.g. too few colours for the veils, or veils and locks leaving fewer than 2 open filled tubes) with a reason, instead of retrying forever.
```
**Code:** step 5. If the team takes product's export instead, these become checks on the imported levels.
**Why:** each rule prevents an unsolvable, unfair or repetitive level, or an endless generator loop. Review rows M7, M11, M17, M22, L17, L21.

### A11 · Add · Pointer to the level plan · no code
**Where:** Generator, after "Product teammate curates the final 100 and hand-authors tutorial levels for each mechanic."
```md
- Product's plan for the 100 levels (episodes, tiers, mechanic unlocks, target difficulty per level) and the prototype's generator recipe: docs/design/level-plan.md.
```
**Why:** the generator and curation work in step 5 needs to find the plan. Review row L30.

## docs/screens.md

### A12 · Edit · Tier rewards · code with A6
**Where:** Persistent player data, the Coins row. "+20 per win." becomes:
```md
+20 per win; +60 on a Hard level, +100 on a Super Hard level.
```
**Code:** `EconomyConfig` gets the two rewards; `LevelFlow` picks the reward by tier (today it always passes `WinReward`); the Win panel shows that amount.
**Why:** the plan's tier levels pay more. Review row S7.

### A13 · Add · Configs are read-only · no code
**Where:** after "All numbers live in an `EconomyConfig` ScriptableObject, not in code."
```md
Configs are read-only at runtime; player state lives only in the save.
```
**Why:** states a rule the code already follows, before more configs arrive. Review row S20.

### A14 · Add · Tier tags · code with A6
**Where 1:** Home screen, the "Center" bullet, at the end:
```md
On a Hard level the button also shows a "Hard Level" tag and the reward multiple ("x3" with the default rewards); on a Super Hard level it turns red and shows "Super Hard" and "x5".
```
**Where 2:** Level screen, the "Top-center" item:
```md
Under it, "HARD" or "SUPER HARD" on tier levels.
```
**Where 3:** Win, at the end of the bullet:
```md
On a tier level the panel also shows the tier and its reward.
```
**Code:** small UI scripts plus scene edits through Unity MCP (logged in docs/unity-editor-changes.md).
**Why:** players should know a level is special before they start it. Review row S7.

### A15 · Add · Tutorial toasts · needs code
**Where 1:** Level screen, after the "Settings popup (Level)" bullet.
```md
- **Tutorial toast:** on a level with a `TutorialConfig` entry (level → message), the first time it is played, a toast shows the message 0.6 s after the board appears, for 3.6 s. It never blocks input. Levels whose toast has been shown are saved (`SeenTutorials`). `meta.tutorial` in level files is design metadata and isn't read at runtime. Texts: docs/design/copy.md.
```
**Where 2:** Persistent player data, a new table row:
```md
| SeenTutorials | [] | Levels whose tutorial toast has been shown. |
```
**Code:** a `TutorialConfig` ScriptableObject; a `Toast.Show` overload with a duration (today the 1.6 s is fixed); a hook in `LevelFlow`; `SaveData.SeenTutorials` with a save version bump and migration.
**Why:** each new mechanic needs an introduction, and the copy exists. One runtime source avoids two competing flags. Review rows M25, S15.

### A16 · Add · Ball flight · no code until step 6
**Where:** Level screen, after the "Input" bullet.
```md
- In step 6, the balls of one move fly one by one with a short stagger (`AnimationConfig`); it is still one move and one undo step.
```
**Why:** settles that the stagger is view-only, so nobody turns it into several moves. Review row S16.

### A17 · Add · Feedback section · code in step 6
**Where:** a new section before "## Shared UI pieces".
```md
## Feedback (step 6)
- `LevelFeedback` maps each Core event to animation, VFX, SFX and haptics, with one config row per event (duration, ease, offsets, VFX prefab, clips, pitch variance, haptic type). No numbers in code.
- Cosmetic animation never blocks input, the level intro included; a new tap finishes or kills the running tween first.
- Each settings toggle mutes its channel.
- Values, the chosen look and the sound map: docs/design/feel.md.
```
**Why:** one place for feel, all values tunable, and the input rule stated where step 6 work will read it. Review rows S22, S24.

### A18 · Add · Editor debug tools · small code
**Where:** Scenes & flow, after the "Editor-only override for testing …" bullet.
```md
- Editor-only debug, with the step 4c level jump: force a win, and show the solver's next move as a hint (solved off the main thread, like Shuffle).
```
**Code:** Editor-only (`#if UNITY_EDITOR` or the Editor assembly); never in player builds.
**Why:** faster testing of the win flow and of late, hard levels. Review row S29.

### A19 · Edit · Bottom nav order · scene edit
**Where:** Home screen, the "Bottom nav" bullet.
```md
- **Bottom nav:** Shop · Home (selected) · Leaderboard.
```
**Code:** reorder the buttons in the Home scene through Unity MCP and log it in docs/unity-editor-changes.md.
**Why:** product's layout call, at no cost. Review row S13.
