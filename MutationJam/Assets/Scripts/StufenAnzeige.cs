using UnityEngine;
using TMPro;

// Stufen-Badge im SCREEN-SPACE-OVERLAY. Statt im 3D-Raum zu rendern (wo es mit
// Tiefenpuffer und Render-Queue kaempfen muesste), wird das Badge auf einem
// Screen Space - Overlay Canvas platziert. Dieser Canvas wird NACH der ganzen
// 3D-Szene komponiert -> die Zahl liegt IMMER ueber Segment und Tower, ohne
// ZTest- oder Queue-Tricks.
//
// Das Skript projiziert jeden Frame die Welt-Position des Segments per
// WorldToScreenPoint auf den Bildschirm und setzt das Badge dorthin.
[RequireComponent(typeof(RectTransform))]
public class StufenAnzeige : MonoBehaviour
{
    private Transform     ziel;        // das Segment, dem gefolgt wird
    private Vector3       weltOffset;  // Offset im Weltraum (z.B. leicht ueber dem Segment)

    private RectTransform rect;
    private TMP_Text      text;
    private Camera        kamera;

    [Header("Pop beim Mergen")]
    [Tooltip("Auf wie viel die Zahl kurz hochskaliert (1.6 = 160 %).")]
    public float popSkala = 1.6f;
    [Tooltip("Gesamtdauer des Hoch-und-wieder-Runter-Tweens in Sekunden.")]
    public float popDauer = 0.25f;

    private Vector3 basisScale       = Vector3.one;
    private bool    istInitialisiert = false;

    public void Initialisiere(Transform zielSegment, Vector3 offset, int stufe)
    {
        ziel       = zielSegment;
        weltOffset = offset;

        rect   = GetComponent<RectTransform>();
        text   = GetComponentInChildren<TMP_Text>();
        kamera = Camera.main;

        // Ruhescale merken – Pop-Tweens kehren immer hierher zurueck.
        basisScale = rect.localScale;

        SetzeStufe(stufe);
        Positioniere();
        istInitialisiert = true;
        Pop();
    }

    public void SetzeStufe(int stufe)
    {
        if (text != null)
            text.text = stufe.ToString();

        if (istInitialisiert)
            Pop();
    }

    private void LateUpdate()
    {
        // Segment weg (Match aufgeloest / Reset / Gegner) -> Badge mitnehmen.
        if (ziel == null)
        {
            Destroy(gameObject);
            return;
        }

        Positioniere();
    }

    private void Positioniere()
    {
        if (kamera == null)
        {
            kamera = Camera.main;
            if (kamera == null) return;
        }

        Vector3 screenPos = kamera.WorldToScreenPoint(ziel.position + weltOffset);

        // z <= 0 bedeutet: Segment ist hinter der Kamera -> Badge ausblenden.
        bool sichtbar = screenPos.z > 0f;
        if (text != null && text.enabled != sichtbar)
            text.enabled = sichtbar;

        if (sichtbar)
        {
            // Bei Screen Space - Overlay ist rect.position direkt in Bildschirm-Pixeln.
            screenPos.z = 0f;
            rect.position = screenPos;
        }
    }

    // Kurzer Hoch-und-Runter-Scale als Merge-Feedback.
    private void Pop()
    {
        if (popSkala <= 0f || popDauer <= 0f) return;

        LeanTween.cancel(gameObject);
        rect.localScale = basisScale;

        LeanTween.scale(rect, basisScale * popSkala, popDauer * 0.5f)
                 .setEaseOutQuad()
                 .setOnComplete(() =>
                 {
                     if (this != null)
                         LeanTween.scale(rect, basisScale, popDauer * 0.5f)
                                  .setEaseInQuad();
                 });
    }
}
