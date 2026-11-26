# UltimatePenaltyCup3D — Auditoría Técnica del Proyecto

**Fecha:** 2026-11-25  
**Proyecto:** `UltimatePenaltyCup3D` (A&R Game Labs)  
**Motor:** Unity 6 (`6000.6.4f1`, URP)

---

## 1. Hallazgos clave corregidos en esta auditoría

1. **`RoundEndUI` en objeto inactivo:** Estaba adjunto directamente a `Panel_RoundEnd` (`SetActive(false)`), por lo que `Start()` nunca se ejecutaba y ninguna ronda mostraba Victoria/Derrota. Movido al controlador siempre activo `RoundEndManager`.
2. **Portero sin colliders ni `Rigidbody`:** `CharacterAndStadiumUpgrader` destruía todos los colliders de Malágon, dejándolo intangible y mirando de espaldas al tirador en `z = 0`. Agregado `Rigidbody` cinemático, `CapsuleCollider` corporal, colliders en guantes, rotación `180°` en `z = -0.45f` y barrido continuo en `FixedUpdate`.
3. **Rondas 2–5 caían a valores por defecto de Ronda 1:** `GameManager.roundConfigs` no quedaba serializado en `MainMenu.unity` porque `RoundManager.SetRounds()` usaba `GameManager.Instance` en modo Editor (`null`). Corregido asignando `gm.SetRoundConfigs(...)` directamente en `ProjectSetupBuilder` y sincronizando en `RoundManager.Awake()`.
4. **`MatchCameraController` ausente en `Main Camera`:** El generador de `MatchScene` no añadía el componente a la cámara principal. Agregado y reencuadrado.
5. **Textura facial aplicada a brazos y piernas:** `Mat_Skin` compartía `Tex_Player_Face.png`. Se separó estrictamente en `Mat_Skin_Body` (`Tex_Skin_Body.png`) para extremidades/cuello y `Mat_Face_Ruben` / `Mat_Face_Malagon` mapeados únicamente al hemisferio frontal de la cabeza.
6. **Build Settings & Nombre:** `SampleScene.unity` excluida de `EditorBuildSettings` (`MainMenu` = 0, `MatchScene` = 1), `ProductName = UltimatePenaltyCup3D`, `CompanyName = A&R Game Labs`, `Version = 1.0.0`.
7. **Auto-ejecución `[InitializeOnLoad]`:** Eliminada de `ProjectSetupBuilder.cs` para impedir sobreescrituras accidentales de escenas y prefabs al recargar dominios.
