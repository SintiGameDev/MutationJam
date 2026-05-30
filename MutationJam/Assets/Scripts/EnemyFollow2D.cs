using UnityEngine;

public class EnemyFollow2D : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]
    public float speed = 3f;

    private Transform playerTransform;

    void Start()
    {
        // Sucht das GameObject mit dem Tag "Player" in der Szene
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Speichert die Transform Komponente des Spielers für den schnellen Zugriff
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Es wurde kein GameObject mit dem Tag 'Player' in der Szene gefunden.");
        }
    }

    void Update()
    {
        // Führt die Bewegung nur aus, wenn der Spieler gefunden wurde
        if (playerTransform != null)
        {
            // Berechnet den nächsten Schritt in Richtung des Spielers
            transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, speed * Time.deltaTime);
        }
    }
}