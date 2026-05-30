using UnityEngine;

// Kommt auf jeden Gegner. Verwaltet dessen eigene Lebenspunkte:
//  - Schaden durch Projektile (je nach Projektilwert)
//  - sofortiger Tod, wenn der SchlangenKOPF den Gegner beruehrt
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

    // Wird von Projektilen aufgerufen. 'schaden' = Damage-Wert des Projektils.
    public void NimmSchaden(float schaden)
    {
        if (istTot || schaden <= 0f)
        {
            return;
        }

        aktuellesLeben -= schaden;

        if (aktuellesLeben <= 0f)
        {
            Stirb();
        }
    }

    // Sofortiger Tod, z.B. wenn der Schlangenkopf den Gegner frisst.
    public void SofortToeten()
    {
        if (istTot)
        {
            return;
        }

        aktuellesLeben = 0f;
        Stirb();
    }

    private void Stirb()
    {
        if (istTot)
        {
            return;
        }
        istTot = true;

        // Optional Punkte vergeben (nur wenn ein ScoreManager existiert)
        if (punkteBeiTod > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.PunkteHinzufuegen(punkteBeiTod);
        }

        Destroy(gameObject);
    }

    // Beruehrung durch den Schlangenkopf -> sofort sterben.
    // Der Kopf ist das GameObject mit der Snake-Komponente; die Koerpersegmente
    // tragen NUR SnakeSegment, also loesen sie diesen Tod nicht aus.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Snake>() != null)
        {
            SofortToeten();
        }
    }
}