# UltimatePenaltyCup3D — Sistema de Notificaciones, Modales y Transiciones UI

## 1. Arquitectura de `NotificationManager.cs`

`NotificationManager` (persistente con `GameManager`) centraliza las alertas de la interfaz sobre un `NotificationOverlayCanvas` (`ScreenSpaceOverlay`, `sortingOrder = 500`) con referencias de pantalla `1920×1080`:

- **Cero costo en reposo:** `SyncOverlayCanvasActive()` desactiva `overlayCanvas.enabled` cuando no hay toasts, logros ni modales activos.
- **Cola FIFO de toasts (`ShowToast`):** Cinco estilos (`Info`, `Success`, `Warning`, `Error`, `Reward`) con entrada deslizable cúbica desde la derecha, temporización con `Time.unscaledDeltaTime` (funciona incluso en pausa) y acento cromático coherente con la paleta marina/dorada de **UltimatePenaltyCup3D**.
- **Tarjetas de logro (`ShowAchievementCard`):** Banner superior dorado con medalla `#1` (sin glifos faltantes), título, descripción y desglose de recompensa en monedas y puntos.
- **Modales de confirmación (`ShowConfirmModal`):** Usados en acciones destructivas o confirmaciones (salir del juego en `MainMenuUI`, reiniciar/salir al menú en `PauseUI`, restaurar valores en `SettingsUI`), con cierre directo por tecla `ESC`.

---

## 2. Transiciones y banners de partido

- **`UIPanelTransition.cs`:** Animaciones de entrada y salida de paneles principales con `CanvasGroup` y `Time.unscaledDeltaTime`.
- **`MatchHUD.cs`:** Banners animados (`¡GOOOOL!`, `¡GOLAZO AL ÁNGULO!`, `¡ATAJADA!`, `¡AL POSTE!`, `¡FUERA!`, aviso de **`¡ÚLTIMO PENAL!`**) utilizando comprobación segura `== null` sobre `CanvasGroup` para garantizar compatibilidad en runtime.
- **`RoundEndUI.cs`:** Contadores animados progresivos para goles, puntuación y monedas obtenidas al concluir la ronda.
- **`MatchCameraController.cs`:** Barrido cinematográfico inicial de entrada al estadio (con bandera `SkipIntroSweepForTests` para pruebas deterministas).
