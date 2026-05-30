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
        // Nutzt snake.Segments (englisch) – so wie es in Snake.cs definiert ist
        List<Transform> segmente = snake.Segments;

        if (segmente.Count < 4) return false;

        Nahrungstyp runTyp = null;
        int runStart = -1;
        int runLaenge = 0;

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
                    EntferneSegmente(segmente, runStart, runLaenge);
                    Debug.Log($"Match-3 aufgeloest ab Index {runStart}, Laenge {runLaenge}. Verbleibend: {segmente.Count}");
                    return true;
                }

                runTyp = typ;
                runStart = i;
                runLaenge = 1;
            }
        }

        if (runLaenge >= 3)
        {
            EntferneSegmente(segmente, runStart, runLaenge);
            Debug.Log($"Match-3 aufgeloest ab Index {runStart}, Laenge {runLaenge}. Verbleibend: {segmente.Count}");
            return true;
        }

        return false;
    }

    // Entfernt Segmente direkt aus der Liste und zerstoert die GameObjects.
    // Kein EntferneSegmente() auf Snake noetig.
    private void EntferneSegmente(List<Transform> segmente, int startIndex, int anzahl)
    {
        for (int i = startIndex; i < startIndex + anzahl; i++)
        {
            if (segmente[i] != null)
            {
                LeanTween.cancel(segmente[i].gameObject);
                Destroy(segmente[i].gameObject);
            }
        }

        segmente.RemoveRange(startIndex, anzahl);
    }
}