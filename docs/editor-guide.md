# Editor Guide — playthrough reference

For teammates who know a little Unity but not this project. Everything here is Editor-only. Nothing in it ships.

**Contents:** [1 Ball Sort menu](#1-ball-sort-menu) · [2 What costs what](#2-what-costs-lives-coins-boosters-or-progress) ·
[3 Config assets](#3-config-assets) · [4 Recipes](#4-quick-recipes) · [5 Gotchas](#5-gotchas)

**Basics**
- Scenes: `Assets/Scenes/Home.unity` (the main menu) and `Assets/Scenes/Level.unity` (the board). Pressing Play works in either one.
- Pressing Play in **Home** is the real game. Pressing Play in **Level** also plays the real game, on your saved current level.
- There is one save per machine. Every Play session reads it and writes to it (section 5, *Save data*).

---

## 1. Ball Sort menu

| Menu item | Opens | Works in Play mode? |
|---|---|---|
| **Ball Sort ▸ Play Level…** | Play Level window | No (leave Play mode first) |
| **Ball Sort ▸ Validate Levels…** | Validate Levels window | Its Play buttons don't |
| **Ball Sort ▸ Player Data...** | Player Data window | Yes (edits the live player) |
| **Ball Sort ▸ Reset Save** | Runs right away | No (greyed out) |
| **Ball Sort ▸ Art ▸ Prepare Sprites** | Runs right away | Don't |

### Play Level… — play any level or test file, in any variant
A **test visit**. It opens the Level scene, enters Play mode and plays the level you picked, once.

1. **Variant**: slider from 1 to 5. Every level has 5 variants. A test visit plays this variant on every try, Restart included.
2. **Level of the run**: pick the *Level database* (filled in for you) and a **Level** from 1 to 100. The *File* line shows the JSON it will load. Then click **Play level N, VX**.
3. **Level file**: drag any level JSON in, usually one from `Assets/Data/TestLevels/`, then click **Play \<file\>, VX**.

- If a scene has unsaved changes, Unity asks you to save it first. Choose **Don't Save** unless you meant to change the scene (section 5).
- The HUD title shows the visit, e.g. `Level 6 · V5` or `stuck_lock · V1`.
- **One visit only.** Leaving to Home and pressing the Level button there plays your saved level again, as a real run.
- What a test visit records, and what it still spends: section 2.

Test files in `Assets/Data/TestLevels/`:

| File | What it's for |
|---|---|
| `stuck_after_move.json` | Gets stuck in one move: move the **3rd** tube's top ball onto the **4th** tube |
| `stuck_lock.json` | Gets stuck with a locked tube still closed (V1: **2nd → 5th**, then **1st → 2nd**). Good for testing Play on |
| `veil.json` | Veil (towel). Finish red to lift it. V5 also has a hidden ball |
| `lock_and_key.json` | Lock and key. The first move finds the key |
| `clay_large_tube.json` | Clay (ice) on a capacity-10 tube that spans two rows |
| `uneven_rows.json`, `full_rows.json` | Layouts: rows of 4 + 3, and 6 + 6 (the widest row allowed) |

Tubes are counted left to right, top row first.

### Validate Levels… — check that every level is valid and solvable
1. Leave *Level database* (`Assets/Data/LevelDatabase.asset`) and the node limits as they are: *Fast node limit* 200,000 and *Optimal node limit* 500,000.
2. Click **Validate all levels**. It checks every variant of all 100 levels. A progress bar shows how far it has got and has a **Cancel** button. A full run takes minutes.
3. You get one row per variant:

| Column | Meaning |
|---|---|
| Level, Variant | `level_042`, `V3`. A row that says `file` lists problems outside any single variant |
| Colours / tubes | Board size |
| Mechanics | Mechanics used (hidden, veil, lock, clay, large tube) |
| Solvable | `yes`, `NO` (unsolvable), `unknown` (hit the node limit) or `invalid` (red error box under the row) |
| Optimal | Shortest solution in moves. `≥ n` means the search hit its limit, `–` means it didn't run |
| Nodes, Time | How hard the solver worked: *fast / optimal* |
| **Play** | Starts a test visit of that row's level and variant (like Play Level) |

- **Write meta.optimalMoves** saves the Optimal column into the level JSON files. ⚠ This **edits files** under `Assets/Data/Levels/`, so they show up in git. Only the level designer should press it.
- Validating doesn't touch the save.

### Player Data... — edit coins, lives, boosters, progress, tutorials
The banner at the top tells you which data you are editing:
- **Edit mode**: the save on disk. Changes apply on the next Play.
- **Play mode**: the live player. The HUD updates at once and the change is saved.

| Control | What it does |
|---|---|
| **Coins** | Coin balance (≥ 0) |
| **Lives (max 5)** | Lives, 0–5 |
| **Next life in (minutes)** | Time until the next life comes back. Minimum 0.05 min (3 s). Greyed out at full lives |
| **Player name** | 1–16 characters. An invalid name is kept as it was, with a warning in the Console |
| **Avatar** | 1–5 |
| **Undo / Shuffle / Add tube** | Booster counts (≥ 0) |
| **Apply** | Writes **all** the fields above at once |
| **Reload** | Re-reads the fields from the player |
| Status line | Read-only: level, retries, last variant, level in progress, tutorials seen |
| **Current level** + **Jump** | Moves progress to level N and **clears the retries and the level-in-progress mark**. Apply never does this |
| **Clear tutorials seen** | Every tutorial shows again on its level |

⚠ In Play mode the fields don't follow the game. **Press Reload before you edit.** Otherwise Apply writes back stale values, such as coins from before your last win.

### Reset Save
Deletes the save. The next Play starts a new player: level 1, 1000 coins, 5 lives, 99 of each booster, no tutorials seen, Sound on. This menu item is greyed out in Play mode, because the running game would write its data straight back.

### Art ▸ Prepare Sprites — not for playthroughs
For a new art delivery:
1. Move files from `Assets/Art/Incoming/` to their places, or copy them over the files they replace.
2. **Trim the image files in place.**
3. Apply the import settings (100 PPU, no mipmaps, ASTC on mobile).

You can safely run it more than once, but it changes art files, so leave it to the developer (docs/art-inventory.md).

### Automatic, no menu
- **Sound import rules**: a new file in `Assets/Audio/SFX/` gets mobile import settings the first time it is imported.
- **Assets ▸ Create ▸ Ball Sort ▸ …** creates new config assets. You don't need it, and a second `LevelDatabase` can confuse the tools (section 5).

---

## 2. What costs lives, coins, boosters or progress

**Real run** = Home's Level button, or pressing Play in the Level scene. **Test visit** = Play Level, or a Play button in Validate Levels.

| Action | Real run | Test visit |
|---|---|---|
| Win | **+20 / +60 / +100 coins** (regular / hard / super hard), **progress +1**, retries reset | Nothing. The Win panel shows no reward |
| Settings ▸ Home, Give up, or Settings ▸ Restart (confirmed) | **−1 life, +1 retry**. Restart loads the next variant | Nothing. Restart replays the same variant |
| **Stop Play mode mid-level** (= closing the app) | **−1 life, +1 retry on the next Play** | Nothing |
| Restart at 0 lives | Opens More Lives instead | Works |
| Use Undo / Shuffle / Add tube | **−1 booster** (cancelling a Shuffle pick is free) | **−1 booster** (same inventory) |
| Buy a booster (tap one at 0) | **−300** Undo, **−300** Shuffle, **−900** Add tube coins | **Same, real coins** |
| Play on (when stuck) | **−900 coins**, no life | **−900 coins** |
| Refill hearts (Home) | **−900 coins** | — |
| Shop coin bundles | **+coins, free** (no real payment) | — |
| Tutorial popup dismissed | Saved as seen | Shows on every visit, never saved |
| Episode Unlocked | Shows after winning 19 / 39 / 59 / 79 | Never |
| Player Data, Reset Save | Change the save directly | — |
| Validate Levels, Prepare Sprites | No save changes. They can **edit files** (see above) | — |

In short, a test visit never touches lives, retries, rewards, progress or tutorials, **but it still spends boosters and coins**.

---

## 3. Config assets

All in `Assets/Data/Config/`, except `Assets/Data/LevelDatabase.asset`. Select one in the Project window and edit it in the Inspector. Field names below are the Inspector labels.

**Edits made in Play mode are kept.** Config assets are files, so unlike scene objects they don't reset when you stop Play. They also show up in git, so revert test tweaks before anyone commits. The **When** column says when a change takes effect:

| When | Meaning |
|---|---|
| **Live** | On the next use, while you play (an animation already running finishes with the old value) |
| **Next level** | When a level next loads (Restart, or Home → Level) |
| **Next Home** | When the Home scene next loads |
| **Restart Play** | Stop and press Play again |
| **New save** | Only for a new player (after Reset Save) |

**Units:** **s** = seconds. On the board these are board seconds at Speed 1, so they scale with Speed. **d** = ball diameters (one ball slot, 0.9 u). **u** = board units (a tube is 1.1 u wide before the board is scaled to fit the screen). **×** = multiplier. **°** = degrees. Shake and wobble **rate** is in rad/s, **damping** in 1/s (higher = settles faster). **semitones**: 12 = one octave. **TL**: display text only.

### EconomyConfig — `Assets/Data/Config/EconomyConfig.asset`
| Group | Field | Value | Unit | What it controls | When |
|---|---|---|---|---|---|
| New player | Start Coins | 1000 | coins | Coins of a new player | New save |
| | Start Undo / Start Shuffle / Start Add Tube | 99 / 99 / 99 | boosters | Boosters of a new player | New save |
| Lives | Max Lives | 5 | lives | Most lives a player can hold | Restart Play |
| | Life Refill Minutes | 30 | minutes | Wait for one life to come back | Restart Play |
| Rewards and prices | Regular / Hard / Super Hard Reward | 20 / 60 / 100 | coins | Coins for a win, by level tier | Live |
| | Play On Cost | 900 | coins | Price of Play on when stuck | Live |
| Boosters | Added Tube Capacity | 4 | balls | Size of a tube from Add tube or Play on | Next level |
| Buying with coins | Undo Price / Shuffle Price / Add Tube Price | 300 / 300 / 900 | coins | Buy popup prices | Live |
| | Boosters Per Purchase | 1 | boosters | Boosters one purchase gives | Live |
| | Lives Refill Price | 900 | coins | Refill hearts (Home) | Live |
| Shop | Coin Bundles (6) | 1,000 · 5,000 · 10,000 · 25,000 · 50,000 · 100,000 | coins | Shop grid, top left to bottom right | Next Home |
| | — their price labels | 99,99 · 399,99 · 799,99 · 1499,99 · 2999,99 · 4999,99 | TL | Text on the price tag | Next Home |
| | Coin Packs (3) | 1,000 / 259,99 · 10,000 / 899,99 · 50,000 / 3099,99 | coins / TL | Monster, Mega and Super Pack cards | Next Home |

### AnimationConfig — `Assets/Data/Config/AnimationConfig.asset`
Every field is **Live**. Details for each beat are in docs/animation-spec.md (the section numbers match the Inspector headers).

| Group | Field | Value | Unit | What it controls |
|---|---|---|---|---|
| **Global** | **Speed** | 1 | × | **All board animations and the Win panel.** 0.25 = four times slower |
| 2.2 Select | Lift Height | 0.72 | d | How high a selected ball floats above the tube mouth (also the tap area above the rim) |
| | Lift Duration · Lift Ease | 0.23 · OutCubic | s | The lift |
| | Lift Bob · Lift Bob Period | 0.06 · 1.25 | d · s | The held ball bobbing up and down |
| 2.3 Flight | Rise Duration · Rise Ease | 0.1 · OutQuad | s | The ball leaving the source tube |
| | Arc Base Duration | 0.15 | s | The arc between tubes… |
| | Arc Duration Per Screen Width | 0.23 | s | …plus this per screen width travelled |
| | Arc Ease | InOutSine | — | Arc timing curve |
| | Arc Height Base · Arc Height Per Distance | 0.5 · 0.32 | d · d per u | Arc height |
| | Drop Duration · Drop Ease | 0.12 · InQuad | s | The drop into the target tube |
| | Chain Delay | 0.08 | s | Gap between balls of one move |
| | Squash | 0.7 | × | Scales every squash and stretch |
| | Stretch Full Speed · Stretch Max · Stretch Min Speed | 6.7 · 0.3 · 0.15 | screen widths/s · fraction · screen widths/s | Stretch while flying |
| | Trail · Trail Time · Trail Width · Trail Alpha | on · 0.15 · 1.4 · 0.45 | — · s (real time) · ball radii · 0–1 | Trail behind a flying ball |
| 2.4 Landing | Land Squash Amount · Duration · Damping · Frequency | 0.32 · 0.45 · 11 · 30 | fraction · s · 1/s · rad/s | Squash when a ball lands |
| | Hop Bounce Height · Duration · Damping | 0.4 · 0.55 · 4 | d · s · 1/s | Bounce of the landing ball |
| | Bump Height | 0.07 | d | Bump of the balls below and of the tube |
| | Ball Bump Duration · Bump Ripple Delay · Tube Bump Duration | 0.2 · 0.03 · 0.22 | s | Bump timing (the ripple delay is per ball of depth) |
| | Droplets | on | — | Splash droplets on landing |
| 2.5 Invalid move | Invalid Shake Amount · Duration · Frequency · Damping | 0.16 · 0.4 · 55 · 9 | d · s · rad/s · 1/s | Shake of the tapped tube |
| | Shock Face Duration | 0.7 | s | How long the balls look shocked |
| 2.6 Tube complete | Hit Stop Duration · Hit Stop Time Scale | 0.06 · 0.06 | s (real time) · × | Short freeze when the cork seats |
| | Punch Scale · Punch Duration | 0.028 · 0.3 | fraction · s | Board scale kick |
| | Cork Drop Height · Cork Drop Duration · Cork Ease · Cork Impact Time | 1.2 · 0.45 · OutBounce · 0.16 | d · s · — · s | The cork falling in |
| | Glow · Glow Fade Duration · Glow Idle Alpha · Glow Pulse · Glow Pulse Period | on · 0.6 · 0.5 · 0.12 · 1.6 | — · s · 0–1 · 0–1 · s | Glow behind a completed tube |
| | Dance · Dance Stagger | on · 0.07 | — · s | Balls hop, bottom to top |
| | Sparkles · Confetti · Chain Escalate | on · on · on | — | Effects. Chain Escalate gives more sparkles for each tube completed |
| 3.4 Ice break | Ice Crack Time | 0.16 | s | Cracking before the burst |
| | Ice Shiver Amount · Ice Shiver Frequency | 0.04 · 70 | d · rad/s | Ice shivering while it cracks |
| | Ice Crack Brighten | 0.5 | 0–1 | How white the ice turns before bursting |
| 6.1 Blink | Blink | on | — | Random blinking |
| | Board Blink Interval Min · Max | 2 · 5 | s | Time between blinks (one ball each time) |
| | Blink Close · Closed · Open Duration | 0.08 · 0.16 · 0.08 | s | The blink frames |
| 3.1 Hidden reveal | Reveal Flip Duration | 0.32 | s | "?" ball flipping to its colour |
| 3.2 Veil (towel) | Towel Delay | 0.45 | s | Wait before the towel lifts |
| | Towel Lift Duration · Towel Rise · Towel Tilt | 0.6 · 2.6 · 5 | s · d · ° | The lift |
| | Towel Stagger | 0.08 | s | Gap between several towels lifting |
| | Towel Sway · Towel Sway Angle · Towel Sway Period | on · 1 · 4 | — · ° · s | Idle sway of a closed towel |
| 3.3 Lock box | Key Wait After Complete | 0.25 | s | Pause before the key flies (when the move also completed a tube) |
| | Key Fly Duration · Key Fly Ease · Key Arc Height | 0.6 · InOutCubic · 1.5 | s · — · d | Key flight to the slot |
| | Key Hold Duration · Lock Fade Duration | 0.1 · 0.25 | s | The seated key holds, then fades |
| | Box Wobble Angle · Squash · Duration · Frequency · Damping | 2 · 0.05 · 0.25 · 40 · 12 | ° · fraction · s · rad/s · 1/s | Box wobble when the key seats |
| | Box Open Delay · Box Slide Duration · Box Slide Ease · Box Hold Duration | 0.55 · 0.45 · OutCubic · 0.15 | s | Doors sliding open |
| 4 Shuffle | Shuffle Speed | 1.7 | × | Shuffle flights compared with a normal move |
| | Shuffle Stagger | 0.05 | s | Gap between moving balls |
| | Shuffle Rise Height · Shuffle Arc Height | 0.4 · 0.3 | d | The hop |
| | Shuffle Wiggle Angle · Duration · Frequency · Damping | 8 · 0.4 · 30 · 8 | ° · s · rad/s · 1/s | Wiggle of a ball that keeps its slot |
| 5.1 Stuck | Stuck Delay · Stuck Fade Duration | 0.4 · 0.25 | s (fade: real time) | Stuck bar timing after the board settles |
| 5.2 Win: board | Win Celebration Delay · Win Tube Stagger · Win Hop Spread · Win Overlay Delay | 0.3 · 0.06 · 0.4 · 0.7 | s | Board celebration, then the Win panel |
| 5.2 Win: panel | Win Dim Duration · Win Picture Pop Duration | 0.35 · 0.7 | s | Dim and picture pop |
| | Win Card Delay · Win Title Start | 1 · 0.2 | s | When the title starts |
| | Win Letter Duration · Win Letter Stagger · Win Stamp Base | 0.3 · 0.065 · 0.18 | s | Title letters and their stamp sound |
| | Win Reward Delay · Win Continue Delay · Win Pop Duration | 0.95 · 1.3 · 0.45 | s | Reward line and Continue button |
| UI (real time) | Purchase Punch Scale · Duration · Vibrato | 0.25 · 0.35 · 6 | fraction · s · count | Punch on a filled badge, heart or coins pill. Speed doesn't affect it |

### TubeVisualConfig — `Assets/Data/Config/TubeVisualConfig.asset`
Every field is **Live**: changing one in Play mode rebuilds the board on screen.

| Group | Field | Value | Unit | What it controls |
|---|---|---|---|---|
| Tube | Rim Back / Rim Front / Middle / Bottom / Outline Sprite | final art | sprite | Tube parts (art only) |
| | Tube Tint | white | colour | Tint of the tube |
| | Art Body Width | 145 | px | Width of the tube body in the art. It scales the art to Tube Width |
| Glass front | Front Sprite · Front Tint | none · white, 12 % | sprite · colour | Optional glass over the balls (unused) |
| Cork | Cap Sprite · Cap Tint | plain cork · white | sprite · colour | Fallback cork for colours without their own |
| Glow | Glow Sprite · Glow Tint · Glow Padding | art · white · 0.12 | sprite · colour · u | Glow around a completed tube |
| Geometry | Tube Width | 1.1 | u | Tube body width |
| | Slot Height | 0.91 | u | Height each ball slot adds (also sets row spacing) |
| | Bottom Offset | 0.59 | u | Bottom ball's centre above the tube base |
| | Top Padding | 0.15 | u | Gap between the top slot and the rim |
| | Ball Diameter | 0.9 | u | Slot size. **d** in AnimationConfig is this |
| Balls | Ball Scale | 0.9 | × of Ball Diameter | Drawn ball size |
| | Ball Spacing | 0.855 | u | Distance between resting balls (keep it ≤ Slot Height) |
| Spacing | Horizontal Spacing | 0.45 | u | Gap between neighbouring tubes |
| Veil | Veil Sprite · Veil Tint | towel art · white | sprite · colour | The veil |
| | Veil Height Scale | 0.92 | × | How far the veil hangs down |
| | Veil Sticker Size | 0.55 | d | Ball sticker on the veil |
| Lock box | Lock Side / Face / Inside / Cover Sprite | final art | sprite | Box parts |
| | Lock Box Padding | (0.08, 0.1) | u | How far the box reaches past the tubes (x, y) |
| | Lock Slot Centre | (0.5, 0.518) | fraction of the mid art | Where the key seats |
| | Lock Slot Key Width | 0.68 | fraction of the mid width | Size of the seated key |
| | Lock Cover Inner Edge | 0.816 | fraction of the cover width | Where the two door halves meet |
| Clay | Clay Sprite · Clay Tint · Clay Scale | ice art · white · 1.1 | sprite · colour · × tube width | Ice at the base of clay tubes |
| Shuffle pick | Shuffle Highlight Tint | pale yellow, 90 % | colour | Glow on tubes you can pick |
| | Shuffle Dim Shade | 0.45 | 0–1 brightness | Darkening of the other tubes |

### ColorPalette — `Assets/Data/Config/ColorPalette.asset`
**Next level** (or any TubeVisualConfig edit, which rebuilds the board).

| Field | Value | Unit | What it controls |
|---|---|---|---|
| Default Sprite | `Placeholder/Ball_Circle` | sprite | Ball used for colours without art |
| Art Sprite Size | 1.4628571 | d | Width of the ball art canvas compared with the round body. Art-related, don't touch |
| Entries[0–11] | see below | — | One entry per colour id |
| — Open / Half Open / Closed / Laugh / Shock | monster faces | sprite | Faces. Shock is empty, so the closed face is used |
| — Sprite Tint | white (ids 0–9) | colour | Multiplies the sprite |
| — Color | per colour | colour | Glow, trail, droplets and the plain cork |
| — Cork | per-colour cork (ids 0–9) | sprite | Cork of a completed tube |
| Hidden Sprite · Hidden Tint | "?" monster · white | sprite · colour | Hidden ball |
| Hidden Mark Sprite · Hidden Mark Size | none · 0.55 | sprite · d | Extra "?" (empty: the art has its own) |
| Key Mark Size · Key Mark Offset | 0.5 · (0, −0.3) | d | Key held by a ball |

Colour ids: 0 Red · 1 Blue · 2 Yellow · 3 Green · 4 Purple · 5 Orange · 6 Turquoise · 7 Pink · 8 Lilac · 9 Brown · 10 navy placeholder · 11 white placeholder (no art, unused by the levels).

### LockPalette — `Assets/Data/Config/LockPalette.asset`
**Next level.** Locks[*n*] = lock id *n*, each with *Name* (label only), *Mid* (the box's coloured middle with the key slot) and *Key* sprites. 0 Green · 1 Purple · 2 Yellow · 3 Pink · 4 Blue. The levels use 0–2.

### SfxConfig — `Assets/Data/Config/SfxConfig.asset`
| Field | Value | Unit | What it controls | When |
|---|---|---|---|---|
| Sfx Volume | 0.7 | 0–1 | Master volume for every sound | Live |
| Random Pitch Range | 0.03 | ± fraction (0.03 = ±3 %) | Pitch variety for clips with Random Pitch | Live |
| Voices | 10 | sounds at once | Oldest sound cut when all are busy | Restart Play |
| Land Semitones | 0, 1, 2, 3.5, 4.5, 6, 7 | semitones | Landing pitch by slot (bottom first) | Live |
| Chain Semitones · Chain Semitone Cap | 1 · 12 | semitones | Completion chime rising with each tube completed | Live |
| 22 sound slots, each with Clip · Volume · Random Pitch · Start Offset | clip · 1 · on/off · 0 or 0.05 | — · 0–1 · — · s | One slot per sound | Live |

The slots, grouped as in the Inspector:
- **Gameplay**: Ball Select, Ball Deselect, Ball Land, Move Invalid, Tube Complete Chime, Tube Cork.
- **Mechanics**: Hidden Reveal, Towel Pull, Lockbox Key Fly, Lockbox Key In, Lockbox Break, Ice Break.
- **Boosters**: Booster Undo, Booster Shuffle, Booster Add Tube.
- **Win and rewards**: Win Fanfare, Reward Jingle, Win Title Stamp, Coin Gain.
- **UI**: Ui Button, Ui Play, Life Lost.

The .mp3 slots use Random Pitch on and Start Offset 0.05 s, which skips the encoder's silence. The .wav slots use 0, except Ball Deselect, which also has Random Pitch on. Clips are in `Assets/Audio/SFX/`.

### EpisodeConfig — `Assets/Data/Config/EpisodeConfig.asset`
Each Episodes[*n*] entry has First Level (a level number), Home Background, Level Background and Mascot Frame (sprites).

| # | First Level | Home bg | Level bg · mascot |
|---|---|---|---|
| 1 | 1 | Ep2 Summer (stand-in) | Ep1 Lab |
| 2 | 20 | Ep2 Summer | Ep2 Summer |
| 3 | 40 | Ep2 Summer (stand-in) | Ep3 Ice |
| 4 | 60 | Ep2 Summer (stand-in) | Ep4 Lava |
| 5 | 80 | Ep5 Space | Ep5 Space |

Sprites apply on the **next scene load**. First Level changes need **Restart Play**.

### TutorialConfig — `Assets/Data/Config/TutorialConfig.asset`
**Next level.** Each Entries[*n*] has Level Id, Text (TMP rich text: `<color=#FFD23A>` highlights a word, `<size=75%>` makes the hint line smaller) and Image (optional, none today).

| Level | Text (first line) |
|---|---|
| 1 | Tap a tube, then tap the target. Put same-color monsters together! |
| 7 | Move the top monster to **REVEAL** the one below! |
| 20 | Gather one color in the **BIG TUBE**! |
| 30 | Complete the badge color to lift the **TOWEL**! |
| 55 | Fill the tube to break the **ICE**! |
| 80 | Find the **KEY** to open the **BOX**! |

### AvatarConfig — `Assets/Data/Config/AvatarConfig.asset`
**Next time the screen shows.**
- **Pictures[0–4]**: avatars 1–5.
- **Frame**: the frame around an avatar.
- **Selected Frame**: the frame of the picked avatar in the avatar menu.

### LeaderboardConfig — `Assets/Data/Config/LeaderboardConfig.asset`
Bots[*n*] has a Name and an Avatar Id (1–5). These are the ranks 2–11 under the player: Bubbles, Captain Fizz, Luna Star, Pip Pepper, Mochi, Ziggy Zap, Sunny Sprout, Professor Plum, Nova, Tilly Tumble. Name and avatar changes apply each time the page shows. Adding or removing a bot applies on the **next Home**.

### LevelDatabase — `Assets/Data/LevelDatabase.asset`
Levels[0–99] = `level_001` … `level_100` (JSON files in `Assets/Data/Levels/`). Slot *n* must hold `level_n`. Leave a slot empty rather than shifting the others. Edits to a level's JSON apply on the **next level** load. A different level count needs **Restart Play**. Never put test files here.

---

## 4. Quick recipes

**Start from scratch**
1. Leave Play mode. Run **Ball Sort ▸ Reset Save**.
2. Open `Home.unity` and press Play. You are on level 1 with 1000 coins, 5 lives and 99 of each booster. The level 1 tutorial shows.

**Jump to level N**
- *Just to look or test (records nothing)*: **Play Level…** → Level N, pick a variant → **Play level N**.
- *As real progress* (rewards, lives, Episode Unlocked): **Player Data...** → Current level N → **Jump** → press Play in Home. This also resets retries, so the level starts at V1.

**Get stuck on purpose**
1. **Play Level…** → Level file `stuck_after_move` → Play.
2. Move the 3rd tube's top ball onto the 4th tube. After a moment the stuck bar (Give up / Play on) replaces the boosters.

For Play on with a locked tube, use `stuck_lock` V1: move the 2nd tube to the 5th, then the 1st to the 2nd.

**Test buying with and without coins**
1. In Play mode, open **Player Data...** → **Reload** → set Undo to 0 and Coins to 0 → **Apply**.
2. In a level, tap Undo (it shows a green plus) → **Buy** → toast "Not enough coins". The popup stays open.
3. Set Coins to 1000 → Apply → tap **Buy** again → 1 Undo is added, the badge punches, and you have 700 coins left.

Other purchases:
- *Hearts*: set Lives below 5. The plus on Home's lives pill opens **Refill hearts** (900).
- *Play on*: get stuck (above). With ≥ 900 coins the button works; with less it is disabled.
- *Free coins*: Home ▸ Shop tab ▸ any bundle.

**Test 0 lives and the lives timer**
1. In Play mode on Home: **Player Data...** → **Reload** → Lives 0, Next life in 0.1 (6 seconds) → **Apply**.
2. Home's lives pill counts down. Tapping the Level button opens **More Lives** with the timer. After 6 s you have 1 life.
3. *Restart at 0 lives*: in a **real run** (not a test visit), set Lives 0 → Settings ▸ Restart → More Lives opens instead.
4. *A real-length wait*: set Life Refill Minutes in EconomyConfig (Restart Play), and set it back to 30 afterwards.

**Trigger Episode Unlocked**
1. **Player Data...** → Current level **19** → **Jump**.
2. Press Play in Home → Level → win level 19 → **Continue**.
3. About 0.7 s after Home appears, the "Episode 2 unlocked" overlay shows. Tap to close.

Levels 39, 59 and 79 unlock episodes 3–5. This moves your real progress to 20, so Jump back afterwards if you need to.

**Replay a tutorial**
- *Quickest*: **Play Level…** → level 1, 7, 20, 30, 55 or 80. The tutorial shows on every test visit.
- *Real run*: **Player Data...** → **Clear tutorials seen** (and Jump to the level).

**Slow all animations down**
1. Select `AnimationConfig` → **Speed** 0.25 (four times slower). It works in Play mode at once.
2. Set it back to **1** when done: the change stays in the asset.

Speed covers the board and the Win panel, not the UI punches or settings switches. Changing `Time.timeScale` won't slow the board.

**Turn sound off**
- *In the game*: the gear button ▸ **Sound** switch. It is saved with the player and comes back on after Reset Save.
- *Editor only*: the **Mute Audio** button in the Game view toolbar.
- Don't set SfxConfig's volume to 0, because it is kept in the asset. Music does nothing (there is no music yet). Haptics doesn't vibrate yet.

**Test layout on different phones (Device Simulator)**
1. On the Game view tab, open the **Game** dropdown (top left) → **Simulator**.
2. Pick a device at the top. Check a 16:9 (iPhone SE), a 19.5:9 with a notch (recent iPhone), a tall 20:9 Android and a 3:4 iPad.
3. Turn on **Safe Area** to see the notch and home-bar insets.

Mouse clicks act as taps. The board refits on its own. Look for text that overflows, buttons under the notch, and the bottom bar.

---

## 5. Gotchas

- **Don't play while Claude Code is driving Play mode.** It starts, pauses and steps Play mode, switches levels and edits the save through Unity MCP. Two drivers collide. Wait for it to finish, or ask it to stop first. Its checks can also spend boosters or coins from the same save.
- **Stopping Play mid-level counts as quitting.** In a real run, the save keeps a "level in progress" mark, and the next Play costs **1 life and 1 retry** (and moves you to the next variant). Test visits don't have this problem.
  - *To avoid it*: leave through Settings ▸ Home, or win, before you stop.
  - *To undo it before the next Play*: in Edit mode, **Player Data...** → Jump to the same level. This clears the mark (and the retries).
- **Config edits in Play mode stay** (section 3). They appear in git, so revert anything you only tried out. Scene and prefab edits made in Play mode are lost as usual.
- **"Scene has been modified" prompts**: the Level and Home scenes often get marked changed just from playing (the layout moves things). Choose **Don't Save** unless you changed the scene on purpose.
- **Gizmos icon over the game** (e.g. the light bulb of the *Global Light 2D*): click **Gizmos** in the Game view toolbar to turn it off. Its ▾ menu can hide single icons. The Scene view has its own Gizmos button.
- **Player Data in Play mode shows stale values.** Press **Reload** before you edit, or **Apply** writes old numbers back over coins or boosters earned since.
- **Test visits spend real boosters and coins** (section 2). Top them up with Player Data.
- **The lives timer uses the PC clock.** Moving the clock forward gives lives. Moving it back restarts the wait.
- **Test levels marked as tutorials** (`veil`, `lock_and_key`, `clay_large_tube`) show no popup and log a warning ("…has no entry"). This is expected: TutorialConfig only has entries for the real levels.
- **Keep one LevelDatabase.** Play Level and Validate Levels pick the first one they find.
- **Save data lives in PlayerPrefs**, key `BallSort.Save`. On Windows that is the registry, under `HKEY_CURRENT_USER\Software\Unity\UnityEditor\DefaultCompany\Ball Sort Game - GJG`. Each teammate's machine has its own save, separate from any phone build. Ways to wipe it:
  1. **Ball Sort ▸ Reset Save** (Edit mode). Best.
  2. **Edit ▸ Clear All PlayerPrefs**. This also clears any other PlayerPrefs of this project.
  3. Delete the `BallSort.Save…` value in `regedit`.

  A broken or unreadable save is replaced by a new player, with a warning in the Console.
