# UltimatePenaltyCup3D — Historial de Cambios (CHANGELOG)

## [1.2.0] — 2026-11-26 — Corrección Integral, Animaciones de 17 Huesos, Personalización y Calibración

### Modelo 3D del Jugador, Texturas, Cámara y Logo
- **`HumanoidMeshBuilder.cs`:** Hemisferios frontal y trasero independientes en torso (`BuildLoftedHemisphere`) eliminando estiramientos laterales y el `#10` espejado, shorts y piernas anatomizadas, botines unificados (`BuildSculptedBoot`) y actualización directa mediante `existing.Clear(false)` sin `CopySerialized`.
- **`GameTextureGenerator.cs`:** Eliminado el sangrado diagonal de banda en mangas de `Tex_Jersey_Ruben`, dorsal `#10` nítido en espalda (`bx=64..188`) y reintento automático de E/S en `SaveTextureToFile`.
- **`ProjectSetupBuilder.cs`:** Recorte automático de franjas laterales grises en `UltimatePenaltyCup3D_Logo_Clean.png` con transparencia en esquinas, más configuración de tres presets URP reales (`Baja`: `URP_Low.asset`, `Media`: `URP_Medium.asset`, `Alta`: `PC_RPAsset.asset`).
- **`MatchCameraController.cs` & `MenuCameraOrbit.cs`:** Cámara de partido retrocedida a `(0.58, 1.92, -17.35)` (`aimFov = 49°`) para mostrar siempre cuerpo completo hasta botines; exhibición 3D de Rubén González en `MainMenu`.

### Física de Disparo, Animaciones Articuladas e IA del Portero
- **`BallController.cs` & `PlayerShooter.cs`:** Constantes físicas compartidas y solucionador balístico unificado (`ComputeShotSolution`, `SimulateToGoalPlane`) garantizando coincidencia exacta entre flecha, retícula, trayectoria e impacto en todo rango de potencia; zona muerta de `36 px` al cargar potencia.
- **`PlayerController.cs`:** Referencias e inicialización de cadera en `Awake()` (evita hundimiento bajo el césped), 17 huesos conectados y sincronización exacta de carrera curva, apoyo, golpeo (`ContactStrike`) y seguimiento (`FollowThrough`).
- **`GoalkeeperAI.cs`:** 9 estilos activos de atajada (`GoalkeeperDiveStyle`), anticipación (`OnKickerRunUpStarted`), balanceo correcto en estirada horizontal, aterrizaje y recuperación progresiva en el césped, radios anatómicos realistas, guardas `!isActiveAndEnabled` y calibración progresiva de Rondas 1–5 (`11.8%` en R1 hasta `58.8%` en R5).

### Estadios, Personalización, Pruebas y Benchmark 1080p
- **`StadiumController.cs` & `CustomizationUI.cs`:** Prioridad `[DefaultExecutionOrder(-50)]`, aplicación estricta entre modo Automático por ronda y Estadio Fijo en las 5 rondas (`20` combinaciones verificadas), y limpieza visual de etiquetas (`(DESACTIVADO)`).
- **`StandalonePerfBenchmarkRunner.cs`:** Telemetría automatizada real a `1920×1080` sobre el ejecutable compilado (`67.0 FPS` promedio / `60.3 1% Low` en tiros reales, `60.1 FPS` estable bajo `vSync=1`, `0.00 KB/f` GC).
- **Suites de prueba:** Edit Mode **`19/19 PASS`**, Play Mode **`7/7 PASS`**, **32 capturas `1920×1080`** y compilación Windows x64 (`116.12 MB`, `0` errores).

---

## [1.1.0] — 2026-11-25 — Optimización, Fluidez, Jugabilidad y Estabilidad

### Estabilidad y Flujo de Inicio
- **`PlayModeStartSceneConfig.cs`:** Inicio obligatorio en `MainMenu.unity` (índice 0) salvo modo de pruebas.
- **`GameManager.cs` / `SaveManager.cs` / `AudioManager.cs`:** `60 FPS` + `vSync=1`, deduplicación de `EventSystem` / `AudioListener`, flush opcional a disco y síntesis sin GC.

### Física del Balón, Disparo, IA del Portero y Cámara
- **`BallController.cs` & `GoalDetector.cs`:** Corrección de `ResetBall()` (`rb.position` + `Physics.SyncTransforms()`) y garantía de 1 sola evaluación por disparo.
- **`PC_RPAsset.asset` & `CharacterAndStadiumUpgrader.cs`:** GPU Instancing, eliminación de sombras en reflectores secundarios y combinación estática de mallas por material en estadios.

---

## [1.0.0] — 2026-11-25 — Corrección Integral, Rediseño Visual y Finalización

- Estructura base de 5 rondas progresivas, portero Malágon, `RoundEndManager`, `HumanoidMeshBuilder`, 4 estadios e interfaz rediseñada con vistas previas 3D transparentes.
