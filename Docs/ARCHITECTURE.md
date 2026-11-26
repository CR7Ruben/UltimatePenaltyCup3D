# UltimatePenaltyCup3D — Arquitectura del Software

## Visión general

**UltimatePenaltyCup3D** sigue una arquitectura modular orientada a objetos y dirigida por eventos en C# sobre Unity 6 (`6000.6.4f1`, URP `17.6.0`), organizada en ensamblados explícitos (`.asmdef`) para separar runtime, herramientas de editor y pruebas reales en Play Mode.

```
Assets/Scripts/UltimatePenaltyCup.Runtime.asmdef
├── MainMenu (Índice 0, Persistent Managers via DontDestroyOnLoad)
│    ├── GameManager            (Escenas, 60 FPS/vSync, deduplicación singletons, RoundConfigs)
│    ├── GameStateManager       (Máquina de estados UI/Partida)
│    ├── RoundManager           (Catálogo de 5 RoundDataSO + sincronización)
│    ├── SaveManager            (Persistencia JSON en PlayerPrefs con flush opcional)
│    ├── CurrencyManager        (Monedas, puntos y código PENALTY2026 en Dev/Editor)
│    ├── ScoreManager           (Puntuación escalonada y rachas)
│    ├── ShopManager            (Compras de BallItemSO / StadiumItemSO)
│    ├── CustomizationManager   (Equipar balón, estadio fijo o automático por ronda)
│    ├── AchievementManager     (7 logros, progreso en vivo, sin duplicación)
│    └── AudioManager           (Música, SFX y síntesis procedimental sin GC por muestra)
└── MatchScene (Índice 1)
     ├── PenaltyManager         (Máquina de estados del penal, fin anticipado y timeouts tempranos)
     ├── GoalkeeperAI           (IA de 8 estados, Slerp articular e interceptación por segmento)
     ├── PlayerController       (Locomoción procedimental suavizada sobre 17 huesos)
     ├── PlayerShooter          (Apuntado amortiguado, curva compensada, MaterialPropertyBlock)
     ├── BallController         (Orden [200], Physics.SyncTransforms en reset, Magnus controlado)
     ├── GoalDetector           (Cruce único garantizado + backup continuo en FixedUpdate)
     ├── GoalNetRipple          (Mallas dinámicas MarkDynamic y culling espacial)
     ├── MatchCameraController  (Cámara deportiva con SmoothDamp y paralaje)
     ├── StadiumController      (4 estadios con batching estático por material)
     └── Canvas_MatchHUD        (MatchHUD, PauseManager/PauseUI, RoundEndManager/RoundEndUI)

Assets/Scripts/Editor/UltimatePenaltyCup.Editor.asmdef
└── PlayModeStartSceneConfig, ProjectSetupBuilder, HumanoidMeshBuilder, CharacterAndStadiumUpgrader,
    GameTextureGenerator, UIStyleGenerator, AssetPreviewRenderer, GameSystemsTestRunner (15 tests),
    VisualValidationCapture, GameBuildPipeline

Assets/Tests/PlayMode/UltimatePenaltyCup.PlayModeTests.asmdef
└── GameplaySimulationPlayModeTests (4 suites [UnityTest] incluyendo 100 disparos y FPS)
```

---

## Inventario de namespaces (`Assets/Scripts/`)

| Namespace | Scripts | Responsabilidad |
|---|---|---|
| `UltimatePenaltyCup.Core` | `GameEnums`, `GameManager`, `GameStateManager` | Estados globales, 60 FPS pacing, deduplicación y escenas |
| `UltimatePenaltyCup.Levels` | `RoundDataSO`, `RoundManager` | Datos de rondas 1–5 y progresión |
| `UltimatePenaltyCup.Gameplay` | `PenaltyManager`, `BallController`, `GoalDetector`, `GoalNetRipple`, `MatchCameraController`, `StadiumController`, `MenuCameraOrbit` | Ciclo del penal, física PhysX sincronizada, cámara y estadios |
| `UltimatePenaltyCup.Player` | `PlayerController`, `PlayerShooter` | Animación esquelética suavizada y apuntado/potencia |
| `UltimatePenaltyCup.Goalkeeper` | `GoalkeeperAI` | IA de 8 estados, atajadas bajas/medias/altas y barrido continuo |
| `UltimatePenaltyCup.Economy` | `CurrencyManager`, `ScoreManager` | Economía, balance no negativo y bonos |
| `UltimatePenaltyCup.SaveSystem` | `SaveData`, `SaveManager` | Serialización JSON retrocompatible |
| `UltimatePenaltyCup.Shop` | `ShopManager`, `BallItemSO`, `StadiumItemSO` | Catálogo y validación de compras |
| `UltimatePenaltyCup.Customization` | `CustomizationManager` | Equipamiento de balones y estadios |
| `UltimatePenaltyCup.Achievements` | `AchievementManager`, `AchievementSO` | Evaluación y progreso de logros |
| `UltimatePenaltyCup.Audio` | `AudioManager` | BGM/SFX con síntesis matemática procedimental |
| `UltimatePenaltyCup.UI` | `MainMenuUI`, `RoundSelectUI`, `ShopUI`, `ShopItemCardUI`, `CustomizationUI`, `AchievementsUI`, `SettingsUI`, `MatchHUD`, `PauseUI`, `RoundEndUI`, `UIButtonAnimator` | Interfaz deportiva responsiva 1920×1080 |
| `UltimatePenaltyCup.Editor` | `PlayModeStartSceneConfig`, `ProjectSetupBuilder`, `HumanoidMeshBuilder`, `CharacterAndStadiumUpgrader`, `GameTextureGenerator`, `UIStyleGenerator`, `AssetPreviewRenderer`, `GameSystemsTestRunner`, `VisualValidationCapture`, `GameBuildPipeline` | Inicio en MainMenu, arte, rig, batching estático, tests, capturas y build |
