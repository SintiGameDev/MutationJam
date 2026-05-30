using UnityEngine;

// Steht als eigene Datei, damit Food, SnakeSegment und Snake
// alle auf diesen Typ zugreifen koennen.
[System.Serializable]
public class Nahrungstyp
{
    public string bezeichnung;          // z.B. "Herz", "Kirschen", "Sterne"
    public Color farbe = Color.white;   // Im Inspector setzen – Alpha nicht vergessen!
}
