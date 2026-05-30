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

        if (segments.Count < 4) return;

        for (int i = 1; i < segments.Count - 2; i++)
        {
            SnakeSegment seg1 = segments[i].GetComponent<SnakeSegment>();
            SnakeSegment seg2 = segments[i + 1].GetComponent<SnakeSegment>();
            SnakeSegment seg3 = segments[i + 2].GetComponent<SnakeSegment>();

            if (seg1 != null && seg2 != null && seg3 != null &&
                seg1.Typ != null && seg2.Typ != null && seg3.Typ != null)
            {
                if (seg1.Typ.bezeichnung == seg2.Typ.bezeichnung &&
                    seg2.Typ.bezeichnung == seg3.Typ.bezeichnung)
                {
                    ZerstoereSegmenteUndSchliesseLuecke(i, i + 2);
                    break;
                }
            }
        }
    }

    private void ZerstoereSegmenteUndSchliesseLuecke(int startIndex, int endIndex)
    {
        List<Transform> segments = snake.Segments;
        List<Transform> zuZerstoeren = new List<Transform>();

        for (int i = startIndex; i <= endIndex; i++)
        {
            zuZerstoeren.Add(segments[i]);
        }

        // Strikte Rasterung der Zielkoordinate
        Vector3 zielPosition = new Vector3(
            Mathf.RoundToInt(segments[startIndex].position.x),
            Mathf.RoundToInt(segments[startIndex].position.y),
            0f
        );

        foreach (Transform seg in zuZerstoeren)
        {
            LeanTween.cancel(seg.gameObject);
            segments.Remove(seg);
            Destroy(seg.gameObject);
        }

        int ritz = startIndex;
        for (int i = ritz; i < segments.Count; i++)
        {
            LeanTween.cancel(segments[i].gameObject);

            if (i == ritz)
            {
                segments[i].position = zielPosition;
            }
            else
            {
                // Auch hier: Nachrückende Teile strikt auf das Grid zwingen
                segments[i].position = new Vector3(
                    Mathf.RoundToInt(segments[i - 1].position.x),
                    Mathf.RoundToInt(segments[i - 1].position.y),
                    0f
                );
            }
        }

        Debug.Log($"Match-3 aufgelöst! Neue Segmente: {segments.Count}");
    }
}