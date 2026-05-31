using System.Collections;
using UnityEngine;

public class SnakeSegment : MonoBehaviour
{
    [Header("Gesundheit")]
    public float maxHealth = 3f;
    [Tooltip("Zusaetzliches Leben pro Mutationsstufe ueber Stufe 1. " +
             "Stufe 1 = maxHealth, Stufe 2 = maxHealth + 1x dieser Wert, usw.")]
    public float lebenProMutationsstufe = 2f;

    [Header("Schadens-Blitz")]
    [Tooltip("Kuerzeste Aufleucht-Dauer (bei vollem Leben).")]
    public float minBlitzDauer = 0.08f;
    [Tooltip("Laengste Aufleucht-Dauer (bei wenig Leben). Wird auf 0,5 s gedeckelt.")]
    public float maxBlitzDauer = 0.5f;

    public Nahrungstyp Typ { get; private set; }
    public Tower AktuellerTurm { get; private set; }
    public float AktuelleHealth { get; private set; }

    private float effektiveMaxHealth;

    public int Mutationsstufe { get; private set; } = 1;

    public GameObject StandardTurmPrefab { private get; set; }

    public GameObject StufenAnzeigePrefab { private get; set; }
    public Vector3 StufenAnzeigeOffset { private get; set; }
    public int AnzeigeAbStufe { private get; set; } = 2;
    public Canvas StufenAnzeigeCanvas { private get; set; }

    public event System.Action<SnakeSegment> OnSegmentGestorben;

    // Beide Renderer-Typen unterstuetzen: 2D-Sprite UND 3D-Mesh.
    private SpriteRenderer spriteRenderer;
    private MeshRenderer meshRenderer;
    private Color grundFarbeSprite = Color.white;
    private Color grundFarbeMesh = Color.white;

    private bool istAmSterben = false;
    private StufenAnzeige stufenAnzeige;
    private Coroutine blitzCoroutine;

    private void Awake()
    {
        ErmittleRenderer();

        effektiveMaxHealth = maxHealth;
        AktuelleHealth = effektiveMaxHealth;
    }

    // Sucht Sprite- und/oder Mesh-Renderer (eigenes Objekt oder Kind) und merkt
    // sich deren aktuelle Grundfarbe.
    private void ErmittleRenderer()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
        if (spriteRenderer != null)
        {
            grundFarbeSprite = spriteRenderer.color;
        }

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>(true);
        }
        if (meshRenderer != null && meshRenderer.material != null)
        {
            grundFarbeMesh = meshRenderer.material.color;
        }
    }

    public void SetzeTyp(Nahrungstyp typ, int stufe = 1)
    {
        Typ = typ;
        Mutationsstufe = Mathf.Max(1, stufe);

        effektiveMaxHealth = BerechneMaxHealth();
        AktuelleHealth = effektiveMaxHealth;

        if (typ == null) return;

        AktualisiereStufenAnzeige();

        // 2D-Sprite faerben (Renderer kann am Kind haengen)
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
        {
            sr.color = typ.farbe;
            spriteRenderer = sr;
            grundFarbeSprite = typ.farbe;
        }

        // 3D-Mesh: Material setzen und dessen Farbe als Grundfarbe merken
        if (typ.material != null)
        {
            MeshRenderer mr = GetComponentInChildren<MeshRenderer>(true);
            if (mr != null)
            {
                mr.material = typ.material;
                meshRenderer = mr;
                grundFarbeMesh = mr.material.color;
            }
        }

        SpawneTurm(typ);
    }

    private float BerechneMaxHealth()
    {
        return maxHealth + lebenProMutationsstufe * (Mutationsstufe - 1);
    }

    public void NimmSchaden(float schaden)
    {
        if (istAmSterben) return;

        AktuelleHealth -= schaden;

        // Vorherigen Blitz stoppen und Grundfarbe wiederherstellen
        if (blitzCoroutine != null)
        {
            StopCoroutine(blitzCoroutine);
            SetzeFarbe(false);
        }
        blitzCoroutine = StartCoroutine(SchadensBlitz());

        if (AktuelleHealth <= 0f)
        {
            Stirb();
        }
    }

    private IEnumerator SchadensBlitz()
    {
        float dauer = BerechneBlitzDauer();

        SetzeFarbe(true);   // weiss
        yield return new WaitForSeconds(dauer);
        SetzeFarbe(false);  // zurueck auf Grundfarbe
        blitzCoroutine = null;
    }

    // Faerbt beide Renderer (falls vorhanden) weiss bzw. zurueck auf Grundfarbe.
    private void SetzeFarbe(bool weiss)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = weiss ? Color.white : grundFarbeSprite;
        }
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = weiss ? Color.white : grundFarbeMesh;
        }
    }

    private float BerechneBlitzDauer()
    {
        float max = Mathf.Max(0.0001f, effektiveMaxHealth);
        float ratio = Mathf.Clamp01(AktuelleHealth / max);
        float dauer = Mathf.Lerp(minBlitzDauer, maxBlitzDauer, 1f - ratio);
        return Mathf.Min(dauer, 0.5f);
    }

    private void Stirb()
    {
        istAmSterben = true;

        if (AktuellerTurm != null)
        {
            AktuellerTurm.enabled = false;
        }

        OnSegmentGestorben?.Invoke(this);

        StartCoroutine(PopOutUndZerstoere());
    }

    private IEnumerator PopOutUndZerstoere()
    {
        float dauer = 0.2f;
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

    private void AktualisiereStufenAnzeige()
    {
        if (StufenAnzeigePrefab == null) return;

        if (Mutationsstufe < AnzeigeAbStufe)
        {
            if (stufenAnzeige != null) Destroy(stufenAnzeige.gameObject);
            return;
        }

        if (stufenAnzeige == null)
        {
            Transform parent = StufenAnzeigeCanvas != null ? StufenAnzeigeCanvas.transform : null;
            GameObject go = Instantiate(StufenAnzeigePrefab, parent);

            stufenAnzeige = go.GetComponent<StufenAnzeige>();
            if (stufenAnzeige == null) stufenAnzeige = go.AddComponent<StufenAnzeige>();

            stufenAnzeige.Initialisiere(transform, StufenAnzeigeOffset, Mutationsstufe);
        }
        else
        {
            stufenAnzeige.SetzeStufe(Mutationsstufe);
        }
    }

    private void SpawneTurm(Nahrungstyp typ)
    {
        TurmKonfiguration config = typ?.turmKonfiguration;
        GameObject prefabZuSpawnen = (config != null) ? config.turmPrefab : StandardTurmPrefab;

        if (prefabZuSpawnen == null) return;

        GameObject turmGO = Instantiate(prefabZuSpawnen, transform.position, Quaternion.identity, transform);
        turmGO.transform.localPosition = Vector3.zero;

        AktuellerTurm = turmGO.GetComponent<Tower>();

        WendeKonfigAn(config);
    }

    public void AktualisiereTurm(TurmKonfiguration neueKonfig)
    {
        if (AktuellerTurm != null)
        {
            Destroy(AktuellerTurm.gameObject);
            AktuellerTurm = null;
        }

        if (neueKonfig == null || neueKonfig.turmPrefab == null) return;

        GameObject turmGO = Instantiate(neueKonfig.turmPrefab, transform.position, Quaternion.identity, transform);
        turmGO.transform.localPosition = Vector3.zero;

        AktuellerTurm = turmGO.GetComponent<Tower>();

        WendeKonfigAn(neueKonfig);
    }

    private void WendeKonfigAn(TurmKonfiguration config)
    {
        if (AktuellerTurm == null || config == null) return;

        TurmKonfiguration.SkalierteWerte werte = config.BerechneWerte(Mutationsstufe);

        AktuellerTurm.range = werte.reichweite;
        AktuellerTurm.fireRate = werte.schussrate;
        AktuellerTurm.schaden = werte.schaden;

        if (config.projektilPrefab != null)
        {
            AktuellerTurm.projectilePrefab = config.projektilPrefab;
        }
    }
}