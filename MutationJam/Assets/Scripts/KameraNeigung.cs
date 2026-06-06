using UnityEngine;

/// <summary>
/// Platziere dieses Script auf der Kamera.
/// Die Kamera neigt sich sanft in Richtung des Schlangenkopfes,
/// ohne ihre Position zu veraendern. Funktioniert fuer Top-down 2D-Setups
/// (Kamera schaut entlang der Z-Achse nach unten).
///
/// Funktionsweise:
///   1. Der Offset des Kopfes relativ zum Raummittelpunkt (oder einem
///      frei waehhlbaren Ankerpunkt) wird berechnet.
///   2. Dieser Offset wird auf einen maximalen Neigungswinkel abgebildet.
///   3. Die Kamera rotiert per Slerp weich zur Zielneigung.
///      X-Rotation  = vertikale Neigung (Kopf oben/unten im Raum)
///      Y-Rotation  = horizontale Neigung (Kopf links/rechts im Raum)
///      Z-Rotation  = unveraendert (kein Roll)
/// </summary>
public class KameraNeigung : MonoBehaviour
{
    [Header("Referenzen")]
    [Tooltip("Transform des Schlangenkopfes. Wird automatisch gesucht falls leer.")]
    public Transform schlangenkopf;

    [Tooltip("Weltposition, die als 'Nullpunkt' fuer die Neigungsberechnung gilt. " +
             "Leer lassen = Weltkoordinate (0, 0, 0).")]
    public Transform raumMittelpunkt;

    [Header("Neigungsstaerke")]
    [Tooltip("Maximale Neigung in Grad bei voller Aussteuerung des Raumes.")]
    [Range(0f, 15f)]
    public float maxNeigungsWinkel = 5f;

    [Tooltip("Radius (in Welt-Einheiten) bei dem die maximale Neigung erreicht wird. " +
             "Entspricht ungefaehr dem halben Raumdurchmesser.")]
    [Range(1f, 50f)]
    public float referenzRadius = 10f;

    [Header("Glaettung")]
    [Tooltip("Wie schnell die Kamera der Zielneigung folgt. " +
             "Kleiner = traeger/ruhiger, Groesser = direkter.")]
    [Range(0.5f, 10f)]
    public float glaettungsGeschwindigkeit = 2f;

    [Tooltip("Rueckkehrer-Geschwindigkeit wenn der Kopf nicht gefunden wird (z.B. nach Tod).")]
    [Range(0.5f, 10f)]
    public float rueckkehrGeschwindigkeit = 3f;

    // Basis-Euler-Winkel der Kamera zum Spielstart (z.B. -90 / 0 / 0 fuer Top-down).
    private Vector3 basisRotation;

    // Ziel-Euler-Winkel in diesem Frame.
    private Vector3 zielRotation;

    private void Start()
    {
        // Ausgangsrotation der Kamera merken.
        basisRotation = transform.eulerAngles;
        zielRotation  = basisRotation;

        // Schlangenkopf auto-suchen falls nicht per Inspector gesetzt.
        if (schlangenkopf == null)
        {
            Snake snake = FindObjectOfType<Snake>();
            if (snake != null)
            {
                schlangenkopf = snake.transform;
                Debug.Log("[KameraNeigung] Schlangenkopf automatisch gefunden: " + snake.name);
            }
            else
            {
                Debug.LogWarning("[KameraNeigung] Kein Snake-Objekt in der Szene gefunden. " +
                                 "Bitte 'schlangenkopf' manuell im Inspector setzen.");
            }
        }
    }

    private void LateUpdate()
    {
        BerechneZielRotation();
        WendeRotationAn();
    }

    private void BerechneZielRotation()
    {
        if (schlangenkopf == null)
        {
            // Kein Kopf vorhanden → zurueck zur Basisrotation.
            zielRotation = basisRotation;
            return;
        }

        // Ankerpunkt (Weltkoordinate (0,0,0) oder benutzerdefiniert).
        Vector3 ankerpunkt = raumMittelpunkt != null
            ? raumMittelpunkt.position
            : Vector3.zero;

        // Offset des Schlangenkopfes vom Ankerpunkt (nur X/Y relevant).
        Vector3 offset = schlangenkopf.position - ankerpunkt;

        // Offset auf [-1, 1] normieren, begrenzt durch den Referenzradius.
        float normX = Mathf.Clamp(offset.x / referenzRadius, -1f, 1f);
        float normY = Mathf.Clamp(offset.y / referenzRadius, -1f, 1f);

        // Neigungswinkel berechnen.
        // Top-down: Kopf nach rechts  → Kamera nach rechts neigen  → positiver Y-Winkel
        //           Kopf nach oben    → Kamera nach oben neigen    → negativer X-Winkel
        //           (negative X damit die Oberkante sich dem Spieler entgegenstreckt)
        float zielX = basisRotation.x + (-normY * maxNeigungsWinkel);
        float zielY = basisRotation.y + ( normX * maxNeigungsWinkel);
        float zielZ = basisRotation.z; // kein Roll

        zielRotation = new Vector3(zielX, zielY, zielZ);
    }

    private void WendeRotationAn()
    {
        // Geschwindigkeit anpassen je nachdem ob ein Ziel vorhanden ist.
        float geschwindigkeit = schlangenkopf != null
            ? glaettungsGeschwindigkeit
            : rueckkehrGeschwindigkeit;

        Quaternion aktuelle = transform.rotation;
        Quaternion ziel     = Quaternion.Euler(zielRotation);

        // Slerp fuer gleichmaessige Drehbewegung.
        transform.rotation = Quaternion.Slerp(aktuelle, ziel,
            1f - Mathf.Exp(-geschwindigkeit * Time.deltaTime));
    }

#if UNITY_EDITOR
    // Hilfsgizmo: zeigt den Referenzradius als Kreis im Scene-View.
    private void OnDrawGizmosSelected()
    {
        Vector3 ankerpunkt = raumMittelpunkt != null ? raumMittelpunkt.position : Vector3.zero;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        DrawWireCircle(ankerpunkt, referenzRadius, 48);
    }

    private static void DrawWireCircle(Vector3 center, float radius, int schritte)
    {
        float deltaWinkel = 360f / schritte;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= schritte; i++)
        {
            float winkel = i * deltaWinkel * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(winkel) * radius,
                                                Mathf.Sin(winkel) * radius, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
