# Design System — "Emerald Ops"

**Version:** 1.0 · **Date:** 2026-11-22 · **Status:** Proposed (D-017). Awaiting owner review of the PDF.
**See it:** `docs/design/mockups/UI_Mockups_v1.pdf` (15 pages: 24 screens in light and dark, plus alternative palettes)
**Source of truth for the tokens:** `prototype/ui-mock/styles.css`. Re-render with `python3 -I tools/render_mockups.py`.
**History:** v0.1 "Fleet Signage" (Ink Heather / Putty / Canary, D-015) was rejected by the owner on 2026-11-22. The owner then released the palette (D-017).

---

## 1. Direction

**Modern, confident, friendly operations app.**
- **Emerald** means trust and action: primary buttons, hero cards, active tabs.
- **Amber** means anything live: the trip timer bar, "on trip" states, breaks in progress.
- **Rich detail:** gradient hero cards with route-line artwork, vehicle illustrations, plate badges, charts and a map.
- **Dark mode:** a deep "night" base with bright mint accents.

**Signature elements**
1. **Live bar.** An amber pill pinned under the status bar while a vehicle is out. It shows the plate and a ticking timer, and turns red when the trip is overdue.
2. **Plate badge.** Registrations are drawn like UAE plates (code | number).
3. **Hero card.** An emerald gradient with route lines, used for the one thing that matters now (start a trip, the active trip, live fleet count).
4. **Centre scan button.** Raised in the middle of the **staff** tab bar, because scanning is the core action (UX-12, D-022). The admin tab bar has 4 plain tabs and no scan button.

## 2. Colour tokens

### Light

| Token | Hex | Use |
|---|---|---|
| bg | `#F3F6F5` | App background |
| surface / surface2 / sunken | `#FFFFFF` / `#EEF3F1` / `#E8EFEC` | Cards / segmented tracks / bar tracks |
| border | `#E1E8E5` | Dividers, outlines |
| text / text2 / text3 | `#0D1B1E` / `#4B5B5F` / `#5F6F73` | 16.2 / 6.5 / ≥ 4.6 : 1 |
| primary | `#0B7A62` | Buttons, active tab, links. White on it 5.28 : 1 |
| primary deep / end | `#06503F` / `#0C7F64` | Hero gradient |
| primary soft / on | `#DFF3EC` / `#075A48` | Success tints, chips (7.1 : 1) |
| accent (live) | `#FFB020`, text `#2B1B00` | Live bar, "on trip" (9.1 : 1) |
| accent soft / text | `#FFF1D6` / `#8A4B00` | Live tints (6.1 : 1) |
| danger | `#D92D20`, text `#B42318` on `#FDE7E5` | Overdue, cancel (5.6 : 1) |
| warning | `#B54708` on `#FEF0C7` | Full lot, maintenance (4.8 : 1) |
| info | `#175CD3` on `#DCEBFF` | Transfers, admin-ended (5.0 : 1) |

### Dark

| Token | Hex | Use |
|---|---|---|
| bg | `#0A1214` | Night background |
| surface / surface2 / sunken | `#111C1F` / `#172529` / `#0E181B` | Cards (1 px border instead of shadow) |
| border | `#223337` | — |
| text / text2 / text3 | `#ECF3F1` / `#A7B8B5` / `#82938F` | 16.8 / 8.4 / 5.4 : 1 |
| primary | `#34D3A6` (mint), text on it `#022A20` | 8.1 : 1 |
| hero gradient | `#0C3B31` → `#11684F` | White text ≥ 6.7 : 1 |
| accent | `#FFB020`, soft `#3A2A0D`, text `#FFC65C` | 8.9 : 1 |
| danger / warning / info | `#FF8A80` / `#FF9A4D` / `#84ADFF` on their soft tints | ≥ 6.4 : 1 |

**Rules**
- Status always shows an icon and a word, never colour alone.
- Amber is reserved for "live". Warnings use orange-brown tints.
- Every ratio above was computed with the WCAG formula.

### Alternative palettes (rendered on the last PDF page, not chosen)
- **Ocean:** blue `#1F5EFF` + orange `#FF7A1A`.
- **Midnight Lime:** olive `#2F4A06` / lime `#C6F432`.

Switching palettes is a token change only.

## 3. Typography

**Plus Jakarta Sans** (Google Fonts, OFL), weights 400–800. Tabular figures for every timer and number.

| Role | Size / weight | Use |
|---|---|---|
| Timer | 44 / 800, −1.5 tracking | Active trip, summary |
| Screen title | 28 / 800 | Tab roots |
| Section / sheet title | 20 / 800 | Steps, sheets |
| Card title | 16.5 / 700 | Rows, cards |
| Body | 14.5–15 / 400–500 | Text, inputs |
| Caption | 12–13 / 500–600 | Meta, labels |

**Arabic (later, D-012):** pair with IBM Plex Sans Arabic or Noto Kufi Arabic. Plus Jakarta Sans has no Arabic script.

## 4. Shape, space, elevation, icons

- **Spacing:** 4-pt grid. Screen gutter 20.
- **Radius:** cards 22, hero 26, buttons 17 (54 px tall), inputs 16, chips 12, tiles 14, sheets 30.
- **Elevation:**
  - Light: a soft two-layer shadow on cards, and a tinted glow under primary buttons and the hero.
  - Dark: 1 px borders, no shadows.
- **Icons:** Material Symbols Rounded. Filled versions in coloured tiles, outline in navigation (filled when active).
- **Touch targets:** at least 44 px. Primary actions sit in the bottom thumb zone.

## 5. Components (built in `prototype/ui-mock`)

- **Chrome:** status bar, live bar, header (avatar or back + title + actions), tab bar with the centre scan button.
- **Containers:** hero card, card, list row, icon tile.
- **Buttons:** primary, white (on hero), glass (on hero), secondary, danger, amber.
- **Labels:** chip, segmented control, status badge, plate badge.
- **Inputs:** input (focus ring), radio.
- **Progress and time:** stepper, capacity bar, timer ring, timeline.
- **Overlays:** bottom sheet.
- **Visuals:** map (stylised), bar chart, QR, vehicle illustrations (garbage truck, car, ambulance).

## 6. Flutter mapping (Phase 8)

- Tokens become a `ThemeExtension` (colours, gradients, radii) and two `ThemeData` (light and dark).
- Fonts are bundled as assets, not fetched at runtime.
- The illustrations are exported as SVG assets.
- The live bar is an `OverlayEntry` or a scaffold wrapper, so it shows on every staff route.
