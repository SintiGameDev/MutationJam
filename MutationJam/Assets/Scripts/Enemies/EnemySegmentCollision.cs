using UnityEngine;

// Auf jedem Enemy-GameObject platzieren.
// Verursacht beim Beruehren eines Schlangensegments Schaden.
[RequireComponent(typeof(Collider2D))]
public class EnemySegmentCollision : MonoBehaviour
{
    [Tooltip("Schadenspunkte pro Kollision")]
    public float schaden = 1f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        SnakeSegment segment = other.GetComponent<SnakeSegment>();

        if (segment != null) {
            segment.NimmSchaden(schaden);
        }
    }

    // Falls der Gegner physikbasiert kollidiert statt Trigger
    private void OnCollisionEnter2D(Collision2D collision)
    {
        SnakeSegment segment = collision.gameObject.GetComponent<SnakeSegment>();

        if (segment != null) {
            segment.NimmSchaden(schaden);
        }
    }
}
