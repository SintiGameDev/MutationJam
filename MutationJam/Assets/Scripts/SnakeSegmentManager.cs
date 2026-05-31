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

    public void PruefeUndZerkleinereKette() => PruefeKombos();

    public void PruefeKombos()
    {
        while (PruefeEinzelneKombo()) { }
    }

    private bool PruefeEinzelneKombo()
    {
        List<Transform> segmente = snake.Segments;

        if (segmente.Count < 4) return false;

        Nahrungstyp runTyp  = null;
        int runStart        = -1;
        int runLaenge       = 0;

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
                    EntferneSegmente(runStart, runLaenge);
                    Debug.Log($"Match-3 aufgeloest ab Index {runStart}, Laenge {runLaenge}. Verbleibend: {snake.Segments.Count}");
                    return true;
                }

                runTyp    = typ;
                runStart  = i;
                runLaenge = 1;
            }
        }

        if (runLaenge >= 3)
        {
            EntferneSegmente(runStart, runLaenge);
            Debug.Log($"Match-3 aufgeloest ab Index {runStart}, Laenge {runLaenge}. Verbleibend: {snake.Segments.Count}");
            return true;
        }

        return false;
    }

    private void EntferneSegmente(int startIndex, int anzahl)
    {
        // Snake kuemmert sich um logikPositionen/vorigeLogikPos-Synchronisierung
        // und spielt den PopOut-Effekt ab
        snake.EntferneSegmente(startIndex, anzahl);
    }
}
