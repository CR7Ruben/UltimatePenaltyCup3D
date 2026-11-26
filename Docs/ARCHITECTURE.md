# UltimatePenaltyCup3D — Arquitectura del Software

## Visión general

**UltimatePenaltyCup3D** sigue una arquitectura modular orientada a objetos y dirigida por eventos en C# sobre Unity 6 (URP).

```
MainMenu (Índice 0, Persistent Managers via DontDestroyOnLoad)
 ├── GameManager            (Coordinador de escenas, RoundConfigs, estado global)
 ├── GameStateManager       (Máquina de estados UI/Partida)
 ├── RoundManager           (Catálogo de 5 RoundDataSO + sincronización)
 ├── SaveManager            (Persistencia JSON en PlayerPrefs)
 ├── CurrencyManager        (Monedas, puntos y código PENALTY2026 en Dev/Editor)
 ├── ScoreManager           (Puntuación escalonada y rachas)
 ├── ShopManager            (Compras de BallItemSO / StadiumItemSO)
 ├── CustomizationManager   (Equipar balón, estadio fijo o automático por ronda)
 ├── AchievementManager     (7 logros, progreso en vivo, sin duplicación)
 └── AudioManager           (Música, SFX y síntesis procedimental de respaldo)

MatchScene (Índice 1)
 ├── PenaltyManager         (Máquina de estados del penal y fin anticipado)
 ├── GoalkeeperAI           (IA de 8 estados, predicción balística y atajadas)
 ├── PlayerController       (Locomoción procedimental sobre esqueleto de 17 huesos)
 ├── PlayerShooter          (Apuntado en plano de gol, curva, sweet spot y potencia)
 ├── BallController         (Física FIFA 0.43kg, 0.22m, estelas y partículas VFX)
 ├── GoalDetector           (Cruce real de línea de gol + detección de ángulo)
 ├── GoalNetRipple          (Deformación dinámica de mallas subdivididas de la red)
 ├── MatchCameraController  (Cámara de penal deportiva con seguimiento suavizado)
 ├── StadiumController      (4 estadios diferenciados + iluminación + confeti)
 └── Canvas_MatchHUD        (MatchHUD, PauseManager/PauseUI, RoundEndManager/RoundEndUI)
```

---

## Inventario de namespaces (`Assets/Scripts/`)

| Namespace | Scripts | Responsabilidad |
|---|---|---|
| `UltimatePenaltyCup.Core` | `GameEnums`, `GameManager`, `GameStateManager` | Estados globales, enums y transición de escenas |
| `UltimatePenaltyCup.Levels` | `RoundDataSO`, `RoundManager` | Datos de rondas 1–5 y progresión |
| `UltimatePenaltyCup.Gameplay` | `PenaltyManager`, `BallController`, `GoalDetector`, `GoalNetRipple`, `MatchCameraController`, `StadiumController`, `MenuCameraOrbit` | Ciclo completo del penal, física, cámara y estadios |
| `UltimatePenaltyCup.Player` | `PlayerController`, `PlayerShooter` | Animación esquelética y mecánicas de disparo |
| `UltimatePenaltyCup.Goalkeeper` | `GoalkeeperAI` | IA de 8 estados con atajada física y verificación continua |
| `UltimatePenaltyCup.Economy` | `CurrencyManager`, `ScoreManager` | Economía, balance no negativo y bonos |
| `UltimatePenaltyCup.SaveSystem` | `SaveData`, `SaveManager` | Serialización JSON retrocompatible |
| `UltimatePenaltyCup.Shop` | `ShopManager`, `BallItemSO`, `StadiumItemSO` | Catálogo y validación de compras |
| `UltimatePenaltyCup.Customization` | `CustomizationManager` | Equipamiento de balones y estadios |
| `UltimatePenaltyCup.Achievements` | `AchievementManager`, `AchievementSO` | Evaluación y progreso de logros |
| `UltimatePenaltyCup.Audio` | `AudioManager` | BGM/SFX con síntesis matemática procedimental |
| `UltimatePenaltyCup.UI` | `MainMenuUI`, `RoundSelectUI`, `ShopUI`, `ShopItemCardUI`, `CustomizationUI`, `AchievementsUI`, `SettingsUI`, `MatchHUD`, `PauseUI`, `RoundEndUI`, `UIButtonAnimator` | Interfaz deportiva responsiva 1920×1080 |
| `UltimatePenaltyCup.Editor` | `ProjectSetupBuilder`, `HumanoidMeshBuilder`, `CharacterAndStadiumUpgrader`, `GameTextureGenerator`, `UIStyleGenerator`, `AssetPreviewRenderer`, `GameSystemsTestRunner`, `VisualValidationCapture`, `GameBuildPipeline` | Generación integral de arte, rig, UI, tests, capturas y build |
