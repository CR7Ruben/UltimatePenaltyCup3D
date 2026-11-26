# UltimatePenaltyCup3D — Instrucciones de Compilación

## Requisitos previos

- **Unity 6 (`6000.6.4f1`)** instalado en Windows x64.
- Ruta del editor utilizada en este proyecto: `D:\Program Files\Unity Hub\6000.6.4f1\Editor\Unity.exe`.
- Paquetes activos (en `Packages/manifest.json`):
  - Universal Render Pipeline (`com.unity.render-pipelines.universal` `17.6.0`)
  - Input System (`com.unity.inputsystem` `1.20.0`)
  - uGUI / TextMeshPro (`com.unity.ugui` `2.6.0`)

---

## Compilación desde Unity Editor (GUI)

1. Abre el proyecto `D:\Program Files\UltimatePenaltyCup3D\UltimatePenaltyCup3D` en Unity 6.
2. En el menú superior ejecuta:
   - **`Ultimate Penalty Cup → Execute Complete Project Setup`**  
     (Genera texturas, sprites de UI, materiales URP, mallas `SkinnedMesh` de 17 huesos, prefabs, previsualizaciones 3D reales, reconstruye `MainMenu.unity` y `MatchScene.unity`, y registra únicamente esas dos escenas en Build Settings con `MainMenu` en índice `0`).
3. Ejecuta las pruebas del sistema:
   - **`Ultimate Penalty Cup → Run All Automated System Tests`**
4. Compila el ejecutable Windows x64:
   - **`Ultimate Penalty Cup → Build Windows Standalone (x64)`**
5. El ejecutable final queda en:
   `Builds/Windows/UltimatePenaltyCup3D.exe`

---

## Compilación automatizada por línea de comandos (Batch Mode)

> **Importante:** Cierra Unity Editor antes de lanzar comandos `-batchmode` sobre el mismo proyecto para evitar conflicto de bloqueo en `Temp/UnityLockfile`.  
> **Nota sobre renderizado:** No agregues `-nographics` al ejecutar `ExecuteCompleteSetup`, `RenderAllPreviews` o `CaptureAllScreenshots`, ya que requieren contexto gráfico para renderizar los `RenderTexture`.

```powershell
$Unity = "D:\Program Files\Unity Hub\6000.6.4f1\Editor\Unity.exe"
$Proj  = "D:\Program Files\UltimatePenaltyCup3D\UltimatePenaltyCup3D"

# 1. Reconstruir proyecto completo (con GPU activa para previews 3D)
& $Unity -quit -batchmode -projectPath $Proj -executeMethod UltimatePenaltyCup.Editor.ProjectSetupBuilder.ExecuteCompleteSetup -logFile "$Proj\Logs\Setup.log"

# 2. Ejecutar suite de pruebas automatizadas
& $Unity -quit -batchmode -nographics -projectPath $Proj -executeMethod UltimatePenaltyCup.Editor.GameSystemsTestRunner.RunAllTests -logFile "$Proj\Logs\Tests.log"

# 3. Capturar capturas de validación visual
& $Unity -quit -batchmode -projectPath $Proj -executeMethod UltimatePenaltyCup.Editor.VisualValidationCapture.CaptureAllScreenshots -logFile "$Proj\Logs\Screenshots.log"

# 4. Compilar ejecutable Windows x64
& $Unity -quit -batchmode -nographics -projectPath $Proj -executeMethod UltimatePenaltyCup.Editor.GameBuildPipeline.BuildWindowsStandalone -logFile "$Proj\Logs\Build.log"
```

---

## Orden oficial de escenas (Build Settings)

| Índice | Escena | Incluida |
|---|---|---|
| `0` | `Assets/Scenes/MainMenu.unity` | Sí |
| `1` | `Assets/Scenes/MatchScene.unity` | Sí |
| — | `Assets/Scenes/SampleScene.unity` | Excluida |
