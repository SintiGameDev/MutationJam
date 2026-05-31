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

    // Effektives Maximum nach Skalierung mit der Mutationsstufe.
    private float effektiveMaxHealth;

    // Mutationsstufe dieses Segments (1 = frisch gefressen, kein Bonus).
    public int Mutationsstufe { get; private set; } = 1;

    public GameObject StandardTurmPrefab { private get; set; }

    public GameObject StufenAnzeigePrefab { private get; set; }
    public Vector3 StufenAnzeigeOffset { private get; set; }
    public int AnzeigeAbStufe { private get; set; } = 2;
    public Canvas StufenAnzeigeCanvas { private get; set; }

    public event System.Action<SnakeSegment> OnSegmentGestorben;

    private SpriteRenderer spriteRenderer;
    private bool istAmSterben = false;
    private StufenAnzeige stufenAnzeige;

    // Grundfarbe (zum Zuruecksetzen nach dem Blitz) und laufende Blitz-Coroutine.
    private Color grundFarbe = Color.white;
    private Coroutine blitzCoroutine;

    private void Awake()
    {
        // Renderer auf diesem Objekt ODER einem Kind (Sprite kann am Child haengen)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
        if (spriteRenderer != null)
        {
            grundFarbe = spriteRenderer.color;
        }

        effektiveMaxHealth = maxHealth;
        AktuelleHealth = effektiveMaxHealth;
    }

    public void SetzeTyp(Nahrungstyp typ, int stufe = 1)
    {
        Typ = typ;
        Mutationsstufe = Mathf.Max(1, stufe);

        // Leben je nach Mutationsstufe (Turmlevel) neu berechnen und auffuellen.
        effektiveMaxHealth = BerechneMaxHealth();
        AktuelleHealth = effektiveMaxHealth;

        // Start-Segmente (typlos) bekommen WEDER Turm NOCH Stufenanzeige.
        if (typ == null) return;

        AktualisiereStufenAnzeige();

        // 2D-Sprite faerben – auch wenn der SpriteRenderer-Child inaktiv ist
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
        {
            sr.color = typ.farbe;
            spriteRenderer = sr;        // Blitz nutzt denselben Renderer
            grundFarbe = typ.farbe;     // Grundfarbe fuer das Zuruecksetzen merken
        }

        // 3D-Sphere: Material des ersten MeshRenderer-Child setzen
        if (typ.material != null)
        {
            MeshRenderer mr = GetComponentInChildren<MeshRenderer>(true);
            if (mr != null) mr.material = typ.material;
        }

        SpawneTurm(typ);
    }

    // Leben = Basis + Bonus * (Stufe - 1)
    private float BerechneMaxHealth()
    {
        return maxHealth + lebenProMutationsstufe * (Mutationsstufe - 1);
    }

    // Wird vom Gegner bei Kollision aufgerufen
    public void NimmSchaden(float schaden)
    {
        if (istAmSterben) return;

        AktuelleHealth -= schaden;

        // Vorherigen Blitz stoppen und Farbe sichern, damit sich Blitze nicht
        // gegenseitig die Grundfarbe "wegfressen".
        if (blitzCoroutine != null)
        {
            StopCoroutine(blitzCoroutine);
            if (spriteRenderer != null) spriteRenderer.color = grundFarbe;
        }
        blitzCoroutine = StartCoroutine(SchadensBlitz());

        if (AktuelleHealth <= 0f)
        {
            Stirb();
        }
    }

    private IEnumerator SchadensBlitz()
    {
        if (spriteRenderer == null) yield break;

        float dauer = BerechneBlitzDauer();

        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(dauer);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = grundFarbe;
        }
        blitzCoroutine = null;
    }

    // Viel (Rest-)Leben -> kurzer Blitz; wenig Leben -> langer Blitz (max 0,5 s).
    private float BerechneBlitzDauer()
    {
        float max = Mathf.Max(0.0001f, effektiveMaxHealth);
        float ratio = Mathf.Clamp01(AktuelleHealth / max);  // 1 = voll, 0 = leer

        // Bei vollem Leben minBlitzDauer, bei leerem Leben maxBlitzDauer.
        float dauer = Mathf.Lerp(minBlitzDauer, maxBlitzDauer, 1f - ratio);

        // Harte Obergrenze 0,5 s
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