using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawnt unterhalb des Schlangenkopfes dunkle Schatten-Sprites,
/// die eine ins Terrain gegrabene Spur simulieren.
///
/// Setup-Anleitung:
///   1. Dieses Script auf dasselbe GameObject wie Snake legen.
///   2. Ein Sprite-Prefab zuweisen (spurPrefab). Der Sprite sollte:
///      - Einen SpriteRenderer mit Sorting Order unterhalb der Schlange haben.
///      - Material: "Sprites/Multiply" (Built-in) oder fuer URP einen
///        Unlit Shader mit Blendmode Multiply, damit der Hintergrund
///        verdunkelt statt uebermalt wird.
///      Alternativ generiert das Script einen prozeduralen Radial-Gradient-
///      Sprite, falls kein Prefab gesetzt wird (Prototyping-Fallback).
///   3. SpurAbstand, MaxSprites, FadeDauer und SpawnOffset im Inspector einstellen.
/// </summary>
[RequireComponent(typeof(Snake))]
public class SchlangenSpur : MonoBehaviour
{
    [Header("Spur Prefab")]
    [Tooltip("Prefab mit SpriteRenderer (Multiply-Material empfohlen). " +
             "Leer lassen = prozedurale Prototyp-Textur wird generiert.")]
    public GameObject spurPrefab;

    [Header("Spur Einstellungen")]
    [Tooltip("Mindestabstand (Welt-Einheiten) den der Kopf zuruecklegen muss, " +
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

    [Tooltip("Welt-Offset des Sprites relativ zur Kopfposition. " +
             "Z-Wert positiv = hinter der Schlange (hoehere Zahl = weiter hinten im Layer).")]
    public Vector3 spawnOffset = new Vector3(0f, -0.1f, 0.1f);

    [Tooltip("Basis-Scale des Spur-Sprites.")]
    public Vector3 spurScale = Vector3.one;

    [Tooltip("Sorting Order des SpriteRenderers. Sollte unter den Segmenten liegen.")]
    public int sortingOrder = -1;

    [Tooltip("Sorting Layer Name (muss im Project existieren).")]
    public string sortingLayerName = "Default";

    [Header("Visuell")]
    [Tooltip("Startfarbe / Tint des Sprites. Alpha bestimmt die maximale Deckkraft beim Spawn.")]
    public Color spurFarbe = new Color(0f, 0f, 0f, 0.55f);

    [Tooltip("Ob der Sprite mit der Kopf-Rotation ausgerichtet wird (Bewegungsrichtung).")]
    public bool rotiereMitRichtung = true;

    // -------------------------------------------------------------------------
    // Interna
    // -------------------------------------------------------------------------

    private Snake snake;

    // Queue: vorne = aelteste Instanz, hinten = juengste.
    private readonly Queue<SpriteEintrag> aktiveSprites = new Queue<SpriteEintrag>();

    private Vector3 letzteSpurPos;
    private bool ersteSpur = true;

    // Prozedural erzeugter Sprite (nur falls kein Prefab gesetzt).
    private Sprite prozeduralSprite;

    // Wrapper damit wir SpriteRenderer + startAlpha sicher zusammen halten.
    private struct SpriteEintrag
    {
        public GameObject Go;
        public SpriteRenderer Sr;
        // Das Alpha zum Zeitpunkt des Spawns – nicht vom SR lesen, da
        // Unity den Wert erst nach dem naechsten Render-Frame committed.
        public float StartAlpha;
    }

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        snake = GetComponent<Snake>();
    }

    private void Start()
    {
        if (spurPrefab == null)
        {
            prozeduralSprite = ErstelleGradientSprite();
            Debug.Log("[SchlangenSpur] Kein spurPrefab gesetzt – prozedurale Prototyp-Textur wird verwendet.");
        }
    }

    private void LateUpdate()
    {
        // Index 0 = Kopf.
        List<Transform> segs = snake.Segments;
        if (segs == null || segs.Count < 1) return;

        Transform kopf = segs[0];
        if (kopf == null) return;

        Vector3 kopfPos = kopf.position + spawnOffset;

        if (ersteSpur)
        {
            letzteSpurPos = kopfPos;
            ersteSpur = false;
            SpawneSpurSprite(kopfPos, kopf.rotation);
            return;
        }

        if (Vector3.Distance(kopfPos, letzteSpurPos) >= spurAbstand)
        {
            letzteSpurPos = kopfPos;
            SpawneSpurSprite(kopfPos, kopf.rotation);
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
            SpriteEintrag aeltester = aktiveSprites.Dequeue();
            if (aeltester.Go != null)
                StartCoroutine(FadeUndZerstoere(aeltester));
        }

        GameObject go;
        SpriteRenderer sr;

        Quaternion zielRot = rotiereMitRichtung ? rotation : Quaternion.identity;

        if (spurPrefab != null)
        {
            go = Instantiate(spurPrefab, pos, zielRot);
            sr = go.GetComponent<SpriteRenderer>();

            // Falls das Prefab keinen SR direkt hat, in Kindern suchen.
            if (sr == null)
                sr = go.GetComponentInChildren<SpriteRenderer>();
        }
        else
        {
            go = new GameObject("SpurSprite");
            go.transform.position = pos;
            go.transform.rotation = zielRot;
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = prozeduralSprite;
        }

        go.transform.localScale = spurScale;

        if (sr != null)
        {
            // Sorting immer setzen – ueberschreibt den Prefab-Wert absichtlich.
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;

            // WICHTIG: Farbe direkt auf spurFarbe setzen.
            // Wir lesen sie NICHT zurueck aus sr.color, weil Unity den Wert
            // erst nach dem ersten Render-Frame auf der Material-Instanz committed.
            // Stattdessen speichern wir spurFarbe.a als startAlpha im Eintrag.
            sr.color = spurFarbe;
        }

        aktiveSprites.Enqueue(new SpriteEintrag
        {
            Go = go,
            Sr = sr,
            StartAlpha = spurFarbe.a
        });
    }

    // -------------------------------------------------------------------------
    // Fade – benutzt StartAlpha aus dem Eintrag, NICHT sr.color.a
    // -------------------------------------------------------------------------

    private IEnumerator FadeUndZerstoere(SpriteEintrag eintrag)
    {
        GameObject go = eintrag.Go;
        SpriteRenderer sr = eintrag.Sr;
        float startAlpha = eintrag.StartAlpha;

        if (go == null) yield break;

        // Einen Frame warten damit Unity die Farbe rendern kann,
        // bevor wir anfangen sie wegzublenden.
        yield return null;

        if (go == null || sr == null) yield break;

        float elapsed = 0f;
        Color baseColor = new Color(spurFarbe.r, spurFarbe.g, spurFarbe.b, startAlpha);

        while (elapsed < fadeDauer)
        {
            if (go == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDauer);

            // Smooth Ease-In-Out: langsam starten, Mitte schnell, am Ende sanft.
            float tGedaempft = t * t * (3f - 2f * t);
            float alpha = Mathf.Lerp(startAlpha, 0f, tGedaempft);

            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        if (go != null)
            Destroy(go);
    }

    // -------------------------------------------------------------------------
    // Prozedurale Radial-Gradient Textur (Prototyping-Fallback)
    // Erzeugt eine elliptische dunkle Delle – dunkler Ring aussen,
    // etwas heller zur Mitte hin (Tiefen-Illusion).
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
                // Elliptische Verzerrung: breiter als hoch (Vogelperspektive).
                float xNorm = (x - mitte.x) / (radius * 1.5f);
                float yNorm = (y - mitte.y) / (radius * 0.75f);
                float ellipseDist = Mathf.Sqrt(xNorm * xNorm + yNorm * yNorm);
                float t = Mathf.Clamp01(ellipseDist);

                if (t >= 1f)
                {
                    pixel[y * groesse + x] = Color.clear;
                    continue;
                }

                // Rand dunkel & undurchsichtig, Mitte etwas heller.
                float randFaktor = Mathf.SmoothStep(0f, 1f, t);
                float mitteFaktor = 1f - Mathf.SmoothStep(0f, 1f, t * 1.6f);

                float helligkeit = Mathf.Lerp(0.05f, 0.22f, mitteFaktor);
                float alpha = Mathf.Lerp(0.85f, 0f, mitteFaktor) * (1f - randFaktor * 0.25f);

                pixel[y * groesse + x] = new Color(helligkeit, helligkeit, helligkeit, alpha);
            }
        }

        tex.SetPixels(pixel);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, groesse, groesse),
            new Vector2(0.5f, 0.5f),
            groesse
        );
    }

    // -------------------------------------------------------------------------
    // Aufraeumen
    // -------------------------------------------------------------------------

    private void OnDestroy()
    {
        StopAllCoroutines();

        foreach (SpriteEintrag e in aktiveSprites)
        {
            if (e.Go != null) Destroy(e.Go);
        }
        aktiveSprites.Clear();

        if (prozeduralSprite != null)
            Destroy(prozeduralSprite.texture);
    }
}