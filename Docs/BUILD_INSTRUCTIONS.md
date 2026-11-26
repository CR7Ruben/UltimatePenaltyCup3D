# UltimatePenaltyCup3D — Instrucciones de Compilación

## Requisitos previos

- **Unity 6 (`6000.6.4f1`)** instalado en Windows x64.
- Ruta del editor utilizada en este proyecto: `D:\Program Files\Unity Hub\6000.6.4f1\Editor\Unity.exe`.
- Paquetes activos (en `Packages/manifest.json`):
  - Universal Render Pipeline (`com.unity.render-pipelines.universal` `17.6.0`)
  - Input System (`com.unity.inputsystem` `1.20.0`)
  - uGUI / TextMeshPro (`com.unity.ugui` `2.6.0`)
  - Unity Test Framework (`com.unity.test-framework` `1.8.0`)

---

## Inicio automático en MainMenu desde el Editor

- El script `Assets/Scripts/Editor/PlayModeStartSceneConfig.cs` (`[InitializeOnLoad]`) configura `EditorSceneManager.playModeStartScene` hacia `Assets/Scenes/MainMenu.unity` por defecto, de modo que pulsar **Play** incluso teniendo abierta `MatchScene.unity` siempre inicia en el menú principal.
- Para pruebas directas de depuración sobre `MatchScene.unity`, activa la opción explícita en el menú superior:  
  **`Ultimate Penalty Cup → Debug → Allow Direct MatchScene Play`**  
  *(Al ejecutar Play Mode Test Runner con `-runTests` o desde `InitTestScene`, el override se desactiva temporalmente de forma automática para no interferir con Unity Test Framework).*

---

## Compilación desde Unity Editor (GUI)

1. Abre el proyecto `D:\Program Files\UltimatePenaltyCup3D\UltimatePenaltyCup3D` en Unity 6.
2. En el menú superior ejecuta:
   - **`Ultimate Penalty Cup → Execute Complete Project Setup`**  
     (Genera texturas, sprites UI, materiales URP con GPU Instancing, mallas `SkinnedMesh` de 17 huesos, estadios con batching estático `StaticBatch_*`, prefabs, previsualizaciones 3D reales y reconstruye `MainMenu.unity` y `MatchScene.unity`).
3. Ejecuta las pruebas del sistema:
   - **`Ultimate Penalty Cup → Run All Automated System Tests`** (15 pruebas Edit Mode).
   - O abre *Window → General → Test Runner → PlayMode* para ejecutar la simulación real de 100 disparos.
4. Compila el ejecutable Windows x64:
   - **`Ultimate Penalty Cup → Build Windows Standalone (x64)`**
5. El ejecutable final queda en:
   `Builds/Windows/UltimatePenaltyCup3D.exe`

---

## Compilación y pruebas por línea de comandos (Batch Mode)

> **Importante:** Cierra Unity Editor antes de lanzar comandos `-batchmode` sobre el mismo proyecto para evitar conflicto de bloqueo en `Temp/UnityLockfile`.  
> **Nota sobre renderizado:** No agregues `-nographics` al ejecutar `ExecuteCompleteSetup`, `CaptureAllScreenshots` o pruebas Play Mode, ya que requieren contexto gráfico para renderizar URP y `RenderTexture`.

```powershell
$Unity = "D:\Program Files\Unity Hub\6000.6.4f1\Editor\Unity.exe"
$Proj  = "D:\Program Files\UltimatePenaltyCup3D\UltimatePenaltyCup3D"

# 1. Reconstruir proyecto completo
& $Unity -quit -batchmode -projectPath $Proj -executeMethod UltimatePenaltyCup.Editor.ProjectSetupBuilder.ExecuteCompleteSetup -logFile "$Proj\Logs\FullSetup.log"

# 2. Ejecutar suite Edit Mode (15 pruebas)
& $Unity -quit -batchmode -projectPath $Proj -executeMethod UltimatePenaltyCup.Editor.GameSystemsTestRunner.RunAllTests -logFile "$Proj\Logs\EditModeTests.log"

# 3. Ejecutar suite Play Mode (flujo completo + 100 disparos físicos + FPS; NO usar -quit con -runTests)
& $Unity -batchmode -projectPath $Proj -runTests -testPlatform PlayMode -testResults "$Proj\Logs\PlayModeResults.xml" -logFile "$Proj\Logs\PlayModeTests.log"

# 4. Capturar capturas de validación visual
& $Unity -quit -batchmode -projectPath $Proj -executeMethod UltimatePenaltyCup.Editor.VisualValidationCapture.CaptureAllScreenshots -logFile "$Proj\Logs\Screenshots.log"

# 5. Compilar ejecutable Windows x64
& $Unity -quit -batchmode -projectPath $Proj -executeMethod UltimatePenaltyCup.Editor.GameBuildPipeline.BuildWindowsStandalone -logFile "$Proj\Logs\BuildWindows.log"
```

---

## Orden oficial de escenas (Build Settings)

| Índice | Escena | Incluida |
|---|---|---|
| `0` | `Assets/Scenes/MainMenu.unity` | Sí |
| `1` | `Assets/Scenes/MatchScene.unity` | Sí |
| — | `Assets/Scenes/SampleScene.unity` | Excluida |
