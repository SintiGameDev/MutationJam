using UnityEngine;

// ============================================================
//  EnemyPersoenlichkeitsWechsler
// ============================================================
//  Liegt auf demselben GameObject wie EnemyFollow2D.
//  Zieht in zufaelligen Abstaenden eine neue Persoenlichkeit
//  aus dem konfigurierten Pool und schreibt deren Werte in
//  EnemyFollow2D. Kein Eingriff in die Bewegungslogik noetig.
//
//  Setup: Pool im Inspector besetzen. Wird der Pool leer
//  gelassen, passiert nichts (EnemyFollow2D Standardwerte
//  bleiben aktiv).
// ============================================================

[RequireComponent(typeof(EnemyFollow2D))]
public class EnemyPersoenlichkeitsWechsler : MonoBehaviour
{
    [Header("Persoenlichkeits-Pool")]
    [Tooltip("Alle moeglichen Persoenlichkeiten. Gewichtung erfolgt gleichmaessig. " +
             "Dieselbe Persoenlichkeit mehrfach eintragen = hoehere Wahrscheinlichkeit.")]
    public EnemyPersoenlichkeit[] pool;

    [Header("Startverhalten")]
    [Tooltip("Zufaellige Persoenlichkeit direkt beim Spawn setzen. " +
             "Deaktiviert = erstes Element des Pools wird verwendet.")]
    public bool zufaelligerStart = true;

    [Tooltip("Zufaelliger Start-Offset des ersten Wechsels in Sekunden, " +
             "damit nicht alle Gegner gleichzeitig wechseln.")]
    public float startOffsetMax = 3f;

    [Header("Debug")]
    [Tooltip("Aktuelle Persoenlichkeit und Timer im Log ausgeben.")]
    public bool debugLog = false;

    // ── Laufzeit ────────────────────────────────────────────
    private EnemyFollow2D follow;
    private EnemyPersoenlichkeit aktivePersoenlichkeit;
    private float wechselTimer;

    private void Awake()
    {
        follow = GetComponent<EnemyFollow2D>();
    }

    private void Start()
    {
        if (pool == null || pool.Length == 0) return;

        // Startwert: zufaellig oder erstes Element
        int startIndex = zufaelligerStart ? Random.Range(0, pool.Length) : 0;
        WendePersoenlichkeitAn(pool[startIndex]);

        // Naechsten Wechsel planen (mit zufaelligem Offset fuer Desynchronisation)
        float offset = Random.Range(0f, startOffsetMax);
        wechselTimer = WechselIntervall() + offset;
    }

    private void Update()
    {
        if (pool == null || pool.Length == 0) return;

        wechselTimer -= Time.deltaTime;
        if (wechselTimer <= 0f)
            WaehleNaechstePersoenlichkeit();
    }

    // Waehlt eine neue Persoenlichkeit aus dem Pool — bevorzugt eine andere
    // als die aktuell aktive (sofern der Pool mehr als einen Eintrag hat).
    private void WaehleNaechstePersoenlichkeit()
    {
        EnemyPersoenlichkeit naechste;

        if (pool.Length == 1)
        {
            naechste = pool[0];
        }
        else
        {
            // Aktuelle Persoenlichkeit aus der Auswahl temporaer ausschliessen,
            // damit nicht zweimal hintereinander dieselbe gezogen wird.
            int versuche = 0;
            do
            {
                naechste = pool[Random.Range(0, pool.Length)];
                versuche++;
            }
            while (naechste == aktivePersoenlichkeit && versuche < 10);
        }

        WendePersoenlichkeitAn(naechste);
        wechselTimer = WechselIntervall();
    }

    // Schreibt alle Werte der Persoenlichkeit in EnemyFollow2D.
    private void WendePersoenlichkeitAn(EnemyPersoenlichkeit p)
    {
        if (p == null) return;
        aktivePersoenlichkeit = p;

        // Zielwahl
        follow.zielNaechstesSegment     = p.zielNaechstesSegment;

        // Halteverhalten
        follow.haltDistanz       = p.haltDistanz;
        follow.weiterfahrDistanz = p.weiterfahrDistanz;

        // Bewegung
        follow.geschwindigkeitsFaktor   = p.geschwindigkeitsFaktor;
        follow.bremsTraegheit           = p.bremsTraegheit;

        // Ausrichtung
        follow.drehGeschwindigkeitFahrt = p.drehGeschwindigkeitFahrt;
        follow.drehGeschwindigkeitStopp = p.drehGeschwindigkeitStopp;

        if (debugLog)
            Debug.Log($"[{name}] Persoenlichkeit → {p.bezeichnung} " +
                      $"(naechster Wechsel in {wechselTimer:F1}s)");
    }

    // Zieht ein zufaelliges Intervall aus dem Bereich der aktiven Persoenlichkeit.
    private float WechselIntervall()
    {
        if (aktivePersoenlichkeit == null) return 8f;
        return Random.Range(aktivePersoenlichkeit.minDauer, aktivePersoenlichkeit.maxDauer);
    }

    // Oeffentlich zugaenglich — z.B. vom Spawner aufrufbar um
    // eine bestimmte Persoenlichkeit zu erzwingen.
    public void SetzePersoenlichkeit(EnemyPersoenlichkeit p)
    {
        WendePersoenlichkeitAn(p);
        wechselTimer = WechselIntervall();
    }

    public EnemyPersoenlichkeit AktivePersoenlichkeit => aktivePersoenlichkeit;
}
