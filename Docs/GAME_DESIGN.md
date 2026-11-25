# Ultimate Penalty Cup 3D — Game Design Reference

## Overview

| Field | Value |
|---|---|
| Genre | Sports Arcade / Casual |
| Perspective | Third-person (behind shooter) |
| Core Loop | Aim → Charge → Shoot → Result → Repeat |
| Session Length | ~5–10 minutes per round |
| Target Platform | Windows PC (future: Android) |

---

## Round Progression

| Round | Name | Required Goals | Reward Coins | Goalkeeper Level |
|---|---|---|---|---|
| 1 | Cantera | 2/5 | 300 | Principiante |
| 2 | Liga Local | 3/5 | 500 | Semiprofesional |
| 3 | Liga Profesional | 3/5 | 800 | Profesional |
| 4 | Semifinal | 4/5 | 1,200 | Avanzado |
| 5 | Gran Final | 4/5 | 2,000 | Campeón |

---

## Scoring System

| Shot Type | Points | Coins |
|---|---|---|
| Normal Goal | 100 | 100 |
| High Power Goal (≥90% charge) | 150 | 125 |
| Corner/Precision Goal | 200 | 150 |
| Miss | 0 | 0 |
| Saved | 0 | 0 |
| Streak Bonus (per extra consecutive goal) | +50 per streak | +25 per streak |

---

## Ball Catalog

| Ball | Price | Description |
|---|---|---|
| Clásico | FREE | Official match ball |
| Azul | 500 | Electric blue power ball |
| Fuego | 1,200 | Flaming trail ball |
| Arcoíris | 2,000 | Rainbow multicolor special |
| Dorado | 3,500 | 24K champion edition |

---

## Stadium Catalog

| Stadium | Price | Atmosphere |
|---|---|---|
| Cantera | FREE | Bright daylight training ground |
| Nocturno | 1,500 | Dramatic 4-tower night match |
| Profesional | 3,000 | Twilight full-capacity stadium |
| Mundial | 6,000 | Championship golden arena with confetti |

---

## Achievement System

| ID | Name | Condition | Reward |
|---|---|---|---|
| FIRST_GOAL | Primer Gol | Score your first goal | 50 coins |
| SNIPER_10 | Francotirador | Score 10 corner/precision goals | 300 coins |
| PERFECT_STREAK | Racha Perfecta | 5 consecutive goals (one round) | 500 coins |
| CHAMPION | Campeón | Win Round 5 (Gran Final) | 1,000 coins |
| MILLIONAIRE | Millonario | Accumulate 10,000 coins | 500 coins |
| COLLECTOR | Coleccionista | Unlock all 5 balls | 400 coins |
| LEGEND | Leyenda | Unlock all 7 achievements | 2,000 coins |

---

## Camera System

- **Idle**: Positioned at `(0, 2.8, -16.8)` looking at the goal
- **Shot tracking**: Follows ball trajectory with subtle FOV zoom during flight
- **Reset**: Smooth lerp back to home position after each attempt
- FOV transitions: 60° (idle) → 72° (ball in flight) → 60° (reset)

---

## Audio Design

| Enum | Type | Description |
|---|---|---|
| M01_Menu | BGM | Main menu ambient sports theme |
| M02_Match | BGM | Tension-building in-match loop |
| M03_Victory | BGM | Upbeat victory fanfare |
| M04_Defeat | BGM | Subdued defeat theme |
| S01_Kick | SFX | Ball kick impact |
| S02_Goal | SFX | Crowd roar goal celebration |
| S03_Save | SFX | Goalkeeper glove save |
| S04_Miss | SFX | Miss / deflection sound |
| S05_Crowd | SFX | Ambient crowd chatter |
| S06_ButtonClick | SFX | UI button press |

All audio falls back to procedural `Math.Sin` synthesis if no AudioClip is assigned.
