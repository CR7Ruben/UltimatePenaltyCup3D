# UltimatePenaltyCup3D — Informe de Pruebas y Validación Visual

**Fecha:** 2026-11-26  
**Estudio:** A&R Game Labs  
**Motor:** Unity 6 (`6000.6.4f1`, URP `17.6.0`)  
**Plataforma:** Windows x64 (`1920×1080`, Presets: `Baja`, `Media`, `Alta`)  
**Hardware verificado:** Intel(R) UHD Graphics (Direct3D 11) · 12th Gen Intel(R) Core(TM) i5-12450H (12 núcleos)

---

## 1. Compilación y reconstrucción limpia

- **`Logs/setup_opt_final.log`:** `ExitCode: 0`, `0` errores de compilación. Generó mallas `SkinnedMesh` humanizadas de **22 huesos + 5 BlendShapes faciales** (`Blink`, `Focus`, `Effort`, `Happy`, `Frustrated`), texturas PBR (`Tex_Jersey_Ruben.png` con dorsal **`#7`** limpio en espalda, `Tex_Face_Ruben.png` y `Tex_Face_Malagon.png` de `512×512`, `Tex_Fabric_Normal.png`, `Tex_Skin_Normal.png`), logo cuadrado transparente sin barras grises, presets URP (`URP_Low.asset`, `URP_Medium.asset`, `PC_RPAsset.asset`), público instanciado y escenas `MainMenu.unity` (índice `0`) + `MatchScene.unity` (índice `1`).
- **`Logs/BuildWindowsOptFinal.log`:** `ExitCode: 0`, **`BUILD SUCCEEDED! Total Size: 116.91 MB | Warnings: 7 | Errors: 0`** (`Builds/Windows/UltimatePenaltyCup3D.exe`).
- **`Logs/StandalonePlayerRuntime.log`:** `ExitCode: 0`, `0` excepciones y `0` advertencias de glifos en ejecución real del ejecutable compilado.

---

## 2. Suite Edit Mode (`GameSystemsTestRunner` — `Logs/EditModeTestsOptFinal.log`)

**Resultado:** **`24 / 24 TESTS PASSED! (100.0%)`**, `ExitCode: 0`.

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
17. `Player 3D Mesh Integrity (22 Bones, 5 BlendShapes, Back #7 UV & Pixel Dorsal #7)` — PASS
18. `Shop, Stadium Override Persistence & Locked Item Enforcement` — PASS
19. `Quality Presets (Baja / Media / Alta)` — PASS
20. `Goalkeeper 22-Bone Mesh, Gloves & Facial BlendShapes Integrity` — PASS
21. `Stadium Crowd Shader, CrowdManager & StadiumAmbienceController Wiring` — PASS
22. `Audio 5-Channel SaveData Backward Compatibility (crowdVolume / uiVolume)` — PASS
23. `NotificationManager Queued Toasts, Modal & Unscaled Pause Support` — PASS
24. `Mat_Stadium_Banners Clean UV Offset & Runtime Guard` — PASS

---

## 3. Suite Play Mode Real (`Logs/PlayModeResultsOptFinal.xml` & `Logs/PlayModeBenchmarkReport.txt`)

**Resultado:** **`10 / 10 PASSED`** (`total=10, passed=10, failed=0, inconclusive=0, skipped=0`), `ExitCode: 0`.

| Prueba `[UnityTest]` | Estado | Duración | Cobertura verificada |
|---|---|---:|---|
| `Test01_MainMenuBootAndPanelNavigation` | PASS | 1.21 s | Inicio limpio en `MainMenu`, singletons únicos y navegación |
| `Test02_RoundFlow_EarlyVictoryAndMathDefeat` | PASS | 4.62 s | Fin anticipado por meta, derrota matemática y Rondas 1–5 |
| `Test03_PauseResumeRestartAndNoDuplicateManagers` | PASS | 0.87 s | Pausa/Reanudación/Reinicio sin duplicar managers ni bloqueo |
| `Test04_ArrowAndTrajectoryAccuracyAcrossPowers` | PASS | 4.71 s | Correspondencia exacta retícula ↔ curva ↔ impacto del balón |
| `Test05_100ShotManualFlowGoalkeeperCalibrationAndAnimationSync` | PASS | 113.93 s | 100 disparos con carrera completa, sincronía pie-balón y calibración natural |
| `Test06_FullBodyPlayerCameraFrustumVisibility` | PASS | 0.29 s | Visibilidad completa de cabeza, torso, piernas y botines dentro del frustum |
| `Test07_All20StadiumRoundCombinationsAndPersistence` | PASS | 2.45 s | Las 20 combinaciones (`4 estadios × 5 rondas`), bloqueo de no comprados y persistencia |
| `Test08_CrowdSpawningQualityScalingAndDynamicReactions` | PASS | 6.44 s | Público GPU-instanced por estadio/calidad y reacciones en vivo |
| `Test09_LocalizedCrowdAudioAndVolumePersistence` | PASS | 0.31 s | Audio ambiental 3D en 4 sectores y persistencia `crowdVolume`/`uiVolume` |
| `Test10_NotificationsModalsAndPauseCompatibility` | PASS | 5.10 s | Toasts FIFO, logros, modales de confirmación y compatibilidad con pausa |

### Simulación física real de 100 disparos con carrera completa (20 por ronda)

| Ronda | Disparos | Goles | Atajadas | Postes | Fuera | Tasa de atajada al arco |
|---|---:|---:|---:|---:|---:|---:|
| Ronda 1 (*Cantera*) | 20 | 15 | 2 | 1 | 2 | 11.8% |
| Ronda 2 (*Liga Local*) | 20 | 11 | 6 | 1 | 2 | 35.3% |
| Ronda 3 (*Liga Profesional*) | 20 | 11 | 6 | 1 | 2 | 35.3% |
| Ronda 4 (*Semifinal*) | 20 | 7 | 10 | 1 | 2 | 58.8% |
| Ronda 5 (*Gran Final*) | 20 | 6 | 11 | 1 | 2 | 64.7% |
| **TOTAL** | **100** | **50** | **35** | **5** | **10** | **41.2%** |

- **Intentos activos de estirada del portero (`Keeper Active Dive Attempt Rate`):** `100.0%`
- **Evaluaciones duplicadas o perdidas:** `0 / 100`
- **GameView Editor (`640×480`):** Promedio `59.9 FPS` (`1% Low: 58.2 FPS`, `16.69 ms`).

---

## 4. Benchmark real en el ejecutable Windows x64 a 1920×1080 (`Logs/Standalone1080pBenchmark.txt`)

Ejecutado directamente sobre `Builds/Windows/UltimatePenaltyCup3D.exe -runPerfBenchmark1080 -screen-width 1920 -screen-height 1080 -screen-fullscreen 0` (`ExitCode: 0`, Build Release `v1.0.0`, Direct3D 11, Timestamp `2026-11-26 16:26:39`):

### Resultados finales optimizados (`1920×1080`)

| Fase medida (`1920×1080`) | Avg FPS | 1% Low FPS | Avg Frame | Max Spike | SetPass / Tris | GC/Frame |
|---|---:|---:|---:|---:|---:|---:|
| **MainMenu (120 frames)** | 48.2 | 38.5 | 20.73 ms | 39.74 ms | `23` / `33,961` | `0.00 KB/f` |
| **Estadio Cantera (90 frames)** | **72.9** | **63.0** | 13.72 ms | 16.24 ms | `38` / `79,930` | — |
| **Estadio Nocturno (90 frames)** | 56.9 | 52.3 | 17.58 ms | 19.31 ms | `42` / `63,442` | — |
| **Estadio Profesional (90 frames)** | 54.2 | 49.7 | 18.47 ms | 20.15 ms | `40` / `85,700` | — |
| **Estadio Mundial (90 frames)** | 46.9 | 43.0 | 21.31 ms | 24.24 ms | `44` / `128,914` | — |
| **Disparos reales + estiradas (6 penales)** | 54.5 | 41.1 | 18.36 ms | 51.18 ms | `44` / `106,976` | `0.00 KB/f` |
| **Estabilidad Shipping vSync=1 (60 frames)** | **60.1** | **60.1** | 16.65 ms | 16.65 ms | `44` / `86,661` | `0.00 KB/f` |
| **WorldCup + Público + Alerta — Preset `Baja` (`253` fans)** | **97.2** | **82.9** | 10.29 ms | — | `45` / `71,755` | — |
| **WorldCup + Público + Alerta — Preset `Media` (`469` fans)** | **64.1** | 46.1 | 15.59 ms | — | `43` / `95,187` | — |
| **WorldCup + Público + Alerta — Preset `Alta` (`722` fans)** | **61.6** | 56.3 | 16.23 ms | — | `46` / `130,022` | — |

- **Comparativa de optimización en `WorldCup Alta` + público + alerta UI:** Subió de `40.1 FPS` (`974` espectadores / `201,859` triángulos) antes de optimizar a **`61.6 FPS`** (`722` espectadores / `130,022` triángulos) tras optimizar mallas de público (`~45%` menos triángulos), mover iluminación instanciada a *vertex shader*, desactivar el canvas overlay en reposo y omitir `DepthPrepass` innecesario en URP.
- **Memoria:** `MonoHeap = 4.47 MB` | `UnityAllocated = 333.15 MB` | `AvgGC/Frame = 0.00 KB`.

---

## 5. Validación visual: Capturas `1920×1080` y GIFs animados

- **38 capturas `1920×1080`** en [`Docs/Screenshots/`](Screenshots/) (`Logs/VisualCaptureOptFinal.log`, `ExitCode: 0`):
  - `08b_Ruben_Front.png`, `08c_Ruben_Back.png`, `08d_Ruben_Left.png`, `08e_Ruben_Right.png`, `08f_Malagon_CloseUp.png`: Anatomía de 22 huesos, dorsal **`#7`** nítido, mangas y shorts limpios sin traspasos, rostro separado del cuerpo.
  - `Anim_Player_01_Prep.png` .. `Anim_Player_04_FollowThrough.png` y `Anim_GK_01_Ready.png` .. `Anim_GK_04_Recover.png`: Fases completas de carrera, golpeo, estirada curva y recuperación.
  - `13_Stadium_Cantera.png` .. `16_Stadium_WorldCup.png`: Cuatro sedes con densidad escalonada de público animado GPU-instanced.
  - `17_Alert_PurchaseToast.png` .. `22_Alert_SaveBroadcast.png`: Toasts de compra/error, modal de confirmación, logro `#1` (`FRANCOTIRADOR`) y banners de transmisión.
- **4 GIFs animados en movimiento** en [`Docs/Videos/`](Videos/) generados desde las secuencias reales de Play Mode (`Test05`, `Test08`, `Test10`):
  - [`PlayerKick_And_Crowd.gif`](Videos/PlayerKick_And_Crowd.gif) (`20` cuadros, `960×540`)
  - [`KeeperDive_And_Save.gif`](Videos/KeeperDive_And_Save.gif) (`20` cuadros, `960×540`)
  - [`Stadium_Crowd_Reaction.gif`](Videos/Stadium_Crowd_Reaction.gif) (`16` cuadros, `960×540`)
  - [`UI_Alerts_And_Modals.gif`](Videos/UI_Alerts_And_Modals.gif) (`18` cuadros, `960×540`)
