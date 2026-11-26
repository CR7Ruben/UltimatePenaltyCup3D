# UltimatePenaltyCup3D

**Estudio:** A&R Game Labs  
**Desarrolladores:** Rubén Alberto González Camargo & Xitzia Aimara Fierro Rivera  
**Motor:** Unity 6 (`6000.6.4f1`) · URP (`17.6.0`) · C#  
**Plataforma:** Windows x64

---

## Descripción

**UltimatePenaltyCup3D** es un videojuego deportivo arcade 3D en tercera persona donde encarnas a **Rubén González (#10)** en una serie de cinco rondas de tiros penales contra el portero controlado por inteligencia artificial **Malágon**.

Cada ronda aumenta la dificultad natural de la IA, exige una meta de goles en un máximo de cinco intentos y traslada la acción por cuatro estadios diferenciados (**Cantera**, **Nocturno**, **Profesional** y **Mundial**).

---

## Características principales

- **Menú Principal en vivo:** Inicia siempre en `MainMenu` (índice `0`) con logotipo limpio transparente, cámara orbital y exhibición 3D de **Rubén González**.
- **5 rondas progresivas calibradas:**
  - Ronda 1 (*Cantera*): 3 goles en máximo 5 intentos.
  - Ronda 2 (*Liga Local*): 3 goles en máximo 5 intentos.
  - Ronda 3 (*Liga Profesional*): 4 goles en máximo 5 intentos.
  - Ronda 4 (*Semifinal*): 4 goles en máximo 5 intentos.
  - Ronda 5 (*Gran Final*): 4 goles en máximo 5 intentos + celebración de campeonato.
- **Disparo balístico fiel y cámara completa:** Solucionador unificado entre la flecha 3D, la trayectoria curva y la física real PhysX/Magnus del balón, con encuadre completo hasta los botines.
- **Animaciones de cuerpo completo (17 huesos):** Carrera curva, apoyo, golpeo sincronizado con el balón y seguimiento en el tirador; postura atlética, split-step de anticipación, 9 estilos de atajada y recuperación progresiva desde el césped en **Malágon**.
- **Personalización de Estadios y Calidad:** Soporte verificado para alternar entre **Modo Automático por Ronda** y **Estadio Fijo Personalizado** (`4 × 5 = 20` combinaciones), además de presets gráficos reales **Baja**, **Media** y **Alta**.
- **Tienda, Logros, Audio y Persistencia:** 5 balones y 4 estadios con previsualización 3D real, 7 logros con barras de progreso y guardado JSON retrocompatible.
- **Validado con pruebas y compilación Windows:** `19/19` pruebas Edit Mode, `7/7` pruebas Play Mode (incluidos 100 disparos con carrera completa) y benchmark real a `1920×1080` sobre `Builds/Windows/UltimatePenaltyCup3D.exe`.

---

## Ejecución y compilación

- Ejecutable Windows x64: `Builds/Windows/UltimatePenaltyCup3D.exe` (`116.12 MB`)
- Guía completa de compilación: ver [`BUILD_INSTRUCTIONS.md`](BUILD_INSTRUCTIONS.md)
- Controles detallados: ver [`CONTROLS.md`](CONTROLS.md)
- Informe de pruebas y benchmark 1080p: ver [`TEST_REPORT.md`](TEST_REPORT.md)

---

*© 2026 A&R Game Labs. Todos los derechos reservados.*
