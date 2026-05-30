using System.Collections.Generic;
using UnityEngine;

// Spawnt Gegner fortlaufend bis zu einer Maximalzahl.
// Platzierung beachtet (wie beim FoodSpawner):
//  - innerhalb der gridArea-Bounds
//  - nicht auf der Schlange (kein Segment)
//  - nicht zu nah am Kopf (mindestAbstandZumSpieler)
//  - nicht auf einem Food
//  - nicht auf einem anderen Gegner
//
// Hinweis: Gegner brauchen den Tag "Enemy" und einen EnemyHealthManager,
// damit Projektile/Schlangenkopf sie toeten koennen.
public class EnemySpawner : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Derselbe Bereich wie bei den Foods.")]
    public Collider2D gridArea;

    [Tooltip("Das Gegner-Prefab, das gespawnt wird.")]
    public Transform gegnerPrefab;

    [Header("Spawn-Verhalten")]
    [Min(0)]
    [Tooltip("Maximale Anzahl gleichzeitig lebender Gegner.")]
    public int maxGegner = 5;

    [Min(0f)]
    [Tooltip("Wartezeit (Sekunden) nach Spielstart, bevor gespawnt wird.")]
    public float startVerzoegerung = 2f;

    [Min(0.05f)]
    [Tooltip("Sekunden zwischen zwei Spawn-Versuchen (kleiner = schneller).")]
    public float spawnIntervall = 3f;

    [Header("Platzierung")]
    [Min(0f)]
    [Tooltip("Mindestabstand (in Grid-Feldern) zum Schlangenkopf.")]
    public float mindestAbstandZumSpieler = 5f;

    private Snake snake;

    // Aktuell platzierte Gegner (kann zerstoerte/null-Eintraege enthalten)
    private readonly List<Transform> gegner = new List<Transform>();

    // Anzahl noch lebender Gegner
    public int AnzahlGegner
    {
        get
        {
            int n = 0;
            foreach (Transform g in gegner)
            {
                if (g != null) n++;
            }
            return n;
        }
    }

    private void Awake()
    {
        snake = FindObjectOfType<Snake>();
    }

    private void Start()
    {
        float intervall = Mathf.Max(0.05f, spawnIntervall);
        // Nach startVerzoegerung, dann alle 'intervall' Sekunden SpawneEinen aufrufen
        InvokeRepeating(nameof(SpawneEinen), startVerzoegerung, intervall);
    }

    // Wird per InvokeRepeating regelmaessig aufgerufen: spawnt EINEN Gegner,
    // falls die Maximalzahl noch nicht erreicht ist.
    public void SpawneEinen()
    {
        EntferneZerstoerte();

        if (gegnerPrefab == null || gridArea == null)
        {
            return;
        }

        if (AnzahlGegner >= maxGegner)
        {
            return; // Feld voll – nichts tun
        }

        List<Vector2Int> freieFelder = SammleGueltigeFelder(null);

        if (freieFelder.Count == 0)
        {
            // Kein gueltiges Feld frei – beim naechsten Intervall erneut versuchen
            return;
        }

        Vector2Int feld = freieFelder[Random.Range(0, freieFelder.Count)];
        Transform neuer = Instantiate(
            gegnerPrefab,
            new Vector3(feld.x, feld.y, 0f),
            Quaternion.identity);

        gegner.Add(neuer);
    }

    // Versetzt einen bestehenden Gegner auf ein neues gueltiges Feld.
    public void PlatziereGegner(Transform g)
    {
        if (g == null) return;

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

    // Entfernt zerstoerte (null) Gegner aus der Liste.
    private void EntferneZerstoerte()
    {
        gegner.RemoveAll(g => g == null);
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