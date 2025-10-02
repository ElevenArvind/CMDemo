using System;
using System.Collections;
using System.Collections.Generic;
using CMDemo.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarController : MonoBehaviour
{
    [SerializeField] private RectTransform ReplayButtonTransform;
    [SerializeField] private RectTransform BackButtonTransform;
    [SerializeField] private RectTransform SettingsButtonTransform;
    [SerializeField] private Image FillAmountImage;
    [SerializeField] private GameObject SavedGameIndicator;

    public Action onBackButtonPressed;

    public Action onReplayButtonPressed;

    public Action onSettingsButtonPressed;

    public Action onSaveGamePressed;

    private bool _shouldAnimate = true;
    void OnEnable()
    {
        if (_shouldAnimate)
        {
            AnimateIn();
        }

        GameManager.onScoreUpdated += UpdateScore;
        GameManager.onComboTimerLeft += OnComboTimerUpdate;
    }

    private bool _isTweening = false;
    void OnDisable()
    {
        GameManager.onScoreUpdated -= UpdateScore;
        GameManager.onComboTimerLeft -= OnComboTimerUpdate;
    }

    private void AnimateIn()
    {
        _isTweening = true;
        // Slide in from top
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 originalPosition = rectTransform.anchoredPosition;
        Vector2 offscreenPosition = new Vector2(originalPosition.x, originalPosition.y + rectTransform.rect.height);

        rectTransform.anchoredPosition = offscreenPosition;
        rectTransform.DOAnchorPos(originalPosition, 0.5f).SetEase(Ease.OutCubic).OnComplete(() => _isTweening = false);
    }

    public void OnBackButtonPressed()
    {
        if (_isTweening) return;

        _shouldAnimate = true;
        onBackButtonPressed?.Invoke();
    }

    public void OnReplayButtonPressed()
    {
        if (_isTweening) return;

        _shouldAnimate = false;
        // Small rotation animation with ping-pong back to original
        ReplayButtonTransform.DORotate(new Vector3(0, 0, -15f), 0.1f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(() => ReplayButtonTransform.rotation = Quaternion.identity);

        onReplayButtonPressed?.Invoke();
    }

    public void OnSettingsButtonPressed()
    {
        if (_isTweening) return;

        _shouldAnimate = false;
        // Small rotation animation with ping-pong back to original
        SettingsButtonTransform.DORotate(new Vector3(0, 0, 15f), 0.1f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(() => SettingsButtonTransform.rotation = Quaternion.identity);

        onSettingsButtonPressed?.Invoke();
    }

    [SerializeField] private TextMeshProUGUI ScoreLabelText;

    public void UpdateScore(int score)
    {
        // Update the score text
        ScoreLabelText.SetText($"<color=#000000>Score: </color>{score}");

        // Animate score update with a pop effect
        AnimateScoreUpdate();
    }

    private void AnimateScoreUpdate()
    {
        // Create a satisfying score update animation
        var sequence = DOTween.Sequence();

        // Scale up with bounce
        sequence.Append(ScoreLabelText.transform.DOScale(1.3f, 0.15f).SetEase(Ease.OutBack));

        // Scale back down
        sequence.Append(ScoreLabelText.transform.DOScale(1f, 0.2f).SetEase(Ease.InBack));

        // Optional: Add a subtle color flash
        Color originalColor = ScoreLabelText.color;
        Color flashColor = Color.red;

        sequence.Join(ScoreLabelText.DOColor(flashColor, 0.3f)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(() => ScoreLabelText.color = originalColor));
    }

    private bool _wasTimerActive = false;

    private void OnComboTimerUpdate(float fillAmount)
    {
        Debug.Log($"TopBarController: Received combo timer update - fillAmount: {fillAmount}");

        // Check if FillAmountImage is assigned
        if (FillAmountImage == null)
        {
            Debug.LogError("TopBarController: FillAmountImage is null! Please assign it in the inspector.");
            return;
        }

        // Update the fill amount of the image
        FillAmountImage.fillAmount = fillAmount;

        // Change color based on time left
        Color timerColor;
        if (fillAmount > 0.6f)
        {
            // Green when plenty of time left
            timerColor = Color.green;
            _wasTimerActive = true;
        }
        else if (fillAmount > 0.3f)
        {
            // Yellow when moderate time left
            timerColor = Color.yellow;
            _wasTimerActive = true;
        }
        else if (fillAmount > 0f)
        {
            // Red when little time left
            timerColor = Color.red;
            _wasTimerActive = true;
        }
        else
        {
            // fillAmount is 0 - could be timer expired or game reset
            timerColor = Color.clear;
            if (_wasTimerActive)
            {
                // Timer was active and now expired - trigger combo loss animation
                OnComboTimerExpired();
            }
            _wasTimerActive = false;
            return;
        }

        FillAmountImage.color = timerColor;
    }

    private void OnComboTimerExpired()
    {
        // Visual feedback when combo timer expires
        Debug.Log("Combo timer expired - UI feedback triggered");

        // Hide the timer image
        FillAmountImage.fillAmount = 0f;
        FillAmountImage.color = Color.clear;

        // Animate score label to show combo loss
        var sequence = DOTween.Sequence();

        // Shake the score label
        sequence.Append(ScoreLabelText.transform.DOShakePosition(0.5f, 10f, 20, 90f));

        // Flash red to indicate combo loss
        Color originalColor = ScoreLabelText.color;
        Color lossColor = Color.red;

        sequence.Join(ScoreLabelText.DOColor(lossColor, 0.2f)
            .SetLoops(3, LoopType.Yoyo)
            .OnComplete(() => ScoreLabelText.color = originalColor));
    }

    public void OnSaveGame()
    {
        if (_isTweening) return;

        _shouldAnimate = false;
        onSaveGamePressed?.Invoke();
        
        // Animate the indicator with fade in and fade out
        StartCoroutine(AnimateSavedGameIndicator(2.0f));
    }
    
    private IEnumerator AnimateSavedGameIndicator(float displayDuration)
    {
        // Setup CanvasGroup for fade animations
        CanvasGroup canvasGroup = SavedGameIndicator.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = SavedGameIndicator.AddComponent<CanvasGroup>();
        }
        
        // Start with indicator invisible
        canvasGroup.alpha = 0f;
        SavedGameIndicator.SetActive(true);
        
        // Fade in animation
        canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
        
        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out animation
        canvasGroup.DOFade(0f, 0.5f).SetEase(Ease.InQuad).OnComplete(() => {
            SavedGameIndicator.SetActive(false);
            canvasGroup.alpha = 1f; // Reset alpha for next time
        });
    }
}
