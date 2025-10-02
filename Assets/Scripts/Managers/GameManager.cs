using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CMDemo.Components;
using UnityEngine;

namespace CMDemo.Managers
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private RectTransform CardsParent;
        [SerializeField] private GameObject CardPrefab;
        [SerializeField] private RectTransform PlayArea;

        private List<Card> _cards = new List<Card>();

        public enum CardLayout
        {
            TwoxTwo,
            TwoxThree,
            FivexSix,
        }

        private CardLayout _currentLayout = CardLayout.FivexSix;
        void Awake()
        {
            InitSymbols();
        }

        private string[] _pairSymbols;
        private Color[] _pairColors;
        private void InitSymbols()
        {
            _pairSymbols = new string[]
            {
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O",
                "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                "★", "♠", "♣", "♥", "♦", "♪", "♫", "☀", "☂", "☃", "⚡", "✈", "⚽", "🎵", "🌟"
            };

            // Define colors for each pair
            _pairColors = new Color[]
            {
                Color.red, Color.blue, Color.green, Color.yellow, Color.magenta,
                Color.cyan, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f),
                new Color(1f, 0.75f, 0.8f), new Color(0.5f, 1f, 0.5f),
                new Color(1f, 1f, 0.5f), new Color(0.8f, 0.4f, 0.2f),
                new Color(0.2f, 0.8f, 0.8f), new Color(0.8f, 0.2f, 0.8f),
                new Color(0.4f, 0.4f, 0.8f)
            };
        }

        void Start()
        {
            InitCards(_currentLayout);
        }

        private void ResetCards()
        {
            foreach (var card in _cards)
            {
                Destroy(card.gameObject);
            }
            _cards.Clear();
        }

        private Dictionary<CardLayout, Vector2> _layoutDimensions = new Dictionary<CardLayout, Vector2>
        {
            { CardLayout.TwoxTwo, new Vector2(2, 2) },
            { CardLayout.TwoxThree, new Vector2(2, 3) },
            { CardLayout.FivexSix, new Vector2(5, 6) },
        };

        [ContextMenu("Set 2x2 Layout")]
        public void SetTwoByTwoLayout()
        {
            _currentLayout = CardLayout.TwoxTwo;
            InitCards(_currentLayout);
        }

        [ContextMenu("Set 2x3 Layout")]
        public void SetTwoByThreeLayout()
        {
            _currentLayout = CardLayout.TwoxThree;
            InitCards(_currentLayout);
        }

        [ContextMenu("Set 5x6 Layout")]
        public void SetFiveBySixLayout()
        {
            _currentLayout = CardLayout.FivexSix;
            InitCards(_currentLayout);
        }

        private void InitCards(CardLayout layout)
        {
            ResetCards();

            var layoutInfo = GetLayoutInfo(layout);
            var cardData = GenerateCardPairs(layoutInfo.totalCards);
            ShuffleCards(cardData.symbols, cardData.colors);
            CreateAndPositionCards(layoutInfo, cardData.symbols, cardData.colors);
        }

        private (int columns, int rows, int totalCards, Vector2 cardSize, float startX, float startY, float padding) GetLayoutInfo(CardLayout layout)
        {
            Vector2 layoutDimensions = _layoutDimensions[layout];
            int columns = (int)layoutDimensions.x;
            int rows = (int)layoutDimensions.y;
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

            // Add pairs of symbols and their corresponding colors
            for (int i = 0; i < pairCount; i++)
            {
                string symbol = _pairSymbols[i % _pairSymbols.Length];
                Color pairColor = _pairColors[i % _pairColors.Length];
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
                int randomIndex = Random.Range(i, symbols.Count);

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

        private List<int> _flippedCards = new List<int>();
        private List<int> _matchedCards = new List<int>();
        private List<int> _cardsBeingProcessed = new List<int>();
        private Queue<(int, int)> _pendingMatches = new Queue<(int, int)>();
        private bool _isProcessingQueue = false;

        private void OnCardFlipped(int cardId)
        {
            // Prevent flipping if card is already matched or already flipped
            if (_matchedCards.Contains(cardId) || _flippedCards.Contains(cardId))
                return;

            Debug.Log($"Card {cardId} flipped!");
            _flippedCards.Add(cardId);

            // Check for pairs and queue them for processing
            CheckForNewPairs();
            
            // Start processing queue if not already running
            if (!_isProcessingQueue && _pendingMatches.Count > 0)
            {
                StartCoroutine(ProcessMatchQueue());
            }
        }

        private void CheckForNewPairs()
        {
            // Process pairs from flipped cards (excluding already matched and being processed)
            var availableCards = _flippedCards.Where(id => 
                !_matchedCards.Contains(id) && 
                !_cardsBeingProcessed.Contains(id)).ToList();
            
            // Queue pairs for processing
            while (availableCards.Count >= 2)
            {
                int firstCard = availableCards[0];
                int secondCard = availableCards[1];
                
                _pendingMatches.Enqueue((firstCard, secondCard));
                
                // Mark these cards as being processed to prevent duplicate pairs
                _cardsBeingProcessed.Add(firstCard);
                _cardsBeingProcessed.Add(secondCard);
                
                availableCards.RemoveAt(0);
                availableCards.RemoveAt(0);
            }
        }

        private IEnumerator ProcessMatchQueue()
        {
            _isProcessingQueue = true;

            while (_pendingMatches.Count > 0)
            {
                var (firstCardId, secondCardId) = _pendingMatches.Dequeue();

                Card firstCard = _cards.Find(c => c.Id == firstCardId);
                Card secondCard = _cards.Find(c => c.Id == secondCardId);

                if (firstCard != null && secondCard != null)
                {
                    Debug.Log($"Checking match: Card {firstCardId} ({firstCard.Value}) vs Card {secondCardId} ({secondCard.Value})");

                    // Wait a moment to let players see both cards
                    yield return new WaitForSeconds(1.0f);

                    if (DoCardsMatch(firstCard, secondCard))
                    {
                        // Cards match - keep them flipped and mark as matched
                        _matchedCards.Add(firstCardId);
                        _matchedCards.Add(secondCardId);
                        Debug.Log($"Match found! Cards {firstCardId} and {secondCardId}");

                        // Play match found animations
                        firstCard.OnMatchFound();
                        secondCard.OnMatchFound();

                        // Remove matched cards from all lists
                        _flippedCards.Remove(firstCardId);
                        _flippedCards.Remove(secondCardId);
                        _cardsBeingProcessed.Remove(firstCardId);
                        _cardsBeingProcessed.Remove(secondCardId);

                        // Check if all cards are matched (game won)
                        if (_matchedCards.Count == _cards.Count)
                        {
                            Debug.Log("Congratulations! All cards matched!");
                            OnGameWon();
                            break; // Exit the loop as game is restarting
                        }
                    }
                    else
                    {
                        // Cards don't match - show mismatch animation then flip back
                        firstCard.OnMatchNotFound();
                        secondCard.OnMatchNotFound();
                        Debug.Log($"No match. Cards {firstCardId} and {secondCardId} will flip back after mismatch animation.");

                        // Wait for mismatch animation to complete before flipping back
                        StartCoroutine(FlipBackAfterMismatch(firstCard, secondCard, firstCardId, secondCardId));
                    }
                }

                // Small delay between processing matches for better UX
                yield return new WaitForSeconds(0.1f);
            }

            _isProcessingQueue = false;
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

            // Remove non-matching cards from all lists
            _flippedCards.Remove(firstCardId);
            _flippedCards.Remove(secondCardId);
            _cardsBeingProcessed.Remove(firstCardId);
            _cardsBeingProcessed.Remove(secondCardId);
        }

        private void OnGameWon()
        {
            Debug.Log("Game completed! Starting new game...");
            // Optional: Add a delay before starting a new game
            StartCoroutine(RestartGameAfterDelay(2.0f));
        }

        private IEnumerator RestartGameAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _matchedCards.Clear();
            _flippedCards.Clear();
            _cardsBeingProcessed.Clear();
            _pendingMatches.Clear();
            _isProcessingQueue = false;
            InitCards(_currentLayout);
        }
    }
}
