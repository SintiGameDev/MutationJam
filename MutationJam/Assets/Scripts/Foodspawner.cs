using UnityEngine;

// Optional: Spawnt beim Start mehrere Foods aus EINEM Prefab.
// Alternativ kannst du auch einfach mehrere Food-Objekte direkt in die
// Szene legen – dank der Registry in Food.cs ueberlappen sie sich nicht.
public class FoodSpawner : MonoBehaviour
{
    [Tooltip("Das Food-Prefab. gridArea und typen sollten am Prefab gesetzt sein.")]
    public GameObject foodPrefab;

    [Min(1)]
    [Tooltip("Wie viele Foods gleichzeitig auf dem Feld liegen sollen.")]
    public int anzahl = 3;

    private void Start()
    {
        for (int i = 0; i < anzahl; i++)
        {
            // Jedes instanziierte Food platziert sich in seinem eigenen Start()
            // selbst zufaellig und meidet dabei Schlange + andere Foods.
            Instantiate(foodPrefab);
        }
    }
}