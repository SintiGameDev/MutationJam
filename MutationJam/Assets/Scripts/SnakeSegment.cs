using System.Collections;
using UnityEngine;

public class SnakeSegment : MonoBehaviour
{
    [Header("Gesundheit")]
    public float maxHealth = 3f;

    public Nahrungstyp Typ          { get; private set; }
    public Tower AktuellerTurm      { get; private set; }
    public float AktuelleHealth     { get; private set; }

    // Mutationsstufe dieses Segments (1 = frisch gefressen, kein Bonus).
    // Hoehere Stufen skalieren die Turmwerte ueber TurmKonfiguration.BerechneWerte().
    public int Mutationsstufe       { get; private set; } = 1;

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

    // stufe optional: alte Aufrufe (SetzeTyp(typ)) bleiben gueltig und liefern Stufe 1.
    public void SetzeTyp(Nahrungstyp typ, int stufe = 1)
    {
        Typ            = typ;
        Mutationsstufe = Mathf.Max(1, stufe);

        if (typ == null) { SpawneTurm(typ); return; }

        // 2D-Sprite faerben – auch wenn der SpriteRenderer-Child inaktiv ist
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) sr.color = typ.farbe;

        // 3D-Sphere: Material des ersten MeshRenderer-Child setzen
        if (typ.material != null)
        {
            MeshRenderer mr = GetComponentInChildren<MeshRenderer>(true);
            if (mr != null) mr.material = typ.material;
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

        WendeKonfigAn(config);
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

        WendeKonfigAn(neueKonfig);
    }

    // Schreibt die – je nach Mutationsstufe skalierten – Werte auf den aktuellen Turm.
    private void WendeKonfigAn(TurmKonfiguration config)
    {
        if (AktuellerTurm == null || config == null) return;

        TurmKonfiguration.SkalierteWerte werte = config.BerechneWerte(Mutationsstufe);

        AktuellerTurm.range    = werte.reichweite;
        AktuellerTurm.fireRate = werte.schussrate;
        AktuellerTurm.schaden  = werte.schaden;

        if (config.projektilPrefab != null) {
            AktuellerTurm.projectilePrefab = config.projektilPrefab;
        }
    }
}
