using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace CMDemo.Components
{
    public class Card : MonoBehaviour
    {
        [SerializeField] private GameObject FrontSideObject;
        [SerializeField] private GameObject BackSideObject;
        [SerializeField] private TextMeshProUGUI FrontTextLabel;

        private int _id;
        public int Id => _id;
        private string _value;

        public string Value => _value;
        
        public Color CardColor => FrontTextLabel?.color ?? Color.white;

        private Action<int> _onClick;
        public void SetProperties(int id, string text, Color color, Vector3 position, Vector2 sizeDelta, Action<int> onClick = null)
        {
            _id = id;
            _value = text;
            FrontTextLabel.SetText(text);
            FrontTextLabel.color = color;
            _onClick = onClick;
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = sizeDelta;
            }

            FrontSideObject.SetActive(false);
            BackSideObject.SetActive(true);
        }

        private bool _isTweening = false;
        public void OnSelectedCard()
        {
            // Only prevent if currently tweening - allow re-flipping if card is back to closed state
            if (_isTweening) return;

            // Only flip if card is currently showing back side (closed)
            if (!BackSideObject.activeInHierarchy) return;

            _isTweening = true;
            
            Flip();
        }

        private void Flip()
        {
            transform.SetAsLastSibling();
            // Play card flip sound
            AudioManager.Instance?.PlayCardFlip();
            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector3 originalPosition = rectTransform.anchoredPosition3D;
            
            // Create sequence for flip with lift effect
            var sequence = DOTween.Sequence();
            
            // Lift up and rotate to 90 degrees simultaneously
            sequence.Append(rectTransform.DOAnchorPos3D(originalPosition + new Vector3(0, 20f, 0), 0.15f).SetEase(Ease.OutQuad));
            sequence.Join(rectTransform.DORotate(new Vector3(0, 90, 0), 0.15f));
            
            sequence.AppendCallback(() =>
            {
                // Switch sides at the middle of the flip
                FrontSideObject.SetActive(true);
                BackSideObject.SetActive(false);
            });
            
            // Come back down and complete rotation
            sequence.Append(rectTransform.DOAnchorPos3D(originalPosition, 0.15f).SetEase(Ease.InQuad));
            sequence.Join(rectTransform.DORotate(new Vector3(0, 0, 0), 0.15f));
            
            sequence.OnComplete(() =>
            {
                _isTweening = false;
                _onClick?.Invoke(_id);
            });
        }

        public void FlipToBack()
        {
            if (_isTweening) return;

            _isTweening = true;

            transform.SetAsLastSibling();

            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector3 originalPosition = rectTransform.anchoredPosition3D;
            
            // Create sequence for flip back with lift effect
            var sequence = DOTween.Sequence();
            
            // Lift up and rotate to 90 degrees simultaneously
            sequence.Append(rectTransform.DOAnchorPos3D(originalPosition + new Vector3(0, 20f, 0), 0.15f).SetEase(Ease.OutQuad));
            sequence.Join(rectTransform.DORotate(new Vector3(0, 90, 0), 0.15f));
            
            sequence.AppendCallback(() =>
            {
                // Switch sides at the middle of the flip
                FrontSideObject.SetActive(false);
                BackSideObject.SetActive(true);
            });
            
            // Come back down and complete rotation
            sequence.Append(rectTransform.DOAnchorPos3D(originalPosition, 0.15f).SetEase(Ease.InQuad));
            sequence.Join(rectTransform.DORotate(new Vector3(0, 0, 0), 0.15f));
            
            sequence.OnComplete(() =>
            {
                _isTweening = false;
            });
        }

        public void OnMatchFound()
        {
            if (_isTweening) return;
            _isTweening = true;

            transform.SetAsLastSibling();

            RectTransform rectTransform = GetComponent<RectTransform>();
            
            // Create a satisfying match animation sequence
            var sequence = DOTween.Sequence();
            
            // First: Scale up slightly (pop effect)
            sequence.Append(rectTransform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
            
            // Then: Scale down to shrink
            sequence.Append(rectTransform.DOScale(0.8f, 0.3f).SetEase(Ease.InBack));
            
            // Optional: Add a slight rotation for extra flair
            sequence.Join(rectTransform.DORotate(new Vector3(0, 0, 10f), 0.25f).SetEase(Ease.OutQuad));
            
            // Finally: Scale to zero (disappear completely)
            sequence.Append(rectTransform.DOScale(0f, 0.2f).SetEase(Ease.InQuad));
            
            sequence.OnComplete(() =>
            {
                _isTweening = false;
                // Card is now invisible (scaled to zero)
            });
        }

        public void ResetCard()
        {
            // Reset all visual states
            _isTweening = false;
            FrontSideObject.SetActive(false);
            BackSideObject.SetActive(true);
            
            // Reset transform
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;
            rectTransform.rotation = Quaternion.identity;
        }

        public void OnMatchNotFound()
        {
            if (_isTweening) return;
            _isTweening = true;

            transform.SetAsLastSibling();

            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector3 originalPosition = rectTransform.anchoredPosition3D;
            Vector3 originalRotation = rectTransform.rotation.eulerAngles;
            
            // Create a "shake" animation to indicate mismatch
            var sequence = DOTween.Sequence();
            
            // Quick shake left and right
            sequence.Append(rectTransform.DOAnchorPos3D(originalPosition + new Vector3(-10f, 0, 0), 0.1f).SetEase(Ease.OutQuad));
            sequence.Append(rectTransform.DOAnchorPos3D(originalPosition + new Vector3(10f, 0, 0), 0.1f).SetEase(Ease.InOutQuad));
            sequence.Append(rectTransform.DOAnchorPos3D(originalPosition + new Vector3(-5f, 0, 0), 0.08f).SetEase(Ease.InOutQuad));
            sequence.Append(rectTransform.DOAnchorPos3D(originalPosition, 0.08f).SetEase(Ease.InQuad));
            
            // Add a slight red tint effect (optional - if you have an Image component)
            // sequence.Join(GetComponent<Image>().DOColor(Color.red, 0.15f).SetLoops(2, LoopType.Yoyo));
            
            sequence.OnComplete(() =>
            {
                _isTweening = false;
                // Card is ready for flip back animation
            });
        }

        // Method to set card to front side without animation (for loading saved games)
        public void SetToFrontSide()
        {
            FrontSideObject.SetActive(true);
            BackSideObject.SetActive(false);
        }
    }
}
