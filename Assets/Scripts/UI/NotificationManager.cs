using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Achievements;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public enum NotificationKind
    {
        Info,
        Success,
        Warning,
        Error,
        Reward
    }

    /// <summary>
    /// Centralized broadcast-styled notification manager for UltimatePenaltyCup3D.
    /// Provides sporty toast queues, trophy achievement/reward cards, and custom confirmation modals.
    /// Runs entirely on Time.unscaledDeltaTime so alerts work seamlessly even during Pause.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        public bool IsToastVisible { get; private set; } = false;
        public bool IsAchievementVisible { get; private set; } = false;
        public bool IsModalOpen { get; private set; } = false;
        public int QueuedToastCount => toastQueue.Count;
        public string LastToastTitle { get; private set; } = string.Empty;
        public string LastToastMessage { get; private set; } = string.Empty;
        public Canvas OverlayCanvas => overlayCanvas;

        private struct ToastRequest
        {
            public string title;
            public string message;
            public NotificationKind kind;
            public float duration;
        }

        private struct AchievementCardRequest
        {
            public string title;
            public string description;
            public int coins;
            public int points;
        }

        private readonly Queue<ToastRequest> toastQueue = new Queue<ToastRequest>();
        private readonly Queue<AchievementCardRequest> achievementQueue = new Queue<AchievementCardRequest>();

        private Canvas overlayCanvas;
        private Sprite roundedCardSprite;
        private Sprite circleSprite;

        // Toast UI refs
        private RectTransform toastRect;
        private CanvasGroup toastGroup;
        private Image toastBg;
        private Image toastAccentBar;
        private Image toastIconBg;
        private TMP_Text txtToastSymbol;
        private TMP_Text txtToastTitle;
        private TMP_Text txtToastMsg;

        // Achievement Card refs
        private RectTransform achRect;
        private CanvasGroup achGroup;
        private TMP_Text txtAchTitle;
        private TMP_Text txtAchDesc;
        private TMP_Text txtAchRewards;

        // Modal refs
        private GameObject modalOverlay;
        private CanvasGroup modalGroup;
        private RectTransform modalCardRect;
        private Image modalAccentStripe;
        private Image modalIconCircle;
        private TMP_Text txtModalIcon;
        private TMP_Text txtModalTitle;
        private TMP_Text txtModalMsg;
        private Button btnModalConfirm;
        private Button btnModalCancel;
        private Image imgModalConfirmBg;
        private TMP_Text txtModalConfirm;
        private TMP_Text txtModalCancel;

        private Action pendingConfirmCallback;
        private Action pendingCancelCallback;
        private Coroutine toastCoroutine;
        private Coroutine achCoroutine;
        private Coroutine modalAnimCoroutine;

        // Brand palette (matches MainMenuUI)
        private static readonly Color ColNavyCard   = new Color(0.055f, 0.115f, 0.235f, 0.96f);
        private static readonly Color ColNavyDeep   = new Color(0.028f, 0.062f, 0.135f, 0.96f);
        private static readonly Color ColElectric   = new Color(0.08f, 0.56f, 1.00f, 1.0f);
        private static readonly Color ColGreen      = new Color(0.14f, 0.80f, 0.45f, 1.0f);
        private static readonly Color ColGold       = new Color(1.00f, 0.78f, 0.22f, 1.0f);
        private static readonly Color ColAmber      = new Color(0.96f, 0.55f, 0.12f, 1.0f);
        private static readonly Color ColRed        = new Color(0.90f, 0.26f, 0.28f, 1.0f);
        private static readonly Color ColWhite      = new Color(0.97f, 0.98f, 1.00f, 1.0f);
        private static readonly Color ColMuted      = new Color(0.75f, 0.82f, 0.93f, 1.0f);

        public static NotificationManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var existing = FindAnyObjectByType<NotificationManager>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }
            GameObject go = new GameObject("NotificationManager");
            return go.AddComponent<NotificationManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlayUI();
        }

        private void Start()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= HandleAchievementUnlocked;
                AchievementManager.Instance.OnAchievementUnlocked += HandleAchievementUnlocked;
            }
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= HandleAchievementUnlocked;
            }
            if (roundedCardSprite != null) Destroy(roundedCardSprite);
            if (circleSprite != null) Destroy(circleSprite);
        }

        private void Update()
        {
            if (IsModalOpen)
            {
                bool esc = Keyboard.current != null
                    ? Keyboard.current.escapeKey.wasPressedThisFrame
                    : Input.GetKeyDown(KeyCode.Escape);
                if (esc)
                {
                    InvokeCancelModal();
                }
            }
        }

        private void HandleAchievementUnlocked(AchievementSO ach)
        {
            if (ach == null) return;
            ShowAchievementCard(ach.title, ach.description, ach.rewardCoins, ach.rewardPoints);
        }

        public void ShowToast(string title, string message, NotificationKind kind = NotificationKind.Info, float duration = 2.5f)
        {
            EnsureOverlayBuilt();
            LastToastTitle = title;
            LastToastMessage = message;
            toastQueue.Enqueue(new ToastRequest
            {
                title = title,
                message = message,
                kind = kind,
                duration = Mathf.Max(0.4f, duration)
            });

            if (toastCoroutine == null)
            {
                toastCoroutine = StartCoroutine(ProcessToastQueueRoutine());
            }
        }

        public void ShowAchievementCard(string title, string description, int rewardCoins, int rewardPoints)
        {
            EnsureOverlayBuilt();
            achievementQueue.Enqueue(new AchievementCardRequest
            {
                title = title,
                description = description,
                coins = rewardCoins,
                points = rewardPoints
            });

            if (achCoroutine == null)
            {
                achCoroutine = StartCoroutine(ProcessAchievementQueueRoutine());
            }
        }

        public void ShowConfirmModal(string title, string message, string confirmLabel, string cancelLabel,
            Action onConfirm, Action onCancel = null, bool isDestructive = false)
        {
            EnsureOverlayBuilt();
            pendingConfirmCallback = onConfirm;
            pendingCancelCallback = onCancel;
            IsModalOpen = true;

            Color accent = isDestructive ? ColRed : ColGold;
            if (modalAccentStripe != null) modalAccentStripe.color = accent;
            if (modalIconCircle != null) modalIconCircle.color = isDestructive ? ColRed : ColElectric;
            if (txtModalIcon != null) txtModalIcon.text = isDestructive ? "!" : "?";
            if (txtModalTitle != null) txtModalTitle.text = title;
            if (txtModalMsg != null) txtModalMsg.text = message;
            if (txtModalConfirm != null) txtModalConfirm.text = string.IsNullOrEmpty(confirmLabel) ? "CONFIRMAR" : confirmLabel;
            if (txtModalCancel != null) txtModalCancel.text = string.IsNullOrEmpty(cancelLabel) ? "CANCELAR" : cancelLabel;
            if (imgModalConfirmBg != null) imgModalConfirmBg.color = isDestructive ? ColRed : ColGreen;

            modalOverlay.SetActive(true);
            SyncOverlayCanvasActive();
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);

            if (modalAnimCoroutine != null) StopCoroutine(modalAnimCoroutine);
            modalAnimCoroutine = StartCoroutine(AnimateModalRoutine(opening: true));
        }

        public void CloseModal(bool invokeCancel = false)
        {
            if (!IsModalOpen) return;
            if (invokeCancel) InvokeCancelModal();
            else HideModalImmediateOrAnimated();
        }

        public void ClearAllNotifications()
        {
            toastQueue.Clear();
            achievementQueue.Clear();
            if (toastCoroutine != null) { StopCoroutine(toastCoroutine); toastCoroutine = null; }
            if (achCoroutine != null) { StopCoroutine(achCoroutine); achCoroutine = null; }
            if (toastRect != null) toastRect.gameObject.SetActive(false);
            if (achRect != null) achRect.gameObject.SetActive(false);
            IsToastVisible = false;
            IsAchievementVisible = false;
            CloseModal(false);
        }

        private void InvokeConfirmModal()
        {
            AudioManager.Instance?.PlaySfx(SfxType.UiConfirm);
            var cb = pendingConfirmCallback;
            pendingConfirmCallback = null;
            pendingCancelCallback = null;
            HideModalImmediateOrAnimated();
            cb?.Invoke();
        }

        private void InvokeCancelModal()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            var cb = pendingCancelCallback;
            pendingConfirmCallback = null;
            pendingCancelCallback = null;
            HideModalImmediateOrAnimated();
            cb?.Invoke();
        }

        private void HideModalImmediateOrAnimated()
        {
            IsModalOpen = false;
            if (modalOverlay != null)
            {
                if (modalAnimCoroutine != null) StopCoroutine(modalAnimCoroutine);
                modalOverlay.SetActive(false);
            }
            SyncOverlayCanvasActive();
        }

        private void SyncOverlayCanvasActive()
        {
            if (overlayCanvas == null) return;
            overlayCanvas.enabled = IsToastVisible || IsAchievementVisible || IsModalOpen;
        }

        private IEnumerator ProcessToastQueueRoutine()
        {
            while (toastQueue.Count > 0)
            {
                var req = toastQueue.Dequeue();
                IsToastVisible = true;
                ApplyToastStyle(req);
                toastRect.gameObject.SetActive(true);
                SyncOverlayCanvasActive();

                // Slide in from right with cubic ease-out (0.20s unscaled)
                float enterT = 0f;
                Vector2 targetPos = new Vector2(-32f, -112f);
                Vector2 hiddenPos = new Vector2(460f, -112f);
                while (enterT < 0.20f)
                {
                    enterT += Time.unscaledDeltaTime;
                    float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(enterT / 0.20f), 3f);
                    toastRect.anchoredPosition = Vector2.Lerp(hiddenPos, targetPos, u);
                    toastGroup.alpha = u;
                    yield return null;
                }
                toastRect.anchoredPosition = targetPos;
                toastGroup.alpha = 1f;

                float stayT = 0f;
                while (stayT < req.duration)
                {
                    stayT += Time.unscaledDeltaTime;
                    yield return null;
                }

                float exitT = 0f;
                while (exitT < 0.18f)
                {
                    exitT += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(exitT / 0.18f);
                    toastRect.anchoredPosition = Vector2.Lerp(targetPos, new Vector2(460f, -112f), u * u);
                    toastGroup.alpha = 1f - u;
                    yield return null;
                }
                toastRect.gameObject.SetActive(false);
                IsToastVisible = false;
                SyncOverlayCanvasActive();
            }
            toastCoroutine = null;
        }

        private void ApplyToastStyle(ToastRequest req)
        {
            Color accent = req.kind switch
            {
                NotificationKind.Success => ColGreen,
                NotificationKind.Warning => ColAmber,
                NotificationKind.Error   => ColRed,
                NotificationKind.Reward  => ColGold,
                _                        => ColElectric
            };
            string symbol = req.kind switch
            {
                NotificationKind.Success => "+",
                NotificationKind.Warning => "!",
                NotificationKind.Error   => "X",
                NotificationKind.Reward  => "*",
                _                        => "i"
            };
            if (toastAccentBar != null) toastAccentBar.color = accent;
            if (toastIconBg != null) toastIconBg.color = accent;
            if (txtToastSymbol != null) txtToastSymbol.text = symbol;
            if (txtToastTitle != null)
            {
                txtToastTitle.text = req.title;
                txtToastTitle.color = accent == ColGold ? ColGold : ColWhite;
            }
            if (txtToastMsg != null) txtToastMsg.text = req.message;
            if (toastBg != null) toastBg.color = ColNavyCard;

            if (req.kind == NotificationKind.Error || req.kind == NotificationKind.Warning)
                AudioManager.Instance?.PlaySfx(SfxType.UiError);
            else if (req.kind == NotificationKind.Success || req.kind == NotificationKind.Reward)
                AudioManager.Instance?.PlaySfx(SfxType.UiConfirm);
        }

        private IEnumerator ProcessAchievementQueueRoutine()
        {
            while (achievementQueue.Count > 0)
            {
                var req = achievementQueue.Dequeue();
                IsAchievementVisible = true;
                if (txtAchTitle != null) txtAchTitle.text = req.title.ToUpper();
                if (txtAchDesc != null) txtAchDesc.text = req.description;
                if (txtAchRewards != null)
                {
                    txtAchRewards.text = $"+{req.coins:N0} Monedas   |   +{req.points:N0} Pts";
                }

                achRect.gameObject.SetActive(true);
                SyncOverlayCanvasActive();
                float enterT = 0f;
                Vector2 topHidden = new Vector2(0f, 150f);
                Vector2 topVisible = new Vector2(0f, -34f);
                while (enterT < 0.24f)
                {
                    enterT += Time.unscaledDeltaTime;
                    float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(enterT / 0.24f), 3f);
                    achRect.anchoredPosition = Vector2.Lerp(topHidden, topVisible, u);
                    achGroup.alpha = u;
                    yield return null;
                }
                achRect.anchoredPosition = topVisible;
                achGroup.alpha = 1f;

                float stayT = 0f;
                while (stayT < 3.0f)
                {
                    stayT += Time.unscaledDeltaTime;
                    yield return null;
                }

                float exitT = 0f;
                while (exitT < 0.20f)
                {
                    exitT += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(exitT / 0.20f);
                    achRect.anchoredPosition = Vector2.Lerp(topVisible, topHidden, u);
                    achGroup.alpha = 1f - u;
                    yield return null;
                }
                achRect.gameObject.SetActive(false);
                IsAchievementVisible = false;
                SyncOverlayCanvasActive();
            }
            achCoroutine = null;
        }

        private IEnumerator AnimateModalRoutine(bool opening)
        {
            float elapsed = 0f;
            float dur = 0.18f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / dur);
                float eased = opening ? (1f - Mathf.Pow(1f - u, 3f)) : (1f - u);
                if (modalGroup != null) modalGroup.alpha = eased;
                if (modalCardRect != null)
                {
                    float s = Mathf.Lerp(0.90f, 1f, eased);
                    modalCardRect.localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }
            if (modalGroup != null) modalGroup.alpha = 1f;
            if (modalCardRect != null) modalCardRect.localScale = Vector3.one;
        }

        private void EnsureOverlayBuilt()
        {
            if (overlayCanvas == null) BuildOverlayUI();
        }

        private void BuildOverlayUI()
        {
            if (roundedCardSprite == null) roundedCardSprite = CreateRoundedSprite(64, 64, 14);
            if (circleSprite == null) circleSprite = CreateRoundedSprite(64, 64, 31);

            GameObject canvasObj = new GameObject("NotificationOverlayCanvas");
            canvasObj.transform.SetParent(transform, false);
            overlayCanvas = canvasObj.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 500;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            BuildToastPanel(canvasObj.transform);
            BuildAchievementBanner(canvasObj.transform);
            BuildConfirmModal(canvasObj.transform);
            SyncOverlayCanvasActive();
        }

        private void BuildToastPanel(Transform parent)
        {
            GameObject go = new GameObject("ToastNotification");
            go.transform.SetParent(parent, false);
            toastRect = go.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(1f, 1f);
            toastRect.anchorMax = new Vector2(1f, 1f);
            toastRect.pivot = new Vector2(1f, 1f);
            toastRect.anchoredPosition = new Vector2(-32f, -112f);
            toastRect.sizeDelta = new Vector2(460f, 92f);

            toastGroup = go.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;

            toastBg = go.AddComponent<Image>();
            toastBg.sprite = roundedCardSprite;
            toastBg.type = Image.Type.Sliced;
            toastBg.color = ColNavyCard;
            toastBg.raycastTarget = false;

            // Left glowing accent strip
            GameObject barObj = new GameObject("AccentBar");
            barObj.transform.SetParent(go.transform, false);
            RectTransform barRt = barObj.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.anchoredPosition = new Vector2(8f, 0f);
            barRt.sizeDelta = new Vector2(6f, -18f);
            toastAccentBar = barObj.AddComponent<Image>();
            toastAccentBar.color = ColGreen;

            // Circular icon badge
            GameObject iconObj = new GameObject("IconBadge");
            iconObj.transform.SetParent(go.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(48f, 0f);
            iconRt.sizeDelta = new Vector2(44f, 44f);
            toastIconBg = iconObj.AddComponent<Image>();
            toastIconBg.sprite = circleSprite;
            toastIconBg.color = ColGreen;

            txtToastSymbol = CreateLabel("TxtSymbol", iconObj.transform, Vector2.zero, new Vector2(40f, 40f), "+", 24, ColNavyDeep, FontStyles.Bold, TextAlignmentOptions.Center);

            // Title & Message
            txtToastTitle = CreateLabel("TxtToastTitle", go.transform, new Vector2(36f, 16f), new Vector2(350f, 30f), "NOTIFICACIÓN", 20, ColWhite, FontStyles.Bold, TextAlignmentOptions.Left);
            txtToastMsg   = CreateLabel("TxtToastMsg",   go.transform, new Vector2(36f, -16f), new Vector2(350f, 34f), "", 16, ColMuted, FontStyles.Normal, TextAlignmentOptions.Left);

            go.SetActive(false);
        }

        private void BuildAchievementBanner(Transform parent)
        {
            GameObject go = new GameObject("AchievementBanner");
            go.transform.SetParent(parent, false);
            achRect = go.AddComponent<RectTransform>();
            achRect.anchorMin = new Vector2(0.5f, 1f);
            achRect.anchorMax = new Vector2(0.5f, 1f);
            achRect.pivot = new Vector2(0.5f, 1f);
            achRect.anchoredPosition = new Vector2(0f, -34f);
            achRect.sizeDelta = new Vector2(560f, 108f);

            achGroup = go.AddComponent<CanvasGroup>();
            achGroup.blocksRaycasts = false;
            achGroup.interactable = false;

            Image goldRim = go.AddComponent<Image>();
            goldRim.sprite = roundedCardSprite;
            goldRim.type = Image.Type.Sliced;
            goldRim.color = ColGold;

            GameObject inner = new GameObject("InnerCard");
            inner.transform.SetParent(go.transform, false);
            RectTransform inRt = inner.AddComponent<RectTransform>();
            inRt.anchorMin = Vector2.zero;
            inRt.anchorMax = Vector2.one;
            inRt.offsetMin = new Vector2(4f, 4f);
            inRt.offsetMax = new Vector2(-4f, -4f);
            Image inBg = inner.AddComponent<Image>();
            inBg.sprite = roundedCardSprite;
            inBg.type = Image.Type.Sliced;
            inBg.color = ColNavyDeep;

            GameObject medal = new GameObject("TrophyBadge");
            medal.transform.SetParent(inner.transform, false);
            RectTransform mRt = medal.AddComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0f, 0.5f);
            mRt.anchorMax = new Vector2(0f, 0.5f);
            mRt.anchoredPosition = new Vector2(48f, 0f);
            mRt.sizeDelta = new Vector2(56f, 56f);
            Image mImg = medal.AddComponent<Image>();
            mImg.sprite = circleSprite;
            mImg.color = ColGold;
            CreateLabel("Star", medal.transform, Vector2.zero, new Vector2(50f, 50f), "#1", 22, ColNavyDeep, FontStyles.Bold, TextAlignmentOptions.Center);

            CreateLabel("Tag", inner.transform, new Vector2(36f, 29f), new Vector2(420f, 22f), "LOGRO DESBLOQUEADO", 14, ColGold, FontStyles.Bold, TextAlignmentOptions.Left);
            txtAchTitle = CreateLabel("AchTitle", inner.transform, new Vector2(36f, 5f), new Vector2(420f, 30f), "FRANCOTIRADOR", 22, ColWhite, FontStyles.Bold, TextAlignmentOptions.Left);
            txtAchDesc = CreateLabel("AchDesc", inner.transform, new Vector2(36f, -20f), new Vector2(420f, 24f), "", 14, ColMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            txtAchRewards = CreateLabel("AchRewards", inner.transform, new Vector2(36f, -38f), new Vector2(420f, 22f), "", 15, ColGreen, FontStyles.Bold, TextAlignmentOptions.Left);

            go.SetActive(false);
        }

        private void BuildConfirmModal(Transform parent)
        {
            modalOverlay = new GameObject("ConfirmModalOverlay");
            modalOverlay.transform.SetParent(parent, false);
            RectTransform ovRt = modalOverlay.AddComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.offsetMin = Vector2.zero;
            ovRt.offsetMax = Vector2.zero;
            modalGroup = modalOverlay.AddComponent<CanvasGroup>();

            Image dim = modalOverlay.AddComponent<Image>();
            dim.color = new Color(0.015f, 0.035f, 0.08f, 0.78f);

            // Modal card
            GameObject card = new GameObject("ModalCard");
            card.transform.SetParent(modalOverlay.transform, false);
            modalCardRect = card.AddComponent<RectTransform>();
            modalCardRect.sizeDelta = new Vector2(620f, 340f);
            Image cardBg = card.AddComponent<Image>();
            cardBg.sprite = roundedCardSprite;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = ColNavyCard;

            // Top color stripe
            GameObject stripe = new GameObject("TopStripe");
            stripe.transform.SetParent(card.transform, false);
            RectTransform stRt = stripe.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 1f);
            stRt.anchorMax = new Vector2(1f, 1f);
            stRt.pivot = new Vector2(0.5f, 1f);
            stRt.anchoredPosition = Vector2.zero;
            stRt.sizeDelta = new Vector2(0f, 8f);
            modalAccentStripe = stripe.AddComponent<Image>();
            modalAccentStripe.color = ColGold;

            // Header Icon Circle
            GameObject icObj = new GameObject("ModalIcon");
            icObj.transform.SetParent(card.transform, false);
            RectTransform icRt = icObj.AddComponent<RectTransform>();
            icRt.anchoredPosition = new Vector2(0f, 108f);
            icRt.sizeDelta = new Vector2(62f, 62f);
            modalIconCircle = icObj.AddComponent<Image>();
            modalIconCircle.sprite = circleSprite;
            modalIconCircle.color = ColGold;
            txtModalIcon = CreateLabel("TxtModalIcon", icObj.transform, Vector2.zero, new Vector2(56f, 56f), "?", 32, ColNavyDeep, FontStyles.Bold, TextAlignmentOptions.Center);

            txtModalTitle = CreateLabel("TxtModalTitle", card.transform, new Vector2(0f, 44f), new Vector2(560f, 42f), "CONFIRMAR", 28, ColWhite, FontStyles.Bold, TextAlignmentOptions.Center);
            txtModalMsg   = CreateLabel("TxtModalMsg",   card.transform, new Vector2(0f, -12f), new Vector2(540f, 76f), "¿Continuar?", 20, ColMuted, FontStyles.Normal, TextAlignmentOptions.Center);

            // Buttons row
            btnModalCancel = CreateModalButton("BtnModalCancel", card.transform, new Vector2(-135f, -106f), new Vector2(230f, 56f), "CANCELAR", ColNavyDeep, out _, out txtModalCancel);
            btnModalConfirm = CreateModalButton("BtnModalConfirm", card.transform, new Vector2(135f, -106f), new Vector2(230f, 56f), "CONFIRMAR", ColGreen, out imgModalConfirmBg, out txtModalConfirm);

            btnModalConfirm.onClick.AddListener(InvokeConfirmModal);
            btnModalCancel.onClick.AddListener(InvokeCancelModal);

            modalOverlay.SetActive(false);
        }

        private Button CreateModalButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, Color bgCol, out Image imgOut, out TMP_Text txtOut)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            imgOut = go.AddComponent<Image>();
            imgOut.sprite = roundedCardSprite;
            imgOut.type = Image.Type.Sliced;
            imgOut.color = bgCol;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = imgOut;
            go.AddComponent<UIButtonAnimator>();
            txtOut = CreateLabel("Label", go.transform, Vector2.zero, size, label, 20, ColWhite, FontStyles.Bold, TextAlignmentOptions.Center);
            return btn;
        }

        private static TMP_Text CreateLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color col, FontStyles style, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = col;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Sprite CreateRoundedSprite(int width, int height, int radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (width - 1 - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (height - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.5f - dist) * 255f);
                    pixels[y * width + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
