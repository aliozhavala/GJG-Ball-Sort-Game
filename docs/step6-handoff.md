# Step 6 handoff: art integration and animations

State at the end of roadmap 7c (2026-10-30). Every sprite below already has a slot; final art drops in without
code changes unless a row says otherwise. Placeholders: Unity's built-in UI skin (`UISprite`, `Knob`, `Background`,
`Checkmark`, `InputFieldBackground`), tinted, plus the files in `Assets/Art/Sprites/Placeholder/`.

## 1. Art slots

### Board (Level scene, world space)
Prefab sprite renderers (`Ball`, `Tube`, `Cover`) are filled from the configs at runtime: replace art in the configs.

| Slot | For | Placeholder |
|---|---|---|
| `ColorPalette._entries[0..11]._sprite` (+ tint) | Ball body per colour id | `Ball_Circle`, tinted |
| `ColorPalette._defaultSprite` | Ball of a colour id with no entry | `Ball_Circle` |
| `ColorPalette._hiddenSprite` | Hidden ball body | `Ball_Circle` |
| `ColorPalette._hiddenMarkSprite` | "?" on a hidden ball | `Hidden_Mark` |
| `ColorPalette._keyMarkSprite` | Key on a keyed ball (tint from `LockPalette`) | `Key_Icon` |
| `TubeVisualConfig._backSprite` / `_frontSprite` | Tube back / glass front (9-sliced) | `Tube_9Sliced` |
| `TubeVisualConfig._capSprite` | Cap on a completed tube | `Tube_9Sliced` |
| `TubeVisualConfig._claySprite` | Clay overlay at the bottom of clay tubes | `Tube_9Sliced` |
| `TubeVisualConfig._veilSprite` / `_veilBadgeSprite` | Veil body / badge in the veil's colour | `Tube_9Sliced` / `Veil_Badge` |
| `TubeVisualConfig._doorSprite` / `_lockIconSprite` | Lock door / lock icon (tint from `LockPalette`) | `Tube_9Sliced` / `Lock_Icon` |
| `LockPalette` | Lock colours (4, ids 0–3); colours only | — |
| `Tube.prefab/Flash` | Invalid-tap flash (VFX layer, `AnimationConfig._invalidFlashColor`) | `Tube_9Sliced` |
| `EpisodeConfig._episodes[n]._levelBackground` → `Level.unity/Background` | Level background per episode | `Episode1–5_Level` gradients |

### Level HUD (`LevelCanvas`)
| Slot | For | Placeholder |
|---|---|---|
| `EpisodeConfig._episodes[n]._mascotFrame` → `TopBar/MascotFrame` | Scientist in a frame, top right | `Episode1–5_Mascot` (generated) |
| `TopBar/SettingsButton` | Settings button | `UISprite` + "≡" text (no icon slot yet) |
| `BoosterBar/{Undo,Shuffle,AddTube}Button` + `/Badge` | Booster buttons and count badges | `UISprite` + text labels, `Knob` badge (no icon slots yet) |
| `StuckPanel/Buttons/{GiveUp,PlayOn}Button` | Stuck bar buttons | `UISprite` |
| `Celebration` | Win celebration | text only |
| Popups (`Prefabs/UI`): `SettingsPopup`, `WinPopup`, `AreYouSurePopup`, `MoreLivesPopup`, `TutorialPopup`, `Toast` | Panels, buttons, toggles (`Background`/`Checkmark`), close buttons; `MoreLivesPopup/Panel/Heart` | `UISprite`; heart = `Heart.png` (generated) tinted #E53935 |
| `TutorialConfig._entries[i]._image` → `TutorialPopup/Panel/Picture` | Optional tutorial picture (hidden when empty) | none |

### Home (`HomeCanvas`)
| Slot | For | Placeholder |
|---|---|---|
| `EpisodeConfig._episodes[n]._homeBackground` → `Background` | Home background per episode | `Episode1–5_Home` gradients |
| `TopBar/AvatarButton` | Avatar (opens Edit Profile) | `UISprite` + "You" text (no picture yet) |
| `TopBar/Lives` + `/Heart` | Lives pill and heart with the count on it | `UISprite`; `Heart.png` tinted |
| `TopBar/Coins` | Coins pill | `UISprite` + "●" text (no coin icon slot yet) |
| `TopBar/SettingsButton` | Settings | `UISprite` + "≡" |
| `HomePage/LevelButton` + `/TierBadge` | Level button (tinted per tier by `LevelTierLook`) and tier badge | `UISprite` |
| `BottomNav` and each tab's button, `/Icon`, `/Highlight` | Tab bar, tab icons, selected-tab bar | bar colour only; `UISprite`; `Knob` tinted gold/blue/green; `UISprite` #FFC107 |
| `LeaderboardPage` / `ShopPage` `/Panel`, `/RowN` | Placeholder pages | `UISprite` |
| `EditProfilePopup` (panel, `NameField`, Save, Close), `MoreLivesPopup`, `EpisodeUnlockedOverlay` (`/Panel/Mascot` ← episode mascot), `SettingsPopup`, `Toast` | Home popups | `UISprite`, `InputFieldBackground` |
| Every popup root | Dimmed full-screen background | black at 60 % (Episode Unlocked 75 %) |

## 2. Animation hooks
Rule: game logic never waits on animation. The model applies a move at once and raises events; views animate and must
accept being interrupted (complete or kill a ball's tween before moving it again). Timings and eases already live in
`AnimationConfig` (lift, fly, drop, stagger, reveal flip, invalid shake, cap drop, invalid flash, celebration).

| Animation | Hook (one place each) | Today |
|---|---|---|
| Select / deselect lift | `TubeView.Lift` / `Lower` → `BallView.MoveTo`, `SetRaised` | snap |
| Ball move (fly + drop, staggered) | `BoardView.Transfer` (on `MoveApplied`, `MoveUndone` reversed) → `BallView.MoveTo` | snap |
| Reveal flip | `BallView.Reveal` (on `BallRevealed`) | snap |
| Key mark leaves | `BallView.RemoveKey` → `UpdateKeyMark` (on `KeyFound`) | snap |
| Veil / door opens | `CoverView.SetOpen` (on `TubeUnveiled` / `LockOpened`); it also shows the covered balls | snap |
| Cap drops | `TubeView.SetCapVisible` (on `TubeCompleted`, also after an undo that completes) + `LevelFeedback.PlayTubeCompleted` (sparkle, sound, haptic) | snap / empty |
| Invalid move | `LevelFeedback.PlayInvalidMove` (from `TubeInput.InvalidMove`) | tube flash |
| Celebration | `LevelFeedback.PlayCelebrationAsync` (awaited before the Win panel) | label for 1 s |
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
  (`HomePage`, `LeaderboardPage`, `ShopPage`) sit under `SafeArea` and are switched by `HomeTabs` (page + highlight).
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
- Missing image slots: coin icon ("●"), settings gear ("≡"), booster icons (text labels), avatar picture ("You").
- Leaderboard and Shop are placeholders; More Lives offers no way to get lives.
- Consider adding "start position not stuck" to `LevelValidator` (today only `ShippedLevelTests` checks it).
- Re-measure the stuck check and the shuffle search on a device.

Known issues:
- `Level.unity` and `Home.unity` are often marked dirty with no change (Editor quirk; save is a no-op).
- Opening `Level.unity` as a preview scene while it is open logs "More than one global light…" (tooling only).
- `Popup.IsOpen` is the GameObject's active state: animated closes must keep input blocked until the close ends.
- Stacked popups: the settings panel shows around Are You Sure / More Lives (by design; art may prefer to hide it).
- The mascot frame (136×170) nearly fills the 180-high top bar.
- Lives refill uses the device clock, so moving the clock forward grants lives (offline game; accepted).
- In the Editor, stopping Play mode mid-level counts as closing the app and costs a life on the next Play.
