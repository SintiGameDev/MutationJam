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

    // Prueft solange auf 3er-Reihen, bis keine mehr gefunden werden (Kaskaden).
    public void PruefeKombos()
    {
        while (PruefeEinzelneKombo()) { }
    }

    // Rueckwaertskompatibilitaet falls Snake.OnTriggerEnter2D noch den alten Namen nutzt
    public void PruefeUndZerkleinereKette() => PruefeKombos();

    private bool PruefeEinzelneKombo()
    {
        IReadOnlyList<Transform> segmente = snake.Segmente;

        // Mindestens Kopf + 3 Koerper-Segmente noetig
        if (segmente.Count < 4) return false;

        Nahrungstyp runTyp = null;
        int runStart = -1;
        int runLaenge = 0;

        // Index 0 ist der Kopf (kein SnakeSegment) → ab Index 1 starten
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
                    Debug.Log($"Match-3 aufgeloest ab Index {runStart}, Laenge {runLaenge}. Neue Segmentanzahl: {snake.Segmente.Count}");
                    return true;
                }

                runTyp = typ;
                runStart = i;
                runLaenge = 1;
            }
        }

        // Letzten laufenden Block noch pruefen (Reihe bis ans Schwanzende)
        if (runLaenge >= 3)
        {
            snake.EntferneSegmente(runStart, runLaenge);
            Debug.Log($"Match-3 aufgeloest ab Index {runStart}, Laenge {runLaenge}. Neue Segmentanzahl: {snake.Segmente.Count}");
            return true;
        }

        return false;
    }
}