# Phase 0 Research: Backend de Administración para SSO

Todas las decisiones tecnológicas de alto nivel (lenguaje C# 12, .NET 10, EF Core, ASP.NET/Razor/Bootstrap/JS vanilla, estructura de 6 proyectos) están fijadas por `AGENTS.md` y no requieren investigación. Este documento resuelve las decisiones de diseño técnico que el spec dejó explícitamente abiertas (ver sección "Clarifications" y "Assumptions" de `spec.md`) más las decisiones de librerías necesarias para implementar de forma verificable los requisitos.

## 1. Autenticación del endpoint `POST /api/sso/verificar`

- **Decision**: `AuthenticationHandler<ApiKeyAuthenticationOptions>` personalizado registrado como esquema de autenticación en `SsoAdmin.API`, que exige el header `X-Api-Key` y lo compara (comparación de tiempo constante) contra un valor leído de `IOptions<SsoApiKeyOptions>`. Solicitudes sin el header o con un valor inválido devuelven `401 Unauthorized` antes de ejecutar cualquier lógica de negocio (FR-016).
- **Rationale**: Resuelve directamente la clarificación de spec (Sesión 2026-07-20, Q1: API key por header). Un `AuthenticationHandler` nativo de ASP.NET Core se integra con `[Authorize]` y con el pipeline estándar, evita lógica de auth duplicada por controlador, y permite testear el rechazo con `WebApplicationFactory` sin mocks.
- **Alternatives considered**: mTLS (mayor complejidad operativa para un solo consumidor conocido, fuera de lo pedido); IP allow-listing (no verificable a nivel de aplicación/test, depende de infraestructura).

## 2. Hashing de contraseña de Login (SI)

- **Decision**: `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (PBKDF2-HMACSHA256, salteado, con parámetros por defecto de .NET 10) usado de forma independiente, sin adoptar el resto de ASP.NET Core Identity (tablas de roles, claims, etc.), ya que el spec excluye explícitamente roles diferenciados y autogestión (sección "Fuera de Alcance").
- **Rationale**: Satisface FR-007 ("hash no reversible") y Principio I/constitución sin agregar una dependencia externa nueva; es el componente estándar de Microsoft para este propósito exacto y ya viene con el framework.
- **Alternatives considered**: BCrypt.Net-Next (dependencia externa adicional sin beneficio claro dado que `PasswordHasher<T>` ya es estándar de la industria y suficiente); Argon2 (sin soporte de primera clase en .NET, complejidad injustificada para el alcance).

## 3. Autenticación del login de SI (app Web)

- **Decision**: `Microsoft.AspNetCore.Authentication.Cookies` en `SsoAdmin.Web`; el formulario de login (US2) valida contra `LoginSI` vía `PasswordHasher<T>` y, si es válido, emite una cookie de autenticación; páginas administrativas protegidas con `[Authorize]`.
- **Rationale**: Encaja naturalmente con Razor Pages/MVC server-rendered; no se requiere token JWT porque no hay cliente SPA ni consumidor externo del login de SI (solo el propio navegador). Para que la cookie sea válida sin configuración adicional de CORS ni de key ring compartido, los endpoints de administración (`/api/auth/*`, `/api/usuarios`, `/api/credenciales`, `/api/aplicaciones`, `/api/permisos`) se hospedan como controllers dentro del propio host `SsoAdmin.Web` — no en `SsoAdmin.API` — de modo que el `fetch()` de `wwwroot/js` sea siempre same-origin. `SsoAdmin.API` únicamente expone `POST /api/sso/verificar` (autenticado por API key) para el consumidor externo.
- **Alternatives considered**: JWT en `localStorage` (superficie de ataque XSS mayor, innecesario para una app server-rendered de un solo host).

## 4. Persistencia y unicidad de `username`+`emisor` bajo concurrencia

- **Decision**: Índice único compuesto en EF Core (`HasIndex(c => new { c.Username, c.Emisor }).IsUnique()`), aplicado por el motor de base de datos. La capa de `Application` intenta el insert y traduce una violación de restricción única (`DbUpdateException` con el número de error específico del proveedor) a un error de dominio `400` ("la combinación ya existe" / "la credencial ya está en uso").
- **Rationale**: Resuelve la clarificación de spec (Sesión 2026-07-20, Q3): una validación previa a nivel de aplicación por sí sola no cierra la ventana de carrera entre dos solicitudes concurrentes; solo una restricción a nivel de base de datos lo garantiza atómicamente.
- **Alternatives considered**: Lock aplicativo en memoria (no funciona con más de una instancia del proceso `API`); validar y luego insertar sin restricción de BD (deja la ventana de carrera abierta, ya descartado por la clarificación).

## 5. No solapamiento de períodos de `PermisoAcceso` bajo concurrencia

- **Decision**: La creación de un permiso se ejecuta dentro de una transacción EF Core con nivel de aislamiento `Serializable`, que primero consulta los períodos existentes del mismo usuario+aplicación (incluyendo el caso de permiso indefinido, edge case ya documentado) y solo si no hay solapamiento inserta el nuevo período, todo dentro de la misma transacción.
- **Rationale**: El solapamiento es una regla de rango (no una igualdad simple), por lo que no puede expresarse como una restricción única de columna(s) como en el caso de credenciales; `Serializable` evita inserciones fantasma concurrentes que pasarían la validación de solapamiento de forma independiente y luego chocarían al persistir. Este es el mecanismo que satisface el edge case "dos solicitudes concurrentes intentan crear permisos solapados" ya presente en el spec.
- **Alternatives considered**: Restricción de exclusión a nivel de BD tipo PostgreSQL `EXCLUDE USING gist` (no disponible de forma nativa en SQL Server, el motor elegido); optimistic concurrency con `rowversion` (no previene el solapamiento en sí, solo detecta ediciones concurrentes sobre la misma fila, no inserciones nuevas que se solapan).

## 6. Motor de base de datos

- **Decision**: SQL Server (LocalDB para desarrollo/tests locales, SQL Server estándar en producción), acomodado vía EF Core `UseSqlServer`.
- **Rationale**: Pairing por defecto del stack .NET/EF Core indicado en AGENTS.md, con soporte completo de índices únicos y aislamiento `Serializable` requeridos por las decisiones #4 y #5; cadena de conexión 100% externalizada (Principio II).
- **Alternatives considered**: PostgreSQL (viable técnicamente, pero sin ninguna señal en AGENTS.md/PRD que lo prefiera sobre el default de Microsoft; se descarta por no agregar valor y apartarse del stack implícito).

## 7. Framework y estrategia de testing

- **Decision**: xUnit como framework de tests; tests de integración con `WebApplicationFactory<Program>` contra `SsoAdmin.API` y `SsoAdmin.Web`, usando el proveedor relacional `Microsoft.EntityFrameworkCore.Sqlite` (modo archivo temporal o `:memory:` con conexión persistente) en lugar del proveedor `InMemory` de EF Core.
- **Rationale**: El proveedor `InMemory` de EF Core no aplica restricciones únicas ni niveles de aislamiento de transacción, por lo que no puede verificar los criterios de concurrencia de las decisiones #4 y #5 ni el Principio III (criterios de aceptación verificables) para esos casos específicos. SQLite relacional sí aplica restricciones únicas reales, acercándose más al comportamiento de SQL Server que el proveedor InMemory.
- **Alternatives considered**: EF Core `InMemory` provider (descartado por la razón anterior); LocalDB real en CI (viable pero más pesado/lento que SQLite para la mayoría de los casos; se reserva como opción si algún test de `Serializable` no es reproducible fielmente en SQLite).

## 8. Validación de entrada

- **Decision**: FluentValidation, con un validador por slice de `Application` (p. ej. `CrearCredencialValidator`, `OtorgarPermisoValidator`), invocado antes de ejecutar el handler; falla de validación se traduce a `400 Bad Request` con detalle de campos.
- **Rationale**: Encaja naturalmente con la organización Vertical Slice (un validador junto a su handler, no atributos de `DataAnnotations` dispersos en DTOs compartidos); centraliza reglas como "URL no vacía" (FR-003), "`fecha_desde` <= `fecha_hasta`" (edge case) y campos requeridos del endpoint SSO (FR-009).
- **Alternatives considered**: `DataAnnotations` en los DTOs (mezcla reglas de validación con el contrato de transporte, menos expresivo para reglas cruzadas como la comparación de fechas).

## 9. Documentación del contrato HTTP

- **Decision**: Swashbuckle.AspNetCore en `SsoAdmin.API` para generar el documento OpenAPI, usado como referencia técnica adicional a `contracts/`.
- **Rationale**: Estándar de facto en ASP.NET Core, sin costo de configuración relevante, útil para que el equipo de SI y el SSO externo validen el contrato.
- **Alternatives considered**: Documentación manual únicamente (se mantiene igual en `contracts/`, pero sin Swagger se pierde la validación ejecutable/interactiva del contrato).

**Output**: Todas las decisiones de diseño técnico quedaron resueltas; no quedan marcadores `NEEDS CLARIFICATION` en `Technical Context` de `plan.md`.
