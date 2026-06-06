using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Snake))]
public class SnakeSegmentManager : MonoBehaviour
{
    private Snake snake;

    private void Awake()
    {
        snake = GetComponent<Snake>();
    }

    public void PruefeUndZerkleinereKette() => PruefeKombos();

    public void PruefeKombos()
    {
        // Kaskaden: nach jeder aufgeloesten Mutation erneut pruefen, da das
        // neu angehaengte Stufe-+1-Segment selbst ein neues Triple bilden kann.
        while (PruefeEinzelneKombo()) { }
    }

    // Sucht drei Segmente GLEICHEN Typs UND gleicher Mutationsstufe – egal wo
    // in der Schlange sie liegen (muessen NICHT nebeneinander sein).
    // Findet sich ein solches Triple: die drei entfernen und stattdessen EIN
    // neues Segment gleichen Typs mit Stufe +1 hinten anhaengen.
    private bool PruefeEinzelneKombo()
    {
        List<Transform> segmente = snake.Segments;

        // Index 0 ist der Kopf -> wird nie einbezogen.
        // Gruppen nach (Typ, Stufe). Pro Gruppe merken wir uns die Segment-Indizes.
        Dictionary<KomboSchluessel, List<int>> gruppen = new Dictionary<KomboSchluessel, List<int>>();

        for (int i = 1; i < segmente.Count; i++)
        {
            if (segmente[i] == null) continue;

            SnakeSegment seg = segmente[i].GetComponent<SnakeSegment>();
            if (seg == null || seg.Typ == null) continue;

            KomboSchluessel key = new KomboSchluessel(seg.Typ, seg.Mutationsstufe);

            if (!gruppen.TryGetValue(key, out List<int> indizes))
            {
                indizes = new List<int>();
                gruppen[key] = indizes;
            }
            indizes.Add(i);
        }

        foreach (KeyValuePair<KomboSchluessel, List<int>> gruppe in gruppen)
        {
            if (gruppe.Value.Count >= 3)
            {
                // Die ersten drei Vorkommen entfernen (Reihenfolge egal fuers Spiel,
                // da nur die Anzahl zaehlt).
                List<int> zuEntfernen = gruppe.Value.GetRange(0, 3);
                snake.EntferneSegmenteAnIndizes(zuEntfernen);

                // Ein Segment gleichen Typs, eine Stufe hoeher, hinten anhaengen.
                Nahrungstyp typ = gruppe.Key.Typ;
                int neueStufe = gruppe.Key.Stufe + 1;
                snake.Grow(typ, neueStufe);   // spieleSpawnSound bleibt false -> kein Spawn-Sound

                // Merge-Sound (statt Spawn-Sound) fuer die Mutation.
                SoundManager.Instance?.SpieleMerge();

                Debug.Log($"Mutation: 3x {typ.bezeichnung} (Stufe {gruppe.Key.Stufe}) " +
                          $"-> 1x Stufe {neueStufe}. Verbleibend: {snake.Segments.Count}");
                return true;
            }
        }

        return false;
    }

    // Schluessel fuer die Gruppierung. Verglichen wird ueber die BEZEICHNUNG
    // (nicht die Objekt-Referenz!), weil Nahrungstyp eine schlichte
    // [System.Serializable]-Klasse ist: zwei "rote" Foods aus verschiedenen
    // Food-Objekten oder verschiedenen Array-Slots sind verschiedene Instanzen,
    // aber dieselbe Sorte. Die Typ-Referenz selbst behalten wir nur, um beim
    // Anhaengen des Stufe-+1-Segments Farbe/Material/Turmkonfig mitzugeben.
    private struct KomboSchluessel : System.IEquatable<KomboSchluessel>
    {
        public readonly Nahrungstyp Typ;   // Repraesentant der Sorte
        public readonly int Stufe;

        public KomboSchluessel(Nahrungstyp typ, int stufe)
        {
            Typ = typ;
            Stufe = stufe;
        }

        // Stabile Identitaet der Sorte = bezeichnung
        private string Id => Typ != null ? Typ.bezeichnung : null;

        public bool Equals(KomboSchluessel other)
        {
            return Stufe == other.Stufe && string.Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is KomboSchluessel andere && Equals(andere);
        }

        public override int GetHashCode()
        {
            int idHash = Id != null ? Id.GetHashCode() : 0;
            return idHash * 397 ^ Stufe;
        }
    }
}