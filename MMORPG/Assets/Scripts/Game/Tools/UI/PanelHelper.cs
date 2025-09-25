using DG.Tweening;
using System;
using UnityEngine;

namespace MMORPG.Tool
{
    /// <summary>
    /// Auxiliary panel switching
    /// </summary>
    public static class PanelHelper
    {
        private static CanvasGroup GetOrCreateCanvasGroup(GameObject obj, float initAlpha = 1)
        {
            if (!obj.TryGetComponent<CanvasGroup>(out var cg))
            {
                cg = obj.AddComponent<CanvasGroup>();
                cg.alpha = initAlpha;
            }

            return cg;
        }

        /// <summary>
        /// Gradient display
        /// </summary>
        public static void FadeIn(
            GameObject target,
            float duration = 0.3f,
            bool autoActive = true,
            bool autoBlocksRaycasts = true,
            bool autoAlpha = true,
            Action onComplete = null)
        {
            if (autoActive)
                target.SetActive(true);
            var cg = GetOrCreateCanvasGroup(target, 0);
            if (autoAlpha)
                cg.alpha = 0;
            var twe = cg.DOFade(1, duration);
            twe.onComplete += () =>
            {
                if (autoBlocksRaycasts)
                    cg.blocksRaycasts = true;
                onComplete?.Invoke();
            };
        }

        /// <summary>
        /// Gradient hidden
        /// </summary>
        /// <param name="target"></param>
        /// <param name="duration">duration</param>
        /// <param name="autoActive">Whether to automatically set active</param>
        /// <param name="onComplete">Gradient completed</param>
        public static void FadeOut(
            GameObject target,
            float duration = 0.3f,
            bool autoActive = true,
            bool autoBlocksRaycasts = true,
            bool autoAlpha = true,
            Action onComplete = null)
        {
            var cg = GetOrCreateCanvasGroup(target);
            if (autoBlocksRaycasts)
                cg.blocksRaycasts = false;
            if (autoAlpha)
                cg.alpha = 1;
            var twe = cg.DOFade(0, duration);
            twe.onComplete += () =>
            {
                if (autoActive) target.SetActive(false);
                onComplete?.Invoke();
            };
        }
    }
}
