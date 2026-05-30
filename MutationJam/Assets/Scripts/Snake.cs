using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Snake : MonoBehaviour
{
    public Transform segmentPrefab;
    public Vector2Int direction = Vector2Int.right;
    public float speed = 20f;
    public float speedMultiplier = 1f;
    public int initialSize = 4;
    public bool moveThroughWalls = false;

    [Header("Juice-Einstellungen")]
    [Tooltip("Wie stark der Squash-Effekt ist (z.B. 1.3 = 30% breiter)")]
    public float squashBetrag = 1.3f;
    [Tooltip("Wie lange ein kompletter Squash-Zyklus dauert, als Anteil eines Schritt-Ticks (0..1)")]
    [Range(0.1f, 1f)]
    public float squashDauer = 0.6f;
    [Tooltip("Verzoegerung pro Segment-Index, damit die Welle sichtbar nachlaeuft")]
    public float raupenVerzoegerung = 0.04f;

    private readonly List<Transform> segments = new List<Transform>();
    private Vector2Int input;
    private float nextUpdate;

    // Liest die Segmentliste fuer den SnakeSegmentManager
    public IReadOnlyList<Transform> Segmente => segments;

    private void Start()
    {
        ResetState();
    }

    private void Update()
    {
        if (direction.x != 0f)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                input = Vector2Int.up;
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                input = Vector2Int.down;
            }
        }
        else if (direction.y != 0f)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                input = Vector2Int.right;
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                input = Vector2Int.left;
            }
        }
    }

    private void FixedUpdate()
    {
        if (Time.fixedTime < nextUpdate)
        {
            return;
        }

        if (input != Vector2Int.zero)
        {
            direction = input;
        }

        // --- Positionen sofort setzen (grid-exakt, kein Tween-Drift) ---
        for (int i = segments.Count - 1; i > 0; i--)
        {
            segments[i].position = segments[i - 1].position;
        }

        int x = Mathf.RoundToInt(transform.position.x) + direction.x;
        int y = Mathf.RoundToInt(transform.position.y) + direction.y;
        transform.position = new Vector2(x, y);

        float stepDauer = 1f / (speed * speedMultiplier);
        nextUpdate = Time.fixedTime + stepDauer;

        // --- Squash-Welle ueber alle Segmente starten ---
        SpielSquashWelle(stepDauer);
    }

    // Startet fuer jedes Segment eine eigene Coroutine mit gestaffeltem Delay.
    // Laeuft vollstaendig unabhaengig von FixedUpdate → kein Cancel-Problem.
    private void SpielSquashWelle(float stepDauer)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                StartCoroutine(SquashSegment(segments[i], i, stepDauer));
            }
        }
    }

    // Squasht ein einzelnes Segment: erst breit+flach, dann zurueck auf (1,1,1).
    private IEnumerator SquashSegment(Transform seg, int index, float stepDauer)
    {
        // Staffelung: spaetere Segmente starten etwas verzoegert
        float verzoegerung = index * raupenVerzoegerung;
        if (verzoegerung > 0f)
        {
            yield return new WaitForSeconds(verzoegerung);
        }

        if (seg == null) yield break;

        // Gesamtdauer der Animation (als Anteil des Schritt-Ticks, damit es
        // bei hoher Geschwindigkeit nicht ueber den naechsten Tick laeuft)
        float animDauer = stepDauer * squashDauer;
        float halbDauer = animDauer * 0.5f;

        // --- Phase 1: Squash rein (breit + flach) ---
        float elapsed = 0f;
        Vector3 startScale = seg.localScale;           // i.d.R. (1,1,1)
        Vector3 squashScale = new Vector3(squashBetrag, 1f / squashBetrag, 1f);

        while (elapsed < halbDauer)
        {
            elapsed += Time.deltaTime;
            if (seg == null) yield break;
            seg.localScale = Vector3.Lerp(startScale, squashScale, elapsed / halbDauer);
            yield return null;
        }

        if (seg == null) yield break;
        seg.localScale = squashScale;

        // --- Phase 2: Zurueck auf normal ---
        elapsed = 0f;
        while (elapsed < halbDauer)
        {
            elapsed += Time.deltaTime;
            if (seg == null) yield break;
            seg.localScale = Vector3.Lerp(squashScale, Vector3.one, elapsed / halbDauer);
            yield return null;
        }

        if (seg == null) yield break;
        seg.localScale = Vector3.one;
    }

    // Wachsen: neues Segment erscheint mit Pop-In-Animation
    public void Grow(Nahrungstyp typ = null)
    {
        Transform segment = Instantiate(segmentPrefab);
        segment.position = new Vector3(
            Mathf.RoundToInt(segments[segments.Count - 1].position.x),
            Mathf.RoundToInt(segments[segments.Count - 1].position.y),
            0f
        );

        SnakeSegment snakeSegment = segment.gameObject.AddComponent<SnakeSegment>();
        snakeSegment.SetzeTyp(typ);

        segments.Add(segment);

        // Pop-In: von 0 auf (1,1,1) skalieren
        StartCoroutine(PopIn(segment));
    }

    private IEnumerator PopIn(Transform seg)
    {
        float dauer = 0.2f;
        float elapsed = 0f;
        seg.localScale = Vector3.zero;

        // Overshoot-Kurve manuell: ueberschiesst kurz ueber 1 hinaus
        while (elapsed < dauer)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dauer;
            // Einfacher Overshoot ohne externe Library
            float skala = t < 0.7f
                ? Mathf.Lerp(0f, 1.2f, t / 0.7f)
                : Mathf.Lerp(1.2f, 1f, (t - 0.7f) / 0.3f);

            if (seg == null) yield break;
            seg.localScale = new Vector3(skala, skala, 1f);
            yield return null;
        }

        if (seg != null)
        {
            seg.localScale = Vector3.one;
        }
    }

    public void ResetState()
    {
        StopAllCoroutines();

        direction = Vector2Int.right;
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;

        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                Destroy(segments[i].gameObject);
            }
        }

        segments.Clear();
        segments.Add(transform);

        for (int i = 0; i < initialSize - 1; i++)
        {
            Grow();
        }
    }

    // Entfernt eine zusammenhaengende Reihe gleicher Segmente (fuer SnakeSegmentManager).
    // Aufgerufen mit dem Start-Index und der Anzahl der zu entfernenden Segmente.
    public void EntferneSegmente(int startIndex, int anzahl)
    {
        for (int i = startIndex; i < startIndex + anzahl; i++)
        {
            if (segments[i] != null)
            {
                StartCoroutine(PopOut(segments[i]));
            }
        }
        segments.RemoveRange(startIndex, anzahl);
    }

    private IEnumerator PopOut(Transform seg)
    {
        float dauer = 0.15f;
        float elapsed = 0f;
        Vector3 startScale = seg != null ? seg.localScale : Vector3.one;

        while (elapsed < dauer)
        {
            elapsed += Time.deltaTime;
            if (seg == null) yield break;
            seg.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / dauer);
            yield return null;
        }

        if (seg != null)
        {
            Destroy(seg.gameObject);
        }
    }

    public bool Occupies(int x, int y)
    {
        foreach (Transform segment in segments)
        {
            if (segment == null) continue;
            if (Mathf.RoundToInt(segment.position.x) == x &&
                Mathf.RoundToInt(segment.position.y) == y)
            {
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Food"))
        {
            Food food = other.GetComponent<Food>();
            Nahrungstyp typ = (food != null) ? food.AktuellerTyp : null;

            Grow(typ);

            SnakeSegmentManager segmentManager = GetComponent<SnakeSegmentManager>();
            if (segmentManager != null)
            {
                segmentManager.PruefeKombos();
            }

            if (food != null)
            {
                food.RandomizePosition();
            }
        }
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            int segmentIndex = segments.IndexOf(other.transform);
            if (segmentIndex > 0 && segmentIndex <= 2)
            {
                return;
            }
            ResetState();
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            if (moveThroughWalls)
            {
                Traverse(other.transform);
            }
            else
            {
                ResetState();
            }
        }
    }

    private void Traverse(Transform wall)
    {
        Vector3 position = transform.position;

        if (direction.x != 0f)
        {
            position.x = Mathf.RoundToInt(-wall.position.x + direction.x);
        }
        else if (direction.y != 0f)
        {
            position.y = Mathf.RoundToInt(-wall.position.y + direction.y);
        }

        transform.position = position;
    }
}