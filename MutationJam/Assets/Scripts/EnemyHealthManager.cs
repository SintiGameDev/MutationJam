using UnityEngine;

// Kommt auf jeden Gegner. Verwaltet dessen eigene Lebenspunkte:
//  - Schaden durch Projektile (je nach Projektilwert)
//  - sofortiger Tod, wenn der SchlangenKOPF den Gegner FRISST
//    (nur solange die Schlange noch Koerper-Segmente hat)
// Beim Tod gibt es Score in Hoehe des Maximallebens (siehe ScoreManager.GegnerGetoetet).
// Optional: Ein Todes-Prefab wird beim Tod gespawnt und fadet nach einem Timer aus.
[RequireComponent(typeof(Collider2D))]
public class EnemyHealthManager : MonoBehaviour
{
    [Header("Lebenspunkte")]
    public float maxLeben = 100f;

    [Header("Belohnung")]
    [Tooltip("Zusaetzliche Bonuspunkte beim Toeten (0 = nur das Maximalleben zaehlt).")]
    public int bonusPunkteBeiTod = 0;

    [Header("Todes-Effekt")]
    [Tooltip("Prefab, das beim Tod gespawnt wird (z.B. Leiche, Explosion, Splatter). Leer lassen fuer keinen Effekt.")]
    public GameObject todesPrefab;

    private float aktuellesLeben;
    private bool istTot = false;
    public float AktuellesLeben => aktuellesLeben;

    private void Awake()
    {
        aktuellesLeben = maxLeben;
    }

    public void NimmSchaden(float schaden)
    {
        if (istTot || schaden <= 0f) return;

        EnemyFollow2D followScript = GetComponent<EnemyFollow2D>();
        if (followScript != null)
        {
            followScript.AufleuchtenLassen();
        }

        aktuellesLeben -= schaden;
        if (aktuellesLeben <= 0f)
        {
            Stirb();
        }
    }

    public void SofortToeten()
    {
        if (istTot) return;
        aktuellesLeben = 0f;
        Stirb();
    }

    private void Stirb()
    {
        if (istTot) return;
        istTot = true;

        // Todes-Prefab spawnen (gleiche Position, Rotation und Skalierung)
        if (todesPrefab != null)
        {
            GameObject instanz = Instantiate(todesPrefab, transform.position, transform.rotation);
            instanz.transform.localScale = transform.lossyScale;

            // TodesPrefabFader muss auf dem Prefab liegen (oder wird hier automatisch hinzugefuegt)
            TodesPrefabFader fader = instanz.GetComponent<TodesPrefabFader>();
            if (fader == null)
            {
                fader = instanz.AddComponent<TodesPrefabFader>();
            }
            fader.StarteAusfaden();
        }

        if (ScoreManager.Instance != null)
        {
            // Score += Maximalleben des Gegners ...
            ScoreManager.Instance.GegnerGetoetet(maxLeben);

            // ... plus optionaler Bonus
            if (bonusPunkteBeiTod > 0)
            {
                ScoreManager.Instance.PunkteHinzufuegen(bonusPunkteBeiTod);
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Snake snake = other.GetComponent<Snake>();
        if (snake != null)
        {
            // Der Kopf frisst den Gegner NUR, solange noch Koerper-Segmente
            // existieren. Ist nur noch der Kopf uebrig, ueberlebt der Gegner.
            if (!snake.NurKopfUebrig)
            {
                SofortToeten();
            }
        }
    }
}
