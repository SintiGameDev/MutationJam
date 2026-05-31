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

    // Burst-Zustand
    private int   schussImBurst = 0;     // wie viele Schuesse der aktuellen Salve schon raus sind
    private float naechsterSchuss = 0f;  // Zeitpunkt (Time.time), ab dem wieder gefeuert werden darf

    [Header("Unity Setup")]
    public string enemyTag = "Enemy";

    [Tooltip("Einzelner Feuerpunkt (Fallback). Wird nur benutzt, wenn 'Fire Points' " +
             "leer ist – so bleiben bestehende Prefabs ohne Aenderung lauffaehig.")]
    public Transform firePoint;

    [Tooltip("Mehrere Feuerpunkte. Bei einem Schuss feuern ALLE gleichzeitig auf das " +
             "anvisierte Ziel. Ist die Liste leer, wird 'Fire Point' oben benutzt.")]
    public Transform[] firePoints;

    public GameObject projectilePrefab;

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

    void Start()
    {
        // Sucht zweimal pro Sekunde nach einem Ziel, um die Performance zu schonen
        InvokeRepeating("UpdateTarget", 0f, 0.5f);
    }

    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        // Wenn ein Gegner in Reichweite gefunden wurde, setze ihn als Ziel
        if (nearestEnemy != null && shortestDistance <= range)
        {
            target = nearestEnemy.transform;
        }
        else
        {
            target = null;
        }
    }

    void Update()
    {
        if (target == null)
        {
            // Ohne Ziel: laufende Salve abbrechen, naechste startet frisch beim
            // naechsten Ziel (kein Mid-Burst-Rest, der sich seltsam anfuehlt).
            schussImBurst = 0;
            return;
        }

        if (Time.time < naechsterSchuss)
            return;

        // Ein Schuss der aktuellen Salve
        Shoot();
        schussImBurst++;

        if (schussImBurst >= Mathf.Max(1, schuessProBurst))
        {
            // Salve fertig -> lange Pause, dann neue Salve
            schussImBurst = 0;
            naechsterSchuss = Time.time + pauseZwischenBursts;
        }
        else
        {
            // Naechster Schuss innerhalb der Salve -> kurzer Takt
            naechsterSchuss = Time.time + taktImBurst;
        }
    }

    // Ausrichtung NACH der Segment-Bewegung (LeanTween laeuft im Update),
    // damit auf die finale Position dieses Frames gezielt wird.
    void LateUpdate()
    {
        RichteAufZiel();
    }

    private void RichteAufZiel()
    {
        if (target == null) return;   // kein Ziel -> Geschuetz bleibt, wo es war

        // Richtung in der XY-Ebene (2D-Spielfeld)
        Vector3 richtung = target.position - transform.position;
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
                if (fp != null) FeuereVon(fp);
            }
        }
        else if (firePoint != null)
        {
            FeuereVon(firePoint);
        }
    }

    // Spawnt EIN Projektil an einem Feuerpunkt und schickt es auf das Ziel.
    private void FeuereVon(Transform fp)
    {
        if (projectilePrefab == null) return;

        GameObject projectileGO = Instantiate(projectilePrefab, fp.position, fp.rotation);

        Projectiles projectile = projectileGO.GetComponent<Projectiles>();
        if (projectile != null)
        {
            // Schaden des Turms (ggf. durch Mutationsstufe skaliert) ans Projektil weitergeben
            projectile.damage = schaden;
            projectile.Seek(target);
        }
    }

    // Hilfreich im Unity Editor: Zeigt die Reichweite als rote Kugel an
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
