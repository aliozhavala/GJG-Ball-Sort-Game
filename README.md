# Ball Sort

A mobile ball-sort puzzle with 100 levels: move balls between tubes until every tube holds a single colour.
Made for the Good Job Games case study by a team of three (developer, product, art).

## How to play

- Open the project in **Unity 6000.2.6**.
- Open `Assets/Scenes/Home.unity` and press **Play**.
- Use a portrait view: the Device Simulator, or a portrait Game view resolution (e.g. 1080×1920).

## Features

- **Mechanics:** hidden balls, veil, ice, lock and key, large tubes (each introduced with a tutorial).
- **Boosters:** Undo, Shuffle (one tube you pick) and Add Tube, bought with coins when you run out.
- **Lives:** 5 at most, one back every 30 minutes; leaving a level unfinished costs one.
- **Tiers:** regular, hard and super hard levels, with bigger coin rewards.
- **Episodes:** 5 themed episodes, each with its own backgrounds and mascot.
- **Variants:** every level has 5 difficulty variants. Your retry count picks the one you play, so a retry is never the same puzzle.

## Architecture

- **Pure C# Core:** rules, level session, player data and solver, with no Unity dependency (enforced by its assembly definition), covered by 1,700+ EditMode tests.
- **Unity layer on top:** views, input, UI and audio react to Core events and never change the board directly.
- **Patterns:**
  - Command, for moves and undo
  - Observer, through plain C# events
  - state machine, for the level flow
  - object pooling, for balls and VFX
  - a single composition root (`AppRoot`)
  - ScriptableObject configs, for art, timings, economy and episodes
- **Every level verified:** the built-in solver validates and solves all 500 level variants, and a test plays each solution to a win.

## Editor tools (Ball Sort menu)

- **Play Level…:** play any level and variant, or a test level file, without touching progress.
- **Validate Levels…:** check and solve every variant, and report optimal move counts.
- **Player Data…:** edit coins, lives, boosters and progress, including while in Play mode.
- **Art ▸ Prepare Sprites:** move new art into place, trim it and apply mobile import settings.

## Known limitations

- Shop purchases are simulated: no real payment, coins are added immediately.
- The leaderboard is simulated, with fixed bots.
- Haptics are stubbed: the calls are in place, but nothing vibrates yet.
- Lives refill from the device clock, so changing the clock affects them.
