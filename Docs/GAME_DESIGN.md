# UltimatePenaltyCup3D — Documento de Diseño (GDD)

## Identidad del proyecto

- **Nombre definitivo:** `UltimatePenaltyCup3D`
- **Estudio:** A&R Game Labs
- **Género:** Deportivo / Arcade / Casual
- **Perspectiva:** Tercera persona detrás del lanzador (vista lateral deportiva)

---

## Progresión de rondas

| Ronda | Nombre | Meta / Intentos | Estadio temático | Nivel de Malágon | Delay | DiveSpeed | Error | Bono |
|---|---|---|---|---|---|---|---|---|
| 1 | Cantera | 3 / 5 | Cantera (Día) | Principiante | 0.24 s | 5.2 | 42 % | +500 pts / +300 monedas |
| 2 | Liga Local | 3 / 5 | Nocturno | Intermedio | 0.19 s | 6.8 | 30 % | +500 pts / +450 monedas |
| 3 | Liga Profesional | 4 / 5 | Profesional | Profesional | 0.15 s | 8.4 | 20 % | +650 pts / +650 monedas |
| 4 | Semifinal | 4 / 5 | Nocturno | Avanzado | 0.11 s | 10.0 | 12 % | +800 pts / +900 monedas |
| 5 | Gran Final | 4 / 5 | Mundial | Élite | 0.08 s | 11.6 | 6 % | +1,200 pts / +1,500 monedas |

---

## Puntuación y economía

| Resultado | Puntos base | Monedas base |
|---|---|---|
| Gol normal | 100 | 100 |
| Gol de alta potencia (`>= 82%`) | 150 | 125 |
| Gol al ángulo / precisión | 200 | 150 |
| Atajada / Al poste / Fuera | 0 | 0 |
| Bono por racha (`streak >= 2`) | +50 × `(streak - 1)` | +25 × `(streak - 1)` |

---

## Catálogo de Tienda y Personalización

- **Balones (5):** Clásico (Gratis), Azul (500), Fuego (1,000), Arcoíris (2,000), Dorado (5,000).
- **Estadios (4):** Cantera (Gratis), Nocturno (2,000), Profesional (4,000), Mundial (7,500). Incluye modo **Automático por Ronda** o **Estadio Fijo** personalizado.
- **Logros (7):** Primer Gol (`100`), Francotirador (`300`), Racha Perfecta (`500`), Campeón (`1,500`), Millonario (`2,000`), Coleccionista (`2,500`), Leyenda (`3,000`).
