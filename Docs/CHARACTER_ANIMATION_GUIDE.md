# UltimatePenaltyCup3D — Guía de Personajes y Animaciones (22 Huesos + BlendShapes)

## 1. Identidad visual y texturas PBR

- **Rubén González (Tirador, dorsal `#7`):**
  - Textura de jersey `Tex_Jersey_Ruben.png` con dorsal **`#7`** en espalda (`bx: 84..174, y: 156..340`), mangas limpias y escudo frontal.
  - Rostro dedicado `Tex_Face_Ruben.png` (`512×512`) separado estrictamente del material corporal `Mat_Skin_Body.mat`.
- **Malágon (Portero, dorsal `#1`):**
  - Jersey naranja/negro `Tex_Jersey_Malagon.png` con guantes acolchados dedicados (`Mat_GK_Gloves.mat` + `Tex_Gloves_Malagon.png`) y rostro `Tex_Face_Malagon.png` (`512×512`).
- **Materiales PBR realistas (`GameTextureGenerator.cs`):**
  - Mapas normales `Tex_Fabric_Normal.png` y `Tex_Skin_Normal.png` con suavidad calibrada (`0.16–0.22` tela, `0.26` piel, `0.34` guantes, `0.42` botines) para acabado textil mate sin brillo plástico.

---

## 2. Arquitectura del esqueleto de 22 huesos (`HumanoidMeshBuilder.cs`)

| Índice | Hueso | Índice | Hueso |
|---:|---|---:|---|
| `0` | `Hips` | `11` | `RightUpperArm` |
| `1` | `Spine` | `12` | `RightForearm` |
| `2` | `UpperSpine` | `13` | `RightHand` |
| `3` | `Chest` | `14` | `LeftThigh` |
| `4` | `Neck` | `15` | `LeftShin` |
| `5` | `Head` | `16` | `LeftFoot` |
| `6` | `LeftShoulder` | `17` | `LeftToe` |
| `7` | `LeftUpperArm` | `18` | `RightThigh` |
| `8` | `LeftForearm` | `19` | `RightShin` |
| `9` | `LeftHand` | `20` | `RightFoot` |
| `10` | `RightShoulder` | `21` | `RightToe` |

---

## 3. BlendShapes faciales procedurales (`SkinnedMeshRenderer`)

Ambos personajes (`Mesh_RubenGonzalez.asset` y `Mesh_GoalkeeperMalagon.asset`) incluyen 5 deformaciones faciales controladas en tiempo real desde `PlayerController` y `GoalkeeperAI`:

1. `Blink` — Parpadeo natural automático en intervalos aleatorios.
2. `Focus` — Ceño concentrado durante la carrera de aproximación o espera del portero.
3. `Effort` — Tensión en cejas y boca en el momento de impacto o estirada máxima.
4. `Happy` — Expresión de celebración tras gol o atajada.
5. `Frustrated` — Expresión de lamento tras fallo o gol recibido.

---

## 4. Ciclo de animación deportiva

- **Tirador (`PlayerController.cs` & `PlayerShooter.cs`):** Respiración y cambio de peso en reposo, carrera de aproximación curva (`Prep` → `RunUp`), apoyo de pie izquierdo y golpeo (`ContactStrike`) sincronizado con `PlayKickAnimation`, además de celebraciones y frustraciones variadas según el resultado.
- **Portero (`GoalkeeperAI.cs`):** Rebote atlético y juego de pies lateral, *split-step* anticipado al arranque del tirador, 9 estilos de atajada (`GoalkeeperDiveStyle`) con arco espinal en los 3 segmentos (`Spine`, `UpperSpine`, `Chest`), aterrizaje amortiguado sobre el césped y recuperación progresiva.
