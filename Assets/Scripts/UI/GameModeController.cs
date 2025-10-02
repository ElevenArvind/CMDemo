using System;
using System.Collections;
using System.Collections.Generic;
using CMDemo.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CMDemo.UI
{
    public class GameModeController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI RowTextLabel;
        [SerializeField] private TextMeshProUGUI ColumnTextLabel;
        [SerializeField] private GameObject ToastObject;
        [SerializeField] private TextMeshProUGUI ToastLabel;

        public Action<int, int> onStartGame;
        private int _rows = 2;
        private int _columns = 2;

        private Vector2 _animationStartPoint;
        void Awake()
        {
            _animationStartPoint = ToastObject.GetComponent<RectTransform>().anchoredPosition;
            ToastObject.SetActive(false);

            UpdateRowText();
            UpdateColumnText();
        }

        public void OnIncreaseRowButton()
        {
            if (_rows >= GameDataManager.Instance.GetMaxRows()) return;

            _rows++;
            UpdateRowText();
        }

        public void OnDecreaseRowButton()
        {
            if (_rows <= GameDataManager.Instance.GetMinRows()) return;

            _rows--;
            UpdateRowText();
        }
        private void UpdateRowText()
        {
            RowTextLabel.SetText($"<size=30>Row</size>\n{_rows}");
        }

        public void OnIncreaseColumnButton()
        {
            if (_columns >= GameDataManager.Instance.GetMaxColumns()) return;

            _columns++;
            UpdateColumnText();
        }

        public void OnDecreaseColumnButton()
        {
            if (_columns <= GameDataManager.Instance.GetMinColumns()) return;

            _columns--;
            UpdateColumnText();
        }

        private void UpdateColumnText()
        {
            ColumnTextLabel.SetText($"<size=30>Column</size>\n{_columns}");
        }

        public void OnStartGameButton()
        {
            if ((_rows * _columns) % 2 != 0)
            {
                ShowIncorrectInputToast();
                return;
            }

            onStartGame?.Invoke(_rows, _columns);
        }

        private void ShowIncorrectInputToast()
        {
            if (ToastObject.activeInHierarchy)
                return;
                
            ToastLabel.SetText(StringDataConstants.ToastInvalidInput);
            ToastObject.SetActive(true);

            AnimateToastIn();
            HideToastAfterDelay(2f);
        }

        private void AnimateToastIn()
        {
            RectTransform toastRect = ToastObject.GetComponent<RectTransform>();
            Vector2 targetPosition = new Vector2(_animationStartPoint.x, _animationStartPoint.y + 100);
            
            // Reset position and animate in
            toastRect.anchoredPosition = _animationStartPoint;
            toastRect.DOAnchorPos(targetPosition, 0.5f)
                .SetEase(Ease.OutBack);
        }

        private void HideToastAfterDelay(float delay)
        {
            RectTransform toastRect = ToastObject.GetComponent<RectTransform>();
            
            // Create a sequence: wait -> animate out -> deactivate
            var sequence = DOTween.Sequence();
            
            sequence.AppendInterval(delay);
            sequence.Append(toastRect.DOAnchorPos(_animationStartPoint, 0.5f).SetEase(Ease.InBack));
            sequence.AppendCallback(() => ToastObject.SetActive(false));
        }
    }
}
