# UltimatePenaltyCup3D — Informe de Pruebas y Validación Visual

**Fecha:** 2026-11-25  
**Motor:** Unity 6 (`6000.6.4f1`, URP)  
**Plataforma:** Windows x64

---

## 1. Compilación de scripts

- **`Logs/FullSetup2.log`:** `ExitCode: 0`, 0 errores de compilación. Se generaron mallas `SkinnedMesh` de 17 huesos, prefabs, previsualizaciones 3D reales, `MainMenu.unity`, `MatchScene.unity` y `EditorBuildSettings`.
- **`Logs/Editor.log` (tras ediciones de pulido visual y pruebas):** `initialDomainReloadingComplete`, **0 errores `error CS`**.

---

## 2. Suite automatizada (`GameSystemsTestRunner`)

### Ejecución observada (`Logs/Tests.log` — previa al ajuste `BindForTest`)
- **Aprobadas (7/13):**
  - `1. Save Data Integrity and Backward Compatibility` — PASS
  - `3. Score Processing & Rule Priority (No Double Dipping)` — PASS
  - `9. ScriptableObjects 5-Round Difficulty & Rules Verification` — PASS
  - `10. Character Prefabs: SkinnedMesh, Separated Face Material & Colliders` — PASS
  - `11. Ball (0.22m) & Goal (7.32x2.44m) FIFA Proportions` — PASS
  - `12. BuildSettings & Startup Scene Index (MainMenu=0, No SampleScene)` — PASS
  - `13. MainMenu & MatchScene Wiring (GameManager, Camera, RoundEndUI)` — PASS
- **Fallidas en `Logs/Tests.log` (6/13 por inicialización Edit Mode / `PlayerPrefs`):**
  - Tests 2, 4, 5, 6, 7, 8 fallaron porque `SaveManager.Awake()` y `CurrencyManager.Awake()` no se ejecutan automáticamente en Edit Mode fuera de Play Mode.
- **Corrección implementada y pendiente de re-ejecución:** Se añadieron `SaveManager.SetTestDataInMemory()` (sin tocar `PlayerPrefs` del usuario) y `CurrencyManager.BindForTest()` en `GameSystemsTestRunner.cs`. La re-ejecución batch (`Logs/TestsFinal.log`) quedó bloqueada porque Unity Editor GUI (PID `19884`) abrió el proyecto bloqueando `Temp/UnityLockfile`.

---

## 3. Validación visual con capturas reales (`Docs/Screenshots/`)

Se capturaron **14 capturas reales en `1920×1080`** mediante `VisualValidationCapture.CaptureAllScreenshots` (`Logs/Screenshots.log`, `ExitCode: 0`) con GPU activa (`Intel(R) UHD Graphics`) e inspeccionadas visualmente:

| Captura | Estado inspeccionado | Hallazgos / Acciones tomadas |
|---|---|---|
| `01_MainMenu.png` | Estadio 3D y menú | Se corrigió pivot de `TopRightBadges` para mostrar Monedas + Récord, fondo de escudo sin bordes grises y estadio `Professional` (en `ProjectSetupBuilder.cs`, pendiente de regeneración de escena). |
| `02_RoundSelect.png` | 5 tarjetas con preview real | Layout limpio sin superposición de `BLOQUEADO`; muestra vistas reales de los estadios. |
| `03_Shop_Balls.png` | 5 balones 3D reales | Se cambió fondo de `AssetPreviewRenderer` a transparente (`Color.clear`) para eliminar recuadros oscuros (pendiente de re-renderizado). |
| `04_Shop_Stadiums.png` | 4 estadios reales | Tarjetas limpias, alineadas, con precios e imágenes reales. |
| `05_Customization.png` | Inspector + cuadrícula | Se agregó `isCustomizationOnly` (`BLOQUEADO` para no adquiridos) y se asignó sprite inicial a `ImgActivePreview` (pendiente de regeneración). |
| `06_Achievements.png` | Lista de logros | Se amplió contenedor vertical a `830 px` para evitar recorte de la tarjeta 7 (`Leyenda`) y se pobló el progreso (pendiente de regeneración). |
| `07_Settings.png` | Controles de opciones | Todas las filas alineadas limpiamente sin superposición ni recortes. |
| `08_Match_Gameplay.png` | Cámara deportiva y HUD | HUD broadcast claro; se corrigió inversión horizontal de UV `#10` en espalda, mangas y tamaño de punto penal `0.18 m` en `HumanoidMeshBuilder.cs` / `CharacterAndStadiumUpgrader.cs` (pendiente de regeneración). |
| `08b_Character_Ruben_CloseUp.png` | Close-up Rubén 3D | Anatomía unificada `SkinnedMesh` sin extremidades flotantes y rostro únicamente en cabeza; se refinaron botines en `HumanoidMeshBuilder.cs`. |
| `08c_Character_Malagon_CloseUp.png` | Close-up Malágon 3D | Guantes, shorts, calcetas y portero orientados al tirador con colliders activos. |
| `09_Match_Pause.png` | Modal de pausa | Limpio y centrado, sin superposición de controles. |
| `10_Match_Victory.png` | Pantalla de victoria | Muestra ronda, goles, intentos, puntuación, monedas, récord y botones `SIGUIENTE RONDA` / `REPETIR RONDA` / `VOLVER AL MENÚ`. |
| `11_Match_Defeat.png` | Pantalla de derrota | Clara, con `REINTENTAR` y `VOLVER AL MENÚ`. |
| `12_Match_GrandFinal.png` | Pantalla Campeón | Trofeo dorado, resumen final, logros y botones `VOLVER A JUGAR` / `VER LOGROS` / `VOLVER AL MENÚ`. |

---

## 4. Bloqueo pendiente y pruebas manuales pendientes

- **Bloqueo técnico real:** Unity Editor GUI (PID `19884`) mantiene abierto el proyecto (`Temp/UnityLockfile`), impidiendo ejecutar en `-batchmode` la regeneración final (`FullSetup3`), el re-test (`13/13`), las capturas actualizadas y la compilación final de `Builds/Windows/UltimatePenaltyCup3D.exe` (el ejecutable actual en disco data de `2026-11-25 17:33:47`, anterior a las correcciones de la sesión).
- **Pendiente tras cerrar Unity Editor GUI (o ejecutar desde el menú dentro del Editor):**
  1. Ejecutar `ExecuteCompleteSetup` -> `RunAllTests` (confirmar `13/13 PASS`) -> `CaptureAllScreenshots` -> `BuildWindowsStandalone`.
  2. Ejecutar smoke test manual/runtime de `Builds/Windows/UltimatePenaltyCup3D.exe` verificando jugabilidad en vivo (paradas de Malágon en rondas 1–5, fin anticipado de ronda y flujo completo de tienda/guardado).
