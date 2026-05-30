using UnityEngine;

// Wird zur Laufzeit an jeden neuen Schlangenblock gehaengt.
// Speichert die Kategorie des verschluckten Foods, damit spaetere
// Mechaniken (z.B. Matching, Punkte) wissen, was jeder Block ist.
public class SnakeSegment : MonoBehaviour
{
    public Nahrungstyp Typ { get; private set; }

    public void SetzeTyp(Nahrungstyp typ)
    {
        Typ = typ;

        if (typ == null) {
            return;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null) {
            sr.color = typ.farbe;
        }
    }
}
