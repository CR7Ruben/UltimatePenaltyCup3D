# UltimatePenaltyCup3D — Informe de Pruebas y Validación Visual

**Fecha:** 2026-11-26  
**Estudio:** A&R Game Labs  
**Motor:** Unity 6 (`6000.6.4f1`, URP `17.6.0`)  
**Plataforma:** Windows x64 (`1920×1080`, Presets: `Baja`, `Media`, `Alta`)  
**Hardware verificado:** Intel(R) UHD Graphics · 12th Gen Intel(R) Core(TM) i5-12450H (12 núcleos)

---

## 1. Compilación y reconstrucción limpia

- **`Logs/RebuildAll.log` & `Logs/SyncConfigs2.log`:** `ExitCode: 0`, `0` errores de compilación. Generó mallas `SkinnedMesh` de 17 huesos con actualización directa de buffers (sin `CopySerialized`), texturas limpias (`Tex_Jersey_Ruben` sin sangrado en mangas y dorsal `#10` derecho), logo cuadrado transparente sin barras grises (`Assets/Art/Logo/UltimatePenaltyCup3D_Logo_Clean.png`), ScriptableObjects balanceados y escenas `MainMenu.unity` (índice `0`) + `MatchScene.unity` (índice `1`).
- **`Logs/BuildWindowsFinal.log`:** `ExitCode: 0`, **`BUILD SUCCEEDED! Total Size: 116.12 MB | Warnings: 11 | Errors: 0`** (`Builds/Windows/UltimatePenaltyCup3D.exe`).

---

## 2. Suite Edit Mode (`GameSystemsTestRunner` — `Logs/EditModeTestsFinal.log`)

**Resultado:** **`19 / 19 TESTS PASSED! (100.0%)`**, `ExitCode: 0`.

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
16. `Clean Logo Asset Exists With Zero Gray Pillarboxes` — PASS
17. `Player 3D Mesh Integrity (Full Height, Legs/Boots Submeshes, 17 Bones, Back #10 UV)` — PASS
18. `Shop, Stadium Override Persistence & Locked Item Enforcement` — PASS
19. `Quality Presets (Baja / Media / Alta)` — PASS

---

## 3. Suite Play Mode Real (`Logs/PlayModeResultsFinal.xml` & `Logs/PlayModeBenchmarkReport.txt`)

**Resultado:** **`7 / 7 PASSED`** (`total=7, passed=7, failed=0, skipped=0`), `ExitCode: 0`.

| Prueba `[UnityTest]` | Estado | Duración | Cobertura verificada |
|---|---|---:|---|
| `Test01_MainMenuBootAndPanelNavigation` | PASS | 0.70 s | Inicio limpio en `MainMenu`, singletons únicos y navegación |
| `Test02_RoundFlow_EarlyVictoryAndMathDefeat` | PASS | 4.45 s | Fin anticipado por meta, derrota matemática y Rondas 1–5 |
| `Test03_PauseResumeRestartAndNoDuplicateManagers` | PASS | 0.77 s | Pausa/Reanudación/Reinicio sin duplicar managers ni bloqueo |
| `Test04_ArrowAndTrajectoryAccuracyAcrossPowers` | PASS | 4.67 s | Correspondencia exacta retícula ↔ curva ↔ impacto del balón sin desvío lateral |
| `Test05_100ShotManualFlowGoalkeeperCalibrationAndAnimationSync` | PASS | 112.64 s | 100 disparos con carrera completa (`skipRunUp: false`), sincronía pie-balón y calibración natural del portero |
| `Test06_FullBodyPlayerCameraFrustumVisibility` | PASS | 0.26 s | Visibilidad completa de cabeza, torso, piernas y botines dentro del frustum |
| `Test07_All20StadiumRoundCombinationsAndPersistence` | PASS | 1.79 s | Las 20 combinaciones (`4 estadios × 5 rondas`), bloqueo de no comprados y persistencia |

### Simulación física real de 100 disparos con carrera completa (20 por ronda)

| Ronda | Disparos | Goles | Atajadas | Postes | Fuera | Tasa de atajada al arco |
|---|---:|---:|---:|---:|---:|---:|
| Ronda 1 (*Cantera*) | 20 | 15 | 2 | 1 | 2 | 11.8% |
| Ronda 2 (*Liga Local*) | 20 | 14 | 3 | 1 | 2 | 17.6% |
| Ronda 3 (*Liga Profesional*) | 20 | 10 | 7 | 1 | 2 | 41.2% |
| Ronda 4 (*Semifinal*) | 20 | 8 | 9 | 1 | 2 | 52.9% |
| Ronda 5 (*Gran Final*) | 20 | 7 | 10 | 1 | 2 | 58.8% |
| **TOTAL** | **100** | **54** | **31** | **5** | **10** | **36.5%** |

- **Intentos activos de estirada del portero (`Keeper Active Dive Attempt Rate`):** `100.0%`
- **Evaluaciones duplicadas o perdidas:** `0 / 100`
- **GameView Editor (`640×480`):** Promedio `59.6 FPS` (`1% Low: 54.0 FPS`, `16.77 ms`).

---

## 4. Benchmark real en el ejecutable Windows x64 a 1920×1080 (`Logs/Standalone1080pBenchmark.txt`)

Ejecutado directamente sobre `Builds/Windows/UltimatePenaltyCup3D.exe -runPerfBenchmark1080 -screen-width 1920 -screen-height 1080 -screen-fullscreen 0` (`ExitCode: 0`, Build Release `v1.0.0`, Quality `Alta`, Direct3D 11):

| Fase medida (`1920×1080`) | Avg FPS | 1% Low FPS | Avg Frame | Max Spike | DrawCalls / SetPass / Tris | GC/Frame |
|---|---:|---:|---:|---:|---:|---:|
| **MainMenu (120 frames sin tope)** | **103.7** | **61.0** | 9.64 ms | 19.27 ms | `0` / `17` / `30,649` | `0.00 KB/f` |
| **Estadio Cantera (90 frames)** | **82.8** | 29.4 | 12.08 ms | 42.59 ms | `0` / `34` / `71,821` | — |
| **Estadio Nocturno (90 frames)** | **83.6** | **77.6** | 11.96 ms | 12.94 ms | `0` / `32` / `40,213` | — |
| **Estadio Profesional (90 frames)** | **79.8** | **74.5** | 12.53 ms | 13.44 ms | `0` / `32` / `40,402` | — |
| **Estadio Mundial (90 frames)** | **67.0** | **61.8** | 14.92 ms | 18.37 ms | `0` / `34` / `44,539` | — |
| **Disparos reales + estiradas (6 penales)** | **67.0** | **60.3** | 14.93 ms | 20.49 ms | `0` / `30` / `44,505` | `0.00 KB/f` |
| **Estabilidad Shipping vSync=1 (60 frames)** | **60.1** | **60.1** | 16.65 ms | 16.65 ms | `0` / `33` / `41,136` | `0.00 KB/f` |

- **Transición `MainMenu → MatchScene`:** `110.6 ms`
- **Memoria:** `MonoHeap = 3.48 MB` | `UnityAllocated = 319.35 MB` | `AvgGC/Frame = 0.00 KB`

---

## 5. Validación visual con capturas `1920×1080` (`Docs/Screenshots/` — `Logs/VisualCaptureFinal.log`)

Regeneradas **32 capturas** (`ExitCode: 0`) e inspeccionadas visualmente:
- `01_MainMenu.png`: Logo limpio con bordes redondeados transparentes sin franjas grises y jugador héroe Rubén González en vivo.
- `05b_Customization_Stadiums.png`: Pestaña de estadios con miniaturas reales y textos limpios sin glifos rotos (`(DESACTIVADO)` / `MODO AUTOMÁTICO ACTIVO`).
- `08_Match_Gameplay.png` y `08b_Ruben_Front.png` .. `08f_Malagon_CloseUp.png`: Cuerpo completo de Rubén González con piernas y botines visibles sobre el césped, dorsal `#10` nítido en espalda, mangas limpias y rostro separado del cuerpo.
- `13_Stadium_Cantera.png` .. `16_Stadium_WorldCup.png`: Los cuatro estadios diferenciados en geometría e iluminación.
- `Anim_Player_01_Prep.png` .. `Anim_Player_04_FollowThrough.png` y `Anim_GK_01_Ready.png` .. `Anim_GK_04_Recover.png`: Secuencias reales cuadro a cuadro verificando al tirador completamente erguido al golpear y al portero Malágon ejecutando su estirada lateral y recuperación sobre el césped.
