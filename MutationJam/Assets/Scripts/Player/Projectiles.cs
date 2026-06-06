using UnityEngine;

// Projektil mit optionalem Homing (Steering-Force).
//
// Zwei Modi:
//   - Homing AN  : Der Turm gibt per SetzZiel() ein Transform mit. Das Projektil
//                  dreht seine Flugrichtung pro Frame sanft zum Ziel hin (max.
//                  lenkStaerke Grad/Sek). Stirbt das Ziel, fliegt das Projektil
//                  in seiner letzten Richtung weiter und laeuft nach lebenszeit ab.
//
//   - Homing AUS : Reiner Geradlinigflug. Richtung per SchiesseInRichtung() setzen
//                  (genau wie vorher – keine bestehenden Aufrufe brechen).
//
// 2.5D-Hinweis: Die Tiefen-Achse wird weiterhin konstant gehalten, damit das
// Projektil nicht aus der Gegner-Ebene herausdriftet.

public class Projectiles : MonoBehaviour
{
    [Header("Projektil-Werte")]
    public float speed = 10f;

    [Tooltip("Schaden, den dieses Projektil dem getroffenen Gegner zufuegt.")]
    public float damage = 10f;

    [Tooltip("Maximale Lebenszeit in Sekunden.")]
    public float lebenszeit = 3f;

    [Header("Kollision")]
    public string enemyTag = "Enemy";
    [Tooltip("Tag fuer Waende/Hindernisse. Projektil wird bei Beruehrung zerstoert (ohne Schaden).")]
    public string wallTag = "Wall";

    // -------------------------------------------------------------------------
    [Header("Homing")]
    [Tooltip("Homing aktivieren. Der Turm muss per SetzZiel() ein Target mitgeben.\n" +
             "Ist kein Ziel gesetzt oder stirbt es, fliegt das Projektil geradeaus weiter.")]
    public bool homingAktiv = true;

    [Tooltip("Maximale Lenkrate in Grad pro Sekunde.\n" +
             "  ~120 = spaet einschwenkend, Gegner koennen knapp entkommen (sanft)\n" +
             "  ~240 = zuverlaessig treffend, wirkt aber noch physikalisch (mittel)\n" +
             "  ~600 = dreht fast sofort nach, verfehlt nie (hart)\n" +
             "Empfehlung fuer 'Mittel': 200-260.")]
    public float lenkStaerke = 240f;

    // -------------------------------------------------------------------------
    public enum Tiefenachse { Z, Y, Keine }

    [Header("2.5D – Bewegung auf einer Ebene halten")]
    [Tooltip("Welche Achse die 'Tiefe' ist und konstant gehalten wird:\n" +
             "  Z     = Spielfeld in der XY-Ebene (Standard).\n" +
             "  Y     = Spielfeld in der XZ-Ebene (Boden).\n" +
             "  Keine = echtes 3D, keine Achse wird festgehalten.")]
    public Tiefenachse tiefenachse = Tiefenachse.Z;

    [Tooltip("Wenn an: Tiefen-Achse wird auf 'Feste Tiefe' gesetzt.")]
    public bool tiefeFixieren = true;

    [Tooltip("Fester Wert auf der Tiefen-Achse. Wird vom Turm automatisch gesetzt.")]
    public float festeTiefe = 0f;

    [Header("Ausrichtung")]
    [Tooltip("Wohin das Sprite im Ruhezustand zeigt:\n" +
             "  -90 = Spitze nach OBEN (+Y),  0 = nach RECHTS (+X).")]
    public float blickrichtungOffset = -90f;

    // -------------------------------------------------------------------------
    // Laufzeit-Zustand
    private Vector3   flugrichtung  = Vector3.right;
    private float     timer         = 0f;
    private bool      wirdZerstoert = false;
    private Transform ziel          = null;   // vom Turm gesetzt, kann null werden

    // =========================================================================
    // Oeffentliche API (wird vom Tower aufgerufen)
    // =========================================================================

    // Vom Turm aufgerufen: setzt das Homing-Ziel.
    // Kann jederzeit auch nach SchiesseInRichtung() aufgerufen werden.
    public void SetzZiel(Transform target)
    {
        ziel = target;
    }

    // Vom Turm aufgerufen: legt die Starttiefe auf die Ebene des anvisierten Punkts.
    public void SetzeTiefeAusWeltpunkt(Vector3 weltpunkt)
    {
        if      (tiefenachse == Tiefenachse.Z) festeTiefe = weltpunkt.z;
        else if (tiefenachse == Tiefenachse.Y) festeTiefe = weltpunkt.y;
        HalteAufEbene();
    }

    // Vom Turm aufgerufen: setzt die initiale Flugrichtung (Vorhalt-Richtung des Turms).
    // Beim Homing ist das nur die Startrichtung – die Lenkung uebernimmt danach.
    public void SchiesseInRichtung(Vector3 richtung)
    {
        richtung = ProjiziereAufEbene(richtung);
        if (richtung.sqrMagnitude > 0.0001f)
            flugrichtung = richtung.normalized;

        HalteAufEbene();
        AusrichtenNach(flugrichtung);
    }

    // =========================================================================
    // Update
    // =========================================================================

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lebenszeit)
        {
            ZerstoereSelbst();
            return;
        }

        // Ziel verloren (null oder Objekt zerstoert)?
        // -> einfach geradeaus weiterfliegen (flugrichtung bleibt unveraendert).
        bool hatLebendigesZiel = ziel != null;

        if (homingAktiv && hatLebendigesZiel)
        {
            LenkeZumZiel();
        }

        // Vorwaertsbewegung in aktueller flugrichtung
        transform.Translate(flugrichtung * speed * Time.deltaTime, Space.World);
        HalteAufEbene();
    }

    // =========================================================================
    // Homing-Lenkung
    // =========================================================================

    // Dreht flugrichtung pro Frame um maximal lenkStaerke Grad in Richtung Ziel.
    // Nutzt Vector3.RotateTowards – das ist einfaches Steering ohne Overshoot.
    private void LenkeZumZiel()
    {
        // Richtung zum aktuellen Ziel (Tiefen-Anteil entfernen, damit nur in der
        // Spielebene gelenkt wird – kein z-/y-Drift durch Hoehendifferenz).
        Vector3 zumZiel = ziel.position - transform.position;
        zumZiel = ProjiziereAufEbene(zumZiel);

        if (zumZiel.sqrMagnitude < 0.0001f) return;   // direkt auf dem Ziel

        zumZiel = zumZiel.normalized;

        // Maximale Drehung dieses Frames (Bogenmass fuer RotateTowards)
        float maxWinkelRad = lenkStaerke * Mathf.Deg2Rad * Time.deltaTime;

        flugrichtung = Vector3.RotateTowards(flugrichtung, zumZiel, maxWinkelRad, 0f);
        flugrichtung = ProjiziereAufEbene(flugrichtung).normalized;   // Tiefen-Drift nochmal sichern

        AusrichtenNach(flugrichtung);
    }

    // =========================================================================
    // Kollision
    // =========================================================================

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

    // =========================================================================
    // Hilfsmethoden (unveraendert)
    // =========================================================================

    private Vector3 ProjiziereAufEbene(Vector3 richtung)
    {
        if      (tiefenachse == Tiefenachse.Z) richtung.z = 0f;
        else if (tiefenachse == Tiefenachse.Y) richtung.y = 0f;
        return richtung;
    }

    private void HalteAufEbene()
    {
        if (!tiefeFixieren || tiefenachse == Tiefenachse.Keine) return;

        Vector3 p = transform.position;
        if      (tiefenachse == Tiefenachse.Z) p.z = festeTiefe;
        else if (tiefenachse == Tiefenachse.Y) p.y = festeTiefe;
        transform.position = p;
    }

    private void AusrichtenNach(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        float winkel = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + blickrichtungOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, winkel);
    }

    private void ZerstoereSelbst()
    {
        if (wirdZerstoert) return;
        wirdZerstoert = true;
        Destroy(gameObject);
    }
}
