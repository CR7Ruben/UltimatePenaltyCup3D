using System.Collections;
using UnityEngine;

namespace UltimatePenaltyCup.UI
{
    /// <summary>
    /// Smooth fade and micro-scale transition using unscaledDeltaTime so UI panels animate cleanly
    /// without delaying SetActive(true) availability or freezing when Time.timeScale == 0.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanelTransition : MonoBehaviour
    {
        [SerializeField] private float duration = 0.18f;
        [SerializeField] private float startScale = 0.96f;

        private CanvasGroup canvasGroup;
        private Coroutine animRoutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(PlayEnterRoutine());
        }

        private void OnDisable()
        {
            if (animRoutine != null) { StopCoroutine(animRoutine); animRoutine = null; }
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        private IEnumerator PlayEnterRoutine()
        {
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * startScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                canvasGroup.alpha = eased;
                float s = Mathf.Lerp(startScale, 1f, eased);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;
            animRoutine = null;
        }
    }
}
