using UnityEngine;
public class EnemyFollow2D : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]
    public float speed = 3f;

    // Kein Caching in Start mehr: Der Spieler-Kopf bewegt sich, Segmente
    // koennen verschwinden (Matches). Deshalb wird das Ziel jeden Frame neu
    // bestimmt.
    void Update()
    {
        Transform ziel = FindeZiel();

        if (ziel != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, ziel.position, speed * Time.deltaTime);
        }
    }

    // Priorisierung: Spieler (Kopf) zuerst, sonst naechstes SnakeSegment.
    private Transform FindeZiel()
    {
        // 1. Spieler (Schlangenkopf) bevorzugen
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform;
        }

        // 2. Sonst das naechstgelegene Segment ansteuern
        return FindeNaechstesSegment();
    }

    // Sucht das raeumlich naechste SnakeSegment in der Szene.
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
}