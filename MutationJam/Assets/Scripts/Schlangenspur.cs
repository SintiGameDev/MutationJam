using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawnt unterhalb des Schlangenecks (letztes Segment) dunkle Schatten-Sprites,
/// die eine ins Terrain gegrabene Grube simulieren.
///
/// Setup-Anleitung:
///   1. Dieses Script auf dasselbe GameObject wie Snake legen.
///   2. Ein Sprite-Prefab zuweisen (spritesPrefab). Der Sprite selbst sollte:
///      - Einen SpriteRenderer mit Sorting Layer unterhalb der Schlange haben.
///      - Material: "Sprites/Multiply" oder ein Custom Unlit-Shader mit
///        Blendmode Multiply/Darken fuer den Gruben-Effekt.
///      Alternativ generiert das Script einen prozeduralen Radial-Gradient-
///      Sprite, falls kein Prefab gesetzt wird (nur fuer Prototyping).
///   3. SpurAbstand, MaxSprites, FadeDauer und VertikalerOffset im Inspector einstellen.
/// </summary>
[RequireComponent(typeof(Snake))]
public class SchlangenSpur : MonoBehaviour
{
    [Header("Spur Prefab")]
    [Tooltip("Prefab mit SpriteRenderer (Multiply-Material empfohlen). " +
             "Leer lassen = prozedurale Prototyp-Textur wird generiert.")]
    public GameObject spurPrefab;

    [Header("Spur Einstellungen")]
    [Tooltip("Mindestabstand (Welt-Einheiten) den der Schwanz zuruecklegen muss, " +
             "bevor ein neuer Spur-Sprite gespawnt wird.")]
    [Range(0.05f, 3f)]
    public float spurAbstand = 0.5f;

    [Tooltip("Maximale Anzahl gleichzeitig sichtbarer Spur-Sprites. Ist das Limit " +
             "erreicht, beginnt der aelteste zu faden und wird danach zerstoert.")]
    [Range(2, 64)]
    public int maxSprites = 16;

    [Tooltip("Dauer des Alpha-Fade-outs des aeltesten Sprites in Sekunden.")]
    [Range(0.1f, 5f)]
    public float fadeDauer = 1.2f;

    [Tooltip("Welt-Offset des Sprites relativ zur Schwanzposition (z.B. leicht nach unten/hinten).")]
    public Vector3 vertikalerOffset = new Vector3(0f, -0.15f, 0.1f);

    [Tooltip("Basis-Scale des Spur-Sprites. Unabhaengig von schwanzScale der Snake.")]
    public Vector3 spurScale = Vector3.one;

    [Tooltip("Sorting Order des SpriteRenderers. Sollte unter den Segmenten liegen.")]
    public int sortingOrder = -1;

    [Tooltip("Sorting Layer Name (muss im Project existieren).")]
    public string sortingLayerName = "Default";

    [Header("Visuell")]
    [Tooltip("Startfarbe / Tint des Sprites. Alpha = maximale Deckkraft.")]
    public Color spurFarbe = new Color(0f, 0f, 0f, 0.55f);

    [Tooltip("Ob der Sprite leicht mit dem Schwanz rotiert (Bewegungsrichtung).")]
    public bool rotiereMitRichtung = true;

    // -------------------------------------------------------------------------
    // Interna
    // -------------------------------------------------------------------------

    private Snake snake;

    // Queue: vorne = aelteste Instanz, hinten = juengste.
    private readonly Queue<GameObject> aktiveSprites = new Queue<GameObject>();

    // Laufender Fade-Coroutine-Tracker: damit nie zwei Fades gleichzeitig
    // auf demselben Objekt laufen (sollte nicht passieren, aber sicher ist sicher).
    private readonly HashSet<GameObject> amFaden = new HashSet<GameObject>();

    private Vector3 letzteSpurPos;
    private bool ersteSpur = true;

    // Prozedural erzeugter Sprite (nur falls kein Prefab gesetzt).
    private Sprite prozeduralSprite;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        snake = GetComponent<Snake>();
    }

    private void Start()
    {
        // Prozedurale Fallback-Textur erzeugen, falls kein Prefab gesetzt.
        if (spurPrefab == null)
        {
            prozeduralSprite = ErstelleGradientSprite();
            Debug.Log("[SchlangenSpur] Kein spurPrefab gesetzt – prozedurale Prototyp-Textur wird verwendet.");
        }
    }

    private void LateUpdate()
    {
        // Schwanz ist immer das letzte Segment (Index Count-1), sofern vorhanden.
        // Index 0 ist der Kopf, also brauchen wir mindestens 2 Segmente.
        List<Transform> segs = snake.Segments;
        if (segs == null || segs.Count < 2) return;

        Transform schwanz = segs[segs.Count - 1];
        if (schwanz == null) return;

        Vector3 schwanzPos = schwanz.position + vertikalerOffset;

        // Ersten Spawn initialisieren.
        if (ersteSpur)
        {
            letzteSpurPos = schwanzPos;
            ersteSpur = false;
            SpawneSpurSprite(schwanzPos, schwanz.rotation);
            return;
        }

        // Nur spawnen wenn Mindestabstand erreicht.
        if (Vector3.Distance(schwanzPos, letzteSpurPos) >= spurAbstand)
        {
            letzteSpurPos = schwanzPos;
            SpawneSpurSprite(schwanzPos, schwanz.rotation);
        }
    }

    // -------------------------------------------------------------------------
    // Spur-Sprite Logik
    // -------------------------------------------------------------------------

    private void SpawneSpurSprite(Vector3 pos, Quaternion rotation)
    {
        // Limit pruefen – aeltesten Sprite faden lassen falls voll.
        if (aktiveSprites.Count >= maxSprites)
        {
            GameObject aeltester = aktiveSprites.Dequeue();
            if (aeltester != null && !amFaden.Contains(aeltester))
            {
                StartCoroutine(FadeUndZerstoere(aeltester));
            }
        }

        GameObject go;

        if (spurPrefab != null)
        {
            go = Instantiate(spurPrefab, pos, rotiereMitRichtung ? rotation : Quaternion.identity);
        }
        else
        {
            // Prozedurale Fallback-Variante.
            go = new GameObject("SpurSprite");
            go.transform.position = pos;
            go.transform.rotation = rotiereMitRichtung ? rotation : Quaternion.identity;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = prozeduralSprite;
            sr.color = spurFarbe;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
        }

        go.transform.localScale = spurScale;

        // SpriteRenderer-Einstellungen auch auf Prefabs anwenden
        // (falls das Prefab keinen fertigen SR-Setup hat).
        SpriteRenderer srComp = go.GetComponent<SpriteRenderer>();
        if (srComp != null)
        {
            srComp.color = spurFarbe;
            srComp.sortingLayerName = sortingLayerName;
            srComp.sortingOrder = sortingOrder;
        }

        aktiveSprites.Enqueue(go);
    }

    private IEnumerator FadeUndZerstoere(GameObject go)
    {
        if (go == null) yield break;
        amFaden.Add(go);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            amFaden.Remove(go);
            Destroy(go);
            yield break;
        }

        float elapsed = 0f;
        Color startFarbe = sr.color;

        while (elapsed < fadeDauer)
        {
            elapsed += Time.deltaTime;
            if (go == null) yield break;

            float t = elapsed / fadeDauer;
            // Ease-In: langsam starten, dann schneller verschwinden.
            float alpha = Mathf.Lerp(startFarbe.a, 0f, t * t);
            sr.color = new Color(startFarbe.r, startFarbe.g, startFarbe.b, alpha);
            yield return null;
        }

        if (go != null)
        {
            amFaden.Remove(go);
            Destroy(go);
        }
    }

    // -------------------------------------------------------------------------
    // Prozedurale Radial-Gradient Textur (Prototyping-Fallback)
    //
    // Erzeugt einen kreisfoermigen Gradienten: Mitte dunkel/transparent,
    // Rand noch dunkler – wie eine ins Terrain gedrueckte Delle.
    // -------------------------------------------------------------------------

    private Sprite ErstelleGradientSprite()
    {
        const int groesse = 128;
        Texture2D tex = new Texture2D(groesse, groesse, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixel = new Color[groesse * groesse];
        Vector2 mitte = new Vector2(groesse * 0.5f, groesse * 0.5f);
        float radius = groesse * 0.5f;

        for (int y = 0; y < groesse; y++)
        {
            for (int x = 0; x < groesse; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), mitte);
                float t = Mathf.Clamp01(dist / radius);

                // Elliptisch abflachen: Breite groesser als Hoehe (Perspektiv-Eindruck).
                float xNorm = (x - mitte.x) / (radius * 1.4f);
                float yNorm = (y - mitte.y) / (radius * 0.7f);
                float ellipseDist = Mathf.Sqrt(xNorm * xNorm + yNorm * yNorm);
                float tEllipse = Mathf.Clamp01(ellipseDist);

                // Ausserhalb der Ellipse: vollstaendig transparent.
                if (tEllipse >= 1f)
                {
                    pixel[y * groesse + x] = Color.clear;
                    continue;
                }

                // Gradient: Rand sehr dunkel und undurchsichtig, Mitte heller/transparenter.
                // Das simuliert eine Kante einer Grube die Licht schlueckt.
                float randFaktor = Mathf.SmoothStep(0f, 1f, tEllipse);
                float mitteFaktor = 1f - Mathf.SmoothStep(0f, 1f, tEllipse * 1.8f);

                // Dunkler Ring am Rand, weicher Kern in der Mitte.
                float helligkeit = Mathf.Lerp(0.08f, 0.25f, mitteFaktor);
                float alpha = Mathf.Lerp(0.9f, 0.0f, mitteFaktor) * (1f - randFaktor * 0.3f);

                pixel[y * groesse + x] = new Color(helligkeit, helligkeit, helligkeit, alpha);
            }
        }

        tex.SetPixels(pixel);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, groesse, groesse),
            new Vector2(0.5f, 0.5f),   // Pivot: Mitte
            groesse                     // PPU = Texturgrösse -> 1x1 Welt-Einheit
        );
    }

    // -------------------------------------------------------------------------
    // Aufraeumen wenn das Objekt zerstoert wird
    // -------------------------------------------------------------------------

    private void OnDestroy()
    {
        StopAllCoroutines();

        foreach (GameObject go in aktiveSprites)
        {
            if (go != null) Destroy(go);
        }
        aktiveSprites.Clear();
        amFaden.Clear();

        if (prozeduralSprite != null)
            Destroy(prozeduralSprite.texture);
    }
}