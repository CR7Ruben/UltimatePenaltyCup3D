# UltimatePenaltyCup3D — Historial de Cambios (CHANGELOG)

## [1.3.0] — 2026-11-26 — Perfeccionamiento Visual, Humanización (22 Huesos), Público GPU y Rediseño de Alertas

### Dorsal Oficial #7, Modelado Anatómico y Materialización PBR
- **`GameTextureGenerator.cs`:** Cambio oficial del dorsal de **Rubén González** a **`#7`** en `Tex_Jersey_Ruben.png` (`bx=84..174, y=156..340`) y `#1` en **Malágon**; rostros detallados de `512×512` (`Tex_Face_Ruben.png`, `Tex_Face_Malagon.png`); generación de `Tex_Fabric_Normal.png` y `Tex_Skin_Normal.png` con suavidad calibrada (`0.16–0.26` en tela/piel, `0.42` en botines) y limpieza explícita de mapas residuales en shorts y medias.
- **`HumanoidMeshBuilder.cs` & `CharacterAndStadiumUpgrader.cs`:** Esqueleto ampliado a **22 huesos** (`UpperSpine`, `LeftShoulder`/`RightShoulder`, `LeftToe`/`RightToe`), torso anatómico de 24 segmentos, deltoides/brazos sin costura en muñeca, **5 dedos individuales** en Rubén, guantes acolchados en Malágon, cabeza esculpida con mandíbula/nariz/orejas/cabello y **5 BlendShapes faciales** (`Blink`, `Focus`, `Effort`, `Happy`, `Frustrated`).

### Animaciones Humanizadas, Expresiones, Público GPU-Instanced y Audio 3D
- **`PlayerController.cs`, `PlayerShooter.cs` & `GoalkeeperAI.cs`:** Respiración natural, parpadeo y expresiones reactivas mediante BlendShapes, variaciones orgánicas de disparo sincronizadas con el contacto del botín, juego lateral de pies, estiradas con curvatura espinal de 3 segmentos, aterrizaje amortiguado y recuperación progresiva.
- **`CrowdInstanced.shader`, `CrowdManager.cs` & `StadiumFlagWave.cs`:** Multitud animada en GPU por estadio (`Cantera < Night < Professional < WorldCup`) escalada según calidad (`35% / 65% / 100%`) con iluminación precomputada en vértice y reacciones dinámicas en vivo (`Tension`, `GoalCelebrate`, `SaveGasp`, `MissGroan`), más banderines ondeando por viento.
- **`StadiumAmbienceController.cs`, `AudioManager.cs` & `SaveData.cs`:** Audio ambiental 3D en 4 sectores del estadio, 19 `SfxType` (`NetSwish`, alertas UI, ruido filtrado cálido) y persistencia retrocompatible de 5 canales (`crowdVolume = 0.85f`, `uiVolume = 0.90f`).
- **`StadiumController.cs`:** Animación de vallas publicitarias vía `MaterialPropertyBlock` (`_BaseMap_ST`), eliminando por completo la deriva sucia en `Mat_Stadium_Banners.mat`.

### Sistema de Alertas, Modales, Pruebas y Optimización 1080p
- **`NotificationManager.cs` & `UIPanelTransition.cs`:** Cola FIFO de toasts, tarjetas de logro (`#1`), modales de confirmación no bloqueantes compatibles con pausa y desactivación automática de `NotificationOverlayCanvas` en reposo; integración en menús, tienda, opciones, pausa, `MatchHUD`, `RoundEndUI` e intro de cámara (`MatchCameraController`).
- **Suites de prueba y evidencias:** Edit Mode **`24/24 PASS`**, Play Mode **`10/10 PASS`** (100 disparos con progresión natural `R1 11.8%` → `R5 64.7%`), **38 capturas `1920×1080`** y **4 GIFs animados** (`Docs/Videos/`).
- **Optimización URP/Público y Build Windows x64:** Reducción ~45% de triángulos por espectador, ajuste URP sin `DepthPrepass` innecesario (`ProjectSetupBuilder.cs`), compilación Windows x64 (`116.91 MB`, `0` errores) y **`60.1 FPS` estable (`vSync=1`)** / **`61.6 FPS` en `WorldCup` Alta (`722` espectadores)** a `1920×1080`.

---

## [1.2.0] — 2026-11-26 — Corrección Integral, Animaciones Articuladas, Personalización y Calibración

### Modelo 3D del Jugador, Texturas, Cámara y Logo
- **`HumanoidMeshBuilder.cs`:** Hemisferios frontal y trasero independientes en torso (`BuildLoftedHemisphere`) eliminando estiramientos laterales y dorsal espejado, shorts y piernas anatomizadas, botines unificados (`BuildSculptedBoot`) y actualización directa mediante `existing.Clear(false)` sin `CopySerialized`.
- **`GameTextureGenerator.cs`:** Eliminado el sangrado diagonal de banda en mangas de `Tex_Jersey_Ruben` y reintento automático de E/S en `SaveTextureToFile`.
- **`ProjectSetupBuilder.cs`:** Recorte automático de franjas laterales grises en `UltimatePenaltyCup3D_Logo_Clean.png` con transparencia en esquinas, más configuración de tres presets URP reales (`Baja`: `URP_Low.asset`, `Media`: `URP_Medium.asset`, `Alta`: `PC_RPAsset.asset`).
- **`MatchCameraController.cs` & `MenuCameraOrbit.cs`:** Cámara de partido retrocedida a `(0.58, 1.92, -17.35)` (`aimFov = 49°`) para mostrar siempre cuerpo completo hasta botines; exhibición 3D de Rubén González en `MainMenu`.

### Física de Disparo, Animaciones Articuladas e IA del Portero
- **`BallController.cs` & `PlayerShooter.cs`:** Constantes físicas compartidas y solucionador balístico unificado (`ComputeShotSolution`, `SimulateToGoalPlane`) garantizando coincidencia exacta entre flecha, retícula, trayectoria e impacto en todo rango de potencia; zona muerta de `36 px` al cargar potencia.
- **`PlayerController.cs`:** Referencias e inicialización de cadera en `Awake()` (evita hundimiento bajo el césped), huesos conectados y sincronización exacta de carrera curva, apoyo, golpeo (`ContactStrike`) y seguimiento (`FollowThrough`).
- **`GoalkeeperAI.cs`:** 9 estilos activos de atajada (`GoalkeeperDiveStyle`), anticipación (`OnKickerRunUpStarted`), balanceo correcto en estirada horizontal, aterrizaje y recuperación progresiva en el césped, radios anatómicos realistas, guardas `!isActiveAndEnabled` y calibración progresiva de Rondas 1–5 (`11.8%` en R1 hasta `58.8%` en R5).

### Estadios, Personalización, Pruebas y Benchmark 1080p
- **`StadiumController.cs` & `CustomizationUI.cs`:** Prioridad `[DefaultExecutionOrder(-50)]`, aplicación estricta entre modo Automático por ronda y Estadio Fijo en las 5 rondas (`20` combinaciones verificadas), y limpieza visual de etiquetas (`(DESACTIVADO)`).
- **`StandalonePerfBenchmarkRunner.cs`:** Telemetría automatizada real a `1920×1080` sobre el ejecutable compilado (`67.0 FPS` promedio en tiros en línea base, `60.1 FPS` estable bajo `vSync=1`, `0.00 KB/f` GC).
- **Suites de prueba iniciales:** Edit Mode `19/19 PASS`, Play Mode `7/7 PASS`, 32 capturas `1920×1080` y compilación Windows x64 (`116.12 MB`, `0` errores).

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
