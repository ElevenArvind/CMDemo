using UnityEngine;
using System.Collections.Generic;

namespace CMDemo.Managers
{
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
    }
}
