# Decisions and status

## Product decision ids
Product's decision log isn't in this repo. The proposals cite these ids; only what each was cited for is known here.

| Id | Cited for |
|---|---|
| D18, D25 | Level variants, so a retry feels fresh |
| D23 | Tier difficulty targets; no big-bottle tube on tier levels |
| D24 | A target bot win rate per level |
| D26 | Hidden balls capped at 65% of the non-top balls |
| D27 | No move cap |
| D30 | Board spacing: tubes close and big, the board fills the width |
| D31 | Clay combination rules |
| D32 | Mechanic unlock levels |

The proposals also wrote difficulty buckets as "D5" to "D7". This folder writes "bucket 5" so the two don't mix.

## Product status
As of 28 Oct 2026, from the proposals; none of it can be checked from this repo.
- The prototype `prototype/potion-sort-lab.html` (Level Design tab) and `tools/levelgen/` (Node) are in the product repo.
- The plan for all 100 levels is set (level-plan.md).
- Layouts are approved for L1–23 and L28; the others are still under review.
- Final variants (5, plus 3 DLB on tier levels) are built for 17 levels: 1, 3, 4, 6, 7, 11, 13, 14, 16, 17, 19, 23, 26, 27, 44, 49, 51. L1, 3 and 4 have a single variant by design. Every other level has 1 verified stop-gap variant.
- The prototype now follows this repo's rules: keys open when they reach the selectable run, a tube with a hidden ball isn't completed, and grids stay within 3 rows × 6 cells. Every stored solution was re-solved under them. A few variants now rate outside their ±5 band and will be regenerated before export.
- Every prototype layout passes the prototype's verifier (counts per colour, solution replay, replay without a false "stuck"). The prototype is a separate JS implementation, so our loader validation and `ShippedLevelTests` stay the gate for anything imported.
- **Export offer:** product can export these levels to our JSON format, grid included.

## Pending team decisions
From the developer review, 28 Oct 2026.
- **Generator:** use product's level export, or build the C# generator in step 5. This decides most of step 5.
- **Difficulty measure:** the casual-player bot (level-plan.md) instead of the signals listed in docs/levels.md.
- **Each idea in meta-ideas.md:** lives, variants, booster buying, shop, leaderboard, ads, episode overlay, win grades, the level top bar, the Music setting.
- **Lock colours** vs ball colours (names-and-art.md).
- **Player-facing vocabulary** (copy.md).
- **The game's name** for the win logo (feel.md).
- **Typed haptics**, which need a native plugin (feel.md).
- **Validate Levels warnings** for product's design rules: only worth it if levels are hand-edited or imported.
- **Product resolves** the open issues in level-plan.md, names-and-art.md, feel.md and copy.md.
