using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Snake))]
public class SnakeSegmentManager : MonoBehaviour
{
    private Snake snake;

    private void Awake()
    {
        snake = GetComponent<Snake>();
    }

    // Einstiegspunkt aus Snake.OnTriggerEnter2D (alter Name bleibt als Alias)
    public void PruefeUndZerkleinereKette() => PruefeKombos();
    public void PruefeKombos()
    {
        while (PruefeEinzelneKombo()) { }
    }

    private bool PruefeEinzelneKombo()
    {
        IReadOnlyList<Transform> segmente = snake.Segmente;

        // Mindestens Kopf + 3 Koerper-Segmente noetig
        if (segmente.Count < 4) return false;

        Nahrungstyp runTyp  = null;
        int runStart        = -1;
        int runLaenge       = 0;

        // Index 0 ist der Kopf (hat kein SnakeSegment) → ab Index 1 starten
        for (int i = 1; i < segmente.Count; i++)
        {
            Nahrungstyp typ = segmente[i].GetComponent<SnakeSegment>()?.Typ;

            if (typ != null && typ == runTyp)
            {
                runLaenge++;
            }
            else
            {
                if (runLaenge >= 3)
                {
                    snake.EntferneSegmente(runStart, runLaenge);
                    Debug.Log($"Match-3 aufgeloest ab Index {runStart}, Laenge {runLaenge}. Verbleibend: {snake.Segmente.Count}");
                    return true;
                }

                runTyp    = typ;
                runStart  = i;
                runLaenge = 1;
            }
        }

        // Letzten Block pruefen (Reihe bis ans Schwanzende)
        if (runLaenge >= 3)
        {
            snake.EntferneSegmente(runStart, runLaenge);
            Debug.Log($"Match-3 aufgeloest ab Index {runStart}, Laenge {runLaenge}. Verbleibend: {snake.Segmente.Count}");
            return true;
        }

        return false;
    }
}
