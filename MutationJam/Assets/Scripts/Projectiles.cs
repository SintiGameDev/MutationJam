using UnityEngine;

// Geradlinig fliegendes Projektil (kein Homing). Der Turm berechnet beim
// Abschuss den Vorhalt-Punkt und gibt die feste Flugrichtung per SchiesseInRichtung()
// vor. Treffer per Collider (Tag Enemy) oder nach Ablauf der Lebenszeit.
public class Projectiles : MonoBehaviour
{
    [Header("Projektil-Werte")]
    public float speed = 10f;

    [Tooltip("Schaden, den dieses Projektil dem getroffenen Gegner zufuegt.")]
    public float damage = 10f;

    [Tooltip("Maximale Lebenszeit in Sekunden, danach zerstoert es sich selbst " +
             "(falls es nichts trifft).")]
    public float lebenszeit = 3f;

    [Header("Kollision")]
    public string enemyTag = "Enemy";
    [Tooltip("Tag fuer Waende/Hindernisse. Projektil wird bei Beruehrung zerstoert (ohne Schaden).")]
    public string wallTag = "Wall";

    [Header("Ausrichtung")]
    [Tooltip("Wohin das Sprite im Ruhezustand zeigt:\n" +
             "  -90 = Spitze nach OBEN (+Y),  0 = nach RECHTS (+X).")]
    public float blickrichtungOffset = -90f;

    private Vector3 flugrichtung = Vector3.right;
    private float   timer = 0f;
    private bool    wirdZerstoert = false;

    // Vom Turm beim Abschuss aufgerufen: feste, bereits vorgehaltene Richtung.
    public void SchiesseInRichtung(Vector3 richtung)
    {
        if (richtung.sqrMagnitude > 0.0001f)
            flugrichtung = richtung.normalized;

        AusrichtenNach(flugrichtung);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lebenszeit)
        {
            ZerstoereSelbst();
            return;
        }

        transform.Translate(flugrichtung * speed * Time.deltaTime, Space.World);
    }

    private void AusrichtenNach(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        float winkel = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + blickrichtungOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, winkel);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(enemyTag))
        {
            EnemyHealthManager leben = other.GetComponent<EnemyHealthManager>();
            if (leben != null) leben.NimmSchaden(damage);
            ZerstoereSelbst();
        }
        else if (other.CompareTag(wallTag))
        {
            ZerstoereSelbst();
        }
    }

    private void ZerstoereSelbst()
    {
        if (wirdZerstoert) return;
        wirdZerstoert = true;
        Destroy(gameObject);
    }
}
