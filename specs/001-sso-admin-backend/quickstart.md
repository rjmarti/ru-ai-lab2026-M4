# Quickstart: Backend de Administración para SSO

Guía para validar la feature de punta a punta una vez implementada. Referencia `contracts/` para el detalle de cada endpoint y `data-model.md` para el detalle de cada entidad.

## Prerrequisitos

- .NET 10 SDK instalado.
- SQL Server LocalDB (desarrollo) disponible, o cadena de conexión a un SQL Server accesible.
- Repositorio en la rama `001-sso-admin-backend`.

## 1. Restaurar, compilar y probar (obligatorio antes de dar por terminada cualquier tarea — AGENTS.md §5)

Todos los comandos `dotnet` se ejecutan desde `./src` (donde vive `SsoAdmin.sln`, AGENTS.md §3).

```powershell
cd src
dotnet restore
dotnet build
dotnet test
```

Todos los tests en `SsoAdmin.Test` deben pasar, incluidos los que trazan a cada FR/AC (ver `contracts/*.md`).

## 2. Configurar secretos locales (Principio II — nunca hardcodeados)

```powershell
cd src/SsoAdmin.API
dotnet user-secrets set "ConnectionStrings:Default" "Server=(localdb)\mssqllocaldb;Database=SsoAdmin;Trusted_Connection=True;"
dotnet user-secrets set "SsoApiKey:Value" "<valor-de-desarrollo>"
```

## 3. Aplicar migraciones y seed inicial

```powershell
cd src
dotnet ef database update --project SsoAdmin.Data --startup-project SsoAdmin.API
```

Al primer arranque de `SsoAdmin.API` (o `SsoAdmin.Web`, según dónde se registre el seeder), se precarga el `LoginSI` `admin`/`admin` (FR-007).

## 4. Levantar los dos hosts

```powershell
# desde ./src
dotnet run --project SsoAdmin.API
dotnet run --project SsoAdmin.Web
```

## 5. Validar el flujo de administración (US2, US3, US4)

1. Abrir `SsoAdmin.Web`, iniciar sesión con `admin`/`admin` (US2-AC1).
2. Crear un usuario nuevo en la sección Usuarios (US2-AC3).
3. Crear una credencial para ese usuario (`username`, `emisor`) en Credenciales; intentar crear una duplicada y verificar el rechazo `400` (US3-AC1, US3-AC3).
4. Registrar una aplicación con nombre y URL en Aplicaciones (US4-AC4); intentar registrar una con URL vacía y verificar el rechazo `400` (US4-AC1).
5. Otorgar un permiso al usuario para esa aplicación con `fechaDesde`/`fechaHasta`; intentar crear uno solapado y verificar `409 Conflict` (US4-AC2).
6. Dar de baja al usuario y confirmar que el listado refleja `Activo=false` de inmediato (US2-AC4, SC-002).

## 6. Validar el endpoint SSO (US1)

```powershell
curl -X POST http://localhost:<puerto-api>/api/sso/verificar `
  -H "Content-Type: application/json" `
  -H "X-Api-Key: <valor-de-desarrollo>" `
  -d '{ "username": "<username>", "emisor": "<emisor>", "aplicacionUrl": "<url>" }'
```

Casos a probar manual o vía tests de integración (ver `contracts/sso-verificar.md` para la matriz completa):

- Usuario/credencial/permiso vigentes → `200 { "allowed": true }`.
- Tras dar de baja al usuario del paso 5.6 → `200 { "allowed": false, "motivo": "usuario_inactivo" }`.
- Tras revocar el permiso → `200 { "allowed": false, "motivo": "permiso_vencido" }`.
- Sin header `X-Api-Key` → `401`.
- Falta algún campo del body → `400`.

## 7. Validar rendimiento de referencia

- Con datos de referencia cargados (hasta 100 aplicaciones, 3000 usuarios), medir que `POST /api/sso/verificar` responde en <500ms (FR-014/SC-001) y que la baja lógica de un usuario con permisos en múltiples aplicaciones caduca todos sus permisos en <3s (FR-015/SC-002).

## 8. Validar ausencia de contraseñas de usuarios finales (SC-006)

- Inspeccionar la tabla `Credencial` en la base de datos y confirmar que no existe ninguna columna ni valor que contenga una contraseña, hash o derivado.
