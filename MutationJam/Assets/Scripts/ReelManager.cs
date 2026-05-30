using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Steuert alle Walzen des Spielautomaten.
/// Verwaltet Start, Stop-Sequenz und Gewinnlinien-Auswertung.
/// </summary>
public class ReelManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector-Konfiguration
    // -------------------------------------------------------------------------

    [Header("Walzen")]
    [Tooltip("Alle Walzen in Reihenfolge von links nach rechts.")]
    public List<Reel> Walzen = new();

    [Header("Stopp-Sequenz")]
    [Tooltip("Verzoegerung zwischen dem Stopp jeder Walze in Sekunden.")]
    public float StoppVerzoegerung = 0.3f;

    [Header("Events")]
    [Tooltip("Wird ausgeloest wenn alle Walzen gestartet wurden.")]
    public UnityEvent OnSpinGestartet;

    [Tooltip("Wird ausgeloest wenn alle Walzen gestoppt haben. Parameter: Gesamtgewinn.")]
    public UnityEvent<int> OnAuswertungAbgeschlossen;

    [Tooltip("Wird ausgeloest wenn ein Gewinn erzielt wurde. Parameter: Gesamtgewinn.")]
    public UnityEvent<int> OnGewinnErzielt;

    [Tooltip("Wird ausgeloest wenn kein Gewinn erzielt wurde.")]
    public UnityEvent OnKeinGewinn;

    // -------------------------------------------------------------------------
    // Laufzeit-Zustand
    // -------------------------------------------------------------------------

    private bool _laeuft = false;
    private Coroutine _stoppSequenzCoroutine;

    // -------------------------------------------------------------------------
    // Oeffentliche Steuerung
    // -------------------------------------------------------------------------

    /// <summary>
    /// Startet alle Walzen gleichzeitig.
    /// Nur moeglich wenn die Maschine gerade nicht laeuft.
    /// </summary>
    public void SpinStarten()
    {
        if (_laeuft)
        {
            Debug.LogWarning("[ReelManager] SpinStarten ignoriert – Walzen laufen bereits.");
            return;
        }

        if (Walzen == null || Walzen.Count == 0)
        {
            Debug.LogError("[ReelManager] Keine Walzen konfiguriert!");
            return;
        }

        _laeuft = true;

        foreach (Reel walze in Walzen)
        {
            if (walze != null)
                walze.Starten();
        }

        OnSpinGestartet?.Invoke();
        Debug.Log($"[ReelManager] {Walzen.Count} Walzen gestartet.");
    }

    /// <summary>
    /// Stoppt alle Walzen von links nach rechts mit konfigurierter Verzoegerung.
    /// Loest nach dem letzten Stopp automatisch die Auswertung aus.
    /// </summary>
    public void StoppSequenzStarten()
    {
        if (!_laeuft)
        {
            Debug.LogWarning("[ReelManager] StoppSequenzStarten ignoriert – Walzen laufen nicht.");
            return;
        }

        if (_stoppSequenzCoroutine != null)
            StopCoroutine(_stoppSequenzCoroutine);

        _stoppSequenzCoroutine = StartCoroutine(StoppSequenzCoroutine());
    }

    // -------------------------------------------------------------------------
    // Stopp-Sequenz Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator StoppSequenzCoroutine()
    {
        for (int i = 0; i < Walzen.Count; i++)
        {
            if (Walzen[i] != null)
            {
                Walzen[i].StoppAnfordern();
                Debug.Log($"[ReelManager] Walze {i} Stopp angefordert.");
            }

            if (i < Walzen.Count - 1)
                yield return new WaitForSeconds(StoppVerzoegerung);
        }

        // Warten bis alle Walzen vollstaendig gestoppt haben
        yield return new WaitUntil(AlleWalzenGestoppt);

        Debug.Log("[ReelManager] Alle Walzen gestoppt. Starte Auswertung.");
        _laeuft = false;

        AuswertungDurchfuehren();
    }

    private bool AlleWalzenGestoppt()
    {
        foreach (Reel walze in Walzen)
        {
            if (walze != null && walze.Laeuft)
                return false;
        }
        return true;
    }

    // -------------------------------------------------------------------------
    // Gewinnauswertung
    // -------------------------------------------------------------------------

    private void AuswertungDurchfuehren()
    {
        List<SlotSymbolInstance> gewinnlinie = GetGewinnlinie();

        if (gewinnlinie == null || gewinnlinie.Count == 0)
        {
            Debug.LogWarning("[ReelManager] Gewinnlinie konnte nicht ausgelesen werden.");
            return;
        }

        // Mutationsstufen aller ausgespielten Symbole erhoehen
        foreach (SlotSymbolInstance instanz in gewinnlinie)
            instanz?.BeiAusspielung();

        // Gewinn berechnen und loggen
        int gesamtGewinn = BerechneGewinn(gewinnlinie);
        LoggeGewinnlinie(gewinnlinie, gesamtGewinn);

        // Events ausloesen
        OnAuswertungAbgeschlossen?.Invoke(gesamtGewinn);

        if (gesamtGewinn > 0)
            OnGewinnErzielt?.Invoke(gesamtGewinn);
        else
            OnKeinGewinn?.Invoke();
    }

    /// <summary>
    /// Liest die mittlere Reihe aller Walzen aus.
    /// </summary>
    public List<SlotSymbolInstance> GetGewinnlinie()
    {
        List<SlotSymbolInstance> linie = new();

        foreach (Reel walze in Walzen)
        {
            if (walze == null) continue;

            SlotSymbolInstance instanz = walze.GetMittlereInstanz();
            if (instanz != null)
                linie.Add(instanz);
        }

        return linie;
    }

    private int BerechneGewinn(List<SlotSymbolInstance> gewinnlinie)
    {
        if (gewinnlinie == null || gewinnlinie.Count < 2) return 0;

        // Pruefen ob alle Symbole in der Gewinnlinie identisch sind
        SlotSymbolDefinition erstesSymbol = gewinnlinie[0]?.Definition;
        if (erstesSymbol == null) return 0;

        bool alleGleich = true;
        foreach (SlotSymbolInstance instanz in gewinnlinie)
        {
            if (instanz?.Definition != erstesSymbol)
            {
                alleGleich = false;
                break;
            }
        }

        if (!alleGleich) return 0;

        // Gewinn: Summe aller Einzelgewinne der Linie
        int gesamtGewinn = 0;
        foreach (SlotSymbolInstance instanz in gewinnlinie)
            gesamtGewinn += instanz.GetGewinn();

        return gesamtGewinn;
    }

    private void LoggeGewinnlinie(List<SlotSymbolInstance> linie, int gewinn)
    {
        System.Text.StringBuilder sb = new();
        sb.Append("[ReelManager] Gewinnlinie: ");

        foreach (SlotSymbolInstance instanz in linie)
        {
            sb.Append(instanz?.Definition?.name ?? "NULL");
            sb.Append($"(Stufe {instanz?.Mutationsstufe ?? 0}) | ");
        }

        sb.Append($"→ Gewinn: {gewinn}");
        Debug.Log(sb.ToString());
    }
}
