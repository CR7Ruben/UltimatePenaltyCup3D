# UltimatePenaltyCup3D — Sistema de Público GPU-Instanced y Audio 3D

## 1. Multitud animada GPU-Instanced (`CrowdManager.cs` & `CrowdInstanced.shader`)

- **Renderizado sin objetos individuales:** Cada estadio precalcula matrices `TRS` y propiedades por instancia (`_ShirtColor`, `_PantsColor`, `_SkinColor`, `_AnimParams` con fase, velocidad y estilo) enviadas mediante `Graphics.DrawMeshInstanced` sin proyectar ni recibir sombras.
- **Optimización geométrica y de shader:** Malla procedural reducida a `50` triángulos (espectador estándar) y `68` triángulos (espectador con banderín), iluminación principal (`GetMainLight` + `SampleSH`) calculada por vértice en `CrowdInstanced.shader` y ubicación concentrada en las gradas visibles (norte y laterales norte).
- **Escalamiento por estadio y calidad (`Alta` / `Media` / `Baja`):**
  - `Cantera` < `Night` < `Professional` < `WorldCup` (`722` espectadores en `WorldCup Alta`, `469` en `Media` = `65%`, `253` en `Baja` = `35%`).
- **Reacciones dinámicas (`CrowdReactionMode`):**
  - `Idle`: balanceo y aplauso suave.
  - `Tension` (al ejecutar disparo): inclinación hacia el campo y aplauso rápido.
  - `GoalCelebrate` (al anotar): saltos de celebración y brazos levantados.
  - `SaveGasp` (atajada de Malágon): sobresalto colectivo.
  - `MissGroan` (fallo/poste): postura abatida.
- **Banderines de esquina:** `StadiumFlagWave.cs` anima banderines en esquinas del terreno acorde al viento.

---

## 2. Audio ambiental 3D en 4 sectores (`StadiumAmbienceController.cs` & `AudioManager.cs`)

- **4 fuentes espaciales independientes:** Ubicadas en gradas **Norte**, **Sur**, **Este** y **Oeste**, variando ganancia y tono según el estadio activo y las reacciones de gol, atajada o tensión.
- **AudioManager de 5 canales (`SaveData v2`):** Control independiente para **General**, **Música**, **Efectos**, **Público (`crowdVolume = 0.85f`)** e **Interfaz (`uiVolume = 0.90f`)**, manteniendo retrocompatibilidad con partidas guardadas previas.
- **SFX de red (`SfxType.NetSwish`):** Reproducido por `GoalDetector` cuando el balón estremece las mallas de la portería.
