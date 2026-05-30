using UnityEngine;

[System.Serializable]
public class Nahrungstyp
{
    public string bezeichnung;
    public Color farbe = Color.white;

    [Tooltip("Welche Turmkonfiguration Segmente dieses Typs erhalten. " +
             "Leer lassen = Standard-Turm aus Snake.standardTurmPrefab.")]
    public TurmKonfiguration turmKonfiguration;

    public static readonly Nahrungstyp Rot    = new Nahrungstyp { bezeichnung = "Rot",    farbe = Color.red };
    public static readonly Nahrungstyp Blau   = new Nahrungstyp { bezeichnung = "Blau",   farbe = Color.blue };
    public static readonly Nahrungstyp Gruen  = new Nahrungstyp { bezeichnung = "Gruen",  farbe = Color.green };
    public static readonly Nahrungstyp Orange = new Nahrungstyp { bezeichnung = "Orange", farbe = new Color(1f, 0.5f, 0f) };
}
