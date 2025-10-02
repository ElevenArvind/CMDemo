using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace CMDemo.UI
{
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private GameObject ToastObject;
        [SerializeField] private TMPro.TextMeshProUGUI ToastLabel;
        [SerializeField] private TMPro.TextMeshProUGUI StartingInLabel;

        public void ShowGameOverToast()
        {
            ToastLabel.SetText(StringDataConstants.ToastGameOver);
            ToastObject.SetActive(true);

            // Get the RectTransform for animations
            RectTransform toastRect = ToastObject.GetComponent<RectTransform>();

            // Store original values
            Vector3 originalScale = toastRect.localScale;
            Color originalTextColor = ToastLabel.color;

            // Set initial state - start invisible and small
            toastRect.localScale = Vector3.zero;
            ToastLabel.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);

            // Create animation sequence
            Sequence toastSequence = DOTween.Sequence();

            // Scale up with bounce effect
            toastSequence.Append(toastRect.DOScale(originalScale * 1.2f, 0.3f).SetEase(Ease.OutBack));

            // Scale down to normal size
            toastSequence.Append(toastRect.DOScale(originalScale, 0.2f).SetEase(Ease.InBack));

            // Fade in text simultaneously with first scale
            toastSequence.Join(ToastLabel.DOColor(originalTextColor, 0.3f));

            // Optional: Auto-hide after a few seconds with fade out
            toastSequence.AppendInterval(2.0f); // Show for 2 seconds
            toastSequence.Append(ToastLabel.DOColor(new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f), 0.5f));
            toastSequence.Join(toastRect.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));

            // Hide the object when animation completes
            toastSequence.OnComplete(() =>
            {
                ToastObject.SetActive(false);
                // Reset for next use
                toastRect.localScale = originalScale;
                ToastLabel.color = originalTextColor;
            });
        }

        public void ShowStartingIn(float seconds)
        {
            StartingInLabel.SetText($"{StringDataConstants.ToastStartingNewGame} {seconds}...");
            StartingInLabel.gameObject.SetActive(true);
            // You can add animations here if needed
        }
    }
}
