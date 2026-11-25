# Ultimate Penalty Cup 3D — Build Instructions

## Prerequisites

- **Unity 6** (6000.6.4f1) installed via Unity Hub
- Unity Hub install path: `D:\Program Files\Unity Hub\6000.6.4f1\Editor\Unity.exe`
- Required packages (pre-installed):
  - Universal Render Pipeline (URP)
  - TextMeshPro
  - Input System
  - 2D Sprite

---

## Build from Unity Editor

1. Open Unity Hub → Open project: `D:\Program Files\UltimatePenaltyCup3D\UltimatePenaltyCup3D`
2. Wait for compilation to finish (bottom status bar)
3. Run the visual upgrade tool:
   - **Menu**: `Ultimate Penalty Cup → Visual Upgrade → 05 - Run ALL Upgrades`
4. Rebuild scenes:
   - **Menu**: `Ultimate Penalty Cup → Execute Complete Project Setup`
5. Build the game:
   - **Menu**: `Ultimate Penalty Cup → Build Windows Standalone (x64)`
6. Output: `Builds/Windows/UltimatePenaltyCup3D.exe`

---

## Automated Batch Build (PowerShell)

Run the included PowerShell script from the project root:

```powershell
cd "D:\Program Files\UltimatePenaltyCup3D\UltimatePenaltyCup3D"
powershell -ExecutionPolicy Bypass -File Build_Windows.ps1
```

This script runs all 4 phases headlessly:
1. Project setup (materials, ScriptableObjects, prefabs)
2. Visual character & stadium upgrades
3. Match scene rebuild
4. Windows x64 standalone build

Logs are saved to `Logs/` for each phase.

---

## Troubleshooting

| Issue | Solution |
|---|---|
| "Scripts have compiler errors" | Open Unity Editor → check Console for errors |
| Missing TextMeshPro assets | Window → TextMeshPro → Import TMP Essential Resources |
| Build fails with license error | Ensure Unity license is activated (requires internet on first run) |
| Black screen on start | Check that `MainMenu.unity` is Scene 0 in Build Settings |

---

## Project Structure

```
Assets/
  Art/
    Logo/          ← Official game logo
    Textures/      ← 16 PNG game textures
    UI/            ← UI assets
  Materials/       ← 18 URP materials
  Prefabs/         ← 6 prefabs (Ball, Goal, Player, Goalkeeper, Stadium, Trophy)
  ScriptableObjects/
    Items/         ← Ball & Stadium shop items + Achievements
    Rounds/        ← 5 round configurations
  Scenes/
    MainMenu.unity ← Scene 0
    MatchScene.unity ← Scene 1
  Scripts/
    Core/          ← GameManager, GameStateManager, Enums
    Player/        ← PlayerController, PlayerShooter
    Goalkeeper/    ← GoalkeeperAI
    Gameplay/      ← BallController, GoalDetector, PenaltyManager, MatchCameraController, StadiumController
    Economy/       ← CurrencyManager, ScoreManager
    SaveSystem/    ← SaveManager, SaveData
    Shop/          ← ShopManager, BallItemSO, StadiumItemSO
    Customization/ ← CustomizationManager
    Achievements/  ← AchievementManager, AchievementSO
    Audio/         ← AudioManager
    Levels/        ← RoundManager, RoundDataSO
    UI/            ← All UI classes
    Editor/        ← ProjectSetupBuilder, CharacterAndStadiumUpgrader, GameBuildPipeline, GameSystemsTestRunner
Builds/
  Windows/         ← Output EXE
Docs/              ← Documentation
Logs/              ← Build & setup logs
```
