using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFollow2D : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]
    public float minSpeed = 2f;
    public float maxSpeed = 4f;
    private float currentSpeed;

    // Wird vom EnemyPersoenlichkeitsWechsler gesetzt.
    // 1 = unveraendert, < 1 = langsamer, > 1 = schneller.
    [HideInInspector] public float geschwindigkeitsFaktor = 1f;

    // true = Ziel ist das naechste Segment (Aggressiv), false = Kopf (Normal/Aengstlich)
    [HideInInspector] public bool zielNaechstesSegment = false;

    [Header("Kampf")]
    [Tooltip("Schaden, den der Gegner pro Anrempeln an ein Segment ODER den (allein stehenden) Kopf macht.")]
    public float schadenAnSegment = 1f;

    [Header("Rueckstoss")]
    public float rueckstossDistanz = 1.5f;
    public float rueckstossDauer = 0.15f;
    public float stunDauer = 0.4f;

    [Header("Haltedistanz")]
    [Tooltip("Distanz zur Schlange, ab der der Gegner abbremst und stoppt.")]
    public float haltDistanz = 3f;
    [Tooltip("Distanz zur Schlange, ab der der Gegner wieder anfaehrt (Hysterese, > haltDistanz setzen).")]
    public float weiterfahrDistanz = 4f;
    [Tooltip("Wie weich gebremst und angefahren wird. 0 = sofort, hoeher = traeger.")]
    [Range(1f, 20f)]
    public float bremsTraegheit = 8f;

    [Header("Ausrichtung")]
    [Tooltip("Wohin das Sprite im Ruhezustand zeigt. Bei rechtsgerichtetem Sprite meist 0 oder -180.")]
    public float blickrichtungOffset = 0f;
    [Tooltip("Drehgeschwindigkeit in Grad/Sek waehrend der Fahrt. 0 = sofort ausrichten.")]
    public float drehGeschwindigkeitFahrt = 180f;
    [Tooltip("Drehgeschwindigkeit in Grad/Sek im Stillstand. 0 = sofort ausrichten.")]
    public float drehGeschwindigkeitStopp = 45f;

    [Header("Separation (Gegner-Abstand)")]
    [Tooltip("Layer auf dem alle Gegner liegen. Nur dieser Layer wird fuer die Separation abgefragt.")]
    public LayerMask gegnerLayer;
    [Tooltip("Radius in dem nach Nachbargegnern gesucht wird. Circa Fahrzeugbreite x 1.5 empfohlen.")]
    public float separationsRadius = 1.5f;
    [Tooltip("Wie stark der Abstoessungsvektor auf die Bewegung wirkt.")]
    [Range(0f, 5f)]
    public float separationsStaerke = 1.5f;
    [Tooltip("Maximale Anzahl gleichzeitig erkannter Nachbargegner. Mehr als 6 selten noetig.")]
    [Range(2, 12)]
    public int maxNachbarn = 6;
    [Tooltip("Alle wieviel Frames die Separation neu berechnet wird. 3 = jeder Gegner rechnet jeden 3. Frame.")]
    [Range(1, 6)]
    public int separationsIntervall = 3;

    [Header("Hinderniss-Ausweichen (Context Steering)")]
    [Tooltip("Layer-Maske fuer Hindernisse (3D-Collider). Gegner und Schlange NICHT einbeziehen!")]
    public LayerMask hindernisLayer;
    [Tooltip("Anzahl radialer Raycasts. 8 reicht fuer offene Karten, 12 fuer enge.")]
    [Range(4, 24)]
    public int anzahlStrahlen = 8;
    [Tooltip("Reichweite der Obstacle-Raycasts. Circa 1-1.5x Fahrzeugbreite.")]
    public float detektionsRadius = 2.5f;
    [Tooltip("Wie stark blockierte Nachbarstrahlen abgestraft werden (Spread).")]
    [Range(0f, 1f)]
    public float gefahrSpread = 0.3f;
    [Tooltip("Gewichtung des Steering-Vektors relativ zur Zielrichtung. " +
             "Hoeher = weicht Hindernissen aggressiver aus.")]
    [Range(0f, 5f)]
    public float steeringStaerke = 2f;
    [Tooltip("Raycasts im Scene-View sichtbar machen (nur Editor).")]
    public bool debugStrahlen = false;

    [Header("Visuelles Feedback")]
    public float flashDauer = 0.1f;
    [Tooltip("Material, das beim Treffer kurz angezeigt wird (z.B. weisses Sprite-Material). Leer lassen = kein Flash.")]
    public Material flashMaterial;

    private enum Zustand { Frei, Gestoppt, Rueckstoss, Stun }
    private Zustand zustand = Zustand.Frei;

    private float timer;
    private Vector2 rueckstossRichtung;
    private float aktuelleTempo = 0f;   // wird weich interpoliert

    // Ueberlappte Koerper Segmente
    private readonly List<Transform> ueberlappendeSegmente = new List<Transform>();

    // Ueberlappter Kopf
    private Transform ueberlappenderKopf;
    private Snake kopfSnake;

    // Separation: heap-freier Buffer fuer OverlapCircleNonAlloc
    private Collider2D[] separationsBuffer;
    private Vector2      separationsDruck    = Vector2.zero;
    private int          separationsFrameOffset;

    // Context Steering: gecachter Ausweich-Vektor
    private Vector2 steeringDruck = Vector2.zero;

    // Fuer das Aufleuchten
    private MeshRenderer meshRenderer;
    private Material originalMaterial;

    void Start()
    {
        currentSpeed = Random.Range(minSpeed, maxSpeed);
        aktuelleTempo = currentSpeed;
        separationsBuffer      = new Collider2D[maxNachbarn];
        separationsFrameOffset = Random.Range(0, separationsIntervall);

        GameObject body = null;
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag("EnemyBody"))
            {
                body = child.gameObject;
                break;
            }
        }
        if (body != null)
        {
            meshRenderer = body.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                originalMaterial = meshRenderer.material;
        }

        Transform ziel = FindeZiel();
        if (ziel != null)
        {
            Vector2 richtung = (Vector2)ziel.position - (Vector2)transform.position;
            AusrichtenNach(richtung, sofort: true);
        }
    }

    void Update()
    {
        ueberlappendeSegmente.RemoveAll(s => s == null);

        switch (zustand)
        {
            case Zustand.Frei:
            case Zustand.Gestoppt:
                bool kopfVerwundbar = kopfSnake != null
                                      && ueberlappenderKopf != null
                                      && kopfSnake.NurKopfUebrig;

                if (ueberlappendeSegmente.Count > 0 || kopfVerwundbar)
                {
                    StarteRueckstoss(kopfVerwundbar);
                    break;
                }

                AktualisiereHalteZustand();
                BewegeZuZiel();
                break;

            case Zustand.Rueckstoss:
                float geschw = rueckstossDistanz / Mathf.Max(0.0001f, rueckstossDauer);
                transform.position += (Vector3)(rueckstossRichtung * geschw * Time.deltaTime);

                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    zustand = Zustand.Stun;
                    timer = stunDauer;
                }
                break;

            case Zustand.Stun:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                    zustand = Zustand.Frei;
                break;
        }
    }

    // Prueft die Distanz zur Schlange und wechselt zwischen Frei/Gestoppt.
    // Hysterese: erst bei haltDistanz stoppen, erst bei weiterfahrDistanz wieder losfahren.
    private void AktualisiereHalteZustand()
    {
        Transform ziel = FindeZiel();
        if (ziel == null) return;

        float distanz = Vector2.Distance(transform.position, ziel.position);

        if (zustand == Zustand.Frei && distanz <= haltDistanz)
        {
            zustand = Zustand.Gestoppt;
        }
        else if (zustand == Zustand.Gestoppt && distanz > weiterfahrDistanz)
        {
            zustand = Zustand.Frei;
        }
    }

    private void BewegeZuZiel()
    {
        Transform ziel = FindeZiel();
        if (ziel == null) return;

        // Separation + Steering nur alle N Frames neu berechnen (versetzt per Frame-Offset)
        if ((Time.frameCount + separationsFrameOffset) % separationsIntervall == 0)
        {
            BerechneSeperationsDruck();
            BerechneSteeringDruck();
        }

        // Zieltempo: 0 wenn gestoppt, currentSpeed * Persoenlichkeitsfaktor wenn frei
        float zielTempo = (zustand == Zustand.Gestoppt)
            ? 0f
            : currentSpeed * Mathf.Max(0f, geschwindigkeitsFaktor);

        // Sanft interpolieren (abbremsen und anfahren)
        aktuelleTempo = Mathf.Lerp(aktuelleTempo, zielTempo, bremsTraegheit * Time.deltaTime);

        if (aktuelleTempo < 0.01f) aktuelleTempo = 0f;

        // Bewegungsrichtung: Zielrichtung + Separation + Hindernisausweichen
        Vector2 zielRichtung = ((Vector2)ziel.position - (Vector2)transform.position).normalized;
        Vector2 bewegungsRichtung = (zielRichtung + separationsDruck + steeringDruck).normalized;

        Vector3 neuePosition = transform.position
            + (Vector3)(bewegungsRichtung * aktuelleTempo * Time.deltaTime);
        neuePosition.z = transform.position.z;
        transform.position = neuePosition;

        AusrichtenNach(zielRichtung);
    }

    // Berechnet per N radialen 3D-Raycasts einen Ausweich-Vektor von Hindernissen weg.
    // Blockierte Strahlen erzeugen einen Gegenvektor; Nachbarstrahlen werden per
    // gefahrSpread abgestraft. Der resultierende Vektor wird gecacht.
    private void BerechneSteeringDruck()
    {
        float winkelSchritt = 360f / anzahlStrahlen;
        float[] gefahr = new float[anzahlStrahlen];

        for (int i = 0; i < anzahlStrahlen; i++)
        {
            Vector2 dir2D = Quaternion.Euler(0f, 0f, i * winkelSchritt) * Vector2.right;
            Vector3 dir3D = new Vector3(dir2D.x, dir2D.y, 0f);

            if (Physics.Raycast(transform.position, dir3D, detektionsRadius, hindernisLayer))
            {
                gefahr[i] = 1f;
                int links  = (i - 1 + anzahlStrahlen) % anzahlStrahlen;
                int rechts = (i + 1) % anzahlStrahlen;
                gefahr[links]  = Mathf.Max(gefahr[links],  gefahrSpread);
                gefahr[rechts] = Mathf.Max(gefahr[rechts], gefahrSpread);
            }

#if UNITY_EDITOR
            if (debugStrahlen)
            {
                Color farbe = gefahr[i] >= 1f ? Color.red
                            : gefahr[i] >  0f ? Color.yellow
                            : Color.green;
                Debug.DrawRay(transform.position, dir3D * detektionsRadius, farbe);
            }
#endif
        }

        // Abstoessungsvektor: jeder blockierte Strahl schiebt in die Gegenrichtung
        Vector2 druck = Vector2.zero;
        for (int i = 0; i < anzahlStrahlen; i++)
        {
            if (gefahr[i] <= 0f) continue;
            Vector2 dir2D = Quaternion.Euler(0f, 0f, i * winkelSchritt) * Vector2.right;
            druck -= dir2D * gefahr[i];
        }

        steeringDruck = druck * steeringStaerke;
    }

    // Sammelt alle Gegner im separationsRadius per NonAlloc (kein Heap-Alloc)
    // und berechnet einen Abstoessungsvektor weg von jedem Nachbarn.
    // Naehere Nachbarn stossen staerker ab (1/Distanz Gewichtung).
    private void BerechneSeperationsDruck()
    {
        int gefunden = Physics2D.OverlapCircleNonAlloc(
            transform.position, separationsRadius, separationsBuffer, gegnerLayer);

        Vector2 druck = Vector2.zero;

        for (int i = 0; i < gefunden; i++)
        {
            if (separationsBuffer[i] == null) continue;
            // Eigenen Collider ignorieren
            if (separationsBuffer[i].transform == transform) continue;

            Vector2 weg = (Vector2)transform.position - (Vector2)separationsBuffer[i].transform.position;
            float distanz = weg.magnitude;

            // Abstoessung staerker bei geringer Distanz; durch Distanz dividieren,
            // Null-Division absichern
            if (distanz > 0.001f)
                druck += weg.normalized / distanz;
        }

        // Skalieren und cachen; bei keinem Nachbarn sanft auf 0 zurueck
        separationsDruck = Vector2.Lerp(
            separationsDruck,
            druck * separationsStaerke,
            10f * Time.deltaTime);
    }

    public void AufleuchtenLassen()
    {
        if (meshRenderer != null && flashMaterial != null && gameObject.activeInHierarchy)
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        meshRenderer.material = flashMaterial;
        yield return new WaitForSeconds(flashDauer);
        meshRenderer.material = originalMaterial;
    }

    private void StarteRueckstoss(bool kopfVerwundbar)
    {
        Transform ziel = NaechstesUeberlapptesSegment();
        float zielDist = (ziel != null)
            ? Vector2.Distance(transform.position, ziel.position)
            : Mathf.Infinity;
        bool zielIstKopf = false;

        if (kopfVerwundbar && ueberlappenderKopf != null)
        {
            float kopfDist = Vector2.Distance(transform.position, ueberlappenderKopf.position);
            if (kopfDist < zielDist)
            {
                ziel = ueberlappenderKopf;
                zielIstKopf = true;
            }
        }

        if (ziel != null)
        {
            if (zielIstKopf)
                kopfSnake.KopfNimmtSchaden(schadenAnSegment);
            else
            {
                SnakeSegment segment = ziel.GetComponent<SnakeSegment>();
                if (segment != null) segment.NimmSchaden(schadenAnSegment);
            }

            Vector2 richtung = (Vector2)transform.position - (Vector2)ziel.position;
            rueckstossRichtung = (richtung.sqrMagnitude < 0.0001f)
                ? Random.insideUnitCircle.normalized
                : richtung.normalized;

            AusrichtenNach(-rueckstossRichtung);
        }
        else
        {
            rueckstossRichtung = Random.insideUnitCircle.normalized;
        }

        zustand = Zustand.Rueckstoss;
        timer = rueckstossDauer;
    }

    private void AusrichtenNach(Vector2 dir, bool sofort = false)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        float winkel = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + blickrichtungOffset;
        Quaternion zielRotation = Quaternion.Euler(0f, 0f, winkel);

        if (sofort)
        {
            transform.rotation = zielRotation;
            return;
        }

        float geschwindigkeit = (zustand == Zustand.Gestoppt)
            ? drehGeschwindigkeitStopp
            : drehGeschwindigkeitFahrt;

        // 0 = sofort einrasten (Originalverhalten als Fallback)
        if (geschwindigkeit <= 0f)
        {
            transform.rotation = zielRotation;
            return;
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, zielRotation, geschwindigkeit * Time.deltaTime);
    }

    private Transform FindeZiel()
    {
        // Aggressiv: direkt naechstes Segment ansteuern, Kopf ignorieren
        if (zielNaechstesSegment)
            return FindeNaechstesSegment();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            return player.transform;
        return FindeNaechstesSegment();
    }

    private Transform FindeNaechstesSegment()
    {
        SnakeSegment[] segmente = FindObjectsOfType<SnakeSegment>();
        Transform naechstes = null;
        float kuerzesteDistanz = Mathf.Infinity;
        foreach (SnakeSegment seg in segmente)
        {
            if (seg == null) continue;
            float distanz = Vector2.Distance(transform.position, seg.transform.position);
            if (distanz < kuerzesteDistanz)
            {
                kuerzesteDistanz = distanz;
                naechstes = seg.transform;
            }
        }
        return naechstes;
    }

    private Transform NaechstesUeberlapptesSegment()
    {
        Transform naechstes = null;
        float kuerzesteDistanz = Mathf.Infinity;
        foreach (Transform seg in ueberlappendeSegmente)
        {
            if (seg == null) continue;
            float distanz = Vector2.Distance(transform.position, seg.position);
            if (distanz < kuerzesteDistanz)
            {
                kuerzesteDistanz = distanz;
                naechstes = seg;
            }
        }
        return naechstes;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SnakeSegment>() != null)
        {
            if (!ueberlappendeSegmente.Contains(other.transform))
                ueberlappendeSegmente.Add(other.transform);
            return;
        }

        Snake snake = other.GetComponent<Snake>();
        if (snake != null)
        {
            ueberlappenderKopf = other.transform;
            kopfSnake = snake;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<SnakeSegment>() != null)
        {
            ueberlappendeSegmente.Remove(other.transform);
            return;
        }

        if (other.transform == ueberlappenderKopf)
        {
            ueberlappenderKopf = null;
            kopfSnake = null;
        }
    }
}
