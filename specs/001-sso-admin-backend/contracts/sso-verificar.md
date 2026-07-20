# Contract: POST /api/sso/verificar

Contrato externo con el SSO. Es el único endpoint consumido por un sistema fuera de la organización de administración (US1, FR-008/FR-009/FR-016).

## Autenticación

- Header requerido: `X-Api-Key: <valor configurado>`
- Ausente o inválido → `401 Unauthorized`, sin evaluar el cuerpo de la solicitud (research.md #1).

## Request

```json
POST /api/sso/verificar
X-Api-Key: <api-key>
Content-Type: application/json

{
  "username": "string, requerido",
  "emisor": "string, requerido",
  "aplicacionUrl": "string, requerido"
}
```

Campo faltante → `400 Bad Request`.

## Response — 200 OK

Se devuelve `200 OK` para toda solicitud bien formada y autenticada, incluidas las denegaciones.

```json
{
  "allowed": true
}
```

```json
{
  "allowed": false,
  "motivo": "credencial_no_encontrada"
}
```

`motivo` está presente únicamente cuando `allowed=false`, con uno de los siguientes valores:

| `motivo` | Condición |
|---|---|
| `credencial_no_encontrada` | `username`+`emisor` no existen en el sistema |
| `usuario_inactivo` | El usuario dueño de la credencial está dado de baja |
| `aplicacion_no_encontrada` | `aplicacionUrl` no corresponde a ninguna aplicación registrada |
| `permiso_no_encontrado` | No existe un `PermisoAcceso` vigente (incluye `FechaDesde` futura) para ese usuario+aplicación |
| `permiso_vencido` | El `PermisoAcceso` existente ya expiró (`FechaHasta` en el pasado) |

## Response — otros códigos

| Código | Condición |
|---|---|
| `400 Bad Request` | Falta `username`, `emisor` o `aplicacionUrl` |
| `401 Unauthorized` | Header `X-Api-Key` ausente o inválido |
| `500 Internal Server Error` | Error inesperado no controlado |

## Acuerdo de rendimiento

- p100 < 500ms con hasta 100 aplicaciones y 3000 usuarios registrados (FR-014/SC-001).

## Casos de prueba mínimos (trazan a US1 Acceptance Scenarios 1-8)

1. Credencial válida + usuario activo + permiso vigente → `200 { allowed: true }`, <500ms.
2. Credencial válida + permiso vencido → `200 { allowed: false, motivo: "permiso_vencido" }`.
3. Credencial válida + usuario inactivo → `200 { allowed: false, motivo: "usuario_inactivo" }`.
4. Credencial válida + `aplicacionUrl` inexistente → `200 { allowed: false, motivo: "aplicacion_no_encontrada" }`.
5. `username`/`emisor` inexistente → `200 { allowed: false, motivo: "credencial_no_encontrada" }`.
6. Credencial válida sin permiso registrado → `200 { allowed: false, motivo: "permiso_no_encontrado" }`.
7. Falta un campo requerido → `400`.
8. Sin `X-Api-Key` o inválida → `401`.
