using System.Collections;
using TMPro;
using UnityEngine;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class WrongNoticePopup : MonoBehaviour
    {
        private const float ShowDuration = 0.16f;
        private const float HoldDuration = 2.2f;
        private const float HideDuration = 0.14f;

        public TMP_Text messageText;

        private RectTransform rectTransform;
        private Coroutine routine;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            messageText = messageText != null ? messageText : GetComponentInChildren<TMP_Text>(true);
        }

        public void Show(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(ShowRoutine());
            CancelInvoke(nameof(CloseNow));
            Invoke(nameof(CloseNow), ShowDuration + HoldDuration + HideDuration + 0.35f);
        }

        private IEnumerator ShowRoutine()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.zero;
            }

            yield return AnimateScale(0f, 1.12f, ShowDuration * 0.65f);
            yield return AnimateScale(1.12f, 1f, ShowDuration * 0.35f);

            var holdTime = 0f;
            while (holdTime < HoldDuration)
            {
                holdTime += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return AnimateScale(1f, 1.08f, HideDuration * 0.35f);
            yield return AnimateScale(1.08f, 0f, HideDuration * 0.65f);

            Destroy(gameObject);
        }

        private void CloseNow()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private IEnumerator AnimateScale(float from, float to, float duration)
        {
            if (rectTransform == null)
            {
                yield break;
            }

            var time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(time / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                rectTransform.localScale = Vector3.one * Mathf.Lerp(from, to, t);
                yield return null;
            }

            rectTransform.localScale = Vector3.one * to;
        }
    }
}
