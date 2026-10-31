# Potion Sort: in-game text changes (product decisions, 31 Oct 2026)

Product went through every player-facing string in the prototype and in the Unity repo (`GJG-Ball-Sort-Game`, commit `640800e`) and picked the final text for each one. This file lists what to change in Unity, in this order:

1. [Terminology](#1-terminology): words every player-facing string must use.
2. [Replace text in existing UI](#2-replace-text-in-existing-ui): find and replace only, no new UI.
3. [Tutorials: the New Item card](#3-tutorials-the-new-item-card): new `TutorialConfig` content and a richer tutorial popup.
4. [New UI to build](#4-new-ui-to-build): screens and elements Unity doesn't have yet, with their texts.
5. [Rule and config changes these texts need](#5-rule-and-config-changes-these-texts-need).
6. [Waiting on art](#6-waiting-on-art).
7. [Not changing](#7-not-changing): strings that stay as they are.

Texts in `code` are exact: copy them as written, including case and punctuation. `●` is the coin glyph used today (`<color=#FFC107>●</color>`); swap in the coin sprite when it exists. `N` is a number filled in at runtime.

---

## 1. Terminology

These words apply to **player-facing text only**. Class, field, asset and JSON names (`Clay`, `Veil`, `Lock`, `BallView`, `TubeCover`…) stay as they are.

| Concept | Write | Don't write |
|---|---|---|
| The container | **tube** | bottle |
| The thing you move | **monster** | ball, creature |
| Hidden ball | **hidden monster** (card name `Hidden Monster`) | hidden ball |
| Large clay tube | **Big Tube** / big tube | big bottle, clay tube |
| Normal-size clay tube | **Ice** / ice tube | clay tube, ice bottle |
| Veil | **Towel** | veil, veiled tube |
| Lock and key | **Locked Box**, **box**, **key** | door, locked tube, lock box |
| Add-tube booster | **Add Tube** | Extra Bottle, + Tube |
| Spelling | **color** (US) | colour |

---

## 2. Replace text in existing UI

Find and replace only. Where a string is a serialized field, change the value in the scene or prefab, not just the C# default.

| Where | Now | New |
|---|---|---|
| `Level.unity` · `StuckPanel._bannerText` | `No more useful moves` | `No available moves` |
| `Level.unity` · `StuckPanel._playOnFormat` | `Play on — {0} ●` | `Play on ● {0}` (coin before the price) |
| `Level.unity` · Celebration text | `Well done!` | the grade: `PERFECT!`, `AWESOME!` or `GOOD JOB!` (rule in §5) |
| `WinPopup.prefab` · `_titleFormat` | `Level {0} complete` | `Level {0}` |
| `AreYouSurePopup.prefab` · body | `You will lose a life.` | `You will lose 1 life!` (still left out at 0 lives) |
| `AreYouSurePopup.prefab` · confirm button | `Confirm` | `Quit` (the same label for Home, Restart and Give up) |
| `MoreLivesPopup.prefab` · `_timerFormat` | `Next life in {0}` | two lines: a label `Time to next life` and the timer on its own, `{0}` (mm:ss) |
| `MoreLivesPopup.prefab` · `_fullText` | `Lives full` | `Your lives are full!` (hide the timer while full) |
| `EpisodeUnlockedOverlay.prefab` · tap text | `Tap anywhere to continue` | `Tap to Continue` |
| `TutorialPopup.prefab` · tap text | `Tap anywhere to continue` | `Tap to Continue` |
| `Level.unity` · booster button labels | `Undo`, `Shuffle`, `+ Tube` | no label: icon and count only |
| `TutorialConfig.asset` · all six entries | see §3 | see §3 |

Toasts that go away (there is no toast in these cases any more):

| Field | Now | Why |
|---|---|---|
| `BoosterBar._noUndosLeft` | `No undos left` | tapping a booster at 0 opens the Out of Boosters popup (§4.2) |
| `BoosterBar._noShufflesLeft` | `No shuffles left` | same |
| `BoosterBar._noAddTubesLeft` | `No add-tube boosters left` | same |

---

## 3. Tutorials: the New Item card

Level 1 keeps today's tutorial popup with a new text. Every mechanic's first level (7, 20, 30, 55, 80) gets a **New Item card** in its place, shown once like today's tutorial. Nothing shows on replays: the prototype's replay toast is dropped.

**Card layout, top to bottom:**
`Level N` · the mechanic's name · `New Item Unlocked!` · picture (§6) · main line, with the key word in caps and highlighted · hint line in smaller type · `Tap to Continue`.

`TutorialConfig` entries need three fields for this: `name`, `line` (rich text for the highlighted word) and `hint`, next to the existing `_image`. Level 1 only uses `line`.

| Level | Name | Main line (highlighted word in **bold**) | Hint |
|---|---|---|---|
| 1 | (none: plain tutorial popup, no card header) | `Tap a tube, then tap the target. Put same-color monsters together!` | (none) |
| 7 | `Hidden Monster` | `Move the top monster to` **`REVEAL`** `the one below!` | `A "?" shows its color once it is on top.` |
| 20 | `Big Tube` | `Gather one color in the` **`BIG TUBE`**`!` | `It only takes its own color.` |
| 30 | `Towel` | `Complete the badge color to lift the` **`TOWEL`**`!` | `The tube under it opens up.` |
| 55 | `Ice` | `Fill the tube to break the` **`ICE`**`!` | `You can pour in, but never out.` |
| 80 | `Locked Box` | `Find the` **`KEY`** `to open the` **`BOX`**`!` | `Dig out the monster carrying it.` |

Plain-text versions to paste:

```
L1  line: Tap a tube, then tap the target. Put same-color monsters together!
L7  name: Hidden Monster   line: Move the top monster to REVEAL the one below!   hint: A "?" shows its color once it is on top.
L20 name: Big Tube         line: Gather one color in the BIG TUBE!               hint: It only takes its own color.
L30 name: Towel            line: Complete the badge color to lift the TOWEL!     hint: The tube under it opens up.
L55 name: Ice              line: Fill the tube to break the ICE!                 hint: You can pour in, but never out.
L80 name: Locked Box       line: Find the KEY to open the BOX!                   hint: Dig out the monster carrying it.
```

---

## 4. New UI to build

Screens and elements Unity doesn't have yet. Each one lists every text it shows.

### 4.1 Level HUD: tier tag
Under the `Level N` plate, show a tag on hard levels: `HARD` or `SUPER HARD` (the prototype also adds a skull icon next to the plate). Nothing on regular levels.

### 4.2 Out of Boosters popup
Opens when the player taps a booster whose count is 0, instead of a toast. It shows the coin balance at the top, a close button (✕), the booster's icon, and these texts:

| | Undo | Shuffle | Add Tube |
|---|---|---|---|
| Title | `Undo` | `Shuffle` | `Add Tube` |
| Heading | `Out of boosters!` | `Out of boosters!` | `Out of boosters!` |
| Description | `Take back your last move.` | `Mixes the monsters in the selected tube.` | `Adds an empty tube to the board.` |
| Button | `Buy ● 300` | `Buy ● 400` | `Buy ● 900` |

Buy spends the coins, adds 1 to that booster and closes the popup. No toast after buying. Not enough coins: see §4.3.

### 4.3 Not enough coins + Coins popup
- Toast `Not enough coins` when the player can't pay for **Play on**, **Buy** (Out of Boosters) or **Refill** (More Lives). Today Play on is only disabled; it should stay tappable and show the toast.
- After Play on or Buy fails, the **Coins** popup opens. It also opens from the `+` on the coin pill. Contents: title `Coins`, the coin packs from §4.8 (amount and price), close (✕), and the note `Mock store: no real payment.` Packs don't give anything yet.

### 4.4 Win card
Replaces today's Win panel content:
- Title `Level N`.
- Grade line: `PERFECT!`, `AWESOME!` or `GOOD JOB!` (rule in §5). The same word is the big celebration text before the card (§2).
- Label `Rewards:`.
- The reward as a **coin pile with the amount**: 20 / 60 / 100 for regular / hard / super hard (§5). This replaces `+20 ●`.
- `Continue` (unchanged).

### 4.5 More Lives popup: Refill and ad
Below the timer, two buttons (both disabled while lives are full):
- `Refill ● 900`: refills lives to the maximum for 900 coins. Not enough coins → §4.3.
- `+1 Life` with an ad icon: plays the mock rewarded ad below, then adds 1 life.

**Mock ad screen** (full screen, a 3-second countdown, then closes by itself):
`MOCK REWARDED AD` · the countdown number · `The reward arrives when the ad finishes`. No toast afterwards.

### 4.6 Episode Unlocked: level range
Under `Episode N unlocked` and the mascot frame, add the range line `Level 21 - 40` (the episode's first and last level). No episode names.

### 4.7 Home: reward multiplier on the Play button
Next to the `Hard` / `Super hard` badge, show the reward multiplier: `x3` on hard, `x5` on super hard (reward ÷ regular reward, §5).

### 4.8 Shop page content
Replaces the three placeholder bundle cards. Static: buttons do nothing and show no toast.

Section headers: `Special Offers` · `Lab Bundles` · `Coins`.

| Section | Name | Coins | ∞ lives | Each booster | Price | Tag |
|---|---|---|---|---|---|---|
| Special Offers | `Starter Lab Kit` | 1 000 | 1h | x1 | `49,99 TL` | `90% OFF` |
| Lab Bundles | `Intern Bundle` | 2 000 | 3h | x1 | `249,99 TL` | |
| Lab Bundles | `Assistant Bundle` | 4 000 | 6h | x3 | `499,99 TL` | |
| Lab Bundles | `Scientist Bundle` | 8 000 | 12h | x8 | `999,99 TL` | `Popular` |
| Lab Bundles | `Professor Bundle` | 20 000 | 36h | x18 | `2.499,99 TL` | |
| Lab Bundles | `Mad Genius Bundle` | 60 000 | 72h | x36 | `4.999,99 TL` | `Best Offer` |

All Lab Bundles also show a "no ads" perk icon.

Coins section, six tiles: 1 000 · `99,99 TL`, 5 000 · `399,99 TL`, 10 000 · `799,99 TL`, 25 000 · `1.499,99 TL`, 50 000 · `2.999,99 TL`, 100 000 · `4.999,99 TL`.

Note at the bottom of the page: `Mock store: no real payment. Prices copied from the Magic Sort reference.`

### 4.9 Leaderboard page
There is no backend: the page is filled from the mock data below, so it looks like the real thing. It replaces today's placeholder rows and `Coming soon`.

- **Locked** (player's level below 40): a lock instead of the lists: `Unlocks at Level 40`, then `Keep sorting! Compete with players around the world once you reach level 40.` and a progress bar `N / 40`. (Set the unlock level to 10 in a debug config to test it sooner.)
- **Header:** title `Leaderboard`, tabs `Weekly` · `Players`, and on `Weekly` a countdown to the weekly reset (Sunday 00:00 local) as `Nd Nh`, e.g. `5d 20h`.
- **`Weekly` tab:** a `Weekly Cup` of 6: the five `weekly` players below plus the player. Score = levels won this week (for now, the player's completed levels is fine). Sort by score, highest first; on a tie the player goes first. Places 1–3 stand on a podium, each with avatar, name, `Score: N` and its prize from `prizes` (coin icon + 1000 / 500 / 250). Places 4–6 are rows: rank, avatar, name, team, label `Score` and the score.
- **`Players` tab:** a `World` · `Country` switch over the 20 players of `world` or `country`. Each row: rank, avatar, name, team under the name (left out when empty), label `Level` and the level. Ranks 1–3 show a gold / silver / bronze medal instead of the number. The player's own row is pinned below the list and highlighted: rank = players above the player's level + 1, shown as `1000+` when the player would fall past the end of the list.
- **Avatars:** `av` is a monster colour id; show that colour's monster face. The player's row uses their own avatar.

Mock data (the prototype's own; it could go into a `TextAsset` such as `Assets/Data/Config/LeaderboardMock.json`):

```json
{
  "unlockLevel": 40,
  "prizes": [1000, 500, 250],
  "weekly": [
    {"name": "ANGRYROGUE",   "team": "",            "score": 9, "av": 2},
    {"name": "floppytats",   "team": "The goodies", "score": 7, "av": 6},
    {"name": "happycat",     "team": "",            "score": 4, "av": 5},
    {"name": "WhiteSpectre", "team": "",            "score": 2, "av": 3},
    {"name": "роман",        "team": "",            "score": 1, "av": 1}
  ],
  "world": [
    {"name": "labRat_77",   "team": "Mad Scientists", "level": 100, "av": 4},
    {"name": "Stacyjo",     "team": "Imago Dei",      "level": 100, "av": 7},
    {"name": "alex :)",     "team": "Canada",         "level": 98,  "av": 2},
    {"name": "Sajjad",      "team": "Iraq",           "level": 96,  "av": 5},
    {"name": "Nikita",      "team": "gogo",           "level": 93,  "av": 6},
    {"name": "David",       "team": "USA Players",    "level": 91,  "av": 1},
    {"name": "xXx",         "team": "",               "level": 88,  "av": 0},
    {"name": "safe",        "team": "Team America",   "level": 86,  "av": 3},
    {"name": "PotionQueen", "team": "Brew Crew",      "level": 83,  "av": 7},
    {"name": "Mira.k",      "team": "Germany",        "level": 80,  "av": 8},
    {"name": "bubbles",     "team": "",               "level": 78,  "av": 2},
    {"name": "Dr.Fizz",     "team": "Lab Coats",      "level": 75,  "av": 4},
    {"name": "hanna_s",     "team": "Sweden",         "level": 72,  "av": 9},
    {"name": "GooGoo",      "team": "",               "level": 70,  "av": 1},
    {"name": "Kenji",       "team": "Japan",          "level": 67,  "av": 6},
    {"name": "flaskfan",    "team": "Brew Crew",      "level": 64,  "av": 5},
    {"name": "Lucía",       "team": "Spain",          "level": 61,  "av": 0},
    {"name": "n0va",        "team": "",               "level": 58,  "av": 3},
    {"name": "Omar_92",     "team": "Egypt",          "level": 55,  "av": 8},
    {"name": "Zuzu",        "team": "Mad Scientists", "level": 52,  "av": 2}
  ],
  "country": [
    {"name": "seraa",   "team": "Urla",         "level": 100, "av": 7},
    {"name": "freya",   "team": "",             "level": 99,  "av": 2},
    {"name": "sezer",   "team": "",             "level": 97,  "av": 5},
    {"name": "Nilufer", "team": "TÜRKİYE",      "level": 95,  "av": 1},
    {"name": "Teytip",  "team": "Edirne",       "level": 92,  "av": 4},
    {"name": "Sude",    "team": "",             "level": 90,  "av": 0},
    {"name": "Esmail",  "team": "Team America", "level": 87,  "av": 6},
    {"name": "Ferina",  "team": "Çay Sevenler", "level": 85,  "av": 3},
    {"name": "nero",    "team": "Orka",         "level": 82,  "av": 8},
    {"name": "Seco",    "team": "TÜRKİYE",      "level": 79,  "av": 2},
    {"name": "ikra",    "team": "",             "level": 77,  "av": 9},
    {"name": "Gjdkfjd", "team": "Bobby",        "level": 74,  "av": 5},
    {"name": "Sima",    "team": "Galatasaray",  "level": 71,  "av": 7},
    {"name": "kaanX",   "team": "İzmir",        "level": 68,  "av": 1},
    {"name": "Elif.b",  "team": "Çay Sevenler", "level": 65,  "av": 4},
    {"name": "mert35",  "team": "İzmir",        "level": 62,  "av": 0},
    {"name": "Deniz",   "team": "Bodrum",       "level": 59,  "av": 3},
    {"name": "Ayşe",    "team": "",             "level": 56,  "av": 6},
    {"name": "tuna",    "team": "Orka",         "level": 53,  "av": 8},
    {"name": "Berk",    "team": "Ankara",       "level": 50,  "av": 2}
  ]
}
```

### 4.10 Edit Profile
- Two tabs under the name field: `Avatar` · `Frame` (grids empty until art arrives, §6). No frame names, no lock texts.
- Save with fewer than 2 letters: Save stays tappable, shows the toast `Name needs at least 2 letters` and doesn't save.
- After a successful save, toast `Profile saved`.

---

## 5. Rule and config changes these texts need

- **Shuffle works on one selected tube** (rule change, replaces the whole-board shuffle in `docs/mechanics.md`):
  - The player selects a tube first (the normal tap selection), then taps Shuffle. Only that tube's monsters are reordered; every other tube is untouched. The selection clears afterwards.
  - The selected tube must be open: not covered (towel or locked box), not ice / Big Tube (clay) and not completed.
  - Today's other shuffle rules still apply inside that tube: a monster carrying a key keeps its slot; hidden monsters keep their hidden flag, except one that lands on top is revealed; the result must differ from the current order, complete no tube, expose no key and leave the board solvable. Spent only on success, clears the undo history, not undoable.
  - Shuffle tapped with nothing selected: nothing happens and the booster isn't spent. Toast: `Select a tube first`.
  - No valid order for that tube (for example one color only, or every order fails the rules): the existing toast `Can't shuffle right now`, booster not spent.
- **Win rewards** (`EconomyConfig`): regular 20 (unchanged), hard 40 → **60**, super hard 60 → **100**. The `x3` / `x5` badge (§4.7) follows from these.
- **Win grade** (§4.4), from the level's `meta.optimalMoves` (`min`) and the moves made:
  - `PERFECT!`: no booster used (Undo, Shuffle, Add Tube or Play on) **and** moves ≤ `ceil(min × 1.25) + 1`
  - `AWESOME!`: moves ≤ `ceil(min × 1.8) + 2`
  - `GOOD JOB!`: otherwise
  Keep the two factors and two slacks as config fields.
- **Booster prices** (new in `EconomyConfig`): Undo 300, Shuffle 400, Add Tube 900, each buys 1.
- **Lives refill price** (new in `EconomyConfig`): 900 coins.
- **Booster at 0**: tapping it opens the Out of Boosters popup (§4.2). That includes Undo, so `No undos left` is never shown. `Nothing to undo` and `Can't undo past a completed tube` still show when Undo has charges but can't run.
- **Player name**: at least 2 letters (was 1). The maximum stays 16.
- Today's starting booster counts (99) mean the popup almost never opens. Lower them to test it (the prototype starts at 3 each).

---

## 6. Waiting on art

Build these with placeholders now and swap the sprites in when they arrive.

- **New Item card pictures** for Hidden Monster, Big Tube, Towel, Ice and Locked Box. Until then, use the board's own sprites (a hidden monster, the ice art, a towel, the door with its lock).
- **Coin pile** on the Win card. Until then, the coin icon with the amount.
- **Skull icon** next to the in-level tier tag.
- **Ad icon** on the `+1 Life` button, and the **no ads** / **∞ lives** perk icons on shop bundles.
- **Shop art**: bundle illustrations, coin stacks, section ribbons.
- **Avatars and frames** for Edit Profile.
- **Ice breaking**: the L55 card says `Fill the tube to break the ICE!`, so the ice art should visibly break or disappear when an ice tube is completed.

---

## 7. Not changing

These stay exactly as they are in Unity today:

`Level N` (HUD and Play button) · `Nothing to undo` · `Can't undo past a completed tube` · `Can't shuffle right now` · `No room for more tubes` · `Shuffling…` · `Give up` · `Continue` · `Are you sure?` · `Settings` · `Sound` · `Music` · `Haptics` (both scenes) · `Restart` · `Home` · `More lives` · `Full` (Home lives pill) · `Episode N unlocked` · `All levels complete` · `Hard` · `Super hard` · `Shop` · `Leaderboard` · bottom bar art (Shop · Home · Leaderboard) · `Edit profile` · `Name` / `Your name` · `Save` · `Level N could not be loaded`.

Today's `Coming soon` and `#1 ———` / `You` placeholder rows on the Leaderboard page go away (§4.9).

Not added from the prototype: the No Moves popup, episode names, Notifications / Terms / Privacy / Restore Purchase in Settings, the `No lives left!` toast (More Lives opens directly, as today), the `Undo +1` toast after buying, and toasts after shop purchases or ads.
