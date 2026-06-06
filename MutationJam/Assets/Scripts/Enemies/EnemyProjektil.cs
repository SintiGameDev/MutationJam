using UnityEngine;

// ============================================================
//  EnemyProjektil
// ============================================================
//  Wird von EnemyFollow2D (Fernkaempfer) auf das naechste
//  Schlangensegment abgefeuert. Das Projektil-Prefab braucht:
//    - Dieses Script
//    - Rigidbody2D  (Gravity Scale: 0, Body Type: Dynamic)
//    - Collider2D   (Is Trigger: true)
//
//  Beim Treffer auf ein SnakeSegment → NimmSchaden().
//  Das Projektil zerstoert sich danach selbst (oder nach Timeout).
// ============================================================

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjektil : MonoBehaviour
{
    [Tooltip("Maximale Lebensdauer in Sekunden, falls kein Treffer.")]
    public float lebensdauer = 4f;

    private float   schaden;
    private Vector2 richtung;
    private float   geschwindigkeit;

    private Rigidbody2D rb;
    private bool        hatGetroffen = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Wird direkt nach Instantiate() von EnemyFollow2D aufgerufen.
    /// </summary>
    public void Initialisiere(Vector2 richtung, float geschwindigkeit, float schaden)
    {
        this.richtung       = richtung.normalized;
        this.geschwindigkeit = geschwindigkeit;
        this.schaden        = schaden;

        rb.linearVelocity = this.richtung * this.geschwindigkeit;

        Destroy(gameObject, lebensdauer);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hatGetroffen) return;

        // Schlangensegment treffen
        SnakeSegment segment = other.GetComponent<SnakeSegment>();
        if (segment != null)
        {
            hatGetroffen = true;
            segment.NimmSchaden(schaden);
            Destroy(gameObject);
            return;
        }

        // Schlangenkopf treffen (nur wenn NurKopfUebrig)
        Snake snake = other.GetComponent<Snake>();
        if (snake != null && snake.NurKopfUebrig)
        {
            hatGetroffen = true;
            snake.KopfNimmtSchaden(schaden);
            Destroy(gameObject);
            return;
        }

        // Hindernis (kein Gegner, keine Schlange) → Projektil zerstoeren
        // Eigene Gegner-Layer nicht beachten, da Projektil nur Snake/Segment targetet.
        // Fuer Wand-Kollision: Wand-Layer im Collider setzen und hier optional pruefen.
        if (other.gameObject.CompareTag("Obstacle") || other.gameObject.CompareTag("Wall"))
        {
            hatGetroffen = true;
            Destroy(gameObject);
        }
    }
}
