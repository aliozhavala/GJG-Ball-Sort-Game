# Copy

Player-facing text proposed by product. The current in-game strings (booster toasts, "No available moves", …) are in docs/screens.md and aren't repeated here.

> **Open issue: vocabulary.** The tutorials mix plain words (tube, ball: L1, L7) with art names (bottle, creature, towel, lock box: L20, L30, L55, L80). Pick one set for everything the player reads; the art names fit the creature art.

## Tutorial toasts
Shown the first time the level is played, 0.6 s after the board appears, for 3.6 s; they never block input. The texts come from a `TutorialConfig` (level → message).

| Level | Mechanic | Text |
|---|---|---|
| 1 | basics | Tap a tube, then tap the target. Put same-colour balls together! |
| 7 | hidden balls | New: Hidden balls! The colour of a "?" ball shows when the ball above it moves. |
| 20 | large clay, big-bottle skin | New: Big bottle! It only takes its own colour. Gather all of that colour here. |
| 30 | veil | New: Towels! Complete the colour on the badge and the towels come off. |
| 55 | capacity-4 clay | New: Ice bottles! They are frozen to the floor: you can fill them, but never pour out of them. |
| 80 | lock and key | New: Lock boxes! Dig out the creature carrying a key and the box with the matching lock opens. |

> **Open issue (L7):** a hidden ball shows when it becomes the top ball, which can take more than one move when several balls sit above it. Something like "shows when it reaches the top" is exact.

## Tier tags
- Home Level button: on a Hard level, a "Hard Level" chip and a red "x3" tag. On a Super Hard level, the button turns red with "Super Hard" and "x5".
- Level screen, under `Level N`: "HARD" / "SUPER HARD".
- Win panel title tab: "Level N" plus "HARD" / "SUPER HARD".

## Win panel
- Graded titles (meta-ideas.md → Win grades): "PERFECT!", "AWESOME!", "GOOD JOB!".
- "Rewards:" above the coin amount.
- Logo: "LAB SORT" (the name isn't decided; feel.md).

## Text for unscheduled features (meta-ideas.md)
**Lives**
- Are You Sure: "You will lose 1 life!", with a red button named after the action: "Home", "Restart" or "Give up".
- "No lives left!"
- More Lives: "Time to next life" + `mm:ss`, or "Your lives are full!"; buttons "Refill" (coin icon + price) and "+1 Life" (ad icon).

**Coins and boosters**
- "Not enough coins"
- Booster Buy: the booster's name as the title, "Out of boosters!", a one-line description, a "Buy" button with the price. After buying: "Undo +1" (etc.).

**Shop**
- Footer: "Mock shop: no real payment." After a purchase: "Mock purchase complete (no payment taken)".
- Offers: Starter Lab Kit (−90% badge). Bundles: Intern → Assistant → Scientist ("Popular") → Professor → Mad Genius ("Best Offer").

**Leaderboard**
- Locked: "Unlocks at Level 40", with progress `current / 40`.
- Tabs "Weekly | Players" and "World | Country"; a reset chip like "4d 8h".

**Episodes**
- "Episode Unlocked!", the episode name, "Level 20 – 39", "Tap to Continue".

**Ads**
- "MOCK REWARDED AD", a 3-2-1 countdown, "The reward arrives when the ad finishes".
