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
    private int schussImBurst = 0;     // wie viele Schuesse der aktuellen Salve schon raus sind
    private float naechsterSchuss = 0f;  // Zeitpunkt (Time.time), ab dem wieder gefeuert werden darf

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

    // Vorhalt: vorige Zielposition + Projektiltempo, um Gegnergeschwindigkeit
    // zu schaetzen und den Abfangpunkt zu berechnen.
    private Vector3 vorigeZielPos;
    private bool habeVorigeZielPos = false;
    private float projektilSpeed = 10f;

    // Vorhalt-Punkt wird pro Frame nur EINMAL berechnet (siehe HoleVorhaltPunkt).
    private Vector3 aktuellerVorhaltPunkt;
    private int vorhaltFrame = -1;

    void Start()
    {
        LiesProjektilTempo();
    }

    // Projektiltempo einmalig vom Prefab lesen (fuer die Vorhalt-Rechnung).
    private void LiesProjektilTempo()
    {
        if (projectilePrefab != null)
        {
            Projectiles p = projectilePrefab.GetComponent<Projectiles>();
            if (p != null) projektilSpeed = p.speed;
        }
    }

    // ------------------------------------------------------------------
    // Zentraler, EINZIGER Einstiegspunkt fuer das Segment, um die Werte aus
    // der TurmKonfiguration zu uebernehmen. Ueber diese Methode anwenden statt
    // die Felder einzeln zu beschreiben.
    //
    // Wichtig gegen "zu viele Projektile":
    //  - Mehrfaches Aufrufen mit DENSELBEN Werten (z.B. jeden Frame durchs
    //    Segment) setzt den Burst-Zustand NICHT zurueck -> kein Dauerfeuer.
    //  - 'naechsterSchuss' wird nie nach vorne (in die Vergangenheit) gezogen,
    //    eine laufende Salven-Pause bleibt also erhalten.
    // ------------------------------------------------------------------
    public void WendeKonfigurationAn(TurmKonfiguration konfig, int stufe)
    {
        if (konfig == null) return;

        TurmKonfiguration.SkalierteWerte w = konfig.BerechneWerte(stufe);

        // Werte uebernehmen (guenstig, darf jeder Frame passieren).
        range = w.reichweite;
        schaden = w.schaden;
        schuessProBurst = Mathf.Max(1, w.schuessProBurst);
        taktImBurst = w.taktImBurst;
        pauseZwischenBursts = w.pauseZwischenBursts;

        schussSound = konfig.schussSound;
        schussLautstaerke = konfig.schussLautstaerke;

        if (konfig.projektilPrefab != null && projectilePrefab != konfig.projektilPrefab)
        {
            projectilePrefab = konfig.projektilPrefab;
            LiesProjektilTempo();
        }

        // Nur bei einer ECHTEN Aenderung (anderer Turmtyp oder andere Stufe)
        // den Burst sauber neu starten. Sonst Zustand unangetastet lassen.
        bool veraendert = (konfig != letzteKonfig) || (stufe != letzteStufe);
        if (veraendert)
        {
            schussImBurst = 0;
            // Cooldown nie verkuerzen: laufende Pause bleibt, frisch frueher als jetzt geht nicht.
            naechsterSchuss = Mathf.Max(naechsterSchuss, Time.time + Mathf.Max(MIN_TAKT, taktImBurst));
            letzteKonfig = konfig;
            letzteStufe = stufe;
        }
    }

    // Sucht den naechsten Gegner (Tag 'enemyTag') in Reichweite. Naechster Gegner
    // = groesste Bedrohung, weil er dem Turm am ehesten Schaden zufuegen kann.
    // Liefert null, wenn kein Gegner in Reichweite ist.
    private Transform FindeBestesZiel()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        float kuerzesteDistanz = range;   // nur Gegner innerhalb der Reichweite zaehlen
        Transform bestesZiel = null;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            float distanz = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanz <= kuerzesteDistanz)
            {
                kuerzesteDistanz = distanz;
                bestesZiel = enemy.transform;
            }
        }

        return bestesZiel;
    }

    void Update()
    {
        // 1) Ziel fuer die AUSRICHTUNG aktualisieren (gedrosselt ueber zielSuchIntervall,
        //    0 = jeden Frame). So bleibt das Geschuetz auch waehrend der Salven-Pause
        //    auf den naechsten Gegner gerichtet.
        if (Time.time >= naechsteZielsuche)
        {
            target = FindeBestesZiel();
            naechsteZielsuche = Time.time + Mathf.Max(0f, zielSuchIntervall);
        }

        if (target == null)
        {
            // Ohne Ziel: laufende Salve abbrechen, naechste startet frisch beim
            // naechsten Ziel. 'naechsterSchuss' bleibt erhalten, damit eine
            // gerade begonnene Pause NICHT uebersprungen wird.
            schussImBurst = 0;
            return;
        }

        if (Time.time < naechsterSchuss)
            return;

        // 2) PRO SCHUSS frisch das beste Ziel waehlen – unabhaengig von der
        //    Ausrichtungs-Drosselung. So trifft jeder einzelne Schuss garantiert
        //    den aktuell naechsten (bedrohlichsten) Gegner.
        target = FindeBestesZiel();
        if (target == null)
        {
            schussImBurst = 0;
            return;
        }

        // Ein Schuss der aktuellen Salve
        Shoot();
        schussImBurst++;

        if (schussImBurst >= Mathf.Max(1, schuessProBurst))
        {
            // Salve fertig -> lange Pause, dann neue Salve
            schussImBurst = 0;
            naechsterSchuss = Time.time + Mathf.Max(MIN_PAUSE, pauseZwischenBursts);
        }
        else
        {
            // Naechster Schuss innerhalb der Salve -> kurzer Takt
            naechsterSchuss = Time.time + Mathf.Max(MIN_TAKT, taktImBurst);
        }
    }

    // Ausrichtung NACH der Segment-Bewegung (LeanTween laeuft im Update),
    // damit auf die finale Position dieses Frames gezielt wird.
    void LateUpdate()
    {
        RichteAufZiel();
    }

    // Liefert den Vorhalt-Punkt und berechnet ihn pro Frame nur EINMAL.
    // Wichtig: BerechneVorhaltPunkt() aktualisiert die geschaetzte Gegner-
    // geschwindigkeit (vorigeZielPos). Wuerde es zweimal pro Frame laufen
    // (Shoot in Update + RichteAufZiel in LateUpdate), kaeme im zweiten Aufruf
    // eine Geschwindigkeit von 0 heraus -> kein Vorhalt. Der Frame-Cache
    // verhindert das.
    private Vector3 HoleVorhaltPunkt()
    {
        if (Time.frameCount != vorhaltFrame)
        {
            aktuellerVorhaltPunkt = BerechneVorhaltPunkt();
            vorhaltFrame = Time.frameCount;
        }
        return aktuellerVorhaltPunkt;
    }

    // Berechnet den Vorhalt-Punkt: wo der Gegner sein wird, wenn das Projektil
    // ankommt. Nutzt die aus zwei Frames geschaetzte Gegnergeschwindigkeit.
    private Vector3 BerechneVorhaltPunkt()
    {
        if (target == null) return transform.position;

        Vector3 zielPos = target.position;

        // Gegnergeschwindigkeit schaetzen (Positionsdifferenz pro Sekunde)
        Vector3 zielGeschw = Vector3.zero;
        if (habeVorigeZielPos && Time.deltaTime > 0f)
            zielGeschw = (zielPos - vorigeZielPos) / Time.deltaTime;

        vorigeZielPos = zielPos;
        habeVorigeZielPos = true;

        // Abfang-Gleichung loesen: |zielPos + zielGeschw*t - turmPos| = projektilSpeed*t
        // => quadratisch in t.  a*t^2 + b*t + c = 0
        Vector3 relPos = zielPos - transform.position;
        float a = Vector3.Dot(zielGeschw, zielGeschw) - projektilSpeed * projektilSpeed;
        float b = 2f * Vector3.Dot(relPos, zielGeschw);
        float c = Vector3.Dot(relPos, relPos);

        float t = 0f;

        if (Mathf.Abs(a) < 0.0001f)
        {
            // Gegner etwa so schnell wie das Projektil -> lineare Naeherung
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

                // Kleinste positive Loesung waehlen
                t = Mathf.Min(t1, t2);
                if (t < 0f) t = Mathf.Max(t1, t2);
            }
        }

        if (t < 0f) t = 0f;   // kein sinnvoller Vorhalt -> auf aktuelle Position zielen

        return zielPos + zielGeschw * t;
    }

    private void RichteAufZiel()
    {
        if (target == null) return;   // kein Ziel -> Geschuetz bleibt, wo es war

        // Vorhalt-Punkt aus dem Frame-Cache (siehe HoleVorhaltPunkt).
        Vector3 vorhalt = HoleVorhaltPunkt();

        // Richtung zum VORHALT-Punkt (nicht zur aktuellen Gegnerposition)
        Vector3 richtung = vorhalt - transform.position;
        if (richtung.sqrMagnitude < 0.0001f) return;

        float winkel = Mathf.Atan2(richtung.y, richtung.x) * Mathf.Rad2Deg + blickrichtungOffset;

        // WELT-Rotation setzen, damit die Segment-Drehung (RichtungsWinkel)
        // die Ausrichtung NICHT mitverdreht.
        Quaternion ziel = Quaternion.Euler(0f, 0f, winkel);

        if (drehGeschwindigkeit <= 0f)
        {
            transform.rotation = ziel;   // sofort
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, ziel, drehGeschwindigkeit * Time.deltaTime);
        }
    }

    void Shoot()
    {
        // Vorhalt-Punkt aus dem Frame-Cache holen (gleicher Wert wie die Ausrichtung).
        Vector3 vorhalt = HoleVorhaltPunkt();

        // Schuss-Sound EINMAL pro Schuss (nicht pro Feuerpunkt), damit eine
        // Salve aus mehreren Firepoints nicht mehrfach gleichzeitig knallt.
        if (schussSound != null)
            SoundManager.Instance?.SpieleClip(schussSound, schussLautstaerke);

        // Alle aktiven Feuerpunkte ermitteln. Liste hat Vorrang; ist sie leer,
        // faellt es auf den einzelnen firePoint zurueck.
        if (firePoints != null && firePoints.Length > 0)
        {
            foreach (Transform fp in firePoints)
            {
                if (fp != null) FeuereVon(fp, vorhalt);
            }
        }
        else if (firePoint != null)
        {
            FeuereVon(firePoint, vorhalt);
        }
    }

    // Spawnt EIN Projektil an einem Feuerpunkt und schickt es gerade in
    // Richtung des Vorhalt-Punkts.
    private void FeuereVon(Transform fp, Vector3 vorhalt)
    {
        if (projectilePrefab == null) return;

        GameObject projectileGO = Instantiate(projectilePrefab, fp.position, fp.rotation);

        Projectiles projectile = projectileGO.GetComponent<Projectiles>();
        if (projectile != null)
        {
            // Schaden des Turms (ggf. durch Mutationsstufe skaliert) ans Projektil weitergeben
            projectile.damage = schaden;

            // Gerade Richtung zum vorgehaltenen Abfangpunkt (vom firePoint aus).
            Vector3 richtung = vorhalt - fp.position;
            projectile.SchiesseInRichtung(richtung);
        }
    }

    // Schuetzt vor unsinnigen Inspector-Werten (verhindert 0/negative Zeiten).
    void OnValidate()
    {
        schuessProBurst = Mathf.Max(1, schuessProBurst);
        taktImBurst = Mathf.Max(MIN_TAKT, taktImBurst);
        pauseZwischenBursts = Mathf.Max(MIN_PAUSE, pauseZwischenBursts);
        zielSuchIntervall = Mathf.Max(0f, zielSuchIntervall);
    }

    // Hilfreich im Unity Editor: Zeigt die Reichweite als rote Kugel an
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}