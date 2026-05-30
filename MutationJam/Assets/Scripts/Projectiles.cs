using UnityEngine;

public class Projectiles : MonoBehaviour
{
    private Transform target;

    [Header("Projektil-Werte")]
    public float speed = 10f; // Geschwindigkeit des Projektils

    // Diese Methode wird vom Turm aufgerufen, um dem Projektil sein Ziel zu geben
    public void Seek(Transform _target)
    {
        target = _target;
    }

    void Update()
    {
        // --- 1. Billboard-Effekt (Immer zur Kamera zeigen) ---
        if (Camera.main != null) // Stelle sicher, dass eine Hauptkamera existiert
        {
            // Setze die Rotation des Projektils exakt auf die Rotation der Kamera.
            // Dadurch zeigt die Vorderseite des Sprites immer "nach vorne" zur Kamera.
            transform.rotation = Camera.main.transform.rotation;
        }

        // --- 2. Zielprüfung ---
        // Wenn das Ziel zerstört wurde, zerstöre auch dieses Projektil
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // --- 3. Bewegung und "Kollision" ---
        // Berechne die Richtung zum Ziel
        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        // Eigene Kollisionsabfrage: Haben wir den Gegner erreicht?
        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        // Bewege das Projektil in Richtung des Ziels.
        // Wir nutzen Space.World, damit die Bewegung unabhängig von der Billboard-Rotation ist.
        transform.Translate(dir.normalized * distanceThisFrame, Space.World);

        // DIE ZEILE transform.LookAt(target); WURDE ENTFERNT!
    }

    void HitTarget()
    {
        // Hier kannst du später Schaden hinzufügen (z.B. target.GetComponent<Enemy>().TakeDamage(10))

        Destroy(gameObject); // Zerstöre das Projektil
    }
}