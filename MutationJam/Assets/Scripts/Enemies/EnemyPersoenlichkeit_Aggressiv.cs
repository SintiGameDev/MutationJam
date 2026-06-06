using UnityEngine;

// ============================================================
//  Persoenlichkeit: Aggressiv
// ============================================================
//  Faehrt sehr nah heran und zielt auf das naechstgelegene
//  Segment statt auf den Kopf. Dreht sich schnell und praesize
//  aus — wirkt entschlossen und bedrohlich.
// ============================================================

[CreateAssetMenu(fileName = "Persoenlichkeit_Aggressiv",
                 menuName  = "Enemy/Persoenlichkeit/Aggressiv")]
public class EnemyPersoenlichkeit_Aggressiv : EnemyPersoenlichkeit
{
    private void Reset()
    {
        bezeichnung              = "Aggressiv";
        zielNaechstesSegment     = true;   // rammt naechstes Segment, nicht den Kopf
        haltDistanz              = 0.5f;
        weiterfahrDistanz        = 1.5f;
        geschwindigkeitsFaktor   = 1.3f;
        bremsTraegheit           = 15f;
        drehGeschwindigkeitFahrt = 360f;
        drehGeschwindigkeitStopp = 180f;
        minDauer                 = 3f;
        maxDauer                 = 8f;
    }
}
