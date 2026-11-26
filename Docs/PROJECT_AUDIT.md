# UltimatePenaltyCup3D — Auditoría Técnica del Proyecto

**Fecha:** 2026-11-26  
**Proyecto:** `UltimatePenaltyCup3D` (A&R Game Labs)  
**Motor:** Unity 6 (`6000.6.4f1`, URP `17.6.0`)

---

## 1. Hallazgos clave corregidos en la auditoría funcional y visual

1. **`RoundEndUI` en objeto inactivo:** Adjunto inicialmente a `Panel_RoundEnd` (`SetActive(false)`), impedía mostrar Victoria/Derrota. Movido al controlador siempre activo `RoundEndManager`.
2. **Portero sin colliders ni `Rigidbody`:** Agregado `Rigidbody` cinemático, `CapsuleCollider` corporal, colliders en guantes, rotación `180°` en `z = -0.45f` y detección continua por segmento en `FixedUpdate`.
3. **Rondas 2–5 caían a Ronda 1:** Corregida serialización directa de `GameManager.roundConfigs` y sincronización en `RoundManager.Awake()`.
4. **Textura facial en extremidades:** Separada estrictamente entre `Mat_Skin_Body` y `Mat_Face_Ruben` / `Mat_Face_Malagon`.
5. **Build Settings y nombre oficial:** `SampleScene.unity` excluida (`MainMenu = 0`, `MatchScene = 1`), `ProductName = UltimatePenaltyCup3D`, `CompanyName = A&R Game Labs`.

---

## 2. Hallazgos críticos corregidos en la fase de Optimización, Fluidez y Estabilidad

6. **Rebotes anómalos de PhysX al reiniciar el balón (`m_AutoSyncTransforms = 0` + `Interpolate`):** Al asignar `transform.position` en `ResetBall` sin sincronizar `rb.position` ni `Physics.SyncTransforms()`, PhysX conservaba el pose previo causando rebotes extremos contra el césped en `z = -11 m`. Corregido desactivando temporalmente la interpolación, asignando `rb.position` + `Physics.SyncTransforms()` y elevando el punto de salida a `y = 0.115f`.
7. **Disparo múltiple de `OnTriggerStay` en `GoalDetector`:** Añadida bandera `BallController.GoalScoredThisShot` activada antes de disparar eventos/audio/red, más verificación continua en `FixedUpdate` con orden `[DefaultExecutionOrder(200)]`.
8. **Sobrecarga de sombras y draw calls en estadios:** Desactivadas sombras en luces `Spot` (`LightShadows.None`), desactivado `m_AdditionalLightShadowsSupported` y `m_RequireOpaqueTexture`, activado `enableInstancing = true` en materiales URP y combinadas mallas estáticas por material en `Venue_*`.
9. **Asignaciones GC en arranque y cada cuadro:** Eliminadas asignaciones de arreglos dentro de bucles en `AudioManager` y reemplazado `aimReticleRenderer.material` por `MaterialPropertyBlock`.
10. **Conflicto `playModeStartScene` con Unity Test Runner:** `PlayModeStartSceneConfig` omite el forzado de `MainMenu.unity` únicamente durante `-runTests` o cuando la escena activa es `InitTestScene`.

---

## 3. Hallazgos críticos corregidos en la fase de Corrección Real, Animaciones y Rediseño Visual

11. **Piernas del tirador enterradas en el césped durante Play Mode:** `PlayerController.Start()` inicializaba `hipsBaseLocalPos` después de que `PenaltyManager.Start()` invocara `ResetPosition()`, fijando la cadera en `Vector3.zero` (`-0.94 m` bajo el campo). Resuelto moviendo `AutoAssignRigReferences()` e inicialización de `hipsBaseLocalPos` a `Awake()` (con piso anatómico de seguridad `0.94 m` / `0.99 m`).
12. **Corrupción de streams de malla y UV lateral/trasera en jugador 3D:** `EditorUtility.CopySerialized` sobre assets `Mesh` con distinto número de vértices rompía los buffers de skinning en Unity 6. Resuelto actualizando buffers directamente con `existing.Clear(false)` sobre el asset, separando hemisferios frontal/trasero en torso para eliminar espejado del `#10`, limpiando el sangrado en mangas de `Tex_Jersey_Ruben` y esculpiendo botines dedicados.
13. **Desviación entre flecha 3D, curva de trayectoria y disparo potente:** Eliminado el desplazamiento lateral sintético (`curlSign * previewPower`) en `PlayerShooter`, compartidas las constantes reales de `BallController` (`LinearDampingValue = 0.12f`, `MagnusCoefficient = 0.0020f`, `MaxMagnusAccel = 1.1f`) con solucionador balístico unificado de 2 iteraciones (`ComputeShotSolution`) y zona muerta de `36 px` al cargar potencia.
14. **Franja gris del logo y encuadre de cámara en partido:** Recorte automático de franjas laterales grises de `UltimatePenaltyCup3D_Logo.jpg` hacia `UltimatePenaltyCup3D_Logo_Clean.png` con alfa transparente y esquinas redondeadas; ajuste de `MatchCameraController` a `(0.58, 1.92, -17.35)` (`aimFov = 49`) para garantizar visibilidad íntegra desde la cabeza hasta los botines.
15. **Corrutina sobre `Goalkeeper_Malagon` inactivo y calibración justa R1–R5:** Agregada guarda `if (!isActiveAndEnabled) return;` en `PlaySaveCelebration()`, `PlayConcededReaction()` y `ResetPosition()`; reducido el `CapsuleCollider` estático del cuerpo del portero para no bloquear estiradas laterales y calibrados `Round1..5` (R1 `11.8%` → R5 `58.8%` con flujo humano real `skipRunUp: false`).
16. **Personalización de estadio y presets gráficos reales:** `[DefaultExecutionOrder(-50)]` en `StadiumController`, aplicación estricta del estadio personalizado en las 5 rondas o modo automático por ronda, limpieza del glifo `□` en `CustomizationUI` y creación de `URP_Low.asset` / `URP_Medium.asset` / `PC_RPAsset.asset` (`Baja` / `Media` / `Alta`).
