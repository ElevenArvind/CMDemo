using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CMDemo.Components;
using CMDemo.UI;
using UnityEngine;

namespace CMDemo.Managers
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private RectTransform CardsParent;
        [SerializeField] private GameObject CardPrefab;
        [SerializeField] private RectTransform PlayArea;
        [SerializeField] private UIController UIController;
        private List<Card> _cards = new List<Card>();
        private List<int> _flippedCards = new List<int>();
        private List<int> _matchedCards = new List<int>();
        private Vector2 _currentLayout;
        private int _currentScore;
        public static Action<int> onScoreUpdated;
        public static Action<float> onComboTimerLeft;
        public static Action onGameWon;
        public static Action onGameRestarted;
        public static Action<int> onGameStartingIn;

        // Combo system variables
        private float _lastMatchTime;
        private int _comboLevel = 0;
        private bool _isFirstMatch = true;
        private Coroutine _comboTimerCoroutine;
        void OnEnable()
        {
            UIController.onStartGame += StartGame;
            UIController.onReplayGame += ReplayGame;
            UIController.onResumeGame += ResumeGame;
            UIController.onSaveGame += SaveGameState;
        }

        void OnDisable()
        {
            UIController.onStartGame -= StartGame;
            UIController.onReplayGame -= ReplayGame;
            UIController.onResumeGame -= ResumeGame;
            UIController.onSaveGame -= SaveGameState;
        }

        private void ResetCards()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    card.ResetCard(); // Reset card state before destroying
                }
                Destroy(card.gameObject);
            }
            _cards.Clear();
            
            // Clear game state lists
            _matchedCards.Clear();
            _flippedCards.Clear();

            // Reset combo system
            _comboLevel = 0;
            _isFirstMatch = true;

            // Stop combo timer
            if (_comboTimerCoroutine != null)
            {
                StopCoroutine(_comboTimerCoroutine);
                _comboTimerCoroutine = null;
            }

            onComboTimerLeft?.Invoke(0f); // Hide timer UI
        }


        public void StartGame(int rows, int columns)
        {
            _currentLayout = new Vector2(rows, columns);
            InitGame(rows, columns);
        }

        private void InitGame(int rows, int columns)
        {
            OnResetScore();
            ResetCards();

            var layoutInfo = GetLayoutInfo(rows, columns);
            var cardData = GenerateCardPairs(layoutInfo.totalCards);
            ShuffleCards(cardData.symbols, cardData.colors);
            CreateAndPositionCards(layoutInfo, cardData.symbols, cardData.colors);
            
            // Clear any existing save data since we're starting a new game
            DeleteSavedGame();
        }

        private (int columns, int rows, int totalCards, Vector2 cardSize, float startX, float startY, float padding) GetLayoutInfo(int rows, int columns)
        {
            int totalCards = columns * rows;

            // Get PlayArea dimensions
            Vector2 playAreaSize = PlayArea.rect.size;

            // Calculate card size with some padding
            float padding = 10f; // Padding between cards
            float cardWidth = (playAreaSize.x - (padding * (columns + 1))) / columns;
            float cardHeight = (playAreaSize.y - (padding * (rows + 1))) / rows;
            Vector2 cardSize = new Vector2(cardWidth, cardHeight);

            // Calculate starting position (top-left corner)
            float startX = -playAreaSize.x * 0.5f + padding + cardWidth * 0.5f;
            float startY = playAreaSize.y * 0.5f - padding - cardHeight * 0.5f;

            return (columns, rows, totalCards, cardSize, startX, startY, padding);
        }

        private (List<string> symbols, List<Color> colors) GenerateCardPairs(int totalCards)
        {
            List<string> cardSymbols = new List<string>();
            List<Color> cardColors = new List<Color>();
            int pairCount = totalCards / 2;

            // Add pairs of symbols and their corresponding colors from GameDataManager
            for (int i = 0; i < pairCount; i++)
            {
                string symbol = GameDataManager.Instance.GetSymbol(i);
                Color pairColor = GameDataManager.Instance.GetColor(i);
                cardSymbols.Add(symbol);
                cardSymbols.Add(symbol);
                cardColors.Add(pairColor);
                cardColors.Add(pairColor);
            }

            return (cardSymbols, cardColors);
        }

        private void ShuffleCards(List<string> symbols, List<Color> colors)
        {
            // Shuffle the card symbols and colors together
            for (int i = 0; i < symbols.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, symbols.Count);

                // Swap symbols
                string tempSymbol = symbols[i];
                symbols[i] = symbols[randomIndex];
                symbols[randomIndex] = tempSymbol;

                // Swap colors
                Color tempColor = colors[i];
                colors[i] = colors[randomIndex];
                colors[randomIndex] = tempColor;
            }
        }

        private void CreateAndPositionCards(
            (int columns, int rows, int totalCards, Vector2 cardSize, float startX, float startY, float padding) layoutInfo,
            List<string> symbols,
            List<Color> colors)
        {
            for (int i = 0; i < layoutInfo.totalCards; i++)
            {
                int row = i / layoutInfo.columns;
                int col = i % layoutInfo.columns;

                // Calculate card position
                float posX = layoutInfo.startX + col * (layoutInfo.cardSize.x + layoutInfo.padding);
                float posY = layoutInfo.startY - row * (layoutInfo.cardSize.y + layoutInfo.padding);
                Vector2 cardPosition = new Vector2(posX, posY);

                GameObject cardObject = Instantiate(CardPrefab, CardsParent);
                Card cardComponent = cardObject.GetComponent<Card>();

                // Use the shuffled card symbol and its matching color
                string cardSymbol = symbols[i];
                Color cardColor = colors[i];
                cardComponent.SetProperties(i, cardSymbol, cardColor, cardPosition, layoutInfo.cardSize, OnCardFlipped);
                _cards.Add(cardComponent);
            }
        }

        private void OnCardFlipped(int cardId)
        {
            // Prevent flipping if card is already matched or already flipped
            if (_matchedCards.Contains(cardId) || _flippedCards.Contains(cardId))
            {
                Debug.Log($"Card {cardId} flip prevented - already matched: {_matchedCards.Contains(cardId)}, already flipped: {_flippedCards.Contains(cardId)}");
                return;
            }

            Debug.Log($"Card {cardId} flipped! Current flipped cards: [{string.Join(", ", _flippedCards)}]");
            _flippedCards.Add(cardId);
            Debug.Log($"After adding: [{string.Join(", ", _flippedCards)}]");

            // Process pairs independently - each pair gets its own processing
            ProcessAvailablePairs();
        }

        private void ProcessAvailablePairs()
        {
            // Process pairs in groups of 2, each independently
            while (_flippedCards.Count >= 2)
            {
                // Take the first two cards for independent processing
                int firstCardId = _flippedCards[0];
                int secondCardId = _flippedCards[1];
                
                // Remove them from the flipped list immediately so they don't interfere with other pairs
                _flippedCards.Remove(firstCardId);
                _flippedCards.Remove(secondCardId);
                
                Debug.Log($"Processing independent pair: {firstCardId} and {secondCardId}");
                Debug.Log($"Remaining flipped cards: [{string.Join(", ", _flippedCards)}]");
                
                // Start independent processing for this pair
                StartCoroutine(ProcessPairIndependently(firstCardId, secondCardId));
            }
        }

        private IEnumerator ProcessPairIndependently(int firstCardId, int secondCardId)
        {
            Card firstCard = _cards.Find(c => c.Id == firstCardId);
            Card secondCard = _cards.Find(c => c.Id == secondCardId);

            // Validate that both cards exist and aren't already matched
            if (firstCard == null || secondCard == null || 
                _matchedCards.Contains(firstCardId) || _matchedCards.Contains(secondCardId))
            {
                Debug.LogWarning($"Pair processing cancelled - cards {firstCardId}, {secondCardId} no longer valid for processing");
                yield break;
            }

            Debug.Log($"Checking match: Card {firstCardId} ({firstCard.Value}) vs Card {secondCardId} ({secondCard.Value})");

            // Brief moment to let players see both cards
            yield return new WaitForSeconds(0.3f);

            // Double-check cards haven't been matched by another pair during the delay
            if (_matchedCards.Contains(firstCardId) || _matchedCards.Contains(secondCardId))
            {
                Debug.LogWarning($"Pair processing cancelled after delay - cards {firstCardId}, {secondCardId} already matched by another pair");
                yield break;
            }

            if (DoCardsMatch(firstCard, secondCard))
            {
                // Cards match - keep them flipped and mark as matched
                _matchedCards.Add(firstCardId);
                _matchedCards.Add(secondCardId);
                Debug.Log($"Match found! Cards {firstCardId} and {secondCardId}");
                Debug.Log($"Total matched cards now: {_matchedCards.Count} out of {_cards.Count}");

                // Play match found sound
                AudioManager.Instance?.PlayMatchFound();

                OnScoreUpdate();

                // Play match found animations immediately
                firstCard.OnMatchFound();
                secondCard.OnMatchFound();

                // Cards are already removed from _flippedCards list in ProcessAvailablePairs
                // so no need to remove them again here

                    // Check if all cards are matched (game won)
                    Debug.Log($"Game Win Check - Matched cards: {_matchedCards.Count}, Total cards: {_cards.Count}");
                    Debug.Log($"Matched card IDs: [{string.Join(", ", _matchedCards)}]");
                    
                    if (_matchedCards.Count == _cards.Count)
                    {
                        Debug.Log("Congratulations! All cards matched!");
                        // Stop and hide combo timer since game is over
                        if (_comboTimerCoroutine != null)
                        {
                            StopCoroutine(_comboTimerCoroutine);
                            _comboTimerCoroutine = null;
                        }
                        onComboTimerLeft?.Invoke(0f); // Hide timer UI
                        
                        // Play victory sound with a slight delay for better effect
                        AudioManager.Instance?.PlaySoundWithDelay(AudioManager.SoundType.Victory, 0.1f);
                        
                        OnGameWon();
                    }
                }
                else
                {
                    // Cards don't match - show mismatch animation then flip back
                    // Play mistake sound
                    AudioManager.Instance?.PlayMistakeFound();
                    
                    firstCard.OnMatchNotFound();
                    secondCard.OnMatchNotFound();
                    Debug.Log($"No match. Cards {firstCardId} and {secondCardId} will flip back after mismatch animation.");

                    // Cards are already removed from _flippedCards list in ProcessAvailablePairs
                    // Wait for mismatch animation to complete before flipping back
                    StartCoroutine(FlipBackAfterMismatch(firstCard, secondCard, firstCardId, secondCardId));
                }
        }

        private bool DoCardsMatch(Card firstCard, Card secondCard)
        {
            return firstCard.Value == secondCard.Value;
        }

        private void FlipCardBack(Card card)
        {
            card.FlipToBack();
        }

        private IEnumerator FlipBackAfterMismatch(Card firstCard, Card secondCard, int firstCardId, int secondCardId)
        {
            // Wait for the mismatch animation to complete (0.36 seconds total)
            yield return new WaitForSeconds(0.4f);

            // Now flip the cards back
            FlipCardBack(firstCard);
            FlipCardBack(secondCard);

            // Cards have been flipped back, no need to clear the list as we already removed these cards
            // This allows other cards that might have been clicked to remain in the list
        }

        private void OnGameWon()
        {
            Debug.Log("Game completed! Starting new game...");
            onGameWon?.Invoke();
            // Optional: Add a delay before starting a new game
            StartCoroutine(RestartGameAfterDelay(3.0f));
        }

        private IEnumerator RestartGameAfterDelay(float delay)
        {
            // Countdown from delay seconds to 1
            for (int countdown = (int)delay; countdown > 0; countdown--)
            {
                onGameStartingIn?.Invoke(countdown);
                yield return new WaitForSeconds(1.0f);
            }

            _matchedCards.Clear();
            _flippedCards.Clear();
            onGameRestarted?.Invoke();
            InitGame((int)_currentLayout.x, (int)_currentLayout.y);
        }

        private void ReplayGame()
        {
            Debug.Log("Replaying game with same layout...");
            _matchedCards.Clear();
            _flippedCards.Clear();
            InitGame((int)_currentLayout.x, (int)_currentLayout.y);
        }

        private void OnScoreUpdate()
        {
            float currentTime = Time.time;
            int basePoints = GameDataManager.Instance.GetGameData().BaseMatchPoints;
            int pointsToAdd = basePoints;

            // Check for combo
            if (!_isFirstMatch)
            {
                float timeSinceLastMatch = currentTime - _lastMatchTime;
                float comboWindow = GameDataManager.Instance.GetGameData().ComboTimeWindow;

                if (timeSinceLastMatch <= comboWindow)
                {
                    // Player got a combo!
                    _comboLevel = Mathf.Min(_comboLevel + 1, GameDataManager.Instance.GetGameData().MaxComboLevel);
                    int comboMultiplier = GameDataManager.Instance.GetGameData().ComboMultiplier;
                    pointsToAdd = basePoints + (basePoints * comboMultiplier * _comboLevel);

                    Debug.Log($"COMBO x{_comboLevel}! +{pointsToAdd} points (Time since last match: {timeSinceLastMatch:F1}s)");
                }
                else
                {
                    // Combo broken
                    _comboLevel = 0;
                    onComboTimerLeft?.Invoke(0f); // Hide timer UI
                    Debug.Log($"Combo broken. Time since last match: {timeSinceLastMatch:F1}s");
                }
            }
            else
            {
                _isFirstMatch = false;
                Debug.Log($"First match! +{pointsToAdd} points");
            }

            _currentScore += pointsToAdd;
            _lastMatchTime = currentTime;

            onScoreUpdated?.Invoke(_currentScore);

            // Start/restart combo timer
            StartComboTimer();
        }

        private void StartComboTimer()
        {
            // Stop existing timer if running
            if (_comboTimerCoroutine != null)
            {
                StopCoroutine(_comboTimerCoroutine);
            }

            // Always start timer on every match - it restarts each time
            _comboTimerCoroutine = StartCoroutine(ComboTimerCoroutine());
        }

        private IEnumerator ComboTimerCoroutine()
        {
            float comboWindow = GameDataManager.Instance.GetGameData().ComboTimeWindow;
            Debug.Log($"GameManager: Starting combo timer coroutine - window: {comboWindow}s, current combo level: {_comboLevel}");

            while (Time.time - _lastMatchTime < comboWindow)
            {
                float timeLeft = comboWindow - (Time.time - _lastMatchTime);
                float fillAmount = timeLeft / comboWindow;
                onComboTimerLeft?.Invoke(fillAmount);
                yield return null;
            }

            // Timer expired - combo opportunity lost
            if (_comboLevel > 0)
            {
                Debug.Log($"Combo timer expired! Lost combo level {_comboLevel}");
                _comboLevel = 0;
            }
            else
            {
                Debug.Log("Combo opportunity timer expired - no combo achieved");
            }
            onComboTimerLeft?.Invoke(0f); // Send 0 to indicate timer expired

            _comboTimerCoroutine = null;
        }

        private void OnResetScore()
        {
            _currentScore = 0;
            _comboLevel = 0;
            _isFirstMatch = true;

            // Stop combo timer
            if (_comboTimerCoroutine != null)
            {
                StopCoroutine(_comboTimerCoroutine);
                _comboTimerCoroutine = null;
            }

            onScoreUpdated?.Invoke(_currentScore);
            onComboTimerLeft?.Invoke(0f); // Hide timer UI
        }

        [ContextMenu("Increase Score")]
        private void IncreaseScore()
        {
            _currentScore += 1;
            onScoreUpdated?.Invoke(_currentScore);
        }

        [ContextMenu("Save Game")]
        private void SaveGameDebug()
        {
            SaveGameState();
        }

        [ContextMenu("Load Game")]
        private void LoadGameDebug()
        {
            LoadGameState();
        }

        [ContextMenu("Delete Save")]
        private void DeleteSaveDebug()
        {
            DeleteSavedGame();
        }

        private void ResumeGame()
        {
            LoadGameState();
        }

        public void SaveGameState()
        {
            SaveData saveData = new SaveData
            {
                rows = (int)_currentLayout.x,
                columns = (int)_currentLayout.y,
                currentScore = _currentScore,
                lastMatchTime = _lastMatchTime,
                comboLevel = _comboLevel,
                isFirstMatch = _isFirstMatch,
                flippedCardIds = new List<int>(_flippedCards),
                matchedCardIds = new List<int>(_matchedCards)
            };

            // Save card data
            foreach (Card card in _cards)
            {
                if (card != null)
                {
                    bool isFlipped = _flippedCards.Contains(card.Id);
                    bool isMatched = _matchedCards.Contains(card.Id);
                    
                    // Get card color from the Card component
                    Color cardColor = card.CardColor;
                    
                    CardData cardData = new CardData(card.Id, card.Value, cardColor, isFlipped, isMatched);
                    saveData.cards.Add(cardData);
                }
            }

            // Use GameDataManager to save
            GameDataManager.Instance.SaveGameState(saveData);
        }

        public void LoadGameState()
        {
            // Use GameDataManager to load
            SaveData saveData = GameDataManager.Instance.LoadGameState();
            
            if (saveData == null)
            {
                return;
            }

            // Clear existing game state
            ResetCards();

            // Restore game state
            _currentLayout = new Vector2(saveData.rows, saveData.columns);
            _currentScore = saveData.currentScore;
            _lastMatchTime = saveData.lastMatchTime;
            _comboLevel = saveData.comboLevel;
            _isFirstMatch = saveData.isFirstMatch;
            _flippedCards = new List<int>(saveData.flippedCardIds);
            _matchedCards = new List<int>(saveData.matchedCardIds);

            // Get layout info
            var layoutInfo = GetLayoutInfo(saveData.rows, saveData.columns);

            // Recreate cards from saved data
            foreach (CardData cardData in saveData.cards)
            {
                GameObject cardObject = Instantiate(CardPrefab, CardsParent);
                Card cardComponent = cardObject.GetComponent<Card>();

                // Calculate position based on card ID and layout
                int row = cardData.id / layoutInfo.columns;
                int col = cardData.id % layoutInfo.columns;
                float posX = layoutInfo.startX + col * (layoutInfo.cardSize.x + layoutInfo.padding);
                float posY = layoutInfo.startY - row * (layoutInfo.cardSize.y + layoutInfo.padding);
                Vector2 cardPosition = new Vector2(posX, posY);

                cardComponent.SetProperties(cardData.id, cardData.value, cardData.GetColor(), 
                    cardPosition, layoutInfo.cardSize, OnCardFlipped);

                // Restore card state
                if (cardData.isMatched)
                {
                    // Card should stay flipped and show as matched (scale 0 to indicate disappeared)
                    cardComponent.SetToFrontSide();
                    cardComponent.transform.localScale = Vector3.zero;
                }
                else if (cardData.isFlipped)
                {
                    // Card should be flipped but not matched
                    cardComponent.SetToFrontSide();
                }

                _cards.Add(cardComponent);
            }

            // Update UI
            onScoreUpdated?.Invoke(_currentScore);

            // Restore combo timer if needed
            if (_comboLevel > 0 && !_isFirstMatch)
            {
                StartComboTimer();
            }
        }

        public bool HasSavedGame()
        {
            return GameDataManager.Instance.HasSavedGame();
        }

        public void DeleteSavedGame()
        {
            GameDataManager.Instance.DeleteSavedGame();
        }
    }
}
