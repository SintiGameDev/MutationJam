using UnityEngine;

[System.Serializable]
public class Nahrungstyp
{
    public string bezeichnung;
    public Color  farbe = Color.white;

    [Tooltip("Material fuer die 3D-Sphere des Segments (und des Foods).")]
    public Material material;

    [Tooltip("Welche Turmkonfiguration Segmente dieses Typs erhalten.")]
    public TurmKonfiguration turmKonfiguration;

    public static readonly Nahrungstyp Rot    = new Nahrungstyp { bezeichnung = "Rot",    farbe = Color.red };
    public static readonly Nahrungstyp Blau   = new Nahrungstyp { bezeichnung = "Blau",   farbe = Color.blue };
    public static readonly Nahrungstyp Gruen  = new Nahrungstyp { bezeichnung = "Gruen",  farbe = Color.green };
    public static readonly Nahrungstyp Orange = new Nahrungstyp { bezeichnung = "Orange", farbe = new Color(1f, 0.5f, 0f) };
}
