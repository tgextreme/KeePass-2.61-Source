# KeePass 2.61 Source (Fork Modern Vibe)

Fork de KeePass 2.61 orientado a mejorar experiencia de usuario, arquitectura por servicios y estabilidad de build en entorno Windows/.NET Framework.

Repositorio: https://github.com/tgextreme/KeePass-2.61-Source

## Resumen

Este fork mantiene la base de KeePass 2.61 y añade mejoras funcionales en la capa de aplicación (`KeePass`) sin romper compatibilidad con el núcleo de datos y criptografía.

Objetivos principales:

- Mejorar UX con nuevas capacidades y formularios.
- Introducir una arquitectura más modular (services/integration/infrastructure).
- Evitar cambios destructivos en el núcleo de librería.
- Mantener build y tests estables en entornos con antivirus agresivo.

## Estado Actual (Highlights)

- Importación de credenciales migrada a flujo CSV (navegadores y formato genérico).
- Wizard de importación con:
  - Selección de formato.
  - Selección de archivo CSV.
  - Previsualización de credenciales.
  - Detección de duplicados.
  - Selección de destino antes de importar.
- Estabilización de compilación para coexistencia entre app y tests:
  - Salidas separadas para build normal y referencia de tests.
  - Configuración específica para minimizar falsos positivos AV.
- Suite de pruebas validada (233/233 en el último ciclo validado en esta rama).

## Principios de Arquitectura

- No modificar `KeePassLib` (cuando sea posible): priorizar extensiones en `KeePass`.
- Separación por capas:
  - UI (Forms/UI)
  - Services
  - Integration
  - Infrastructure
- Bajo acoplamiento y extensibilidad.
- Compatibilidad con flujo de ejecución tradicional de KeePass.

## Estructura del Repositorio

Carpetas principales:

- `KeePass/`: aplicación WinForms principal (target .NET Framework 4.8 en variante N48).
- `KeePassLib/`: librería de dominio y criptografía.
- `KeePass.Tests/`: pruebas automatizadas.
- `Translation/`: utilidades y recursos de traducción.
- `Build/`: salidas de compilación.
- `Docs/`: documentación histórica y archivos de soporte.

Soluciones destacadas:

- `KeePass_N48.sln`: solución principal para .NET Framework 4.8.
- `KeePass.sln` y `KeePass_N35.sln`: variantes heredadas/alternativas.

## Requisitos

- Windows.
- .NET Framework 4.8 Developer Pack.
- Visual Studio 2022 (recomendado).
- SDK de .NET compatible para ejecutar `dotnet test` de `KeePass.Tests`.

## Compilación

### Opción A: Visual Studio

1. Abrir `KeePass_N48.sln`.
2. Seleccionar `Debug|Any CPU` o `Release|Any CPU`.
3. Compilar solución.

### Opción B: CLI (MSBuild)

Ejemplo desde raíz del repo:

```powershell
msbuild .\KeePass_N48.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU"
```

Release:

```powershell
msbuild .\KeePass_N48.sln /t:Build /p:Configuration=Release /p:Platform="Any CPU"
```

Salidas esperadas:

- Debug app: `Build\KeePass\Debug\`
- Release app: `Build\KeePass\Release\`

## Pruebas

Desde raíz:

```powershell
dotnet test .\KeePass.Tests\KeePass.Tests.csproj --configuration Debug
```

Notas:

- El proyecto de tests referencia `KeePass_N48.csproj` con `BuildingForTests=true`.
- En ese modo, `KeePass` compila como librería para test y usa salida separada.

## Consideraciones Antivirus (AV)

Este fork incluye ajustes de compilación para reducir incidencias de cuarentena/bloqueo por heurística AV en builds de ejecutables:

- Separación de salidas para build normal vs. referencia de tests.
- Configuración de `OutputType` condicional (`WinExe` normal, `Library` para tests).
- Ajustes de símbolos/optimización en Release para entornos sensibles.
- `GenerateSerializationAssemblies` desactivado en Release para evitar rutas de tooling conflictivas.

Si tu AV sigue eliminando binarios:

- Añade exclusiones para carpeta del repo y/o salidas `Build\KeePass`.
- Repite build desde consola limpia y verifica el archivo inmediatamente tras compilar.

## Importación de Credenciales (CSV)

Flujo general:

1. Abrir opción de importación CSV desde el menú de la aplicación.
2. Seleccionar formato (Chrome/Edge/Brave/Firefox/genérico).
3. Seleccionar archivo CSV exportado desde navegador.
4. Revisar previsualización.
5. Revisar/filtrar duplicados.
6. Elegir grupo destino.
7. Confirmar importación.

Este enfoque evita dependencias de lectura directa de perfiles de navegador y mejora trazabilidad/UX del proceso.

## Desarrollo

Recomendaciones:

- Trabajar sobre `KeePass_N48.sln` como base principal.
- Mantener cambios funcionales en `KeePass/` y evitar tocar `KeePassLib/` salvo necesidad real.
- Añadir o ajustar pruebas en `KeePass.Tests/` para cada cambio relevante.
- Verificar siempre:
  - Build Debug y Release.
  - `dotnet test`.

## Troubleshooting Rápido

- Error de salida bloqueada por proceso: cerrar instancias de app/VS que puedan mantener handles.
- EXE desaparece tras compilar solución con tests: confirmar que está vigente la separación de salidas para `BuildingForTests=true`.
- Fallo por paquetes: restaurar dependencias y validar ruta `packages/`.

## Hoja de Ruta

Líneas de trabajo activas:

- Consolidación de arquitectura por servicios.
- Mejora continua de UX (wizards, flujos guiados, feedback visual).
- Endurecimiento de estabilidad build/test en entorno Windows real.
- Incremento de cobertura y escenarios de prueba.

## Licencia

Proyecto base: KeePass Password Safe.

Este repositorio mantiene la licencia del proyecto original y sus términos aplicables. Consulta cabeceras de archivo y documentación legal en `Docs/`.

## Créditos

- Proyecto original KeePass: Dominik Reichl.
- Este fork: mejoras incrementales de arquitectura, UX y estabilidad de ingeniería.