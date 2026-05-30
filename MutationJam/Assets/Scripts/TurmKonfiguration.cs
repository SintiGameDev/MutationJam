using UnityEngine;

// ScriptableObject: Im Project-Fenster per
// Rechtsklick → Create → Snake / Turmkonfiguration anlegen.
// Jeder Nahrungstyp zeigt auf eine dieser Konfigurationen.
[CreateAssetMenu(fileName = "TurmKonfiguration", menuName = "Snake/Turmkonfiguration")]
public class TurmKonfiguration : ScriptableObject
{
    [Header("Aussehen")]
    public GameObject turmPrefab;       // Welches Prefab gespawnt wird

    [Header("Kampfwerte")]
    public float reichweite  = 5f;
    public float schussrate  = 1f;      // Schuesse pro Sekunde
}
