# Unified Pixel Art Direction

## Rendering Contract

- World reference resolution: **640 × 360** through Unity Pixel Perfect Camera; 1920 × 1080 is an exact **3×** presentation.
- World art baseline: **32 pixels per world unit**. Construction and terrain grid: **0.5 world unit**.
- Terrain source tiles: **32 × 32**, Point filtered, uncompressed, no mipmaps.
- UI reference layout remains 1920 × 1080 for readable Chinese and English text, but frames use hard 2-pixel rails, stepped corners, square notches and no rounded vector shapes.
- Every texture under `Assets/Resources/Art` and `Assets/_Game/Art` is normalized to Point filtering, no mipmaps and no compression by the scene baker.

## Shared Palette

- Ground: deep umber, ochre sand, charcoal rock.
- Technology: oxidized copper and bone ceramic.
- Functional light: restrained cyan/teal.
- High tier: amber/gold only for upgrades and rare equipment.
- Medical identity: pale ceramic, teal shadow and small vermilion markers.

## Silhouette Rules

- Factory: rectangular-octagonal industrial enclosure with crushers and vents.
- Barracks: hexagonal defensive enclosure with three turret posts.
- Hospital: rounded-square clean enclosure with four beacon pylons.
- Academy: triangular research enclosure with three antenna nodes.
- Nursery: irregular open sand depression; never a wall or perfect ring.
- All facilities remain hollow so assigned units stay visible in the courtyard.

## Upgrade Readability

- Level 1: basic perimeter and gate.
- Level 2: larger perimeter plus colored utility modules.
- Level 3: full footprint plus command modules and gold high-tier fittings.
- The maximum 4.4 × 4.4 footprint is reserved at construction even though the visible wall grows later.

## Character Consistency

Existing bug silhouettes and roles are preserved. Infant, adult, elder, worker, soldier and doctor all pass through the same high-density pixel-perfect render, point-sampled imports and shared cave palette. Hit feedback remains a universal red flash, shake and knockback rather than requiring separate damage sheets.

## Localization Contract

- Every player-facing string comes from `LocalizationTable`; gameplay scripts do not embed display text.
- Simplified Chinese, Traditional Chinese and every non-Chinese locale have separate translations. A locale never combines two languages in one label.
- Missing translations fall back to English, never Simplified Chinese, for non-Chinese locales.
- Layouts reserve expansion room and use auto-sizing where necessary so future Japanese, Korean, French, German, Spanish, Portuguese and Russian text can be added without rebuilding the HUD.
