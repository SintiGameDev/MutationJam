using System.Collections.Generic;
using UnityEngine;

// Spawnt Gegner aehnlich wie der FoodSpawner.
// Beachtet beim Platzieren:
//  - innerhalb der gridArea-Bounds
//  - nicht auf der Schlange (kein Segment)
//  - nicht zu nah am Kopf (mindestAbstandZumSpieler)
//  - nicht auf einem Food
//  - nicht auf einem anderen Gegner
//
// Tipp: Gib dem Gegner-Prefab den Tag "Obstacle", dann loest die
// bestehende Snake-Logik beim Beruehren automatisch ResetState() aus.
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Derselbe Bereich wie bei den Foods.")]
    public Collider2D gridArea;

    [Tooltip("Das Gegner-Prefab, das gespawnt wird.")]
    public Transform gegnerPrefab;

    [Min(0)]
    [Tooltip("Wie viele Gegner beim Start erzeugt werden.")]
    public int anzahl = 3;

    [Min(0f)]
    [Tooltip("Mindestabstand (in Grid-Feldern) zum Schlangenkopf.")]
    public float mindestAbstandZumSpieler = 5f;

    private Snake snake;

    // Aktuell platzierte Gegner, damit sie sich nicht gegenseitig ueberlappen
    private readonly List<Transform> gegner = new List<Transform>();

    private void Awake()
    {
        snake = FindObjectOfType<Snake>();
    }

    private void Start()
    {
        SpawneAlle();
    }

    // Erzeugt 'anzahl' Gegner und platziert jeden auf einem gueltigen Feld.
    public void SpawneAlle()
    {
        for (int i = 0; i < anzahl; i++)
        {
            Transform neuer = Instantiate(gegnerPrefab);
            gegner.Add(neuer);
            PlatziereGegner(neuer);
        }
    }

    // Platziert (oder versetzt) einen einzelnen Gegner auf ein zufaelliges
    // gueltiges Feld. Auch von aussen aufrufbar, falls Gegner spaeter
    // umziehen sollen.
    public void PlatziereGegner(Transform g)
    {
        List<Vector2Int> freieFelder = SammleGueltigeFelder(g);

        if (freieFelder.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: Kein gueltiges Feld gefunden " +
                "(evtl. mindestAbstandZumSpieler zu gross fuer das Feld).");
            return;
        }

        Vector2Int feld = freieFelder[Random.Range(0, freieFelder.Count)];
        g.position = new Vector2(feld.x, feld.y);
    }

    // Sammelt alle Felder im Grid, die alle Bedingungen erfuellen.
    private List<Vector2Int> SammleGueltigeFelder(Transform ausnahme)
    {
        List<Vector2Int> ergebnis = new List<Vector2Int>();
        Bounds bounds = gridArea.bounds;

        int minX = Mathf.RoundToInt(bounds.min.x);
        int maxX = Mathf.RoundToInt(bounds.max.x);
        int minY = Mathf.RoundToInt(bounds.min.y);
        int maxY = Mathf.RoundToInt(bounds.max.y);

        // Foods einmalig einsammeln (statt pro Feld erneut zu suchen)
        Food[] foods = FindObjectsOfType<Food>();

        Vector2 kopf = snake != null
            ? (Vector2)snake.transform.position
            : Vector2.zero;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (IstGueltig(x, y, kopf, foods, ausnahme))
                {
                    ergebnis.Add(new Vector2Int(x, y));
                }
            }
        }

        return ergebnis;
    }

    private bool IstGueltig(int x, int y, Vector2 kopf, Food[] foods, Transform ausnahme)
    {
        // 1. Nicht auf der Schlange
        if (snake != null && snake.Occupies(x, y))
        {
            return false;
        }

        // 2. Nicht zu nah am Kopf
        if (snake != null)
        {
            float distanz = Vector2.Distance(new Vector2(x, y), kopf);
            if (distanz < mindestAbstandZumSpieler)
            {
                return false;
            }
        }

        // 3. Nicht auf einem Food
        foreach (Food food in foods)
        {
            if (Mathf.RoundToInt(food.transform.position.x) == x &&
                Mathf.RoundToInt(food.transform.position.y) == y)
            {
                return false;
            }
        }

        // 4. Nicht auf einem anderen Gegner (sich selbst ausgenommen)
        foreach (Transform g in gegner)
        {
            if (g == null || g == ausnahme)
            {
                continue;
            }

            if (Mathf.RoundToInt(g.position.x) == x &&
                Mathf.RoundToInt(g.position.y) == y)
            {
                return false;
            }
        }

        return true;
    }

    // Entfernt alle gespawnten Gegner (z.B. beim Neustart).
    public void RaeumeAuf()
    {
        foreach (Transform g in gegner)
        {
            if (g != null)
            {
                Destroy(g.gameObject);
            }
        }
        gegner.Clear();
    }
}