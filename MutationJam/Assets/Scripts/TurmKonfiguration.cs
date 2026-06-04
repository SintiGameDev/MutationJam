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
    // Harte Untergrenzen fuer die Zeitabstaende. Verhindert, dass eine hohe
    // 'schussrate' (Frequenz-Faktor) Takt und Pause gegen 0 teilt -> Dauerfeuer.
    private const float MIN_TAKT  = 0.02f;   // max. 50 Schuss/Sek innerhalb der Salve
    private const float MIN_PAUSE = 0.05f;
    // Sinnvolle Obergrenze fuer den Frequenz-Faktor, damit Skalierung nicht
    // entgleist (sonst dividiert man Takt/Pause durch z.B. 30).
    private const float MAX_FREQUENZ = 10f;

    [Header("Aussehen")]
    public GameObject turmPrefab;

    [Header("Kampfwerte (Basis = Stufe 1)")]
    public float reichweite = 5f;

    [Tooltip("Frequenz-Faktor (1 = Basistempo). Hoehere Werte = schnelleres Feuern. " +
             "Skaliert mit der Mutationsstufe und verkuerzt Burst-Takt UND Pause. " +
             "Wird zur Sicherheit auf 10 begrenzt.")]
    public float schussrate = 1f;

    [Tooltip("Schaden pro Projektil bei Stufe 1. Wird auf das gespawnte Projektil geschrieben.")]
    public float schaden = 10f;

    [Header("Burst-Feuer")]
    [Tooltip("Wie viele Schuesse eine Salve abgibt.")]
    public int schuessProBurst = 3;

    [Tooltip("Abstand zwischen den Schuessen INNERHALB einer Salve (Sekunden). " +
             "Klein halten fuer schnelles Stakkato, z.B. 0.1.")]
    public float taktImBurst = 0.1f;

    [Tooltip("Pause zwischen zwei Salven (Sekunden). Die lange Erholung nach einer Salve.")]
    public float pauseZwischenBursts = 1.5f;

    [Header("Projektil")]
    [Tooltip("Wird direkt auf Tower.projectilePrefab gesetzt. " +
             "Leer lassen = Prefab-Standardwert des Tower-Prefabs wird benutzt.")]
    public GameObject projektilPrefab;

    [Header("Sound")]
    [Tooltip("Schuss-Sound dieses Turmtyps. Wird einmal pro Schuss abgespielt " +
             "(nicht pro Feuerpunkt). Leer lassen = kein Sound.")]
    public AudioClip schussSound;
    [Range(0f, 1f)]
    [Tooltip("Lautstaerke des Schuss-Sounds.")]
    public float schussLautstaerke = 1f;

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

        // Burst: Anzahl bleibt fix, die beiden Zeiten sind bereits stufen-skaliert
        // (durch den Frequenz-Faktor geteilt -> hoehere Stufe = schneller).
        public int   schuessProBurst;
        public float taktImBurst;
        public float pauseZwischenBursts;
    }

    // Liefert die fertigen Werte fuer eine Mutationsstufe (1 = Basis, kein Bonus).
    public SkalierteWerte BerechneWerte(int stufe)
    {
        float frequenzFaktor = schussrateSkaliert ? Skaliere(schussrate, stufe) : schussrate;

        // Frequenz-Faktor begrenzen und nach unten absichern, damit nichts auf 0
        // faellt und die Skalierung nicht entgleist.
        frequenzFaktor = Mathf.Clamp(frequenzFaktor, 0.0001f, MAX_FREQUENZ);

        // Frequenz-Faktor verkuerzt die Zeitabstaende. >1 = schneller, daher dividieren.
        // Anschliessend auf die harte Untergrenze klemmen -> kein Dauerfeuer.
        float takt  = Mathf.Max(MIN_TAKT,  taktImBurst         / frequenzFaktor);
        float pause = Mathf.Max(MIN_PAUSE, pauseZwischenBursts / frequenzFaktor);

        return new SkalierteWerte
        {
            reichweite = reichweiteSkaliert ? Skaliere(reichweite, stufe) : reichweite,
            schussrate = frequenzFaktor,
            schaden    = schadenSkaliert ? Skaliere(schaden, stufe) : schaden,

            schuessProBurst     = Mathf.Max(1, schuessProBurst),
            taktImBurst         = takt,
            pauseZwischenBursts = pause
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
