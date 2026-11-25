# Ultimate Penalty Cup 3D — Changelog

## [v1.0.0] — 2026-11-25 — Initial Release

### Added
- Complete Unity 6 URP project setup
- 5-round penalty shootout progression system
- Rubén González (#10) humanoid player character
- Malágon AI goalkeeper with 5 difficulty levels
- Procedural humanoid locomotion animations (walk, kick, celebrate, miss)
- Goalkeeper AI: idle shuffle, reaction dive, full-body tilt, save deflection
- Physics-accurate FIFA ball (0.43kg, bounciness 0.65)
- 5 ball skins: Clásico, Azul, Fuego, Arcoíris, Dorado
- 4 stadium environments: Cantera, Nocturno, Profesional, Mundial
- Striped grass turf with regulation pitch lines (penalty box, goal box, arc, spot)
- Multi-tier grandstands with LED perimeter advertising boards
- 4 floodlight tower spotlights
- Match HUD: power bar with gradient, 5 shot indicator circles, streak display
- Main menu with logo, 6 navigation panels (Play, Shop, Customization, Achievements, Settings, Quit)
- Round Select: 5 animated cards with lock/unlock system
- Shop: separate tabs for balls and stadiums, buy/equip/owned states
- Customization panel: equip unlocked balls and stadiums
- Achievements panel: 7 achievements with unlock badges
- Settings: music and SFX sliders
- Save system: JSON via PlayerPrefs, persists coins, points, highscore, inventory, audio settings
- CurrencyManager: coins & points, non-negative guard, highscore via match end only
- ScoreManager: tiered scoring (Corner 200pts/150c, HighPower 150pts/125c, Normal 100pts/100c) + streak bonuses
- AchievementManager: 7 achievements (FirstGoal, Sniper10, PerfectStreak, Champion, Millionaire, Collector, Legend)
- AudioManager: procedural synthesized fallback audio (BGMs + SFX)
- Confetti particle system for Round 5 champion celebration
- BallController: VFX particle system per ball type, dynamic trail colours
- StadiumController: dynamic lighting per venue (daylight/night/twilight/championship)
- Secret dev code PENALTY2026 (+5,000 coins in debug builds)
- Editor tools: ProjectSetupBuilder, CharacterAndStadiumUpgrader, GameBuildPipeline, GameSystemsTestRunner
- Windows Standalone x64 build pipeline
- Complete documentation suite (README, BUILD, CONTROLS, ARCHITECTURE, CHANGELOG)

### Fixed
- BallController.ResetBall: zeroes velocity before isKinematic = true (eliminates Unity 6 kinematic warning)
- CurrencyManager.AddPoints: highScore now only updated at round end via SaveManager.UpdateHighScore()
- RoundEndUI: missing `using UltimatePenaltyCup.SaveSystem` namespace directive
- StadiumController: StadiumType enum values corrected (Night/Professional/WorldCup)
- MatchHUD: txtStreak correctly hidden on Start, only shown for streak >= 2

### Technical Notes
- Unity 6 (6000.6.4f1) + Universal Render Pipeline
- InputSystem `activeInputHandler` = Both (legacy + new)
- All scripts use `#pragma warning disable 0649` for clean 0-warning builds
- TextMeshPro uses `textWrappingMode = TextWrappingModes.Normal` (Unity 6 API)
