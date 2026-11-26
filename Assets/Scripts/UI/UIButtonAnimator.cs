using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimatePenaltyCup.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.045f;
        [SerializeField] private float pressScale = 0.96f;
        [SerializeField] private float animSpeed = 14f;

        private Button btn;
        private Vector3 targetScale = Vector3.one;
        private bool isHovered = false;

        private void Awake()
        {
            btn = GetComponent<Button>();
            targetScale = Vector3.one;
        }

        private void OnDisable()
        {
            isHovered = false;
            targetScale = Vector3.one;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (transform.localScale != targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (btn != null && !btn.interactable) return;
            isHovered = true;
            targetScale = Vector3.one * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            targetScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (btn != null && !btn.interactable) return;
            targetScale = Vector3.one * pressScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (btn != null && !btn.interactable)
            {
                targetScale = Vector3.one;
                return;
            }
            targetScale = isHovered ? Vector3.one * hoverScale : Vector3.one;
        }
    }
}
