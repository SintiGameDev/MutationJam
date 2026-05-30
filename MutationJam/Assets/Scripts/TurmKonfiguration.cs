using UnityEngine;

[CreateAssetMenu(fileName = "TurmKonfiguration", menuName = "Snake/Turmkonfiguration")]
public class TurmKonfiguration : ScriptableObject
{
    [Header("Aussehen")]
    public GameObject turmPrefab;

    [Header("Kampfwerte")]
    public float reichweite = 5f;
    public float schussrate = 1f;

    [Header("Projektil")]
    [Tooltip("Wird direkt auf Tower.projectilePrefab gesetzt. " +
             "Leer lassen = Prefab-Standardwert des Tower-Prefabs wird benutzt.")]
    public GameObject projektilPrefab;
}
