using UnityEngine;

// Bestimmt, wie sich die Kampfwerte mit der Mutationsstufe eines Segments aendern.
public enum Skalierungsmodus
{
    // Wert = Basis * (1 + (Stufe - 1) * Faktor)
    // Beispiel Faktor 0.5:  Stufe1 = 100%,  Stufe2 = 150%,  Stufe3 = 200%
    Linear,

    // Wert = Basis * Faktor^(Stufe - 1)
    // Beispiel Faktor 1.5:  Stufe1 = 100%,  Stufe2 = 150%,  Stufe3 = 225%
    Multiplikativ
}

[CreateAssetMenu(fileName = "TurmKonfiguration", menuName = "Snake/Turmkonfiguration")]
public class TurmKonfiguration : ScriptableObject
{
    [Header("Aussehen")]
    public GameObject turmPrefab;

    [Header("Kampfwerte (Basis = Stufe 1)")]
    public float reichweite = 5f;
    public float schussrate = 1f;

    [Tooltip("Schaden pro Projektil bei Stufe 1. Wird auf das gespawnte Projektil geschrieben.")]
    public float schaden = 10f;

    [Header("Projektil")]
    [Tooltip("Wird direkt auf Tower.projectilePrefab gesetzt. " +
             "Leer lassen = Prefab-Standardwert des Tower-Prefabs wird benutzt.")]
    public GameObject projektilPrefab;

    [Header("Mutationsstufen-Skalierung")]
    [Tooltip("Welche Formel die Werte pro Mutationsstufe hochrechnet.")]
    public Skalierungsmodus skalierungsModus = Skalierungsmodus.Linear;

    [Tooltip("Linear:        Wert = Basis * (1 + (Stufe-1) * Faktor).  z.B. 0.5 => Stufe2 = 150%\n" +
             "Multiplikativ: Wert = Basis * Faktor^(Stufe-1).          z.B. 1.5 => Stufe2 = 150%")]
    public float skalierungsFaktor = 0.5f;

    [Header("Welche Werte skalieren?")]
    [Tooltip("Haken setzen = dieser Wert waechst mit der Mutationsstufe. " +
             "Ohne Haken bleibt der Basiswert auf jeder Stufe gleich.")]
    public bool reichweiteSkaliert = false;
    public bool schussrateSkaliert = false;
    public bool schadenSkaliert = true;

    // Ergebnis-Paket der Skalierung fuer eine bestimmte Stufe.
    public struct SkalierteWerte
    {
        public float reichweite;
        public float schussrate;
        public float schaden;
    }

    // Liefert die fertigen Werte fuer eine Mutationsstufe (1 = Basis, kein Bonus).
    public SkalierteWerte BerechneWerte(int stufe)
    {
        return new SkalierteWerte
        {
            reichweite = reichweiteSkaliert ? Skaliere(reichweite, stufe) : reichweite,
            schussrate = schussrateSkaliert ? Skaliere(schussrate, stufe) : schussrate,
            schaden = schadenSkaliert ? Skaliere(schaden, stufe) : schaden
        };
    }

    private float Skaliere(float basis, int stufe)
    {
        // Stufe 1 ergibt immer den Basiswert (Exponent / Multiplikator = 0).
        int n = Mathf.Max(1, stufe) - 1;

        switch (skalierungsModus)
        {
            case Skalierungsmodus.Multiplikativ:
                return basis * Mathf.Pow(skalierungsFaktor, n);

            case Skalierungsmodus.Linear:
            default:
                return basis * (1f + n * skalierungsFaktor);
        }
    }
}