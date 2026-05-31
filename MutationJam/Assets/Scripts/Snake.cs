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

    [Header("Tower-Einstellungen")]
    [Tooltip("Wird an Segmente ohne eigene TurmKonfiguration uebergeben (z.B. Startsegmente)")]
    public GameObject standardTurmPrefab;

    [Header("Stufen-Anzeige")]
    [Tooltip("Welt-Badge (Prefab mit TextMeshPro) das die Mutationsstufe zeigt. " +
             "Leer lassen = keine Anzeige.")]
    public GameObject stufenAnzeigePrefab;
    [Tooltip("Welt-Offset relativ zum Segment. Bleibt im Weltraum, dreht/squasht NICHT mit. " +
             "Tipp: nach unten und leicht zur Kamera, damit der Turm in der Mitte frei bleibt.")]
    public Vector3 stufenAnzeigeOffset = new Vector3(0f, -0.5f, -0.1f);
    [Tooltip("Ab welcher Stufe die Anzeige erscheint. 1 = immer, 2 = erst ab erster Mutation.")]
    public int stufenAnzeigeAbStufe = 2;
    [Tooltip("Screen Space - Overlay Canvas, unter dem die Stufen-Badges erzeugt werden. " +
             "Pflicht, sonst rendert das UI-Badge nicht.")]
    public Canvas stufenAnzeigeCanvas;

    [Header("Juice Einstellungen (LeanTween)")]
    public LeanTweenType moveEaseType = LeanTweenType.linear;
    public LeanTweenType segmentEaseType = LeanTweenType.easeOutQuad;
    public float squashAmount = 1.25f;
    public float RaupenFaktor = 0.05f;

    private readonly List<Transform> segments = new List<Transform>();
    // Spiegelt segments 1:1 – speichert die grid-exakten Logik-Positionen.
    // Tweens nutzen diese als Ziel, die Transform.position ist nur Optik.
    private readonly List<Vector3> logikPositionen = new List<Vector3>();
    // Vorherige Logik-Positionen – fuer Richtungsberechnung pro Segment
    private readonly List<Vector3> vorigeLogikPos = new List<Vector3>();
    // Vollstaendige Pfadhistorie des Kopfes – Segmente lesen ihre
    // Position bei Index (segmentIndex * segmentAbstand) daraus
    private readonly List<Vector3> pfadHistorie = new List<Vector3>();
    private Vector2Int input;
    private float nextUpdate;
    // Vorige Kopf-Logikposition – fuer den visuellen Move-Tween
    private Vector3 kopfVorigePos;
    [Tooltip("Basis-Scale des Kopfes (wird fuer Squash-Rueckgabe genutzt).")]
    public Vector3 basisScale = Vector3.one;
    [Tooltip("Scale der Koerper-Segmente (unabhaengig vom Kopf).")]
    public Vector3 segmentScale = Vector3.one;
    [Tooltip("Abstand zwischen Segmenten in Grid-Einheiten. 1 = direkt aneinander, 2 = eine Luecke dazwischen.")]
    [Range(0.5f, 4f)]
    public float segmentAbstand = 1f;

    public List<Transform> Segments => segments;

    private void Start()
    {
        // Kopf immer ueber allen Segmenten rendern
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = 10;

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

        float stepDuration = 1f / (speed * speedMultiplier);

        // --- Listen synchron halten ---
        while (logikPositionen.Count < segments.Count)
        {
            Vector3 pos = segments[logikPositionen.Count].position;
            logikPositionen.Add(pos);
            vorigeLogikPos.Add(pos);
        }
        while (logikPositionen.Count > segments.Count)
        {
            logikPositionen.RemoveAt(logikPositionen.Count - 1);
            vorigeLogikPos.RemoveAt(vorigeLogikPos.Count - 1);
        }
        // vorigeLogikPos ebenfalls auf segments-Laenge bringen
        while (vorigeLogikPos.Count < logikPositionen.Count)
            vorigeLogikPos.Add(logikPositionen[vorigeLogikPos.Count]);
        while (vorigeLogikPos.Count > logikPositionen.Count)
            vorigeLogikPos.RemoveAt(vorigeLogikPos.Count - 1);

        // --- Vorige Positionen sichern (vor dem Vorschieben) ---
        for (int i = 0; i < logikPositionen.Count; i++)
        {
            vorigeLogikPos[i] = logikPositionen[i];
        }

        // --- Kopf eine Zelle vorschieben ---
        int x = Mathf.RoundToInt(logikPositionen[0].x) + direction.x;
        int y = Mathf.RoundToInt(logikPositionen[0].y) + direction.y;
        logikPositionen[0] = new Vector3(x, y, 0f);

        // Kopf-Logikposition setzen (fuer Kollisionserkennung via Occupies/OnTrigger)
        // transform.position bleibt NICHT sofort gesetzt – stattdessen tweenen wir
        // den Root von der vorigen zur neuen Logikposition, genau wie die Segmente.
        // Die Kollision laeuft ueber den BoxCollider2D, der dem Transform folgt.
        Vector3 kopfZiel = logikPositionen[0];
        LeanTween.cancel(gameObject);
        transform.position = kopfZiel;          // sofort fuer Kollision
        // Optisch: von voriger Position zur neuen gleiten
        if (kopfVorigePos != kopfZiel)
        {
            transform.position = kopfVorigePos; // kurz zurueck fuer den Tween-Start
            LeanTween.move(gameObject, kopfZiel, stepDuration * 0.9f)
                .setEase(moveEaseType)
                .setUseEstimatedTime(true)
                .setOnComplete(() => { if (this != null) transform.position = kopfZiel; });
        }
        kopfVorigePos = kopfZiel;

        // --- Pfadhistorie: neue Kopfposition vorne einfuegen ---
        pfadHistorie.Insert(0, logikPositionen[0]);

        // Historie auf benoedigte Laenge kuerzen:
        // letztes Segment braucht Index (Count-1) * segmentAbstand
        int maxHistorie = Mathf.CeilToInt((segments.Count - 1) * segmentAbstand) + 2;
        while (pfadHistorie.Count > maxHistorie)
            pfadHistorie.RemoveAt(pfadHistorie.Count - 1);

        // --- Segment-Logikpositionen aus der Pfadhistorie lesen ---
        for (int i = 1; i < segments.Count; i++)
        {
            float zielIdx = i * segmentAbstand;
            int idxA = Mathf.FloorToInt(zielIdx);
            int idxB = idxA + 1;
            float t = zielIdx - idxA;

            idxA = Mathf.Clamp(idxA, 0, pfadHistorie.Count - 1);
            idxB = Mathf.Clamp(idxB, 0, pfadHistorie.Count - 1);

            logikPositionen[i] = Vector3.Lerp(pfadHistorie[idxA], pfadHistorie[idxB], t);
        }

        // Kopf in Bewegungsrichtung drehen (Root dreht sich, Child folgt mit)
        Vector3 kopfDelta = new Vector3(direction.x, direction.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, RichtungsWinkel(kopfDelta));

        // --- Koerper-Segmente: Tween + richtungsabhaengiger Squash ---
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] == null) continue;

            GameObject segGO = segments[i].gameObject;
            Vector3 ziel = logikPositionen[i];

            // Bewegungsrichtung dieses Segments (von voriger zu neuer Logik-Position)
            Vector3 delta = logikPositionen[i] - vorigeLogikPos[i];
            bool bewegtSichHorizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);

            // Squash-Achsen abhaengig von Bewegungsrichtung:
            // horizontal bewegt → in X strecken, in Y quetschen (und umgekehrt)
            // Squash relativ zur basisScale – damit skalierte Segmente korrekt zurueckkehren
            float sx = bewegtSichHorizontal ? squashAmount : (2f - squashAmount);
            float sy = bewegtSichHorizontal ? (2f - squashAmount) : squashAmount;
            float squashDauer = stepDuration * 0.4f;

            LeanTween.cancel(segGO);
            LeanTween.move(segGO, ziel, stepDuration * 0.85f)
                .setEase(segmentEaseType)
                .setUseEstimatedTime(true);

            // Segment in seine Bewegungsrichtung drehen
            if (delta.sqrMagnitude > 0.0001f)
                segments[i].rotation = Quaternion.Euler(0f, 0f, RichtungsWinkel(delta));

            GameObject cap = segGO;
            Vector3 squashZiel = new Vector3(segmentScale.x * sx, segmentScale.y * sy, segmentScale.z);
            LeanTween.scale(cap, squashZiel, squashDauer)
                .setEase(LeanTweenType.easeOutQuad)
                .setUseEstimatedTime(true)
                .setOnComplete(() =>
                {
                    if (cap != null)
                        LeanTween.scale(cap, segmentScale, squashDauer)
                            .setEase(LeanTweenType.easeInOutQuad)
                            .setUseEstimatedTime(true);
                });
        }

        // --- Kopf-Squash (Richtung aus direction) ---
        bool kopfHorizontal = direction.x != 0;
        float kopfScaleX = kopfHorizontal ? squashAmount : (2f - squashAmount);
        float kopfScaleY = kopfHorizontal ? (2f - squashAmount) : squashAmount;

        Vector3 kopfSquashZiel = new Vector3(basisScale.x * kopfScaleX, basisScale.y * kopfScaleY, basisScale.z);
        LeanTween.scale(gameObject, kopfSquashZiel, stepDuration * 0.25f)
            .setUseEstimatedTime(true)
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, basisScale, stepDuration * 0.75f)
                    .setUseEstimatedTime(true);
            });

        nextUpdate = Time.fixedTime + stepDuration;
    }

    public void Grow(Nahrungstyp typ = null, int stufe = 1)
    {
        Transform segment = Instantiate(segmentPrefab);

        // Logik-Position = aktuelle Schwanzposition (grid-exakt)
        Vector3 schwanzPos = logikPositionen.Count > 0
            ? logikPositionen[logikPositionen.Count - 1]
            : new Vector3(
                Mathf.RoundToInt(segments[segments.Count - 1].position.x),
                Mathf.RoundToInt(segments[segments.Count - 1].position.y),
                0f);

        segment.position = schwanzPos;
        logikPositionen.Add(schwanzPos);

        // Pop-In: von 0 auf basisScale mit leichtem Overshoot
        segment.localScale = Vector3.zero;
        LeanTween.scale(segment.gameObject, segmentScale, 0.25f)
            .setEase(LeanTweenType.easeOutBack)
            .setUseEstimatedTime(true);

        SnakeSegment snakeSegment = segment.gameObject.AddComponent<SnakeSegment>();
        snakeSegment.StandardTurmPrefab  = standardTurmPrefab;
        snakeSegment.StufenAnzeigePrefab = stufenAnzeigePrefab;
        snakeSegment.StufenAnzeigeOffset = stufenAnzeigeOffset;
        snakeSegment.AnzeigeAbStufe      = stufenAnzeigeAbStufe;
        snakeSegment.StufenAnzeigeCanvas = stufenAnzeigeCanvas;
        snakeSegment.SetzeTyp(typ, stufe);
        snakeSegment.OnSegmentGestorben += SegmentGestorben;

        // Segment unter dem Kopf rendern; spaetere Segmente unter frueheren
        foreach (SpriteRenderer sr in segment.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = Mathf.Max(0, 9 - segments.Count);

        segments.Add(segment);
    }

    // Wird vom SnakeSegment-Event aufgerufen wenn ein Segment stirbt
    public void SegmentGestorben(SnakeSegment segment)
    {
        int idx = segments.IndexOf(segment.transform);
        if (idx >= 0)
        {
            segments.RemoveAt(idx);
            if (idx < logikPositionen.Count)
                logikPositionen.RemoveAt(idx);
            if (idx < vorigeLogikPos.Count)
                vorigeLogikPos.RemoveAt(idx);
        }
    }

    public void EntferneSegmente(int startIndex, int anzahl)
    {
        for (int i = startIndex; i < startIndex + anzahl; i++)
        {
            if (segments[i] != null)
            {
                LeanTween.cancel(segments[i].gameObject);
                StartCoroutine(PopOutUndZerstoere(segments[i].gameObject));
            }
        }

        segments.RemoveRange(startIndex, anzahl);

        // Hilfslisten synchron halten
        int listenAnzahl = Mathf.Min(anzahl, logikPositionen.Count - startIndex);
        if (listenAnzahl > 0)
        {
            logikPositionen.RemoveRange(startIndex, listenAnzahl);
            if (startIndex < vorigeLogikPos.Count)
                vorigeLogikPos.RemoveRange(startIndex,
                    Mathf.Min(listenAnzahl, vorigeLogikPos.Count - startIndex));
        }
    }

    // Entfernt mehrere, NICHT zusammenhaengende Segmente (z.B. ein Match aus
    // verstreuten Segmenten gleichen Typs + gleicher Stufe).
    // Index 0 (Kopf) wird nie entfernt. Luecken schliessen sich automatisch,
    // weil die verbleibenden Segmente ihre Position jeden Tick neu aus der
    // pfadHistorie lesen.
    public void EntferneSegmenteAnIndizes(List<int> indizes)
    {
        if (indizes == null || indizes.Count == 0) return;

        // Absteigend sortieren, damit die Indizes beim Entfernen gueltig bleiben
        List<int> sortiert = new List<int>(indizes);
        sortiert.Sort();
        sortiert.Reverse();

        foreach (int idx in sortiert)
        {
            // Kopf (0) und ungueltige Indizes ueberspringen
            if (idx <= 0 || idx >= segments.Count) continue;

            if (segments[idx] != null)
            {
                LeanTween.cancel(segments[idx].gameObject);
                StartCoroutine(PopOutUndZerstoere(segments[idx].gameObject));
            }

            segments.RemoveAt(idx);
            if (idx < logikPositionen.Count) logikPositionen.RemoveAt(idx);
            if (idx < vorigeLogikPos.Count)  vorigeLogikPos.RemoveAt(idx);
        }
    }

    private System.Collections.IEnumerator PopOutUndZerstoere(GameObject go)
    {
        float dauer = 0.15f;
        float elapsed = 0f;
        Vector3 start = go != null ? go.transform.localScale : Vector3.one;

        while (elapsed < dauer)
        {
            elapsed += Time.deltaTime;
            if (go == null) yield break;
            go.transform.localScale = Vector3.Lerp(start, Vector3.zero, elapsed / dauer);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    public void ResetState()
    {
        LeanTween.cancelAll();

        direction = Vector2Int.right;
        transform.position = Vector3.zero;
        transform.localScale = basisScale;
        kopfVorigePos = Vector3.zero;

        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                Destroy(segments[i].gameObject);
            }
        }

        segments.Clear();
        logikPositionen.Clear();
        vorigeLogikPos.Clear();
        pfadHistorie.Clear();

        segments.Add(transform);
        logikPositionen.Add(Vector3.zero);
        vorigeLogikPos.Add(Vector3.zero);

        for (int i = 0; i < initialSize - 1; i++)
        {
            Grow();
        }

        // pfadHistorie vorbelegen: genug Eintraege damit jedes Startsegment
        // seine korrekte Startposition lesen kann statt alle auf Position 0
        int benoetigt = Mathf.CeilToInt((segments.Count - 1) * segmentAbstand) + 2;
        pfadHistorie.Clear();
        for (int i = 0; i < benoetigt; i++)
            pfadHistorie.Add(Vector3.zero);
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
                segmentManager.PruefeUndZerkleinereKette();
            }

            if (food != null)
            {
                food.RandomizePosition();
            }
        }
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            int segmentIndex = segments.IndexOf(other.transform);

            // Eigenes Segment getroffen:
            // Index 1+2 ignorieren (Hals – streift den Kopf durch Squash-Optik).
            // Alle anderen eigenen Segmente ebenfalls ignorieren solange die Schlange
            // noch im Aufbau ist (z.B. Startsegmente liegen alle auf Position 0).
            if (segmentIndex > 0)
            {
                // Nur echte Selbstkollision zaehlt: Segment muss sich an einer
                // anderen Logik-Position befinden als der Kopf.
                if (segmentIndex <= 2) return;

                Vector3 kopfPos = logikPositionen.Count > 0 ? logikPositionen[0] : transform.position;
                Vector3 segPos = segmentIndex < logikPositionen.Count
                    ? logikPositionen[segmentIndex]
                    : other.transform.position;

                // Nur resetten wenn das Segment wirklich auf derselben Grid-Zelle sitzt
                if (Mathf.RoundToInt(kopfPos.x) != Mathf.RoundToInt(segPos.x) ||
                    Mathf.RoundToInt(kopfPos.y) != Mathf.RoundToInt(segPos.y))
                {
                    return;
                }
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

    // Berechnet Z-Winkel aus Bewegungsvektor (Sprite zeigt standardmaessig nach rechts = 0 Grad)
    private static float RichtungsWinkel(Vector3 delta)
    {
        if (delta.sqrMagnitude < 0.0001f) return 0f;
        return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
    }

    private void Traverse(Transform wall)
    {
        LeanTween.cancel(gameObject);
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