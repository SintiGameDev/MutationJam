using UnityEngine;

public class Projectiles : MonoBehaviour
{
    // Was passiert, wenn das anvisierte Ziel verschwindet (z.B. ein anderes
    // Projektil der Salve hat den Gegner schon getoetet)?
    public enum ZielVerloren
    {
        // A: In der zuletzt bekannten Richtung geradeaus weiterfliegen und
        //    nach kurzer Lebenszeit verschwinden (Schaden kann verpuffen).
        GeradeausWeiter,

        // B: Sofort den naechsten lebenden Gegner suchen und ihn anvisieren.
        //    Wird keiner gefunden, fliegt es geradeaus weiter (wie A).
        NeuesZielSuchen
    }

    private Transform target;

    [Header("Projektil-Werte")]
    public float speed = 10f;

    [Tooltip("Schaden, den dieses Projektil dem getroffenen Gegner zufuegt.")]
    public float damage = 10f;

    [Header("Verhalten bei verlorenem Ziel")]
    [Tooltip("A (GeradeausWeiter): fliegt in der letzten Richtung weiter.\n" +
             "B (NeuesZielSuchen): visiert den naechsten lebenden Gegner an.")]
    public ZielVerloren beiZielVerlust = ZielVerloren.NeuesZielSuchen;

    [Tooltip("Tag, nach dem bei Variante B (und beim Neusuchen) gesucht wird.")]
    public string enemyTag = "Enemy";

    [Tooltip("Lebenszeit in Sekunden, nachdem das Ziel verloren ist und das " +
             "Projektil nur noch geradeaus fliegt. Verhindert ewig fliegende Projektile.")]
    public float lebenszeitOhneZiel = 2f;

    [Header("Ausrichtung")]
    [Tooltip("Wohin das Sprite im Ruhezustand zeigt:\n" +
             "  -90 = Sprite-Spitze zeigt nach OBEN (+Y)\n" +
             "    0 = Spitze zeigt nach RECHTS (+X)\n" +
             "  180 = nach links,  90 = nach unten.")]
    public float blickrichtungOffset = -90f;

    // Zuletzt bekannte Flugrichtung – fuer das Geradeausfliegen ohne Ziel.
    private Vector3 letzteRichtung = Vector3.right;
    private bool hatZielVerloren = false;
    private float ohneZielTimer = 0f;

    public void Seek(Transform _target)
    {
        target = _target;
    }

    void LateUpdate()
    {
        float distanceThisFrame = speed * Time.deltaTime;

        // Ziel noch da? Richtung aktualisieren und ggf. treffen.
        if (target != null)
        {
            Vector3 dir = target.position - transform.position;

            if (dir.sqrMagnitude > 0.0001f)
                letzteRichtung = dir.normalized;

            if (dir.magnitude <= distanceThisFrame)
            {
                HitTarget();
                return;
            }
        }
        else
        {
            // Ziel weg -> je nach Einstellung reagieren.
            if (!hatZielVerloren)
            {
                hatZielVerloren = true;

                if (beiZielVerlust == ZielVerloren.NeuesZielSuchen)
                {
                    target = FindeNaechstesZiel();
                    if (target != null)
                    {
                        // Neues Ziel gefunden -> wie gewohnt weiter (kein Timer).
                        hatZielVerloren = false;
                    }
                }
            }

            // Kein (neues) Ziel: Timer hochzaehlen, dann verschwinden.
            if (target == null)
            {
                ohneZielTimer += Time.deltaTime;
                if (ohneZielTimer >= lebenszeitOhneZiel)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // Ausrichtung + Bewegung anhand der (ggf. zuletzt bekannten) Richtung.
        AusrichtenNach(letzteRichtung);
        transform.Translate(letzteRichtung * distanceThisFrame, Space.World);
    }

    private Transform FindeNaechstesZiel()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float shortest = Mathf.Infinity;
        Transform nearest = null;

        foreach (GameObject e in enemies)
        {
            if (e == null) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < shortest)
            {
                shortest = d;
                nearest = e.transform;
            }
        }

        return nearest;
    }

    private void AusrichtenNach(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        float winkel = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + blickrichtungOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, winkel);
    }

    void HitTarget()
    {
        if (target != null)
        {
            EnemyHealthManager leben = target.GetComponent<EnemyHealthManager>();
            if (leben != null)
            {
                leben.NimmSchaden(damage);
            }
        }
        Destroy(gameObject);
    }
}