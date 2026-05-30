using UnityEngine;
public class Projectiles : MonoBehaviour
{
    private Transform target;
    private Transform camTransform; // Speicher für die Kamera
    [Header("Projektil-Werte")]
    public float speed = 10f;
    [Tooltip("Schaden, den dieses Projektil dem getroffenen Gegner zufuegt.")]
    public float damage = 10f;
    void Start()
    {
        // Speichere die Hauptkamera einmalig beim Start des Projektils
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }
    public void Seek(Transform _target)
    {
        target = _target;
    }
    void LateUpdate() // Nutze LateUpdate für Kamera-Effekte
    {
        // Billboard-Effekt mit der gespeicherten Kamera
        if (camTransform != null)
        {
            transform.rotation = camTransform.rotation;
        }
        // --- Bewegung (Hier in LateUpdate, damit es nach der Drehung passiert) ---
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;
        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }
        transform.Translate(dir.normalized * distanceThisFrame, Space.World);
    }
    void HitTarget()
    {
        // Schaden am getroffenen Gegner anwenden (falls er Leben hat)
        if (target != null)
        {
            EnemyHealthManager leben = target.GetComponent<EnemyHealthManager>();
            if (leben != null)
            {
                leben.NimmSchaden(damage);
            }
        }
        Destroy(gameObject);
    }
}