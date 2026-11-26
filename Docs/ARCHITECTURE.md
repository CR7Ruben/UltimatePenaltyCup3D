# UltimatePenaltyCup3D — Arquitectura del Software

## Visión general

**UltimatePenaltyCup3D** sigue una arquitectura modular orientada a objetos y dirigida por eventos en C# sobre Unity 6 (`6000.6.4f1`, URP `17.6.0`), organizada en ensamblados explícitos (`.asmdef`) para separar runtime, herramientas de editor y pruebas reales en Play Mode.

```
Assets/Scripts/UltimatePenaltyCup.Runtime.asmdef
├── MainMenu (Índice 0, Persistent Managers via DontDestroyOnLoad)
│    ├── GameManager                 (Escenas, vSync/60 FPS, calidad Baja/Media/Alta, singletons)
│    ├── GameStateManager            (Máquina de estados UI/Partida)
│    ├── RoundManager                (Catálogo de 5 RoundDataSO + sincronización)
│    ├── SaveManager                 (Persistencia JSON retrocompatible v2 en PlayerPrefs)
│    ├── CurrencyManager             (Monedas, puntos y código PENALTY2026 en Dev/Editor)
│    ├── ScoreManager                (Puntuación escalonada y rachas)
│    ├── ShopManager                 (Compras de BallItemSO / StadiumItemSO)
│    ├── CustomizationManager        (Equipar balón, estadio fijo o automático por ronda)
│    ├── AchievementManager          (7 logros, progreso en vivo, sin duplicación)
│    ├── AudioManager                (5 canales BGM/SFX/Público/UI + síntesis procedimental sin GC)
│    ├── NotificationManager         (Cola de toasts, tarjetas de logros y modales unscaledTime)
│    └── StandalonePerfBenchmarkRunner (Telemetría real 1920x1080 + presets con -runPerfBenchmark1080)
└── MatchScene (Índice 1)
     ├── PenaltyManager              (Máquina de estados del penal, fin anticipado y timeouts)
     ├── GoalkeeperAI                (IA con 9 estilos, 22 huesos, 5 BlendShapes y recuperación progresiva)
     ├── PlayerController            (Init en Awake, 22 huesos, 5 BlendShapes y contacto pie-balón)
     ├── PlayerShooter               (Solucionador balístico unificado, deadzone 36px y flecha 3D)
     ├── BallController              (Orden [200], constantes físicas compartidas, Physics.SyncTransforms)
     ├── GoalDetector                (Cruce único garantizado + SfxType.NetSwish + backup FixedUpdate)
     ├── GoalNetRipple               (Mallas dinámicas MarkDynamic y culling espacial)
     ├── MatchCameraController       (Intro cinematográfica + encuadre completo en z=-17.35 SmoothDamp)
     ├── StadiumController           (Orden [-50], 4 estadios, batching estático y vallas MPB)
     ├── CrowdManager                (Espectadores GPU-instanced CrowdInstanced.shader por estadio/calidad)
     ├── StadiumFlagWave             (Ondulación eólica de banderines de esquina)
     ├── StadiumAmbienceController   (Audio ambiental 3D en 4 sectores y reacciones dinámicas)
     └── Canvas_MatchHUD             (MatchHUD banners animados, PauseManager/UI, RoundEndManager/UI)

Assets/Scripts/Editor/UltimatePenaltyCup.Editor.asmdef
└── PlayModeStartSceneConfig, ProjectSetupBuilder, HumanoidMeshBuilder (22 huesos + BlendShapes),
    CharacterAndStadiumUpgrader, GameTextureGenerator (dorsal #7, rostros 512p, normales PBR),
    UIStyleGenerator, AssetPreviewRenderer, GameSystemsTestRunner (24 tests),
    VisualValidationCapture (38 capturas 1080p), GameBuildPipeline

Assets/Tests/PlayMode/UltimatePenaltyCup.PlayModeTests.asmdef
└── GameplaySimulationPlayModeTests (10 suites [UnityTest] + captura de cuadros para GIFs)
```

---

## Presets gráficos URP (`ProjectSettings/QualitySettings.asset`)

| Nivel | Nombre UI | Render Pipeline Asset | Render Scale | MSAA | Sombras / Cascadas | Densidad Público |
|---|---|---|---:|---:|---|---:|
| `0` | **Baja** | `Assets/Settings/URP_Low.asset` | `0.80×` | Desactivado | `26 m`, `1` cascada (`512`) | `35%` |
| `1` | **Media** | `Assets/Settings/URP_Medium.asset` | `0.92×` | Desactivado | `36 m`, `1` cascada (`1024`) | `65%` |
| `2` | **Alta** | `Assets/Settings/PC_RPAsset.asset` | `1.00×` | Desactivado | `42 m`, `2` cascadas (`1024`) | `100%` |

---

## Inventario de namespaces (`Assets/Scripts/`)

| Namespace | Scripts | Responsabilidad |
|---|---|---|
| `UltimatePenaltyCup.Core` | `GameEnums`, `GameManager`, `GameStateManager`, `StandalonePerfBenchmarkRunner` | Estados globales, calidad, 60 FPS pacing y telemetría 1080p |
| `UltimatePenaltyCup.Levels` | `RoundDataSO`, `RoundManager` | Datos calibrados de rondas 1–5 y progresión |
| `UltimatePenaltyCup.Gameplay` | `PenaltyManager`, `BallController`, `GoalDetector`, `GoalNetRipple`, `MatchCameraController`, `StadiumController`, `CrowdManager`, `StadiumFlagWave`, `MenuCameraOrbit` | Ciclo del penal, física PhysX/Magnus sincronizada, cámara, 4 estadios y público GPU |
| `UltimatePenaltyCup.Player` | `PlayerController`, `PlayerShooter` | Rig de 22 huesos, 5 BlendShapes y solucionador balístico |
| `UltimatePenaltyCup.Goalkeeper` | `GoalkeeperAI` | IA de 8 estados, 22 huesos, 5 BlendShapes, 9 estilos de estirada y barrido continuo |
| `UltimatePenaltyCup.Economy` | `CurrencyManager`, `ScoreManager` | Economía, balance no negativo y bonos |
| `UltimatePenaltyCup.SaveSystem` | `SaveData`, `SaveManager` | Serialización JSON retrocompatible (incluye `crowdVolume`, `uiVolume`) |
| `UltimatePenaltyCup.Shop` | `ShopManager`, `BallItemSO`, `StadiumItemSO` | Catálogo y validación de compras |
| `UltimatePenaltyCup.Customization` | `CustomizationManager` | Equipamiento de balones y estadios |
| `UltimatePenaltyCup.Achievements` | `AchievementManager`, `AchievementSO` | Evaluación y progreso de logros |
| `UltimatePenaltyCup.Audio` | `AudioManager`, `StadiumAmbienceController` | 5 canales BGM/SFX/UI/Público 3D en 4 sectores con síntesis procedimental |
| `UltimatePenaltyCup.UI` | `MainMenuUI`, `RoundSelectUI`, `ShopUI`, `ShopItemCardUI`, `CustomizationUI`, `AchievementsUI`, `SettingsUI`, `MatchHUD`, `PauseUI`, `RoundEndUI`, `NotificationManager`, `UIPanelTransition`, `UIButtonAnimator` | Interfaz deportiva responsiva 1920×1080, alertas, modales y transiciones |
| `UltimatePenaltyCup.Editor` | `PlayModeStartSceneConfig`, `ProjectSetupBuilder`, `HumanoidMeshBuilder`, `CharacterAndStadiumUpgrader`, `GameTextureGenerator`, `UIStyleGenerator`, `AssetPreviewRenderer`, `GameSystemsTestRunner`, `VisualValidationCapture`, `GameBuildPipeline` | Inicio en MainMenu, arte PBR, rig 22 huesos, presets, 24 tests, 38 capturas y build |
