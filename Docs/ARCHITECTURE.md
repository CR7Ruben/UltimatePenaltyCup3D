# UltimatePenaltyCup3D — Arquitectura del Software

## Visión general

**UltimatePenaltyCup3D** sigue una arquitectura modular orientada a objetos y dirigida por eventos en C# sobre Unity 6 (`6000.6.4f1`, URP `17.6.0`), organizada en ensamblados explícitos (`.asmdef`) para separar runtime, herramientas de editor y pruebas reales en Play Mode.

```
Assets/Scripts/UltimatePenaltyCup.Runtime.asmdef
├── MainMenu (Índice 0, Persistent Managers via DontDestroyOnLoad)
│    ├── GameManager                 (Escenas, vSync/60 FPS, calidad Baja/Media/Alta, singletons)
│    ├── GameStateManager            (Máquina de estados UI/Partida)
│    ├── RoundManager                (Catálogo de 5 RoundDataSO + sincronización)
│    ├── SaveManager                 (Persistencia JSON retrocompatible en PlayerPrefs)
│    ├── CurrencyManager             (Monedas, puntos y código PENALTY2026 en Dev/Editor)
│    ├── ScoreManager                (Puntuación escalonada y rachas)
│    ├── ShopManager                 (Compras de BallItemSO / StadiumItemSO)
│    ├── CustomizationManager        (Equipar balón, estadio fijo o automático por ronda)
│    ├── AchievementManager          (7 logros, progreso en vivo, sin duplicación)
│    ├── AudioManager                (Música, SFX y síntesis procedimental sin GC por muestra)
│    └── StandalonePerfBenchmarkRunner (Telemetría real 1920x1080 con flag -runPerfBenchmark1080)
└── MatchScene (Índice 1)
     ├── PenaltyManager              (Máquina de estados del penal, fin anticipado y timeouts)
     ├── GoalkeeperAI                (IA con 9 estilos de atajada, 17 huesos y recuperación progresiva)
     ├── PlayerController            (Init en Awake, 17 huesos y contacto pie-balón sincronizado)
     ├── PlayerShooter               (Solucionador balístico unificado, deadzone 36px y flecha 3D)
     ├── BallController              (Orden [200], constantes físicas compartidas, Physics.SyncTransforms)
     ├── GoalDetector                (Cruce único garantizado + backup continuo en FixedUpdate)
     ├── GoalNetRipple               (Mallas dinámicas MarkDynamic y culling espacial)
     ├── MatchCameraController       (Encuadre de cuerpo completo en z=-17.35 con SmoothDamp)
     ├── StadiumController           (Orden [-50], 4 estadios con batching estático por material)
     └── Canvas_MatchHUD             (MatchHUD, PauseManager/PauseUI, RoundEndManager/RoundEndUI)

Assets/Scripts/Editor/UltimatePenaltyCup.Editor.asmdef
└── PlayModeStartSceneConfig, ProjectSetupBuilder, HumanoidMeshBuilder, CharacterAndStadiumUpgrader,
    GameTextureGenerator, UIStyleGenerator, AssetPreviewRenderer, GameSystemsTestRunner (19 tests),
    VisualValidationCapture (32 capturas), GameBuildPipeline

Assets/Tests/PlayMode/UltimatePenaltyCup.PlayModeTests.asmdef
└── GameplaySimulationPlayModeTests (7 suites [UnityTest]: flujo UI, balística, 100 tiros, frustum, 20 estadios)
```

---

## Presets gráficos URP (`ProjectSettings/QualitySettings.asset`)

| Nivel | Nombre UI | Render Pipeline Asset | Render Scale | MSAA | Sombras / Cascadas |
|---|---|---|---:|---:|---|
| `0` | **Baja** | `Assets/Settings/URP_Low.asset` | `0.85×` | Desactivado | `26 m`, `1` cascada (`512`) |
| `1` | **Media** | `Assets/Settings/URP_Medium.asset` | `1.00×` | `2×` | `36 m`, `2` cascadas (`1024`) |
| `2` | **Alta** | `Assets/Settings/PC_RPAsset.asset` | `1.00×` | `4×` | `45 m`, `2` cascadas (`2048`) |

---

## Inventario de namespaces (`Assets/Scripts/`)

| Namespace | Scripts | Responsabilidad |
|---|---|---|
| `UltimatePenaltyCup.Core` | `GameEnums`, `GameManager`, `GameStateManager`, `StandalonePerfBenchmarkRunner` | Estados globales, calidad, 60 FPS pacing y telemetría 1080p |
| `UltimatePenaltyCup.Levels` | `RoundDataSO`, `RoundManager` | Datos calibrados de rondas 1–5 y progresión |
| `UltimatePenaltyCup.Gameplay` | `PenaltyManager`, `BallController`, `GoalDetector`, `GoalNetRipple`, `MatchCameraController`, `StadiumController`, `MenuCameraOrbit` | Ciclo del penal, física PhysX/Magnus sincronizada, cámara y 4 estadios |
| `UltimatePenaltyCup.Player` | `PlayerController`, `PlayerShooter` | Rig de 17 huesos sincronizado con el balón y solucionador balístico |
| `UltimatePenaltyCup.Goalkeeper` | `GoalkeeperAI` | IA de 8 estados, 9 estilos de estirada y barrido continuo en `FixedUpdate` |
| `UltimatePenaltyCup.Economy` | `CurrencyManager`, `ScoreManager` | Economía, balance no negativo y bonos |
| `UltimatePenaltyCup.SaveSystem` | `SaveData`, `SaveManager` | Serialización JSON retrocompatible |
| `UltimatePenaltyCup.Shop` | `ShopManager`, `BallItemSO`, `StadiumItemSO` | Catálogo y validación de compras |
| `UltimatePenaltyCup.Customization` | `CustomizationManager` | Equipamiento de balones y estadios |
| `UltimatePenaltyCup.Achievements` | `AchievementManager`, `AchievementSO` | Evaluación y progreso de logros |
| `UltimatePenaltyCup.Audio` | `AudioManager` | BGM/SFX con síntesis matemática procedimental |
| `UltimatePenaltyCup.UI` | `MainMenuUI`, `RoundSelectUI`, `ShopUI`, `ShopItemCardUI`, `CustomizationUI`, `AchievementsUI`, `SettingsUI`, `MatchHUD`, `PauseUI`, `RoundEndUI`, `UIButtonAnimator` | Interfaz deportiva responsiva 1920×1080 |
| `UltimatePenaltyCup.Editor` | `PlayModeStartSceneConfig`, `ProjectSetupBuilder`, `HumanoidMeshBuilder`, `CharacterAndStadiumUpgrader`, `GameTextureGenerator`, `UIStyleGenerator`, `AssetPreviewRenderer`, `GameSystemsTestRunner`, `VisualValidationCapture`, `GameBuildPipeline` | Inicio en MainMenu, arte, rig 17 huesos, presets, tests, capturas y build |
