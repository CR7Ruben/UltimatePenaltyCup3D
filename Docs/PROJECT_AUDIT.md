# UltimatePenaltyCup3D — Auditoría Técnica del Proyecto

**Fecha:** 2026-11-25  
**Proyecto:** `UltimatePenaltyCup3D` (A&R Game Labs)  
**Motor:** Unity 6 (`6000.6.4f1`, URP `17.6.0`)

---

## 1. Hallazgos clave corregidos en la auditoría funcional y visual

1. **`RoundEndUI` en objeto inactivo:** Adjunto inicialmente a `Panel_RoundEnd` (`SetActive(false)`), impedía mostrar Victoria/Derrota. Movido al controlador siempre activo `RoundEndManager`.
2. **Portero sin colliders ni `Rigidbody`:** Agregado `Rigidbody` cinemático, `CapsuleCollider` corporal, colliders en guantes, rotación `180°` en `z = -0.45f` y detección continua por segmento en `FixedUpdate`.
3. **Rondas 2–5 caían a Ronda 1:** Corregida serialización directa de `GameManager.roundConfigs` y sincronización en `RoundManager.Awake()`.
4. **`MatchCameraController` ausente y brusco:** Agregado a `Main Camera`, reencuadrado a `(0.62, 2.02, -15.35)` y suavizado con `SmoothDamp` + paralaje sutil.
5. **Textura facial en extremidades:** Separada estrictamente entre `Mat_Skin_Body` y `Mat_Face_Ruben` / `Mat_Face_Malagon`.
6. **Build Settings y nombre oficial:** `SampleScene.unity` excluida (`MainMenu = 0`, `MatchScene = 1`), `ProductName = UltimatePenaltyCup3D`, `CompanyName = A&R Game Labs`.

---

## 2. Hallazgos críticos corregidos en la fase de Optimización, Fluidez y Estabilidad

7. **Rebotes anómalos de PhysX al reiniciar el balón (`m_AutoSyncTransforms = 0` + `Interpolate`):** Detectado gracias a la simulación Play Mode de 100 disparos: al asignar `transform.position` en `ResetBall` sin sincronizar `rb.position` ni `Physics.SyncTransforms()`, PhysX conservaba el pose previo causando rebotes extremos contra el césped en `z = -11 m`. Corregido desactivando temporalmente la interpolación, asignando `rb.position` + `Physics.SyncTransforms()` y elevando el punto de salida a `y = 0.115f`.
8. **Disparo múltiple de `OnTriggerStay` en `GoalDetector`:** Añadida bandera `BallController.GoalScoredThisShot` activada antes de disparar eventos/audio/red, más verificación continua en `FixedUpdate` con orden de ejecución `[DefaultExecutionOrder(200)]` en `BallController`.
9. **Sobrecarga de sombras adicionales y draw calls en estadios:** Desactivadas sombras en luces `Spot` secundarias (`LightShadows.None`), desactivado `m_AdditionalLightShadowsSupported` y `m_RequireOpaqueTexture` en `PC_RPAsset.asset`, activado `enableInstancing = true` en materiales URP y combinadas las mallas estáticas por material en `Venue_*` y líneas de cancha (`CombineStaticMeshesByMaterial`).
10. **Asignaciones GC en arranque y cada cuadro:** Eliminadas asignaciones de arreglos de notas dentro de los bucles de síntesis de `AudioManager` y reemplazado `aimReticleRenderer.material` por `MaterialPropertyBlock`.
11. **Conflicto `playModeStartScene` con Unity Test Runner:** `PlayModeStartSceneConfig` omite el forzado de `MainMenu.unity` únicamente durante `-runTests` o cuando la escena activa es `InitTestScene`.
