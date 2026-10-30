# Screens, Flow & Economy

Read this when working on UI, scene flow, lives, tutorials, boosters, or player data. All UI is placeholder until final art (step 6): Unity default UI sprites, TextMeshPro, flat colors. Animations are step 6 too — step 3 snaps instantly, but every ball movement goes through one view method so tweens can replace the snap in one place.

## Scenes & flow
- `Home` (build index 0) → tap Level button → `Level` (build index 1). At 0 lives the Level button opens the More Lives popup instead (Lives, below).
- `Level` → win → celebration → Win panel → **Continue** → `Home`, with the Episode Unlocked overlay if the win moved the player into a new episode (Episodes, below).
- `Level` → stuck → **Give up** → Are You Sure → **Confirm** → −1 life → `Home` (no progress, no reward).
- `Level` → settings → **Home** → Are You Sure → **Confirm** → −1 life → `Home` (same as giving up).
- `Level` → settings → **Restart** → Are You Sure → **Confirm** → −1 life → the level reloads in its next variant. At 0 lives, Restart opens the More Lives popup instead.
- `Level` → the level file fails to load → `Home` with the toast "Level N could not be loaded" (the error is logged). It costs no life and counts no retry: the player did not leave.
- The Level scene always loads `PlayerData.CurrentLevel`, in the variant its retry count picks (docs/levels.md, Variants and retries). Editor-only level jump for testing: **Ball Sort ▸ Play Level…** (or Play on a Validate Levels row) opens the Level scene on any level, or on a level file outside the run (test and draft levels in `Assets/Data/TestLevels/`), in a chosen variant, for one visit; the next visit uses the save again. Such a visit is a test: every attempt (Restart included) plays the chosen variant, the title names it (e.g. "Level 6 · V5", or the file name for a level file), winning pays no reward (the Win panel shows none) and moves no progress, and lives are neither checked nor spent (no retry counted, no level-in-progress mark). Its popups still show so they can be tested: Are You Sure before leaving, and the tutorial on every visit, never saved as seen.
- Pressing Play in either scene in the Editor must work (persistent services auto-create).

## Persistent player data (one save, survives scenes and restarts)
| Field | Start value | Notes |
|---|---|---|
| CurrentLevel | 1 | Advances when the current level is won. After level 100 is won: stays 100, `AllLevelsComplete = true`. Replays (level 100 after the run) pay the reward but never move progress; editor level jumps record nothing. |
| Coins | 1000 | +20 / +40 / +60 per win of a regular / hard / super hard level (the level's `tier`). Play on costs 900. |
| Lives | 5 (full) | 0 to the maximum (5). Spent and refilled as described in Lives, below. |
| Life timer | none | UTC time the current wait for a life began; kept only while lives are below the maximum. |
| Boosters: Undo / Shuffle / AddTube | 99 / 99 / 99 | Global totals, spent in any level, no refills. |
| Retry count / last variant | 0 / none | Attempts at the current level that cost a life since it was last won, and the variant the latest attempt played (docs/levels.md, Variants and retries). Reset by a win. |
| Level in progress | none | The level being played: set when it starts, cleared by a win or a confirmed Home / Give up. Still set at launch → the app was closed mid-level (Lives, below). |
| Player name | `Player` | Set in Edit Profile; 1–16 characters, trimmed. |
| Tutorials seen | none | Ids of the levels whose tutorial popup has been dismissed. |
| Settings: Sound / Music / Haptics | on / on / on | Saved; no effect until audio/haptics exist. |

All numbers live in an `EconomyConfig` ScriptableObject, not in code, including the win reward of each tier, the lives maximum and the refill interval. Progress and rewards are saved **at the moment of the win**, before any celebration or panel.

**Save format:** one versioned JSON blob (`SaveData`) in PlayerPrefs under `BallSort.Save`, written on every change and on pause/quit. Every field is required: a corrupt save, a save with missing fields, or one from a newer version is replaced by defaults with a warning. Out-of-range values are clamped on load. Bump `SaveData.CurrentVersion` and add a migration to `SaveJsonCodec` when the shape changes (version 2 added the try fields; a version 1 save migrates with try 0 and no last variant. Version 3 adds the life timer, the level-in-progress mark, the player name and the tutorials seen, and turns the try count into the retry count: a version 2 save keeps its count as retries, starts the life timer at load if lives are below the maximum, and gets no mark, the name `Player` and no tutorials seen). Editor tools: **Ball Sort ▸ Reset Save** (disabled in Play mode) and **Ball Sort ▸ Player Data…** (set coins, lives, the time to the next life, booster counts and the player name, jump to any level (retries cleared), and clear the tutorials seen; it also shows the level, retries, last variant, level in progress and tutorials seen; edits the save in Edit mode, the live player in Play mode).

## Home screen
- **Top bar:** avatar (top-left: the current episode's mascot frame), lives (the count on the art's heart, the time to
  the next life on its pill) and coins (the count on the pill next to the art's coin) between the corners, settings
  button (top-right).
- **Center:** big **Level button** (green) showing `Level N`. If `AllLevelsComplete`: shows `All levels complete` and
  replays level 100. Its look depends on the level's tier: regular, hard or super hard (for now only the "Hard" or
  "Super hard" badge; tier button art is missing, docs/art-inventory.md). At 0 lives it opens the More Lives popup
  instead of loading the level.
- **Bottom nav:** Shop · Home · Leaderboard, the order of the bar art. One bar image in three versions, each with its
  tab raised; selecting a tab swaps the bar to its version. Three invisible tap areas match the drawn sections
  (41 / 19 / 41 % of the bar's width). Home is selected on arriving. Every tab stays interactable (`HomeTabs` enforces it).
  Leaderboard and Shop swap the center for their page; Home brings the Level button back. The top bar stays on every tab.
  - **Shop page:** a vertical scroll view: the shop header ("Shop"), the shop panel (six empty item frames until item
    art arrives) and three copies of the bundle card with a name and a price. All static: nothing in it does anything.
  - **Leaderboard page:** placeholder (a title and five empty rows).
- **Avatar** → **Edit Profile** popup: the player name in an input field, prefilled with the saved name. **Save** stores
  it (trimmed, 1–16 characters; disabled while empty) and closes; **Close** (✕) closes without saving.
- Settings popup (Home): Sound, Music, Haptics toggles.

## Level screen
- **Top-left:** settings icon. **Top-center:** `Level N`. **Top-right:** the episode's mascot frame (display only, for now). No avatar, coins, or lives here.
- **Middle:** the board (tubes laid out on the level's grid).
- **Bottom:** booster bar — Undo, Shuffle, Add tube — each with its remaining count.
- **Input:** tap a tube to select it (its run lifts above the glass), tap it again to deselect, tap another tube to
  move. An illegal move, or a tap on a tube that can't be selected, plays invalid-move feedback and clears the
  selection. Tapping empty space deselects. The selection is also dropped on undo, add tube, Play on, restart,
  entering/leaving stuck and the win.
- Settings popup (Level): Sound, Music, Haptics toggles + **Restart** + **Home**, both behind Are You Sure (Lives, below). Gameplay input is blocked while it is open.

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
- **Restart** (settings, behind Are You Sure; it costs a life) reloads the level from data in its next variant: hidden balls hidden again, added tubes gone, undo history cleared. Boosters already spent stay spent.

### Stuck (not won, and nothing useful left to do)
- Checked after every move, undo, added tube, Play on, shuffle and restart.
- Stuck = nothing meaningful left to do: no legal move, or only moves that go round in a loop (rule in docs/mechanics.md, Win and stuck).
- Show **"No more useful moves"** above the board (`StuckPanel._bannerText`, the one place to change it; does not cover or block the tubes).
- Booster bar hidden (boosters cannot be used while stuck); tubes can still be selected.
- Two buttons side by side in the booster bar's place: **Give up** (behind Are You Sure; it costs a life) and **Play on — 900 ●**.
  - Play on: spend 900 coins, add one empty capacity-4 tube, leave the stuck state. It continues the same attempt and costs no life.
  - Play on is disabled (still showing its price) if coins < 900 or every row already has 8 cells. Give up is then the only choice.

### Win
- Input locks, progress + reward are saved and the level-in-progress mark is cleared (one save), short placeholder celebration (~1 s, e.g. "Well done!" text), then the **Win panel**: "Level N complete", `+20 ●` (the tier's reward), **Continue** → Home. A win never costs a life.

## Lives
- **Maximum 5**; below it, **one life comes back every 30 minutes** until full (both numbers in `EconomyConfig`). Lives never go below 0.
- The refill is computed from the saved life timer, never counted while the app runs, so it carries on while the app is
  closed: lives gained = whole intervals since the timer started (up to the maximum), and the timer moves forward by
  that many intervals. Reaching the maximum stops the timer; spending a life at the maximum starts it at that moment.
  Time to the next life = timer + interval − now. If now is earlier than the timer (the device clock was turned back),
  the timer restarts from now: the wait starts over and no life is gained.
- **Every exit from a level before a win costs a life**, whatever moves were made: settings → Home, settings → Restart,
  stuck → Give up, and closing the app mid-level (below). A win never costs one, and neither does Play on. Each such
  exit also counts one retry (docs/levels.md, Variants and retries); at 0 lives it still counts the retry, and lives
  stay at 0.
- **Are You Sure popup:** settings → Home, settings → Restart and stuck → Give up each open it first: "Are you sure?" and
  "You will lose a life." (that line is left out at 0 lives). **Confirm** → lives −1, then the action. **Close** (✕) →
  back to where it came from: the settings popup (still open) or the stuck bar. Nothing is spent before Confirm.
- **Closing the app mid-level:** when a level starts, the save gets a level-in-progress mark naming the level. A win or a
  confirmed Home / Give up clears it in the same save that records them; a confirmed Restart keeps it for the new
  attempt. On the next launch, before any screen shows, a mark still set means the attempt ended with neither a win nor
  a normal exit (the player closed the app, or the OS killed it in the background): lives −1 (never below 0), +1 retry
  for that level, mark cleared. Pausing and resuming the app costs nothing.
- **0 lives:**
  - Home: the Level button opens the More Lives popup instead of loading the level.
  - Level (lives reach 0 there when Restart spends the last one): **Restart** opens the More Lives popup instead of
    restarting, with no Are You Sure first; its Close goes back to the settings popup. **Home** and **Give up** still
    work, through Are You Sure as usual, and lives stay at 0. Once a life comes back, Restart works again.
- **More Lives popup:** a heart, the lives count and "Next life in 12:34", both refreshed every second, and **Close** (✕)
  → back to Home or the settings popup. There is no way to get lives faster yet.
- **Home display:** `♥ N` and the time to the next life as `mm:ss`, or `Full` at the maximum, refreshed every second.

## Episodes
- The run is split into episodes, each with its own Home background, Level background and mascot frame (the scientist
  in a frame). An `EpisodeConfig` ScriptableObject lists each episode's first level and those three sprites; currently
  5 episodes × 20 levels (first levels 1, 21, 41, 61, 81). Placeholder art until step 6.
- The current episode is derived from the current level (the last episode whose first level is not after it), never
  stored. Home shows the episode of `PlayerData.CurrentLevel`; the Level scene shows the episode of the level being
  played: its background, and its mascot frame at the top right (for now).
- **Episode Unlocked:** Continue on the Win panel of a win that moved `CurrentLevel` into a new episode (to level 21,
  41, 61 or 81) brings up Home on the previous episode's background. About 0.7 s later (`HomeFlow._episodeUnlockedDelay`) the
  Episode Unlocked overlay appears ("Episode N unlocked" and the new episode's mascot frame) and the Home background
  switches to the new episode behind it. A tap anywhere closes the overlay; Home input is blocked from arriving until
  then. The Level scene shows the new episode from then on, as it always shows the played level's episode. The
  hand-off from the Win panel to Home is kept in memory (`AppRoot`), not saved: if the app closes in between, Home
  simply opens on the new episode. Replays and editor test visits never move progress, so they never show it.

## Tutorials
- A level whose file sets `tutorial: true` (docs/levels.md) shows a tutorial popup the first time the player enters it,
  over the loaded board: a short text and an optional image, from the level's entry in the `TutorialConfig`
  ScriptableObject (entries keyed by level id). A tap anywhere dismisses it; until then everything else (tubes,
  boosters, settings) is blocked.
- "Seen" is saved per level id when the popup is dismissed, so it never shows again for that level (Restart, later
  attempts, replays). Closing the app before dismissing it shows it again on the next entry.
- `TutorialConfig` has an entry for each tutorial level (1, 7, 20, 30, 55, 80) with placeholder text on the mechanic it
  introduces; product writes the final text in the asset, with no code change. A tutorial level without an entry shows
  no popup and logs a warning; `TutorialConfig.OnValidate` warns about duplicate level ids and empty texts.
- Editor test visits show the popup on every visit and never save it as seen.

## Shared UI pieces
- **Toast:** short message, auto-hides, never blocks input. A new message replaces the current one instead of queueing.
- **Popup:** modal with dimmed background; blocks input beneath it. Open = GameObject active. Draw order is hierarchy order: screen content, then popups, then the toast last. Popups can stack: Are You Sure and More Lives open over the settings popup, and only the topmost takes input. The tutorial popup and the Episode Unlocked overlay close on a tap anywhere; the others have buttons.
- Buttons (art, 9-sliced so they stretch to any width): green for Continue, the Level button and Save (Edit Profile);
  red for Give up and Are You Sure's Confirm; yellow for Play on and the shop's prices; the round ✕ closes every popup;
  the round gear is both settings buttons. Settings' Restart and Home and the booster buttons keep placeholders.
- Canvas Scaler 1080×1920, match 0.5; `SafeArea` on root panels.
- **Art slots:** per-episode art (Home and Level backgrounds, mascot frame) is in `EpisodeConfig`, the tutorial image in
  `TutorialConfig`, the bottom bar's three versions on `HomeTabs`, and everything else is an `Image` on its screen or
  popup object. Which slots hold final art and which still hold placeholders (Unity's built-in UI sprites, tinted, and
  the generated heart in `Art/Sprites/Placeholder/UI/`) is listed in docs/art-inventory.md. Home backgrounds exist for
  episodes 2 and 5; episodes 1, 3 and 4 use episode 2's until theirs arrive.
