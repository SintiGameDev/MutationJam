using UnityEngine;

public class SnakeSegment : MonoBehaviour
{
    public Nahrungstyp Typ { get; private set; }

    // Referenz auf den aktuell angebrachten Turm (fuer spaetere Upgrades)
    public Tower AktuellerTurm { get; private set; }

    // Wird von Snake.Grow() gesetzt, bevor SetzeTyp() aufgerufen wird.
    // So braucht SnakeSegment kein eigenes serialisiertes Prefab-Feld.
    public GameObject StandardTurmPrefab { private get; set; }

    public void SetzeTyp(Nahrungstyp typ)
    {
        Typ = typ;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && typ != null) {
            sr.color = typ.farbe;
        }

        SpawneTurm(typ);
    }

    private void SpawneTurm(Nahrungstyp typ)
    {
        // Konfiguration und Prefab aus dem Typ holen, sonst Standard nutzen
        TurmKonfiguration config    = typ?.turmKonfiguration;
        GameObject prefabZuSpawnen  = (config != null) ? config.turmPrefab : StandardTurmPrefab;

        if (prefabZuSpawnen == null) {
            return;
        }

        GameObject turmGO = Instantiate(prefabZuSpawnen, transform.position, Quaternion.identity, transform);
        turmGO.transform.localPosition = Vector3.zero;

        AktuellerTurm = turmGO.GetComponent<Tower>();

        if (AktuellerTurm != null && config != null)
        {
            AktuellerTurm.range    = config.reichweite;
            AktuellerTurm.fireRate = config.schussrate;
        }
    }

    // Tauscht den Turm gegen eine neue Konfiguration aus (fuer spaetere Upgrades)
    public void AktualisiereTurm(TurmKonfiguration neueKonfig)
    {
        if (AktuellerTurm != null) {
            Destroy(AktuellerTurm.gameObject);
            AktuellerTurm = null;
        }

        if (neueKonfig == null || neueKonfig.turmPrefab == null) {
            return;
        }

        GameObject turmGO = Instantiate(neueKonfig.turmPrefab, transform.position, Quaternion.identity, transform);
        turmGO.transform.localPosition = Vector3.zero;

        AktuellerTurm = turmGO.GetComponent<Tower>();

        if (AktuellerTurm != null)
        {
            AktuellerTurm.range    = neueKonfig.reichweite;
            AktuellerTurm.fireRate = neueKonfig.schussrate;
        }
    }
}
