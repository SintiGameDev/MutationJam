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

        // Kopf + 3 Segmente sind das absolute Minimum für ein Match
        if (segments.Count < 4) return;

        // Wir prüfen von hinten nach vorne
        for (int i = segments.Count - 1; i >= 3; i--)
        {
            SnakeSegment seg1 = segments[i].GetComponent<SnakeSegment>();
            SnakeSegment seg2 = segments[i - 1].GetComponent<SnakeSegment>();
            SnakeSegment seg3 = segments[i - 2].GetComponent<SnakeSegment>();

            // Sicherstellen, dass alle Segmente gültig sind und einen Nahrungstyp besitzen
            if (seg1 != null && seg2 != null && seg3 != null &&
                seg1.Typ != null && seg2.Typ != null && seg3.Typ != null)
            {
                // Entspricht Match-3 Logik über die Bezeichnung des Typs
                if (seg1.Typ.bezeichnung == seg2.Typ.bezeichnung &&
                    seg2.Typ.bezeichnung == seg3.Typ.bezeichnung)
                {
                    ZerstoereSegmenteUndSchliesseLuecke(i - 2, i);

                    // Nach einem Treffer brechen wir ab, da sich die Indizes verschieben.
                    // Folge-Matches werden beim nächsten Essen aufgelöst.
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
            segments.Remove(seg);
            Destroy(seg.gameObject);
        }

        // 4. LÜCKE SCHLIESSEN:
        // Alle Segmente hinter der gelöschten Dreiergruppe aufrücken lassen
        int ritz = startIndex;
        for (int i = ritz; i < segments.Count; i++)
        {
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