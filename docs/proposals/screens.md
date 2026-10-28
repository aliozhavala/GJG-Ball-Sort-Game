# Screens, Flow & Economy

> **[+P] = product addition** on top of the developer's version (commit `4f77fb8`, 28 Oct 2026). Text without the mark is the developer's, unchanged. **[+P decision]** marks the few places where product asks for something different from the developer's line just above it (top bar, bottom nav order, music toggle, buying boosters); everything else only adds. Product additions fill in what this file leaves as placeholders or "for now": hearts, tiers, the win sequence, the screens behind "Coming soon", and the feedback pass. Art for every screen: `docs/art/shots/` and `docs/art/ART_ASSET_BRIEF.md` in the product repo; the prototype `prototype/potion-sort-lab.html` shows each flow working.

Read this when working on UI, scene flow, boosters, or player data. All UI is placeholder until final art (step 6): Unity default UI sprites, TextMeshPro, flat colors. Animations are step 6 too — step 3 snaps instantly, but every ball movement goes through one view method so tweens can replace the snap in one place.

## Scenes & flow
- `Home` (build index 0) → tap Level button → `Level` (build index 1).
- `Level` → win → celebration → Win panel → **Continue** → `Home`.
- `Level` → stuck → **Give up** → `Home` (no progress, no reward).
- `Level` → settings → **Home** → `Home` (same as giving up).
- `Level` → the level file fails to load → `Home` with the toast "Level N could not be loaded" (the error is logged).
- The Level scene always loads `PlayerData.CurrentLevel`. Editor-only override for testing (temporary Inspector field until the step 4 level jump).
- Pressing Play in either scene in the Editor must work (persistent services auto-create).
- **[+P] Flows that hearts and variants add:**
  - `Home` → Level button with **0 lives** → **More Lives** popup instead of loading the level.
  - **Leaving costs a life** once the player has made at least one move in this attempt. That covers settings → **Home**, settings → **Restart** and stuck → **Give up**. Each goes through the **Are You Sure** popup first: confirm → lives −1 → go on; close → back where it came from (the settings popup or the stuck bar). With 0 moves made, Home and Restart are free and skip the popup.
  - Every attempt that costs a life also does `retryCount[level] + 1`, so the next load picks the next variant (`docs/levels.md` → Variants). Restart reloads into that next variant; a win resets the count.
  - `Level` → win → Continue → `Home`. If the next level opens a new episode (20, 40, 60, 80): **Episode Unlocked** overlay on Home ~0.7 s after arriving, and Home switches to the new episode's background.
  - `Home` → avatar → **Edit Profile** popup · Shop tab → **Shop** page · Leaderboard tab → **Leaderboard** page. These replace the "Coming soon" placeholders.

## Persistent player data (one save, survives scenes and restarts)
| Field | Start value | Notes |
|---|---|---|
| CurrentLevel | 1 | Advances when the current level is won. After level 100 is won: stays 100, `AllLevelsComplete = true`. Replays (level 100 after the run, editor override) pay the reward but never move progress. |
| Coins | 1000 | +20 per win. Play on costs 900. |
| Lives | 5 | Display only for now. |
| Boosters: Undo / Shuffle / AddTube | 99 / 99 / 99 | Global totals, spent in any level, no refills. |
| Settings: Sound / Music / Haptics | on / on / on | Saved; no effect until audio/haptics exist. |
| **[+P]** Coins on a tier win | — | Hard win **+60**, Super Hard win **+100** (regular stays +20). |
| **[+P]** NextLifeAt (UTC) | none | Set when lives drop below max: +1 life every **30 min of real time**, also while the app is closed. Cleared at max. |
| **[+P]** InfiniteLivesUntil (UTC) | none | From shop bundles. While in the future, no life is spent and the lives pill shows ∞ + time left. |
| **[+P]** NoAds | false | From shop bundles (mock; no ads ship). |
| **[+P]** RetryCount[level] | 0 | +1 on every attempt that costs a life, 0 on a win. Picks the variant. |
| **[+P]** BestGrade[level] | 0 | 1 Good Job, 2 Awesome, 3 Perfect (Win screen). |
| **[+P]** Profile | name "Player", avatar 0, frame "Neon" | `{ name, avatarId, frameId, unlockedFrames[] }`. |
| **[+P]** WeeklyCup | — | `{ weekId, score, lastPrizeWeekId }`: score = levels won this week. |
| **[+P]** SeenTutorials | [] | Levels whose tutorial toast has been shown. |
| **[+P decision]** Settings | Sound / Haptics on | **No Music setting**: the game has no music. |

All numbers live in an `EconomyConfig` ScriptableObject, not in code. Progress and rewards are saved **at the moment of the win**, before any celebration or panel.

- **[+P] More config fields** (same rule: no numbers in code):
  - `EconomyConfig`: `hardWinReward` 60, `superHardWinReward` 100, `coinCountUpSeconds` 0.8.
  - `BoosterConfig`, one row per booster (Undo, Shuffle, AddTube): `startingCount` (the 99 above stays the default; product doesn't mind the starting number), `buyPriceCoins`, `buyAmount`. Defaults: Undo 300 / Shuffle 400 / AddTube 900 coins for +1. Product wants to tune the whole purchase economy here, so no price or amount in code.
  - `LivesConfig`: `maxLives` 5, `regenMinutes` 30, `refillCost` 900 coins, `adLivesReward` 1, `lifeCostOnLeave` 1, `freeLeaveIfNoMoves` true.
  - `WinGradingConfig`: see Win below.
  - `EpisodeConfig`: list of `{ id, name, levelFrom, levelTo, homeBackground, levelBackground, cardArt }`. Defaults: Classic Lab 1–19 · Lava Lab 20–39 · Beach Lab 40–59 · Arctic Lab 60–79 · Space Lab 80–100.
  - `LeaderboardConfig`: `unlockLevel` 40, `cupGroupSize` 6, `cupPrizes` [1000, 500, 250], `weekResetDay` Sunday 00:00 local.
  - `ProfileCatalog`, `ShopCatalog`, `AdConfig` (`mockAdSeconds` 3), `TutorialConfig` (level → message): sections below.
  - Configs are read-only at runtime; player state lives only in the save.

**Save format:** one versioned JSON blob (`SaveData`) in PlayerPrefs under `BallSort.Save`, written on every change and on pause/quit. Every field is required: a corrupt save, a save with missing fields, or one from a newer version is replaced by defaults with a warning. Out-of-range values are clamped on load. Bump `SaveService.CurrentVersion` and add a migration when the shape changes. Editor tools: **Ball Sort ▸ Reset Save** (disabled in Play mode) and **Ball Sort ▸ Player Data…** (set coins, lives and booster counts; edits the save in Edit mode, the live player in Play mode).

## Home screen
- **Top bar:** avatar (top-left), lives `♥ 5` and coins `● 1000` between the corners, settings icon (top-right).
- **Center:** big **Level button** showing `Level N`. If `AllLevelsComplete`: shows `All levels complete` and replays level 100.
- **Bottom nav:** Leaderboard · Home (selected) · Shop.
- **[+P decision]** Bottom nav order: **Shop (left) · Home (centre) · Leaderboard (right)**, as in Magic Sort.
- Avatar, Leaderboard, Shop: placeholders → "Coming soon" toast.
- Settings popup (Home): Sound, Music, Haptics toggles.
- **[+P decision]** No **Music** toggle on either settings popup: the game has no music. Sound and Haptics stay.
- **[+P] What product wants on Home** (`01_home.jpg`, `02_…hard…`, `03_…super_hard…`):
  - **Avatar** shows the player's avatar in their frame; tap → **Edit Profile**.
  - **Lives pill:** count inside a heart, plus "Full" or a countdown `mm:ss` to the next life, and a small green "+". Tap → **More Lives** popup.
  - **Coins pill:** amount and a small green "+". Tap → **Shop**, scrolled to the Coins section.
  - **Level button tier tag:** if the next level is Hard, a "Hard Level" chip and a red "x3" tag. If Super Hard, the button turns red with "Super Hard" and "x5".
  - **Centre scene:** the current episode's background, with creatures bobbing over a cauldron (idle loop and blink).
  - **Leaderboard tab** shows a padlock until `unlockLevel`.

## Level screen
- **Top-left:** settings icon. **Top-center:** `Level N`. No avatar, coins, or lives here.
- **Middle:** the board (tubes laid out on the level's grid).
- **Bottom:** booster bar — Undo, Shuffle, Add tube — each with its remaining count.
- **Input:** tap a tube to select it (its run lifts above the glass), tap it again to deselect, tap another tube to
  move. An illegal move, or a tap on a tube that can't be selected, plays invalid-move feedback and clears the
  selection. Tapping empty space deselects. The selection is also dropped on undo, add tube, Play on, restart,
  entering/leaving stuck and the win.
- Settings popup (Level): Sound, Music, Haptics toggles + **Restart** + **Home**. Gameplay input is blocked while it is open.
- **[+P decision] Top bar in product style** (`21_gameplay_level1.jpg`), instead of the top line above:
  - **Top-left:** coins pill with a small green "+"; tap → **Coins popup**, so the player can buy coins without leaving the level.
  - **Top-center:** level plate showing the player's **profile picture** in its frame above `Level N`.
  - **Top-right:** settings button.
  - No lives in the level.
- **[+P] Additions:**
  - **Tier tag** under `Level N`: "HARD" or "SUPER HARD" on tier levels.
  - **Tutorial toast** on a level that has a `TutorialConfig` entry (L1, 7, 20, 30, 55, 80; texts in `docs/mechanics.md`). It appears 0.6 s after the board, stays 3.6 s, never blocks input, and shows only the first time (`SeenTutorials`).
  - **Move = whole run:** a run lifts and flies ball by ball with an 80 ms stagger (`chainDelayMs`), and it counts as one move and one undo step. This is a view detail on top of the existing `min(runLength, freeSpace)` rule.
  - **Restart** and **Home** from settings go through **Are You Sure** once a move has been made (Scenes & flow above).

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
- **Shuffle:** rearranges the balls of the open tubes, keeping the board solvable (rules in docs/mechanics.md). Spent only when it succeeds; clears the undo history, so Undo then shows "Nothing to undo". Not undoable.
- **Restart** (settings) reloads the level from data: hidden balls hidden again, added tubes gone, undo history cleared. Boosters already spent stay spent.
- **[+P]** Any booster used in a level sets `usedHelp` for that attempt (it blocks the PERFECT grade on the Win screen).
- **[+P] Buying boosters with coins.** When the reason is "no boosters left" (`No undos left` / `No add-tube boosters left` / `No shuffles left`), the tap opens the **Booster Buy** popup instead of only the toast. The other reasons keep their toasts and precedence. At 0 the button shows a green "+" badge instead of the count. Prices and amounts come from `BoosterConfig`.

### Stuck (no legal moves and not won)
- Checked after every move, undo, added tube and shuffle.
- Show **"No available moves"** text above the board (does not cover or block the tubes).
- Booster bar hidden (boosters cannot be used while stuck); tubes can still be selected.
- Two buttons side by side in the booster bar's place: **Give up** and **Play on — 900 ●**.
  - Play on: spend 900 coins, add one empty capacity-4 tube, leave the stuck state.
  - Play on is disabled (still showing its price) if coins < 900 or every row already has 8 cells. Give up is then the only choice.
- **[+P]** **Give up** costs a life (a stuck board always has at least one move behind it), so it goes through **Are You Sure** ("You will lose 1 life!", button **Give up**); closing it returns to the stuck bar.

### Win
- Input locks, progress + reward are saved, short placeholder celebration (~1 s, e.g. "Well done!" text), then the **Win panel**: "Level N complete", `+20 ●`, **Continue** → Home.
- **[+P] Product win sequence** (step 6; `41_win_logo.jpg`, `42_…`, `43_…super_hard`). Every step is tap-to-skip; pre-load Home during it.
  1. ~1.0 s after the last cork: creatures jump in a wave.
  2. **Logo** "LAB SORT" pops in with fireworks, holds `winLogoSeconds` 2.6, flies out.
  3. **Win panel:**
     - Title tab "Level N", plus "HARD" / "SUPER HARD" (the card is red on Super Hard).
     - A **graded title**, letters popping in one by one: **PERFECT!** if moves ≤ `ceil(optimal × 1.25) + 1` **and no booster was used**; **AWESOME!** if moves ≤ `ceil(optimal × 1.8) + 2`; else **GOOD JOB!**. `optimal` = `meta.optimalMoves`; with no value, show GOOD JOB!. The factors, slacks and titles live in `WinGradingConfig`.
     - "Rewards:", then a coin pile on a pedestal with rays and the amount (+20 / +60 / +100).
     - **Continue**, and a close X that does the same.
  4. Continue → Home: coins fly into the coins pill and the counter counts up (`coinCountUpSeconds`); the Level button already shows the next level.
  5. **Episode Unlocked** if the next level opens an episode (Scenes & flow).
  - On the win, also: `RetryCount[level] = 0`, `BestGrade[level] = max(...)`, `WeeklyCup.score + 1`.

## [+P] New screens
All popups use one **popup kit** prefab (card, title tab, close X, inner panel, button row; colour variants purple / red), which the artist is drawing (art brief §B9). Same rules as the existing Popup: modal, dimmed background, blocks input beneath.

### More Lives popup (`13_more_lives.jpg`, `15_more_lives_full.jpg`)
- Coins pill top-left. Big heart with the count; "Time to next life" + `mm:ss`, or "Your lives are full!".
- **Refill** (green, coin icon + 900):
  - Coins ≥ cost → lives = max.
  - Otherwise → toast "Not enough coins" and open the Shop at Coins.
- **+1 Life** (magenta, ad icon) → mock rewarded ad → lives +1.
- Both are disabled when lives are full. While `InfiniteLivesUntil` is active, show ∞.

### Booster Buy popup (`35_booster_buy_*.jpg`)
- Opens from a booster at 0.
- Title = the booster's name, a big booster tile, "Out of boosters!", a one-line description, and a green **Buy [coin] price** button.
  - Enough coins → coins −price, booster +`buyAmount`, toast "Undo +1" (etc.), and the popup closes.
  - Not enough → toast "Not enough coins" and the **Coins popup** opens.
- Close X: back to the level. Gameplay input is blocked while it is open, like the settings popup.

### Coins popup (`36_coin_popup.jpg`), from the coins "+" in the level
- The same 6 coin packs as the Shop's Coins section, in a popup, so the player doesn't leave the level.
- Buying goes through `IPurchaseService` (mock).
- Footer "Mock shop: no real payment."

### Are You Sure popup (`38_…`, `39_…heart_breaking`)
- Cracked heart and "You will lose 1 life!".
- A red button named after the action: **Home**, **Restart** or **Give up**.
- Close X = cancel and go back.
- Confirm → heart-break animation (~0.75 s) → lives −1 (start the regen timer if lives were full) → do the action.
- With **0 lives**, Home / Restart / Give up after a move don't open this popup: toast "No lives left!" and open **More Lives** instead.

### Edit Profile popup (`11_…avatar`, `12_…frame`), opened from the Home avatar
- Top: the current avatar in its frame, the name and a pencil button.
  - Tap the name or the pencil to edit inline; Enter or blur commits.
  - The name is trimmed with spaces collapsed, length 2–14. Otherwise toast "Name needs at least 2 letters" and keep the old one.
- Tabs **Avatar | Frame**, 3-column grid:
  - Avatar tiles: tap selects (green rim + check).
  - Frame tiles show my avatar inside each frame. Locked frames are greyed with a padlock and their unlock text; tap → toast "Unlock: …".
  - Frames: Neon, Lab, Gold (owned) · Royal (win a Weekly Cup) · Champion (reach level 100).
- **Save** (green): writes the profile, updates Home and my leaderboard row, toast "Profile saved". Close X discards the changes.

### Shop page (`04_store_top.jpg`, `05_store_scroll2–4.jpg`), Shop tab
- Mock IAP, scrollable. Header: coins pill + "Shop" title tab.
- Sections built from `ShopCatalog`:
  - **Special Offers:** Starter Lab Kit (−90% badge).
  - **Lab Bundles:** Intern → Assistant → Scientist ("Popular") → Professor → Mad Genius ("Best Offer").
  - A banner.
  - **Coins:** 6 packs in a 3×2 grid (1k / 5k / 10k / 25k / 50k / 100k).
- A bundle card shows: the art, the coin amount, perk icons (No Ads, ∞ lives + hours), booster mini tiles "xN", the name and a green price button.
- Price button → `IPurchaseService.Buy(item)`. The mock waits 0.8 s, grants the item and shows the toast "Mock purchase complete (no payment taken)". Granting adds coins and boosters and sets `InfiniteLivesUntil` / `NoAds`.
- Footer: "Mock shop: no real payment."
- Full item table (amounts, hours, reference prices): `docs/dev/DEVELOPER_BRIEF.md` §3.2 in the product repo.

### Leaderboard page (`06_…locked`, `07_…weekly`, `08/09_…players…`), Leaderboard tab
- **Locked** while `CurrentLevel < unlockLevel`: big padlock, "Unlocks at Level 40", progress bar `current / 40`.
- Header: "Leaderboard", tabs **Weekly | Players**, and a chip with the time to the week reset ("4d 8h").
- **Weekly Cup:**
  - A group of 6: 5 mock players + me. Score = levels won this week.
  - Podium in the order 2–1–3, then places 4–6 as rows.
  - At the week's end the top 3 get `cupPrizes`, and 1st place unlocks the Royal frame. Mock: grant on the first app open after the reset.
- **Players:**
  - Toggle **World | Country**, ranked by highest level.
  - Rows show rank (medal for 1–3), avatar, name, team/city and "Level N".
  - My row is pinned at the bottom, with rank `1000+` when I'm below the list.
- Data comes from `ILeaderboardService` (mock players from a TextAsset).

### Episode Unlocked (`44_episode_unlocked.jpg`)
- Dark overlay, fireworks and a tilted gold "Episode Unlocked!".
- A card with the episode art, its name and "Level 20 – 39".
- "Tap to Continue": a tap anywhere closes it.

### Mock rewarded ad (`14_mock_rewarded_ad.jpg`)
- Full-screen black, "MOCK REWARDED AD", a big 3-2-1 countdown (`mockAdSeconds`), "The reward arrives when the ad finishes".
- Behind `IAdService.ShowRewarded()`, so a real SDK can replace it later.

### Services
`IPurchaseService`, `IAdService`, `ILeaderboardService`: offline mocks for the case study, owned by `AppRoot` like the other services, so real ones can be plugged in without touching screens.

## Shared UI pieces
- **Toast:** short message, auto-hides, never blocks input. A new message replaces the current one instead of queueing.
- **Popup:** modal with dimmed background; blocks input beneath it. Open = GameObject active. Draw order is hierarchy order: screen content, then popups, then the toast last.
- Placeholder settings icon is "≡": LiberationSans has no gear glyph.
- Canvas Scaler 1080×1920, match 0.5; `SafeArea` on root panels.
- **[+P]** Button prefabs in four colours: green (confirm / buy), magenta (ad), purple (neutral), red (destructive). Each has normal / pressed / disabled states and is 9-sliced. Pill prefab for coins / lives. The look values (colours, sprites, punch scale, ease) are Inspector fields.

## [+P] Feedback pass (step 6)
- One entry point (e.g. `LevelFeedback` / a `FeedbackService`) maps each Core event to animation, VFX, SFX and haptics. All values go in an `AnimationConfig` / `FeedbackConfig` row per event: duration, ease, scale/offset, VFX prefab, SFX clip list, pitch variance, haptic type.
- Starting values (full table: `docs/internal/FEEDBACK_SPEC.md` in the product repo):
  - Core moves 150–350 ms; run stagger 80 ms.
  - Tube complete: 50 ms hit-stop, then the cork drops, sparkles and a rising chime (+1 semitone for each completion in a row).
  - Invalid move: target shake ±8 px for 250–300 ms and a soft "bonk".
  - Land pitch on a pentatonic scale.
- Creature faces (art): idle (one per colour), blink loop, **shock** when lifted or on an invalid move, **happy** when its tube completes and on the win.
- **Look and feel the team picked** (prototype style "İdil"; each item is a toggle or value in the prototype, so make each one a config field):
  - **Flight:** arc ("hop") from tube to tube, the run flying ball by ball with an 80 ms stagger, speed ×1, squash & stretch 0.7 on take-off and landing, a short motion trail.
  - **Selection:** the run lifts above the glass (as above); creatures' **eyes follow the finger** / the selected run.
  - **Tube complete:** wooden **cork tinted in the tube's colour** drops in, sparkles + shockwave, 60 ms hit-stop, a small camera punch, a glow on the tube, and its **creatures dance**. Each completion in a row plays higher (chain escalation). Not used: green tick, glass-shard burst, sleeping creatures.
  - **Win:** confetti + fireworks (sequence in Win above).
  - **Level intro:** tubes drop in with a ~40 ms stagger, balls pop in, ≤ 800 ms in total; input is live as soon as the first tube lands.
  - **Ball colours** ("Nefario Lab" palette, colour id order): red `#E62E3A`, yellow `#FFD23F`, blue `#2F6BFF`, green `#4FD13A`, purple `#7B4DFF`, orange `#FF7A1A`, teal `#2EC4B6`, pink `#FF8FD0`, brown `#8B5A2B`, white `#F2F2F7`, grey `#8A93A8`, dark green `#1B7A44`. These go in `ColorPalette`; the artist's creature sprites replace the tints.
  - **Colour-blind symbols:** a small symbol per colour drawn on the creature, off by default. Worth a Settings toggle later; keep the symbol sprite in `ColorPalette`.
  - The creature design itself (furry / cyclops / minion…) is the artist's call; the view just swaps sprites.
- SFX starting map (`audio/sfx_library/` in the product repo):

  | Event | Clip |
  |---|---|
  | Select | `bs_select_01` |
  | Land | `bs_land_01` |
  | Complete | `bs_complete_01` |
  | Reveal | `ms_reveal_01` |
  | Unlock (box opens) | `ms_special_01` |
  | Win | `ms_win_fanfare` |
  | Reward | `ms_coin_jingle` |
  | Coin | `ms_coin_amount` |
  | Button | `ms_tap_02` |
  | Perfect | `ms_perfect_stamp` |

  `ms_` clips are reference recordings and **must not ship**.
- Haptics:
  - Light: select, lift, reveal, button, coins.
  - Medium: land.
  - Success: tube complete, win.
  - Warning: invalid move.
  - None during ball flight. At least 50 ms between two haptics.
- The Sound and Haptics toggles mute their channel.
- Never block input on a cosmetic animation; a new tap finishes or kills the running tween first.

## [+P] Editor and debug tools (Editor / development builds only)
Extend **Ball Sort ▸ Player Data…** with:
- set lives and the regen timer; fast-forward the life timer and the week reset;
- set `RetryCount` for a level, to test variants and DLB;
- force win; show the solver's next move;
- `unlockLevel` override (leaderboard); mock ad length.

Level jump (already planned): pick the level **and the variant / DLB variant**.
