using System.Collections.Generic;
using UnityEngine;
public class EnemyFollow2D : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]
    public float speed = 3f;

    [Header("Kampf")]
    [Tooltip("Schaden, den der Gegner pro Anrempeln an ein Segment macht.")]
    public float schadenAnSegment = 1f;

    [Header("Rueckstoss")]
    [Tooltip("Wie weit (in Einheiten) der Gegner beim Treffer zurueckgeschoben wird.")]
    public float rueckstossDistanz = 1.5f;
    [Tooltip("Dauer (Sekunden) der Rueckstoss-Bewegung. Kurz halten fuer einen knackigen Stoss.")]
    public float rueckstossDauer = 0.15f;
    [Tooltip("Dauer (Sekunden), die der Gegner NACH dem Rueckstoss betaeubt stillsteht.")]
    public float stunDauer = 0.4f;

    // Drei Phasen: frei suchend, im Rueckstoss, oder betaeubt.
    private enum Zustand { Frei, Rueckstoss, Stun }
    private Zustand zustand = Zustand.Frei;

    private float timer;                 // Restzeit der aktuellen Phase
    private Vector2 rueckstossRichtung;  // Richtung, in die gestossen wird

    // Segmente, die der Gegner gerade ueberlappt (ueber Enter/Exit gepflegt).
    private readonly List<Transform> ueberlappendeSegmente = new List<Transform>();

    void Update()
    {
        // Zerstoerte (null) Eintraege entfernen – z.B. durch Matches/Tod geloeschte Segmente
        ueberlappendeSegmente.RemoveAll(s => s == null);

        switch (zustand)
        {
            case Zustand.Frei:
                // Ueberlappt der Gegner ein Segment? -> Schaden + Rueckstoss ausloesen
                if (ueberlappendeSegmente.Count > 0)
                {
                    StarteRueckstoss();
                }
                else
                {
                    BewegeZuZiel();
                }
                break;

            case Zustand.Rueckstoss:
                float geschw = rueckstossDistanz / Mathf.Max(0.0001f, rueckstossDauer);
                transform.position += (Vector3)(rueckstossRichtung * geschw * Time.deltaTime);

                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    zustand = Zustand.Stun;
                    timer = stunDauer;
                }
                break;

            case Zustand.Stun:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    zustand = Zustand.Frei;
                }
                break;
        }
    }

    // Macht Schaden am getroffenen Segment und loest dann den Rueckstoss aus.
    private void StarteRueckstoss()
    {
        Transform naechstes = NaechstesUeberlapptesSegment();

        // Schaden zufuegen (einmal pro Anrempeln)
        if (naechstes != null)
        {
            SnakeSegment segment = naechstes.GetComponent<SnakeSegment>();
            if (segment != null)
            {
                segment.NimmSchaden(schadenAnSegment);
            }
        }

        Vector2 richtung = (naechstes != null)
            ? (Vector2)transform.position - (Vector2)naechstes.position
            : Vector2.zero;

        // Exakte Ueberlappung -> zufaellige Richtung, damit es eine Wegrichtung gibt
        rueckstossRichtung = (richtung.sqrMagnitude < 0.0001f)
            ? Random.insideUnitCircle.normalized
            : richtung.normalized;

        zustand = Zustand.Rueckstoss;
        timer = rueckstossDauer;
    }

    private void BewegeZuZiel()
    {
        Transform ziel = FindeZiel();
        if (ziel != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, ziel.position, speed * Time.deltaTime);
        }
    }

    private Transform FindeZiel()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform;
        }
        return FindeNaechstesSegment();
    }

    private Transform FindeNaechstesSegment()
    {
        SnakeSegment[] segmente = FindObjectsOfType<SnakeSegment>();
        Transform naechstes = null;
        float kuerzesteDistanz = Mathf.Infinity;
        foreach (SnakeSegment seg in segmente)
        {
            if (seg == null)
            {
                continue;
            }
            float distanz = Vector2.Distance(transform.position, seg.transform.position);
            if (distanz < kuerzesteDistanz)
            {
                kuerzesteDistanz = distanz;
                naechstes = seg.transform;
            }
        }
        return naechstes;
    }

    private Transform NaechstesUeberlapptesSegment()
    {
        Transform naechstes = null;
        float kuerzesteDistanz = Mathf.Infinity;
        foreach (Transform seg in ueberlappendeSegmente)
        {
            if (seg == null)
            {
                continue;
            }
            float distanz = Vector2.Distance(transform.position, seg.position);
            if (distanz < kuerzesteDistanz)
            {
                kuerzesteDistanz = distanz;
                naechstes = seg;
            }
        }
        return naechstes;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SnakeSegment>() != null &&
            !ueberlappendeSegmente.Contains(other.transform))
        {
            ueberlappendeSegmente.Add(other.transform);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<SnakeSegment>() != null)
        {
            ueberlappendeSegmente.Remove(other.transform);
        }
    }
}