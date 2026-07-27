# Contexto del Proyecto
Este proyecto es una aplicación para administrar un Backend para un Single Sign On (SSO) 

**NombreProyecto** SsoAdmin

## 1. Contexto Tecnológico
- **Lenguaje:** C# 12
- **Frameworks:** .NET 10 
- **Frontend:** ASP.NET, Razor, Bootstrap, Javascript Vanilla.
- **Patrones de Arquitectura:** Vertical Slice Architecture, Domain-Driven Design (DDD), Inyección de Dependencias.
- **ORM:** Entity Framework Core

## 2. Convenciones de Código y Estilo
- Sigue siempre las convenciones oficiales de Microsoft para C#.
- Usa **PascalCase** para nombres de clases, métodos y propiedades.
- Usa **camelCase** para parámetros y variables locales.
- Evita el uso de variables implícitas (`var`) a menos que el tipo sea evidente en la misma línea.
- Las interfaces deben comenzar con la letra `I` (ej. `IRepository`).
- Todos los servicios deben ser inyectados mediante inyección de dependencia en el constructor.
- Documenta clases y métodos públicos utilizando comentarios XML (`/// <summary>`).

## 3. Estructura de Proyectos
Todo el código de la solución debe generarse dentro del folder `./src` (en la raíz
del repositorio). El archivo de solución (`.sln`) también debe vivir en `./src`
(ej. `./src/[NombreProyecto].sln`). Cada proyecto vive en su propia carpeta bajo `./src`:
- `./src/[NombreProyecto].Data`: Acceso y Mapeo a la base de Datos.Implementación de EF Core.
- `./src/[NombreProyecto].Models`: Entidades base.
- `./src/[NombreProyecto].Application`: Casos de uso y lógica de negocios.
- `./src/[NombreProyecto].API`: Controladores, middlewares y configuración de la API.
- `./src/[NombreProyecto].Web`: Sitio Web para administrar la aplicación
- `./src/[NombreProyecto].Test`: Casos de Test para cubrir los criterios de aceptación

## 4. Restricciones
- No expongas entidades del Dominio directamente en las respuestas de la API; utiliza DTOs (Data Transfer Objects).
- No uses `System.Console.WriteLine` para logs. Utiliza siempre `ILogger<T>`.
- Evita llamadas asíncronas bloqueantes (no uses `.Result` o `.Wait()`); utiliza siempre `await`.

## 5. Comandos y Verificaciones (CLI)
Antes de declarar una tarea como terminada, el agente debe ejecutar los siguientes comandos en la terminal desde el folder `./src` (donde vive el `.sln`):
- Restaurar dependencias: `dotnet restore`
- Construir solución: `dotnet build`
- Ejecutar pruebas: `dotnet test`
