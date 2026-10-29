# Screens, Flow & Economy

Read this when working on UI, scene flow, boosters, or player data. All UI is placeholder until final art (step 6): Unity default UI sprites, TextMeshPro, flat colors. Animations are step 6 too — step 3 snaps instantly, but every ball movement goes through one view method so tweens can replace the snap in one place.

## Scenes & flow
- `Home` (build index 0) → tap Level button → `Level` (build index 1).
- `Level` → win → celebration → Win panel → **Continue** → `Home`.
- `Level` → stuck → **Give up** → `Home` (no progress, no reward).
- `Level` → settings → **Home** → `Home` (same as giving up).
- `Level` → the level file fails to load → `Home` with the toast "Level N could not be loaded" (the error is logged).
- The Level scene always loads `PlayerData.CurrentLevel`, in the variant its new try picks (docs/levels.md, Variants and tries). Editor-only level jump for testing: **Ball Sort ▸ Play Level…** (or Play on a Validate Levels row) opens the Level scene on any level, or on a level file outside the run (test and draft levels in `Assets/Data/TestLevels/`), in a chosen variant, for one visit; the next visit uses the save again. Such a visit is a test: every try (Restart included) plays the chosen variant, the title names it (e.g. "Level 6 · V5", or the file name for a level file), and winning pays no reward (the Win panel shows none), moves no progress and counts no try.
- Pressing Play in either scene in the Editor must work (persistent services auto-create).

## Persistent player data (one save, survives scenes and restarts)
| Field | Start value | Notes |
|---|---|---|
| CurrentLevel | 1 | Advances when the current level is won. After level 100 is won: stays 100, `AllLevelsComplete = true`. Replays (level 100 after the run) pay the reward but never move progress; editor level jumps record nothing. |
| Coins | 1000 | +20 / +40 / +60 per win of a regular / hard / super hard level (the level's `tier`). Play on costs 900. |
| Lives | 5 | Display only for now. |
| Boosters: Undo / Shuffle / AddTube | 99 / 99 / 99 | Global totals, spent in any level, no refills. |
| Level try / last variant | 0 / none | Tries started at the current level and the variant the latest one played (docs/levels.md, Variants and tries). Reset by a win. |
| Settings: Sound / Music / Haptics | on / on / on | Saved; no effect until audio/haptics exist. |

All numbers live in an `EconomyConfig` ScriptableObject, not in code, including the win reward of each tier. Progress and rewards are saved **at the moment of the win**, before any celebration or panel.

**Save format:** one versioned JSON blob (`SaveData`) in PlayerPrefs under `BallSort.Save`, written on every change and on pause/quit. Every field is required: a corrupt save, a save with missing fields, or one from a newer version is replaced by defaults with a warning. Out-of-range values are clamped on load. Bump `SaveService.CurrentVersion` and add a migration when the shape changes (version 2 added the try fields; a version 1 save migrates with try 0 and no last variant). Editor tools: **Ball Sort ▸ Reset Save** (disabled in Play mode) and **Ball Sort ▸ Player Data…** (set coins, lives and booster counts; edits the save in Edit mode, the live player in Play mode).

## Home screen
- **Top bar:** avatar (top-left), lives `♥ 5` and coins `● 1000` between the corners, settings icon (top-right).
- **Center:** big **Level button** showing `Level N`. If `AllLevelsComplete`: shows `All levels complete` and replays level 100.
  Its look depends on the level's tier: regular, hard or super hard (placeholder: a tint per tier, plus a "Hard" or
  "Super hard" label).
- **Bottom nav:** Leaderboard · Home (selected) · Shop.
- Avatar, Leaderboard, Shop: placeholders → "Coming soon" toast.
- Settings popup (Home): Sound, Music, Haptics toggles.

## Level screen
- **Top-left:** settings icon. **Top-center:** `Level N`. No avatar, coins, or lives here.
- **Middle:** the board (tubes laid out on the level's grid).
- **Bottom:** booster bar — Undo, Shuffle, Add tube — each with its remaining count.
- **Input:** tap a tube to select it (its run lifts above the glass), tap it again to deselect, tap another tube to
  move. An illegal move, or a tap on a tube that can't be selected, plays invalid-move feedback and clears the
  selection. Tapping empty space deselects. The selection is also dropped on undo, add tube, Play on, restart,
  entering/leaving stuck and the win.
- Settings popup (Level): Sound, Music, Haptics toggles + **Restart** + **Home**. Gameplay input is blocked while it is open.

### Boosters
- Unavailable boosters look dimmed but still react to taps with a short toast explaining why:
  - Undo: "Nothing to undo" / "Can't undo past a completed tube" / "No undos left".
  - Add tube: "No add-tube boosters left" / "No room for more tubes".
  - Shuffle: "No shuffles left" / "Can't shuffle right now" (no valid shuffle found; the booster is not spent).
- Reason precedence (first match wins), so the toast names the real obstacle rather than the booster count:
  - Undo: Stuck → no moves → sealed → no boosters.
  - Add tube: level won → Stuck → no room → no boosters.
  - Shuffle: level won → Stuck → no boosters → no valid shuffle (known only after trying, so the button stays enabled until then).
- **Add tube:** appends an empty tube of capacity 4. Not undoable, doesn't seal undo history. No room once every row already has 8 cells ("No room for more tubes").
- **Shuffle:** rearranges the balls of the open tubes, keeping the board solvable (rules in docs/mechanics.md). Spent only when it succeeds; clears the undo history, so Undo then shows "Nothing to undo". Not undoable. The search runs off the main thread; until it is applied or refused, taps, boosters and settings are locked (flow state `Shuffling`). If it takes longer than 0.3 s (`LevelFlow._shufflingIndicatorDelay`), a "Shuffling…" label appears where the stuck banner would be, and goes when the search ends. The board is rebuilt in place (no animation until step 6).
- **Restart** (settings) reloads the level from data: hidden balls hidden again, added tubes gone, undo history cleared. Boosters already spent stay spent.

### Stuck (no legal moves and not won)
- Checked after every move, undo, added tube and shuffle.
- Show **"No available moves"** text above the board (does not cover or block the tubes).
- Booster bar hidden (boosters cannot be used while stuck); tubes can still be selected.
- Two buttons side by side in the booster bar's place: **Give up** and **Play on — 900 ●**.
  - Play on: spend 900 coins, add one empty capacity-4 tube, leave the stuck state.
  - Play on is disabled (still showing its price) if coins < 900 or every row already has 8 cells. Give up is then the only choice.

### Win
- Input locks, progress + reward are saved, short placeholder celebration (~1 s, e.g. "Well done!" text), then the **Win panel**: "Level N complete", `+20 ●` (the tier's reward), **Continue** → Home.

## Episodes
- The run is split into episodes, each with its own Home and Level background. An `EpisodeConfig` ScriptableObject
  lists each episode's first level and its two background sprites; currently 5 episodes × 20 levels (first levels 1,
  21, 41, 61, 81). Placeholder art until step 6.
- The current episode is derived from the current level (the last episode whose first level is not after it), never
  stored. Home shows the episode of `PlayerData.CurrentLevel`; the Level scene shows the episode of the level being played.

## Shared UI pieces
- **Toast:** short message, auto-hides, never blocks input. A new message replaces the current one instead of queueing.
- **Popup:** modal with dimmed background; blocks input beneath it. Open = GameObject active. Draw order is hierarchy order: screen content, then popups, then the toast last.
- Placeholder settings icon is "≡": LiberationSans has no gear glyph.
- Canvas Scaler 1080×1920, match 0.5; `SafeArea` on root panels.