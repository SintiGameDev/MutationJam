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

    public void PruefeUndZerkleinereKette()
    {
        List<Transform> segments = snake.Segments;

        // Kopf (0) + mindestens 3 Segmente (1, 2, 3) werden benötigt
        if (segments.Count < 4) return;

        // Wir prüfen von vorne nach hinten (ab Index 1, da Index 0 der Kopf ist)
        // Wir stoppen bei Count - 2, da wir Dreiergruppen prüfen (i, i+1, i+2)
        for (int i = 1; i < segments.Count - 2; i++)
        {
            SnakeSegment seg1 = segments[i].GetComponent<SnakeSegment>();
            SnakeSegment seg2 = segments[i + 1].GetComponent<SnakeSegment>();
            SnakeSegment seg3 = segments[i + 2].GetComponent<SnakeSegment>();

            // WICHTIG: Typen dürfen NICHT null sein (Startsegmente ignorieren!)
            if (seg1 != null && seg2 != null && seg3 != null &&
                seg1.Typ != null && seg2.Typ != null && seg3.Typ != null)
            {
                // Match-3 Logik über die Bezeichnung des Typs
                if (seg1.Typ.bezeichnung == seg2.Typ.bezeichnung &&
                    seg2.Typ.bezeichnung == seg3.Typ.bezeichnung)
                {
                    // Wir löschen die Gruppe ab Index i bis i + 2
                    ZerstoereSegmenteUndSchliesseLuecke(i, i + 2);

                    // Wir brechen nach einem Match ab, da sich die Liste verändert hat.
                    // Weitere Matches werden beim nächsten Schritt/Fressen aufgelöst.
                    break;
                }
            }
        }
    }

    private void ZerstoereSegmenteUndSchliesseLuecke(int startIndex, int endIndex)
    {
        List<Transform> segments = snake.Segments;

        // 1. Zu zerstörende Segmente zwischenspeichern
        List<Transform> zuZerstoeren = new List<Transform>();
        for (int i = startIndex; i <= endIndex; i++)
        {
            zuZerstoeren.Add(segments[i]);
        }

        // 2. Die Position des vordersten gelöschten Segments merken.
        // Hierhin rückt das verbleibende Schwanzstück vor.
        Vector3 zielPosition = segments[startIndex].position;

        // 3. Aus der internen Schlangen-Liste entfernen und In-Game zerstören
        foreach (Transform seg in zuZerstoeren)
        {
            // LeanTween-Instanzen auf dem sterbenden Objekt sicherheitshalber löschen
            LeanTween.cancel(seg.gameObject);
            segments.Remove(seg);
            Destroy(seg.gameObject);
        }

        // 4. LÜCKE SCHLIESSEN:
        // Alle verbleibenden Segmente hinter der gelöschten Gruppe aufrücken lassen
        int ritz = startIndex;
        for (int i = ritz; i < segments.Count; i++)
        {
            // Vorhandene Tweens auf den nachrückenden Segmenten stoppen, damit es nicht ruckelt
            LeanTween.cancel(segments[i].gameObject);

            if (i == ritz)
            {
                // Das erste Element nach der Lücke springt auf den freien Platz vor
                segments[i].position = zielPosition;
            }
            else
            {
                // Alle weiteren Segmente folgen direkt im Grid-Abstand auf ihren neuen Vordermann
                segments[i].position = segments[i - 1].position;
            }
        }

        Debug.Log($"Match gefunden! 3 Segmente entfernt. Neue Schlangenlänge: {segments.Count}");
    }

}