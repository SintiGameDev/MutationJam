using UnityEngine;

// ============================================================
//  Persoenlichkeit: Normal
// ============================================================
//  Faehrt wie gewohnt der Schlange hinterher und richtet sich
//  kontinuierlich nach ihr aus. Mittlere Haltedistanz.
// ============================================================

[CreateAssetMenu(fileName = "Persoenlichkeit_Normal",
                 menuName  = "Enemy/Persoenlichkeit/Normal")]
public class EnemyPersoenlichkeit_Normal : EnemyPersoenlichkeit
{
    private void Reset()
    {
        bezeichnung              = "Normal";
        haltDistanz              = 3f;
        weiterfahrDistanz        = 4f;
        geschwindigkeitsFaktor   = 1f;
        bremsTraegheit           = 8f;
        drehGeschwindigkeitFahrt = 180f;
        drehGeschwindigkeitStopp = 45f;
        minDauer                 = 5f;
        maxDauer                 = 12f;
    }
}
