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

    [Header("Kopf-Gesundheit")]
    [Tooltip("Leben des Kopfes. Der Kopf kann ERST sterben, wenn keine anderen Segmente mehr existieren.")]
    public float kopfMaxHealth = 5f;
    [Tooltip("Farbe des Kopf-Aufblitzens. Fuer ein STAERKERES Leuchten den HDR-Intensitaetsregler " +
             "im Farbwaehler ueber 1 ziehen (wirkt mit Bloom/Post-Processing).")]
    [ColorUsage(true, true)]
    public Color kopfBlitzFarbe = Color.white;
    [Tooltip("Kuerzeste Aufleucht-Dauer des Kopfes (bei vollem Leben). Klein = schneller.")]
    public float kopfMinBlitzDauer = 0.04f;
    [Tooltip("Laengste Aufleucht-Dauer des Kopfes (bei wenig Leben). Gedeckelt auf 0,5 s.")]
    public float kopfMaxBlitzDauer = 0.18f;

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

    // Wird gefeuert, wenn die Schlange stirbt (Kopf, Wand oder Selbstkollision).
    // Die UI haengt sich hier dran, um den Death-Screen zu zeigen.
    public event System.Action OnGestorben;

    private readonly List<Transform> segments = new List<Transform>();
    private readonly List<Vector3> logikPositionen = new List<Vector3>();
    private readonly List<Vector3> vorigeLogikPos = new List<Vector3>();
    private readonly List<Vector3> pfadHistorie = new List<Vector3>();
    private Vector2Int input;
    private float nextUpdate;
    private Vector3 kopfVorigePos;
    [Tooltip("Basis-Scale des Kopfes (wird fuer Squash-Rueckgabe genutzt).")]
    public Vector3 basisScale = Vector3.one;
    [Tooltip("Scale der Koerper-Segmente (unabhaengig vom Kopf).")]
    public Vector3 segmentScale = Vector3.one;
    [Tooltip("Abstand zwischen Segmenten in Grid-Einheiten. 1 = direkt aneinander, 2 = eine Luecke dazwischen.")]
    [Range(0.5f, 4f)]
    public float segmentAbstand = 1f;

    public List<Transform> Segments => segments;

    public bool NurKopfUebrig => segments.Count <= 1;
    public float KopfHealth => kopfHealth;

    // --- Kopf-Gesundheit (Laufzeit) ---
    private float kopfHealth;
    private bool kopfStirbt = false;
    private bool istTot = false;
    private SpriteRenderer kopfSprite;
    private MeshRenderer kopfMesh;
    private Color kopfGrundFarbeSprite = Color.white;
    private Color kopfGrundFarbeMesh = Color.white;
    private Coroutine kopfBlitzCoroutine;

    private void Start()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = 10;

        ErmittleKopfRenderer();

        ResetState();
    }

    private void ErmittleKopfRenderer()
    {
        kopfSprite = GetComponent<SpriteRenderer>();
        if (kopfSprite == null)
        {
            kopfSprite = GetComponentInChildren<SpriteRenderer>(true);
        }
        if (kopfSprite != null)
        {
            kopfGrundFarbeSprite = kopfSprite.color;
        }

        kopfMesh = GetComponent<MeshRenderer>();
        if (kopfMesh == null)
        {
            kopfMesh = GetComponentInChildren<MeshRenderer>(true);
        }
        if (kopfMesh != null && kopfMesh.material != null)
        {
            kopfGrundFarbeMesh = kopfMesh.material.color;
        }
    }

    private void Update()
    {
        if (istTot) return;

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
        if (istTot) return;   // tote Schlange bewegt sich nicht mehr

        if (Time.fixedTime < nextUpdate)
        {
            return;
        }

        if (input != Vector2Int.zero)
        {
            direction = input;
        }

        float stepDuration = 1f / (speed * speedMultiplier);

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
        while (vorigeLogikPos.Count < logikPositionen.Count)
            vorigeLogikPos.Add(logikPositionen[vorigeLogikPos.Count]);
        while (vorigeLogikPos.Count > logikPositionen.Count)
            vorigeLogikPos.RemoveAt(vorigeLogikPos.Count - 1);

        for (int i = 0; i < logikPositionen.Count; i++)
        {
            vorigeLogikPos[i] = logikPositionen[i];
        }

        int x = Mathf.RoundToInt(logikPositionen[0].x) + direction.x;
        int y = Mathf.RoundToInt(logikPositionen[0].y) + direction.y;
        logikPositionen[0] = new Vector3(x, y, 0f);

        Vector3 kopfZiel = logikPositionen[0];
        LeanTween.cancel(gameObject);
        transform.position = kopfZiel;
        if (kopfVorigePos != kopfZiel)
        {
            transform.position = kopfVorigePos;
            LeanTween.move(gameObject, kopfZiel, stepDuration * 0.9f)
                .setEase(moveEaseType)
                .setUseEstimatedTime(true)
                .setOnComplete(() => { if (this != null) transform.position = kopfZiel; });
        }
        kopfVorigePos = kopfZiel;

        pfadHistorie.Insert(0, logikPositionen[0]);

        int maxHistorie = Mathf.CeilToInt((segments.Count - 1) * segmentAbstand) + 2;
        while (pfadHistorie.Count > maxHistorie)
            pfadHistorie.RemoveAt(pfadHistorie.Count - 1);

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

        Vector3 kopfDelta = new Vector3(direction.x, direction.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, RichtungsWinkel(kopfDelta));

        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] == null) continue;

            GameObject segGO = segments[i].gameObject;
            Vector3 ziel = logikPositionen[i];

            Vector3 delta = logikPositionen[i] - vorigeLogikPos[i];
            bool bewegtSichHorizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);

            float sx = bewegtSichHorizontal ? squashAmount : (2f - squashAmount);
            float sy = bewegtSichHorizontal ? (2f - squashAmount) : squashAmount;
            float squashDauer = stepDuration * 0.4f;

            LeanTween.cancel(segGO);
            LeanTween.move(segGO, ziel, stepDuration * 0.85f)
                .setEase(segmentEaseType)
                .setUseEstimatedTime(true);

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

        Vector3 schwanzPos = logikPositionen.Count > 0
            ? logikPositionen[logikPositionen.Count - 1]
            : new Vector3(
                Mathf.RoundToInt(segments[segments.Count - 1].position.x),
                Mathf.RoundToInt(segments[segments.Count - 1].position.y),
                0f);

        segment.position = schwanzPos;
        logikPositionen.Add(schwanzPos);

        segment.localScale = Vector3.zero;
        LeanTween.scale(segment.gameObject, segmentScale, 0.25f)
            .setEase(LeanTweenType.easeOutBack)
            .setUseEstimatedTime(true);

        SnakeSegment snakeSegment = segment.gameObject.AddComponent<SnakeSegment>();
        snakeSegment.StandardTurmPrefab = standardTurmPrefab;
        snakeSegment.StufenAnzeigePrefab = stufenAnzeigePrefab;
        snakeSegment.StufenAnzeigeOffset = stufenAnzeigeOffset;
        snakeSegment.AnzeigeAbStufe = stufenAnzeigeAbStufe;
        snakeSegment.StufenAnzeigeCanvas = stufenAnzeigeCanvas;
        snakeSegment.SetzeTyp(typ, stufe);
        snakeSegment.OnSegmentGestorben += SegmentGestorben;

        foreach (SpriteRenderer sr in segment.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = Mathf.Max(0, 9 - segments.Count);

        segments.Add(segment);
    }

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

        int listenAnzahl = Mathf.Min(anzahl, logikPositionen.Count - startIndex);
        if (listenAnzahl > 0)
        {
            logikPositionen.RemoveRange(startIndex, listenAnzahl);
            if (startIndex < vorigeLogikPos.Count)
                vorigeLogikPos.RemoveRange(startIndex,
                    Mathf.Min(listenAnzahl, vorigeLogikPos.Count - startIndex));
        }
    }

    public void EntferneSegmenteAnIndizes(List<int> indizes)
    {
        if (indizes == null || indizes.Count == 0) return;

        List<int> sortiert = new List<int>(indizes);
        sortiert.Sort();
        sortiert.Reverse();

        foreach (int idx in sortiert)
        {
            if (idx <= 0 || idx >= segments.Count) continue;

            if (segments[idx] != null)
            {
                LeanTween.cancel(segments[idx].gameObject);
                StartCoroutine(PopOutUndZerstoere(segments[idx].gameObject));
            }

            segments.RemoveAt(idx);
            if (idx < logikPositionen.Count) logikPositionen.RemoveAt(idx);
            if (idx < vorigeLogikPos.Count) vorigeLogikPos.RemoveAt(idx);
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

    // === KOPF-GESUNDHEIT ===

    public void KopfNimmtSchaden(float schaden)
    {
        if (!NurKopfUebrig) return;   // Kopf geschuetzt, solange Koerper existiert
        if (kopfStirbt) return;

        kopfHealth -= schaden;

        if (kopfBlitzCoroutine != null)
        {
            StopCoroutine(kopfBlitzCoroutine);
            SetzeKopfFarbe(false);
        }
        kopfBlitzCoroutine = StartCoroutine(KopfBlitz());

        if (kopfHealth <= 0f)
        {
            kopfStirbt = true;
            Sterben();
        }
    }

    // Zentraler Todeseinstieg: feuert nur das Event. Die UI zeigt daraufhin
    // den Death-Screen und pausiert das Spiel. Kein ResetState hier – der
    // Neustart laeuft ueber den Button (Szene neu laden).
    private void Sterben()
    {
        if (istTot) return;
        istTot = true;
        OnGestorben?.Invoke();
    }

    private System.Collections.IEnumerator KopfBlitz()
    {
        float dauer = BerechneKopfBlitzDauer();
        SetzeKopfFarbe(true);
        yield return new WaitForSeconds(dauer);
        SetzeKopfFarbe(false);
        kopfBlitzCoroutine = null;
    }

    private void SetzeKopfFarbe(bool blitz)
    {
        if (kopfSprite != null)
        {
            kopfSprite.color = blitz ? kopfBlitzFarbe : kopfGrundFarbeSprite;
        }
        if (kopfMesh != null && kopfMesh.material != null)
        {
            kopfMesh.material.color = blitz ? kopfBlitzFarbe : kopfGrundFarbeMesh;
        }
    }

    private float BerechneKopfBlitzDauer()
    {
        float max = Mathf.Max(0.0001f, kopfMaxHealth);
        float ratio = Mathf.Clamp01(kopfHealth / max);
        float dauer = Mathf.Lerp(kopfMinBlitzDauer, kopfMaxBlitzDauer, 1f - ratio);
        return Mathf.Min(dauer, 0.5f);
    }

    public void ResetState()
    {
        LeanTween.cancelAll();

        direction = Vector2Int.right;
        transform.position = Vector3.zero;
        transform.localScale = basisScale;
        kopfVorigePos = Vector3.zero;

        // Zustand zuruecksetzen
        kopfHealth = kopfMaxHealth;
        kopfStirbt = false;
        istTot = false;
        if (kopfBlitzCoroutine != null)
        {
            StopCoroutine(kopfBlitzCoroutine);
            kopfBlitzCoroutine = null;
        }
        SetzeKopfFarbe(false);

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

            if (segmentIndex > 0)
            {
                if (segmentIndex <= 2) return;

                Vector3 kopfPos = logikPositionen.Count > 0 ? logikPositionen[0] : transform.position;
                Vector3 segPos = segmentIndex < logikPositionen.Count
                    ? logikPositionen[segmentIndex]
                    : other.transform.position;

                if (Mathf.RoundToInt(kopfPos.x) != Mathf.RoundToInt(segPos.x) ||
                    Mathf.RoundToInt(kopfPos.y) != Mathf.RoundToInt(segPos.y))
                {
                    return;
                }
            }

            Sterben();
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            if (moveThroughWalls)
            {
                Traverse(other.transform);
            }
            else
            {
                Sterben();
            }
        }
    }

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