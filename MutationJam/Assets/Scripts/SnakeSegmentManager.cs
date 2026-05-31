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
                int neueStufe    = gruppe.Key.Stufe + 1;
                snake.Grow(typ, neueStufe);

                Debug.Log($"Mutation: 3x {typ.bezeichnung} (Stufe {gruppe.Key.Stufe}) " +
                          $"-> 1x Stufe {neueStufe}. Verbleibend: {snake.Segments.Count}");
                return true;
            }
        }

        return false;
    }

    // Schluessel fuer die Gruppierung: kombiniert Nahrungstyp (Referenzgleichheit)
    // und Mutationsstufe zu einem Dictionary-tauglichen Wert.
    private struct KomboSchluessel : System.IEquatable<KomboSchluessel>
    {
        public readonly Nahrungstyp Typ;
        public readonly int Stufe;

        public KomboSchluessel(Nahrungstyp typ, int stufe)
        {
            Typ   = typ;
            Stufe = stufe;
        }

        public bool Equals(KomboSchluessel other)
        {
            // Nahrungstyp wird per Referenz verglichen – gleiche Food-Typen
            // teilen sich dieselbe Instanz (Inspector-Array bzw. statische Typen).
            return ReferenceEquals(Typ, other.Typ) && Stufe == other.Stufe;
        }

        public override bool Equals(object obj)
        {
            return obj is KomboSchluessel andere && Equals(andere);
        }

        public override int GetHashCode()
        {
            int typHash = Typ != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Typ) : 0;
            return typHash * 397 ^ Stufe;
        }
    }
}
