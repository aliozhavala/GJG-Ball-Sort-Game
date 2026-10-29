# Animations — Potion Sort

> **[+P] Product document.** Every animation in the game: when it plays, what the player sees, what it is for, and the values to start from.
> The values come from the prototype's **"İdil" style** (`prototype/potion-sort-lab.html`, Setup tab; saved style version 10, 29 Oct 2026). That is the look the team picked. To see any of it live, open the prototype, pick the İdil style and play a level. The Feel and Complete tabs have ▶ test buttons.
> Sound and haptics are not in this file. They are in `screens.md` → Feedback pass.

## 0. Rules

1. **Logic first, animation after.** Core applies a move completely and instantly (unlocks, reveals, towel lifts and ice breaks included). Animations only show what has already happened.
2. **Input never waits for an animation.** A new tap finishes or kills the running tween on the balls it touches. The one exception is the win sequence: each step is tap-to-skip.
3. **One speed knob.** Every duration below is divided by `speed` (default 1). Changing `speed` makes the whole game faster or slower.
4. **Every number is a config field.** Put them in `AnimationConfig` (one row per event, see §7), so product can tune them in the Inspector without code changes.
5. **Hit-stop slows only the board.** Use a separate board time scale, not `Time.timeScale`, so UI, input and timers keep running.

**Units used below**

| Unit | Meaning |
|---|---|
| **d** | Ball diameter on the current board (the board scales per level, so animations scale with it). |
| **SW / SH** | Screen width / screen height. |
| **px** | Pixels on the prototype's 390 × 844 reference screen (× ~2.77 for the 1080 × 1920 Canvas Scaler reference). |
| **s** | Seconds at `speed` 1. |
| Ease names | DOTween / PrimeTween names (OutQuad, InOutSine, OutBounce…). |

## 1. Overview

| # | Animation | When | What it is for | Length |
|---|---|---|---|---|
| 2.1 | Level intro | Level loads | Builds the board in a lively way and shows the layout | ≤ 0.9 s |
| 2.2 | Select / deselect | Tap on a tube | Shows which ball will move | ~0.12 s |
| 2.3 | Ball flight | A valid move | Shows where each ball goes; the run follows ball by ball | 0.4–0.6 s per ball |
| 2.4 | Landing | Each ball arrives | Weight and contact ("it's in") | 0.45–0.55 s |
| 2.5 | Invalid move | Move not allowed | A short "no" without punishing | 0.4 s |
| 2.6 | Tube complete | A tube is full of one colour | The main reward moment of the game | ~1–2 s |
| 3.1 | Hidden reveal | A "?" ball becomes the top ball | Shows the newly learned colour | 0.32 s |
| 3.2 | Towel lifts | The badge colour's tube completes | Opens the covered tube | 0.6 s |
| 3.3 | Lock box opens | A key ball is freed | Key flies to the padlock, the crate breaks | ~1.2 s |
| 3.4 | Ice breaks | An ice tube completes | Frees the frozen tube | ~1.3 s |
| 4.x | Undo / Shuffle / Add tube | Booster used | Shows what the booster changed | 0.2–0.5 s |
| 5.1 | Stuck | No legal move left | Tells the player the board is stuck | 0.25 s |
| 5.2 | Level win | Last tube completes | Celebration + reward | ~4 s, tap-to-skip |
| 5.3 | Lose a life | Restart / Quit confirmed | Makes the cost of a life visible | ~0.75 s |
| 5.4 | Episode unlocked | First visit to a new episode | Marks progress | ~3 s |
| 6.x | Creatures, UI | Always | Gives the board character | loops |

## 2. Core moves

### 2.1 Level intro
- Each tube (with its balls inside) starts above the screen: **0.7 SH + 40 px × tube index** higher than its place.
- Tube *i* waits **45 ms × i**, then falls straight down at **~3 SH per second** (linear).
- On arrival it plays the **tube bump** (§2.4).
- 12 tubes are all in place after ~0.9 s. **Input is live from the first frame.**
- On intro levels the tutorial toast appears 0.6 s after load and stays for 3.6 s.

### 2.2 Select / deselect
- **Select:** only the **top ball** lifts out of the tube; the rest of the run stays and follows during the move. It rises to **0.72 d above the tube's mouth** with an exponential follow (rate 20/s, so ~90% of the way in 0.12 s). Each frame: `pos += (target − pos) × (1 − e^(−20·dt))`.
- **While held** the lifted ball bobs **±0.06 d** (sine, ~1.25 s period).
- **Deselect:** the same follow takes it back to its slot. Deselect happens on a tap on the same tube, on empty space, or before an invalid move.
- The tube itself does not move (the "bottle lifts" option was not chosen).
- Tapping a tube that cannot be selected (towel, lock box, ice, completed or empty) plays **Invalid** on that tube (§2.5).

### 2.3 Ball flight ("hop")
The moving balls are the top ball plus every same-colour ball directly under it (the run). Each ball flies the same path in three segments:

| Segment | From → to | Duration | Ease |
|---|---|---|---|
| 1. Rise | Current position → 0.72 d above the **source** mouth | 0.10 s | OutQuad |
| 2. Arc | → 0.72 d above the **target** mouth | **0.15 s + 0.23 s × (D / SW)** | InOutSine |
| 3. Drop | → its slot in the target | 0.12 s | InQuad |

- **D** = horizontal distance + vertical distance between the two mouths. In practice the arc takes 0.18 s to a neighbouring tube and ~0.35 s across the board.
- **Arc height** = **0.5 d + 0.32 × D**, added as a parabola on top of the straight line: `y −= h · 4k(1 − k)`, where k is the eased progress. DOTween `DOJump` with one jump is close enough.
- **Chain:** the second ball starts **80 ms** after the first, the third 80 ms after that, and so on. The top ball goes first.
- **Stretch:** while flying, the ball stretches along its velocity. Amount = `min(speed / (6.7 SW per s), 0.3) × squash`, with `squash` 0.7, so at most ~21%. No stretch below ~0.15 SW per s.
- **Trail:** during segments 2 and 3 a short trail follows the ball: its last 9 frame positions, in the ball's colour, widening to 1.4 × radius and up to 45% opacity at the ball. The trail is cleared on landing.
- The ball becomes part of the target tube in Core at the start of the move. Only the visual is still travelling.

### 2.4 Landing
Plays on each ball as it reaches its slot:
- **Squash:** a damped wobble for **0.45 s**. `f = e^(−11t) · cos(30t)`, `scaleX = 1 + 0.32 × squash × f`, `scaleY = 1 − 0.32 × squash × f`. With `squash` 0.7 the first frame is about 1.22 × 0.78.
- **Hop bounce** (this is what makes it "hop" rather than "arc"): the landed ball bounces twice in its slot: `y offset = |sin(t·π / 0.275)| × 0.4 d × e^(−4t)` for **0.55 s**. The first bounce is ~0.23 d high, the second ~0.08 d.
- **Balls below bump:** every ball under the new one rises **0.07 d** and back (half sine, 0.2 s). The start is delayed by **30 ms × how far below it is**, so a small ripple runs down the stack.
- **Tube bump:** the whole tube rises 0.07 d and back (half sine, 0.22 s).
- **Droplets (VFX):** 5 small dots in the ball's colour pop out with gravity, ~0.35 s.

### 2.5 Invalid move
- The tapped tube shakes sideways: `x offset = sin(55t) × 0.16 d × e^(−9t)` for **0.4 s** (about 8 quick, fading wobbles).
- Its creatures show the **shock** face for 0.7 s.
- If a ball was lifted, it drops back first (deselect).
- Plays on: the target of an illegal move, or a tube that cannot be selected.

### 2.6 Tube complete
Starts when the **last ball of the move lands** in a tube that is now complete. Everything below starts together unless a delay is given:

| Piece | Values | What it adds |
|---|---|---|
| **Hit-stop** | 60 ms during which the board runs at 6% speed (near-freeze) | The "impact" beat |
| **Camera punch** | Board scale 1 → 1.028 → 1, half sine over 0.3 s | A small zoom kick, softer than screen shake |
| **Cork** | Wooden cork, **tinted in the tube's colour**, drops from 90 px above the mouth into the neck, **0.45 s OutBounce**. It is masked so it appears from above the mouth. It stays. | Shows "this tube is finished" |
| **Glow** | Tube outline glows in the ball colour: full → 50% over 0.6 s, then stays at 50% with a soft ±12% pulse (~1.6 s period) | Marks the finished tube for the rest of the level |
| **Sparkles + shockwave** | **14 + 3 × chain** sparkles burst upward from the mouth (white, tube colour, gold), faster with chain. A ring (2.2 d wide) expands from the tube centre over 0.5 s. | Burst of reward; grows with the chain |
| **Confetti** | 26 confetti pieces shoot up from the mouth and fall with gravity (1.4–2.2 s) | Extra party |
| **Creature dance** | Every creature in the tube does the **hop bounce** (§2.4) with a **70 ms** stagger from bottom to top, and shows the **happy** face for 1.6 s | The characters celebrate |

- **Chain:** a counter of tube completions in the level. It starts at 0 on load, and undo restores the previous value. It scales the sparkle count and speed (and the SFX pitch, see `screens.md`).
- **Not used** in the chosen style: glass-shard burst, green tick, sleeping creatures (zZ).
- A completed tube is final: the cork and glow stay, and the tube is no longer tappable.

## 3. Mechanics

### 3.1 Hidden ball reveal
- Plays **0.14 s after the move starts**, when the ball above the "?" has left.
- The "?" ball flips like a card: horizontal scale `cos(π·u)` over **0.32 s** (1 → 0 → −1, drawn as 1 → 0 → 1). At the midpoint (0.16 s) the "?" face is swapped for the real creature.
- **VFX:** a small lilac smoke puff (10 particles, 0.6 s) at the ball.
- Shuffle reveals a "?" that lands on top with the same flip.

### 3.2 Towel lifts
- Trigger: a tube of the **badge colour** completes. Core opens the towel at once.
- Plays **0.45 s after** the last ball lands, so the completion is seen first.
- Over **0.6 s** (smoothstep): the towel rises **2.6 d**, fades to 0 and tilts slightly in the middle of the motion (up to ~5°). It lifts straight off; it does not fade first.
- **VFX at the start:** a pink smoke puff (12 particles) over the tube and 10 gold sparks at the mouth.
- If one completion opens several towels, each starts **80 ms** after the previous one.
- **Idle:** while closed, the towel's hem sways gently (~4 s period).
- The balls under it are drawn at once with their own hidden flags.

### 3.3 Lock box opens
1. **Key flies.** When a key ball is freed, the key flies from that ball to the padlock in **0.6 s, InOutCubic**, along a curve that rises about 60 px above the higher of the two points (quadratic Bézier, control point 120 px above). If the same move completed a tube, the key waits **0.25 s** so the completion reads first.
2. **The box reacts** when the key arrives:
   - 0–0.25 s: the crate wobbles (±1.7°) and the padlock jiggles sideways.
   - At 0.25 s: wood splinters burst out (16 pieces per bottle, with gravity) and a ring in the padlock colour expands (2.6 d).
   - 0.25–0.6 s: the crate scales to 115% and fades out.
3. All bottles of one box open together, with one sound per box.

Core opened the lock when the move happened; the flight is only for show. The dead-end check never waits for it.

### 3.4 Ice breaks
- Only when the **ice tube completes**. An unfinished ice tube keeps its ice.
- 0–0.16 s: three cracks grow across the ice block.
- At 0.16 s the block disappears and bursts: 22 ice shards fly up and fall with gravity (~1.1 s), with a light smoke puff and a ring (2.4 d).
- It plays together with the normal completion (§2.6).

### 3.5 Big bottle
No animation of its own. Moves, landing and completion are the same as a normal tube, and the ice skin follows §3.4.

## 4. Boosters

| Booster | Animation |
|---|---|
| **Undo** | Each ball that moved flies back to its old slot with the normal flight (§2.3) at **1.7× speed**, with no chain stagger (all at once). Landing effects play as usual. Towels and locks that were closed in the restored state appear closed at once. |
| **Shuffle** | Each ball whose slot changed flies to its new slot at **1.7× speed**. The start is staggered by **30 ms × (tube index mod 6)**, so the board ripples. A "?" that ends on top is revealed (§3.1). |
| **Add tube / Play on** | A new empty tube drops in from 200 px above at the intro speed (§2.1), then does the tube bump. Rows re-layout around it. |
| Booster button | Pressed: moves down 5 px and scales to 97% (0.08 s). |

## 5. Level flow

### 5.1 Stuck
"No available moves" fades in (0.25 s) **0.4 s after the last ball lands**, so the move finishes before the message appears.

### 5.2 Level win
Each step is **tap-to-skip**. Pre-load Home while it plays.

```
t = 0       last ball lands → tube complete (§2.6)
t = 0.9 s   win is presented (Core knew at t = 0)
t = 1.2 s   board celebration:
            · confetti from both bottom corners (40 pieces each)
            · every tube does the tube bump, left to right, 60 ms apart
            · every creature hops (delay grows with its x, up to 0.4 s across the board)
              and shows the happy face for 2 s
t = 1.9 s   win overlay:
            · background dims (0.35 s fade)
            · win logo pops in: scale 0.1 → 1.15 → 1 and rotation −10° → 3° → 0° over 0.7 s
            · fireworks for 2.6 s: 3 big bursts at 0.45–0.55 s, then small ones every 0.13–0.25 s
t = 4.2 s   logo shrinks to 50% and fades out (0.3 s, ease-in)
t = 4.5 s   reward card pops in (scale 0.35 → 1, 0.45 s, overshoot)
            · title ("PERFECT!" / "AWESOME!" / "GOOD JOB!"): letters pop one by one,
              0.3 s each, 65 ms apart, starting at 0.2 s
            · coin pile: coins drop in from 80 px above (0.42 s each, delays 0.34–0.6 s)
            · loops: light rays turn (18 s per turn), glow pulses (1.6 s), sparkles twinkle (1.4 s)
Continue →  Home: the coin counter counts up over coinCountUpSeconds (OutCubic; prototype 0.9 s).
            If the next level opens an episode: Episode Unlocked (§5.4) 0.7 s after arriving.
```

Later idea: with a recorded fanfare, the prototype times the big fireworks to the beats of the clip.

### 5.3 Lose a life (Restart / Quit confirm)
- While the popup is open, the heart beats: scale 1 → 1.08 → 0.98 → 1.05 → 1, looping every 1.1 s.
- On confirm the heart breaks: the two halves move 9 px outward and 4 px down and rotate 14° away from each other in **0.45 s** with overshoot. The crack line fades in 0.1 s.
- The action (reload or go Home) runs after ~0.75 s.

### 5.4 Episode unlocked
- The title pops in (0.6 s, overshoot) and the card pops in (0.55 s).
- Fireworks play for 3.2 s.
- "Tap to continue" fades in after 1.2 s, then breathes (1 → 1.05, 1.6 s loop).

## 6. Always-on

### 6.1 Creatures
| Behaviour | Values |
|---|---|
| **Blink** | Every 2–7 s (random per creature): close in ~80 ms, stay closed 0.16 s, open in ~80 ms |
| **Shock** face | 0.7 s, on invalid (§2.5) |
| **Happy** face | 1.6 s on tube complete, 2 s on win |
| Sleep (zZ) | Not used |
| Eyes follow the finger | Not used: the eyes are part of each face sprite, so they stay still |

Each colour has its own idle face (art). The view only swaps sprites: idle, blink, shock, happy.

### 6.2 UI
| Element | Animation |
|---|---|
| Popup | Background dims; card scales 0.4 → 1 in 0.4 s with overshoot (`cubic-bezier(.2,1.6,.4,1)`, roughly OutBack 1.7) |
| Toast | Fades in and slides down 8 px (0.25 s); fades out the same way |
| Play button (Home) | Breathes 1 → 1.05, 1.6 s loop |
| Booster button | See §4 |
| Home scene | Creatures bob over the cauldron (idle loop) and blink |

## 7. Config fields (`AnimationConfig`)

Defaults are the İdil values above.

| Field | Default | § |
|---|---|---|
| `speed` | 1 | all |
| `introStagger` / `introFallSpeed` | 0.045 s / 3 SH per s | 2.1 |
| `liftHeight` / `liftFollowRate` / `liftBob` | 0.72 d / 20 / 0.06 d | 2.2 |
| `riseTime` / `arcBaseTime` / `arcTimePerSW` / `dropTime` | 0.10 / 0.15 / 0.23 / 0.12 s | 2.3 |
| `arcHeightBase` / `arcHeightPerDistance` | 0.5 d / 0.32 | 2.3 |
| `chainDelay` | 0.08 s | 2.3 |
| `squash` | 0.7 | 2.3, 2.4 |
| `trail` | on | 2.3 |
| `landSquashAmount` / `landSquashTime` | 0.32 / 0.45 s | 2.4 |
| `hopBounceHeight` / `hopBounceTime` | 0.4 d / 0.55 s | 2.4, 2.6 |
| `bumpHeight` / `bumpRippleDelay` | 0.07 d / 0.03 s | 2.4 |
| `invalidShakeAmount` / `invalidShakeTime` | 0.16 d / 0.4 s | 2.5 |
| `hitStopTime` / `hitStopTimeScale` | 0.06 s / 0.06 | 2.6 |
| `punchScale` / `punchTime` | 0.028 / 0.3 s | 2.6 |
| `corkDropTime` (OutBounce) | 0.45 s | 2.6 |
| `glowFadeTime` / `glowIdle` | 0.6 s / 50% ± 12% | 2.6 |
| `sparkles`, `confetti`, `dance`, `glow`, `chainEscalate` | all on | 2.6 |
| `danceStagger` | 0.07 s | 2.6 |
| `revealDelay` / `revealFlipTime` | 0.14 / 0.32 s | 3.1 |
| `towelDelay` / `towelLiftTime` / `towelRise` / `towelStagger` | 0.45 s / 0.6 s / 2.6 d / 0.08 s | 3.2 |
| `keyFlyTime` / `keyWaitAfterComplete` | 0.6 / 0.25 s | 3.3 |
| `boxWobbleTime` / `boxOpenTime` | 0.25 / 0.35 s | 3.3 |
| `iceCrackTime` | 0.16 s | 3.4 |
| `boosterFlightSpeed` / `shuffleStagger` | 1.7× / 0.03 s | 4 |
| `stuckDelay` | 0.4 s | 5.1 |
| `winDelay` / `winCelebrationDelay` / `winOverlayDelay` | 0.9 / 0.3 / 1.0 s | 5.2 |
| `winLogoSeconds` | 2.6 s | 5.2 |
| `coinCountUpSeconds` | 0.8–0.9 s | 5.2 |

Particle counts, sizes and colours are VFX and art: keep them on the prefabs, not in this config.

## 8. Unity notes
- Tweens: PrimeTween (free, zero-alloc) or DOTween. The flight is three chained tweens; the arc can be `DOJump` or a custom parabola.
- Write the wobbles (squash, hop bounce, invalid shake, glow pulse) as small formula-driven scripts, not tween chains. The formulas above are copied from the prototype.
- All ball movement goes through one view method, as planned for step 3, so the snap can be replaced with these tweens in one place.
- Use pooling for particles, keys and confetti.

## Appendix: prototype options that are NOT needed
The prototype's Setup panel has other choices. They were tried and not picked, so **don't build them**:
- Move animations Arc, Slide and Screw (only **Hop**).
- "Bottle lifts" selection (only the top ball lifts, the tube stays).
- Eyes following the finger (only blinking).
- Liquid and 2×2 mini balls with the pouring bottle.
- On completion: glass-shard burst, green tick, sleeping creatures.
- Other cork styles: gold cap, crystal, heart, wax seal, ribbon bow (only **wooden cork, tinted in the tube colour**).
- Layer swap / back-row bottles (the layered mechanic was dropped).
