using System.Collections;
using UnityEngine;

public class SnakeSegment : MonoBehaviour
{
    [Header("Gesundheit")]
    public float maxHealth = 3f;

    public Nahrungstyp Typ          { get; private set; }
    public Tower AktuellerTurm      { get; private set; }
    public float AktuelleHealth     { get; private set; }

    // Wird von Snake.Grow() gesetzt, bevor SetzeTyp() aufgerufen wird.
    public GameObject StandardTurmPrefab { private get; set; }

    // Snake lauscht hierauf, um das Segment aus der Liste zu entfernen
    public event System.Action<SnakeSegment> OnSegmentGestorben;

    private SpriteRenderer spriteRenderer;
    private bool istAmSterben = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        AktuelleHealth = maxHealth;
    }

    public void SetzeTyp(Nahrungstyp typ)
    {
        Typ = typ;

        if (spriteRenderer != null && typ != null) {
            spriteRenderer.color = typ.farbe;
        }

        SpawneTurm(typ);
    }

    // Wird vom Gegner bei Kollision aufgerufen
    public void NimmSchaden(float schaden)
    {
        if (istAmSterben) return;

        AktuelleHealth -= schaden;

        // Kurzes Aufblinken als visuelles Feedback
        StartCoroutine(SchadensBlitz());

        if (AktuelleHealth <= 0f)
        {
            Stirb();
        }
    }

    private IEnumerator SchadensBlitz()
    {
        if (spriteRenderer == null) yield break;

        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (spriteRenderer != null) {
            spriteRenderer.color = original;
        }
    }

    private void Stirb()
    {
        istAmSterben = true;

        // Turm sofort deaktivieren damit er nicht mehr schiesst
        if (AktuellerTurm != null) {
            AktuellerTurm.enabled = false;
        }

        // Snake Bescheid geben → entfernt das Segment aus der Liste
        OnSegmentGestorben?.Invoke(this);

        StartCoroutine(PopOutUndZerstoere());
    }

    private IEnumerator PopOutUndZerstoere()
    {
        float dauer   = 0.2f;
        float elapsed = 0f;
        Vector3 start = transform.localScale;

        while (elapsed < dauer)
        {
            elapsed += Time.deltaTime;
            if (this == null) yield break;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, elapsed / dauer);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SpawneTurm(Nahrungstyp typ)
    {
        TurmKonfiguration config   = typ?.turmKonfiguration;
        GameObject prefabZuSpawnen = (config != null) ? config.turmPrefab : StandardTurmPrefab;

        if (prefabZuSpawnen == null) return;

        GameObject turmGO = Instantiate(prefabZuSpawnen, transform.position, Quaternion.identity, transform);
        turmGO.transform.localPosition = Vector3.zero;

        AktuellerTurm = turmGO.GetComponent<Tower>();

        if (AktuellerTurm != null && config != null)
        {
            AktuellerTurm.range    = config.reichweite;
            AktuellerTurm.fireRate = config.schussrate;
            if (config.projektilPrefab != null) {
                AktuellerTurm.projectilePrefab = config.projektilPrefab;
            }
        }
    }

    public void AktualisiereTurm(TurmKonfiguration neueKonfig)
    {
        if (AktuellerTurm != null) {
            Destroy(AktuellerTurm.gameObject);
            AktuellerTurm = null;
        }

        if (neueKonfig == null || neueKonfig.turmPrefab == null) return;

        GameObject turmGO = Instantiate(neueKonfig.turmPrefab, transform.position, Quaternion.identity, transform);
        turmGO.transform.localPosition = Vector3.zero;

        AktuellerTurm = turmGO.GetComponent<Tower>();

        if (AktuellerTurm != null)
        {
            AktuellerTurm.range    = neueKonfig.reichweite;
            AktuellerTurm.fireRate = neueKonfig.schussrate;
        }
    }
}
