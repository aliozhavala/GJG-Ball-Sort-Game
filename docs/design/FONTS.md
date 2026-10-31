# Potion Sort — Fonts

The game uses **one font: Baloo 2** (Google Fonts, SIL Open Font License, see `OFL.txt`; free for commercial use and shipping in the app).

| File | Weight | Use |
|---|---|---|
| `Baloo2-ExtraBold.ttf` | 800 | Almost all game text (default) |
| `Baloo2-Bold.ttf` | 700 | A few small secondary lines (list below) |

Both are static TTFs cut from the Google Fonts variable font, ready for TextMeshPro (make one TMP Font Asset per file).

## Where it is used

Every player-facing text is Baloo 2, including the text drawn on the board:

- **HUD:** level number and difficulty tag on Home, coin/heart pills, booster badges and `+` buttons, "No available moves", toasts.
- **Buttons:** Play, Continue, Retry, Settings, Shop Buy buttons, bottom-menu labels.
- **Popups:** Win (title, reward amount, Continue), Lose / Out of hearts, Settings, Profile, Shop (titles, ribbons, amounts, Best Value / % off tags), Leaderboard, Episode and New-feature intro cards.
- **On the board (canvas):** the `?` on hidden balls, the level name banner at level start, the HARD / SUPER HARD tag, the sleepy `z` particle.

**Bold (700) only for:** toast messages, the "No legal move" popup's body text, the "Restore Purchase" text link in Settings, the locked-leaderboard description, the hint line on New-feature cards. Everything else is ExtraBold (800).

## Look (how the prototype styles it)

- Size scale (in a 390 × 844 reference screen): body/labels 15–22, buttons 22–28, popup titles 40–46, Win headline 52–74, Home level number 46.
- Most large text is **white fill + thick dark outline + a solid shadow 3–6 px straight down in the same outline colour** (typical outline: deep purple `#24106E`; button-specific colours on green/red buttons). In TMP: Outline on the material + Underlay with offset Y and softness 0.
- Outline widths in the prototype are 4–10 px CSS strokes drawn under the fill, so only about **half shows**: visible outline ≈ 2–5 px at the sizes above.
- Gradient text (the big "SORT" word of the Win logo, the level-name banner on the board) uses a white→yellow→orange vertical gradient (`#FFF6C2 → #FFD23A → #FF9500`) — TMP Vertex Gradient.

## Not needed in the game

- **Lexend** (light sans-serif) appears only in the prototype's lab/tool panels and in two debug-only bits that won't ship (the on-screen haptic indicator and the "MOCK REWARDED AD" label). Don't import it.
- `system-ui` text appears only in the "Mock store: no real payment" notes, which are prototype-only.
