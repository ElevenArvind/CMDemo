using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMDemo.UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private GameObject MainMenuUI;
        [SerializeField] private GameObject GameUI;
        [SerializeField] private GameObject GameOverUI;
        [SerializeField] private GameObject GameModeSelectionUI;
        [SerializeField] private GameObject TopBarUI;
        [SerializeField] private GameModeController GameModeController;
        [SerializeField] private TopBarController TopBarController;

        public enum UIState
        {
            MainMenu,
            Game,
            GameOver,
            GameModeSelection
        }

        private UIState _currentState;

        private void Start()
        {
            ShowMainMenu();
        }

        void OnEnable()
        {
            GameModeController.onStartGame += HandleStartGame;
            TopBarController.onBackButtonPressed += ShowMainMenu;
            TopBarController.onReplayButtonPressed += ReplayGame;
            TopBarController.onSettingsButtonPressed += ShowGameModeSelection;
        }

        void OnDisable()
        {
            GameModeController.onStartGame -= HandleStartGame;
        }

        public Action<int, int> onStartGame;
        public Action onReplayGame;
        private void HandleStartGame(int rows, int columns)
        {
            ShowGameUI();

            onStartGame?.Invoke(rows, columns);
        }

        private void ResetScreens()
        {
            MainMenuUI.SetActive(false);
            GameUI.SetActive(false);
            GameOverUI.SetActive(false);
            GameModeSelectionUI.SetActive(false);
            TopBarUI.SetActive(false);
        }

        private void ShowMainMenu()
        {
            ResetScreens();
            MainMenuUI.SetActive(true);
        }

        public void ShowGameModeSelection()
        {
            GameModeSelectionUI.SetActive(true);
        }

        private void ShowGameUI()
        {
            ResetScreens();
            GameUI.SetActive(true);
            TopBarUI.SetActive(true);
        }

        private void ShowGameOverUI()
        {
            ResetScreens();
            GameOverUI.SetActive(true);
        }

        private void ReplayGame()
        {
            ShowGameUI();
            onReplayGame?.Invoke();
        }
    }
}
