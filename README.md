# Vehicle & Parking Management System

One mobile app (Flutter, Android and iOS) with **Staff** and **Admin** roles, on an ASP.NET Core (.NET 8) API and a SQL Server database.

- **Staff (the drivers)** scan a vehicle's QR code, take it out of a parking lot, and bring it back. A live timer runs the whole time.
- **Admins** manage vehicles, staff, lots and transfers, and monitor everything live and in reports.

**Status:** requirements are done (v1.1). UX structure and the visual design ("Emerald Ops") are in review. No application code yet. Delivery is phased: UX first, then architecture, then build.

## Start here

| File | What |
|---|---|
| [`docs/KNOWLEDGE_BASE.md`](docs/KNOWLEDGE_BASE.md) | **Everything in one place**: current rules, roles, screens, design, decisions, open items, environment |
| [`PROJECT_DECISIONS.md`](PROJECT_DECISIONS.md) | Decision log (D-001 to D-023). Source of truth |
| [`REQUIREMENTS.md`](REQUIREMENTS.md) | Functional requirements v1.1 (§0 = latest changes) |
| [`docs/design/mockups/UI_Mockups_v1.pdf`](docs/design/mockups/UI_Mockups_v1.pdf) | UI mock-ups: 24 screens in light and dark |
| [`docs/ux/UX_ARCHITECTURE.md`](docs/ux/UX_ARCHITECTURE.md) | Navigation, screen inventory, journeys, states |
| [`docs/design/DESIGN_SYSTEM.md`](docs/design/DESIGN_SYSTEM.md) | Colours, type, components |
| [`DEVELOPMENT_ROADMAP.md`](DEVELOPMENT_ROADMAP.md) | Phase status |
| [`OPEN_QUESTIONS.md`](OPEN_QUESTIONS.md) | Every question round with the client and owner |
| [`CLAUDE.md`](CLAUDE.md) | Working rules for the AI assistant |

## Re-render the mock-ups

Needs macOS with Google Chrome, Python 3 with Pillow, and an internet connection (for Google Fonts).

```bash
python3 -I tools/render_mockups.py
```

This renders `prototype/ui-mock/screens.html` (the dummy app) to PNGs in `docs/design/mockups/png/` and rebuilds the PDF. Use `--check-only` to only check layouts for overflow, or `--deck-only` to rebuild the PDF from existing PNGs.

## Layout

```
docs/source/      client inputs (original PDF, clarifications, staff mock-up, palette)
docs/ux/          UX architecture + low-fi wireframes
docs/design/      design system, mock-up PDF and PNGs
prototype/ui-mock dummy app used for the mock-ups
tools/            render script
```
