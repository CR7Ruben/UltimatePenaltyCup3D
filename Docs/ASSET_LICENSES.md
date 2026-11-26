# UltimatePenaltyCup3D — Licencias y Procedencia de Recursos

## Motor y paquetes oficiales

| Recurso | Versión | Licencia |
|---|---|---|
| Unity Engine | `6000.6.4f1` | Unity License |
| Universal Render Pipeline | `17.6.0` | Unity Companion / Package License |
| Unity Input System | `1.20.0` | Unity Companion / Package License |
| Unity uGUI / TextMeshPro | `2.6.0` | Unity Companion / Package License |

---

## Código fuente, modelos 3D, texturas y audio

- **Código C# (`Assets/Scripts/`):** Obra original de **A&R Game Labs** (Rubén Alberto González Camargo & Xitzia Aimara Fierro Rivera). Sin dependencias comerciales ni de pago.
- **Mallas 3D (`Assets/Art/Models/`):** Generadas procedimentalmente mediante `HumanoidMeshBuilder.cs` y `CharacterAndStadiumUpgrader.cs`. Uso libre dentro del proyecto.
- **Texturas (`Assets/Art/Textures/`), UI Sprites (`Assets/Art/UI/`) y Previews (`Assets/Art/Previews/`):** Generadas procedimentalmente por scripts propios del estudio (`GameTextureGenerator.cs`, `UIStyleGenerator.cs`, `AssetPreviewRenderer.cs`) más el logotipo oficial `UltimatePenaltyCup3D_Logo.jpg` propiedad del proyecto.
- **Audio (`AudioManager.cs`):** Síntesis matemática procedimental en tiempo real (`Mathf.Sin`) sin archivos protegidos de terceros.
- **Tipografía:** Fuente incluida por defecto en TextMeshPro (`LiberationSans SDF`, SIL Open Font License).
