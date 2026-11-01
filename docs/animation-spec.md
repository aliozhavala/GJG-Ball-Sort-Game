# Animations and feedback (built so far)

What is built from product's `docs/design/animations.md` §2.2–2.6 and the matching sounds of `docs/design/SFX_GUIDE.md`
§3, plus the UI and booster sounds (SFX guide §5, §6.3, §6.4, §7), purchase feedback and the haptic calls (below). Also built:
3.1 hidden reveal, 3.2 veil lift, 3.3 lock box, 3.4 ice break, 4 shuffle (one tube), 5.1 stuck timing and 6.1 blink. Not
built yet: 2.1 intro, undo and add-tube animations, 5.2–5.4 (win, life lost, episode), the other idle and UI loops. Every number is an `AnimationConfig`
field (Inspector-tunable); sound rules are on `SfxConfig`. Choices that differ from the product document are marked
**(ours)**.

## Rules
- **Logic never waits on animation.** Core applies the move at once; views play what already happened.
- **Order follows the events:** move → reveal → cap/laugh → veil → key → its door → win. Later beats are triggered by the
  balls' flights (a ball leaving its tube, the last ball landing), not by fixed timers, so an interrupted flight still
  plays them in order.
- **Input stays live.** Touching a ball or tube that is still animating finishes its tweens first (lift, flight, landing,
  pending beats); so does a layout change (added tube). A beat finished early this way snaps to its end and drops the
  sounds it had not played yet **(ours)** (the lock box today: `TubeView.IsFinishing`).
- **Popups** block input until their close animation ends (no popup animation yet: `Popup.IsOpen` still gates input).
- **Board clock:** every board tween runs on DOTween's manual update, driven by `BoardClock` at `_speed` × the hit-stop
  rate. UI, input, sounds, particles and trails run on normal time. `Time.timeScale` is never touched.
- **Units:** **d** = ball diameter (`TubeVisualConfig.BallDiameter`, board units, so motion scales with the board);
  **SW** = screen width in board units; times in seconds at `_speed` 1.

| § | Hook | Trigger |
|---|---|---|
| 2.2 | `BoardView.ShowSelection` → `TubeView.Lift` / `Lower` → `BallView.MoveTo` | `TubeInput.SelectionChanged` |
| 2.3 | `BoardView.Transfer` → `BallView.MoveTo` (flight) | `MoveApplied`; `MoveUndone` (reversed) |
| 2.4 | `BallView` landing, `TubeView` bump | end of a flight |
| 2.5 | `LevelFeedback.PlayInvalidMove` → `TubeView` shake, `BallView.SetExpression` | `TubeInput.InvalidMove` |
| 2.6 | `BoardView.ShowCompleted` → `TubeView` cork, glow, dance; `LevelFeedback.PlayTubeCompleted` (VFX, hit-stop, punch, sounds) | `TubeCompleted`, played when the tube's last ball lands |

## 2.2 Select / deselect
- **Select:** only the **top ball** lifts **(product; replaces "the run lifts")**; the rest of the run follows during the
  move. It rises to `_liftHeight` **0.72 d** above the mouth in `_liftDuration` **0.23 s**, `_liftEase` **OutCubic**
  (≈90 % in 0.12 s, the prototype's exponential follow at rate 20, as a tween **(ours)**). Sound `select`.
- **Held:** bobs ±`_liftBob` **0.06 d**, period `_liftBobPeriod` **1.25 s** (InOutSine yo-yo loop).
- **Deselect:** back to its slot with the same duration and ease. Sound `deselect`, only when a ball really drops back
  (not when the selection clears because the ball flew off).
- The tube does not move. A tap on a tube that can't be selected plays 2.5.

## 2.3 Ball flight (hop)
The moved balls (top first, partial moves included) each fly three segments, ball *i* starting `_chainDelay`
**0.08 s** × *i* after the first:

| Segment | To | Duration | Ease |
|---|---|---|---|
| Rise | `_liftHeight` above the **source** mouth | `_riseDuration` **0.10** | `_riseEase` **OutQuad** |
| Arc | `_liftHeight` above the **target** mouth | `_arcBaseDuration` **0.15** + `_arcDurationPerScreenWidth` **0.23** × D / SW | `_arcEase` **InOutSine** |
| Drop | its slot | `_dropDuration` **0.12** | `_dropEase` **InQuad** |

- D = horizontal + vertical distance between the two mouths. Arc height = `_arcHeightBase` **0.5 d** +
  `_arcHeightPerDistance` **0.32** × D, a parabola `h·4k(1−k)` on the eased progress k.
- **Stretch** along the velocity: `min(v / _stretchFullSpeed (6.7 SW/s), _stretchMax 0.3) × _squash 0.7` (≤ 21 %),
  none below `_stretchMinSpeed` **0.15 SW/s**.
- **Trail** (`_trail` on) during arc and drop: `_trailTime` **0.15 s** (9 frames at 60 fps), ball colour, widening to
  `_trailWidth` **1.4** × radius, alpha up to `_trailAlpha` **0.45**; cleared on landing.
- The source's new top turns face up (3.1) when the last moved ball has left it (end of its rise).
- **Undo** uses the same flight, all balls reversed, without land sounds (SFX guide §5); its 1.7× speed is §4, later.

## 2.4 Landing (each ball, at the end of its drop)
- **Squash** `_landSquashDuration` **0.45 s**: f = e^(−`_landSquashDamping` 11·t)·cos(`_landSquashFrequency` 30·t),
  scale x = 1 + `_landSquashAmount` **0.32** × `_squash` × f, y = 1 − the same.
- **Hop bounce** `_hopBounceDuration` **0.55 s**: y = |sin(2πt / duration)| × `_hopBounceHeight` **0.4 d** ×
  e^(−`_hopBounceDamping` 4·t): two bounces, ≈0.23 d and ≈0.08 d.
- **Balls below** bump `_bumpHeight` **0.07 d** (half sine, `_ballBumpDuration` **0.2 s**), each `_bumpRippleDelay`
  **0.03 s** × its depth below the new ball later.
- **Tube bump** `_bumpHeight` (half sine, `_tubeBumpDuration` **0.22 s**).
- **Droplets** (`_droplets` on): pooled `DropletsVfx` in the ball colour (count, size, gravity on the prefab).
- Sound `land`, pitch by slot (below).

## 2.5 Invalid move
- On the tapped tube (target of an illegal move, or a tube that can't be selected); a lifted ball drops back first (2.2).
- **Shake** `_invalidShakeDuration` **0.4 s**: x = sin(`_invalidShakeFrequency` 55·t) × `_invalidShakeAmount` **0.16 d** ×
  e^(−`_invalidShakeDamping` 9·t).
- **Shock face** on its revealed balls for `_shockFaceDuration` **0.7 s**, then back to their resting face.
- A covered tube shakes with its cover (the whole door for a lock) **(ours: the cover is a separate view, so the tube alone would peek out)**.
- Replaces the old placeholder tube flash. Sound `invalid` (+ `deselect` if a ball was lifted).

## 2.6 Tube complete
Starts when the completed tube's **last ball lands** (move or undo). From there:

| t | Piece | Values |
|---|---|---|
| 0 | Chime sound | pitch by chain (below) |
| 0 | Cork | the colour's cork (`ColorPalette`; the plain `TubeVisualConfig` cork, tinted, for colours without one), drops `_corkDropHeight` **1.2 d** onto the rim **(ours: the prototype's 90 px, about 2 d, starts inside the row above)** in `_corkDropDuration` **0.45 s**, `_corkEase` **OutBounce**; stays |
| 0 | Glow | tube outline in the ball colour, alpha 1 → `_glowIdleAlpha` **0.5** in `_glowFadeDuration` **0.6 s**, then ±`_glowPulse` **0.12**, period `_glowPulsePeriod` **1.6 s**; stays |
| 0 | Sparkles + ring | pooled `TubeCompleteVfx`: 14 + 3 × chain sparkles (faster with chain), ring 2.2 d over 0.5 s (prefab values) |
| 0 | Confetti | 26 pieces from the mouth, 1.4–2.2 s (prefab values) |
| 0 | Dance | each ball does the hop bounce (2.4), `_danceStagger` **0.07 s** apart, bottom to top; balls laugh from here on **(ours: they stay laughing, not 1.6 s)** |
| `_corkImpactTime` **0.16** | Cork seats | cork sound, hit-stop `_hitStopDuration` **0.06 s** at `_hitStopTimeScale` **0.06**, punch: board scale 1 → 1 + `_punchScale` **0.028** → 1, half sine over `_punchDuration` **0.3 s** **(SFX guide timing; docs/design/animations.md starts them at 0)** |

- **Chain** = completed tubes on the board − 1 (a completion is never undone, so no rollback is needed).
- Toggles: `_sparkles`, `_confetti`, `_dance`, `_glow`, `_chainEscalate` (sparkles and chime pitch grow with chain).
- A tube rebuilt completed (restart, shuffle) shows cork, glow and laugh at rest, without the sequence.
- No mask on the cork: it draws between the balls and the rim's front half, so it sinks into the neck by draw order.
- Depth (ours; the prototype draws in DOM order): a ball inside its tube's mouth draws between the rim halves; from
  the moment its bottom clears the mouth it is above every tube, through the whole arc, until its centre drops below
  the target's clear height (`TubeView.ClearHeight`, mouth + half a ball). So a flight never passes behind a rim, and a
  landing ball tucks behind the target's rim front.

## Later beats in the same move
Timed so the order reads right.
- Veil lifts `_towelDelay` **0.45 s** after the last land (3.2).
- The key leaves at the last land (+ `_keyWaitAfterComplete` **0.25 s** if a tube completed), never before a veil the
  same move opened has finished lifting **(ours: the towel is gone, not just starting)**, a lock box opened earlier in
  the same move has shown its tubes (a key inside a box that is still opening; no shipped level does it), or a ball a
  shuffle turned face up has finished turning; its lock box opens with it (3.3).
- Win: the celebration starts once the board has settled.

## 3.1 Hidden reveal
- **When:** after a move, the moment the last moving ball has left the tube (end of its rise; product's fixed 0.14 s is
  the same moment, ours follows the flight); after a shuffle, when its balls have landed.
- **Flip:** the ball's `Face` scales horizontally by |cos(πu)| over `_revealFlipDuration` **0.32 s** (linear u), so it
  narrows to nothing and widens again; at the midpoint the "?" becomes the creature, in whatever face it holds then
  (laughing if the reveal completed its tube), with its key in its hands if it carries one. `BallView.Reveal(…, animate)`.
- **VFX:** pooled `Vfx_RevealPuff`: 10 lilac smoke circles, 0.45–0.6 s, drawn on Balls −1, behind the balls
  **(ours: on VFX it covered the flip)**.
- **Sound** `hiddenReveal` and a Light haptic at the flip's start, after a move only: a shuffle's reveal flips and puffs
  silently (SFX guide 4.1).
- A ball that is turning never blinks. Touching its tube, snapping or pooling the ball, or its key leaving for the lock
  box ends the flip at once, face up (the reveal is information, so it is never undone).

## 3.2 Veil lifts
- **When:** `_towelDelay` **0.45 s** after the completing move's last land; each further veil the same completion
  opens starts `_towelStagger` **0.08 s** after the previous one. `CoverView.PlayVeilLift`, a beat on the veiled tube.
- **Lift:** over `_towelLiftDuration` **0.6 s**, smoothstep s = u²(3 − 2u): the towel and its sticker rise
  `_towelRise` **2.6 d** × s and fade to 1 − s, tilting `_towelTilt` **5°** × sin(πu) about the towel's middle (most
  mid-way, straight at both ends). The tube's balls show at the lift's start, under the fading towel.
- **VFX at the start:** pooled `Vfx_TowelPuff` (12 pink smoke circles over the towel's middle, 0.5–0.75 s, alpha 0.45)
  and `Vfx_TowelSparks` (10 gold sparks up from the tube's mouth).
- **Sound** `towelPull` and a Heavy haptic, once per completion (the first veil only).
- **Sway (idle):** a closed towel turns ±`_towelSwayAngle` **1°** about its rim, one full sway per `_towelSwayPeriod`
  **4 s**, each veil with its own random phase; `_towelSway` turns it off (read every step). A whole-towel turn
  **(ours: product's "hem sways"; a sliced sprite cannot bend)**. It stops when the lift starts.
- A tap on the veiled tube during the lift ends it at once (towel gone); a tap before it starts shakes the towel with
  its tube (2.5).

## 3.3 Lock box opens
Ours, replacing product's §3.3 (wobble, splinters, ring, crate scaling up): the key flies into the box's slot, the mid
gives way to the two cover halves, which slide apart like doors, then the box fades. SFX guide 4.3's timings are kept.
Starts at the key's time (Later beats, above); `BoardView.OnLockOpened` → `CoverView.PlayUnlock` → `LockBoxView`.

| t (from the key's start) | Piece | Values | Sound, haptic |
|---|---|---|---|
| 0 | Key flies | leaves the ball's hands (`BallView.TryTakeKey`) and flies a quadratic Bézier to the mid's key slot, its control point `_keyArcHeight` **1.5 d** above the higher end; grows from its size in the hands to the slot's (`TubeVisualConfig._lockSlotKeyWidth` 0.68 of the mid); `_keyFlyDuration` **0.6 s**, `_keyFlyEase` **InOutCubic** | `lockboxKeyFly` |
| 0.6 | Key seats, box wobbles | the box tilts ±`_boxWobbleAngle` **2°** (sin) and squashes `_boxWobbleSquash` **5 %** wider and lower (cos) about its base, at `_boxWobbleFrequency` **40**, damped by e^(−`_boxWobbleDamping` 12·t) × (1 − t / duration) over `_boxWobbleDuration` **0.25 s** **(ours, asked for)**; the key holds `_keyHoldDuration` **0.1 s**, then fades over `_lockFadeDuration` **0.25 s** | `lockboxKeyIn`, Medium |
| 0.6 + `_boxOpenDelay` **0.55** | Covers slide | the mid gives way to the two cover halves, meeting in the middle (where their bars end, `_lockCoverInnerEdge`), so the notches show the empty slot; the inside appears behind the tubes and their balls are drawn again. The covers' centres slide to the box's edges over `_boxSlideDuration` **0.45 s**, `_boxSlideEase` **OutCubic** | `lockboxBreak`, Heavy |
| + slide + `_boxHoldDuration` **0.15** | Box fades | sides, covers and inside fade over `_lockFadeDuration` **0.25 s**; then the cover is open (`CoverView.SetOpen` state) | none |

- **Doors (ours):** each face is the door panel behind its cover: it runs from its side to the cover's centre, so it
  shrinks as the cover slides out. A `SpriteMask` the size of the box clips the covers at its edges, under the sides
  (TubeFront 7, over the covers' 6), so the doors slide into the frame. Needed because two covers (2 × 152 px of art)
  are wider than a one-tube box (261 px): there they start clipped and slide out less, never inwards.
- **Sound for the seat:** `lockboxKeyIn` (a thump, then the lock's two clicks at 0.40 s) is the best fit of the pack;
  the box opens 0.55 s after the seat, after the clicks.
- One sound of each per box, however many tubes it spans. The whole opening lasts about 2.0 s; the board is not
  settled until it ends, so the stuck bar waits for it.
- **Interrupting:** the opening is a beat on the box's tubes and on the key's tube. Touching any of them plays it to
  the end at once (box open, key gone) without its remaining sounds. Restart and pooling end it without playing it.
- **Invalid tap** on a box's tube while the box is still drawn (closed or opening) shakes the whole cover with its tubes
  (`CoverView.IsDrawn`).

## 3.4 Ice break
Plays with the normal completion (2.6) when a clay (ice) tube completes; an unfinished clay tube keeps its ice.

| t (from the completion's start: the last ball lands) | Piece | Values |
|---|---|---|
| 0 → `_iceCrackTime` **0.16** | Crack | the ice shivers sideways ±`_iceShiverAmount` **0.04 d** at `_iceShiverFrequency` **70** and whitens up to `_iceCrackBrighten` **50 %** **(ours: stands in for the three growing cracks, whose art is missing)** |
| `_iceCrackTime` | Burst | the ice is gone (`TubeView.BreakIce`); pooled `Vfx_IceBreak`: 22 ice shards up from the ice with gravity (0.8–1.1 s) and a puff of 8 soft circles; the tube-complete ring in `LevelFeedback._iceRingColour` (pale ice blue; 2.2 d, the ring prefab's size, not the document's 2.4 d **(ours)**); `iceBreak` sound and a Heavy haptic |

- With the default timing the burst lands with the cork's impact, so `iceBreak` and `tubeCork` play together (SFX guide
  §4.4), inside the cork's hit-stop.
- Hook: `BoardView.PlayCompletion` keeps a beat on the tube (`IceBroken` event → `LevelFeedback`); touching the tube
  plays it at once. A tube rebuilt completed (restart) shows no ice and no burst.
- "Melt" in the earlier notes is this break: the product document and the sound are a shatter.

## 6.1 Blink
- **One ball at a time (ours, after playtesting; product's document has every creature blink every 2–7 s, which
  read as far too much on a full board):** after a random wait between `_boardBlinkIntervalMin` **2 s** and
  `_boardBlinkIntervalMax` **5 s** (picked again each time), `BlinkScheduler` (owned by `BoardView`) picks one random
  ball that can blink, whatever the ball count. The ball shows `HalfOpen` for `_blinkCloseDuration` **0.08 s**,
  `Closed` for `_blinkClosedDuration` **0.16 s**, `HalfOpen` for `_blinkOpenDuration` **0.08 s**, then its resting face
  (`BallView.TryBlink`). All on the board clock, so a hit-stop slows it too. `_blink` turns it off (read at each blink,
  so it works in Play mode).
- Only a ball that is on screen, revealed, resting with its open face and holding no other face is picked: hidden
  balls, the laughing balls of a completed tube and a ball showing shock never blink. A shock replaces a half-shown
  blink.
- Blinking is not motion: snapping or flying a ball keeps a blink going. Pooling a ball or hiding it under a cover
  ends its blink with the eyes open.
- Colours without blink frames (ids 10–11) fall back to their open face, so they never visibly blink. Silent (SFX
  guide §8).

## 4 Shuffle (one tube)
Adapted from product's §4 (a board-wide shuffle staggered by tube) to the single picked tube **(ours)**.
- Core says where each ball went (`TubeShuffledEventArgs.FromSlots`); the tube keeps its ball views and reorders them
  (`TubeView.Reorder`), so hidden balls and keys stay on the right balls.
- Each ball whose slot changed flies a move's flight (2.3) inside its tube: rise to `_shuffleRiseHeight` **0.4 d** above
  the mouth, a hop of `_shuffleArcHeight` **0.3 d**, drop into its new slot, then the landing (2.4); the whole flight at
  `_shuffleSpeed` **1.7×**. Top ball first, `_shuffleStagger` **0.05 s** apart.
- Each ball that kept its slot wiggles: tilt ±`_shuffleWiggleAngle` **8°**, sin(`_shuffleWiggleFrequency` 30·t) ·
  e^(−`_shuffleWiggleDamping` 8·t) over `_shuffleWiggleDuration` **0.4 s**.
- Sound `boosterShuffle` when applied; the landings are silent and a hidden ball throws no droplets (their colour
  would give it away). The reveal of the new top, a key found and a completion wait for the balls to land, in event
  order.
- Known compromise: a ball rising from deep in the tube passes over the balls that stay (one ball wide).

## 5.1 Stuck
- The level enters Stuck at once (Core decided it with the move): the boosters lock, so nothing in the bar reacts.
- The stuck bar replaces the booster bar only once the board has **settled** (no ball flying, no beat waiting: cork,
  veil, key) and stayed settled for `_stuckDelay` **0.4 s** (÷ `_speed`); a move made meanwhile starts the wait over
  (`LevelFeedback.WaitForStuckRevealAsync`, awaited by `LevelFlow`). Until then the locked bar stays, dimmed.
- It fades in over `_stuckFadeDuration` **0.25 s** (real time, `StuckPanel`'s `CanvasGroup`), with `moveInvalid` and a
  Warning haptic. Leaving Stuck (Play on, restart) hides it at once and cancels a pending reveal.
- **(ours)** The SFX guide times it from the last landing (0.3 + 0.4 s, 0.9 + 0.4 s after a completion);
  animations.md §5.1's 0.4 s after the board settles is used instead: settling already includes the cork's beat and
  the veil and key delays.

## Sounds (`SfxConfig`, played through `AudioService`)
| Sound | When | Pitch |
|---|---|---|
| `select` | ball starts to lift | random ±3 % |
| `deselect` | lifted ball starts back | random ±3 % |
| `land` | each ball lands (not on undo) | 2^(PENT[min(slot, 6)] / 24), PENT = 0 2 4 7 9 12 14, × random ±3 % |
| `invalid` | invalid tap | 1 |
| `tubeCompleteChime` | last ball lands in a completed tube | 2^(min(chain, 12) / 12) × random ±3 % |
| `tubeCork` | cork seats | random ±3 % |
| `hiddenReveal` | a hidden ball starts to turn after a move (not after a shuffle) (3.1) | random ±3 % |
| `towelPull` | the first veil of a completion starts to lift (3.2) | random ±3 % |
| `lockboxKeyFly` / `lockboxKeyIn` / `lockboxBreak` | lock box: key starts, key seats, covers slide (3.3) | random ±3 % |

Master volume `_sfxVolume` **0.7**; every clip plays at 1.0 into it. Sounds overlap freely on 10 pooled sources (the
oldest is cut when all are busy). The Sound setting off mutes everything, including sounds already playing. Random
pitch (`_randomPitchRange` ±3 %) applies to the mp3 clips and `deselect` (SFX guide §2). Land and chain pitches are
`_landSemitones` and `_chainSemitones` / `_chainSemitoneCap` on `SfxConfig`.

- **Files:** `Assets/Audio/SFX/<folder>/` as delivered (22 clips, all slots filled). Import (`SfxImportRules`, first
  import only): short clips Decompress On Load + ADPCM, the fanfare and the reveal Compressed In Memory + Vorbis; all
  preloaded; levels untouched.
- **mp3 lead-in (ours):** every delivered mp3 imports 0.05–0.07 s longer than the guide lists and starts its first
  sound 52–116 ms in (the WAVs at 7–8 ms): the padding mp3 encoding adds. Each mp3 slot skips `_startOffset`
  **0.05 s**, so the land, chime and cork hit on their frame; the towel's and fanfare's later starts are part of the
  clip and stay.
- **Silent on purpose:** flights, undo landings (SFX guide §5).

### Boosters, buttons and purchases
| Sound | When | Played by |
|---|---|---|
| `boosterUndo` / `boosterShuffle` / `boosterAddTube` | the undo / shuffle / added tube is applied (Play on adds a tube, so it plays Add tube's) | `LevelFeedback`, on the session's `MoveUndone` / `Shuffled` / `TubeAdded` |
| `uiButton` | every button press and settings toggle, popups' ✕ and tap-anywhere closes included; a refused booster still clicks | `UiButtonFeedback` on each canvas root |
| `uiPlay` | Home's Level button when it starts the level (`uiButton` at 0 lives: it opens More Lives) | `HomeScreen` |
| `rewardJingle` | a booster bought, the lives refilled | `LevelFlow.TryBuyBooster`, `HomeScreen.TryBuyLives` |
| `coinGain` | a Shop coin bundle taken (with the press's `uiButton`, at once: no store dialog to wait for) **(ours)** | `HomeScreen` |
| `lifeLost` | Are You Sure confirmed when it spends a life (`uiButton` when it does not: 0 lives, editor test visit) | `LevelFlow.OnExitConfirmed` |
| `moveInvalid` | "Not enough coins" (Buy), "No tube to shuffle" and a refused pick; the stuck bar appearing (5.1) | `LevelFlow`, `HomeScreen`, `LevelFeedback.PlayStuck` |

- **Own sound instead of the click:** Home's Level button, Buy, Are You Sure's Confirm (`UiButtonFeedback._ownFeedback`).
  The other booster refusals (nothing to undo, no room) are toast-only, with the button's click.
- **Toggles** click after the setting is saved (`SettingToggle.Toggled`), so turning Sound on is heard and turning it off
  is silent (the mute cuts it) **(ours)**.
- **Disabled buttons** (Play on without the coins, locked boosters, locked avatar frames) never click, so they are silent.
- **Purchase feedback:** what the purchase filled punches (`UiPunch`, real time): the booster's count badge, the lives
  display's heart, the coins pill. Scale 1 → 1 + `_purchasePunchScale` **0.25** → 1, springing (`_purchasePunchVibrato`
  **6**) over `_purchasePunchDuration` **0.35 s** **(ours: the SFX guide has sounds only)**.

## Haptics (placeholders)
`HapticsService` on `AppRoot` has one method per kind of SFX guide §2 (`Selection`, `Light`, `Medium`, `Warning`,
`Success`, `Heavy`, `Win`, plus `Landing`: Medium at most once per `_minLandingInterval` 0.05 s) and is silent while the
Haptics setting is off. **The calls are in place wherever the guide pairs a haptic with a sound that the game plays;
only the vibration itself is missing** (`HapticsService.Fire` is the one place for the platform call). `_logInEditor`
logs each call, to check them.

| Haptic | Where |
|---|---|
| Selection | ball lifts |
| Medium (`Landing`) | each ball lands (not on undo) |
| Medium | a lock box's key seats (3.3) |
| Light | a hidden ball turns face up after a move (3.1) |
| Warning | invalid tap or move; the stuck bar appearing; a life lost |
| Success | cork seats; booster bought; lives refilled |
| Heavy | the ice bursts (3.4); a lock box's covers slide open (3.3); a veil lifts (3.2, once per completion) |
| Light | every button and toggle press (not popups' ✕ or the name pen); each booster applied |

Haptics for sounds not played yet (win, rewards) come with those sounds.
