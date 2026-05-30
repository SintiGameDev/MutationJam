using UnityEngine;

/// <summary>
/// Visuelle Komponente eines einzelnen Slots auf der Walze.
/// Sitzt auf dem SlotPrefab und verwaltet die Darstellung des Symbols.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SlotVisual : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector-Konfiguration
    // -------------------------------------------------------------------------

    [Header("Optionale Debug-Anzeige")]
    [Tooltip("Zeigt Mutationsstufe und Wert im Scene-View als Gizmo-Label.")]
    public bool DebugInfoAnzeigen = false;

    // -------------------------------------------------------------------------
    // Laufzeit-Zustand
    // -------------------------------------------------------------------------

    private SpriteRenderer _spriteRenderer;
    private SlotSymbolInstance _aktuelleInstanz;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // -------------------------------------------------------------------------
    // Oeffentliche Steuerung
    // -------------------------------------------------------------------------

    /// <summary>
    /// Weist diesem Slot eine neue SymbolInstanz zu und aktualisiert die Darstellung.
    /// </summary>
    public void SetzeInstanz(SlotSymbolInstance instanz)
    {
        _aktuelleInstanz = instanz;
        AktualisiereDarstellung();
    }

    public SlotSymbolInstance GetInstanz() => _aktuelleInstanz;

    // -------------------------------------------------------------------------
    // Interne Darstellung
    // -------------------------------------------------------------------------

    private void AktualisiereDarstellung()
    {
        if (_aktuelleInstanz?.Definition?.Symbol == null)
        {
            _spriteRenderer.sprite = null;
            return;
        }

        _spriteRenderer.sprite = _aktuelleInstanz.Definition.Symbol;
    }

    // -------------------------------------------------------------------------
    // Debug Gizmos
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!DebugInfoAnzeigen || _aktuelleInstanz == null) return;

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.4f,
            $"{_aktuelleInstanz.Definition?.name ?? "?"}\n" +
            $"Stufe: {_aktuelleInstanz.Mutationsstufe} | " +
            $"x{_aktuelleInstanz.GetMultiplier():F2}"
        );
    }
#endif
}
