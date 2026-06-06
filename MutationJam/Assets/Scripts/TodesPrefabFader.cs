using System.Collections;
using UnityEngine;

// Dieses Script auf das Todes-Prefab legen (oder es wird automatisch vom EnemyHealthManager hinzugefuegt).
// Nach einem einstellbaren Verzoegerungs-Timer fadet das Objekt smooth aus und zerstoert sich dann selbst.
// Unterstuetzt SpriteRenderer und kann leicht auf Particle-Systems oder andere Renderer erweitert werden.
public class TodesPrefabFader : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Wartezeit in Sekunden, bevor das Ausfaden beginnt.")]
    public float verzoegerung = 0.5f;

    [Tooltip("Dauer des Ausfadens in Sekunden.")]
    public float fadeDauer = 1.0f;

    // SpriteRenderer-Array fuer alle Sprites im Prefab (inkl. Kinder)
    private SpriteRenderer[] spriteRenderer;
    private bool laeuft = false;

    private void Awake()
    {
        // Alle SpriteRenderer im Prefab (Eltern + Kinder) einsammeln
        spriteRenderer = GetComponentsInChildren<SpriteRenderer>(true);
    }

    // Wird vom EnemyHealthManager nach dem Spawnen aufgerufen.
    // Kann aber auch direkt per Inspector-Event oder im Awake automatisch starten
    // (siehe Start weiter unten).
    public void StarteAusfaden()
    {
        if (laeuft) return;
        laeuft = true;
        StartCoroutine(FadeRoutine());
    }

    // Optional: Automatisch starten, falls das Prefab eigenstaendig eingesetzt wird
    // und StarteAusfaden() nicht manuell aufgerufen wird.
    private void Start()
    {
        if (!laeuft)
        {
            StarteAusfaden();
        }
    }

    private IEnumerator FadeRoutine()
    {
        // --- Phase 1: Verzoegerung ---
        if (verzoegerung > 0f)
        {
            yield return new WaitForSeconds(verzoegerung);
        }

        // --- Phase 2: Smooth Ausfaden ---
        float vergangeneZeit = 0f;

        // Startfarben aller SpriteRenderer sichern
        Color[] startFarben = new Color[spriteRenderer.Length];
        for (int i = 0; i < spriteRenderer.Length; i++)
        {
            if (spriteRenderer[i] != null)
                startFarben[i] = spriteRenderer[i].color;
        }

        while (vergangeneZeit < fadeDauer)
        {
            vergangeneZeit += Time.deltaTime;
            float fortschritt = Mathf.Clamp01(vergangeneZeit / fadeDauer);

            // Smooth-Kurve: erst langsam, dann schneller (EaseIn)
            float alpha = Mathf.Lerp(1f, 0f, fortschritt);

            for (int i = 0; i < spriteRenderer.Length; i++)
            {
                if (spriteRenderer[i] != null)
                {
                    Color farbe = startFarben[i];
                    farbe.a = farbe.a * alpha;
                    spriteRenderer[i].color = farbe;
                }
            }

            yield return null;
        }

        // --- Phase 3: Selbstzerstoerung ---
        Destroy(gameObject);
    }
}
