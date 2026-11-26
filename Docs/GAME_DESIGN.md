# UltimatePenaltyCup3D — Documento de Diseño (GDD)

## Identidad del proyecto

- **Nombre definitivo:** `UltimatePenaltyCup3D`
- **Estudio:** A&R Game Labs
- **Género:** Deportivo / Arcade / Casual
- **Perspectiva:** Tercera persona detrás del lanzador (vista lateral deportiva)

---

## Progresión de rondas

| Ronda | Nombre | Meta / Intentos | Estadio temático | Nivel de Malágon | Delay | MoveSpeed | DiveSpeed | Error | Bono |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cantera | 3 / 5 | Cantera (Día) | Principiante | 0.25 s | 4.0 | 5.0 | 44 % | +500 pts / +300 monedas |
| 2 | Liga Local | 3 / 5 | Nocturno | Intermedio | 0.20 s | 5.2 | 6.5 | 32 % | +500 pts / +450 monedas |
| 3 | Liga Profesional | 4 / 5 | Profesional | Profesional | 0.16 s | 6.4 | 8.0 | 22 % | +650 pts / +650 monedas |
| 4 | Semifinal | 4 / 5 | Nocturno | Avanzado | 0.12 s | 7.6 | 9.5 | 14 % | +800 pts / +900 monedas |
| 5 | Gran Final | 4 / 5 | Mundial | Élite | 0.09 s | 8.8 | 11.0 | 8 % | +1,200 pts / +1,500 monedas |

*(Balance validado mediante simulación real de 100 disparos en Play Mode: tasa de atajada al arco de 17.6% en Ronda 1 hasta 55.6% en Ronda 5).*

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
