# Potion Sort — SFX Guide

> **[+P] Product document.** Every sound the game plays in the team's chosen style (**"İdil"** in the prototype), with the file to use, the exact moment it plays, how it lines up with the animation, and the haptic that goes with it.
> Source of truth: the prototype `prototype/potion-sort-lab.html` (İdil style, saved version 15, 29 Oct 2026). Animation values: `animations.md` in the hand-off folder. Every time below is in seconds at `speed` 1 (divide by `speed` like the animations).

## 1. What is in the pack

22 files in 5 folders. The file name is the event; use it as the clip id in `FeedbackConfig`.

| File | Event | Length | Ch |
|---|---|---|---|
| `gameplay/sfx_ball_select.mp3` | A ball is lifted | 0.20 s | mono |
| `gameplay/sfx_ball_deselect.wav` | The lifted ball goes back | 0.30 s | mono |
| `gameplay/sfx_ball_land.mp3` | Each ball lands in its slot | 0.52 s | mono |
| `gameplay/sfx_move_invalid.wav` | Move not allowed / "no" | 0.35 s | mono |
| `gameplay/sfx_tube_complete_chime.mp3` | A tube is completed | 0.70 s | mono |
| `gameplay/sfx_tube_cork.mp3` | The cork seats on a completed tube | 0.47 s | mono |
| `mechanics/sfx_hidden_reveal.mp3` | A "?" ball shows its colour | 1.50 s | stereo |
| `mechanics/sfx_towel_pull.mp3` | A towel comes off a tube | 0.80 s | stereo |
| `mechanics/sfx_lockbox_key_fly.mp3` | A key pops out and flies to the padlock | 0.59 s | stereo |
| `mechanics/sfx_lockbox_key_in.mp3` | The key enters the padlock (thump + 2 clicks) | 0.59 s | stereo |
| `mechanics/sfx_lockbox_break.mp3` | The lock box bursts open | 1.43 s | stereo |
| `mechanics/sfx_ice_break.wav` | The ice block on a tube shatters | 0.50 s | mono |
| `boosters/sfx_booster_undo.wav` | Undo used | 0.30 s | mono |
| `boosters/sfx_booster_shuffle.wav` | Shuffle used | 0.40 s | mono |
| `boosters/sfx_booster_add_tube.wav` | Add tube used / Play on | 0.45 s | mono |
| `win_rewards/sfx_win_fanfare.mp3` | Win logo + fireworks; Episode unlocked | 3.70 s | mono |
| `win_rewards/sfx_reward_jingle.mp3` | Reward card appears; lives / boosters received | 0.50 s | mono |
| `win_rewards/sfx_win_title_stamp.mp3` | Win title ("PERFECT!" / "AWESOME!" / "GOOD JOB!") lands | 0.42 s | mono |
| `win_rewards/sfx_coin_gain.mp3` | Coins are added (reward amount, coin purchase) | 0.92 s | mono |
| `ui/sfx_ui_button.mp3` | Any button / popup open / popup close | 0.13 s | mono |
| `ui/sfx_ui_play.mp3` | Home **Play** button | 0.80 s | stereo |
| `ui/sfx_life_lost.wav` | A life is spent (heart breaks) | 1.10 s | mono |

All files are 44.1 kHz. The `.wav` files are **quieter than the mp3s on purpose** (peaks −9 to −21 dBFS): that is the balance heard in the prototype, so do not normalise them.

## 2. Global rules

| Rule | Value |
|---|---|
| Master SFX volume | **0.7**. Every clip plays at 1.0 into a master of 0.7. Put it in `FeedbackConfig.sfxVolume`. |
| Random pitch | Every **mp3 clip** and `sfx_ball_deselect` plays at a random pitch **0.97–1.03** (±3%) each time. The other synth WAVs play at 1.0. |
| Pitch = speed | Pitch is a playback-rate change: a higher pitch also plays shorter, like `AudioSource.pitch` in Unity. |
| Land pitch | Rises with the slot the ball lands in (pentatonic), see §3.3. |
| Complete pitch | +1 semitone per tube already completed in this level (chain), see §3.5. |
| Overlap | No voice limit and no cut-off: each play is a new one-shot (`PlayOneShot` or a small `AudioSource` pool). Land sounds of a run overlap. |
| Sound toggle | Settings → **Sound** off mutes every SFX. **Haptics** is a separate toggle. |
| No music | The game has no music track. |
| Input | A sound never blocks input. Every sound is fire-and-forget. |

**Haptics used below** (iOS names; Android: closest `VibrationEffect`). At least 50 ms between two **land** haptics.

| Key | iOS | Prototype pattern (ms) |
|---|---|---|
| Selection | `UISelectionFeedbackGenerator` | 8 |
| Light | Impact · Light | 6 (button) / 10 (reveal) |
| Medium | Impact · Medium | 14 |
| Warning | Notification · Warning | 18-40-18 |
| Success | Notification · Success | 25-40-50 |
| Heavy | Impact · Heavy → Success | 60-30-25 |
| Win | Success + custom AHAP | 30-50-30-50-90 |

## 3. Core moves

### 3.1 Select — `sfx_ball_select`
- **When:** the player taps a tube that can be picked (not empty, not completed, not under a towel, lock box or ice, not a big bottle).
- **Timing:** at the tap, the same frame the top ball starts rising (`animations.md` §2.2).
- **Haptic:** Selection.

### 3.2 Deselect — `sfx_ball_deselect`
- **When:** a ball is lifted and the player taps the same tube again, or taps empty space, or makes an invalid move (it plays together with `sfx_move_invalid`), or the board becomes stuck while a ball is lifted.
- **Timing:** at the tap, as the ball starts to drop back. **Haptic:** none.

### 3.3 Land — `sfx_ball_land`
- **When:** each moving ball reaches its slot in the target tube: the end of the flight's **drop** segment, the same frame as the landing squash, hop bounce and droplets (`animations.md` §2.4).
- **One sound per ball.** A run of 3 plays 3 lands, **80 ms apart** (the chain stagger).
- **Timing after the tap:** rise 0.10 + arc (0.15 + 0.23 × D / SW) + drop 0.12, so the first ball lands **~0.40 s** after the tap next door and **~0.6 s** across the board; ball *j* lands 80 ms × *j* later.
- **Pitch by slot** (slot 0 = bottom of the tube), on top of the ±3% random:

  | Slot the ball lands in | 0 | 1 | 2 | 3 | 4 | 5 | 6 and up |
  |---|---|---|---|---|---|---|---|
  | Pitch | 1.000 | 1.059 | 1.122 | 1.223 | 1.297 | 1.414 | 1.498 |
  | Semitones | 0 | +1 | +2 | +3.5 | +4.5 | +6 | +7 |

  Formula: `pitch = 2^(PENT[min(slot, 6)] / 24)`, `PENT = [0, 2, 4, 7, 9, 12, 14]`. A tube filling up plays a rising scale.
- **Haptic:** Medium (skipped if the last land haptic was less than 50 ms ago).

### 3.4 Invalid — `sfx_move_invalid`
Plays in all of these cases, at the moment it happens:

| Case | Same frame | Haptic |
|---|---|---|
| Tap on a tube that can't be picked (towel, lock box, ice, completed, empty, big bottle) | Tube shake 0.4 s + shock faces 0.7 s (`animations.md` §2.5) | Warning |
| A move to a tube that can't take it (full, wrong colour on top) | Deselect sound too; the **target** tube shakes | Warning |
| The board gets stuck: "No available moves" / "No moves left" appears | Text fades in, booster bar becomes Give up / Play on | Warning |
| Shuffle can't find a valid shuffle ("Can't shuffle right now" toast) | Toast | none |
| "Not enough coins": buying a booster, refilling lives, buying a shop item, Play on | Toast | none |

**Stuck timing:** the stuck check runs 0.3 s after the last ball lands (0.9 s if that move completed a tube), and the message and sound come **0.4 s after that**: so ~0.7 s after the last land (~1.3 s after a completion).

### 3.5 Tube complete — `sfx_tube_complete_chime` + `sfx_tube_cork`
The completion has **two sounds 0.16 s apart**:

```
t = 0       last ball of the move lands in the now-complete tube
            → sfx_ball_land (that ball) + sfx_tube_complete_chime
            → sparkles + shockwave ring, confetti, glow, creature dance start
t = 0.16 s  cork has dropped into the neck and seats
            → sfx_tube_cork  + haptic Success + hit-stop 60 ms + camera punch + cork squash
```

- **Chain pitch (chime only):** `pitch = 2^(chain / 12)` × the ±3% random. `chain` = number of tubes already completed in this level before this one (first completion 0, second +1 semitone, …). Reset on level load; Undo restores the previous value. The prototype has no cap; our levels have at most ~9 colours, so it stays under +9 semitones. A cap at +12 is a safe guard.
- The cork plays at the normal ±3% pitch with no chain.
- With the **ice** mechanic the ice sound plays at the cork's moment (§4.4).

## 4. Mechanics

### 4.1 Hidden reveal — `sfx_hidden_reveal`
- **When:** a "?" ball becomes the top ball because the balls above it left.
- **Timing:** **0.14 s after the tap** (the moving ball has left), the same frame the card flip starts (0.32 s flip, the face swaps at 0.16 s) and the lilac smoke puff (`animations.md` §3.1).
- **Haptic:** Light.
- A "?" that ends up on top after **Shuffle** flips without a sound.

### 4.2 Towel — `sfx_towel_pull`
- **When:** a tube of the towel's badge colour completes.
- **Timing:** **0.45 s after the last ball lands** (after the chime and cork), the same frame the towel starts to lift (0.6 s) with the pink smoke and gold sparks (`animations.md` §3.2). One sound even if one completion opens several towels (they lift 80 ms apart).
- The pull itself hits ~80–230 ms into the file, so the audible "whoosh" sits in the first third of the lift.
- **Haptic:** Heavy.

### 4.3 Lock box — `sfx_lockbox_key_fly` → `sfx_lockbox_key_in` → `sfx_lockbox_break`

```
t = 0       the last ball of the move that freed the key ball lands
            (+0.25 s if that move also completed a tube, so the chime reads first)
            → sfx_lockbox_key_fly, the key starts its 0.6 s flight (InOutCubic) to the padlock
t = 0.60 s  key arrives in the padlock
            → sfx_lockbox_key_in (thump, then two clicks at ~0.40 s into the file) + haptic Medium
            → crate wobble + padlock jiggle start
t = 1.15 s  (key arrival + 0.55 s) the box bursts
            → sfx_lockbox_break + haptic Heavy + wood splinters + ring; crate scales to 115% and fades (0.35 s)
```

- **One sound per box**: a box that spans several bottles plays each sound once.
- If the level uses the "complete a colour" lock rule instead of key balls, the key starts **0.5 s** after the last land, from the completed tube; the rest is the same.
- **Correction to `animations.md` §3.3:** the box now bursts **0.55 s** after the key arrives (not 0.25 s), to follow the two lock clicks in the sound. The animation file has been updated.

### 4.4 Ice — `sfx_ice_break`
- **When:** an ice tube is completed (an unfinished ice tube keeps its ice).
- **Timing:** **0.16 s after the last ball lands**: the end of the crack animation, the frame the block bursts into 22 shards. That is the same moment as the cork sound, so cork + ice play together.
- **Haptic:** Heavy.

### 4.5 Big bottle
No sound of its own. Moves, landing, completion and ice use the normal sounds.

### 4.6 New item card (first Ice and first Lock box level) — `sfx_lockbox_key_fly`
- **When:** the "New Item Unlocked" card opens, 0.7 s after the level loads (only for Ice and Lock box; Hidden, Big bottle and Towel use a toast with no sound).
- **Haptic:** Heavy. Closing it (tap): `sfx_ui_button` + Light.

## 5. Boosters

Pressing a booster button always plays **`sfx_ui_button` + haptic Light first**, in the same frame as the booster's own sound below.

| Booster | Sound | When | Animation it goes with | Haptic |
|---|---|---|---|---|
| Undo | `sfx_booster_undo` | The undo is applied | Balls fly back at 1.7× speed, all at once. **No land sounds** for these flights. | Light |
| Shuffle | `sfx_booster_shuffle` | The shuffle is applied | Balls fly to new slots at 1.7×, staggered 30 ms × (tube index mod 6). **No land sounds.** | Light |
| Add tube | `sfx_booster_add_tube` | The tube is added | New empty tube drops in from 200 px above | Light |
| Play on (stuck bar) | `sfx_booster_add_tube` | Coins paid, tube added | Same as Add tube | Light |

- Undo with nothing to undo (or after a cork): toast only, no booster sound (the button sound still plays).
- Shuffle that fails: `sfx_move_invalid` + toast, nothing is spent.
- Booster count 0: the press opens the buy popup (`sfx_ui_button`). Buying: `sfx_reward_jingle` + Success; not enough coins: `sfx_move_invalid`.

## 6. Win and rewards

### 6.1 Level win timeline

```
t = 0       last ball lands, last tube completes → land + complete chime (§3.5)
t = 0.16 s  last cork seats → sfx_tube_cork; board celebration (confetti, tube bumps, creature hops) has NO sound of its own
t = 0.55 s  win overlay + logo pop-in → sfx_win_fanfare + haptic Win
            fireworks have no sound: their big bursts are timed to the fanfare's beats,
            at 1.75, 1.95, 2.09, 2.61, 2.75, 2.89 s into the clip
t = 0.55 + 3.04 s   logo shrinks and fades (0.3 s)
t = 0.55 + 3.34 s   reward card pops in → sfx_reward_jingle + haptic Success      (= card time C)
C + 0.70 s  title has popped in → sfx_win_title_stamp
            (0.18 s + 65 ms × letters: "PERFECT!" / "AWESOME!" 0.70 s, "GOOD JOB!" 0.765 s)
C + 0.95 s  reward amount shows → sfx_coin_gain + haptic Light
Continue    → sfx_ui_button + haptic Light only; the Home coin counter counts up silently (0.9 s)
```

- **logoDur = 3.34 s** comes from the fanfare: last beat (2.89 s) + 0.45 s, kept between 2.6 and 3.6 s. If the fanfare is replaced, recompute it from the new clip's beats (put the beat times on the clip's config row).
- The stamp plays for every title, not only "PERFECT!".
- Each step is tap-to-skip: when a step is skipped, start its sound at the skip and drop the sounds of steps already passed.

### 6.2 Episode unlocked (levels 20, 40, 60, 80)
- 0.7 s after arriving on Home from Continue: overlay opens → `sfx_win_fanfare` + haptic Win; **0.6 s later** `sfx_reward_jingle`. Fireworks for 3.2 s, silent.
- Tap to continue: `sfx_ui_button`.

### 6.3 Coins, lives and boosters received

| Moment | Sound | Haptic |
|---|---|---|
| Shop coin pack bought (mock purchase) | `sfx_ui_button` on the press; `sfx_coin_gain` when the purchase completes (0.8 s later) and the counter counts up | Light on the press |
| Shop bundle or offer that gives coins | `sfx_coin_gain` | — |
| Shop bundle that gives lives | `sfx_reward_jingle` | — |
| Lives refilled with coins (More Lives popup) | `sfx_reward_jingle` | Success |
| "Watch ad" life, at the end of the 3 s mock ad | `sfx_reward_jingle` | — |
| Booster bought with coins | `sfx_reward_jingle` | Success |

### 6.4 Lose a life — `sfx_life_lost`
- **When:** the player confirms the **Are You Sure** popup (Restart, Quit/Home, or Give up), so a life is spent.
- **Timing:** at the confirm tap, the same frame the heart starts to break (0.45 s). The reload or the trip Home follows ~0.75 s later.
- **Haptic:** Warning.
- Opening the popup and closing it with ✕: `sfx_ui_button`. Leaving a level with 0 moves made is free: no popup, `sfx_ui_button` only.

## 7. Buttons and UI

| Button / action | Sound | Haptic |
|---|---|---|
| Home **Play** (lives > 0) | `sfx_ui_play` | Light |
| Home Play with 0 lives (opens More Lives) | `sfx_ui_button` | Light |
| Settings gear (level or Home) | `sfx_ui_button` | Light |
| Sound / Haptics toggles | `sfx_ui_button` | Light |
| Any popup ✕ / Close (settings, coins, booster buy, lives, profile, shop) | `sfx_ui_button` | — |
| Bottom nav: Shop / Home / Leaderboard | `sfx_ui_button` | Light |
| Coin "+" (opens the coin popup), lives pill (opens More Lives) | `sfx_ui_button` | Light on More Lives |
| Avatar → Edit Profile; avatar / frame picks; Save | `sfx_ui_button` | Light |
| Profile and leaderboard tabs, edit name | `sfx_ui_button` | — |
| Settings → Restart / Home (with moves made) | opens Are You Sure → `sfx_ui_button` | Light |
| Settings → Restart / Home (no moves yet) | `sfx_ui_button` | — |
| Win → Continue | `sfx_ui_button` | Light |
| Booster buttons | `sfx_ui_button` + the booster sound (§5) | Light |
| Locked frame, placeholder links | `sfx_ui_button` (only on placeholder links) + toast | — |

Rule of thumb: **every button press plays `sfx_ui_button`**, except Home Play (`sfx_ui_play`) and the buttons whose result has its own sound (lose-life confirm, shop purchases with coins: their result sound replaces the button sound).

## 8. Things that are silent on purpose
- Ball flight (only the landing makes a sound) and the level intro (tubes dropping in).
- Board celebration on win (only the last cork is heard before the fanfare).
- Fireworks (the fanfare carries the bangs).
- Coin count-up on Home after Continue.
- Undo / Shuffle flights (no land sounds) and the Shuffle reveal flip.
- Toasts, blinking creatures, idle loops, the breathing Play button.

## 9. Unity notes
- One `FeedbackConfig` row per event: clip(s), volume, pitch rule (`random ±3%`, `landSlot`, `chain`), delay, haptic. All the delays above go on the row, not in code.
- Short clips (everything except the fanfare and the reveal): Load Type **Decompress On Load**, ADPCM or PCM. Fanfare and reveal: **Compressed In Memory**. Tick **Preload Audio Data**.
- mp3 files can gain a few ms of leading silence after import; check that `sfx_tube_cork` (hits in its first 10 ms) and `sfx_ui_button` start on their first sound, and trim if not.
- Use one 2D `AudioSource` pool (8–12 voices is plenty) so several lands, the chime and the cork can overlap.
- Pitch through `AudioSource.pitch`, which also changes the length, as in the prototype.
