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

    private Collider2D gridArea;

    private void Start()
    {
        // GridArea finden
        GameObject gridAreaObj = GameObject.FindGameObjectWithTag("GridArea");
        if (gridAreaObj != null)
        {
            gridArea = gridAreaObj.GetComponent<Collider2D>();
        }

        for (int i = 0; i < anzahl; i++)
        {
            // Food instanziieren
            GameObject foodInstance = Instantiate(foodPrefab);

            // Startposition zufaellig innerhalb der Walls setzen
            if (gridArea != null)
            {
                Bounds bounds = gridArea.bounds;
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomY = Random.Range(bounds.min.y, bounds.max.y);
                foodInstance.transform.position = new Vector2(randomX, randomY);
            }
        }
    }
}