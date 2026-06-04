using UnityEngine;

// Geradlinig fliegendes Projektil (kein Homing). Der Turm berechnet beim
// Abschuss den Vorhalt-Punkt und gibt die feste Flugrichtung per SchiesseInRichtung()
// vor. Treffer per Collider (Tag Enemy) oder nach Ablauf der Lebenszeit.
//
// 2.5D-Hinweis: In einem 3D-Aufbau liegen Turm, Feuerpunkt und Gegner oft nicht
// exakt auf derselben Tiefe. Damit das Projektil nicht auf der Tiefen-Achse aus
// der Gegner-Ebene heraus driftet (und "darueber hinweg" fliegt), wird die
// Bewegung auf eine Ebene gezwungen: die Tiefen-Achse bleibt konstant.
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

    public enum Tiefenachse { Z, Y, Keine }

    [Header("2.5D – Bewegung auf einer Ebene halten")]
    [Tooltip("Welche Achse die 'Tiefe' ist und konstant gehalten wird:\n" +
             "  Z     = Spielfeld liegt in der XY-Ebene (Standard, passt zur Ausrichtung unten).\n" +
             "  Y     = Spielfeld liegt in der XZ-Ebene (Boden, Y = Hoehe).\n" +
             "  Keine = echtes 3D, keine Achse wird festgehalten (altes Verhalten).")]
    public Tiefenachse tiefenachse = Tiefenachse.Z;

    [Tooltip("Wenn an: die Tiefen-Achse wird auf 'Feste Tiefe' gesetzt, damit das " +
             "Projektil exakt in der Gegner-Ebene bleibt und nicht darueber hinweg fliegt.")]
    public bool tiefeFixieren = true;

    [Tooltip("Fester Wert auf der Tiefen-Achse (z.B. die Z- bzw. Y-Koordinate, auf der die " +
             "Gegner liegen). Der Turm setzt diesen Wert beim Abschuss automatisch auf die " +
             "Ebene des anvisierten Gegners – manuelles Setzen ist nur noetig, wenn das " +
             "Projektil ohne Turm benutzt wird.")]
    public float festeTiefe = 0f;

    [Header("Ausrichtung")]
    [Tooltip("Wohin das Sprite im Ruhezustand zeigt:\n" +
             "  -90 = Spitze nach OBEN (+Y),  0 = nach RECHTS (+X).")]
    public float blickrichtungOffset = -90f;

    private Vector3 flugrichtung = Vector3.right;
    private float timer = 0f;
    private bool wirdZerstoert = false;

    // Vom Turm beim Abschuss aufgerufen: setzt die feste Tiefe auf die Ebene des
    // anvisierten Weltpunkts (i.d.R. die Gegnerposition) und legt das Projektil
    // sofort auf diese Ebene. So spielt ein z-Versatz des Feuerpunkts keine Rolle.
    public void SetzeTiefeAusWeltpunkt(Vector3 weltpunkt)
    {
        if (tiefenachse == Tiefenachse.Z) festeTiefe = weltpunkt.z;
        else if (tiefenachse == Tiefenachse.Y) festeTiefe = weltpunkt.y;
        HalteAufEbene();
    }

    // Vom Turm beim Abschuss aufgerufen: feste, bereits vorgehaltene Richtung.
    public void SchiesseInRichtung(Vector3 richtung)
    {
        // Richtungs-Anteil auf der Tiefen-Achse entfernen -> kein Drift in die Tiefe.
        richtung = ProjiziereAufEbene(richtung);

        if (richtung.sqrMagnitude > 0.0001f)
            flugrichtung = richtung.normalized;

        HalteAufEbene();
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

        // Nach der Bewegung wieder exakt auf die Ebene ziehen (faengt Rundungs-
        // fehler und einen evtl. schon vorhandenen Tiefen-Versatz ab).
        HalteAufEbene();
    }

    // Entfernt den Anteil des Vektors auf der Tiefen-Achse.
    private Vector3 ProjiziereAufEbene(Vector3 richtung)
    {
        if (tiefenachse == Tiefenachse.Z) richtung.z = 0f;
        else if (tiefenachse == Tiefenachse.Y) richtung.y = 0f;
        return richtung;
    }

    // Setzt die Tiefen-Koordinate des Projektils auf 'festeTiefe'.
    private void HalteAufEbene()
    {
        if (!tiefeFixieren || tiefenachse == Tiefenachse.Keine) return;

        Vector3 p = transform.position;
        if (tiefenachse == Tiefenachse.Z) p.z = festeTiefe;
        else if (tiefenachse == Tiefenachse.Y) p.y = festeTiefe;
        transform.position = p;
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