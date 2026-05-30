using UnityEngine;

// Steht als eigene Datei, damit Food, SnakeSegment und Snake
// alle auf diesen Typ zugreifen koennen.
[System.Serializable]
public class Nahrungstyp
{
    public string bezeichnung;          // z.B. "Herz", "Kirschen", "Sterne"
    public Color farbe = Color.white;   // Im Inspector setzen – Alpha nicht vergessen!

    //roten und blauen Typen als Beispiel, damit du sofort loslegen kannst:
    public static readonly Nahrungstyp Rot = new Nahrungstyp { bezeichnung = "Rot", farbe = Color.red };
    public static readonly Nahrungstyp Blau = new Nahrungstyp { bezeichnung = "Blau", farbe = Color.blue };
    public static readonly Nahrungstyp Gruen = new Nahrungstyp { bezeichnung = "Gruen", farbe = Color.green };
    public static readonly Nahrungstyp Orange = new Nahrungstyp { bezeichnung = "Orange", farbe = new Color(1f, 0.5f, 0f) };
}
