# CLAUDE.md — Vehicle & Parking Management System

## Permanent instructions

"When reporting information to me, be extremely concise and sacrifice grammar for sake of concision."

"If my request is ambiguous, ask one clarifying question before doing anything."

"This needs to be a production quality product, take the time to ensure the first draft is the industry standard, if not better."

## Project context

- Product: one mobile app (Android + iOS) with role-based access (Admin, Staff), per D-007. It runs on one centralized backend, API and database. No web portal.
- Stack: Flutter · ASP.NET Core Web API on .NET 8 (D-008) · Microsoft SQL Server.
- Source requirements, in priority order:
  1. Approved decisions in `PROJECT_DECISIONS.md`.
  2. `docs/source/client-clarifications-2026-11-18.md`.
  3. `docs/source/requirements-v1-revised.pdf`.
  4. `docs/source/staff-ui-mockup.pdf` (reference only, don't copy its UI).
- Palette references: `docs/source/palette/` (superseded; the current colours are D-017 "Emerald Ops").
- Design reviews are delivered as rendered screenshots in a PDF, not HTML preview pages (D-018).
- Current phase and status: see `DEVELOPMENT_ROADMAP.md` once it exists; until then `PROJECT_OVERVIEW.md` and `OPEN_QUESTIONS.md`.

## Project files

| File | Purpose |
|---|---|
| `PROJECT_DECISIONS.md` | Permanent decision log. Source of truth for approved decisions. |
| `PROJECT_OVERVIEW.md` | Plain-language understanding of the product, confirmed vs assumed. |
| `OPEN_QUESTIONS.md` | Questionnaire, gaps, contradictions, assumptions register. |
| `REQUIREMENTS.md` | PRD/FRD, role matrix, workflows (Phase 1). |
| `docs/ux/UX_ARCHITECTURE.md` | Navigation, IA, journeys, screen inventory, states (Phase 2). |
| `docs/ux/wireframes.html` | Low-fi wireframes; screen IDs match `UX_ARCHITECTURE.md` (Phase 2). |
| `docs/KNOWLEDGE_BASE.md` | **Start here in a new session.** Everything known, current state: context, rules, screens, design, decisions, environment, tooling, history. Keep it updated after every change. |
| `README.md` | GitHub landing page: what the project is, where to start, how to re-render. |
| `docs/design/DESIGN_SYSTEM.md` | Design system v1.0 "Emerald Ops" (D-017). |
| `prototype/ui-mock/` | Dummy app (HTML/CSS) whose screens are rendered for review. `styles.css` holds the design tokens. |
| `tools/render_mockups.py` | Renders the dummy app to PNG (3×) and builds the PDF. Flags: `--check-only`, `--deck-only`. |
| `docs/design/mockups/` | Rendered PNGs (light, dark, alt) + `UI_Mockups_v1.pdf`. The review deliverable (D-018). |
| `docs/design/ui-preview.html` | v0.1 "Fleet Signage" preview. Rejected (D-015); kept for history. |
| `ARCHITECTURE.md` | Technical architecture (Phase 5). |
| `DATABASE_DESIGN.md` | Entities, relationships, constraints (Phase 6). |
| `API_DOCUMENTATION.md` | API conventions and endpoints (Phase 7). |
| `DEVELOPMENT_ROADMAP.md` | Phases, status, what is done and what remains. |
| `CHANGELOG.md` | Notable changes, once code exists. |
| `DEPLOYMENT.md` | Publishing the API to the server, settings, backups, go-live checklist. |
| `backend/` | API solution (.NET 8): `src/VehicleManagementDM.Api`, `tests/VehicleManagementDM.Api.Tests`. Setup in `README.md`. |

Create a file only when its phase starts. Do not create documentation without a clear purpose.

## Working principles

- Always read relevant project documentation before making changes.
- Never make assumptions about business logic.
- Never make important decisions without discussing them with me.
- Maintain `PROJECT_DECISIONS.md`.
- Follow approved requirements and architecture.
- Do not introduce unnecessary dependencies.
- Prefer maintainable, secure, scalable solutions.
- Follow established coding standards.
- Avoid unnecessary abstractions and overengineering.
- Write readable, production-quality code.
- Do not use placeholder implementations in production functionality.
- Never claim something is implemented or tested unless it actually is.
- Run relevant tests after making changes.
- Keep documentation synchronised with implementation.
- Do not overwrite existing work without inspecting it first.
- Before modifying an existing feature, understand its current implementation and dependencies.

## Development workflow

Before implementing any feature:

1. Understand the requirement.
2. Review related documentation.
3. Inspect the existing code.
4. Identify affected components.
5. Explain the proposed implementation when it involves important decisions.
6. Obtain approval where required.
7. Implement the feature.
8. Test it.
9. Update relevant documentation.
10. Update the decision log if a decision was made.

## Delivery sequence (approved, D-005)

1. Product understanding & business analysis → 2. UX research & information architecture → 3. UI design system (light + dark as complete systems) → 4. High-fidelity UI → 5. Technical architecture → 6. Database design → 7. Backend → 8. Flutter → 9. Testing & QA → 10. Deployment & production readiness.

Do not start a later phase because one piece of an earlier phase is ready.

## Decision log rules

- Never silently make an important decision. Ask when it materially affects the application.
- Record approved decisions immediately; update changed ones.
- Never erase history: mark replaced decisions `Superseded` and link the replacement.
- Check existing decisions before proposing a new one.
- Approved decisions are the source of truth unless explicitly changed.
