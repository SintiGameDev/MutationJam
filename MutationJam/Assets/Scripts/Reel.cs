using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Steuert eine einzelne Walze des Spielautomaten.
/// Verwaltet das Symbol-Recycling, die Scroll-Bewegung und das Einrasten.
/// </summary>
public class Reel : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector-Konfiguration
    // -------------------------------------------------------------------------

    [Header("Symbol-Pool")]
    [Tooltip("Alle moeglichen Symbole fuer diese Walze.")]
    public List<SlotSymbolDefinition> SymbolDefinitionen = new();

    [Header("Walzen-Geometrie")]
    [Tooltip("Hoehe eines einzelnen Slots in World-Units.")]
    public float SlotHoehe = 1f;

    [Tooltip("Anzahl sichtbarer Slots (ohne die zwei halben Puffer).")]
    public int SichtbareReihen = 3;

    [Header("Scroll-Geschwindigkeit")]
    [Tooltip("Scroll-Geschwindigkeit in World-Units pro Sekunde.")]
    public float ScrollGeschwindigkeit = 8f;

    [Header("Stopp-Verhalten")]
    [Tooltip("Abbremskurve beim Einrasten (X = normierte Zeit, Y = Geschwindigkeitsfaktor).")]
    public AnimationCurve AbbremsKurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Tooltip("Dauer der Abbrems-Animation in Sekunden.")]
    public float AbbremsDauer = 0.4f;

    [Header("Referenzen")]
    [Tooltip("Prefab fuer einen einzelnen Slot (benoetigt SlotVisual-Komponente).")]
    public GameObject SlotPrefab;

    // -------------------------------------------------------------------------
    // Laufzeit-Zustand
    // -------------------------------------------------------------------------

    // Alle aktiven Slot-Transforms in Scroll-Reihenfolge (oben nach unten)
    private List<Transform> _slotTransforms = new();

    // Jeder Slot hat genau eine SlotSymbolInstance
    private List<SlotSymbolInstance> _slotInstanzen = new();

    // Anzahl der im Buffer verwalteten Slots (sichtbar + 2 Puffer)
    private int _bufferGroesse;

    // Untere Y-Grenze: Slots unterhalb werden recycled
    private float _untereGrenze;

    // Obere Y-Startposition fuer recycelte Slots
    private float _obereStartPosition;

    // Aktueller Scroll-Zustand
    private bool _laeuft = false;
    private bool _stoppAngefordert = false;
    private Coroutine _stoppCoroutine;

    /// <summary>Gibt an ob die Walze gerade scrollt (lesbar fuer ReelManager).</summary>
    public bool Laeuft => _laeuft;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Buffer: sichtbare Reihen + 1 halber oben + 1 halber unten + 1 Sicherheits-Puffer
        _bufferGroesse = SichtbareReihen + 3;
        BerechnGrenzen();
        ErstelleSlots();
    }

    // -------------------------------------------------------------------------
    // Initialisierung
    // -------------------------------------------------------------------------

    private void BerechnGrenzen()
    {
        // Mittelpunkt der Walze liegt bei localPosition.y = 0
        // Sichtbarer Bereich: SichtbareReihen * SlotHoehe + je eine halbe Reihe oben/unten
        float sichtbarerBereich = (SichtbareReihen + 1) * SlotHoehe;

        _untereGrenze = -(sichtbarerBereich / 2f) - SlotHoehe;
        _obereStartPosition = (sichtbarerBereich / 2f) + SlotHoehe;
    }

    private void ErstelleSlots()
    {
        if (SlotPrefab == null)
        {
            Debug.LogError($"[Reel] SlotPrefab fehlt auf {gameObject.name}!");
            return;
        }

        if (SymbolDefinitionen == null || SymbolDefinitionen.Count == 0)
        {
            Debug.LogError($"[Reel] Keine SymbolDefinitionen auf {gameObject.name} konfiguriert!");
            return;
        }

        for (int i = 0; i < _bufferGroesse; i++)
        {
            // Position: von oben nach unten aufsteigend
            float yPos = _obereStartPosition - (i * SlotHoehe);
            GameObject slot = Instantiate(SlotPrefab, transform);
            slot.transform.localPosition = new Vector3(0f, yPos, 0f);
            slot.name = $"Slot_{i}";

            _slotTransforms.Add(slot.transform);

            // Zufaellige Startbelegung
            SlotSymbolInstance instanz = ErstelleZufaelligeInstanz();
            _slotInstanzen.Add(instanz);
            AktualisiereSlotDarstellung(i, instanz);
        }
    }

    // -------------------------------------------------------------------------
    // Oeffentliche Steuerung
    // -------------------------------------------------------------------------

    /// <summary>Startet das Scrollen der Walze.</summary>
    public void Starten()
    {
        if (_laeuft) return;

        _laeuft = true;
        _stoppAngefordert = false;
    }

    /// <summary>
    /// Fordert einen sauberen Stopp an.
    /// Die Walze laeuft bis zum naechsten Slot-Einrastpunkt weiter.
    /// </summary>
    public void StoppAnfordern()
    {
        if (!_laeuft || _stoppAngefordert) return;

        _stoppAngefordert = true;
        _stoppCoroutine = StartCoroutine(SauberStoppen());
    }

    /// <summary>Gibt die SlotSymbolInstance der mittleren (Gewinn-)Reihe zurueck.</summary>
    public SlotSymbolInstance GetMittlereInstanz()
    {
        int mittlererIndex = GetMittlerenSlotIndex();
        if (mittlererIndex < 0 || mittlererIndex >= _slotInstanzen.Count) return null;
        return _slotInstanzen[mittlererIndex];
    }

    // -------------------------------------------------------------------------
    // Update - Scroll-Loop
    // -------------------------------------------------------------------------

    private void Update()
    {
        if (!_laeuft) return;

        float bewegung = ScrollGeschwindigkeit * Time.deltaTime;

        for (int i = 0; i < _slotTransforms.Count; i++)
        {
            _slotTransforms[i].localPosition += Vector3.down * bewegung;

            // Recycling: Slot unterhalb der Grenze nach oben teleportieren
            if (_slotTransforms[i].localPosition.y < _untereGrenze)
            {
                RecycleSlot(i);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Recycling
    // -------------------------------------------------------------------------

    private void RecycleSlot(int index)
    {
        // Hoechste aktuelle Y-Position finden
        float hoechsteY = float.MinValue;
        foreach (var t in _slotTransforms)
            hoechsteY = Mathf.Max(hoechsteY, t.localPosition.y);

        // Slot direkt ueber den obersten Slot setzen
        _slotTransforms[index].localPosition = new Vector3(0f, hoechsteY + SlotHoehe, 0f);

        // Neue zufaellige Instanz zuweisen
        SlotSymbolInstance neueInstanz = ErstelleZufaelligeInstanz();
        _slotInstanzen[index] = neueInstanz;
        AktualisiereSlotDarstellung(index, neueInstanz);
    }

    // -------------------------------------------------------------------------
    // Stopp-Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator SauberStoppen()
    {
        // Warte bis der naechste saubere Einrastpunkt erreichbar ist
        yield return new WaitUntil(NaechsterEinrastpunktErreicht);

        // Abbremsen
        float timer = 0f;
        float startGeschwindigkeit = ScrollGeschwindigkeit;

        while (timer < AbbremsDauer)
        {
            timer += Time.deltaTime;
            float faktor = AbbremsKurve.Evaluate(timer / AbbremsDauer);
            float aktuelleGeschwindigkeit = startGeschwindigkeit * faktor;

            for (int i = 0; i < _slotTransforms.Count; i++)
            {
                _slotTransforms[i].localPosition += Vector3.down * aktuelleGeschwindigkeit * Time.deltaTime;

                if (_slotTransforms[i].localPosition.y < _untereGrenze)
                    RecycleSlot(i);
            }

            yield return null;
        }

        // Exakt auf Slot-Raster einrasten
        EinrastenAufRaster();
        _laeuft = false;
    }

    private bool NaechsterEinrastpunktErreicht()
    {
        // Einrasten sobald ein Slot nahe am Raster-Mittelpunkt ist
        foreach (var t in _slotTransforms)
        {
            float modulo = Mathf.Abs(t.localPosition.y % SlotHoehe);
            if (modulo < SlotHoehe * 0.1f) return true;
        }
        return false;
    }

    private void EinrastenAufRaster()
    {
        // Naechstgelegenen Raster-Y-Wert fuer jeden Slot berechnen
        for (int i = 0; i < _slotTransforms.Count; i++)
        {
            float y = _slotTransforms[i].localPosition.y;
            float eingerasteteY = Mathf.Round(y / SlotHoehe) * SlotHoehe;
            _slotTransforms[i].localPosition = new Vector3(0f, eingerasteteY, 0f);
        }
    }

    // -------------------------------------------------------------------------
    // Hilfsmethoden
    // -------------------------------------------------------------------------

    private SlotSymbolInstance ErstelleZufaelligeInstanz()
    {
        int zufaelligerIndex = Random.Range(0, SymbolDefinitionen.Count);
        return new SlotSymbolInstance(SymbolDefinitionen[zufaelligerIndex]);
    }

    private void AktualisiereSlotDarstellung(int index, SlotSymbolInstance instanz)
    {
        SlotVisual visual = _slotTransforms[index].GetComponentInChildren<SlotVisual>();
        if (visual != null)
            visual.SetzeInstanz(instanz);
    }

    /// <summary>
    /// Gibt den Index des Slots zurueck, der am naechsten an der Mitte (Y=0) liegt.
    /// </summary>
    private int GetMittlerenSlotIndex()
    {
        int besterIndex = 0;
        float kleinsterAbstand = float.MaxValue;

        for (int i = 0; i < _slotTransforms.Count; i++)
        {
            float abstand = Mathf.Abs(_slotTransforms[i].localPosition.y);
            if (abstand < kleinsterAbstand)
            {
                kleinsterAbstand = abstand;
                besterIndex = i;
            }
        }

        return besterIndex;
    }
}
