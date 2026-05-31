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
    public Transform firePoint;     // Der Punkt, an dem das Projektil spawnt
    public GameObject projectilePrefab;

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
