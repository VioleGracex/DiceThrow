using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BG3DiceSystem.Core.Utilities.Tweening
{
    public enum Ease
    {
        Linear,
        OutQuad,
        InQuad,
        InOutQuad,
        OutBack,
        InBack,
        OutBounce,
        OutElastic
    }

    public class CustomTween
    {
        public Action OnCompleteCallback;
        public bool IsActive = true;

        public CustomTween OnComplete(Action callback)
        {
            OnCompleteCallback += callback;
            return this;
        }

        public virtual void Kill()
        {
            IsActive = false;
        }
    }

    public class CustomSequence : CustomTween
    {
        private List<Func<IEnumerator>> _tasks = new List<Func<IEnumerator>>();

        public CustomSequence Append(CustomTween tween)
        {
            _tasks.Add(() => WaitForTween(tween));
            return this;
        }

        public CustomSequence AppendCallback(Action callback)
        {
            _tasks.Add(() => ExecuteCallback(callback));
            return this;
        }

        public CustomSequence AppendInterval(float duration)
        {
            _tasks.Add(() => WaitForSeconds(duration));
            return this;
        }

        private IEnumerator WaitForTween(CustomTween tween)
        {
            while (tween != null && tween.IsActive)
            {
                yield return null;
            }
        }

        private IEnumerator ExecuteCallback(Action callback)
        {
            callback?.Invoke();
            yield return null;
        }

        private IEnumerator WaitForSeconds(float duration)
        {
            yield return new WaitForSeconds(duration);
        }

        public IEnumerator RunSequence()
        {
            foreach (var task in _tasks)
            {
                if (!IsActive) yield break;
                yield return task();
            }
            IsActive = false;
            OnCompleteCallback?.Invoke();
        }
    }

    public static class CustomDOTween
    {
        private class CoroutineRunner : MonoBehaviour { }
        private static CoroutineRunner _runner;

        private static void EnsureRunner()
        {
            if (!Application.isPlaying) return;
            if (_runner == null)
            {
                GameObject obj = new GameObject("[CustomDOTween_Runner]");
                UnityEngine.Object.DontDestroyOnLoad(obj);
                _runner = obj.AddComponent<CoroutineRunner>();
            }
        }

        public static CustomSequence Sequence()
        {
            if (!Application.isPlaying) return new CustomSequence();
            EnsureRunner();
            CustomSequence seq = new CustomSequence();
            if (_runner != null) _runner.StartCoroutine(seq.RunSequence());
            return seq;
        }

        public static Coroutine StartCoroutine(IEnumerator routine)
        {
            if (!Application.isPlaying) return null;
            EnsureRunner();
            return _runner != null ? _runner.StartCoroutine(routine) : null;
        }

        public static float EvaluateEase(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.OutQuad: return t * (2 - t);
                case Ease.InQuad: return t * t;
                case Ease.InOutQuad: return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
                case Ease.OutBack:
                    float c1 = 1.70158f;
                    float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
                case Ease.OutBounce:
                    float n1 = 7.5625f;
                    float d1 = 2.75f;
                    if (t < 1 / d1) return n1 * t * t;
                    else if (t < 2 / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
                    else if (t < 2.5 / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
                    else return n1 * (t -= 2.625f / d1) * t + 0.984375f;
                default: return t;
            }
        }
    }

    public static class CustomTweenExtensions
    {
        public static CustomTween DOScale(this Transform target, Vector3 endValue, float duration, Ease ease = Ease.OutQuad)
        {
            CustomTween tween = new CustomTween();
            if (Application.isPlaying) CustomDOTween.StartCoroutine(AnimateScale(target, endValue, duration, ease, tween));
            return tween;
        }

        public static CustomTween DOScale(this Transform target, float endValue, float duration, Ease ease = Ease.OutQuad)
        {
            return target.DOScale(Vector3.one * endValue, duration, ease);
        }

        private static IEnumerator AnimateScale(Transform target, Vector3 endValue, float duration, Ease ease, CustomTween tween)
        {
            if (target == null) yield break;
            Vector3 startValue = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null || !tween.IsActive) yield break;
                elapsed += Time.deltaTime;
                float t = CustomDOTween.EvaluateEase(ease, elapsed / duration);
                target.localScale = Vector3.Lerp(startValue, endValue, t);
                yield return null;
            }
            if (target != null) target.localScale = endValue;
            tween.IsActive = false;
            tween.OnCompleteCallback?.Invoke();
        }

        public static CustomTween DOMove(this Transform target, Vector3 endValue, float duration, Ease ease = Ease.OutQuad)
        {
            CustomTween tween = new CustomTween();
            if (Application.isPlaying) CustomDOTween.StartCoroutine(AnimateMove(target, endValue, duration, ease, tween));
            return tween;
        }

        private static IEnumerator AnimateMove(Transform target, Vector3 endValue, float duration, Ease ease, CustomTween tween)
        {
            if (target == null) yield break;
            Vector3 startValue = target.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null || !tween.IsActive) yield break;
                elapsed += Time.deltaTime;
                float t = CustomDOTween.EvaluateEase(ease, elapsed / duration);
                target.position = Vector3.Lerp(startValue, endValue, t);
                yield return null;
            }
            if (target != null) target.position = endValue;
            tween.IsActive = false;
            tween.OnCompleteCallback?.Invoke();
        }

        public static CustomTween DOFade(this CanvasGroup target, float endValue, float duration, Ease ease = Ease.OutQuad)
        {
            CustomTween tween = new CustomTween();
            if (Application.isPlaying) CustomDOTween.StartCoroutine(AnimateFade(target, endValue, duration, ease, tween));
            return tween;
        }

        private static IEnumerator AnimateFade(CanvasGroup target, float endValue, float duration, Ease ease, CustomTween tween)
        {
            if (target == null) yield break;
            float startValue = target.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null || !tween.IsActive) yield break;
                elapsed += Time.deltaTime;
                float t = CustomDOTween.EvaluateEase(ease, elapsed / duration);
                target.alpha = Mathf.Lerp(startValue, endValue, t);
                yield return null;
            }
            if (target != null) target.alpha = endValue;
            tween.IsActive = false;
            tween.OnCompleteCallback?.Invoke();
        }

        public static CustomTween DOPunchScale(this Transform target, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            CustomTween tween = new CustomTween();
            if (Application.isPlaying) CustomDOTween.StartCoroutine(AnimatePunchScale(target, punch, duration, vibrato, tween));
            return tween;
        }

        private static IEnumerator AnimatePunchScale(Transform target, Vector3 punch, float duration, int vibrato, CustomTween tween)
        {
            if (target == null) yield break;
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null || !tween.IsActive) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float damp = Mathf.Sin(t * Mathf.PI * vibrato) * (1f - t);
                target.localScale = originalScale + Vector3.Scale(punch, Vector3.one * damp);
                yield return null;
            }
            if (target != null) target.localScale = originalScale;
            tween.IsActive = false;
            tween.OnCompleteCallback?.Invoke();
        }

        public static CustomTween DOCounter(this TextMeshProUGUI target, int startValue, int endValue, float duration)
        {
            CustomTween tween = new CustomTween();
            if (Application.isPlaying) CustomDOTween.StartCoroutine(AnimateCounter(target, startValue, endValue, duration, tween));
            return tween;
        }

        private static IEnumerator AnimateCounter(TextMeshProUGUI target, int startValue, int endValue, float duration, CustomTween tween)
        {
            if (target == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null || !tween.IsActive) yield break;
                elapsed += Time.deltaTime;
                int current = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, elapsed / duration));
                target.text = current.ToString();
                yield return null;
            }
            if (target != null) target.text = endValue.ToString();
            tween.IsActive = false;
            tween.OnCompleteCallback?.Invoke();
        }

        public static CustomTween DOAnchorPos(this RectTransform target, Vector2 endValue, float duration, Ease ease = Ease.OutQuad)
        {
            CustomTween tween = new CustomTween();
            if (Application.isPlaying) CustomDOTween.StartCoroutine(AnimateAnchorPos(target, endValue, duration, ease, tween));
            else if (target != null) target.anchoredPosition = endValue;
            return tween;
        }

        private static IEnumerator AnimateAnchorPos(RectTransform target, Vector2 endValue, float duration, Ease ease, CustomTween tween)
        {
            if (target == null) yield break;
            Vector2 startValue = target.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null || !tween.IsActive) yield break;
                elapsed += Time.deltaTime;
                float t = CustomDOTween.EvaluateEase(ease, elapsed / duration);
                target.anchoredPosition = Vector2.Lerp(startValue, endValue, t);
                yield return null;
            }
            if (target != null) target.anchoredPosition = endValue;
            tween.IsActive = false;
            tween.OnCompleteCallback?.Invoke();
        }

        public static CustomTween DORotate(this Transform target, Vector3 endValue, float duration, Ease ease = Ease.OutQuad)
        {
            CustomTween tween = new CustomTween();
            if (Application.isPlaying) CustomDOTween.StartCoroutine(AnimateRotate(target, Quaternion.Euler(endValue), duration, ease, tween));
            else if (target != null) target.localRotation = Quaternion.Euler(endValue);
            return tween;
        }

        private static IEnumerator AnimateRotate(Transform target, Quaternion endValue, float duration, Ease ease, CustomTween tween)
        {
            if (target == null) yield break;
            Quaternion startValue = target.localRotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null || !tween.IsActive) yield break;
                elapsed += Time.deltaTime;
                float t = CustomDOTween.EvaluateEase(ease, elapsed / duration);
                target.localRotation = Quaternion.Slerp(startValue, endValue, t);
                yield return null;
            }
            if (target != null) target.localRotation = endValue;
            tween.IsActive = false;
            tween.OnCompleteCallback?.Invoke();
        }
    }
}
