# UltimatePenaltyCup3D — Historial de Cambios (CHANGELOG)

## [1.0.0] — 2026-11-25 — Corrección Integral, Rediseño Visual y Finalización

### Corregido (Funcionalidad y Jugabilidad)
- **Inicio y progresión:** Eliminada la carga errónea o configuración faltante de rondas; `GameManager` recibe `roundConfigs` serializados directamente en `MainMenu.unity` y se respalda con `RoundManager`. `SampleScene.unity` excluida de compilación.
- **Portero Malágon:** Agregados `Rigidbody` cinemático + `CapsuleCollider` corporal + `BoxCollider` en ambos guantes etiquetados `"Goalkeeper"`, orientación corregida mirando hacia el tirador (`180°` en Y en `z = -0.45f`), máquina de 8 estados (`Idle`, `Ready`, `Reacting`, `DivingLeft`, `DivingRight`, `Saving`, `Recovering`, `Resetting`) y detección continua en `FixedUpdate` para impedir que disparos veloces atraviesen al portero.
- **Finalización anticipada de rondas:** `PenaltyManager` concluye la ronda inmediatamente al alcanzar `requiredGoals` (ej. `3/3` en Ronda 1 o `4/4` en Ronda 3) o cuando matemáticamente ya no es posible cumplir la meta (`GoalsScored + remainingAttempts < requiredGoals`), bloqueando controles de entrada y evitando duplicación de recompensas.
- **Pantalla de fin de ronda:** `RoundEndUI` movido al objeto siempre activo `RoundEndManager` para garantizar su suscripción a `OnRoundEndedDetailed` al iniciar `MatchScene`. Se muestran correctamente las pantallas de **Victoria**, **Derrota** y **Gran Final** (Ronda 5).
- **Logro *Racha Perfecta*:** Adaptado para desbloquearse con 4 goles consecutivos o al completar una ronda sin fallar ningún disparo (compatible con victoria anticipada), preservando logros previos.

### Mejorado (3D, Física, Cámara y Escenarios)
- **Personajes 3D unificados (`HumanoidMeshBuilder`):** Sustitución de primitivas desconectadas por mallas continuas `SkinnedMeshRenderer` de 17 huesos (`Mesh_RubenGonzalez` 1.80 m y `Mesh_GoalkeeperMalagon` 1.90 m), con separación estricta de materiales (`Mat_Skin_Body` en extremidades y `Mat_Face_*` únicamente en el rostro frontal). Corregido reflejo horizontal del dorsal `#10` en espalda.
- **Proporciones FIFA y física:** Balón de `0.22 m` en `(0, 0.11, -11)`, portería de `7.32 × 2.44 × 2.0 m` con `GoalNetRipple` (deformación de malla de red al anotar), punto penal `0.18 m` y cámara deportiva `MatchCameraController` con seguimiento suave.
- **4 Estadios diferenciados:** `Venue_Cantera` (diurno con gradas, vallas y árboles), `Venue_Night` (4 torres de reflectores y gradas nocturnas), `Venue_Professional` (gradas techadas, túnel, bancas y marcador) y `Venue_WorldCup` (macroestadio con pantallas gigantes, arco dorado, confeti y trofeo).
- **Césped natural:** Texturas orgánicas con mapa normal `Tex_Grass_Normal` y franjas de corte reales (eliminado el patrón de ajedrez).

### Rediseñado (UI/UX y Previsualizaciones 3D)
- **Previsualizaciones 3D reales (`AssetPreviewRenderer`):** Renderizado en `RenderTexture` de los 5 balones y 4 estadios para Tienda, Personalización y Selección de Rondas (sin cuadrados planos de color).
- **Rediseño integral de pantallas:** Menú principal sobre estadio en vivo, Selección de rondas en `HorizontalLayoutGroup`, Tienda y Personalización con pestañas independientes y vista previa lateral, Logros con barras de progreso reales, Opciones con controles reales de Audio/Sensibilidad/Resolución/Pantalla completa/Calidad sin superposición, Pausa con submenú de opciones y HUD deportivo con 5 indicadores circulares de penal.
