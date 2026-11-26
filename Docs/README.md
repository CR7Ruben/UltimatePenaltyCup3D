# UltimatePenaltyCup3D

**Estudio:** A&R Game Labs  
**Desarrolladores:** Rubén Alberto González Camargo & Xitzia Aimara Fierro Rivera  
**Motor:** Unity 6 (`6000.6.4f1`) · URP · C#  
**Plataforma:** Windows x64

---

## Descripción

**UltimatePenaltyCup3D** es un videojuego deportivo arcade 3D en tercera persona donde encarnas a **Rubén González (#10)** en una serie de cinco rondas de tiros penales contra el portero controlado por inteligencia artificial **Malágon**.

Cada ronda aumenta la dificultad de la IA, exige una meta de goles en un máximo de cinco intentos y traslada la acción por cuatro estadios diferenciados (**Cantera**, **Nocturno**, **Profesional** y **Mundial**).

---

## Características principales

- **Inicio limpio:** El juego inicia siempre en `MainMenu` (índice `0` en Build Settings) con el estadio en 3D en vivo y cámara orbital.
- **5 rondas progresivas:**
  - Ronda 1 (*Cantera*): 3 goles en máximo 5 intentos.
  - Ronda 2 (*Liga Local*): 3 goles en máximo 5 intentos.
  - Ronda 3 (*Liga Profesional*): 4 goles en máximo 5 intentos.
  - Ronda 4 (*Semifinal*): 4 goles en máximo 5 intentos.
  - Ronda 5 (*Gran Final*): 4 goles en máximo 5 intentos + celebración de campeonato.
- **Finalización anticipada de ronda:** Al alcanzar la meta de goles o cuando es matemáticamente imposible lograrla, la ronda concluye automáticamente mostrando la pantalla de Victoria, Derrota o Gran Final.
- **Portero Malágon con IA y física real:** Máquina de 8 estados (`Idle`, `Ready`, `Reacting`, `DivingLeft`, `DivingRight`, `Saving`, `Recovering`, `Resetting`), detección predictiva de trayectoria, `Rigidbody` cinemático, colliders en cuerpo y guantes, verificación continua en `FixedUpdate` y desvíos hacia adelante.
- **Personajes 3D unificados con rigging:** Mallas anatómicas de 17 huesos con `SkinnedMeshRenderer`, pesos suavizados en articulaciones y materiales separados para piel corporal, rostro frontal, cabello, camiseta, short, medias, botines y guantes.
- **Tienda y Personalización con vistas previas reales:** 5 balones y 4 estadios renderizados en 3D con cámaras de estudio, sin cuadrados planos de color.
- **Audio, logros y persistencia:** 7 logros con barra de progreso, audio procedimental de respaldo (silbato, golpeo, poste, atajada, gol, música) y guardado automático JSON en `PlayerPrefs`.

---

## Ejecución y compilación

- Ejecutable Windows x64: `Builds/Windows/UltimatePenaltyCup3D.exe`
- Guía completa de compilación: ver [`BUILD_INSTRUCTIONS.md`](BUILD_INSTRUCTIONS.md)
- Controles detallados: ver [`CONTROLS.md`](CONTROLS.md)

---

*© 2026 A&R Game Labs. Todos los derechos reservados.*
