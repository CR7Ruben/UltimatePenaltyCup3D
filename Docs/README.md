# UltimatePenaltyCup3D

**Estudio:** A&R Game Labs  
**Desarrolladores:** Rubén Alberto González Camargo & Xitzia Aimara Fierro Rivera  
**Motor:** Unity 6 (`6000.6.4f1`) · URP (`17.6.0`) · C#  
**Plataforma:** Windows x64

---

## Descripción

**UltimatePenaltyCup3D** es un videojuego deportivo arcade 3D en tercera persona donde encarnas a **Rubén González (#7)** en una serie de cinco rondas de tiros penales contra el portero controlado por inteligencia artificial **Malágon (#1)**.

Cada ronda aumenta la dificultad natural de la IA, exige una meta de goles en un máximo de cinco intentos y traslada la acción por cuatro estadios diferenciados (**Cantera**, **Nocturno**, **Profesional** y **Mundial**) con público animado GPU-instanced y audio ambiental 3D espacializado.

---

## Características principales

- **Menú Principal en vivo:** Inicia siempre en `MainMenu` (índice `0`) con logotipo limpio transparente, cámara orbital y exhibición 3D de **Rubén González (#7)** con respiración y parpadeo natural.
- **5 rondas progresivas calibradas:**
  - Ronda 1 (*Cantera*): 3 goles en máximo 5 intentos.
  - Ronda 2 (*Liga Local*): 3 goles en máximo 5 intentos.
  - Ronda 3 (*Liga Profesional*): 4 goles en máximo 5 intentos.
  - Ronda 4 (*Semifinal*): 4 goles en máximo 5 intentos.
  - Ronda 5 (*Gran Final*): 4 goles en máximo 5 intentos + celebración de campeonato.
- **Personajes humanizados (22 huesos + 5 BlendShapes faciales):** Mallas anatómicas esculpidas (manos de 5 dedos, guantes acolchados en portero, rostros en alta definición con mapas PBR de tela/piel y expresiones `Blink`, `Focus`, `Effort`, `Happy`, `Frustrated`), carrera de aproximación curva y estiradas realistas con aterrizaje amortiguado.
- **Público animado GPU-instanced y audio 3D:** Espectadores dinámicos con shader `CrowdInstanced.shader` que reaccionan a cada jugada (`Tension`, `GoalCelebrate`, `SaveGasp`, `MissGroan`), banderines de esquina animados y audio espacializado en 4 sectores más control de 5 canales (`General`, `Música`, `Efectos`, `Público`, `Interfaz`).
- **Sistema unificado de alertas y modales:** `NotificationManager` con cola de toasts sin solapamiento, tarjetas de logros desbloqueados, modales de confirmación, transiciones suaves `UIPanelTransition` y banners de transmisión en vivo.
- **Disparo balístico fiel y cámara completa:** Solucionador unificado entre la flecha 3D, la trayectoria curva y la física real PhysX/Magnus del balón, con encuadre completo hasta los botines e intro cinematográfica opcional.
- **Personalización de Estadios y Calidad:** Soporte verificado para alternar entre **Modo Automático por Ronda** y **Estadio Fijo Personalizado** (`4 × 5 = 20` combinaciones), además de presets gráficos reales **Baja**, **Media** y **Alta**.
- **Tienda, Logros, Audio y Persistencia:** 5 balones y 4 estadios con previsualización 3D real, 7 logros con barras de progreso y guardado JSON retrocompatible (`v2` con `crowdVolume` y `uiVolume`).
- **Validado con pruebas, capturas, GIFs y compilación Windows:** **`24/24`** pruebas Edit Mode, **`10/10`** pruebas Play Mode (incluidos 100 disparos con carrera completa), **38 capturas `1920×1080`**, **4 GIFs animados** (`Docs/Videos/`) y benchmark real a `1920×1080` sobre `Builds/Windows/UltimatePenaltyCup3D.exe`.

---

## Ejecución y compilación

- Ejecutable Windows x64: `Builds/Windows/UltimatePenaltyCup3D.exe` (`116.91 MB`)
- Guía completa de compilación: ver [`BUILD_INSTRUCTIONS.md`](BUILD_INSTRUCTIONS.md)
- Controles detallados: ver [`CONTROLS.md`](CONTROLS.md)
- Guía de personajes y animaciones: ver [`CHARACTER_ANIMATION_GUIDE.md`](CHARACTER_ANIMATION_GUIDE.md)
- Sistema de público y audio 3D: ver [`CROWD_SYSTEM.md`](CROWD_SYSTEM.md)
- Sistema de notificaciones y UI: ver [`UI_NOTIFICATION_SYSTEM.md`](UI_NOTIFICATION_SYSTEM.md)
- Informe de pruebas y benchmark 1080p: ver [`TEST_REPORT.md`](TEST_REPORT.md)

---

*© 2026 A&R Game Labs. Todos los derechos reservados.*
