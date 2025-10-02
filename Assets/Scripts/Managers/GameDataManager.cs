using UnityEngine;
using System.Collections.Generic;
using CMDemo.Components;

namespace CMDemo.Managers
{
    [System.Serializable]
    public class CardData
    {
        public int id;
        public string value;
        public float colorR, colorG, colorB, colorA;
        public bool isFlipped;
        public bool isMatched;

        public CardData(int cardId, string cardValue, Color cardColor, bool flipped, bool matched)
        {
            id = cardId;
            value = cardValue;
            colorR = cardColor.r;
            colorG = cardColor.g;
            colorB = cardColor.b;
            colorA = cardColor.a;
            isFlipped = flipped;
            isMatched = matched;
        }

        public Color GetColor()
        {
            return new Color(colorR, colorG, colorB, colorA);
        }
    }

    [System.Serializable]
    public class SaveData
    {
        public int rows;
        public int columns;
        public int currentScore;
        public List<CardData> cards;
        public float lastMatchTime;
        public int comboLevel;
        public bool isFirstMatch;
        public List<int> flippedCardIds;
        public List<int> matchedCardIds;

        public SaveData()
        {
            cards = new List<CardData>();
            flippedCardIds = new List<int>();
            matchedCardIds = new List<int>();
        }
    }

    public class GameDataManager : MonoBehaviour
    {
        public static GameDataManager Instance { get; private set; }

        [SerializeField] private GameData gameData;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameData GetGameData()
        {
            return gameData;
        }

        // Convenience methods for commonly accessed data
        public int GetMaxRows() => gameData.MaximumRows;
        public int GetMaxColumns() => gameData.MaximumColumns;
        public int GetMinRows() => gameData.MinimumRows;
        public int GetMinColumns() => gameData.MinimumColumns;

        // Card data access methods
        public List<string> GetCardSymbols() => gameData.cardSymbols;
        public List<Color> GetCardColors() => gameData.cardColors;

        public string GetSymbol(int index) => gameData.cardSymbols[index % gameData.cardSymbols.Count];
        public Color GetColor(int index) => gameData.cardColors[index % gameData.cardColors.Count];

        private const string SAVE_KEY = "CMDemo_SaveData";
        
        public bool HasSavedGame()
        {
            return PlayerPrefs.HasKey(SAVE_KEY);
        }

        public void SaveGameState(SaveData saveData)
        {
            try
            {
                // Serialize to JSON
                string jsonData = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(SAVE_KEY, jsonData);
                PlayerPrefs.Save();

                Debug.Log("Game state saved successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save game state: {e.Message}");
            }
        }

        public SaveData LoadGameState()
        {
            try
            {
                if (!HasSavedGame())
                {
                    Debug.LogWarning("No saved game found!");
                    return null;
                }

                string jsonData = PlayerPrefs.GetString(SAVE_KEY);
                SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);

                if (saveData == null)
                {
                    Debug.LogError("Failed to deserialize save data!");
                    return null;
                }

                Debug.Log($"Game state loaded successfully! Score: {saveData.currentScore}, Cards: {saveData.cards.Count}");
                return saveData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load game state: {e.Message}");
                return null;
            }
        }

        public void DeleteSavedGame()
        {
            if (HasSavedGame())
            {
                PlayerPrefs.DeleteKey(SAVE_KEY);
                PlayerPrefs.Save();
                Debug.Log("Saved game deleted!");
            }
        }
    }
}
