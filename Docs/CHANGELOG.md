# UltimatePenaltyCup3D — Historial de Cambios (CHANGELOG)

## [1.1.0] — 2026-11-25 — Optimización, Fluidez, Jugabilidad y Estabilidad

### Estabilidad y Flujo de Inicio
- **`PlayModeStartSceneConfig.cs`:** Al presionar Play en Unity Editor inicia obligatoriamente en `MainMenu.unity` (índice 0), conservando `Ultimate Penalty Cup → Debug → Allow Direct MatchScene Play` para depuración y evitando interferir con `-runTests` / `InitTestScene` de Unity Test Framework.
- **`GameManager.cs`:** Fija `Application.targetFrameRate = 60` y `QualitySettings.vSyncCount = 1`; deduplica automáticamente `EventSystem` y `AudioListener` en `OnSceneLoaded` y restablece siempre `Time.timeScale = 1f`.
- **`SaveManager.cs`:** Flush a disco opcional (`Save(bool flushToDisk = true)`) para evitar micro-tirones síncronos durante cada tiro, manteniendo escritura final en `FinishRound()`, `OnApplicationPause` y `OnApplicationQuit`.
- **`AudioManager.cs`:** Arreglos de notas elevados a `static readonly` eliminando cientos de miles de asignaciones temporales en GC durante la síntesis procedimental inicial.

### Física del Balón, Disparo, IA del Portero y Cámara
- **`BallController.cs`:** Corregido bug crítico detectado en la simulación de 100 disparos: `ResetBall()` desactiva `interpolation`, posiciona `rb.position` + `transform` y llama `Physics.SyncTransforms()` antes de `Launch()`, eliminando rebotes espurios con el césped (`vel > 100 m/s`) cuando `m_AutoSyncTransforms = false`. Añadido flag `GoalScoredThisShot`, `PreviousPosition` (`[DefaultExecutionOrder(200)]`) y efecto Magnus controlado.
- **`GoalDetector.cs`:** Garantía estricta de **1 sola evaluación por disparo** (bloqueando repeticiones de `OnTriggerStay`) más respaldo de cruce continuo de línea de gol en `FixedUpdate()`.
- **`PlayerShooter.cs`:** Apuntado suavizado exponencial, enfriamiento de entrada de `0.20 s` tras pausar/reiniciar para evitar clics accidentales, cero asignaciones por cuadro mediante `MaterialPropertyBlock` y arreglo preasignado de trayectoria física compensada por gravedad/arrastre. Añadido `ExecuteAutomatedShot(...)` para pruebas reales en Play Mode.
- **`PlayerController.cs` & `GoalkeeperAI.cs`:** Transiciones suaves por `Quaternion.Slerp` y `Mathf.SmoothStep` en todas las fases de carrera, golpeo, celebración, frustración, estirada baja/media/alta y aterrizaje/recuperación en el césped; interceptación por distancia mínima al segmento en `FixedUpdate()`.
- **`MatchCameraController.cs`:** Encuadre deportivo en `(0.62, 2.02, -15.35)` con paralaje sutil al apuntar y retorno suavizado con `Vector3.SmoothDamp` entre intentos.
- **`PenaltyManager.cs`:** Resolución temprana de tiros desviados o rebotados en postes mediante sondeo cada `0.08 s` y limpieza garantizada de todas las corrutinas activas en `OnDestroy()`.

### Optimización Gráfica (`60 FPS` objetivo a `1080p`)
- **URP (`PC_RPAsset.asset`):** Desactivados `m_RequireOpaqueTexture` y `m_AdditionalLightShadowsSupported`; sombras direccionales ajustadas a 2 cascadas y `ShadowDistance = 45` con `SoftShadowQuality = 2`.
- **Estadios y Materiales (`CharacterAndStadiumUpgrader` / `ProjectSetupBuilder`):** `enableInstancing = true` en materiales URP, reflectores con `LightShadows.None`, partículas de confeti optimizadas (`maxParticles = 180`), y combinación estática de mallas por material (`CombineStaticMeshesByMaterial`) en líneas de cancha y en las 4 sedes (`Venue_*`).
- **`GoalNetRipple.cs` & `StadiumController.cs`:** `MarkDynamic()`, culling por distancia cuadrada (`< 4.5 m`) y caché de IDs de shader (`Shader.PropertyToID`).

### Pruebas Reales y Compilación
- **Play Mode Test Runner (4/4 PASS + 100 disparos físicos reales):** `55` goles, `33` atajadas (`17.6%` en R1 hasta `55.6%` en R5), `2` postes, `10` fuera, `0` evaluaciones duplicadas o bloqueos; `0.40 ms` promedio por cuadro en GameView batchmode.
- **Edit Mode Test Runner (`15/15 PASS`)** y nuevo ejecutable `Builds/Windows/UltimatePenaltyCup3D.exe` (`117.82 MB`, `0` errores).

---

## [1.0.0] — 2026-11-25 — Corrección Integral, Rediseño Visual y Finalización

### Corregido (Funcionalidad y Jugabilidad)
- **Inicio y progresión:** `GameManager` recibe `roundConfigs` serializados en `MainMenu.unity` y se respalda con `RoundManager`. `SampleScene.unity` excluida.
- **Portero Malágon:** `Rigidbody` cinemático, colliders en cuerpo y guantes, rotación `180°` mirando al tirador, máquina de 8 estados y verificación continua.
- **Finalización anticipada y pantalla de fin de ronda:** `RoundEndUI` montado en objeto siempre activo `RoundEndManager`; victoria anticipada, derrota matemática y Gran Final en Ronda 5.
- **Personajes 3D (`HumanoidMeshBuilder`), césped natural, 4 estadios diferenciados y rediseño UI/UX con `AssetPreviewRenderer`.**
