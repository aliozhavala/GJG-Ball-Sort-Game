# Step 6 handoff: art integration and animations

State at the end of roadmap 7c (2026-10-30), updated after the first art delivery (6a, same day). Every sprite below has
a slot; final art drops in without code changes unless a row says otherwise. Delivered art lives in
`Assets/Art/Sprites/` (prepared with **Ball Sort ▸ Art ▸ Prepare Sprites**; inventory, problems and missing art in
docs/art-inventory.md). Placeholders: Unity's built-in UI skin (`UISprite`, `Knob`, `Background`, `Checkmark`,
`InputFieldBackground`), tinted, plus the files in `Assets/Art/Sprites/Placeholder/`.

## 1. Art slots

### Board (Level scene, world space)
Prefab sprite renderers (`Ball`, `Tube`, `Cover`) are filled from the configs at runtime: replace art in the configs.

| Slot | For | Now |
|---|---|---|
| `ColorPalette._entries[0..9]` `_open` / `_halfOpen` / `_closed` / `_laugh` | Monster per colour id, one sprite per `BallExpression` (sprite tint white, `_spriteSize` 1.46: the art is wider than the body) | **Art** (`Balls/Ball_<Colour>_<Face>`) |
| `ColorPalette._entries[10..11]` | Colour ids without art | `Ball_Circle`, tinted |
| `ColorPalette._entries[n]._color` | The colour itself (veil badge) | sampled from the art |
| `ColorPalette._hiddenSprite` (+ `_hiddenSpriteSize` 1.31) | Hidden ball, "?" drawn in | **Art** (`Ball_Hidden`) |
| `ColorPalette._hiddenMarkSprite` | Separate "?" | empty (the art has it) |
| `ColorPalette._keyMarkSprite` | Key on a keyed ball (tint from `LockPalette`) | `Key_Icon` |
| `TubeVisualConfig._topSprite` / `_middleSprite` / `_bottomSprite` | Tube, middle stretched to the tube's height | **Art** (`Tubes/Tube_*`) |
| `TubeVisualConfig._outlineSprite` | Whole tube, 9-sliced; the invalid-tap flash | **Art** (`Tube_Full`) |
| `TubeVisualConfig._frontSprite` | Glass front over the balls (optional) | empty (missing art) |
| `TubeVisualConfig._capSprite` | Cap on a completed tube | `Tube_9Sliced` |
| `TubeVisualConfig._claySprite` | Clay (ice) at the base, behind the balls, at the tube's drawn width | **Art** (`Mechanics/Clay_Ice`) |
| `TubeVisualConfig._veilSprite` / `_veilBadgeSprite` | Veil hanging from the rim / badge in the veil's colour | **Art** (`Mechanics/Veil_Towel`) / `Veil_Badge` |
| `TubeVisualConfig._doorSprite` / `_lockIconSprite` | Lock door / lock icon (tint from `LockPalette`) | `Tube_9Sliced` / `Lock_Icon` |
| `LockPalette` | Lock colours (4, ids 0–3); colours only | — |
| `EpisodeConfig._episodes[n]._levelBackground` → `Level.unity/Background` | Level background per episode | **Art** (`Backgrounds/Level_Ep<N>_*`) |

### Level HUD (`LevelCanvas`)
| Slot | For | Now |
|---|---|---|
| `EpisodeConfig._episodes[n]._mascotFrame` → `TopBar/MascotFrame` | Scientist in a frame, top left (255×255) | **Art** (`Episodes/MascotFrame_Ep<N>_*`) |
| `TopBar/SettingsButton` | Settings button, top right | **Art** (`Button_Settings`) |
| `BoosterBar/{Undo,Shuffle,AddTube}Button` + `/Badge` | Booster buttons and count badges | `UISprite` + text labels, `Knob` badge (missing art) |
| `StuckPanel/Buttons/GiveUpButton` / `PlayOnButton` | Stuck bar buttons | **Art** (`Button_Red` / `Button_Yellow`) |
| `Celebration` | Win celebration | text only |
| Popups (`Prefabs/UI`) | Close buttons; Win Continue; Are You Sure Confirm | **Art** (`Button_Close`, `Button_Green`, `Button_Red`) |
| Popups (`Prefabs/UI`) | Panels, toggles, Settings' Restart / Home, `MoreLivesPopup/Panel/Heart`, `Toast` | `UISprite`; heart = `Heart.png` tinted |
| `TutorialConfig._entries[i]._image` → `TutorialPopup/Panel/Picture` | Optional tutorial picture (hidden when empty) | none |

### Home (`HomeCanvas`)
| Slot | For | Now |
|---|---|---|
| `EpisodeConfig._episodes[n]._homeBackground` → `Background` | Home background per episode | **Art** for 2 and 5; 1, 3, 4 use episode 2's |
| `TopBar/AvatarButton` | Avatar frame, the same in every episode | `Avatar_Frame_Empty` (the delivered empty frame, standing in) |
| `TopBar/Lives`, `TopBar/Coins` | Displays, sliced; count on the heart / coin side, text on the pill | **Art** (`Display_Lives`, `Display_Coins`) |
| `TopBar/SettingsButton` | Settings | **Art** (`Button_Settings`) |
| `HomePage/LevelButton` + `/TierBadge` | Level button and tier badge (`LevelTierLook` tints are white until tier art exists) | **Art** (`Button_Green`); badge `UISprite` |
| `BottomNav` (`HomeTabs._bar`, `_tabs[n]._barSprite`) | Bar in three versions, invisible tap areas over its sections | **Art** (`UI/Home/BottomBar_*`) |
| `ShopPage/Scroll/Viewport/Content` | Header, panel, three bundle cards | **Art** (`UI/Shop/*`) |
| `LeaderboardPage/Panel`, `/RowN` | Placeholder page | `UISprite` |
| `EditProfilePopup` Save / Close | Buttons | **Art** (`Button_Green`, `Button_Close`) |
| Other Home popups (panels, `NameField`, `EpisodeUnlockedOverlay`, `SettingsPopup`, `Toast`) | Home popups | `UISprite`, `InputFieldBackground` |
| Every popup root | Dimmed full-screen background | black at 60 % (Episode Unlocked 75 %) |

## 2. Animation hooks
Rule: game logic never waits on animation. The model applies a move at once and raises events; views animate and must
accept being interrupted (complete or kill a ball's tween before moving it again). Timings and eases already live in
`AnimationConfig` (lift, fly, drop, stagger, reveal flip, invalid shake, cap drop, invalid flash, celebration).

| Animation | Hook (one place each) | Today |
|---|---|---|
| Select / deselect lift | `TubeView.Lift` / `Lower` → `BallView.MoveTo`, `SetRaised` | **animated** (docs/animations.md 2.2) |
| Ball move (fly + drop, staggered), landing | `BoardView.Transfer` (on `MoveApplied`, `MoveUndone` reversed) → `BallView.MoveTo(BallFlight)`; landing in `BallView` / `TubeView.NotifyLanded` | **animated** (2.3, 2.4) |
| Reveal flip | `BallView.Reveal` (on `BallRevealed`, once the moving balls have left the tube) | snap, timed |
| Key mark leaves | `BallView.RemoveKey` → `UpdateKeyMark` (on `KeyFound`, after the landing) | snap, timed |
| Veil / door opens | `CoverView.SetOpen` (on `TubeUnveiled` / `LockOpened`, after the landing); it also shows the covered balls | snap, timed |
| Cork drops, glow, balls laugh and dance | `TubeView.ShowCompleted` (when the tube's last ball lands, also after an undo that completes) + `LevelFeedback.PlayTubeCompleted` (VFX) and cork seat (hit-stop, punch) | **animated** (2.6) |
| Blinking | `BallView.SetExpression` (`HalfOpen`, `Closed`, back to `Open`); sprites already in `ColorPalette` | not used yet |
| Invalid move | `LevelFeedback.PlayInvalidMove` (from `TubeInput.InvalidMove`) → `BoardView.ShowInvalid` | **animated** (2.5: shake, shock face) |
| Celebration | `LevelFeedback.PlayCelebrationAsync` (awaited before the Win panel) | label for 1 s, once the board has settled |
| Added tube, re-centring | `BoardView.OnTubeAdded` | snap |
| Restart / shuffle | `BoardView.Rebuild` (on `Restarted` / `Shuffled`) | rebuild |
| Stuck bar in / out | `LevelHud.ShowStuck` → `StuckPanel.SetVisible` | snap |
| Popups open / close | `Popup.Open` / `Close` (`Opened` / `Closed` events) | `SetActive` |
| Toast | `Toast.Show` | DOTween fade |
| Episode Unlocked | `HomeFlow.ShowEpisodeUnlocked`: input blocked → 0.7 s → `BackgroundImage.Show` (new episode) + `EpisodeUnlockedOverlay.Show` | snap after the delay |
| Life comes back | `LivesDisplay.Refresh` (count changes) | text |
| Scene transitions | `SceneLoader.Load` (`LoadSceneAsync`) | none |

Event order within one operation (documented on `LevelSession`); animations must play in this order:
- Move or undo: `MoveApplied` / `MoveUndone` → `BallRevealed` → `TubeCompleted` → each `TubeUnveiled` → each `KeyFound`
  then its `LockOpened` → `LevelCompleted`, then `StuckChanged` (only if changed), then `Changed`. So: balls fly, the
  source's new top flips (showing a key it carries), the cap drops, veils open, the key mark leaves, the door opens.
- Added tube (booster or Play on): `TubeAdded` → `StuckChanged` → `Changed`. Shuffle: `Shuffled` → … Restart: `Restarted` → …
- The board finishes every change before the first event, so a handler never sees a half-applied move.

## 3. UI structure
- uGUI + TextMeshPro. One Screen Space Overlay canvas per scene (`HomeCanvas`, `LevelCanvas`), Canvas Scaler 1080×1920,
  match 0.5. Children in draw order: `Background` (full screen, outside the safe area) → `SafeArea` (screen content:
  top bar, pages or HUD, bottom nav or booster bar) → `InputBlocker` (Home) → popup instances → `Toast` last.
- Popups are prefabs in `Assets/Prefabs/UI/`: root = full-screen dim `Image` (blocks input) + `Popup` + the popup's
  component; child `Panel` (`VerticalLayoutGroup` + `ContentSizeFitter`, 820 wide) with TMP texts and buttons; a
  `CloseButton` top right (`LayoutElement.ignoreLayout`). Open = GameObject active, so `Popup.IsOpen` gates input.
  Tutorial and Episode Unlocked put a `Button` on the root as the tap-anywhere area.
- `HomeScreen` / `LevelHud` hold the references; `HomeFlow` / `LevelFlow` drive them; the bootstraps compose. Home pages
  (`HomePage`, `LeaderboardPage`, `ShopPage`) sit under `SafeArea` and are switched by `HomeTabs` (page + the bar's version).
- The board is not UI: world-space sprites fitted by `BoardLayout` into the HUD's `BoardArea` rect.
- Player-facing strings are serialized fields on the components (e.g. `StuckPanel._bannerText`, `WinPopup._titleFormat`).

## 4. Deviations, TODOs, known issues (from the lives / tutorials / stuck session)
Deviations and decisions:
- Stuck search limit is 32 positions, not a few hundred: measured at about 0.1 ms per position at 13 tubes in the
  Editor, so 32 costs 3.5–4.5 ms when reached; real play never explored more than 9 (docs/mechanics.md).
- A stuck three-tube cycle runs A → B, C → A, B → C (one free slot travelling round); the A → B, B → C, C → A board has
  an exit into another loop, so it is correctly not stuck. Add tube is refused while stuck, so its test checks that the
  added tube counts in later stuck checks.
- Player Data window gained Current level → Jump (to test Episode Unlocked) and a status line.
- Generated placeholders for the heart and the mascot frames instead of built-in sprites.
- Editor test visits hide the "You will lose a life." line (nothing is spent).
- Part A choices (docs/screens.md): an exit at 0 lives still counts a retry; a level that fails to load costs nothing;
  Restart at 0 lives opens More Lives directly; the Episode Unlocked hand-off is in memory only; a tutorial is saved as
  seen when dismissed; names are 1–16 characters.

TODOs:
- Final tutorial texts (product) and optional pictures in `TutorialConfig`.
- Missing art (full list for the artist in docs/art-inventory.md): booster icons (text labels), a standalone coin
  ("●" in the Win reward and Play on price), popup panels, door / lock / key / cap, tier looks for the Level button.
- Leaderboard and Shop are placeholders; More Lives offers no way to get lives.
- Consider adding "start position not stuck" to `LevelValidator` (today only `ShippedLevelTests` checks it).
- Re-measure the stuck check and the shuffle search on a device.

Known issues:
- `Level.unity` and `Home.unity` are often marked dirty with no change (Editor quirk; save is a no-op).
- Opening `Level.unity` as a preview scene while it is open logs "More than one global light…" (tooling only).
- `Popup.IsOpen` is the GameObject's active state: animated closes must keep input blocked until the close ends.
- Stacked popups: the settings panel shows around Are You Sure / More Lives (by design; art may prefer to hide it).
- The mascot frame (255×255, top left) hangs below the 180-high top bar, ending above the board area; the stuck banner
  and "Shuffling…" label are inset to clear it.
- Lives refill uses the device clock, so moving the clock forward grants lives (offline game; accepted).
- In the Editor, stopping Play mode mid-level counts as closing the app and costs a life on the next Play.
