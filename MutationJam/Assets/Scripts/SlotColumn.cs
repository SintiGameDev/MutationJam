using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Verwaltet eine einzelne Slot-Spalte.
/// Spawnt Kacheln oben, lässt sie nach unten fallen,
/// friert sie auf Befehl ein und tweent die Mittellinie-Kachel auf den Snap-Y.
/// </summary>
public class SlotColumn : MonoBehaviour
{
    // ─── Inspector-Einstellungen ───────────────────────────────────────────────

    [Header("Referenzen")]
    [Tooltip("Prefab der Slot-Kachel (mit SlotSymbol-Komponente)")]
    public SlotSymbol KachelPrefab;

    [Tooltip("Verfügbare Symbole (Sprites + IDs)")]
    public SymbolDaten[] VerfügbareSymbole;

    [Header("Layout")]
    [Tooltip("Y-Koordinate des Spawn-Punktes (oben)")]
    public float SpawnY = 5f;

    [Tooltip("Y-Koordinate der Auswertungs-Mittellinie")]
    public float MittellinieY = 0f;

    [Tooltip("Y-Koordinate unterhalb der die Kachel zerstört wird")]
    public float DestroyY = -5f;

    [Tooltip("Abstand zwischen zwei Kacheln (Center-to-Center)")]
    public float KachelAbstand = 1.2f;

    [Header("Bewegung")]
    [Tooltip("Fall-Geschwindigkeit in Units/Sekunde")]
    public float FallGeschwindigkeit = 4f;

    [Tooltip("Dauer des Tween beim Einfrieren (Sekunden)")]
    public float TweenDauer = 0.25f;

    // ─── Laufzeit-State ───────────────────────────────────────────────────────

    private readonly List<SlotSymbol> _aktiveKacheln = new();
    private bool  _läuft;
    private bool  _eingefroren;
    private float _nächsterSpawnY;  // Nächste Y-Position für einen Spawn

    // Das Ergebnis-Symbol (auf Höhe der Mittellinie nach dem Freeze)
    public SlotSymbol ErgebnisKachel { get; private set; }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Startet das Drehen der Spalte.</summary>
    public void StarteSpin()
    {
        if (_läuft) return;
        _läuft        = true;
        _eingefroren  = false;
        ErgebnisKachel = null;

        // Erste Kachel sofort spawnen, Rest folgt über Update-Logik
        SpawneKachel();
    }

    /// <summary>Stoppt den Spin und friert alle Kacheln ein.
    /// Die Kachel, die der Mittellinie am nächsten ist, wird auf MittellinieY getweened.</summary>
    public void FreezeSpalte()
    {
        if (_eingefroren) return;
        _eingefroren = true;
        _läuft       = false;

        // Nächste Kachel zur Mittellinie bestimmen
        SlotSymbol nächste = HoleNächsteKachelZurMittellinie();
        ErgebnisKachel = nächste;

        // Offset berechnen, um alle Kacheln aufzureihen
        float offset = nächste != null ? (MittellinieY - nächste.transform.position.y) : 0f;

        foreach (SlotSymbol k in _aktiveKacheln)
        {
            float zielY = k.transform.position.y + offset;
            k.FreezeMitTween(zielY, TweenDauer);
        }
    }

    /// <summary>Setzt die Spalte komplett zurück (z.B. für neuen Spin).</summary>
    public void Reset()
    {
        foreach (SlotSymbol k in _aktiveKacheln)
            if (k != null) Destroy(k.gameObject);

        _aktiveKacheln.Clear();
        _läuft         = false;
        _eingefroren   = false;
        ErgebnisKachel = null;
    }

    /// <summary>Wird von SlotSymbol aufgerufen, wenn eine Kachel zerstört wird.</summary>
    public void MeldeKachelZerstört(SlotSymbol kachel)
    {
        _aktiveKacheln.Remove(kachel);
    }

    // ─── Unity-Lifecycle ──────────────────────────────────────────────────────

    void Update()
    {
        if (!_läuft || _eingefroren) return;

        // Nächsten Spawn prüfen:
        // Sobald die zuletzt gespawnte Kachel weit genug nach unten ist, kommt eine neue
        bool spawnNötig = _aktiveKacheln.Count == 0;

        if (!spawnNötig && _aktiveKacheln.Count > 0)
        {
            SlotSymbol letzte = _aktiveKacheln[_aktiveKacheln.Count - 1];
            if (letzte != null && SpawnY - letzte.transform.position.y >= KachelAbstand)
                spawnNötig = true;
        }

        if (spawnNötig)
            SpawneKachel();
    }

    // ─── Private Hilfsmethoden ────────────────────────────────────────────────

    private void SpawneKachel()
    {
        SymbolDaten data   = ZufälligesSymbol();
        Vector3     pos    = new Vector3(transform.position.x, SpawnY, transform.position.z);
        SlotSymbol  kachel = Instantiate(KachelPrefab, pos, Quaternion.identity, transform);

        kachel.SymbolID              = data.ID;
        kachel.Icon                  = data.Sprite;
        kachel.FallGeschwindigkeit   = FallGeschwindigkeit;
        kachel.DestroyUntergrenze    = DestroyY;
        kachel.ZugehörigeColumn      = this;

        // Sprite setzen (falls SpriteRenderer vorhanden)
        var sr = kachel.GetComponent<SpriteRenderer>();
        if (sr != null && data.Sprite != null)
            sr.sprite = data.Sprite;

        _aktiveKacheln.Add(kachel);
    }

    private SlotSymbol HoleNächsteKachelZurMittellinie()
    {
        SlotSymbol beste   = null;
        float      minDist = float.MaxValue;

        foreach (SlotSymbol k in _aktiveKacheln)
        {
            if (k == null) continue;
            float dist = Mathf.Abs(k.transform.position.y - MittellinieY);
            if (dist < minDist)
            {
                minDist = dist;
                beste   = k;
            }
        }
        return beste;
    }

    private SymbolDaten ZufälligesSymbol()
    {
        if (VerfügbareSymbole == null || VerfügbareSymbole.Length == 0)
            return new SymbolDaten { ID = 0 };

        return VerfügbareSymbole[Random.Range(0, VerfügbareSymbole.Length)];
    }
}

// ─── Datenstrukturen ──────────────────────────────────────────────────────────

[System.Serializable]
public struct SymbolDaten
{
    public int    ID;
    public Sprite Sprite;
    [Tooltip("Optionaler Name für Debugging und Auswertungs-Logs")]
    public string Name;
}
