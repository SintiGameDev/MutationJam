using UnityEngine;

public class SlotSymbolInstance
{
    public SlotSymbolDefinition Definition { get; private set; }
    public int Mutationsstufe { get; private set; }

    public SlotSymbolInstance(SlotSymbolDefinition definition)
    {
        Definition = definition;
        Mutationsstufe = 0;
    }

    public void BeiAusspielung()
    {
        Mutationsstufe++;
    }

    public float GetMultiplier()
    {
        return Definition.MultiplierKurve.Evaluate(Mutationsstufe);
    }

    public int GetGewinn()
    {
        return Mathf.RoundToInt(Definition.Wert * GetMultiplier());
    }
}
