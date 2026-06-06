using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Setup")]
    public Collider2D gridArea;
    public Transform gegnerPrefab;

    [Header("Basis Spawn Verhalten")]
    public int startMaxGegner = 20;
    public float startVerzoegerung = 2f;
    public float startSpawnIntervall = 1f;

    [Header("Endlos Skalierung")]
    public float maxGegnerZuwachsProMinute = 30f;
    public float intervallAbnahmeProMinute = 0.5f;
    public float minimalesSpawnIntervall = 0.15f;

    [Header("Platzierung")]
    public float mindestAbstandZumSpieler = 3f;

    private Snake snake;
    private readonly List<Transform> gegner = new List<Transform>();

    private float ueberlebensZeit = 0f;
    private float naechsterSpawnZeitpunkt = 0f;
    private int aktuellesMaxGegner;
    private float aktuellesIntervall;

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
        ResetSpawnerState();
    }

    private void Update()
    {
        ueberlebensZeit += Time.deltaTime;
        float minutenGespielt = ueberlebensZeit / 60f;

        aktuellesMaxGegner = startMaxGegner + Mathf.FloorToInt(minutenGespielt * maxGegnerZuwachsProMinute);

        aktuellesIntervall = startSpawnIntervall - (minutenGespielt * intervallAbnahmeProMinute);
        aktuellesIntervall = Mathf.Max(aktuellesIntervall, minimalesSpawnIntervall);

        if (Time.time >= naechsterSpawnZeitpunkt)
        {
            SpawneEinen();
            naechsterSpawnZeitpunkt = Time.time + aktuellesIntervall;
        }
    }

    public void SpawneEinen()
    {
        EntferneZerstoerte();

        if (gegnerPrefab == null || gridArea == null)
        {
            return;
        }

        if (AnzahlGegner >= aktuellesMaxGegner)
        {
            return;
        }

        List<Vector2Int> freieFelder = SammleGueltigeFelder(null);

        if (freieFelder.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: Kein freies Feld gefunden.");
            return;
        }

        Vector2Int feld = freieFelder[Random.Range(0, freieFelder.Count)];

        Transform neuer = Instantiate(
            gegnerPrefab,
            new Vector3(feld.x, feld.y, -1f),
            Quaternion.identity);

        gegner.Add(neuer);
    }

    public void PlatziereGegner(Transform g)
    {
        if (g == null) return;

        List<Vector2Int> freieFelder = SammleGueltigeFelder(g);

        if (freieFelder.Count == 0)
        {
            return;
        }

        Vector2Int feld = freieFelder[Random.Range(0, freieFelder.Count)];
        g.position = new Vector3(feld.x, feld.y, -1f);
    }

    private List<Vector2Int> SammleGueltigeFelder(Transform ausnahme)
    {
        List<Vector2Int> ergebnis = new List<Vector2Int>();
        Bounds bounds = gridArea.bounds;

        int minX = Mathf.RoundToInt(bounds.min.x);
        int maxX = Mathf.RoundToInt(bounds.max.x);
        int minY = Mathf.RoundToInt(bounds.min.y);
        int maxY = Mathf.RoundToInt(bounds.max.y);

        Food[] foods = FindObjectsOfType<Food>();
        Vector2 kopf = snake != null ? (Vector2)snake.transform.position : Vector2.zero;

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
        if (snake != null && snake.Occupies(x, y)) return false;

        if (snake != null)
        {
            float distanz = Vector2.Distance(new Vector2(x, y), kopf);
            if (distanz < mindestAbstandZumSpieler) return false;
        }

        foreach (Food food in foods)
        {
            if (Mathf.RoundToInt(food.transform.position.x) == x &&
                Mathf.RoundToInt(food.transform.position.y) == y) return false;
        }

        // Die Prüfung auf andere Gegner wurde hier absichtlich komplett entfernt
        // Dadurch blockieren sich die Gegner beim Spawnen nicht mehr gegenseitig

        return true;
    }

    private void EntferneZerstoerte()
    {
        gegner.RemoveAll(g => g == null);
    }

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

        ResetSpawnerState();
    }

    private void ResetSpawnerState()
    {
        ueberlebensZeit = 0f;
        aktuellesMaxGegner = startMaxGegner;
        aktuellesIntervall = startSpawnIntervall;
        naechsterSpawnZeitpunkt = Time.time + startVerzoegerung;
    }
}