# UltimatePenaltyCup3D — Informe de Pruebas y Validación Visual

**Fecha:** 2026-11-25  
**Motor:** Unity 6 (`6000.6.4f1`, URP `17.6.0`)  
**Plataforma:** Windows x64 (`1920×1080`, Quality: `PC`)  
**Hardware verificado:** Intel(R) UHD Graphics · 12th Gen Intel(R) Core(TM) i5-12450H

---

## 1. Compilación y reconstrucción limpia

- **`Logs/FullSetup5.log`:** `ExitCode: 0`, `0` errores de compilación. Generó materiales URP con GPU Instancing, texturas, mallas `SkinnedMesh` de 17 huesos, prefabs con batching estático por material en los 4 estadios (`Mesh_Venue_*`), vistas previas 3D transparentes, escenas `MainMenu.unity` y `MatchScene.unity`, y Build Settings (`MainMenu = 0`, `MatchScene = 1`, `SampleScene` excluida).
- **`Logs/BuildWindows3.log`:** `ExitCode: 0`, `BUILD SUCCEEDED! Total Size: 117.82 MB | Warnings: 7 | Errors: 0`.

---

## 2. Suite Edit Mode (`GameSystemsTestRunner` — `Logs/EditModeTests5.log`)

**Resultado:** **`15 / 15 TESTS PASSED! (100.0%)`**, `ExitCode: 0`.

1. `Save Data Integrity and Backward Compatibility` — PASS
2. `Currency Transactions & Non-Negative Balance Enforcement` — PASS
3. `Score Processing & Rule Priority (No Double Dipping)` — PASS
4. `Secret Dev Code PENALTY2026 Validation` — PASS
5. `Shop Purchasing, Collector Unlock & Duplicate Rejection` — PASS
6. `Customization, Stadium Override & Automatic Round Mode` — PASS
7. `Achievements Evaluation (Adapted PerfectStreak + No Duplicates)` — PASS
8. `Round Progression (1 to 5) & Early Target Logic` — PASS
9. `ScriptableObjects 5-Round Difficulty & Rules Verification` — PASS
10. `Character Prefabs: SkinnedMesh, Separated Face Material & Colliders` — PASS
11. `Ball (0.22m) & Goal (7.32x2.44m) FIFA Proportions` — PASS
12. `BuildSettings & Startup Scene Index (MainMenu=0, No SampleScene)` — PASS
13. `MainMenu & MatchScene Wiring (GameManager, Camera, RoundEndUI)` — PASS
14. `PlayModeStartScene Enforces MainMenu Start With Debug Bypass` — PASS
15. `Rendering & Performance Audit (GPU Instancing, No Floodlight Shadows, Batched Stadium)` — PASS

---

## 3. Suite Play Mode Real y Simulación de 100 Disparos (`Logs/PlayModeResults.xml` & `Logs/PlayModeBenchmarkReport.txt`)

**Resultado:** **`4 / 4 PASSED`**, duración `60.49 s`, `ExitCode: 0`.

- `Test01_MainMenuBootAndPanelNavigation` — PASS (inicio en `MainMenu` índice 0, singletons únicos, navegación entre los 5 submenús, compra y equipamiento en memoria).
- `Test02_RoundFlow_EarlyVictoryAndMathDefeat` — PASS (fin anticipado por meta en Ronda 1, derrota matemática al `0 + 2 < 3`, carga completa de Rondas 1–5).
- `Test03_PauseResumeRestartAndNoDuplicateManagers` — PASS (`Time.timeScale` en Pausa/Continuar, bloqueo de entrada, reinicio y retorno limpio sin duplicar `GameManager`, `EventSystem` ni `AudioListener`).
- `Test04_100ShotPhysicsGoalkeeperAndPerformanceBenchmark` — PASS (**100 disparos reales** a través de `PlayerShooter -> BallController -> GoalkeeperAI -> GoalDetector`):

### Resultados reales de la simulación física de 100 disparos (20 por ronda)

| Ronda | Disparos | Goles | Atajadas | Postes | Fuera | Tasa de atajada al arco |
|---|---:|---:|---:|---:|---:|---:|
| Ronda 1 (*Cantera*) | 20 | 14 | 3 | 1 | 2 | 17.6% |
| Ronda 2 (*Liga Local*) | 20 | 12 | 5 | 1 | 2 | 29.4% |
| Ronda 3 (*Liga Profesional*) | 20 | 12 | 6 | 0 | 2 | 33.3% |
| Ronda 4 (*Semifinal*) | 20 | 9 | 9 | 0 | 2 | 50.0% |
| Ronda 5 (*Gran Final*) | 20 | 8 | 10 | 0 | 2 | 55.6% |
| **TOTAL** | **100** | **55** | **33** | **2** | **10** | **37.5%** |

- **Evaluaciones duplicadas o perdidas:** `0 / 100` (estrictamente 1 resultado por disparo).
- *(Hallazgo crítico corregido durante estas pruebas: antes del fix en `BallController.ResetBall`, `m_AutoSyncTransforms=0` junto con `Interpolate` provocaba rebotes anómalos en `z = -11 m` al reiniciar el balón entre intentos; tras sincronizar `rb.position` + `Physics.SyncTransforms()`, `100 / 100` disparos mantuvieron trayectoria fiel al objetivo).*

### Métricas reales de rendimiento en Play Mode (con vSync desactivado para medir capacidad bruta en `Intel(R) UHD Graphics`, GameView `640×480`, Calidad `PC`)

| Escenario | Avg FPS | 1% Low FPS | Avg Frame Time |
|---|---:|---:|---:|
| `Cantera` | 2,872.1 | 1,371.6 | 0.35 ms |
| `Night` | 2,532.1 | 1,515.8 | 0.39 ms |
| `Professional` | 2,459.4 | 1,590.6 | 0.41 ms |
| `WorldCup` | 2,547.5 | 1,653.7 | 0.39 ms |
| **Global (Partido + 4 Estadios)** | **2,471.7** | **1,270.5** | **0.40 ms** |

- Memoria administrada reportada (`GC.GetTotalMemory(false)`): `34.84 MB`.
- En ejecución estándar (`MainMenu` y `MatchScene`), `GameManager` fija `Application.targetFrameRate = 60` y `QualitySettings.vSyncCount = 1` a `1920×1080`.

---

## 4. Validación visual con capturas reales (`Docs/Screenshots/` — `Logs/Screenshots3.log`)

Regeneradas **14 capturas `1920×1080`** (`ExitCode: 0`) tras la reconstrucción final e inspeccionadas visualmente:
- `01_MainMenu.png`: Muestra simultáneamente `Récord`, `Monedas`, estadio `Professional` iluminado y menú completo sin artefactos.
- `03_Shop_Balls.png`: 5 balones 3D sobre fondo transparente limpio.
- `05_Customization.png`: Vista previa inicial (`Balón Clásico`) cargada y estado `BLOQUEADO` en artículos no comprados.
- `06_Achievements.png`: Barras de progreso en vivo y medallas por logro.
- `08_Match_Gameplay.png` y `08b_Character_Ruben_CloseUp.png`: Encuadre deportivo en tercera persona sin obstruir portería, punto penal `0.18 m`, Malágon de frente en línea de gol y HUD con indicadores circulares.

---

## 5. Prueba en vivo del ejecutable Windows x64 (`Builds/Windows/UltimatePenaltyCup3D.exe`)

- **Registros:** `Logs/PlayerSmokeTest.log` y `Logs/PlayerSmokeTest2.log`.
- **Resultados:** El ejecutable compilado (`117.82 MB`) inició directamente en `MainMenu` (`level0` sobre `Direct3D 11` / `PhysX 4.1.2`), permaneció estable (`ALIVE_AFTER_8S=True`), cerró limpiamente (`CLOSE_MAIN_WINDOW=True`) y registró **0 excepciones, 0 `NullReferenceException` y 0 errores de ejecución**.
