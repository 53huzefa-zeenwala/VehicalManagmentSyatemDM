# Development Roadmap

Phase order approved in D-005. Updated 2026-11-22.

| # | Phase | Status | Deliverables | Blocked by |
|---|---|---|---|---|
| 0 | Requirement clarification | ✅ Done (2026-11-22) | `OPEN_QUESTIONS.md`, decisions D-001 to D-012 | — |
| 1 | Product understanding and business analysis | ✅ Done (2026-11-22, D-013) | `REQUIREMENTS.md` v1.0 | — |
| 2 | UX research and information architecture | ✅ Approved (D-014, 2026-11-27) | `docs/ux/UX_ARCHITECTURE.md` (navigation, sitemap, journeys, screen inventory, states, patterns) · `docs/ux/wireframes.html` (39 low-fi frames) | Approval of D-014 (UX-01 to UX-11) |
| 3 | UI design system | ✅ Approved (D-017, 2026-11-27) | `docs/design/DESIGN_SYSTEM.md` v1.0 "Emerald Ops" · `docs/design/mockups/UI_Mockups_v1.pdf` (24 screens × light and dark + alternative palettes) · dummy app `prototype/ui-mock` · `tools/render_mockups.py`. v0.1 "Fleet Signage" rejected (D-015). Updated 2026-11-23 for D-020 to D-023 (24 screens, attendance removed) | Approval of D-014, D-017 |
| 4 | High-fidelity UI | ⬜ Pending | Every screen, every state, both themes | Phase 3; OI-04 (app name/logo) |
| 5 | Technical architecture | 🟡 Backend part approved (`ARCHITECTURE.md` v0.3, D-033–D-042) | `ARCHITECTURE.md` | Phase 4; OI-01 (scale), OI-06 (hosting) |
| 6 | Database design | ✅ v0.2 approved (D-043) | `DATABASE_DESIGN.md` | — |
| 7 | Backend (.NET 8, D-008, D-028) | ✅ Built and tested (M1–M6, 2026-11-27); deployment pending (`DEPLOYMENT.md`) | API, tests, `API_DOCUMENTATION.md` | Owner's coding instructions; OI-12 (which DB and login); OI-09 (.NET version risk); OI-02, OI-03 |
| 8 | Flutter apps | 🟡 Staff app built and phone-tested (2026-11-27). Admin app built 2026-11-28: every admin screen (D-057), 33 tests across the workspace | `app/` workspace: `dm_core` + `apps/staff` + `apps/admin` (D-053–D-057) | Admin phone test; live-server check of both apps |
| 9 | Testing and QA | ⬜ Pending | Test plans and reports | Phase 8 |
| 10 | Deployment and production readiness | ⬜ Pending | Runbooks, monitoring, backups | Phase 9; OI-05, OI-06 |

## Next actions

0. **API (D-027–D-048):** all milestones M0–M6 done 2026-11-27 (105 tests). D-048 approved. **Deployed 2026-11-27** to IIS on the app server, port 8445 (D-050, D-052): health OK, JSON errors verified. Remaining: first admin, restart/sign-in check, backups. Deploy with `DEPLOYMENT.md` (IIS, 8.0.18, port 8445 per D-050; own SQL login OI-12, data folders, backups). Flutter started (D-054): see item 1.
1. **Staff app (D-053–D-055):** first cut built 2026-11-27 (`app/apps/staff`) and **tested on a phone** against a local API: every staff flow works after 7 fixes (CHANGELOG). 19 tests pass. Next: you sign in on the phone against the live server.
1a. **Admin app (D-057):** built 2026-11-28 (`app/apps/admin`): Dashboard, Live (trips, transfers, new transfer), trip and transfer detail, Manage (vehicles with photo, status, location, history; QR labels single and bulk with print / share image / save image; categories and types; staff; admins; lots; destinations; settings; audit log), 5 reports, profile, notifications. Map and lot coordinates removed from the whole system (D-056, migration `RemoveLotCoordinates`). Rollout: deploy the new API and hand out the new staff APK together. Every admin page tested on the owner's phone 2026-11-28 (see CHANGELOG). Next: deploy the new API, then both apps against the live server.
2. ~~Owner reviews `docs/design/mockups/UI_Mockups_v1.pdf`, then approves or changes D-014 (UX structure) and D-017 (Emerald Ops). (OI-10 closed by D-024, 2026-11-26.)~~ Done: D-014 and D-017 approved 2026-11-27 (D-054).
3. Optional: validate the themed screens with 2–3 staff and 1 admin.
4. Phase 4: design every remaining screen and state, in both themes (screens without a mock-up follow the design system for now, D-054).
5. ~~Tell the client about D-007: one app with roles instead of two apps (OI-08).~~ Moot: D-053 returns to two apps, as in the original PDF scope.
