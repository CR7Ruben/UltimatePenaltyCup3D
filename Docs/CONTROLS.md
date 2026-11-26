# UltimatePenaltyCup3D — Controles y Mecánicas

## Controles en partido (`MatchScene`)

| Acción | Entrada |
|---|---|
| Mover al futbolista en la zona de carrera | `W` `A` `S` `D` o Flechas de dirección |
| Apuntar hacia la portería | Movimiento del Mouse |
| Cargar potencia de disparo | Mantener clic izquierdo del Mouse |
| Ejecutar disparo | Soltar clic izquierdo del Mouse |
| Pausar / Reanudar partida | Tecla `ESC` o botón de pausa (`II`) en esquina superior derecha |

---

## Sistema de apuntado, potencia y animación

- **Flecha 3D y retícula circular:** Indican en tiempo real la dirección inicial y el punto exacto de llegada del balón al plano de la portería (`z = 0`).
- **Trayectoria balística unificada:** Muestreada desde las mismas constantes físicas (`LinearDampingValue = 0.12f`, `MagnusCoefficient = 0.0020f`) que ejecuta `BallController`, sin desviación lateral artificial al aumentar potencia.
- **Estabilización al cargar (`36 px`):** Al presionar clic izquierdo para cargar potencia, pequeños movimientos involuntarios del cursor dentro de `36 px` no desvían la puntería fijada.
- **Zona Perfecta (`68%–88%`):** Se ilumina en dorado (**¡ZONA PERFECTA!**); por encima de `92%` advierte exceso de potencia en rojo.
- **Sincronización carrera-golpeo:** El balón parte en el instante exacto de contacto del botín tras la carrera de aproximación y el impulso de golpeo de Rubén.

---

## Navegación en menús

- **JUGAR:** Selección de las 5 rondas con ficha del estadio correspondiente y nivel de **Malágon**.
- **TIENDA:** Pestañas separadas para **Balones** y **Estadios** con previsualizaciones 3D transparentes.
- **PERSONALIZACIÓN:** Equipamiento de balones y estadios (estadio fijo en las 5 rondas o modo **Automático por Ronda**).
- **LOGROS:** Medallas y barras de progreso en vivo de los 7 objetivos.
- **OPCIONES:** Volumen General, Música, Efectos, Sensibilidad de apuntado, Resolución, Pantalla completa/ventana, Calidad gráfica (**Baja** / **Media** / **Alta**), Restaurar predeterminados y canje de códigos promocionales.
