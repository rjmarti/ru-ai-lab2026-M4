Summary

Total tasks: 75 (T001–T075)

Per phase:
- Phase 1 (Setup): 4 tasks
- Phase 2 (Foundational): 22 tasks (T005–T026)
- Phase 3 (US1 — SSO verifica acceso, P1): 8 tasks (2 tests + 6 impl)
- Phase 4 (US2 — Login SI + baja unificada, P2): 14 tasks (3 tests + 11 impl)
- Phase 5 (US3 — Gestión de credenciales, P3): 7 tasks (2 tests + 5 impl)
- Phase 6 (US4 — Aplicaciones y permisos, P4): 14 tasks (4 tests + 10 impl)
- Phase 7 (Polish): 5 tasks

Parallel opportunities: entity creation (T005–T009), configurations (T011–T015), and repositories (T019–T023) each parallelize within Foundational; all test tasks within a story parallelize (distinct files); once Foundational is done, all four user stories can be staffed in parallel; Razor Pages/JS tasks parallelize across stories.

Independent test criteria (from spec.md, preserved per story): US1 — preload data directly and call POST /api/sso/verificar; US2 — log in as admin, deactivate a usuario with multi-app permisos, confirm cascade; US3 — create/duplicate/delete credenciales via the API; US4 — register an aplicación, grant/overlap/revoke permisos.

Suggested MVP scope: User Story 1 only (Setup + Foundational + Phase 3) — delivers the SSO-consumable POST /api/sso/verificar contract independent of any admin UI.

Format validation: All 75 tasks use - [ ] T### [P?] [Story?] Description with exact file path; [Story] labels appear only in Phases 3–6; Setup/Foundational/Polish carry no story label, per the required checklist format.
