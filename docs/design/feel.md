# Feel

Motion, effects, sound and haptics for step 6. All of it is view-only: Core state changes are instant, cosmetic animation never blocks input, and a new tap finishes or kills the running tween. Every value is a config field: `AnimationConfig`, or one feedback config row per event (duration, ease, scale/offset, VFX prefab, SFX clips, pitch variance, haptic type). Product's full table: `docs/internal/FEEDBACK_SPEC.md` in the product repo.

## Starting values
- Core moves take 150–350 ms. The balls of one move fly one by one, 80 ms apart (`chainDelayMs`); it is still one move and one undo step.
- Tube complete: a hit-stop, then the cork drops, sparkles and a rising chime (+1 semitone for each completion in a row).
- Invalid move: the target shakes ±8 px for 250–300 ms, with a soft "bonk".
- Landing sounds are pitched on a pentatonic scale.

> **Open issue:** the hit-stop is 50 ms here and 60 ms in the chosen look below.

## Chosen look
The team picked these in the prototype (style preset "İdil"); each item there is a toggle or a value.
- **Flight:** an arc ("hop") from tube to tube, ball by ball with an 80 ms stagger, speed ×1, squash and stretch 0.7 on take-off and landing, a short motion trail.
- **Selection:** the run lifts above the glass; the creatures' eyes follow the finger or the selected run.
- **Tube complete:** a wooden cork tinted in the tube's colour drops in, sparkles and a shockwave, a 60 ms hit-stop, a small camera punch, a glow on the tube, and its creatures dance. Each completion in a row plays higher. Not used: green tick, glass-shard burst, sleeping creatures.
- **Win:** confetti and fireworks (sequence below).
- **Level intro:** tubes drop in with a ~40 ms stagger and balls pop in, 800 ms at most in total.

> **Open issue:** product wants input live "as soon as the first tube lands". The dev rule is that cosmetic animation never blocks input, so input should be live from the start.

## Mechanic effects
- Reveal: a quick flip of the "?" creature.
- Veil: the towel lifts straight off.
- Lock: the key flies from its ball to the padlock, the padlock pops, the crate breaks open.
- Ice: on completion only, it cracks for ~0.15 s, then bursts into shards with a shatter sound.

## Win sequence
Every step is tap-to-skip, and Home pre-loads during it. Mockups: `41_win_logo.jpg`, `42_…`, `43_…super_hard`.
1. About 1.0 s after the last cork, the creatures jump in a wave.
2. The logo pops in with fireworks, holds for 2.6 s (`winLogoSeconds`) and flies out.
3. The win panel:
   - a title tab "Level N", plus "HARD" / "SUPER HARD" (the card is red on Super Hard);
   - a title with letters popping in one by one (graded titles: meta-ideas.md → Win grades);
   - "Rewards:", then a coin pile on a pedestal with rays and the amount (+20 / +60 / +100);
   - **Continue**, and a close X that does the same.
4. Continue → Home: the coins fly into the coins pill and the counter counts up (0.8 s, `coinCountUpSeconds`). The Level button already shows the next level.
5. The Episode Unlocked overlay if the next level opens an episode (meta-ideas.md).

> **Open issue:** the logo reads "LAB SORT", but the game's name isn't decided (the project is "Ball Sort Game - GJG").

## Sound
Starting map, from `audio/sfx_library/` in the product repo. The `ms_` files are reference recordings: they show the intended sound but **must not ship**, so never import them into `Assets/`. Events that still need a shippable clip: reveal, unlock, win, reward, coin, button, perfect.

| Event | Clip |
|---|---|
| Select | `bs_select_01` |
| Land | `bs_land_01` |
| Complete | `bs_complete_01` |
| Reveal | `ms_reveal_01` (reference) |
| Unlock (lock opens) | `ms_special_01` (reference) |
| Win | `ms_win_fanfare` (reference) |
| Reward | `ms_coin_jingle` (reference) |
| Coin | `ms_coin_amount` (reference) |
| Button | `ms_tap_02` (reference) |
| Perfect (needs win grades) | `ms_perfect_stamp` (reference) |

The Sound toggle mutes these. Product says the game has no music (meta-ideas.md → Screen changes).

## Haptics
- Light: select, lift, reveal, button, coins.
- Medium: land.
- Success: tube complete, win.
- Warning: invalid move.
- None during ball flight. At least 50 ms between two haptics. The Haptics toggle mutes them.

> **Note:** Unity itself only offers `Handheld.Vibrate`. Typed haptics need a native plugin, which is a new dependency and a team decision.
