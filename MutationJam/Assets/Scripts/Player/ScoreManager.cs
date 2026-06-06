using System;
using UnityEngine;
// Zentrale Punkteverwaltung. Per Singleton aus jedem Skript erreichbar:
//   ScoreManager.Instance.FoodEingesammelt();
//   ScoreManager.Instance.MatchAufgeloest();
//   ScoreManager.Instance.GegnerGetoetet(maxLeben);
//   ScoreManager.Instance.PunkteHinzufuegen(25);
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    [Header("Punkte-Einstellungen")]
    [Tooltip("Punkte pro eingesammeltem Food.")]
    public int punkteProFood = 10;
    [Tooltip("Bonuspunkte, wenn ein 3er-Match aufgeloest wird.")]
    public int punkteProMatch = 50;
    [Tooltip("Multiplikator fuer Kill-Punkte: Score += maxLeben * dieser Wert.")]
    public float killPunkteFaktor = 1f;
    [Header("Highscore")]
    [Tooltip("Highscore zwischen Sitzungen per PlayerPrefs speichern.")]
    public bool highscoreSpeichern = true;
    private const string HighscoreKey = "snake_highscore";
    public int AktuellerScore { get; private set; }
    public int Highscore { get; private set; }
    public event Action<int> OnScoreChanged;
    public event Action<int> OnHighscoreChanged;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Highscore = highscoreSpeichern ? PlayerPrefs.GetInt(HighscoreKey, 0) : 0;
    }
    // -------- Hilfsmethoden zum Aufrufen aus anderen Skripten --------
    public void FoodEingesammelt()
    {
        PunkteHinzufuegen(punkteProFood);
    }
    public void FoodEingesammelt(Nahrungstyp typ)
    {
        PunkteHinzufuegen(punkteProFood);
    }
    public void MatchAufgeloest()
    {
        PunkteHinzufuegen(punkteProMatch);
    }
    // Ein Gegner wurde getoetet: Score += maxLeben (mal Faktor).
    public void GegnerGetoetet(float maxLeben)
    {
        int punkte = Mathf.RoundToInt(maxLeben * killPunkteFaktor);
        PunkteHinzufuegen(punkte);
    }
    public void PunkteHinzufuegen(int punkte)
    {
        SetzeScore(AktuellerScore + punkte);
    }
    public void SetzeScore(int wert)
    {
        AktuellerScore = Mathf.Max(0, wert);
        OnScoreChanged?.Invoke(AktuellerScore);
        if (AktuellerScore > Highscore)
        {
            Highscore = AktuellerScore;
            if (highscoreSpeichern)
            {
                PlayerPrefs.SetInt(HighscoreKey, Highscore);
                PlayerPrefs.Save();
            }
            OnHighscoreChanged?.Invoke(Highscore);
        }
    }
    public void Zuruecksetzen()
    {
        SetzeScore(0);
    }
}