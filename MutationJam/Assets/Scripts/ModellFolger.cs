using UnityEngine;

/// <summary>
/// Laesst ein (3D) Modell der Position und Rotation eines Ziels folgen,
/// OHNE dessen Scale zu erben. Dadurch wird der Squash/Stretch und eine
/// eventuelle z=0 Scale des Snake Kopfes NICHT uebernommen.
///
/// Scale und Ausgangsrotation werden beim Start vom originalen Prefab
/// uebernommen und festgehalten:
///  - Die Scale wird jeden Frame auf den Prefab Wert (mal Skalierung) erzwungen.
///  - Die Ausgangsrotation des Prefabs ist die Basis; die Bewegungsrichtung
///    des Kopfes wird nur OBENDRAUF gerechnet.
///
/// Bewusst KEIN Parenting: nur Position + Rotation werden in LateUpdate
/// uebernommen, nachdem LeanTween den Kopf fuer diesen Frame bewegt hat.
/// </summary>
public class ModellFolger : MonoBehaviour
{
    [Tooltip("Das Ziel (z.B. der Snake Kopf), dem dieses Modell folgt.")]
    public Transform ziel;

    [Tooltip("Welt Offset relativ zum Ziel (z.B. leicht nach hinten/oben).")]
    public Vector3 positionsOffset = Vector3.zero;

    [Tooltip("Optionaler zusaetzlicher Rotations Feinabgleich, falls die " +
             "Ausgangsrotation des Prefabs noch nicht ganz passt. Normalerweise 0.")]
    public Vector3 rotationsOffset = Vector3.zero;

    [Tooltip("Soll die Rotation des Ziels uebernommen werden? " +
             "Wenn false, bleibt nur die Ausgangsrotation des Prefabs.")]
    public bool folgtRotation = true;

    [Tooltip("Uniformer Groessen Multiplikator auf die Prefab Scale. " +
             "1 = exakt die Prefab Groesse. Uniform -> keine Verzerrung.")]
    public float skalierung = 1f;

    // Vom Original Prefab uebernommene Werte (gegen Verzerrung).
    private Quaternion ursprungsRotation = Quaternion.identity;
    private Vector3 ursprungsScale = Vector3.one;
    private bool initialisiert = false;

    private void Awake()
    {
        MerkeUrsprung();
    }

    /// <summary>
    /// Speichert Scale und Rotation, so wie sie das Prefab mitbringt.
    /// Wird automatisch beim Awake aufgerufen; kann bei Bedarf erneut
    /// aufgerufen werden, falls das Modell bewusst neu ausgerichtet wurde.
    /// </summary>
    public void MerkeUrsprung()
    {
        ursprungsRotation = transform.localRotation;
        ursprungsScale = transform.localScale;
        initialisiert = true;
    }

    private void LateUpdate()
    {
        if (ziel == null) return;

        // Sicherheitsnetz, falls Awake aus Timing Gruenden noch nicht lief.
        if (!initialisiert) MerkeUrsprung();

        // Position 1:1 vom Ziel (plus optionalem Offset) uebernehmen.
        transform.position = ziel.position + positionsOffset;

        // Scale bleibt die des originalen Prefabs (mal uniformem Multiplikator)
        // -> keine Verzerrung, egal was am Kopf an Squash/Scale passiert.
        transform.localScale = ursprungsScale * skalierung;

        // Rotation: Ausgangsrotation des Prefabs als Basis,
        // darauf die Bewegungsrichtung des Kopfes (und optional ein Feinabgleich).
        if (folgtRotation)
            transform.rotation = ziel.rotation * Quaternion.Euler(rotationsOffset) * ursprungsRotation;
        else
            transform.rotation = Quaternion.Euler(rotationsOffset) * ursprungsRotation;
    }
}
