# Contract: API de Administración (consumida por SsoAdmin.Web)

Endpoints internos usados por la app Web (Razor + Bootstrap + JS vanilla) para las funciones de SI (US2, US3, US4). Autenticación: cookie de sesión emitida por `POST /api/auth/login` (research.md #3); todos los demás endpoints de esta sección requieren `[Authorize]` (cookie válida) y devuelven `401 Unauthorized` si no la hay.

Todas las respuestas usan DTOs — nunca las entidades de `SsoAdmin.Models` (restricción AGENTS.md §4 / Constitution: DTOs en el borde de la API).

## Auth

### `POST /api/auth/login`
- Body: `{ "usuario": "string", "password": "string" }`
- `200 OK` + cookie de sesión si las credenciales son válidas.
- `401 Unauthorized` si son inválidas (US2-AC2).

### `POST /api/auth/logout`
- `200 OK`, invalida la cookie.

## Usuarios (US2, FR-010)

| Método | Ruta | Descripción | Errores |
|---|---|---|---|
| `GET` | `/api/usuarios` | Lista usuarios con `nombre`, `activo` | — |
| `POST` | `/api/usuarios` | Crea usuario (`nombre`) | `400` si `nombre` vacío |
| `PUT` | `/api/usuarios/{id}` | Edita `nombre` | `400`, `404` |
| `POST` | `/api/usuarios/{id}/baja` | Baja lógica; caduca en cascada todos los permisos activos (<3s) | `404`; idempotente si ya está inactivo |

## Credenciales (US3, FR-011)

| Método | Ruta | Descripción | Errores |
|---|---|---|---|
| `GET` | `/api/credenciales` | Lista credenciales con el usuario asociado | — |
| `POST` | `/api/credenciales` | Crea (`usuarioId`, `username`, `emisor`) | `400` si la combinación `username`+`emisor` ya existe (US3-AC1/AC2) |
| `DELETE` | `/api/credenciales/{id}` | Elimina (borrado físico) | `404` |

## Aplicaciones (US4, FR-012)

| Método | Ruta | Descripción | Errores |
|---|---|---|---|
| `GET` | `/api/aplicaciones` | Lista aplicaciones (`nombre`, `url`) | — |
| `POST` | `/api/aplicaciones` | Crea (`nombre`, `url`) | `400` si `url` vacía (US4-AC1) |
| `PUT` | `/api/aplicaciones/{id}` | Edita `nombre`/`url` | `400` si `url` vacía, `404` |
| `DELETE` | `/api/aplicaciones/{id}` | Elimina (borrado físico, aun con permisos activos asociados) | `404` |

## Permisos (US4, FR-004/FR-005)

| Método | Ruta | Descripción | Errores |
|---|---|---|---|
| `GET` | `/api/permisos?usuarioId=&aplicacionId=` | Lista permisos, con filtros opcionales | — |
| `POST` | `/api/permisos` | Otorga (`usuarioId`, `aplicacionId`, `fechaDesde`, `fechaHasta?`) | `400` si `fechaDesde > fechaHasta`; `409 Conflict` si se solapa con un período existente (US4-AC2) |
| `POST` | `/api/permisos/{id}/revocar` | Revoca, fija `fechaHasta = hoy` | `404`; idempotente si ya está vencido |

## Casos de prueba mínimos (trazan a US2/US3/US4 Acceptance Scenarios)

- Login válido → `200` + cookie; login inválido → `401` (US2-AC1/AC2).
- Listar/crear/editar/dar de baja usuario refleja el estado inmediatamente (US2-AC3/AC4).
- Crear credencial duplicada (`username`+`emisor`) → `400` (US3-AC1); reasignar credencial existente a otro usuario → `400` (US3-AC2).
- Registrar/editar aplicación con `url` vacía → `400` (US4-AC1).
- Crear permiso solapado → `409` (US4-AC2); revocar permiso activo → `fechaHasta = hoy` y consulta SSO posterior responde `permiso_vencido` (US4-AC3).
