# Development Roadmap

Phase order approved in D-005. Updated 2026-11-22.

| # | Phase | Status | Deliverables | Blocked by |
|---|---|---|---|---|
| 0 | Requirement clarification | ✅ Done (2026-11-22) | `OPEN_QUESTIONS.md`, decisions D-001 to D-012 | — |
| 1 | Product understanding and business analysis | ✅ Done (2026-11-22, D-013) | `REQUIREMENTS.md` v1.0 | — |
| 2 | UX research and information architecture | 🟡 In review | `docs/ux/UX_ARCHITECTURE.md` (navigation, sitemap, journeys, screen inventory, states, patterns) · `docs/ux/wireframes.html` (39 low-fi frames) | Approval of D-014 (UX-01 to UX-11) |
| 3 | UI design system | 🟡 In review (together with Phase 2, D-016) | `docs/design/DESIGN_SYSTEM.md` v1.0 "Emerald Ops" · `docs/design/mockups/UI_Mockups_v1.pdf` (24 screens × light and dark + alternative palettes) · dummy app `prototype/ui-mock` · `tools/render_mockups.py`. v0.1 "Fleet Signage" rejected (D-015). Updated 2026-11-23 for D-020 to D-023 (24 screens, attendance removed) | Approval of D-014, D-017 |
| 4 | High-fidelity UI | ⬜ Pending | Every screen, every state, both themes | Phase 3; OI-04 (app name/logo) |
| 5 | Technical architecture | 🟡 Backend part approved (`ARCHITECTURE.md` v0.3, D-033–D-042) | `ARCHITECTURE.md` | Phase 4; OI-01 (scale), OI-06 (hosting) |
| 6 | Database design | ✅ v0.2 approved (D-043) | `DATABASE_DESIGN.md` | — |
| 7 | Backend (.NET 8, D-008, D-028) | ✅ Built and tested (M1–M6, 2026-11-27); deployment pending (`DEPLOYMENT.md`) | API, tests, `API_DOCUMENTATION.md` | Owner's coding instructions; OI-12 (which DB and login); OI-09 (.NET version risk); OI-02, OI-03 |
| 8 | Flutter app | ⬜ Pending | One role-based app (D-007) | Phase 7 |
| 9 | Testing and QA | ⬜ Pending | Test plans and reports | Phase 8 |
| 10 | Deployment and production readiness | ⬜ Pending | Runbooks, monitoring, backups | Phase 9; OI-05, OI-06 |

## Next actions

0. **API (D-027–D-048):** all milestones M0–M6 done 2026-11-27 (105 tests). Owner reviews D-048. Then deploy with `DEPLOYMENT.md` (own SQL login OI-12, server runtime/port OI-13, data folders, backups). Next phase: Flutter app (Phase 8), after the UX/design approvals (D-014, D-017) and Phase 4 screens.
1. Owner reviews `docs/design/mockups/UI_Mockups_v1.pdf`, then approves or changes D-014 (UX structure) and D-017 (Emerald Ops). (OI-10 closed by D-024, 2026-11-26.)
2. Optional: validate the themed screens with 2–3 staff and 1 admin.
3. Phase 4: design every remaining screen and state, in both themes.
4. Tell the client about D-007: one app with roles instead of two apps (OI-08).
