using UnityEngine;

// ============================================================
//  Persoenlichkeit: Aengstlich
// ============================================================
//  Haelt mehr Abstand zur Schlange und dreht sich nur traege
//  nach ihr aus — wirkt unentschlossen und ausweichend.
// ============================================================

[CreateAssetMenu(fileName = "Persoenlichkeit_Aengstlich",
                 menuName  = "Enemy/Persoenlichkeit/Aengstlich")]
public class EnemyPersoenlichkeit_Aengstlich : EnemyPersoenlichkeit
{
    private void Reset()
    {
        bezeichnung              = "Aengstlich";
        haltDistanz              = 6f;     // haelt fruehzeitig an
        weiterfahrDistanz        = 8f;     // benoetigt grossen Abstand zum Wiederanfahren
        geschwindigkeitsFaktor   = 0.75f;  // faehrt langsamer
        bremsTraegheit           = 5f;     // bremst fruehzeitig und weich
        drehGeschwindigkeitFahrt = 60f;    // richtet sich nur langsam aus
        drehGeschwindigkeitStopp = 15f;    // im Stand fast keine Ausrichtung
        minDauer                 = 4f;
        maxDauer                 = 10f;
    }
}
