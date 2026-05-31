using UnityEngine;
// Kommt auf jeden Gegner. Verwaltet dessen eigene Lebenspunkte:
//  - Schaden durch Projektile (je nach Projektilwert)
//  - sofortiger Tod, wenn der SchlangenKOPF den Gegner FRISST
//    (nur solange die Schlange noch Koerper-Segmente hat)
[RequireComponent(typeof(Collider2D))]
public class EnemyHealthManager : MonoBehaviour
{
    [Header("Lebenspunkte")]
    public float maxLeben = 100f;

    [Header("Belohnung")]
    [Tooltip("Punkte fuer den Spieler beim Toeten dieses Gegners (0 = keine).")]
    public int punkteBeiTod = 0;

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

        if (punkteBeiTod > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.PunkteHinzufuegen(punkteBeiTod);
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Snake snake = other.GetComponent<Snake>();
        if (snake != null)
        {
            // Der Kopf frisst den Gegner NUR, solange noch Koerper-Segmente
            // existieren. Ist nur noch der Kopf uebrig, ueberlebt der Gegner
            // und kann den Kopf angreifen (siehe EnemyFollow2D).
            if (!snake.NurKopfUebrig)
            {
                SofortToeten();
            }
        }
    }
}