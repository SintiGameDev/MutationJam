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
        // Wenn das Ziel zerstört wurde (z.B. von einem anderen Turm), zerstöre auch dieses Projektil
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Berechne die Richtung zum Ziel
        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        // Wenn die Distanz zum Ziel kleiner ist als die Strecke, die wir diesen Frame zurücklegen,
        // haben wir getroffen! (Eigene Kollisionsabfrage)
        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        // Bewege das Projektil in Richtung des Ziels
        transform.Translate(dir.normalized * distanceThisFrame, Space.World);

        // Drehe das Projektil so, dass es das Ziel anschaut
        transform.LookAt(target);
    }

    void HitTarget()
    {
        // Hier kannst du später Schaden hinzufügen (z.B. target.GetComponent<Enemy>().TakeDamage(10))
        // oder Partikeleffekte beim Einschlag abspielen lassen.

        Destroy(gameObject); // Zerstöre das Projektil
    }
}