using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Haupt-Controller der Slot-Maschine.
/// Startet alle Spalten gleichzeitig, friert sie nacheinander ein (mit Verzögerung)
/// und wertet das Ergebnis aus.
/// </summary>
public class SlotMachine : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Spalten")]
    [Tooltip("Alle drei (oder mehr) Slot-Spalten, von links nach rechts")]
    public SlotColumn[] Spalten;

    [Header("Timing")]
    [Tooltip("Verzögerung zwischen den Freeze-Aufrufen der Spalten (Sekunden)")]
    public float VerzögerungZwischenSpalten = 0.4f;

    [Tooltip("Mindest-Spin-Dauer bevor die erste Spalte einfriert (Sekunden)")]
    public float MinSpinDauer = 1.5f;

    [Header("Events")]
    [Tooltip("Wird nach vollständiger Auswertung aufgerufen")]
    public UnityEvent<ErgebnisData> OnErgebnis;

    // ─── State ────────────────────────────────────────────────────────────────

    private bool _läuft;

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Startet einen kompletten Spin-Zyklus.</summary>
    public void StarteSpinZyklus()
    {
        if (_läuft) return;
        StartCoroutine(SpinRoutine());
    }

    // ─── Coroutines ───────────────────────────────────────────────────────────

    private IEnumerator SpinRoutine()
    {
        _läuft = true;

        // 1. Alle Spalten zurücksetzen und starten
        foreach (SlotColumn spalte in Spalten)
        {
            spalte.Reset();
            spalte.StarteSpin();
        }

        // 2. Mindest-Spin-Zeit abwarten
        yield return new WaitForSeconds(MinSpinDauer);

        // 3. Spalten nacheinander einfrieren
        for (int i = 0; i < Spalten.Length; i++)
        {
            Spalten[i].FreezeSpalte();

            if (i < Spalten.Length - 1)
                yield return new WaitForSeconds(VerzögerungZwischenSpalten);
        }

        // 4. Tween der letzten Spalte abwarten, dann auswerten
        yield return new WaitForSeconds(Spalten[Spalten.Length - 1].GetComponent<SlotColumn>()
            ? Spalten[Spalten.Length - 1].TweenDauer + 0.05f
            : 0.3f);

        WerteAus();
        _läuft = false;
    }

    // ─── Auswertung ───────────────────────────────────────────────────────────

    private void WerteAus()
    {
        int[] ergebnisIDs = new int[Spalten.Length];
        bool  alleGleich  = true;

        for (int i = 0; i < Spalten.Length; i++)
        {
            SlotSymbol kachel = Spalten[i].ErgebnisKachel;
            ergebnisIDs[i] = kachel != null ? kachel.SymbolID : -1;

            if (i > 0 && ergebnisIDs[i] != ergebnisIDs[0])
                alleGleich = false;
        }

        ErgebnisData data = new ErgebnisData
        {
            SymbolIDs  = ergebnisIDs,
            Jackpot    = alleGleich && ergebnisIDs[0] >= 0
        };

        Debug.Log($"[SlotMachine] Ergebnis: [{string.Join(", ", ergebnisIDs)}] | Jackpot: {data.Jackpot}");
        OnErgebnis?.Invoke(data);
    }

    // ─── Inspector-Button (für schnellen Test im Editor) ──────────────────────

    [ContextMenu("Spin starten (Test)")]
    private void TestSpin() => StarteSpinZyklus();
}

// ─── Datenstruktur für Events ──────────────────────────────────────────────────

[System.Serializable]
public class ErgebnisData
{
    public int[]  SymbolIDs;
    public bool   Jackpot;
}
