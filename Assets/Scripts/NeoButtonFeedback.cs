using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class NeoButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float HoverScale = 1.05f;
        private const float PressedScale = 0.96f;
        private const float Duration = 0.08f;

        private RectTransform target;
        private Selectable selectable;
        private Vector3 baseScale;
        private Coroutine motion;
        private bool hovering;

        private void Awake()
        {
            target = transform as RectTransform;
            selectable = GetComponent<Selectable>();
            if (target != null)
            {
                baseScale = target.localScale;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            if (!CanAnimate())
            {
                return;
            }

            ScaleTo(HoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            if (!CanAnimate())
            {
                return;
            }

            ScaleTo(1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanAnimate())
            {
                return;
            }

            ScaleTo(PressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!CanAnimate())
            {
                return;
            }

            ScaleTo(hovering ? HoverScale : 1f);
        }

        private bool CanAnimate()
        {
            return target != null && (selectable == null || selectable.interactable);
        }

        private void ScaleTo(float scale)
        {
            var targetScale = baseScale * scale;
            if (!Application.isPlaying)
            {
                target.localScale = targetScale;
                return;
            }

            if (motion != null)
            {
                StopCoroutine(motion);
            }

            motion = StartCoroutine(Animate(targetScale, Duration));
        }

        private IEnumerator Animate(Vector3 targetScale, float animationDuration)
        {
            var startScale = target.localScale;
            var time = 0f;

            while (time < animationDuration)
            {
                time += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(time / animationDuration);
                target.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            target.localScale = targetScale;
        }
    }
}
