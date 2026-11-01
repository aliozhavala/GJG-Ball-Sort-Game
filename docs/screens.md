# Screens, Flow & Economy

Read this when working on UI, scene flow, lives, tutorials, boosters, or player data. All UI is placeholder until final art (step 6): Unity default UI sprites, TextMeshPro, flat colors. Animations are step 6 too — step 3 snaps instantly, but every ball movement goes through one view method so tweens can replace the snap in one place.

## Scenes & flow
- `Home` (build index 0) → tap Level button → `Level` (build index 1). At 0 lives the Level button opens the More Lives popup instead (Lives, below).
- `Level` → win → celebration → Win panel → **Continue** → `Home`, with the Episode Unlocked overlay if the win moved the player into a new episode (Episodes, below).
- `Level` → stuck → **Give up** → Are You Sure → **Quit** → −1 life → `Home` (no progress, no reward).
- `Level` → settings → **Home** → Are You Sure → **Quit** → −1 life → `Home` (same as giving up).
- `Level` → settings → **Restart** → Are You Sure → **Restart** → −1 life → the level reloads in its next variant. At 0 lives, Restart opens the More Lives popup instead.
- `Level` → the level file fails to load → `Home` with the toast "Level N could not be loaded" (the error is logged). It costs no life and counts no retry: the player did not leave.
- The Level scene always loads `PlayerData.CurrentLevel`, in the variant its retry count picks (docs/levels.md, Variants and retries). Editor-only level jump for testing: **Ball Sort ▸ Play Level…** (or Play on a Validate Levels row) opens the Level scene on any level, or on a level file outside the run (test and draft levels in `Assets/Data/TestLevels/`), in a chosen variant, for one visit; the next visit uses the save again. Such a visit is a test: every attempt (Restart included) plays the chosen variant, the title names it (e.g. "Level 6 · V5", or the file name for a level file), winning pays no reward (the Win panel shows none) and moves no progress, and lives are neither checked nor spent (no retry counted, no level-in-progress mark). Its popups still show so they can be tested: Are You Sure before leaving, and the tutorial on every visit, never saved as seen.
- Pressing Play in either scene in the Editor must work (persistent services auto-create).

## Persistent player data (one save, survives scenes and restarts)
| Field | Start value | Notes |
|---|---|---|
| CurrentLevel | 1 | Advances when the current level is won. After level 100 is won: stays 100, `AllLevelsComplete = true`. Replays (level 100 after the run) pay the reward but never move progress; editor level jumps record nothing. |
| Coins | 1000 | +20 / +60 / +100 per win of a regular / hard / super hard level (the level's `tier`). Play on costs 900; boosters and hearts can be bought (Buying, below); the Shop's bundles add coins. |
| Lives | 5 (full) | 0 to the maximum (5). Spent and refilled as described in Lives, below. |
| Life timer | none | UTC time the current wait for a life began; kept only while lives are below the maximum. |
| Boosters: Undo / Shuffle / AddTube | 99 / 99 / 99 | Global totals, spent in any level, no refills. |
| Retry count / last variant | 0 / none | Attempts at the current level that cost a life since it was last won, and the variant the latest attempt played (docs/levels.md, Variants and retries). Reset by a win. |
| Level in progress | none | The level being played: set when it starts, cleared by a win or a confirmed Home / Give up. Still set at launch → the app was closed mid-level (Lives, below). |
| Player name | `Player` | Set in the avatar menu; 1–16 characters, trimmed. |
| Avatar | 1 | Picked in the avatar menu; ids 1–5 (`AvatarRules`), pictures in `AvatarConfig`. |
| Tutorials seen | none | Ids of the levels whose tutorial popup has been dismissed. |
| Settings: Sound / Music / Haptics | on / on / on | Saved. Sound mutes every sound effect (`AudioService`); Haptics silences `HapticsService`, whose calls are in place but vibrate nothing yet; Music has no effect (no music). |

All numbers live in an `EconomyConfig` ScriptableObject, not in code, including the win reward of each tier, the lives maximum and the refill interval, every price, the boosters per purchase and the Shop's coin bundles. Progress and rewards are saved **at the moment of the win**, before any celebration or panel.

**Save format:** one versioned JSON blob (`SaveData`) in PlayerPrefs under `BallSort.Save`, written on every change and on pause/quit. Every field is required: a corrupt save, a save with missing fields, or one from a newer version is replaced by defaults with a warning. Out-of-range values are clamped on load. Bump `SaveData.CurrentVersion` and add a migration to `SaveJsonCodec` when the shape changes (version 2 added the try fields; a version 1 save migrates with try 0 and no last variant. Version 3 adds the life timer, the level-in-progress mark, the player name and the tutorials seen, and turns the try count into the retry count: a version 2 save keeps its count as retries, starts the life timer at load if lives are below the maximum, and gets no mark, the name `Player` and no tutorials seen. Version 4 adds the avatar: an older save gets avatar 1; an id without an avatar is replaced by 1 on load). Editor tools: **Ball Sort ▸ Reset Save** (disabled in Play mode) and **Ball Sort ▸ Player Data…** (set coins, lives, the time to the next life, booster counts, the player name and the avatar, jump to any level (retries cleared), and clear the tutorials seen; it also shows the level, retries, last variant, level in progress and tutorials seen; edits the save in Edit mode, the live player in Play mode).

## Home screen
- **Top bar:** avatar (top-left: the picked avatar in its frame, the same in every episode; mascot frames are a Level scene thing), lives (the count on the art's heart, the time to
  the next life on its pill) and coins (the count on the pill next to the art's coin, on one line: it shrinks to fit and
  is abbreviated, e.g. 125K or 1.2M, only once it would get too small, `AmountLabel`) between the corners, settings
  button (top-right). A green plus (`Icon_Plus`) sits on the right end of the lives pill while lives are below the
  maximum (it opens Refill hearts) and always on the coins pill (it opens the Shop tab); Buying, below.
- **Center:** big **Level button** (green) showing `Level N`, low on the page just above the bottom bar (about a quarter
  of the screen up), so the episode illustration stays visible. If `AllLevelsComplete`: shows `All levels complete` and
  replays level 100. Its look depends on the level's tier (`LevelTierLook`): regular levels show the green button;
  hard and super hard levels show their tier button (`Button_Hard`, purple, and `Button_SuperHard`, red; drawn over
  the button as `TierArt`, the pill stretched to the green button's size; both share one canvas, so the tag sits in the
  same place) with the reward multiplier written on its tilted tag at the tag's angle: "x3" for hard, "x5" for super
  hard (rewards 60 and 100 against 20). At 0 lives it opens the More Lives popup instead of loading the level.
- **Bottom nav:** Shop · Home · Leaderboard, the order of the bar art. One bar image in three versions, each with its
  tab raised; selecting a tab swaps the bar to its version. Three invisible tap areas match the drawn sections
  (41 / 19 / 41 % of the bar's width). Home is selected on arriving. Every tab stays interactable (`HomeTabs` enforces it).
  The bar spans the full screen width on any aspect ratio (`BottomBar`): it sits outside the safe area on the screen's
  bottom edge, scaled uniformly to 112 % of the screen's width (the raised Shop and Leaderboard tabs round in 5.7 % per
  side, so every version's ends are off-screen) and sunk 3.5 % of its height below the edge (its rounded bottom
  corners). The art may reach into the bottom safe-area inset; the tap areas start above it, clear of the home
  gesture. Its height follows the screen's width, so the Home page keeps its bottom 8 units above the bar
  (`KeepAboveBar`).
  Leaderboard and Shop swap the center for their page; Home brings the Level button back. The top bar stays on every tab.
  - **Shop page:** a vertical scroll view that runs down behind the bar to the screen's bottom edge, so items
    scroll out from behind it (hold and swipe); its content's bottom padding is the bar's height plus 40, so the last
    item scrolls fully into view (`ScrollBehindBar`; the Leaderboard page can use the same structure): the shop header
    ("Shop"), the shop panel with the six **coin bundles** in its frames (`ShopPage`, `ShopItem`), and below it three
    copies of the bundle card with a name and a price (static: they do nothing). Bundles, top left to bottom right:
    1,000 / 5,000 / 10,000 / 25,000 / 50,000 / 100,000 coins, labelled 99,99 TL / 399,99 TL / 799,99 TL / 1499,99 TL /
    2999,99 TL / 4999,99 TL (display only, `EconomyConfig`). Each shows its picture (`Icon_Coin`, `Icon_CoinPouch`,
    `Icon_CoinChest`, `Icon_CoinChestFull`, `Icon_CoinTank`, `Icon_CoinTankFull`, in that order), its coins and its
    price in black on a yellow tag (shrinks to fit inside the tag); a tap adds the coins at once (no real payment) and
    toasts "+10,000 coins". Each card shows, left to right above its price, a coin icon (`Icon_Coin`, `Icon_CoinChest`,
    `Icon_CoinTank` for cards 1–3), the no-ads icon and its name; its price is black too.
  - **Leaderboard page** (`LeaderboardPage`): the Shop's structure, a scroll view running behind the bar
    (`ScrollBehindBar`): the leaderboard header ("Leaderboard" written on the plate), then the leaderboard panel,
    9-sliced so its box stretches down to fit the rows. Rows, top to bottom: the rank (1 to N) in the circle on the
    left, the avatar in the ring, the name. Rank 1 is always the player, with their saved name and avatar, on the
    player's row art (`Leaderboard_RowMe`); below come 10 bots on the plain row (`Leaderboard_Row`), fixed in the
    `LeaderboardConfig` asset (family-friendly names, avatar ids picked at random once from our 5). No backend and no
    scores. The top 3 avatars also sit on the discs of the three tubes drawn at the top of the panel: rank 1 on the
    tall middle tube, 2 on the left, 3 on the right. The page refreshes each time it is shown (name and avatar change
    in the avatar menu). Rows are made once, from a template, when Home starts.
- **Avatar** → **avatar menu** (`AvatarMenuPopup`, the `AvatarMenu_Panel` art): top, the picked avatar in its frame,
  the name to its right and the pen button to the right of the name. The name is read-only and takes no taps until
  the pen is tapped, then editable in place (no popup); Enter or a tap anywhere else ends the edit and saves it (trimmed, 1–16 characters;
  a refused name goes back to the saved one). Bottom, a 3 × 3 grid of frames: the first five hold avatars 1–5, a tap
  picks one (saved at once, shown on Home's avatar), the picked one in the green selected frame; the other four show the empty
  frame and cannot be picked. **Close** (✕) closes.
- Settings popup (Home): Sound, Music, Haptics toggles (Settings popup, Shared UI pieces).

## Level screen
- **Top-left:** the episode's mascot frame (display only, for now; it hangs below the top bar, clear of the board).
  **Top-center:** `Level N` (shrinks rather than reach the frame or the button). **Top-right:** settings button, as on
  Home. No avatar, coins, or lives here.
- **Middle:** the board (tubes laid out on the level's grid).
- **Bottom:** booster bar — Undo, Shuffle, Add tube, each drawn by its button art (`UI/Boosters/Booster_*`, 150×152,
  no text label) — each with its remaining count on the badge at its top right corner, or at 0 a green plus in the
  count badge's place (no separate button): tapping that booster opens the Buy popup.
- **Input:** tap a tube to select it (its top ball lifts above the mouth; the rest of the run follows when it moves), tap it again to deselect, tap another tube to
  move. An illegal move, or a tap on a tube that can't be selected, plays invalid-move feedback and clears the
  selection. Tapping empty space deselects. The selection is also dropped on undo, add tube, Play on, restart,
  entering/leaving stuck and the win.
- Settings popup (Level): Sound, Music, Haptics toggles + **Restart** + **Home** side by side on the panel's bottom strip, both behind Are You Sure (Lives, below). Gameplay input is blocked while it is open.

### Boosters
- Boosters work only while playing: while stuck the bar is hidden, and after a win (and during a shuffle pick) it is
  locked, so nothing in it reacts, buying included.
- **At 0** a booster shows the green plus instead of its count, is never dimmed, and a tap opens the **Buy popup**
  (Buying, below), whatever else would refuse it.
- Otherwise, unavailable boosters look dimmed but still react to taps with a short toast explaining why:
  - Undo: "Nothing to undo" / "Can't undo past a completed tube".
  - Add tube: "No room for more tubes".
  - Shuffle: "No tube to shuffle" (no tube is eligible, docs/mechanics.md), with the invalid-move sound.
- Reason precedence (first match wins), so the toast names the real obstacle rather than the booster count:
  - Undo: Stuck → no moves → sealed → no boosters.
  - Add tube: level won → Stuck → no room → no boosters.
  - Shuffle: level won → Stuck → no boosters → no eligible tube.
- **Add tube:** appends an empty tube of capacity 4. Not undoable, doesn't seal undo history. No room once every row already has 8 cells ("No room for more tubes").
- **Shuffle:** tapping it enters the **pick** (flow state `ShufflePicking`): the selection is dropped, the tubes it can
  shuffle are highlighted (the glow in `TubeVisualConfig`'s highlight tint) and the rest dimmed with their balls and
  covers (`TubeView.SetHighlight`, `BoardView.ShowShufflePick`), and the booster bar and settings are locked and let
  taps through (`LevelHud.SetTapsPassThrough`). The next tap anywhere ends the pick: an eligible tube is shuffled into a
  different order (rules in docs/mechanics.md) and one Shuffle is spent; anything else (another tube, empty space, any
  button, Shuffle again) only cancels: it spends nothing and the button tapped does nothing. A
  shuffle clears the undo history, so Undo then shows "Nothing to undo"; it is not undoable, and it may leave the board
  stuck. The tube's balls hop into their new places (docs/animation-spec.md, 4).
- **Restart** (settings, behind Are You Sure; it costs a life) reloads the level from data in its next variant: hidden balls hidden again, added tubes gone, undo history cleared. Boosters already spent stay spent.

### Stuck (not won, and nothing useful left to do)
- Checked after every move, undo, added tube, Play on, shuffle and restart.
- Stuck = nothing meaningful left to do: no legal move, or only moves that go round in a loop (rule in docs/mechanics.md, Win and stuck).
- Show **"No more useful moves"** above the board (`StuckPanel._bannerText`, the one place to change it; does not cover or block the tubes).
- The boosters lock as soon as the level is stuck (no booster, count, green plus or Buy works while stuck); the stuck bar
  replaces the booster bar once the last move's balls have settled, fading in with the invalid-move sound
  (docs/animation-spec.md, 5.1). Tubes can still be selected.
- Two buttons side by side in the booster bar's place: **Give up** (behind Are You Sure; it costs a life) and **Play on ● 900** (the coin before the price, `StuckPanel._playOnFormat`).
  - Play on: spend 900 coins, add one empty capacity-4 tube, leave the stuck state. It continues the same attempt and costs no life.
  - Play on is disabled (still showing its price) if coins < 900 or every row already has 8 cells. Give up is then the only choice.

### Win
- Input locks, progress + reward are saved and the level-in-progress mark is cleared (one save), short placeholder celebration (~1 s, e.g. "Well done!" text), then the **Win panel**: no panel art, the win picture (`UI/Win/Win_Picture`, the scientist hugging the monsters) in the middle of the dimmed screen, "Level N complete" just above it (the art has no banner or ribbon to write on, so the title never crosses the picture's edge), `+20 ●` (the tier's reward) and **Continue** → Home below it. Static for now (its animation is next). A win never costs a life.

## Buying
Prices and amounts are in `EconomyConfig`; the purchases are `PlayerData.TryBuyBooster`, `TryRefillLives` and `AddCoins`
(each saved in one write).
- **Buy popup** (`BuyPopup`, one per scene, over everything but the toast): a title, a picture of what is bought, a
  yellow button showing only the price next to `Icon_Coin`, and the ✕.
  - **Boosters** (Level, from a booster at 0): "Buy Undo" 300, "Buy Shuffle" 300, "Buy Add tube" 900. Under the title,
    the coin balance in a small copy of Home's coins pill (`CoinsDisplay`, bound to the wallet while shown, so it is
    always current); then the booster's picture (`BoosterBar._undoPicture` / `_shufflePicture` / `_addTubePicture`:
    the booster button art, the same as in the bar). One purchase adds 1 booster and closes the popup, and the badge
    shows the count again.
  - **Hearts** (Home, from the plus on the lives pill, shown only below the maximum): "Refill hearts" with `Icon_Heart`
    as the picture (no coin balance: Home's top bar shows it), 900 refills the lives to the maximum and stops the
    timer. If they filled up while the popup was open, it closes with "Lives are already full" and nothing is spent.
- **Bought:** the reward jingle, and what was filled punches (the booster's count badge, the lives heart); a Shop coin
  bundle plays the coin sound and punches the coins pill (docs/animation-spec.md, Boosters, buttons and purchases).
- **Not enough coins:** the invalid-move sound, the toast "Not enough coins", and the popup stays open.
- **Coins:** the plus on Home's coins pill opens the Shop tab, whose bundles add coins (Home screen, Shop page).

## Lives
- **Maximum 5**; below it, **one life comes back every 30 minutes** until full (both numbers in `EconomyConfig`). Lives never go below 0.
- The refill is computed from the saved life timer, never counted while the app runs, so it carries on while the app is
  closed: lives gained = whole intervals since the timer started (up to the maximum), and the timer moves forward by
  that many intervals. Reaching the maximum stops the timer; spending a life at the maximum starts it at that moment.
  Time to the next life = timer + interval − now. If now is earlier than the timer (the device clock was turned back),
  the timer restarts from now: the wait starts over and no life is gained.
- **Every exit from a level before a win costs a life**, whatever moves were made: settings → Home, settings → Restart,
  stuck → Give up, and closing the app mid-level (below). A win never costs one, and neither does Play on. Each such
  exit also counts one retry (docs/levels.md, Variants and retries); at 0 lives it still counts the retry, and lives
  stay at 0.
- **Are You Sure popup:** settings → Home, settings → Restart and stuck → Give up each open it first: "Are you sure?", a
  broken heart (`Icon_HeartBroken`) and "You will lose 1 life!" (the heart and the line are left out at 0 lives). The
  confirm button is named for the action: **Quit** for Home and Give up, **Restart** for Restart
  (`AreYouSurePopup._quitText` / `_restartText`); it spends the life, then does the action. **Close** (✕) →
  back to where it came from: the settings popup (still open) or the stuck bar. Nothing is spent before Confirm.
- **Closing the app mid-level:** when a level starts, the save gets a level-in-progress mark naming the level. A win or a
  confirmed Home / Give up clears it in the same save that records them; a confirmed Restart keeps it for the new
  attempt. On the next launch, before any screen shows, a mark still set means the attempt ended with neither a win nor
  a normal exit (the player closed the app, or the OS killed it in the background): lives −1 (never below 0), +1 retry
  for that level, mark cleared. Pausing and resuming the app costs nothing.
- **0 lives:**
  - Home: the Level button opens the More Lives popup instead of loading the level.
  - Level (lives reach 0 there when Restart spends the last one): **Restart** opens the More Lives popup instead of
    restarting, with no Are You Sure first; its Close goes back to the settings popup. **Home** and **Give up** still
    work, through Are You Sure as usual, and lives stay at 0. Once a life comes back, Restart works again.
- **More Lives popup:** a heart (`Icon_Heart`) with the lives count on it, and under it two lines, "Time to next life"
  and the wait on its own (`12:34`), or "Your lives are full!" at the maximum, both refreshed every second, and **Close** (✕)
  → back to Home or the settings popup. There is no way to get lives faster yet.
- **Home display:** `♥ N` and the time to the next life as `mm:ss`, or `Full` at the maximum, refreshed every second.

## Episodes
- The run is split into episodes, each with its own Home background, Level background and mascot frame (the scientist
  in a frame). An `EpisodeConfig` ScriptableObject lists each episode's first level and those three sprites; currently
  5 episodes: levels 1–19, 20–39, 40–59, 60–79 and 80–100 (first levels 1, 20, 40, 60, 80). Nothing else assumes a
  length: backgrounds, mascot frames and the Episode Unlocked check all follow the first levels.
- The current episode is derived from the current level (the last episode whose first level is not after it), never
  stored. Home shows the episode of `PlayerData.CurrentLevel`; the Level scene shows the episode of the level being
  played: its background, and its mascot frame at the top left. Home never shows a mascot frame except in the Episode
  Unlocked overlay.
- **Episode Unlocked:** Continue on the Win panel of a win that moved `CurrentLevel` into a new episode (winning level
  19, 39, 59 or 79; `EpisodeMap.FindUnlocked`) brings up Home on the previous episode's background. About 0.7 s later (`HomeFlow._episodeUnlockedDelay`) the
  Episode Unlocked overlay appears ("Episode N unlocked" and the new episode's mascot frame) and the Home background
  switches to the new episode behind it. A tap anywhere closes the overlay; Home input is blocked from arriving until
  then. The Level scene shows the new episode from then on, as it always shows the played level's episode. The
  hand-off from the Win panel to Home is kept in memory (`AppRoot`), not saved: if the app closes in between, Home
  simply opens on the new episode. Replays and editor test visits never move progress, so they never show it.

## Tutorials
- A level whose file sets `tutorial: true` (docs/levels.md) shows a tutorial popup the first time the player enters it,
  over the loaded board: a short text and an optional image, from the level's entry in the `TutorialConfig`
  ScriptableObject (entries keyed by level id). A tap anywhere dismisses it; until then everything else (tubes,
  boosters, settings) is blocked.
- "Seen" is saved per level id when the popup is dismissed, so it never shows again for that level (Restart, later
  attempts, replays). Closing the app before dismissing it shows it again on the next entry.
- `TutorialConfig` has an entry for each tutorial level (1, 7, 20, 30, 55, 80) with product's text
  (docs/design/TEXT_CHANGES.md, section 3): level 1 one line; each mechanic a main line with its key word in capitals
  and highlighted (`<color=#FFD23A>`), then a hint line at 75 % size. A non-breaking space keeps the key word with the
  word before it, so it never wraps onto a line alone. The richer New Item card (name, "New Item Unlocked!", picture) is
  not built. Product edits the text in the asset, with no code change. A tutorial level without an entry shows
  no popup and logs a warning; `TutorialConfig.OnValidate` warns about duplicate level ids and empty texts.
- Editor test visits show the popup on every visit and never save it as seen.

## Shared UI pieces
- **Toast:** short message, auto-hides, never blocks input. A new message replaces the current one instead of queueing.
- **Popup:** modal with dimmed background; blocks input beneath it. Open = GameObject active. Draw order is hierarchy order: screen content, then popups, then the toast last. Popups can stack: Are You Sure and More Lives open over the settings popup, and only the topmost takes input. The tutorial popup and the Episode Unlocked overlay close on a tap anywhere; the others have buttons. A popup that follows the player data subscribes while open (`OnEnable`/`OnDisable`): a popup never opened never runs `OnDestroy`, and the player data outlives the scene.
- **Popup panels:** every popup uses the popup panel art (`Popup_Panel`, 9-sliced to fit its content) except Settings
  (`Settings_Panel`, fixed art) and the avatar menu (its own panel).
- **Settings popup** (one prefab, both scenes): the settings panel art with "Settings" in its header lip, and Sound,
  Music and Haptics written on the three bars next to their icons. Each has a switch in the track to the right of its
  bar (`SettingSwitch`, driven by `SettingToggle`): one knob, on the left with `Toggle_Off` when off, on the right with
  `Toggle_On` when on, never both. A tap slides the knob across (0.18 s DOTween, unscaled) and swaps its sprite halfway;
  syncing from the save snaps it.
- **Coin icon in text:** the Win reward (`+20 ●`) and the Play on price draw `Icon_Coin` inline through a TMP sprite
  asset (`Art/Atlases/Icon_Coin_TMP.asset`, `<sprite name="Icon_Coin">` in `WinPopup._rewardFormat` and
  `StuckPanel._playOnFormat`); the label needs that sprite asset assigned.
- Buttons (art, 9-sliced so they stretch to any width): green for Continue and the Level button; red for Give up, Are
  You Sure's Confirm and Settings' Restart and Home; yellow for Play on, Buy and the shop's prices; the round ✕ closes every
  popup; the round gear is both settings buttons; the round pen edits the name; the booster buttons are square art with their icon (no label).
- Canvas Scaler 1080×1920, match 0.5; `SafeArea` on root panels.
- **Text:** every label is Baloo 2 Bold (`Baloo2-Bold SDF`, the TMP default; FONTS.md's ExtraBold read too heavy under
  an outline), a static atlas (docs/art-inventory.md, section 9); a typed name's other characters come from the
  device's system fonts (`SystemFontFallback` on `AppRoot`).
- **Text look:** FONTS.md's style through TMP material presets (docs/art-inventory.md, section 9): white fill, a dark
  outline and a solid shadow straight down, deep purple on panels and backgrounds, the art's own darker colour on
  green, red and yellow buttons and pills. Pick the preset by what the label sits on; never edit a label's material
  directly (that makes a per-label copy).
- **Text fit:** no label may leave its button, panel or field at any aspect ratio. Every label in fixed space
  auto-sizes, from its design size down to about 60 % of it (single-line labels don't wrap; the toast and the
  tutorial text wrap, and the tutorial popup grows with its text). Single-line auto-sized labels keep side margins (4–12 units) wide
  enough for the outline, because TMP's auto-size measures the glyphs only, not their outline or Baloo's overhang. On the 9-sliced button
  art (green, red, yellow), the bottom lip is part of the rect, so each label (or the Buy button's coin-and-price row)
  has a bottom inset of 10 % of the button's height, which centres it on the face. Checked at 9:16, 9:20, 9:21 and 3:4
  with the longest values: `Level 100`, "All levels complete", a 16-character `W` name, 999 boosters, the longest toast
  and every tutorial text.
- **Art slots:** per-episode art (Home and Level backgrounds, mascot frame) is in `EpisodeConfig`, the avatar pictures in
  `AvatarConfig`, the tutorial image in `TutorialConfig`, the bottom bar's three versions on `HomeTabs`, the tier
  buttons on `LevelTierLook`, the switch knobs on each `SettingSwitch`, the leaderboard rows on `LeaderboardPage`, the
  Buy popup's booster pictures on `BoosterBar` and its heart on `HomeScreen`, and everything else is an `Image` on its
  screen or popup object. Which slots hold final art and which still hold placeholders (Unity's built-in UI sprites,
  tinted, and generated ones in `Art/Sprites/Placeholder/UI/`: the heart on Home's lives pill)
  is listed in docs/art-inventory.md. Home backgrounds exist for
  episodes 2 and 5; episodes 1, 3 and 4 use episode 2's until theirs arrive.
