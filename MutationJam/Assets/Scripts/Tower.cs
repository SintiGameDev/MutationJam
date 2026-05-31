using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Turm-Werte")]
    public float range = 5f;        // Reichweite des Turms
    public float fireRate = 1f;     // Schuesse pro Sekunde

    [Tooltip("Schaden pro Projektil. Wird vom Segment je nach Mutationsstufe gesetzt " +
             "und beim Schuss auf das Projektil geschrieben.")]
    public float schaden = 10f;

    private float fireCountdown = 0f;

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
            return;

        // Cooldown-System fuer die Schussrate
        if (fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / fireRate;
        }

        fireCountdown -= Time.deltaTime;
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
        // Erstelle das Projektil am firePoint
        GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Hole das Projectiles-Skript vom erstellten Objekt
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