using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFollow2D : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]
    public float minSpeed = 2f;
    public float maxSpeed = 4f;
    private float currentSpeed;

    [Header("Kampf")]
    [Tooltip("Schaden, den der Gegner pro Anrempeln an ein Segment ODER den (allein stehenden) Kopf macht.")]
    public float schadenAnSegment = 1f;

    [Header("Rueckstoss")]
    public float rueckstossDistanz = 1.5f;
    public float rueckstossDauer = 0.15f;
    public float stunDauer = 0.4f;

    [Header("Haltedistanz")]
    [Tooltip("Distanz zur Schlange, ab der der Gegner abbremst und stoppt.")]
    public float haltDistanz = 3f;
    [Tooltip("Distanz zur Schlange, ab der der Gegner wieder anfaehrt (Hysterese, > haltDistanz setzen).")]
    public float weiterfahrDistanz = 4f;
    [Tooltip("Wie weich gebremst und angefahren wird. 0 = sofort, hoeher = traeger.")]
    [Range(1f, 20f)]
    public float bremsTraegheit = 8f;

    [Header("Ausrichtung")]
    [Tooltip("Wohin das Sprite im Ruhezustand zeigt. Bei rechtsgerichtetem Sprite meist 0 oder -180.")]
    public float blickrichtungOffset = 0f;

    [Header("Visuelles Feedback")]
    public float flashDauer = 0.1f;
    [Tooltip("Material, das beim Treffer kurz angezeigt wird (z.B. weisses Sprite-Material). Leer lassen = kein Flash.")]
    public Material flashMaterial;

    private enum Zustand { Frei, Gestoppt, Rueckstoss, Stun }
    private Zustand zustand = Zustand.Frei;

    private float timer;
    private Vector2 rueckstossRichtung;
    private float aktuelleTempo = 0f;   // wird weich interpoliert

    // Ueberlappte Koerper Segmente
    private readonly List<Transform> ueberlappendeSegmente = new List<Transform>();

    // Ueberlappter Kopf
    private Transform ueberlappenderKopf;
    private Snake kopfSnake;

    // Fuer das Aufleuchten
    private MeshRenderer meshRenderer;
    private Material originalMaterial;

    void Start()
    {
        currentSpeed = Random.Range(minSpeed, maxSpeed);
        aktuelleTempo = currentSpeed;

        GameObject body = null;
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag("EnemyBody"))
            {
                body = child.gameObject;
                break;
            }
        }
        if (body != null)
        {
            meshRenderer = body.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                originalMaterial = meshRenderer.material;
        }

        Transform ziel = FindeZiel();
        if (ziel != null)
        {
            Vector2 richtung = (Vector2)ziel.position - (Vector2)transform.position;
            AusrichtenNach(richtung);
        }
    }

    void Update()
    {
        ueberlappendeSegmente.RemoveAll(s => s == null);

        switch (zustand)
        {
            case Zustand.Frei:
            case Zustand.Gestoppt:
                bool kopfVerwundbar = kopfSnake != null
                                      && ueberlappenderKopf != null
                                      && kopfSnake.NurKopfUebrig;

                if (ueberlappendeSegmente.Count > 0 || kopfVerwundbar)
                {
                    StarteRueckstoss(kopfVerwundbar);
                    break;
                }

                AktualisiereHalteZustand();
                BewegeZuZiel();
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
                    zustand = Zustand.Frei;
                break;
        }
    }

    // Prueft die Distanz zur Schlange und wechselt zwischen Frei/Gestoppt.
    // Hysterese: erst bei haltDistanz stoppen, erst bei weiterfahrDistanz wieder losfahren.
    private void AktualisiereHalteZustand()
    {
        Transform ziel = FindeZiel();
        if (ziel == null) return;

        float distanz = Vector2.Distance(transform.position, ziel.position);

        if (zustand == Zustand.Frei && distanz <= haltDistanz)
        {
            zustand = Zustand.Gestoppt;
        }
        else if (zustand == Zustand.Gestoppt && distanz > weiterfahrDistanz)
        {
            zustand = Zustand.Frei;
        }
    }

    private void BewegeZuZiel()
    {
        Transform ziel = FindeZiel();
        if (ziel == null) return;

        // Zieltempo: 0 wenn gestoppt, currentSpeed wenn frei
        float zielTempo = (zustand == Zustand.Gestoppt) ? 0f : currentSpeed;

        // Sanft interpolieren (abbremsen und anfahren)
        aktuelleTempo = Mathf.Lerp(aktuelleTempo, zielTempo, bremsTraegheit * Time.deltaTime);

        // Untergrenze: bei praktisch 0 ganz aufhoeren (kein endloses Kriechen)
        if (aktuelleTempo < 0.01f) aktuelleTempo = 0f;

        Vector3 zielPositionMitZ = new Vector3(ziel.position.x, ziel.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, zielPositionMitZ, aktuelleTempo * Time.deltaTime);

        Vector2 richtung = (Vector2)ziel.position - (Vector2)transform.position;
        AusrichtenNach(richtung);
    }

    public void AufleuchtenLassen()
    {
        if (meshRenderer != null && flashMaterial != null && gameObject.activeInHierarchy)
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        meshRenderer.material = flashMaterial;
        yield return new WaitForSeconds(flashDauer);
        meshRenderer.material = originalMaterial;
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
                kopfSnake.KopfNimmtSchaden(schadenAnSegment);
            else
            {
                SnakeSegment segment = ziel.GetComponent<SnakeSegment>();
                if (segment != null) segment.NimmSchaden(schadenAnSegment);
            }

            Vector2 richtung = (Vector2)transform.position - (Vector2)ziel.position;
            rueckstossRichtung = (richtung.sqrMagnitude < 0.0001f)
                ? Random.insideUnitCircle.normalized
                : richtung.normalized;

            AusrichtenNach(-rueckstossRichtung);
        }
        else
        {
            rueckstossRichtung = Random.insideUnitCircle.normalized;
        }

        zustand = Zustand.Rueckstoss;
        timer = rueckstossDauer;
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
            return player.transform;
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
