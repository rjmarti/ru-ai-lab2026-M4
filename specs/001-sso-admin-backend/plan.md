# Implementation Plan: Backend de Administración para SSO

**Branch**: `001-sso-admin-backend` | **Date**: 2026-07-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-sso-admin-backend/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

El sistema centraliza la administración de usuarios finales, sus credenciales (`username`+`emisor`), las aplicaciones que participan del SSO y los períodos de permiso de acceso entre un usuario y una aplicación. Expone `POST /api/sso/verificar` (protegido con API key) para que un SSO externo consulte en <500ms si una credencial tiene acceso vigente a una aplicación, y una aplicación web de administración (Razor + Bootstrap + JS vanilla, con login propio de SI) para gestionar usuarios, credenciales, aplicaciones y permisos, incluyendo la baja lógica unificada de un usuario que caduca automáticamente todos sus permisos en <3s.

Enfoque técnico: solución .NET 10 / C# 12 con los seis proyectos exigidos por AGENTS.md (`Models`, `Data`, `Application`, `API`, `Web`, `Test`), Entity Framework Core como ORM, Vertical Slice Architecture dentro de `Application` (cada caso de uso como slice autocontenido: comando/consulta + handler + validador + DTOs), autenticación por cookie para SI en `Web`, autenticación por API key para el endpoint SSO en `API`, y garantías de integridad (unicidad de credencial, no solapamiento de permisos) reforzadas a nivel de base de datos para sostener correctamente la concurrencia.

## Technical Context

**Language/Version**: C# 12 / .NET 10 (fijado por AGENTS.md, no requiere investigación)

**Primary Dependencies**: ASP.NET Core (Web API + Razor Pages/MVC), Entity Framework Core 10, `Microsoft.AspNetCore.Authentication.Cookies` (login SI), `Microsoft.AspNetCore.Identity` — únicamente el componente `PasswordHasher<T>` (hashing no reversible), un `AuthenticationHandler` personalizado para API key, FluentValidation (validadores por slice), Swashbuckle.AspNetCore (documento OpenAPI del contrato SSO)

**Storage**: Relacional vía EF Core — SQL Server (LocalDB en desarrollo, SQL Server en producción); cadena de conexión externalizada en configuración (Principio II de la constitución)

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) para tests de integración de `API` y `Web`; proveedor relacional real (SQLite en archivo/memoria) en tests de integración — no el proveedor `InMemory` de EF Core, que no aplica restricciones únicas ni aislamiento de transacciones y por lo tanto no puede verificar los criterios de concurrencia de FR-001/FR-004

**Target Platform**: Servidor Windows/Linux (ASP.NET Core es multiplataforma); dos aplicaciones host desplegables por separado (`SsoAdmin.API`, `SsoAdmin.Web`) que comparten `Application`/`Data`/`Models` en proceso (sin salto HTTP interno) y una única base de datos

**Project Type**: web (frontend Razor/Bootstrap/JS + backend API), estructura fijada por AGENTS.md §3

**Performance Goals**: `POST /api/sso/verificar` responde en <500ms con hasta 100 aplicaciones y 3000 usuarios (FR-014/SC-001); caducidad de permisos tras baja lógica en <3s (FR-015/SC-002)

**Constraints**: Sin secretos ni configuración específica de ambiente embebidos en código (Principio II); DTOs obligatorios en toda respuesta de `API` (sin exponer entidades de dominio); `ILogger<T>` exclusivamente para logs; sin llamadas asíncronas bloqueantes (`.Result`/`.Wait()`); toda respuesta debe ser trazable a la base de datos (Principio I)

**Scale/Scope**: Volumen de referencia 100 aplicaciones y 3000 usuarios (no es un límite estricto); 4 historias de usuario, 16 requisitos funcionales, 5 entidades de dominio

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio / Estándar | Evaluación | Cumplimiento |
|---|---|---|
| I. Database-Sourced Truth | Toda respuesta (incluida `allowed`/`motivo` del endpoint SSO y los listados administrativos) se deriva de una consulta a la base de datos vía EF Core; ningún valor por defecto inventado; ausencia de dato → error explícito (`400`/`401`/`motivo` explícito), nunca un valor adivinado | PASS |
| II. No Hardcoded Secrets | Cadena de conexión, valor de la API key del endpoint SSO y clave de firma de la cookie de autenticación se leen vía `IOptions<T>`/`IConfiguration`, provistas por `appsettings.{Environment}.json` + variables de entorno/secret manager; ningún literal en código | PASS |
| III. Verifiable Acceptance Criteria (NON-NEGOTIABLE) | Cada FR-001..FR-016 y cada escenario Given/When/Then de las 4 historias de usuario se mapea a un test xUnit en `SsoAdmin.Test` (ver quickstart.md); los tests de concurrencia usan un proveedor relacional real, no el `InMemory` de EF Core | PASS (a verificar en `/speckit-tasks`) |
| Seguridad y Configuración — DTOs en el borde de la API | `SsoAdmin.API` MUST devolver únicamente DTOs definidos en `Application`/`API`, nunca entidades de `Models` | PASS |
| Seguridad y Configuración — Logging | `ILogger<T>` en todos los servicios de `Application`/`API`/`Web`; sin `Console.WriteLine` | PASS |
| Seguridad y Configuración — Async | Todo el pipeline EF Core / ASP.NET Core usa `await` de punta a punta | PASS |

Sin violaciones. No se requiere completar Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-sso-admin-backend/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── sso-verificar.md
│   └── admin-api.md
├── checklists/
│   └── requirements.md
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (`./src`)

Todo el código de la solución vive bajo `./src` (AGENTS.md §3): el archivo de solución
`SsoAdmin.sln` y las carpetas de los seis proyectos. Los comandos `dotnet` se ejecutan
desde `./src`.

```text
src/
├── SsoAdmin.sln
├── SsoAdmin.Models/                  # Entidades de dominio: Usuario, Credencial, Aplicacion, PermisoAcceso, LoginSI
│   ├── Usuario.cs
│   ├── Credencial.cs
│   ├── Aplicacion.cs
│   ├── PermisoAcceso.cs
│   └── LoginSI.cs
│
├── SsoAdmin.Data/                    # EF Core: DbContext, configuraciones fluent, migraciones, repositorios
│   ├── SsoAdminDbContext.cs
│   ├── Configurations/                # IEntityTypeConfiguration<T> por entidad (índices únicos, relaciones)
│   ├── Migrations/
│   ├── Seed/                          # Carga del usuario admin/admin en primer arranque (FR-007)
│   └── Repositories/
│
├── SsoAdmin.Application/             # Vertical Slice Architecture: un folder por caso de uso
│   ├── Features/
│   │   ├── VerificarAcceso/           # US1 — Command/Query + Handler + Validator + DTOs (motivo, allowed)
│   │   ├── AuthSI/                    # US2 — Login SI (hash, emisión de cookie)
│   │   ├── GestionUsuarios/           # US2 — listar/crear/editar/dar de baja
│   │   ├── GestionCredenciales/       # US3 — listar/crear/eliminar, validación de unicidad
│   │   ├── GestionAplicaciones/       # US4 — listar/crear/editar/eliminar
│   │   └── GestionPermisos/           # US4 — otorgar/revocar, validación de solapamiento
│   └── Common/                        # Interfaces compartidas, resultado de dominio, excepciones
│
├── SsoAdmin.API/                     # Host ASP.NET Core — expone únicamente el contrato externo consumido por el SSO
│   ├── Controllers/
│   │   └── SsoController.cs           # POST /api/sso/verificar (auth: API key)
│   ├── Auth/
│   │   └── ApiKeyAuthenticationHandler.cs
│   ├── DTOs/
│   └── Program.cs
│
├── SsoAdmin.Web/                     # Host ASP.NET Core — Razor Pages + su propia API interna, cookie auth
│   ├── Pages/
│   │   ├── Login.cshtml
│   │   ├── Usuarios/
│   │   ├── Credenciales/
│   │   ├── Aplicaciones/
│   │   └── Permisos/
│   ├── Controllers/                   # API interna consumida same-origin por wwwroot/js (mismo host: sin salto de cookie)
│   │   ├── AuthController.cs          # login SI, emite cookie de este mismo host
│   │   ├── UsuariosController.cs
│   │   ├── CredencialesController.cs
│   │   ├── AplicacionesController.cs
│   │   └── PermisosController.cs
│   ├── wwwroot/js/                    # fetch() same-origin a los Controllers de este mismo host
│   └── Program.cs
│
└── SsoAdmin.Test/                    # xUnit
    ├── Unit/                          # Handlers de Application (reglas de negocio, sin I/O real)
    ├── Integration/                   # WebApplicationFactory contra SsoAdmin.API y SsoAdmin.Web + SQLite relacional
    └── TestFixtures/
```

**Structure Decision**: Aplicación web (Option 2 del template) adaptada a los seis proyectos exigidos por AGENTS.md §3, con **todo el código bajo `./src`** (solución y proyectos) según la convención de AGENTS.md §3; los comandos `dotnet restore/build/test` se ejecutan desde `./src`. `API` y `Web` son hosts ASP.NET Core independientes y desplegables por separado (para no acoplar el SLA de <500ms del endpoint SSO al ciclo de vida de la app de administración), y ambos referencian `Application`, `Data` y `Models` directamente en proceso — sin salto HTTP interno entre `Web` y `API`. El contrato de administración (auth, usuarios, credenciales, aplicaciones, permisos) se expone como API interna dentro del propio host `Web` (mismo origen que su JS vanilla), evitando cualquier configuración de cookie/CORS cross-host; `SsoAdmin.API` expone exclusivamente `POST /api/sso/verificar` para el consumidor externo. Dentro de `Application`, el código se organiza por **Vertical Slice** (un folder por caso de uso, con su propio Command/Query, Handler, Validator y DTOs) en lugar de por capas técnicas horizontales, consistente con el patrón de arquitectura exigido por AGENTS.md.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Sin violaciones — tabla no aplicable.
