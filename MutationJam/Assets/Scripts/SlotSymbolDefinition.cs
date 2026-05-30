using UnityEngine;

[CreateAssetMenu(fileName = "NeuesSymbol", menuName = "SlotMachine/Symbol Definition")]
public class SlotSymbolDefinition : ScriptableObject
{
    [Header("Darstellung")]
    public Sprite Symbol;

    [Header("Spielwert")]
    public int Wert;

    [Header("Multiplier-Kurve")]
    [Tooltip("X = Mutationsstufe, Y = Multiplier")]
    public AnimationCurve MultiplierKurve = AnimationCurve.Linear(0, 1, 10, 2);
}
