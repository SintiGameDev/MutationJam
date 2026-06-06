using UnityEngine;

// ============================================================
//  EnemyPersoenlichkeit — ScriptableObject Basisklasse
// ============================================================
//  Beschreibt das Verhalten eines Gegner-Typs als Datensatz.
//  Neue Persoenlichkeiten: Rechtsklick → Create → Enemy → Persoenlichkeit
//  und dann eine der drei Unterklassen auswaehlen.
//
//  Der EnemyPersoenlichkeitsWechsler liest diese Werte und
//  schreibt sie in EnemyFollow2D.
// ============================================================

[CreateAssetMenu(fileName = "Persoenlichkeit_Neu",
                 menuName  = "Enemy/Persoenlichkeit/Basis")]
public class EnemyPersoenlichkeit : ScriptableObject
{
    [Header("Info")]
    [Tooltip("Anzeigename fuer Debug-Logs und den Inspector.")]
    public string bezeichnung = "Unbenannt";

    // ── Zielwahl ────────────────────────────────────────────
    [Header("Zielwahl")]
    [Tooltip("true = Ziel ist das naechste Koerpersegment (aggressives Rammen).\n" +
             "false = Ziel ist der Schlangenkopf (Standardverhalten).")]
    public bool zielNaechstesSegment = false;

    // ── Halteverhalten ──────────────────────────────────────
    [Header("Haltedistanz")]
    [Tooltip("Distanz zur Schlange, ab der gebremst und gestoppt wird.")]
    public float haltDistanz = 3f;

    [Tooltip("Distanz, ab der wieder losgefahren wird (Hysterese, > haltDistanz).")]
    public float weiterfahrDistanz = 4f;

    // ── Bewegung ────────────────────────────────────────────
    [Header("Bewegung")]
    [Tooltip("Multiplikator auf die zufaellige Basisgeschwindigkeit des Gegners.\n" +
             "1 = unveraendert, 0.5 = halb so schnell, 1.5 = 50% schneller.")]
    [Range(0.2f, 3f)]
    public float geschwindigkeitsFaktor = 1f;

    [Tooltip("Wie weich gebremst und angefahren wird.")]
    [Range(1f, 20f)]
    public float bremsTraegheit = 8f;

    // ── Ausrichtung ─────────────────────────────────────────
    [Header("Ausrichtung")]
    [Tooltip("Drehgeschwindigkeit in Grad/Sek waehrend der Fahrt.")]
    public float drehGeschwindigkeitFahrt = 180f;

    [Tooltip("Drehgeschwindigkeit in Grad/Sek im Stillstand.")]
    public float drehGeschwindigkeitStopp = 45f;

    // ── Persoenlichkeits-Timer ──────────────────────────────
    [Header("Wechsel-Intervall")]
    [Tooltip("Minimale Zeit (Sek) bevor diese Persoenlichkeit durch eine andere ersetzt werden kann.")]
    public float minDauer = 5f;

    [Tooltip("Maximale Zeit (Sek) bevor diese Persoenlichkeit durch eine andere ersetzt werden kann.")]
    public float maxDauer = 12f;
}
