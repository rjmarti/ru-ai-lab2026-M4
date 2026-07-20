# Phase 1 Data Model: Backend de Administración para SSO

Derivado de la sección "Key Entities" de `spec.md` y de los requisitos funcionales FR-001..FR-016. Reside conceptualmente en `SsoAdmin.Models` (entidades) y se mapea en `SsoAdmin.Data` (configuraciones EF Core, índices, migraciones).

## Usuario

Persona a la que se le administra el acceso a aplicaciones a través del SSO.

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | `Guid` (PK) | Generado por el sistema |
| `Nombre` | `string` | Obligatorio, no vacío (FR-010) |
| `Activo` | `bool` | Default `true`; `false` = baja lógica (FR-006) |

**Relaciones**: 1 Usuario → N Credenciales; 1 Usuario → N PermisoAcceso.

**Reglas de negocio**:
- Dar de baja (`Activo = false`) MUST caducar en cascada todos los `PermisoAcceso` activos del usuario (fijar `FechaHasta = hoy` en cada uno) en la misma operación, en <3s (FR-006/FR-015/SC-002).
- Dar de baja a un usuario ya inactivo es idempotente (no produce error, edge case ya documentado).
- No existe flujo de reactivación en este alcance (Assumptions).

## Credencial

Identifica a un Usuario ante un proveedor de identidad externo.

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | `Guid` (PK) | Generado por el sistema |
| `UsuarioId` | `Guid` (FK → Usuario) | Obligatorio |
| `Username` | `string` | Obligatorio |
| `Emisor` | `string` | Obligatorio |

**Restricciones**:
- Índice único compuesto `(Username, Emisor)` a nivel de base de datos (research.md #4) — garantiza unicidad global incluso bajo concurrencia (FR-001).
- Un mismo `UsuarioId` no puede tener dos Credenciales con el mismo `Emisor` (se deriva del índice único: si `(Username, Emisor)` es único globalmente, dos credenciales del mismo usuario ya no pueden compartir `Emisor` salvo que compartan también `Username`, lo cual violaría igualmente el índice — FR-002 queda cubierto por el mismo índice).
- No contiene ningún campo de contraseña, hash o derivado (FR-013, verificado también por SC-006).
- Eliminación es física (borrado definitivo — Assumptions).

## Aplicacion

Sistema externo cuyo acceso es controlado por el SSO.

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | `Guid` (PK) | Generado por el sistema |
| `Nombre` | `string` | Obligatorio |
| `Url` | `string` | Obligatorio, no vacío (FR-003); usado como identificador de consulta por el SSO |

**Reglas de negocio**:
- Registro/edición con `Url` vacía → rechazado con `400` (FR-003, US4-AC1).
- Eliminación es física y permitida aunque existan permisos activos asociados; consultas SSO posteriores para esa URL responden `motivo=aplicacion_no_encontrada` (edge case ya documentado, Assumptions).

## PermisoAcceso

Vigencia de acceso de un Usuario a una Aplicación.

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | `Guid` (PK) | Generado por el sistema |
| `UsuarioId` | `Guid` (FK → Usuario) | Obligatorio |
| `AplicacionId` | `Guid` (FK → Aplicacion) | Obligatorio |
| `FechaDesde` | `date` | Obligatoria (FR-004) |
| `FechaHasta` | `date?` | Opcional; `null` = vigencia indefinida (FR-004) |

**Restricciones**:
- Para un mismo `(UsuarioId, AplicacionId)`, los períodos `[FechaDesde, FechaHasta]` (o `[FechaDesde, ∞)` si `FechaHasta` es `null`) MUST NOT solaparse, incluyendo el caso de coincidencia exacta de fechas y el caso de un período posterior a uno ya indefinido (edge cases ya documentados). Verificación transaccional `Serializable` (research.md #5).
- `FechaDesde` MUST ser `<= FechaHasta` cuando `FechaHasta` está presente (edge case, validación de entrada).
- Revocar = fijar `FechaHasta = hoy` sobre el período activo (FR-005); revocar un permiso ya vencido es idempotente (edge case).
- Un permiso con `FechaDesde` futura no habilita acceso todavía: el endpoint SSO responde `motivo=permiso_no_encontrado` en ese caso (edge case).

## LoginSI

Cuenta de un miembro de Seguridad Informática que administra el sistema. Entidad separada de `Usuario`; no participa de las verificaciones del SSO.

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | `Guid` (PK) | Generado por el sistema |
| `Usuario` | `string` | Obligatorio, único |
| `PasswordHash` | `string` | Hash no reversible vía `PasswordHasher<T>` (FR-007, research.md #2) |

**Reglas de negocio**:
- Se precarga en el primer arranque un registro `Usuario = "admin"`, `PasswordHash` = hash de `"admin"` (FR-007).
- Sin roles ni niveles diferenciados: cualquier `LoginSI` autenticado tiene acceso completo a las funciones de administración (spec, "Fuera de Alcance").
- No hay gestión de alta de nuevas cuentas de SI en este alcance (Assumptions).

## Diagrama de relaciones

```text
Usuario (1) ──< Credencial (N)      [único: Username+Emisor]
Usuario (1) ──< PermisoAcceso (N) >── (1) Aplicacion   [sin solape por Usuario+Aplicacion]

LoginSI  (independiente, sin relación con Usuario/Credencial/Aplicacion/PermisoAcceso)
```

## Trazabilidad Requisito → Entidad

| Requisito | Entidad(es) involucradas |
|---|---|
| FR-001, FR-002, FR-013 | Credencial |
| FR-003 | Aplicacion |
| FR-004, FR-005 | PermisoAcceso |
| FR-006, FR-015 | Usuario → PermisoAcceso (cascada) |
| FR-007, FR-016 | LoginSI (login SI) / API key (no es una entidad, es configuración — ver research.md #1) |
| FR-008, FR-009, FR-014 | Usuario, Credencial, Aplicacion, PermisoAcceso (lectura compuesta) |
| FR-010 | Usuario |
| FR-011 | Credencial |
| FR-012 | Aplicacion |
