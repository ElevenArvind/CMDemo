using System;
using System.Collections;
using System.Collections.Generic;
using CMDemo.Managers;
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
        [SerializeField] private GameOverController GameOverController;

        public enum UIState
        {
            MainMenu,
            Game,
            GameOver,
            GameModeSelection
        }

        private UIState _previousState;
        private UIState _currentState;
        public UIState CurrentState => _currentState;

        private void Start()
        {
            ShowMainMenu();
        }

        private void Update()
        {
            BackButtonHandler();
        }

        private void BackButtonHandler()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackButton();
            }
        }

        private void HandleBackButton()
        {
            switch (_currentState)
            {
                case UIState.MainMenu:
                    // On main menu, quit application
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                    break;
                    
                case UIState.GameModeSelection:
                    // Go back to main menu
                    HideGameModeSelection();
                    break;
                    
                case UIState.Game:
                    // Go back to main menu (same as top bar back button)
                    ShowMainMenu();
                    break;
                    
                case UIState.GameOver:
                    // Go back to main menu
                    ShowMainMenu();
                    break;
            }
        }

        void OnEnable()
        {
            GameModeController.onStartGame += HandleStartGame;
            TopBarController.onBackButtonPressed += ShowMainMenu;
            TopBarController.onReplayButtonPressed += ReplayGame;
            TopBarController.onSettingsButtonPressed += ShowGameModeSelection;
            GameManager.onGameWon += ShowGameOverUI;
            GameManager.onGameStartingIn += ShowStartingInUI;
            GameManager.onGameRestarted += HideGameOverUI;
        }

        void OnDisable()
        {
            GameModeController.onStartGame -= HandleStartGame;
            GameManager.onGameWon -= ShowGameOverUI;
            GameManager.onGameStartingIn -= ShowStartingInUI;
            GameManager.onGameRestarted -= HideGameOverUI;
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
            _currentState = UIState.MainMenu;
        }

        public void ShowGameModeSelection()
        {
            _previousState = _currentState;
            GameModeSelectionUI.SetActive(true);
            _currentState = UIState.GameModeSelection;
        }

        private void HideGameModeSelection()
        {
            GameModeSelectionUI.SetActive(false);
            _currentState = _previousState;
        }

        private void ShowGameUI()
        {
            ResetScreens();
            GameUI.SetActive(true);
            TopBarUI.SetActive(true);
            _currentState = UIState.Game;
        }

        private void ShowGameOverUI()
        {
            GameOverUI.SetActive(true);
            GameOverController.ShowGameOverToast();
            _currentState = UIState.GameOver;
        }

        private void ShowStartingInUI(int seconds)
        {
            // You can implement a "Starting In" UI here if needed
            // For now, we'll just log it
            Debug.Log($"Game starting in {seconds} seconds...");
            GameOverController.ShowStartingIn(seconds);
        }

        private void HideGameOverUI()
        {
            GameOverUI.SetActive(false);
            _currentState = UIState.Game;
        }

        private void ReplayGame()
        {
            ShowGameUI();
            onReplayGame?.Invoke();
        }
    }
}
