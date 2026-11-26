# Development Roadmap

Phase order approved in D-005. Updated 2026-11-22.

| # | Phase | Status | Deliverables | Blocked by |
|---|---|---|---|---|
| 0 | Requirement clarification | ✅ Done (2026-11-22) | `OPEN_QUESTIONS.md`, decisions D-001 to D-012 | — |
| 1 | Product understanding and business analysis | ✅ Done (2026-11-22, D-013) | `REQUIREMENTS.md` v1.0 | — |
| 2 | UX research and information architecture | 🟡 In review | `docs/ux/UX_ARCHITECTURE.md` (navigation, sitemap, journeys, screen inventory, states, patterns) · `docs/ux/wireframes.html` (39 low-fi frames) | Approval of D-014 (UX-01 to UX-11) |
| 3 | UI design system | 🟡 In review (together with Phase 2, D-016) | `docs/design/DESIGN_SYSTEM.md` v1.0 "Emerald Ops" · `docs/design/mockups/UI_Mockups_v1.pdf` (24 screens × light and dark + alternative palettes) · dummy app `prototype/ui-mock` · `tools/render_mockups.py`. v0.1 "Fleet Signage" rejected (D-015). Updated 2026-11-23 for D-020 to D-023 (24 screens, attendance removed) | Approval of D-014, D-017 |
| 4 | High-fidelity UI | ⬜ Pending | Every screen, every state, both themes | Phase 3; OI-04 (app name/logo) |
| 5 | Technical architecture | ⬜ Pending | `ARCHITECTURE.md` | Phase 4; OI-01 (scale), OI-06 (hosting) |
| 6 | Database design | ⬜ Pending | `DATABASE_DESIGN.md` | Phase 5 |
| 7 | Backend (.NET 8, D-008) | ⬜ Pending | API, tests, `API_DOCUMENTATION.md` | Phase 6; OI-07 (dev machine); OI-09 (.NET version); OI-02, OI-03 |
| 8 | Flutter app | ⬜ Pending | One role-based app (D-007) | Phase 7 |
| 9 | Testing and QA | ⬜ Pending | Test plans and reports | Phase 8 |
| 10 | Deployment and production readiness | ⬜ Pending | Runbooks, monitoring, backups | Phase 9; OI-05, OI-06 |

## Next actions

1. Owner reviews `docs/design/mockups/UI_Mockups_v1.pdf`, then approves or changes D-014 (UX structure) and D-017 (Emerald Ops). Decide OI-10 (who closes a trip the staff member can't).
2. Optional: validate the themed screens with 2–3 staff and 1 admin.
3. Phase 4: design every remaining screen and state, in both themes.
4. Tell the client about D-007: one app with roles instead of two apps (OI-08).
