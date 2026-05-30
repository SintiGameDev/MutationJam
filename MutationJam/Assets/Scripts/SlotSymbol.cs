using UnityEngine;

/// <summary>
/// Einzelne Slot-Kachel. Fällt mit einer konfigurierbaren Geschwindigkeit nach unten
/// und zerstört sich, sobald sie den Destroy-Y-Wert unterschreitet.
/// </summary>
public class SlotSymbol : MonoBehaviour
{
    [Header("Symbol-Daten")]
    public int SymbolID;          // z.B. 0=Kirsche, 1=Stern, 2=Glocke ...
    public Sprite Icon;

    [HideInInspector] public float FallGeschwindigkeit;
    [HideInInspector] public float DestroyUntergrenze;
    [HideInInspector] public SlotColumn ZugehörigeColumn;

    private bool _eingefroren;
    private bool _zerstört;

    // Tween-Ziel (gesetzt von SlotColumn beim Freeze)
    private bool   _tweening;
    private float  _tweenZielY;
    private float  _tweenDauer;
    private float  _tweenTimer;
    private float  _tweenStartY;

    void Update()
    {
        if (_zerstört) return;

        if (_tweening)
        {
            _tweenTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_tweenTimer / _tweenDauer);
            t = EaseOutQuad(t);

            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(_tweenStartY, _tweenZielY, t);
            transform.position = pos;

            if (t >= 1f)
                _tweening = false;

            return;
        }

        if (_eingefroren) return;

        // Normale Fall-Bewegung
        transform.position += Vector3.down * FallGeschwindigkeit * Time.deltaTime;

        // Destroy-Check
        if (transform.position.y < DestroyUntergrenze)
            ZerstöreSymbol();
    }

    /// <summary>
    /// Friert die Kachel ein (hält Fall an) und tweent sie zu einer Ziel-Y-Position.
    /// </summary>
    public void FreezeMitTween(float zielY, float dauer)
    {
        _eingefroren  = true;
        _tweening     = true;
        _tweenZielY   = zielY;
        _tweenDauer   = dauer;
        _tweenTimer   = 0f;
        _tweenStartY  = transform.position.y;
    }

    /// <summary>
    /// Friert die Kachel ohne Tween ein (sofort).
    /// </summary>
    public void FreezeOhneTween()
    {
        _eingefroren = true;
        _tweening    = false;
    }

    /// <summary>
    /// Gibt die Kachel wieder frei (setzt Fall fort).
    /// </summary>
    public void Unfreeze()
    {
        _eingefroren = false;
    }

    private void ZerstöreSymbol()
    {
        if (_zerstört) return;
        _zerstört = true;
        ZugehörigeColumn?.MeldeKachelZerstört(this);
        Destroy(gameObject);
    }

    // Easing-Hilfsfunktion
    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
