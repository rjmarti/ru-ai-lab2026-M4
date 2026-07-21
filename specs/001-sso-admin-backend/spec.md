# Feature Specification: Backend de Administración para SSO

**Feature Branch**: `001-sso-admin-backend`

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: "PRD-001: Backend para un Single Sign On (SSO) — sistema que centraliza la administración de usuarios, credenciales, aplicaciones y permisos de acceso, y expone una API para que un SSO externo consulte si una credencial tiene acceso a una aplicación determinada."

## Clarifications

### Session 2026-07-20

- Q: ¿Qué mecanismo de autenticación debe usar el endpoint `POST /api/sso/verificar` para el llamador externo (SSO)? → A: Requiere una clave de API (API key) compartida entre el SSO externo y el backend, enviada en un header.
- Q: La sección "Assumptions" menciona "Fuera de Alcance" pero esa sección no existe en la spec. ¿Qué debe declararse explícitamente fuera de alcance? → A: Autogestión de usuarios finales (self-service) — los usuarios finales no pueden iniciar sesión ni gestionar sus propias credenciales/permisos; todo lo hace SI.
- Q: ¿Cómo debe garantizarse la unicidad de `username`+`emisor` ante solicitudes concurrentes de creación de credenciales? → A: Mediante una restricción única a nivel de base de datos (unique constraint), igual que el edge case ya definido para permisos solapados.
- Q: ¿Se requiere un log de auditoría (quién hizo qué cambio administrativo y cuándo) en esta etapa? → A: No se requiere un log de auditoría explícito en esta etapa; solo el estado actual de los datos persiste.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - SSO verifica acceso de una credencial a una aplicación (Priority: P1)

Un servicio SSO externo, luego de autenticar a un usuario final ante un proveedor de identidad, necesita saber si esa credencial tiene permiso vigente para acceder a una aplicación determinada. Consulta al backend con el `username`, `emisor` y la URL de la aplicación, y recibe una respuesta clara de permitido/denegado junto con el motivo cuando corresponde.

**Why this priority**: Es la razón de ser del sistema — el contrato con el que se integra el SSO externo. Sin este endpoint funcionando de forma correcta y rápida, ninguna aplicación puede validar accesos, sin importar qué tan bien administrados estén los datos.

**Independent Test**: Puede probarse de forma independiente precargando en la base de datos un usuario activo, una credencial, una aplicación y un permiso (por API o directamente en los datos), y luego invocando `POST /api/sso/verificar` con distintas combinaciones para verificar que la respuesta (`allowed` + `motivo`) y el tiempo de respuesta sean correctos, sin necesidad de usar la interfaz web de administración.

**Acceptance Scenarios**:

1. **Given** una credencial válida asociada a un usuario activo con permiso vigente en la aplicación consultada, **When** el SSO invoca `POST /api/sso/verificar`, **Then** el sistema responde `200 OK` con `allowed=true` en menos de 500 ms.
2. **Given** una credencial válida cuyo permiso a la aplicación venció, **When** el SSO consulta, **Then** el sistema responde `200 OK` con `allowed=false` y `motivo=permiso_vencido`.
3. **Given** una credencial válida de un usuario dado de baja, **When** el SSO consulta, **Then** el sistema responde `200 OK` con `allowed=false` y `motivo=usuario_inactivo`.
4. **Given** una credencial válida y una URL de aplicación que no existe en el sistema, **When** el SSO consulta, **Then** el sistema responde `200 OK` con `allowed=false` y `motivo=aplicacion_no_encontrada`.
5. **Given** un `username` o `emisor` que no existe en el sistema, **When** el SSO consulta, **Then** el sistema responde `200 OK` con `allowed=false` y `motivo=credencial_no_encontrada`.
6. **Given** una credencial de un usuario sin ningún permiso registrado para la aplicación consultada, **When** el SSO consulta, **Then** el sistema responde `200 OK` con `allowed=false` y `motivo=permiso_no_encontrado`.
7. **Given** una solicitud a la que le falta alguno de los campos requeridos, **When** el SSO consulta, **Then** el sistema responde `400 Bad Request`.
8. **Given** una solicitud sin la clave de API o con una clave de API inválida, **When** el SSO consulta, **Then** el sistema responde `401 Unauthorized` sin evaluar la credencial.

---

### User Story 2 - SI autentica y da de baja unificada a un usuario (Priority: P2)

Un miembro de Seguridad Informática (SI) se autentica en la aplicación de administración y, ante la salida de una persona de la organización o la revocación de su acceso, da de baja lógica al usuario desde un único lugar. Esa baja debe caducar automáticamente todos los permisos activos del usuario en todas las aplicaciones, eliminando la necesidad de intervenir aplicación por aplicación.

**Why this priority**: Es el problema operativo central que motiva el proyecto — hoy dar de baja a un usuario en todas las aplicaciones toma horas. Resolver esto de forma unificada y auditable es el segundo mayor generador de valor después de que el SSO pueda consultar.

**Independent Test**: Puede probarse de forma independiente iniciando sesión con el usuario `admin` precargado, creando un usuario con permisos activos en varias aplicaciones, dándolo de baja desde la app web, y verificando que el listado refleje el estado inactivo y que una consulta SSO posterior para ese usuario devuelva `allowed=false` con `motivo=usuario_inactivo`.

**Acceptance Scenarios**:

1. **Given** un formulario de login con credenciales válidas de SI, **When** se envía el formulario, **Then** el sistema otorga acceso a las funciones de administración.
2. **Given** un formulario de login con credenciales inválidas, **When** se envía el formulario, **Then** el sistema devuelve un error `401` y no otorga acceso.
3. **Given** un usuario de SI autenticado, **When** navega a la sección de Usuarios, **Then** ve el listado de usuarios con su estado activo/inactivo, puede crear un nuevo usuario ingresando su nombre, y puede editar el nombre de un usuario existente.
4. **Given** un usuario con permisos activos en múltiples aplicaciones, **When** SI le da de baja lógica, **Then** todos sus permisos son caducados en menos de 3 segundos, el listado refleja inmediatamente el estado inactivo, y cualquier consulta posterior del SSO para ese usuario devuelve `allowed=false` con `motivo=usuario_inactivo`.

---

### User Story 3 - SI administra credenciales de un usuario (Priority: P3)

Un miembro de SI registra las credenciales (identificadas por `username` + `emisor`) que identifican a un usuario ante distintos proveedores de identidad, permitiendo que un mismo usuario tenga varias credenciales siempre que provengan de emisores distintos, y evitando que una misma combinación `username`+`emisor` quede asociada a más de un usuario.

**Why this priority**: Sin credenciales correctamente registradas y sin duplicados, el endpoint de verificación (User Story 1) no tiene datos confiables sobre los que operar — pero esta gestión puede probarse y entregar valor de forma independiente de la verificación en sí.

**Independent Test**: Puede probarse de forma independiente creando un usuario, agregándole una credencial, intentando crear una credencial duplicada (mismo `username`+`emisor`) y verificando que sea rechazada, y luego eliminando una credencial existente y confirmando que desaparece del listado.

**Acceptance Scenarios**:

1. **Given** que ya existe una credencial con `username=u1` y `emisor=google`, **When** SI intenta crear otra credencial con los mismos valores para cualquier usuario, **Then** el sistema devuelve un error `400` indicando que la combinación ya existe.
2. **Given** que la credencial (`u1`, `google`) está asignada al usuario A, **When** SI intenta asignarla al usuario B, **Then** el sistema devuelve un error `400` indicando que la credencial ya está en uso.
3. **Given** un usuario de SI autenticado en la sección de Credenciales, **When** consulta el listado, **Then** ve todas las credenciales junto con el usuario asociado, puede crear una nueva indicando usuario, `username` y `emisor`, y puede eliminar una credencial existente.
4. **Given** cualquier credencial registrada o actualizada, **When** se inspecciona su registro en la base de datos, **Then** no existe ningún campo que contenga una contraseña, hash o derivado de contraseña.

---

### User Story 4 - SI administra aplicaciones y permisos de acceso (Priority: P4)

Un miembro de SI registra las aplicaciones que participan del SSO (nombre y URL) y otorga o revoca, para un usuario y una aplicación puntual, un período de acceso con fecha de inicio obligatoria y fecha de fin opcional, asegurando que los períodos de un mismo usuario para una misma aplicación no se solapen.

**Why this priority**: Habilita el catálogo de aplicaciones y la asignación fina de accesos con vigencia temporal; depende conceptualmente de que existan usuarios y aplicaciones, por lo que se prioriza después de la gestión base de usuarios y credenciales.

**Independent Test**: Puede probarse de forma independiente registrando una aplicación con nombre y URL, otorgando un permiso a un usuario existente con fecha de inicio y fin, intentando crear un permiso solapado y verificando el rechazo, y revocando un permiso activo estableciendo su fecha de fin en la fecha actual.

**Acceptance Scenarios**:

1. **Given** que SI intenta registrar una aplicación sin URL o con URL vacía, **When** se envía la solicitud, **Then** el sistema devuelve un error `400` de validación.
2. **Given** que el usuario A tiene un permiso a la aplicación X desde `2026-01-01` hasta `2026-06-30`, **When** SI intenta crear un permiso desde `2026-03-01` hasta `2026-12-31`, **Then** el sistema devuelve un error `409 Conflict` indicando solapamiento de períodos.
3. **Given** que el usuario A tiene un permiso activo para la aplicación X, **When** SI revoca el permiso, **Then** la fecha de fin se establece en la fecha actual y cualquier consulta posterior del SSO para ese usuario y esa aplicación devuelve `allowed=false` con `motivo=permiso_vencido`.
4. **Given** un usuario de SI autenticado en la sección de Aplicaciones, **When** consulta el listado, **Then** ve todas las aplicaciones registradas, puede registrar una nueva indicando nombre y URL, editar nombre y URL de una existente, y eliminar una aplicación; el sistema impide registrar o editar una aplicación con URL vacía mostrando un mensaje de error.

---

### Edge Cases

- ¿Qué sucede si se intenta crear un permiso cuyo período coincide exactamente (mismas fechas de inicio y fin) con uno ya existente para el mismo usuario y aplicación? Debe tratarse como solapamiento y rechazarse (RF-04).
- ¿Qué sucede si un usuario ya tiene un permiso indefinido (sin `fecha_hasta`) para una aplicación y se intenta crear otro permiso posterior para la misma aplicación? Debe considerarse solapamiento, ya que el permiso indefinido cubre desde su `fecha_desde` en adelante sin límite.
- ¿Qué sucede si SI intenta otorgar un permiso con `fecha_desde` posterior a `fecha_hasta`? Debe rechazarse como error de validación.
- ¿Qué sucede si se consulta el endpoint SSO para una credencial cuyo usuario tiene un permiso vigente pero cuya `fecha_desde` es futura? Debe responder `allowed=false` con `motivo=permiso_no_encontrado`, ya que el permiso aún no está vigente.
- ¿Qué sucede si SI intenta eliminar una aplicación que tiene permisos activos asociados? El sistema debe permitir la eliminación (no hay requerimiento que lo impida) y las consultas SSO posteriores para esa aplicación deben responder `motivo=aplicacion_no_encontrada`.
- ¿Qué sucede si SI intenta dar de baja a un usuario que ya está inactivo, o revocar un permiso ya vencido? La operación debe ser idempotente y no producir error.
- ¿Qué sucede si dos solicitudes concurrentes intentan crear permisos solapados para el mismo usuario y aplicación? El sistema debe garantizar que solo una de ellas se persista exitosamente.
- ¿Qué sucede si dos solicitudes concurrentes intentan crear una credencial con la misma combinación `username`+`emisor`? El sistema debe garantizar, mediante una restricción única a nivel de base de datos, que solo una de ellas se persista exitosamente y que la otra sea rechazada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST permitir crear una credencial asociada a un usuario, compuesta por `username` y `emisor`, validando que la combinación `username`+`emisor` sea única en todo el sistema mediante una restricción única a nivel de base de datos, de forma que la unicidad se garantice incluso ante solicitudes de creación concurrentes.
- **FR-002**: El sistema MUST permitir asociar múltiples credenciales a un mismo usuario, siempre que cada una provenga de un `emisor` distinto de las demás credenciales de ese usuario.
- **FR-003**: El sistema MUST permitir registrar aplicaciones con un nombre y una URL, rechazando el registro o la edición cuando la URL esté vacía.
- **FR-004**: El sistema MUST permitir otorgar un permiso de acceso de un usuario a una aplicación, con una `fecha_desde` obligatoria y una `fecha_hasta` opcional (ausencia de `fecha_hasta` significa acceso indefinido), rechazando la creación cuando el nuevo período se solape con un período ya existente del mismo usuario para la misma aplicación.
- **FR-005**: El sistema MUST permitir revocar el permiso de acceso de un usuario a una aplicación estableciendo su `fecha_hasta` en la fecha actual.
- **FR-006**: El sistema MUST permitir dar de baja lógica a un usuario, y esa baja MUST caducar automáticamente todos los permisos activos de ese usuario en todas las aplicaciones.
- **FR-007**: El sistema MUST proveer un mecanismo de login para que los usuarios de SI se autentiquen antes de acceder a las funciones de administración, almacenando las credenciales de SI mediante un hash no reversible, y MUST precargar en el primer arranque un usuario `admin` con contraseña `admin`.
- **FR-008**: El sistema MUST exponer un endpoint `POST /api/sso/verificar` que, dado `username`, `emisor` y `aplicacion_url`, responda con `allowed` (booleano) y `motivo` (solo presente cuando `allowed=false`, con uno de los valores: `credencial_no_encontrada`, `usuario_inactivo`, `aplicacion_no_encontrada`, `permiso_no_encontrado`, `permiso_vencido`).
- **FR-009**: El endpoint de verificación MUST responder `200 OK` para toda solicitud bien formada (incluidas las denegaciones), `400 Bad Request` cuando falte algún campo requerido, `401 Unauthorized` cuando falte la clave de API o sea inválida, y `500 Internal Server Error` ante un error inesperado.
- **FR-010**: El sistema MUST permitir, a un usuario de SI autenticado, listar todos los usuarios con su estado activo/inactivo, crear un nuevo usuario, editar su nombre, y darlo de baja lógica.
- **FR-011**: El sistema MUST permitir, a un usuario de SI autenticado, listar todas las credenciales junto con el usuario asociado, crear una nueva credencial indicando usuario, `username` y `emisor`, y eliminar una credencial existente.
- **FR-012**: El sistema MUST permitir, a un usuario de SI autenticado, listar todas las aplicaciones, registrar una nueva aplicación con nombre y URL, editar nombre y URL de una aplicación existente, y eliminar una aplicación.
- **FR-013**: El sistema MUST NOT almacenar contraseñas de credenciales de usuarios finales; únicamente MUST persistir el `emisor` y el `username` de cada credencial.
- **FR-014**: El endpoint `POST /api/sso/verificar` MUST responder en menos de 500 ms bajo una carga de referencia de hasta 100 aplicaciones y 3000 usuarios registrados.
- **FR-015**: La baja lógica de un usuario MUST caducar todos sus permisos activos en menos de 3 segundos desde que se solicita la baja.
- **FR-016**: El endpoint `POST /api/sso/verificar` MUST exigir una clave de API (API key) enviada en un header de la solicitud, validarla contra un valor configurado de forma externalizada, y rechazar con `401 Unauthorized` cualquier solicitud sin clave de API o con una clave inválida, antes de evaluar la credencial.

### Key Entities

- **Usuario**: Persona a la que se le administra el acceso a aplicaciones a través del SSO. Atributos clave: nombre, estado (activo/inactivo). Se relaciona con una o más Credenciales y con cero o más Permisos.
- **Credencial**: Identifica a un Usuario ante un proveedor de identidad externo. Atributos clave: `username`, `emisor`. La combinación `username`+`emisor` es única en el sistema y pertenece exactamente a un Usuario. No contiene contraseñas ni derivados de contraseñas.
- **Aplicación**: Sistema externo cuyo acceso es controlado por el SSO. Atributos clave: nombre, URL (no vacía, usada como identificador de consulta por el SSO).
- **Permiso de Acceso**: Vigencia de acceso de un Usuario a una Aplicación. Atributos clave: `fecha_desde` (obligatoria), `fecha_hasta` (opcional; su ausencia indica vigencia indefinida). Para un mismo Usuario y Aplicación, los períodos no pueden solaparse.
- **Login (cuenta de SI)**: Cuenta de un miembro de Seguridad Informática que administra el sistema. Atributos clave: usuario, contraseña almacenada como hash no reversible. Es una entidad separada del Usuario administrado; no participa de las verificaciones del SSO.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El SSO recibe una respuesta de verificación de acceso en menos de 500 ms en el 100% de las consultas, con hasta 100 aplicaciones y 3000 usuarios registrados.
- **SC-002**: Al dar de baja a un usuario, el 100% de sus permisos activos quedan caducados dentro de los 3 segundos posteriores a la solicitud de baja.
- **SC-003**: Dar de baja a un usuario con acceso a múltiples aplicaciones requiere una única acción de SI, sin necesidad de repetir el proceso aplicación por aplicación.
- **SC-004**: El 100% de los intentos de crear una credencial con una combinación `username`+`emisor` ya existente son rechazados con un mensaje de error claro.
- **SC-005**: El 100% de los intentos de crear un permiso solapado para el mismo usuario y aplicación son rechazados con un mensaje de error claro.
- **SC-006**: Una auditoría de los registros de credenciales no encuentra ningún campo con contraseñas, hashes o derivados de contraseñas de usuarios finales.
- **SC-007**: Un miembro de SI puede completar el ciclo de alta de un usuario, sus credenciales y su permiso de acceso a una aplicación en menos de 2 minutos usando la aplicación web.

## Fuera de Alcance

- Autogestión de usuarios finales (self-service): los usuarios finales (dueños de credenciales) no tienen ningún mecanismo de login ni interfaz propia para ver o gestionar sus credenciales o permisos; toda gestión es realizada por un miembro de SI a través de la aplicación de administración.
- Log de auditoría: no se requiere un registro de auditoría (quién realizó qué cambio administrativo y cuándo) en esta etapa; el sistema solo persiste el estado actual de los datos, no su historial de cambios.
- Roles diferenciados de SI: no se requieren roles ni niveles de permiso distintos entre usuarios de SI; cualquier usuario de SI autenticado tiene acceso completo a las funciones de administración.

## Assumptions

- El "Usuario" administrado (dueño de credenciales y permisos) es una entidad de negocio distinta de la cuenta de "Login" de SI que accede a la aplicación de administración; no se requiere gestión de altas de nuevas cuentas de SI en esta etapa, solo el usuario `admin` precargado.
- La eliminación de una Credencial y de una Aplicación es física (borrado definitivo), no lógica, ya que el PRD solo especifica baja lógica explícitamente para el Usuario.
- No se requiere un flujo de reactivación de un Usuario dado de baja en esta etapa; la baja lógica es unidireccional para el alcance actual.
- El algoritmo específico de hashing para la contraseña de Login de SI queda a criterio de la fase de diseño técnico, siempre que sea no reversible (estándar de la industria).
- No se requieren roles ni niveles de permiso diferenciados entre usuarios de SI; cualquier usuario de SI autenticado tiene acceso completo a las funciones de administración (consistente con "Fuera de Alcance").
- Los 100 aplicaciones / 3000 usuarios de FR-014/SC-001 se toman como volumen de referencia para diseño y pruebas de rendimiento, no como un límite estricto impuesto al sistema.
