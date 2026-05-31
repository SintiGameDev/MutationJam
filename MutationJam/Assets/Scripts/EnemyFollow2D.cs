using System.Collections.Generic;
using UnityEngine;
public class EnemyFollow2D : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]
    public float speed = 3f;

    [Header("Kampf")]
    [Tooltip("Schaden, den der Gegner pro Anrempeln an ein Segment ODER den (allein stehenden) Kopf macht.")]
    public float schadenAnSegment = 1f;

    [Header("Rueckstoss")]
    public float rueckstossDistanz = 1.5f;
    public float rueckstossDauer = 0.15f;
    public float stunDauer = 0.4f;

    private enum Zustand { Frei, Rueckstoss, Stun }
    private Zustand zustand = Zustand.Frei;

    private float timer;
    private Vector2 rueckstossRichtung;

    // Ueberlappte Koerper-Segmente
    private readonly List<Transform> ueberlappendeSegmente = new List<Transform>();

    // Ueberlappter Kopf (nur angreifbar, wenn die Schlange keine Segmente mehr hat)
    private Transform ueberlappenderKopf;
    private Snake kopfSnake;

    void Update()
    {
        ueberlappendeSegmente.RemoveAll(s => s == null);

        switch (zustand)
        {
            case Zustand.Frei:
                bool kopfVerwundbar = kopfSnake != null
                                      && ueberlappenderKopf != null
                                      && kopfSnake.NurKopfUebrig;

                if (ueberlappendeSegmente.Count > 0 || kopfVerwundbar)
                {
                    StarteRueckstoss(kopfVerwundbar);
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

    // Macht Schaden am naechsten Ziel (Segment ODER Kopf) und loest Rueckstoss aus.
    private void StarteRueckstoss(bool kopfVerwundbar)
    {
        Transform ziel = NaechstesUeberlapptesSegment();
        float zielDist = (ziel != null)
            ? Vector2.Distance(transform.position, ziel.position)
            : Mathf.Infinity;
        bool zielIstKopf = false;

        // Kopf als Ziel beruecksichtigen, falls verwundbar und naeher
        if (kopfVerwundbar && ueberlappenderKopf != null)
        {
            float kopfDist = Vector2.Distance(transform.position, ueberlappenderKopf.position);
            if (kopfDist < zielDist)
            {
                ziel = ueberlappenderKopf;
                zielIstKopf = true;
            }
        }

        if (ziel != null)
        {
            if (zielIstKopf)
            {
                kopfSnake.KopfNimmtSchaden(schadenAnSegment);
            }
            else
            {
                SnakeSegment segment = ziel.GetComponent<SnakeSegment>();
                if (segment != null) segment.NimmSchaden(schadenAnSegment);
            }

            Vector2 richtung = (Vector2)transform.position - (Vector2)ziel.position;
            rueckstossRichtung = (richtung.sqrMagnitude < 0.0001f)
                ? Random.insideUnitCircle.normalized
                : richtung.normalized;
        }
        else
        {
            rueckstossRichtung = Random.insideUnitCircle.normalized;
        }

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
            if (seg == null) continue;
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
            if (seg == null) continue;
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
        // Koerper-Segment
        if (other.GetComponent<SnakeSegment>() != null)
        {
            if (!ueberlappendeSegmente.Contains(other.transform))
                ueberlappendeSegmente.Add(other.transform);
            return;
        }

        // Kopf (Snake-Komponente)
        Snake snake = other.GetComponent<Snake>();
        if (snake != null)
        {
            ueberlappenderKopf = other.transform;
            kopfSnake = snake;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<SnakeSegment>() != null)
        {
            ueberlappendeSegmente.Remove(other.transform);
            return;
        }

        if (other.transform == ueberlappenderKopf)
        {
            ueberlappenderKopf = null;
            kopfSnake = null;
        }
    }
}