# Meta-game ideas (proposed, not scheduled)

Product proposed these on 28 Oct 2026. None of them is in the brief (CLAUDE.md) or scheduled, and each needs a team decision first (decisions.md). The concerns from the developer review are listed with each idea. If one is accepted, its rules move into docs/screens.md or docs/levels.md and into code; this file keeps the intent.

## Lives
- **Leaving costs a life** once the player has made at least one move in this attempt: settings → Home, settings → Restart, stuck → Give up. Each goes through an **Are You Sure** popup first (`38_…`, `39_…heart_breaking`). Confirm → heart-break animation (~0.75 s) → lives −1 (the regen timer starts if lives were full) → the action. Close → back where it came from (the settings popup or the stuck bar). With 0 moves made, Home and Restart are free and skip the popup.
- **0 lives:** the Home Level button opens **More Lives** instead of loading the level.
- **Regen:** +1 life every 30 min of real time, also while the app is closed (`NextLifeAt`, UTC; cleared at max).
- **Lives pill** on Home: the count inside a heart, "Full" or a `mm:ss` countdown to the next life, a small green "+". Tap → More Lives. The level shows no lives.
- **More Lives popup** (`13_more_lives.jpg`, `15_more_lives_full.jpg`): coins pill, a big heart with the count, the time to the next life or "lives full".
  - **Refill** (900 coins): lives = max. Not enough coins → "Not enough coins" and the Shop at Coins.
  - **+1 Life**: a mock rewarded ad, then +1 life.
  - Both are disabled when lives are full. Infinite lives (from the shop, `InfiniteLivesUntil`) show ∞ and the time left, and no life is spent.
- `LivesConfig`: `maxLives` 5, `regenMinutes` 30, `refillCost` 900, `adLivesReward` 1, `lifeCostOnLeave` 1, `freeLeaveIfNoMoves` true.

Concerns:
- Today lives are display-only and leaving is free (docs/screens.md). This changes both.
- "A move made" needs a definition: does undoing back to the start count?
- Closing the app mid-level costs nothing, so it beats Give up.
- "0 lives inside a level" can't happen (lives only drop on leaving). If it did, a stuck player with fewer than 900 coins couldn't leave. Leaving should always be possible.
- Real time needs clock-change handling. New save fields need a save version bump. `StartLives` already lives in `EconomyConfig`.

## Level variants, retries and DLB
Each level ships several solver-verified **variants**, so a retry feels fresh (product D18, D25). "DLB" is product's term for the much easier variants of tier levels (the abbreviation isn't explained in the proposals).
- **Same structure, new deal:** all variants of a level share the tube count, capacities, `layout.grid`, mechanics, cover / lock / clay placement and tier. Ball colours and the deal differ.
- **How many:** L1–4 have 1 variant; every other level has 5; Hard and Super Hard levels also have 3 DLB variants (much easier, same structure).
- **Files:** `level_044.json` stays variant 1, so nothing changes for a level without variants. Extras go next to it as `level_044_v2.json` … `level_044_v5.json` and `level_044_dlb1.json` … `level_044_dlb3.json`, each with `"id": 44`. `LevelDatabase` keeps one entry per level with its variant list, its DLB list and `dlbCap`. The id-vs-file-name test reads the number before the suffix.
- **Difficulty per slot** (target = the level's target bot win rate, level-plan.md):

  | Slot | Played on | Difficulty |
  |---|---|---|
  | v1 | first attempt | target ± 5 points |
  | v2 | retry 1 | target ± 5 points |
  | v3–v5 | retries 2, 3, 4 | eased: target + 8 points (product tunes 5–10) |
  | dlb1–3 | Hard / Super Hard after `dlbCap` losses | target + 25 points |

- **Selection:** the save keeps `RetryCount[level]`: +1 on every attempt that costs a life (restart or give up after at least one move), 0 on a win.
  ```
  if (level has DLB && retry >= dlbCap)  use dlb[retry - dlbCap], or a random DLB variant != last played once the list runs out
  else if (retry < variants.Count)      use variants[retry]
  else                                  use a random eased variant (v3–v5) != last played
  ```
- `dlbCap` by episode: 2, 2, 3, 4, 4. Tune after playtests.
- Restart reloads into the next variant. The Level scene still loads `CurrentLevel`; the variant pick happens inside that load.
- **Making variants:** v1 keeps the generated colours; the others are recoloured from the first `colors + 3` palette slots. Variants must be distinct under a structure-aware key: a pure colour swap of another variant counts as the same layout, allowed only as an eased copy when no other candidate fits.
- Per file: `meta.variant` (`"v1"` … `"v5"`, `"dlb1"` … `"dlb3"`), `meta.botWinRate`, `meta.bucket`.
- Editor: the level jump picks the variant or DLB variant; Player Data can set `RetryCount`.

Concerns:
- Changes CLAUDE.md's Restart rule (restart would load a different board) and the shape of `LevelDatabase`.
- 4 + 96 × 5 + 19 × 3 = 541 files to generate, curate and test. `ShippedLevelTests` solving each one would be slow; replaying a stored `meta.solution` would not.
- The save also needs the last played variant (for "!= last played").
- With `dlbCap` 2, tier levels in episodes 1–2 go v1 → v2 → DLB and never use v3–v5.
- `colors + 3` exceeds the 12-colour palette on 10-colour levels: cap it at 12.
- `meta.difficulty` already exists in the level DTO; `bucket` can be computed from `botWinRate`.

## Buying boosters with coins
- When a booster's reason is "none left" ("No undos left", "No add-tube boosters left", "No shuffles left"), the tap opens a **Booster Buy** popup instead of the toast. The other reasons keep their toasts and their precedence. At 0 the button shows a green "+" instead of the count.
- **Popup** (`35_booster_buy_*.jpg`): the booster's name as title, a big booster tile, "Out of boosters!", a one-line description, a green Buy button with the price.
  - Enough coins → coins −price, booster +`buyAmount`, a toast, the popup closes.
  - Not enough → "Not enough coins" and the Coins popup (Shop).
  - Close X → back to the level. Gameplay input is blocked while it is open.
- `BoosterConfig`, one row per booster: `startingCount` (the 99 stays the default), `buyPriceCoins`, `buyAmount`. Defaults: Undo 300, Shuffle 400, AddTube 900 coins for +1. Product wants to tune the whole purchase economy there.

Concerns: the starting counts already live in `EconomyConfig`, and they should stay in one place. With 99 of each, nobody (reviewers included) will see this flow unless the counts drop.

## Shop (mock in-app purchases)
- Shop tab → a scrollable page (`04_store_top.jpg`, `05_store_scroll2–4.jpg`). Header: coins pill and a "Shop" title tab.
- Sections from a `ShopCatalog`:
  - Special Offers: Starter Lab Kit (−90% badge).
  - Lab Bundles: Intern → Assistant → Scientist ("Popular") → Professor → Mad Genius ("Best Offer").
  - A banner.
  - Coins: 6 packs in a 3×2 grid (1k / 5k / 10k / 25k / 50k / 100k).
- A bundle card shows the art, the coin amount, perk icons (No Ads, ∞ lives + hours), booster mini tiles "xN", the name and a green price button.
- The price button calls `IPurchaseService.Buy(item)`. The mock waits 0.8 s, grants the item and shows a toast. Granting adds coins and boosters and sets `InfiniteLivesUntil` / `NoAds`.
- The coins pill's "+" on Home opens the Shop at Coins. In the level, a coins pill with "+" opens a **Coins popup** with the same 6 packs (`36_coin_popup.jpg`), so the player doesn't leave the level.
- The full item table (amounts, hours, reference prices) is in the product repo: `docs/dev/DEVELOPER_BRIEF.md` §3.2.

Concerns: a large feature. `NoAds` is a dead field if no ads ship.

## Leaderboard
- The Leaderboard tab shows a padlock until level 40 (`unlockLevel`). Locked page: a big padlock, "Unlocks at Level 40", a progress bar `current / 40`.
- Header "Leaderboard", tabs **Weekly | Players**, a chip with the time to the week reset.
- **Weekly Cup:** a group of 6 (5 mock players + the player), score = levels won this week. Podium in the order 2–1–3, then places 4–6 as rows. At the week's end the top 3 get `cupPrizes` (1000 / 500 / 250); the mock grants them on the first app open after the reset.
- **Players:** a World | Country toggle, ranked by highest level. Rows show rank (medal for 1–3), avatar, name, team or city and "Level N". The player's row is pinned at the bottom, with rank `1000+` when below the list.
- Data from an `ILeaderboardService` (mock players from a TextAsset). `LeaderboardConfig`: `unlockLevel` 40, `cupGroupSize` 6, `cupPrizes`, `weekResetDay` Sunday 00:00 local. Save: `WeeklyCup { weekId, score, lastPrizeWeekId }`; a win adds 1 to the score.

Concerns: a large feature, and all its data is made up. Replaying L100 after the run would farm the cup score. The first-place reward (a profile frame) went with the dropped profile.

## Mock rewarded ad
- Full-screen black, "MOCK REWARDED AD", a big 3-2-1 countdown (`mockAdSeconds` 3), "The reward arrives when the ad finishes" (`14_mock_rewarded_ad.jpg`).
- Behind `IAdService.ShowRewarded()`, so a real SDK could replace it. Only More Lives uses it.

## Episode Unlocked
- When the next level opens an episode (20, 40, 60, 80), an overlay appears on Home about 0.7 s after arriving, and Home switches to the new episode's background (`44_episode_unlocked.jpg`).
- Dark overlay, fireworks, a tilted gold "Episode Unlocked!", a card with the episode art, its name and its level range, "Tap to Continue" (a tap anywhere closes it).
- `EpisodeConfig`: a list of `{ id, name, levelFrom, levelTo, homeBackground, levelBackground, cardArt }`.

Concerns: small in code; needs art for 5 backgrounds and cards.

## Win grades
- A graded title on the win panel: **PERFECT!** if moves ≤ ceil(optimal × 1.25) + 1 and no booster was used; **AWESOME!** if moves ≤ ceil(optimal × 1.8) + 2; otherwise **GOOD JOB!**. `optimal` = `meta.optimalMoves`; with no value, GOOD JOB!. The factors, slacks and titles live in a `WinGradingConfig`.
- Using any booster in the attempt sets `usedHelp`, which blocks PERFECT.
- The save keeps `BestGrade[level]` (1 Good Job, 2 Awesome, 3 Perfect).

Concerns:
- Define "moves": do undone moves count?
- Does Play on count as help? It isn't a booster.
- `optimalMoves` is only written when Optimal mode proves it, so the big late boards may always show GOOD JOB!. Fall back to the Fast solution length.
- `BestGrade` is never shown anywhere (there's no level select).

## Screen changes
- **Level top bar** (`21_gameplay_level1.jpg`): a coins pill with "+" top-left (opens the Coins popup), a level plate in the centre above `Level N`, the settings button top-right; no lives. The plate shows our main character in its frame (product proposed the player's profile picture, which was dropped).
- **No Music setting:** the game has no music, so both settings popups drop the Music toggle; Sound and Haptics stay. `SaveData.MusicOn` is required today, so this means a save version bump and migration, or keeping the field and hiding the toggle.

## Editor and debug tools
For whichever ideas ship, Editor-only (the dev rule; product also asked for development builds): set lives and the regen timer, fast-forward the life timer and the week reset, set `RetryCount` for a level, override `unlockLevel`, set the mock ad length.

## Services
`IPurchaseService`, `IAdService`, `ILeaderboardService`: offline mocks owned by `AppRoot` like the other services, so real ones could be plugged in without touching screens. Only for the features that ship.

## Dropped
- **Player profile** (Edit Profile popup, name editing, avatar and frame pickers, the Neon / Lab / Gold / Royal / Champion frames, `Profile` in the save, `ProfileCatalog`): the Home avatar is our main character in a frame, not a player profile.
