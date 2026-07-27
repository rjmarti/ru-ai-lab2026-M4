---

description: "Task list for Backend de Administración para SSO"
---

# Tasks: Backend de Administración para SSO

**Input**: Design documents from `/specs/001-sso-admin-backend/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/sso-verificar.md](./contracts/sso-verificar.md), [contracts/admin-api.md](./contracts/admin-api.md), [quickstart.md](./quickstart.md)

**Tests**: Included and REQUIRED. Constitution Principle III (NON-NEGOTIABLE) mandates an automated test for every acceptance scenario, functional requirement, and success criterion — tests are not optional for this feature.

**Organization**: Tasks are grouped by user story (US1–US4, priority order from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Maps the task to US1/US2/US3/US4 for traceability
- File paths are exact, relative to `./src` (where `SsoAdmin.sln` lives — AGENTS.md §3)

## Path Conventions

**All solution code lives under `./src`** (AGENTS.md §3): `src/SsoAdmin.sln` plus the six
project folders below. Every `SsoAdmin.*/...` path in the tasks is relative to `./src`, and
all `dotnet` commands are run from `./src`. Six-project solution fixed by `AGENTS.md` §3 /
`plan.md` Project Structure:

- `src/SsoAdmin.Models/` — domain entities
- `src/SsoAdmin.Data/` — EF Core DbContext, configurations, migrations, seed, repositories
- `src/SsoAdmin.Application/` — Vertical Slice features (Command/Query + Handler + Validator + DTOs)
- `src/SsoAdmin.API/` — ASP.NET Core host: `SsoController` only (external SSO contract), API key auth, Program.cs
- `src/SsoAdmin.Web/` — ASP.NET Core host: Razor Pages, Bootstrap, JS vanilla, cookie auth, plus the internal admin API controllers (auth/usuarios/credenciales/aplicaciones/permisos) consumed same-origin by its own JS
- `src/SsoAdmin.Test/` — xUnit: `Unit/`, `Integration/`, `TestFixtures/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution scaffolding and cross-cutting tooling

- [X] T001 Create `src/SsoAdmin.sln` and the six projects (`SsoAdmin.Models`, `SsoAdmin.Data`, `SsoAdmin.Application`, `SsoAdmin.API`, `SsoAdmin.Web`, `SsoAdmin.Test`) under `./src`, with project references: `Data`→`Models`; `Application`→`Models`,`Data`; `API`→`Application`,`Data`,`Models`; `Web`→`Application`,`Data`,`Models`; `Test`→ all five
- [X] T002 Add NuGet dependencies per project: `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Design` (`SsoAdmin.Data`), `FluentValidation` (`SsoAdmin.Application`), `Microsoft.AspNetCore.Authentication.Cookies` + `Microsoft.AspNetCore.Identity` + `Swashbuckle.AspNetCore` (`SsoAdmin.API`), `xunit` + `Microsoft.AspNetCore.Mvc.Testing` + `Microsoft.EntityFrameworkCore.Sqlite` (`SsoAdmin.Test`)
- [X] T003 [P] Create `appsettings.json`/`appsettings.Development.json` skeletons with placeholder keys `ConnectionStrings:Default` and `SsoApiKey:Value` in `SsoAdmin.API/appsettings.json` and `SsoAdmin.Web/appsettings.json` (Principle II — no literal secrets)
- [X] T004 [P] Create `src/Directory.Build.props` setting `LangVersion=12`, `Nullable=enable`, `ImplicitUsings=enable` for all projects

**Checkpoint**: Solution builds (`dotnet build`) with empty projects wired together.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, persistence, repositories, and host skeletons shared by every user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 [P] Create `Usuario` entity (`Id`, `Nombre`, `Activo`) in `SsoAdmin.Models/Usuario.cs`
- [ ] T006 [P] Create `Credencial` entity (`Id`, `UsuarioId`, `Username`, `Emisor`) in `SsoAdmin.Models/Credencial.cs`
- [ ] T007 [P] Create `Aplicacion` entity (`Id`, `Nombre`, `Url`) in `SsoAdmin.Models/Aplicacion.cs`
- [ ] T008 [P] Create `PermisoAcceso` entity (`Id`, `UsuarioId`, `AplicacionId`, `FechaDesde`, `FechaHasta?`) in `SsoAdmin.Models/PermisoAcceso.cs`
- [ ] T009 [P] Create `LoginSI` entity (`Id`, `Usuario`, `PasswordHash`) in `SsoAdmin.Models/LoginSI.cs`
- [ ] T010 Create `SsoAdminDbContext` with `DbSet<T>` for all five entities in `SsoAdmin.Data/SsoAdminDbContext.cs` (depends on T005-T009)
- [ ] T011 [P] Create `UsuarioConfiguration : IEntityTypeConfiguration<Usuario>` in `SsoAdmin.Data/Configurations/UsuarioConfiguration.cs` (depends on T010)
- [ ] T012 [P] Create `CredencialConfiguration` with unique composite index `(Username, Emisor)` in `SsoAdmin.Data/Configurations/CredencialConfiguration.cs` — enforces FR-001/FR-002 at the database level (depends on T010)
- [ ] T013 [P] Create `AplicacionConfiguration` in `SsoAdmin.Data/Configurations/AplicacionConfiguration.cs` (depends on T010)
- [ ] T014 [P] Create `PermisoAccesoConfiguration` with FKs to `Usuario`/`Aplicacion` and a supporting index on `(UsuarioId, AplicacionId)` in `SsoAdmin.Data/Configurations/PermisoAccesoConfiguration.cs` (depends on T010)
- [ ] T015 [P] Create `LoginSIConfiguration` with unique index on `Usuario` in `SsoAdmin.Data/Configurations/LoginSIConfiguration.cs` (depends on T010)
- [ ] T016 Generate initial EF Core migration `InitialCreate` in `SsoAdmin.Data/Migrations/` (depends on T011-T015)
- [ ] T017 Implement `LoginSISeeder` that precargas `admin`/`admin` via `PasswordHasher<T>` on first run (FR-007) in `SsoAdmin.Data/Seed/LoginSISeeder.cs` (depends on T010, T015)
- [ ] T018 [P] Create shared `Result<T>`/`DomainError` types for handler outcomes in `SsoAdmin.Application/Common/Result.cs`
- [ ] T019 [P] Create `IUsuarioRepository` + `UsuarioRepository` in `SsoAdmin.Data/Repositories/UsuarioRepository.cs` (depends on T010)
- [ ] T020 [P] Create `ICredencialRepository` + `CredencialRepository`, translating unique-index `DbUpdateException` into a domain conflict result, in `SsoAdmin.Data/Repositories/CredencialRepository.cs` (depends on T010, T012)
- [ ] T021 [P] Create `IAplicacionRepository` + `AplicacionRepository` in `SsoAdmin.Data/Repositories/AplicacionRepository.cs` (depends on T010)
- [ ] T022 [P] Create `IPermisoAccesoRepository` + `PermisoAccesoRepository`, with an overlap-check query executed inside a `Serializable` transaction, in `SsoAdmin.Data/Repositories/PermisoAccesoRepository.cs` (depends on T010, T014)
- [ ] T023 [P] Create `ILoginSIRepository` + `LoginSIRepository` in `SsoAdmin.Data/Repositories/LoginSIRepository.cs` (depends on T010)
- [ ] T024 Configure `SsoAdmin.API/Program.cs`: `DbContext` registration via `IConfiguration`, DI registration of the repositories used by `VerificarAcceso`, `SsoController` routing, ApiKey scheme, Swashbuckle/OpenAPI for the sso-verificar contract only (depends on T016, T019-T023)
- [ ] T025 Configure `SsoAdmin.Web/Program.cs`: `DbContext` registration, DI registration of all repositories, cookie authentication scheme, MVC controllers (internal admin API) + Razor Pages, static files (depends on T016, T019-T023)
- [ ] T026 [P] Create `WebApplicationFactory`-based test fixtures using the SQLite relational provider (not `InMemory`) for `SsoAdmin.API` and `SsoAdmin.Web` in `SsoAdmin.Test/TestFixtures/` (depends on T024, T025)

**Checkpoint**: `dotnet build` succeeds across the solution; `dotnet ef database update` applies `InitialCreate`; both hosts start; test fixtures can spin up an in-memory SQLite-backed host. User story implementation can now begin.

---

## Phase 3: User Story 1 - SSO verifica acceso de una credencial a una aplicación (Priority: P1) 🎯 MVP

**Goal**: `POST /api/sso/verificar` (API-key protected) returns `allowed`/`motivo` in <500ms for any combination of credencial/usuario/aplicación/permiso state.

**Independent Test**: Preload a usuario, credencial, aplicación, and permiso directly in the database, then call `POST /api/sso/verificar` with varying inputs and confirm the response and latency without touching the Web admin UI.

### Tests for User Story 1 ⚠️

- [ ] T028 [P] [US1] Integration test suite covering AC1-AC8 (allowed=true, `permiso_vencido`, `usuario_inactivo`, `aplicacion_no_encontrada`, `credencial_no_encontrada`, `permiso_no_encontrado` incl. `fecha_desde` futura edge case, missing-field 400, missing/invalid API key 401) plus a `500 Internal Server Error` scenario (FR-009) by substituting a faulted repository into the test `WebApplicationFactory`, in `SsoAdmin.Test/Integration/SsoVerificarEndpointTests.cs` (depends on T026, API fixture)
- [ ] T029 [P] [US1] Unit tests for `VerificarAccesoHandler` motivo precedence and business rules (no HTTP, no I/O) in `SsoAdmin.Test/Unit/VerificarAccesoHandlerTests.cs`

### Implementation for User Story 1

- [ ] T030 [US1] Create `VerificarAccesoRequest`/`VerificarAccesoResponse` DTOs in `SsoAdmin.Application/Features/VerificarAcceso/VerificarAccesoDtos.cs`
- [ ] T031 [US1] Create `VerificarAccesoValidator` (FluentValidation: `username`/`emisor`/`aplicacionUrl` required) in `SsoAdmin.Application/Features/VerificarAcceso/VerificarAccesoValidator.cs`
- [ ] T032 [US1] Implement `VerificarAccesoHandler` resolving motivo precedence (`credencial_no_encontrada` → `usuario_inactivo` → `aplicacion_no_encontrada` → `permiso_no_encontrado` → `permiso_vencido` → allowed) using `ICredencialRepository`/`IAplicacionRepository`/`IPermisoAccesoRepository`, logged via `ILogger<T>`, in `SsoAdmin.Application/Features/VerificarAcceso/VerificarAccesoHandler.cs` (depends on T030, T031, T019-T023)
- [ ] T033 [US1] Create `ApiKeyAuthenticationHandler` + `ApiKeyAuthenticationOptions` validating header `X-Api-Key` against `IOptions<SsoApiKeyOptions>` (FR-016) in `SsoAdmin.API/Auth/ApiKeyAuthenticationHandler.cs`
- [ ] T034 [US1] Create `SsoController` with `POST /api/sso/verificar`, `[Authorize(AuthenticationSchemes = "ApiKey")]`, returning `200`/`400`/`401`/`500` per contract in `SsoAdmin.API/Controllers/SsoController.cs` (depends on T032, T033)
- [ ] T035 [US1] Register the `ApiKey` authentication scheme and bind `SsoApiKeyOptions` from configuration in `SsoAdmin.API/Program.cs` (depends on T033)

**Checkpoint**: User Story 1 is fully functional and independently testable — MVP deliverable.

---

## Phase 4: User Story 2 - SI autentica y da de baja unificada a un usuario (Priority: P2)

**Goal**: SI logs in via cookie auth and can list/create/edit/deactivate usuarios; deactivation cascades to expire all active permisos in <3s.

**Independent Test**: Log in as the preloaded `admin` user, create a usuario with active permisos across multiple aplicaciones, deactivate it from the Web app, and confirm the listing reflects `Activo=false` and a subsequent SSO query returns `motivo=usuario_inactivo`.

### Tests for User Story 2 ⚠️

- [ ] T036 [P] [US2] Integration tests for `POST /api/auth/login`/`logout` — valid credentials issue a cookie (AC1), invalid credentials return 401 (AC2) — in `SsoAdmin.Test/Integration/AuthControllerTests.cs` (depends on T026, Web fixture)
- [ ] T037 [P] [US2] Integration tests for `/api/usuarios` listar/crear/editar/baja, including the cascade of active permisos on baja and idempotent baja on an already-inactive usuario (AC3, AC4, FR-006, edge case) in `SsoAdmin.Test/Integration/UsuariosControllerTests.cs` (depends on T026, Web fixture)
- [ ] T038 [P] [US2] Unit test asserting `DarBajaUsuarioHandler` expires every active `PermisoAcceso` for the usuario within the same operation in `SsoAdmin.Test/Unit/DarBajaUsuarioHandlerTests.cs`

### Implementation for User Story 2

- [ ] T039 [US2] Create `LoginRequest`/`LoginResponse` DTOs + `LoginValidator` in `SsoAdmin.Application/Features/AuthSI/AuthDtos.cs`
- [ ] T040 [US2] Implement `LoginHandler` validating against `LoginSI` via `PasswordHasher<T>` in `SsoAdmin.Application/Features/AuthSI/LoginHandler.cs` (depends on T023, T039)
- [ ] T041 [US2] Create `AuthController` with `POST /api/auth/login` (cookie sign-in) and `POST /api/auth/logout` in `SsoAdmin.Web/Controllers/AuthController.cs` (depends on T040)
- [ ] T042 [P] [US2] Configure cookie authentication scheme (login path, cookie name/expiry) in `SsoAdmin.Web/Program.cs` (depends on T025)
- [ ] T043 [US2] Create `UsuarioListItem`/`CrearUsuarioRequest`/`EditarUsuarioRequest` DTOs in `SsoAdmin.Application/Features/GestionUsuarios/UsuarioDtos.cs`
- [ ] T044 [P] [US2] Create `CrearUsuarioValidator`/`EditarUsuarioValidator` (nombre no vacío) in `SsoAdmin.Application/Features/GestionUsuarios/UsuarioValidators.cs`
- [ ] T045 [US2] Implement `ListarUsuariosHandler`, `CrearUsuarioHandler`, `EditarUsuarioHandler` in `SsoAdmin.Application/Features/GestionUsuarios/UsuarioHandlers.cs` (depends on T019, T043, T044)
- [ ] T046 [US2] Implement `DarBajaUsuarioHandler` — sets `Activo=false` and, in the same transaction, sets `FechaHasta=hoy` on every active `PermisoAcceso` of the usuario; idempotent when already inactive (FR-006/FR-015) in `SsoAdmin.Application/Features/GestionUsuarios/DarBajaUsuarioHandler.cs` (depends on T019, T022)
- [ ] T047 [US2] Create `UsuariosController` (`GET`/`POST`/`PUT /api/usuarios`, `POST /api/usuarios/{id}/baja`) with `[Authorize]` cookie auth in `SsoAdmin.Web/Controllers/UsuariosController.cs` (depends on T045, T046)
- [ ] T048 [P] [US2] Create `Login.cshtml`/`Login.cshtml.cs` page posting to `/api/auth/login` in `SsoAdmin.Web/Pages/Login.cshtml`
- [ ] T049 [P] [US2] Create Usuarios Razor pages (listar/crear/editar/baja) and `wwwroot/js/usuarios.js` fetch calls against `/api/usuarios` in `SsoAdmin.Web/Pages/Usuarios/`

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - SI administra credenciales de un usuario (Priority: P3)

**Goal**: SI can list/create/delete credenciales, with `username`+`emisor` uniqueness enforced even under concurrency, and no password-derived fields ever stored.

**Independent Test**: Create a usuario, add a credencial, attempt a duplicate `username`+`emisor` and confirm rejection, then delete an existing credencial and confirm it disappears from the listing.

### Tests for User Story 3 ⚠️

- [ ] T050 [P] [US3] Integration tests for `/api/credenciales`: duplicate `username`+`emisor` → 400 (AC1), reassigning an existing credencial to another usuario → 400 (AC2), listar/crear/eliminar (AC3), confirming no password/hash field exists on the entity (AC4/SC-006), and confirming a single usuario can hold two credenciales with the same `username` under two different `emisor` values (FR-002 positive path) in `SsoAdmin.Test/Integration/CredencialesControllerTests.cs` (depends on T026, Web fixture)
- [ ] T051 [P] [US3] Integration test firing two concurrent `POST /api/credenciales` requests with the same `username`+`emisor` and asserting exactly one persists (edge case, FR-001) in `SsoAdmin.Test/Integration/CredencialConcurrencyTests.cs` (depends on T026, Web fixture)

### Implementation for User Story 3

- [ ] T052 [US3] Create `CredencialListItem`/`CrearCredencialRequest` DTOs in `SsoAdmin.Application/Features/GestionCredenciales/CredencialDtos.cs`
- [ ] T053 [P] [US3] Create `CrearCredencialValidator` in `SsoAdmin.Application/Features/GestionCredenciales/CrearCredencialValidator.cs`
- [ ] T054 [US3] Implement `ListarCredencialesHandler`, `CrearCredencialHandler` (maps the unique-index violation from `CredencialRepository` to a `400` domain error), `EliminarCredencialHandler` in `SsoAdmin.Application/Features/GestionCredenciales/CredencialHandlers.cs` (depends on T020, T052, T053)
- [ ] T055 [US3] Create `CredencialesController` (`GET`/`POST`/`DELETE /api/credenciales`) with `[Authorize]` in `SsoAdmin.Web/Controllers/CredencialesController.cs` (depends on T054)
- [ ] T056 [P] [US3] Create Credenciales Razor pages (listar/crear/eliminar) and `wwwroot/js/credenciales.js` in `SsoAdmin.Web/Pages/Credenciales/`

**Checkpoint**: User Stories 1, 2, and 3 all work independently.

---

## Phase 6: User Story 4 - SI administra aplicaciones y permisos de acceso (Priority: P4)

**Goal**: SI can manage the aplicación catalog and grant/revoke time-bounded permisos per usuario+aplicación, with overlap rejected even under concurrency.

**Independent Test**: Register an aplicación with nombre/URL, grant a permiso with fecha_desde/fecha_hasta, attempt an overlapping permiso and confirm rejection, then revoke an active permiso by setting its fecha_hasta to today.

### Tests for User Story 4 ⚠️

- [ ] T057 [P] [US4] Integration tests for `/api/aplicaciones`: empty-URL registration/edit → 400 (AC1), listar/crear/editar/eliminar (AC4) in `SsoAdmin.Test/Integration/AplicacionesControllerTests.cs` (depends on T026, Web fixture)
- [ ] T058 [P] [US4] Integration tests for `/api/permisos`: overlapping period → 409 (AC2, incl. exact-date-match and pre-existing indefinite-permiso edge cases), `fecha_desde > fecha_hasta` → 400 (edge case), revoke sets `fecha_hasta=hoy` and a subsequent SSO query returns `permiso_vencido` (AC3) in `SsoAdmin.Test/Integration/PermisosControllerTests.cs` (depends on T026, Web fixture)
- [ ] T059 [P] [US4] Integration test firing two concurrent `POST /api/permisos` requests with overlapping periods for the same usuario+aplicación and asserting exactly one persists (edge case, FR-004, `Serializable` isolation) in `SsoAdmin.Test/Integration/PermisoConcurrencyTests.cs` (depends on T026, Web fixture)
- [ ] T060 [P] [US4] Integration test deleting an aplicación with active permisos and confirming a subsequent SSO query returns `motivo=aplicacion_no_encontrada` (edge case) in `SsoAdmin.Test/Integration/AplicacionEliminacionTests.cs` (depends on T026, Web fixture for the deletion + API fixture for the SSO follow-up query)

### Implementation for User Story 4

- [ ] T061 [US4] Create `AplicacionListItem`/`CrearAplicacionRequest`/`EditarAplicacionRequest` DTOs + validator (URL no vacía, FR-003) in `SsoAdmin.Application/Features/GestionAplicaciones/AplicacionDtos.cs`
- [ ] T062 [US4] Implement `ListarAplicacionesHandler`, `CrearAplicacionHandler`, `EditarAplicacionHandler`, `EliminarAplicacionHandler` in `SsoAdmin.Application/Features/GestionAplicaciones/AplicacionHandlers.cs` (depends on T021, T061)
- [ ] T063 [US4] Create `AplicacionesController` (`GET`/`POST`/`PUT`/`DELETE /api/aplicaciones`) with `[Authorize]` in `SsoAdmin.Web/Controllers/AplicacionesController.cs` (depends on T062)
- [ ] T064 [US4] Create `PermisoListItem`/`OtorgarPermisoRequest` DTOs in `SsoAdmin.Application/Features/GestionPermisos/PermisoDtos.cs`
- [ ] T065 [P] [US4] Create `OtorgarPermisoValidator` (`fecha_desde <= fecha_hasta` when present) in `SsoAdmin.Application/Features/GestionPermisos/OtorgarPermisoValidator.cs`
- [ ] T066 [US4] Implement `OtorgarPermisoHandler` running the overlap check (incl. exact-match and open-ended-permiso edge cases) inside the `Serializable` transaction from `PermisoAccesoRepository` in `SsoAdmin.Application/Features/GestionPermisos/OtorgarPermisoHandler.cs` (depends on T022, T064, T065)
- [ ] T067 [US4] Implement `ListarPermisosHandler` (optional `usuarioId`/`aplicacionId` filters) and `RevocarPermisoHandler` (sets `FechaHasta=hoy`, idempotent) in `SsoAdmin.Application/Features/GestionPermisos/PermisoHandlers.cs` (depends on T022, T064)
- [ ] T068 [US4] Create `PermisosController` (`GET /api/permisos`, `POST /api/permisos`, `POST /api/permisos/{id}/revocar`) with `[Authorize]` in `SsoAdmin.Web/Controllers/PermisosController.cs` (depends on T066, T067)
- [ ] T069 [P] [US4] Create Aplicaciones Razor pages (listar/crear/editar/eliminar) and `wwwroot/js/aplicaciones.js` in `SsoAdmin.Web/Pages/Aplicaciones/`
- [ ] T070 [P] [US4] Create Permisos Razor pages (listar/otorgar/revocar) and `wwwroot/js/permisos.js` in `SsoAdmin.Web/Pages/Permisos/`

**Checkpoint**: All four user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Performance validation, contract/documentation cross-check, and constitution compliance sweep

- [ ] T071 [P] Verify Swashbuckle/OpenAPI output in `SsoAdmin.API/Program.cs` documents every endpoint consistent with `contracts/sso-verificar.md` and `contracts/admin-api.md`
- [ ] T072 [P] Performance test seeding 100 aplicaciones / 3000 usuarios and asserting `POST /api/sso/verificar` responds in <500ms (FR-014/SC-001) in `SsoAdmin.Test/Integration/SsoVerificarPerformanceTests.cs`
- [ ] T073 [P] Performance test asserting baja lógica of a usuario with permisos across multiple aplicaciones expires all of them in <3s (FR-015/SC-002) in `SsoAdmin.Test/Integration/BajaUsuarioPerformanceTests.cs`
- [ ] T074 [P] Constitution compliance sweep: confirm `ILogger<T>` is used everywhere (no `Console.WriteLine`), no `.Result`/`.Wait()` blocking calls, and `SsoAdmin.API` responses never expose `SsoAdmin.Models` entities directly
- [ ] T075 Run the full `quickstart.md` validation end-to-end (`dotnet restore && dotnet build && dotnet test`, then the manual Web + SSO curl flow) and confirm every step passes
- [ ] T076 [P] Timed walkthrough asserting SC-007: script or manual-QA checklist (tied to `quickstart.md` §5) driving the full onboarding cycle — crear usuario → crear credencial → otorgar permiso — through `SsoAdmin.Web`, asserting completion in under 2 minutes, in `SsoAdmin.Test/Integration/OnboardingTimingTests.cs` (or a documented manual-QA step if UI-timing automation is out of scope)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends only on Foundational — no dependency on US2/US3/US4
- **User Story 2 (Phase 4)**: Depends only on Foundational — independently testable from US1/US3/US4
- **User Story 3 (Phase 5)**: Depends only on Foundational — independently testable from US1/US2/US4
- **User Story 4 (Phase 6)**: Depends only on Foundational — independently testable from US1/US2/US3
- **Polish (Phase 7)**: Depends on all user stories the team chooses to complete

### User Story Dependencies

All four user stories depend solely on Phase 2 (Foundational) and can proceed in parallel or in priority order (P1 → P2 → P3 → P4); none blocks another, matching the "Independent Test" criteria in spec.md.

### Within Each User Story

- Tests are written first and MUST fail before implementation begins (Constitution Principle III)
- DTOs/Validators before Handlers
- Handlers before Controllers
- Controllers before Razor Pages/JS that consume them

### Parallel Opportunities

- All Setup tasks marked [P] (T003, T004) run in parallel
- Within Foundational: entity creation (T005-T009), then configurations (T011-T015), then repositories (T019-T023) each run in parallel as a batch
- Once Foundational (Phase 2) completes, US1, US2, US3, US4 can be staffed and executed in parallel
- All test tasks marked [P] within a story run in parallel (different files)
- Razor Pages/JS tasks marked [P] run in parallel with each other once their controllers exist

---

## Parallel Example: User Story 1

```bash
# Tests (after Foundational, before implementation):
Task: "Integration test suite covering AC1-AC8 in SsoAdmin.Test/Integration/SsoVerificarEndpointTests.cs"
Task: "Unit tests for VerificarAccesoHandler in SsoAdmin.Test/Unit/VerificarAccesoHandlerTests.cs"

# Foundational batch example (Phase 2):
Task: "Create Usuario entity in SsoAdmin.Models/Usuario.cs"
Task: "Create Credencial entity in SsoAdmin.Models/Credencial.cs"
Task: "Create Aplicacion entity in SsoAdmin.Models/Aplicacion.cs"
Task: "Create PermisoAcceso entity in SsoAdmin.Models/PermisoAcceso.cs"
Task: "Create LoginSI entity in SsoAdmin.Models/LoginSI.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run `SsoVerificarEndpointTests` and `VerificarAccesoHandlerTests`, confirm <500ms manually per quickstart.md §6-7
5. Deploy/demo if ready — the SSO integration contract is usable standalone even before any admin UI exists

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. Add User Story 1 → test independently → deploy/demo (MVP — SSO can integrate)
3. Add User Story 2 → test independently → deploy/demo (SI can log in and deactivate usuarios)
4. Add User Story 3 → test independently → deploy/demo (SI can manage credenciales)
5. Add User Story 4 → test independently → deploy/demo (SI can manage aplicaciones/permisos)
6. Phase 7 (Polish) validates performance targets and full constitution compliance

### Parallel Team Strategy

With multiple developers, once Foundational (Phase 2) is done:
- Developer A: User Story 1 (SsoController + VerificarAcceso slice)
- Developer B: User Story 2 (Auth + GestionUsuarios slices + Login/Usuarios pages)
- Developer C: User Story 3 (GestionCredenciales slice + Credenciales pages)
- Developer D: User Story 4 (GestionAplicaciones + GestionPermisos slices + their pages)

---

## Notes

- [P] tasks touch different files and have no unfinished dependency between them
- [Story] label maps every Phase 3+ task to its user story for traceability
- Every acceptance scenario and edge case from spec.md has an explicit test task (Constitution Principle III)
- Verify tests fail before implementing the corresponding handler/controller
- Commit after each task or logical group
- Stop at any checkpoint to validate a story independently before moving to the next priority
