using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Turm-Werte")]
    public float range = 5f;        // Reichweite des Turms

    [Header("Burst-Feuer")]
    [Tooltip("Schuesse pro Salve. Wird vom Segment aus der TurmKonfiguration gesetzt.")]
    public int schuessProBurst = 3;
    [Tooltip("Abstand zwischen Schuessen INNERHALB einer Salve (Sek).")]
    public float taktImBurst = 0.1f;
    [Tooltip("Pause zwischen zwei Salven (Sek).")]
    public float pauseZwischenBursts = 1.5f;

    [Tooltip("Schaden pro Projektil. Wird vom Segment je nach Mutationsstufe gesetzt " +
             "und beim Schuss auf das Projektil geschrieben.")]
    public float schaden = 10f;

    [Tooltip("Schuss-Sound. Wird vom Segment aus der TurmKonfiguration gesetzt. " +
             "Spielt einmal pro Schuss (nicht pro Feuerpunkt).")]
    public AudioClip schussSound;
    [Range(0f, 1f)] public float schussLautstaerke = 1f;

    // --- Harte Untergrenzen, damit der Turm NIE jeden Frame feuern kann. ---
    private const float MIN_TAKT = 0.02f;
    private const float MIN_PAUSE = 0.05f;

    // Burst-Zustand
    private int schussImBurst = 0;
    private float naechsterSchuss = 0f;

    // Merkt sich die zuletzt angewendete Konfiguration/Stufe, damit ein erneutes
    // Anwenden derselben Werte den laufenden Burst NICHT zuruecksetzt.
    private TurmKonfiguration letzteKonfig = null;
    private int letzteStufe = int.MinValue;

    [Header("Unity Setup")]
    public string enemyTag = "Enemy";

    [Tooltip("Einzelner Feuerpunkt (Fallback). Wird nur benutzt, wenn 'Fire Points' " +
             "leer ist – so bleiben bestehende Prefabs ohne Aenderung lauffaehig.")]
    public Transform firePoint;

    [Tooltip("Mehrere Feuerpunkte. Bei einem Schuss feuern ALLE gleichzeitig auf das " +
             "anvisierte Ziel. Ist die Liste leer, wird 'Fire Point' oben benutzt. " +
             "ACHTUNG: jeder Feuerpunkt erzeugt EIN eigenes Projektil pro Schuss – " +
             "3 Feuerpunkte x 3 Schuss/Salve = 9 Projektile pro Salve.")]
    public Transform[] firePoints;

    public GameObject projectilePrefab;

    [Header("Zielwahl")]
    [Tooltip("Sekunden zwischen den Zielsuchen fuer die AUSRICHTUNG. 0 = jeden Frame.\n" +
             "Unabhaengig davon wird DIREKT VOR JEDEM SCHUSS immer frisch der naechste " +
             "Gegner in Reichweite gewaehlt – so trifft der Turm pro Schuss den " +
             "bedrohlichsten (naechsten) Gegner. Wert nur hochsetzen, wenn es bei sehr " +
             "vielen Tuermen/Gegnern Performance-Probleme gibt.")]
    public float zielSuchIntervall = 0f;

    private float naechsteZielsuche = 0f;

    [Header("Ausrichtung")]
    [Tooltip("Wohin das Sprite im Ruhezustand zeigt:\n" +
             "  -90 = Sprite schaut nach OBEN (+Y)\n" +
             "    0 = Sprite schaut nach RECHTS (+X)\n" +
             "  180 = nach links,  90 = nach unten.\n" +
             "An dein Geschuetz-Sprite anpassen.")]
    public float blickrichtungOffset = -90f;

    [Tooltip("Drehgeschwindigkeit in Grad/Sekunde. 0 = sofort ausrichten (kein Nachdrehen).")]
    public float drehGeschwindigkeit = 720f;

    private Transform target;

    // FIX Bug 1: Vorhalt-Zustand ist an das konkrete Target-Objekt gebunden.
    // Wenn das Target wechselt, wird die alte Positionshistorie verworfen,
    // damit keine Phantomgeschwindigkeit entsteht.
    private Transform vorhaltTarget = null;    // fuer welches Objekt vorigeZielPos gilt
    private Vector3   vorigeZielPos;
    private bool      habeVorigeZielPos = false;
    private float     projektilSpeed = 10f;

    // FIX Bug 2: Frame-Cache speichert zusaetzlich das Target, fuer das er
    // berechnet wurde. Ein Target-Wechsel innerhalb desselben Frames (Update-
    // Ausrichtung vs. Schuss-Ziel) liefert jetzt korrekt einen neuen Vorhalt.
    private Vector3   aktuellerVorhaltPunkt;
    private int       vorhaltFrame = -1;
    private Transform vorhaltCacheTarget = null;

    void Start()
    {
        LiesProjektilTempo();
    }

    private void LiesProjektilTempo()
    {
        if (projectilePrefab != null)
        {
            Projectiles p = projectilePrefab.GetComponent<Projectiles>();
            if (p != null) projektilSpeed = p.speed;
        }
    }

    // ------------------------------------------------------------------
    // Zentraler Einstiegspunkt fuer das Segment, um Werte aus der
    // TurmKonfiguration zu uebernehmen.
    // ------------------------------------------------------------------
    public void WendeKonfigurationAn(TurmKonfiguration konfig, int stufe)
    {
        if (konfig == null) return;

        TurmKonfiguration.SkalierteWerte w = konfig.BerechneWerte(stufe);

        range               = w.reichweite;
        schaden             = w.schaden;
        schuessProBurst     = Mathf.Max(1, w.schuessProBurst);
        taktImBurst         = w.taktImBurst;
        pauseZwischenBursts = w.pauseZwischenBursts;

        schussSound       = konfig.schussSound;
        schussLautstaerke = konfig.schussLautstaerke;

        if (konfig.projektilPrefab != null && projectilePrefab != konfig.projektilPrefab)
        {
            projectilePrefab = konfig.projektilPrefab;
            LiesProjektilTempo();
        }

        bool veraendert = (konfig != letzteKonfig) || (stufe != letzteStufe);
        if (veraendert)
        {
            schussImBurst   = 0;
            naechsterSchuss = Mathf.Max(naechsterSchuss, Time.time + Mathf.Max(MIN_TAKT, taktImBurst));
            letzteKonfig    = konfig;
            letzteStufe     = stufe;
        }
    }

    private Transform FindeBestesZiel()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        float     kuerzesteDistanz = range;
        Transform bestesZiel       = null;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            float distanz = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanz <= kuerzesteDistanz)
            {
                kuerzesteDistanz = distanz;
                bestesZiel       = enemy.transform;
            }
        }

        return bestesZiel;
    }

    void Update()
    {
        // 1) Ziel fuer die Ausrichtung aktualisieren (gedrosselt).
        if (Time.time >= naechsteZielsuche)
        {
            SetzeTarget(FindeBestesZiel());
            naechsteZielsuche = Time.time + Mathf.Max(0f, zielSuchIntervall);
        }

        if (target == null)
        {
            schussImBurst = 0;
            return;
        }

        if (Time.time < naechsterSchuss)
            return;

        // 2) Direkt vor dem Schuss nochmal frisch das beste Ziel waehlen.
        SetzeTarget(FindeBestesZiel());
        if (target == null)
        {
            schussImBurst = 0;
            return;
        }

        Shoot();
        schussImBurst++;

        if (schussImBurst >= Mathf.Max(1, schuessProBurst))
        {
            schussImBurst   = 0;
            naechsterSchuss = Time.time + Mathf.Max(MIN_PAUSE, pauseZwischenBursts);
        }
        else
        {
            naechsterSchuss = Time.time + Mathf.Max(MIN_TAKT, taktImBurst);
        }
    }

    void LateUpdate()
    {
        RichteAufZiel();
    }

    // FIX Bug 1: Target-Wechsel invalidiert die Positionshistorie.
    // Ohne diesen Schritt wuerde (GegnerB.pos - GegnerA.pos) / deltaTime
    // als Geschwindigkeit des neuen Ziels interpretiert -> Phantomvorhalt
    // hunderte Units entfernt -> wildes Rotieren.
    private void SetzeTarget(Transform neuesTarget)
    {
        if (neuesTarget == target) return;   // unveraendert, nichts tun

        target = neuesTarget;

        // Alte Positionshistorie gehoert zum alten Objekt -> wegwerfen.
        habeVorigeZielPos = false;
        vorhaltTarget     = target;
    }

    // FIX Bug 2: Cache ist nur gueltig, wenn Target und Frame uebereinstimmen.
    // Wechselt das Target innerhalb eines Frames (Ausrichtung vs. Schuss),
    // wird ein frischer Vorhalt berechnet statt der gecachte Wert zurueck-
    // gegeben zu werden.
    private Vector3 HoleVorhaltPunkt()
    {
        if (Time.frameCount != vorhaltFrame || vorhaltCacheTarget != target)
        {
            aktuellerVorhaltPunkt = BerechneVorhaltPunkt();
            vorhaltFrame          = Time.frameCount;
            vorhaltCacheTarget    = target;
        }
        return aktuellerVorhaltPunkt;
    }

    // Berechnet den Vorhalt-Punkt (Abfangpunkt) per quadratischer Gleichung.
    // FIX Bug 3: Sucht explizit die kleinste POSITIVE Loesung, statt einfach
    // Mathf.Min zu nehmen (das kann eine negative Loesung bevorzugen).
    private Vector3 BerechneVorhaltPunkt()
    {
        if (target == null) return transform.position;

        Vector3 zielPos = target.position;

        // Geschwindigkeit des Gegners schaetzen (Positionsdifferenz / Zeit).
        // habeVorigeZielPos ist false, sobald das Target gewechselt hat (Bug-1-Fix),
        // also starten wir fuer ein neues Ziel sauber mit Geschwindigkeit = 0.
        Vector3 zielGeschw = Vector3.zero;
        if (habeVorigeZielPos && Time.deltaTime > 0f)
            zielGeschw = (zielPos - vorigeZielPos) / Time.deltaTime;

        vorigeZielPos     = zielPos;
        habeVorigeZielPos = true;

        // Abfang-Gleichung:  |relPos + zielGeschw*t| = projektilSpeed * t
        // Ausmultipliziert:  a*t^2 + b*t + c = 0
        Vector3 relPos = zielPos - transform.position;
        float a = Vector3.Dot(zielGeschw, zielGeschw) - projektilSpeed * projektilSpeed;
        float b = 2f * Vector3.Dot(relPos, zielGeschw);
        float c = Vector3.Dot(relPos, relPos);

        float t = 0f;

        if (Mathf.Abs(a) < 0.0001f)
        {
            // Sonderfall: Gegner-Tempo ~ Projektil-Tempo -> linearer Fall
            if (Mathf.Abs(b) > 0.0001f) t = -c / b;
        }
        else
        {
            float diskriminante = b * b - 4f * a * c;
            if (diskriminante >= 0f)
            {
                float wurzel = Mathf.Sqrt(diskriminante);
                float t1 = (-b + wurzel) / (2f * a);
                float t2 = (-b - wurzel) / (2f * a);

                // FIX Bug 3: Kleinste POSITIVE Loesung waehlen.
                // Mathf.Min(t1, t2) kann negativ sein, auch wenn eine der beiden
                // positiv ist – das fuehrte zum Vorbeischuss.
                if (t1 >= 0f && t2 >= 0f)
                    t = Mathf.Min(t1, t2);      // beide positiv -> naeherliegende nehmen
                else if (t1 >= 0f)
                    t = t1;
                else if (t2 >= 0f)
                    t = t2;
                // else: beide negativ -> t bleibt 0 (Gegner hinter dem Turm o.ae.)
            }
        }

        if (t < 0f) t = 0f;

        return zielPos + zielGeschw * t;
    }

    private void RichteAufZiel()
    {
        if (target == null) return;

        Vector3 vorhalt  = HoleVorhaltPunkt();
        Vector3 richtung = vorhalt - transform.position;
        if (richtung.sqrMagnitude < 0.0001f) return;

        float     winkel = Mathf.Atan2(richtung.y, richtung.x) * Mathf.Rad2Deg + blickrichtungOffset;
        Quaternion ziel  = Quaternion.Euler(0f, 0f, winkel);

        if (drehGeschwindigkeit <= 0f)
        {
            transform.rotation = ziel;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, ziel, drehGeschwindigkeit * Time.deltaTime);
        }
    }

    void Shoot()
    {
        Vector3 vorhalt = HoleVorhaltPunkt();

        if (schussSound != null)
            SoundManager.Instance?.SpieleClip(schussSound, schussLautstaerke);

        if (firePoints != null && firePoints.Length > 0)
        {
            foreach (Transform fp in firePoints)
            {
                if (fp != null) FeuereVon(fp, vorhalt, target);
            }
        }
        else if (firePoint != null)
        {
            FeuereVon(firePoint, vorhalt, target);
        }
    }

    // Spawnt EIN Projektil an einem Feuerpunkt.
    // homingZiel wird ans Projektil weitergegeben – ist homingAktiv am Prefab
    // deaktiviert, ignoriert das Projektil den Wert einfach.
    private void FeuereVon(Transform fp, Vector3 vorhalt, Transform homingZiel)
    {
        if (projectilePrefab == null) return;

        GameObject  projektilGO = Instantiate(projectilePrefab, fp.position, fp.rotation);
        Projectiles projektil   = projektilGO.GetComponent<Projectiles>();

        if (projektil != null)
        {
            projektil.damage = schaden;
            projektil.SetzeTiefeAusWeltpunkt(vorhalt);

            // Startrichtung: Vorhalt-Punkt des Turms (gibt dem Homing einen guten
            // Startvektor; ohne Homing ist das die einzige Flugrichtung).
            projektil.SchiesseInRichtung(vorhalt - fp.position);

            // Homing-Ziel mitgeben. Das Projektil ignoriert diesen Aufruf, wenn
            // homingAktiv am Prefab auf false steht.
            projektil.SetzZiel(homingZiel);
        }
    }

    void OnValidate()
    {
        schuessProBurst     = Mathf.Max(1, schuessProBurst);
        taktImBurst         = Mathf.Max(MIN_TAKT, taktImBurst);
        pauseZwischenBursts = Mathf.Max(MIN_PAUSE, pauseZwischenBursts);
        zielSuchIntervall   = Mathf.Max(0f, zielSuchIntervall);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
