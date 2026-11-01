# Step 6 handoff: art integration and animations

State at the end of roadmap 7c (2026-10-30), updated after the first art delivery (6a, same day) and the second
(6c, 2026-10-31: rim halves, corks, avatars and the avatar menu; docs/art-inventory.md §7) and the third (6d, same
day: icons, popup panels, settings switches, tier button, leaderboard; §8). Every sprite below has
a slot; final art drops in without code changes unless a row says otherwise. Delivered art lives in
`Assets/Art/Sprites/` (prepared with **Ball Sort ▸ Art ▸ Prepare Sprites**; inventory, problems and missing art in
docs/art-inventory.md). Placeholders: Unity's built-in UI skin (`UISprite`, `Knob`, `Background`, `Checkmark`,
`InputFieldBackground`), tinted, plus the files in `Assets/Art/Sprites/Placeholder/`.

## 1. Art slots

### Board (Level scene, world space)
Prefab sprite renderers (`Ball`, `Tube`, `Cover`) are filled from the configs at runtime: replace art in the configs.

| Slot | For | Now |
|---|---|---|
| `ColorPalette._entries[0..9]` `_open` / `_halfOpen` / `_closed` / `_laugh` | Monster per colour id, one sprite per `BallExpression` (sprite tint white; every ball sprite drawn at `ColorPalette._artSpriteSize` 1.46: the art is wider than the body) | **Art** (`Balls/Ball_<Colour>_<Face>`, reworked 2026-11-01) |
| `ColorPalette._entries[10..11]` | Colour ids without art | `Ball_Circle`, tinted |
| `ColorPalette._entries[n]._color` | The colour itself (glow, trail, droplets, plain cork) | sampled from the art |
| `ColorPalette._hiddenSprite` | Hidden ball, "?" drawn in, the monsters' size | **Art** (`Ball_Hidden`) |
| `ColorPalette._hiddenMarkSprite` | Separate "?" | empty (the art has it) |
| `LockPalette` entry `_key` (+ `ColorPalette._keyMarkSize` / `_keyMarkOffset`) | Key in the hands of a keyed ball | **Art** (`Mechanics/Keys/Key_<Colour>`) |
| `TubeVisualConfig._rimBackSprite` / `_rimFrontSprite` / `_middleSprite` / `_bottomSprite` | Tube: rim back behind the balls, rim front over them and the cork, middle stretched to the tube's height | **Art** (`Tubes/Tube_*`) |
| `TubeVisualConfig._outlineSprite` | Whole tube, 9-sliced; the invalid-tap flash | **Art** (`Tube_Full`) |
| `TubeVisualConfig._frontSprite` | Glass front over the balls (optional) | empty (missing art) |
| `ColorPalette._entries[n]._cork` | Cork on a tube completed in colour n (untinted) | **Art** (`Tubes/Corks/Cork_<Colour>`, ids 0–9) |
| `TubeVisualConfig._capSprite` | Plain cork, tinted in the colour, for colours without their own (ids 10–11) | **Art** (`Tubes/Corks/Cork_Plain`) |
| `TubeVisualConfig._claySprite` (+ `_clayScale` 1.1) | Clay (ice) at the base, over the balls inside, 1.1× the tube's drawn width; gone on completion | **Art** (`Mechanics/Clay_Ice`) |
| `TubeVisualConfig._veilSprite` (+ `_veilHeightScale` 0.92, `_veilStickerSize`) | Veil hanging from the rim, its hem across the bottom ball; sticker = the colour's open-eyes ball | **Art** (`Mechanics/Veil_Towel`) |
| `TubeVisualConfig._lockSideSprite` / `_lockFaceSprite` (+ `_lockBoxPadding`) | Closed lock box: sides, tiled face (`LockBoxView`) | **Art** (`Mechanics/LockBox/*`) |
| `TubeVisualConfig._lockInsideSprite` / `_lockCoverSprite` | Opening lock box: inside behind the tubes, two cover halves sliding apart (docs/animation-spec.md 3.3) | **Art** |
| `LockPalette` | Per lock id (5, ids 0–4): the box's mid section and the key | **Art** (`LockBox_Mid_<Colour>`, `Key_<Colour>`) |
| `EpisodeConfig._episodes[n]._levelBackground` → `Level.unity/Background` | Level background per episode | **Art** (`Backgrounds/Level_Ep<N>_*`) |

### Level HUD (`LevelCanvas`)
| Slot | For | Now |
|---|---|---|
| `EpisodeConfig._episodes[n]._mascotFrame` → `TopBar/MascotFrame` | Scientist in a frame, top left (255×255) | **Art** (`Episodes/MascotFrame_Ep<N>_*`) |
| `TopBar/SettingsButton` | Settings button, top right | **Art** (`Button_Settings`) |
| `BoosterBar/{Undo,Shuffle,AddTube}Button` + `/Badge` | Booster buttons and count badges | **Art** (`UI/Boosters/Booster_*`, no label); `Knob` badge (missing art) |
| `BoosterBar/*Button/BuyBadge` | Buy badge at 0 | **Art** (`Icon_Plus`) |
| `BoosterBar._undoPicture` / `_shufflePicture` / `_addTubePicture` | Booster picture in the Buy popup | **Art** (the booster buttons) |
| `StuckPanel/Buttons/GiveUpButton` / `PlayOnButton` | Stuck bar buttons | **Art** (`Button_Red` / `Button_Yellow`) |
| `Celebration` | Win celebration | text only |
| Popups (`Prefabs/UI`) | Close buttons; Win Continue; Are You Sure Confirm; Settings' Restart and Home | **Art** (`Button_Close`, `Button_Green`, `Button_Red`) |
| Popups (`Prefabs/UI`) | Panels (`Popup_Panel`; Settings `Settings_Panel`), switch knobs (`SettingSwitch`), More Lives heart, Are You Sure broken heart, Buy popup coin and balance pill | **Art** (docs/art-inventory.md §8) |
| `Toast` | Toast background | `UISprite` |
| `WinPopup/Panel/Picture` | Win picture between the title and the reward (no panel art; static) | **Art** (`UI/Win/Win_Picture`) |
| `WinPopup/Panel/Reward`, `StuckPanel` Play on label | Coin after the amount | **Art** (`Icon_Coin` through the TMP sprite asset `Art/Atlases/Icon_Coin_TMP`) |
| `TutorialConfig._entries[i]._image` → `TutorialPopup/Panel/Picture` | Optional tutorial picture (hidden when empty) | none |

### Home (`HomeCanvas`)
| Slot | For | Now |
|---|---|---|
| `EpisodeConfig._episodes[n]._homeBackground` → `Background` | Home background per episode | **Art** for 2 and 5; 1, 3, 4 use episode 2's |
| `TopBar/AvatarButton` (+ `AvatarFrame`, `/Mask/Picture`) | The picked avatar (`AvatarConfig`) in its frame | **Art** (`AvatarConfig._frame` = `UI/AvatarFrames/Avatar_Frame_Empty`, `UI/Avatars/Avatar_0N`); mask `Placeholder/Ball_Circle` |
| `TopBar/Lives`, `TopBar/Coins` | Displays, sliced; count on the heart / coin side, text on the pill | **Art** (`Display_Lives`, `Display_Coins`) |
| `TopBar/SettingsButton` | Settings | **Art** (`Button_Settings`) |
| `HomePage/LevelButton` + `/TierArt` | Level button; hard / super hard art with the multiplier on its tag (`LevelTierLook`) | **Art** (`Button_Green`, `Button_Hard`, `Button_SuperHard`) |
| `BottomNav` (`HomeTabs._bar`, `_tabs[n]._barSprite`; `BottomBar`) | Bar in three versions, full screen width; invisible tap areas (`/TapAreas`) over its sections, above the safe-area inset | **Art** (`UI/Home/BottomBar_*`) |
| `ShopPage/Scroll/Viewport/Content` | Header, panel, three bundle cards | **Art** (`UI/Shop/*`) |
| `LeaderboardPage` (`Scroll/.../Header`, `Panel`, rows from `RowTemplate`, `Podium1–3`) | Leaderboard page; bots in `LeaderboardConfig` | **Art** (`UI/Leaderboard/*`) |
| `TopBar/Lives/Heart` | Heart under the lives count | `Heart.png` tinted (placeholder; `Icon_Heart` would fit) |
| `AvatarMenuPopup` (`Prefabs/UI`) | Panel, frames, pen, close | **Art** (`AvatarMenu_Panel`, `Button_Pen`, `Button_Close`; frames from `AvatarConfig`: `Avatar_Frame_Empty`, the picked one `Avatar_Frame_Selected`) |
| Other Home popups (`EpisodeUnlockedOverlay`, `SettingsPopup`) | Home popups | **Art** (panels as above); `Toast` `UISprite` |
| Every popup root | Dimmed full-screen background | black at 60 % (Episode Unlocked 75 %) |

## 2. Animation hooks
Rule: game logic never waits on animation. The model applies a move at once and raises events; views animate and must
accept being interrupted (complete or kill a ball's tween before moving it again). Board moves 2.2–2.6, the ice break, the shuffle, the stuck timing and blinking are built:
values, rules and our deviations from product's document are in docs/animation-spec.md, every number in `AnimationConfig`.

| Animation | Hook (one place each) | Today |
|---|---|---|
| Select / deselect lift | `TubeView.Lift` / `Lower` → `BallView.MoveTo` (depth from `BallView.SetMouth`) | **animated** (docs/animation-spec.md 2.2) |
| Ball move (fly + drop, staggered), landing | `BoardView.Transfer` (on `MoveApplied`, `MoveUndone` reversed) → `BallView.MoveTo(BallFlight)`; landing in `BallView` / `TubeView.NotifyLanded` | **animated** (2.3, 2.4) |
| Reveal flip | `BallView.Reveal` (on `BallRevealed`, once the moving balls have left the tube; after a shuffle, once they landed) | **animated** (docs/animation-spec.md 3.1) |
| Key flies, lock box opens | `CoverView.PlayUnlock` → `LockBoxView` (on `KeyFound` then `LockOpened`, after the landing); key leaves via `BallView.TryTakeKey` | **animated** (docs/animation-spec.md 3.3) |
| Veil lifts, idle sway | `CoverView.PlayVeilLift` (on `TubeUnveiled`, after the landing); sway while closed | **animated** (docs/animation-spec.md 3.2) |
| Cork drops, glow, balls laugh and dance | `TubeView.ShowCompleted` (when the tube's last ball lands, also after an undo that completes) + `LevelFeedback.PlayTubeCompleted` (VFX) and cork seat (hit-stop, punch) | **animated** (2.6) |
| Blinking | `BlinkScheduler` (on `BoardView`: one random ball every 2–5 s) → `BallView.TryBlink` (`HalfOpen` → `Closed` → `HalfOpen` → resting face); sprites in `ColorPalette` | **animated** (docs/animation-spec.md 6.1) |
| Invalid move | `LevelFeedback.PlayInvalidMove` (from `TubeInput.InvalidMove`) → `BoardView.ShowInvalid` | **animated** (2.5: shake, shock face) |
| Celebration | `LevelFeedback.PlayCelebrationAsync` (awaited before the Win panel) | label for 1 s, once the board has settled |
| Added tube, re-centring | `BoardView.OnTubeAdded` | snap |
| Restart | `BoardView.Rebuild` (on `Restarted`) | rebuild |
| Shuffle (one tube) | `BoardView.OnShuffled` (on `Shuffled`: the tube, its new order and `FromSlots`) → `TubeView.Reorder`, `BallView.MoveTo(BallFlight)` / `Wiggle` | **animated** (docs/animation-spec.md 4) |
| Shuffle pick | `BoardView.ShowShufflePick` → `TubeView.SetHighlight` (glow on eligible tubes, the rest dimmed) | snap |
| Stuck bar in / out | `LevelHud.ShowStuck` → `StuckPanel.SetVisible`, once the board settles (`LevelFeedback.WaitForStuckRevealAsync`) | **fades in** (docs/animation-spec.md 5.1); out: snap |
| Popups open / close | `Popup.Open` / `Close` (`Opened` / `Closed` events) | `SetActive` |
| Toast | `Toast.Show` | DOTween fade |
| Episode Unlocked | `HomeFlow.ShowEpisodeUnlocked`: input blocked → 0.7 s → `BackgroundImage.Show` (new episode) + `EpisodeUnlockedOverlay.Show` | snap after the delay |
| Life comes back | `LivesDisplay.Refresh` (count changes) | text |
| Scene transitions | `SceneLoader.Load` (`LoadSceneAsync`) | none |

Sounds: `AudioService` (on `AppRoot`, passed by the bootstraps to `LevelFlow` → `LevelFeedback`, `LevelHud` and
`HomeFlow` → `HomeScreen`), `Play(Sfx, pitch)`; one slot per sound in `SfxConfig`. Hooked so far: select, deselect,
land, invalid, completion chime, cork (from `LevelFeedback`, on `BoardView`'s presentation events), the three booster
sounds, button, Play, reward jingle, coin gain and life lost (docs/animation-spec.md, Boosters, buttons and purchases).
the three lock box sounds (3.3), reveal (3.1), towel (3.2). Not played yet: fanfare, title stamp (ice: docs/animation-spec.md 3.4). Haptics: `HapticsService` on
`AppRoot`, called next to the sounds, vibrates nothing yet (docs/animation-spec.md, Haptics).

Event order within one operation (documented on `LevelSession`); animations must play in this order:
- Move or undo: `MoveApplied` / `MoveUndone` → `BallRevealed` → `TubeCompleted` → each `TubeUnveiled` → each `KeyFound`
  then its `LockOpened` → `LevelCompleted`, then `StuckChanged` (only if changed), then `Changed`. So: balls fly, the
  source's new top flips (showing a key it carries), the cap drops, veils open, the key mark leaves, the door opens.
- Added tube (booster or Play on): `TubeAdded` → `StuckChanged` → `Changed`. Shuffle: `Shuffled` → then as after a
  move (`BallRevealed`, `TubeCompleted`, veils, keys and doors, `LevelCompleted`) → … Restart: `Restarted` → …
- The board finishes every change before the first event, so a handler never sees a half-applied move.

## 3. UI structure
- uGUI + TextMeshPro. One Screen Space Overlay canvas per scene (`HomeCanvas`, `LevelCanvas`), Canvas Scaler 1080×1920,
  match 0.5. Children in draw order: `Background` (full screen, outside the safe area) → `SafeArea` (screen content:
  top bar, pages or HUD, booster bar) → `BottomNav` (Home, outside the safe area so it spans the screen) →
  `InputBlocker` (Home) → popup instances → `Toast` last.
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
- Missing art (full list for the artist in docs/art-inventory.md): booster icons and pictures, door / lock / key, a
  hard tier button; for the animations, glow, shock faces and VFX sprites (docs/art-inventory.md, Pending for
  animations).
- The leaderboard is fixed data (no backend, no scores); More Lives offers no way to get lives.
- Consider adding "start position not stuck" to `LevelValidator` (today only `ShippedLevelTests` checks it).
- Re-measure the stuck check on a device.

Known issues:
- `Level.unity` and `Home.unity` are often marked dirty with no change (Editor quirk; save is a no-op).
- Opening `Level.unity` as a preview scene while it is open logs "More than one global light…" (tooling only).
- `Popup.IsOpen` is the GameObject's active state: animated closes must keep input blocked until the close ends.
- Stacked popups: the settings panel shows around Are You Sure / More Lives (by design; art may prefer to hide it).
- The mascot frame (255×255, top left) hangs below the 180-high top bar, ending above the board area; the stuck banner
  is inset to clear it.
- Lives refill uses the device clock, so moving the clock forward grants lives (offline game; accepted).
- In the Editor, stopping Play mode mid-level counts as closing the app and costs a life on the next Play. Saving
  scripts during Play mode reloads the domain and loses `AppRoot`: stop Play first.
- Input fields: never change a `Selectable`'s `interactable` (or the selection) inside `onEndEdit`, which a tap
  elsewhere raises mid-selection ("Attempting to select while already selecting"); `AvatarMenuPopup` blocks the
  name's taps with a `CanvasGroup` instead.
- In automated taps (MCP), the first raycast right after the avatar menu opens sometimes misses the pen; the next
  hits. Not seen by hand yet; worth a manual check on device.
