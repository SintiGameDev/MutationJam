using System;
using UnityEngine;

// Zentrale Punkteverwaltung. Per Singleton aus jedem Skript erreichbar:
//   ScoreManager.Instance.FoodEingesammelt();
//   ScoreManager.Instance.MatchAufgeloest();
//   ScoreManager.Instance.PunkteHinzufuegen(25);
//
// Die UI haengt sich ueber das OnScoreChanged-Event dran (siehe UIManager),
// damit der ScoreManager selbst nichts ueber die Anzeige wissen muss.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Punkte-Einstellungen")]
    [Tooltip("Punkte pro eingesammeltem Food.")]
    public int punkteProFood = 10;

    [Tooltip("Bonuspunkte, wenn ein 3er-Match aufgeloest wird.")]
    public int punkteProMatch = 50;

    [Header("Highscore")]
    [Tooltip("Highscore zwischen Sitzungen per PlayerPrefs speichern.")]
    public bool highscoreSpeichern = true;

    private const string HighscoreKey = "snake_highscore";

    // Aktueller Punktestand. Nur lesbar von aussen.
    public int AktuellerScore { get; private set; }
    public int Highscore { get; private set; }

    // Events: UI (oder andere Systeme) koennen sich hier registrieren.
    public event Action<int> OnScoreChanged;
    public event Action<int> OnHighscoreChanged;

    private void Awake()
    {
        // Sicherstellen, dass es nur einen ScoreManager gibt
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Highscore = highscoreSpeichern ? PlayerPrefs.GetInt(HighscoreKey, 0) : 0;
    }

    // -------- Hilfsmethoden zum Aufrufen aus anderen Skripten --------

    // Standard: ein Food wurde verschluckt.
    public void FoodEingesammelt()
    {
        PunkteHinzufuegen(punkteProFood);
    }

    // Variante mit Typ – falls Nahrungstyp spaeter eigene Punktwerte bekommt,
    // kannst du hier typ-abhaengig vergeben. Vorerst Standardwert.
    public void FoodEingesammelt(Nahrungstyp typ)
    {
        PunkteHinzufuegen(punkteProFood);
    }

    // Ein 3er-Match wurde aufgeloest.
    public void MatchAufgeloest()
    {
        PunkteHinzufuegen(punkteProMatch);
    }

    // Beliebige Punkte addieren (auch negativ moeglich – wird bei 0 gekappt).
    public void PunkteHinzufuegen(int punkte)
    {
        SetzeScore(AktuellerScore + punkte);
    }

    // Score direkt auf einen Wert setzen.
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

    // Score auf 0 zuruecksetzen (Highscore bleibt erhalten).
    public void Zuruecksetzen()
    {
        SetzeScore(0);
    }
}