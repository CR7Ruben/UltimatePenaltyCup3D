# Ultimate Penalty Cup 3D — Architecture

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        GAME MANAGER                          │
│        (DontDestroyOnLoad singleton coordinator)             │
│  GameManager · SaveManager · CurrencyManager · ScoreManager  │
│  AudioManager · ShopManager · CustomizationManager          │
│  AchievementManager · RoundManager                           │
└────────────────────────┬────────────────────────────────────┘
                         │ Events / Singletons
         ┌───────────────┼──────────────────┐
         ▼               ▼                  ▼
  ┌────────────┐  ┌──────────────┐  ┌──────────────┐
  │  MainMenu  │  │  MatchScene  │  │   SaveSystem  │
  │  (Scene 0) │  │  (Scene 1)   │  │  (JSON/Prefs) │
  └────────────┘  └──────┬───────┘  └──────────────┘
                         │
        ┌────────────────┼──────────────────────┐
        ▼                ▼                       ▼
 ┌────────────┐  ┌───────────────┐   ┌───────────────────┐
 │  Player    │  │  Goalkeeper   │   │   PenaltyManager  │
 │  Ruben #10 │  │  Malagon AI   │   │  (Match State     │
 │            │  │  (5 diff lvl) │   │   Machine)        │
 └─────┬──────┘  └───────┬───────┘   └────────┬──────────┘
       │                 │                     │
       ▼                 ▼                     ▼
 PlayerController   GoalkeeperAI        BallController
 PlayerShooter      (reaction delay,    GoalDetector
 (Aim, charge,       dive, shuffle)     (corner/power
  launch physics)                        detection)
```

## Namespace Map

| Namespace | Location | Responsibility |
|---|---|---|
| `UltimatePenaltyCup.Core` | Scripts/Core/ | Enums, GameManager, GameStateManager |
| `UltimatePenaltyCup.Player` | Scripts/Player/ | PlayerController, PlayerShooter |
| `UltimatePenaltyCup.Goalkeeper` | Scripts/Goalkeeper/ | GoalkeeperAI |
| `UltimatePenaltyCup.Gameplay` | Scripts/Gameplay/ | Ball, Goal, Penalty, Camera, Stadium |
| `UltimatePenaltyCup.Economy` | Scripts/Economy/ | CurrencyManager, ScoreManager |
| `UltimatePenaltyCup.SaveSystem` | Scripts/SaveSystem/ | SaveManager, SaveData |
| `UltimatePenaltyCup.Shop` | Scripts/Shop/ | ShopManager, BallItemSO, StadiumItemSO |
| `UltimatePenaltyCup.Customization` | Scripts/Customization/ | CustomizationManager |
| `UltimatePenaltyCup.Achievements` | Scripts/Achievements/ | AchievementManager, AchievementSO |
| `UltimatePenaltyCup.Audio` | Scripts/Audio/ | AudioManager (procedural fallback) |
| `UltimatePenaltyCup.Levels` | Scripts/Levels/ | RoundManager, RoundDataSO |
| `UltimatePenaltyCup.UI` | Scripts/UI/ | All UI MonoBehaviours |
| `UltimatePenaltyCup.Editor` | Scripts/Editor/ | Editor-only tools |

## Key Design Patterns

- **Singleton MonoBehaviour** — All managers use `Instance` with `DontDestroyOnLoad`
- **ScriptableObject Data** — `RoundDataSO`, `BallItemSO`, `StadiumItemSO`, `AchievementSO`
- **Event-Driven UI** — Managers expose `C# events`; UI subscribes in `Start()`, unsubscribes in `OnDestroy()`
- **State Machine** — `PenaltyManager` controls the shot lifecycle: Aiming → Charging → Kicking → InFlight → Evaluated → Resetting
- **Procedural Audio** — `AudioManager` synthesises fallback audio via `Math.Sin` waveforms if no clips are assigned

## Save System

- **Serialization**: `JsonUtility.ToJson(SaveData)` → `PlayerPrefs`
- **Key**: `UPC3D_SaveData_v1`
- **Data**: Coins, totalPoints, highScore, unlockedRounds/Balls/Stadiums, equippedBall, selectedStadium, achievements, statistics, audio volumes

## Goalkeeper AI — Difficulty Levels

| Round | Reaction Delay | Move Speed | Dive Speed | Error Prob |
|---|---|---|---|---|
| 1 — Cantera | 0.55s | 3.5 | 4.0 | 50% |
| 2 — Liga Local | 0.45s | 4.0 | 5.0 | 38% |
| 3 — Liga Pro | 0.35s | 5.0 | 6.5 | 25% |
| 4 — Semifinal | 0.22s | 6.5 | 8.5 | 14% |
| 5 — Gran Final | 0.12s | 8.5 | 11.0 | 5% |
