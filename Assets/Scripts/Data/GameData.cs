using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData", order = 1)]
public class GameData : ScriptableObject
{
    [Header("Game Settings")]
    public int MinimumRows = 2;
    public int MinimumColumns = 2;

    public int MaximumRows = 6;
    public int MaximumColumns = 6;

    [Header("Scoring System")]
    public int BaseMatchPoints = 10;
    public float ComboTimeWindow = 3.0f; // Seconds to get combo
    public int ComboMultiplier = 2; // Multiplier for combo points
    public int MaxComboLevel = 5; // Maximum combo level

    [Header("Card Data")]
    public List<string> cardSymbols = new List<string>
    {
        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O",
        "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
        "★", "♠", "♣", "♥", "♦", "♪", "♫", "☀", "☂", "☃", "✈", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
    };
    
    public List<Color> cardColors = new List<Color>
    {
        Color.red, Color.blue, Color.green, Color.yellow, Color.magenta,
        Color.cyan, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f),
        new Color(1f, 0.75f, 0.8f), new Color(0.5f, 1f, 0.5f),
        new Color(1f, 1f, 0.5f), new Color(0.8f, 0.4f, 0.2f),
        new Color(0.2f, 0.8f, 0.8f), new Color(0.8f, 0.2f, 0.8f),
        new Color(0.4f, 0.4f, 0.8f)
    };
}
