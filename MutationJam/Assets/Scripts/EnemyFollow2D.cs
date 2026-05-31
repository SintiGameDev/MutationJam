using System.Collections;
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

    [Header("Ausrichtung")]
    [Tooltip("Wohin das Sprite im Ruhezustand zeigt (wie bei Projektilen)")]
    public float blickrichtungOffset = -90f;

    [Header("Visuelles Feedback")]
    public float flashDauer = 0.1f;

    private enum Zustand { Frei, Rueckstoss, Stun }
    private Zustand zustand = Zustand.Frei;

    private float timer;
    private Vector2 rueckstossRichtung;

    // Ueberlappte Koerper Segmente
    private readonly List<Transform> ueberlappendeSegmente = new List<Transform>();

    // Ueberlappter Kopf
    private Transform ueberlappenderKopf;
    private Snake kopfSnake;

    // Fuer das weisse Aufleuchten
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Material flashMaterial;

    void Start()
    {
        // Holt sich den SpriteRenderer (entweder auf diesem Objekt oder einem Kind Objekt)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
            // Dieser Shader macht das Sprite komplett weiss
            flashMaterial = new Material(Shader.Find("GUI/Text Shader"));
        }
    }

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

    // Oeffentliche Methode, die beim Treffer aufgerufen wird
    public void AufleuchtenLassen()
    {
        if (spriteRenderer != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.material = flashMaterial;
        yield return new WaitForSeconds(flashDauer);
        spriteRenderer.material = originalMaterial;
    }

    private void StarteRueckstoss(bool kopfVerwundbar)
    {
        Transform ziel = NaechstesUeberlapptesSegment();
        float zielDist = (ziel != null)
            ? Vector2.Distance(transform.position, ziel.position)
            : Mathf.Infinity;
        bool zielIstKopf = false;

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

            AusrichtenNach(-rueckstossRichtung); // Zum Ziel schauen waehrend des Rueckstosses
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
            // Bewegen
            transform.position = Vector2.MoveTowards(
                transform.position, ziel.position, speed * Time.deltaTime);

            // Ausrichten
            Vector2 richtung = (Vector2)ziel.position - (Vector2)transform.position;
            AusrichtenNach(richtung);
        }
    }

    private void AusrichtenNach(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        float winkel = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + blickrichtungOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, winkel);
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
        if (other.GetComponent<SnakeSegment>() != null)
        {
            if (!ueberlappendeSegmente.Contains(other.transform))
                ueberlappendeSegmente.Add(other.transform);
            return;
        }

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