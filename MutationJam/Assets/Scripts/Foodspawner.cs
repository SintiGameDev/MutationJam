using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Das Food Prefab. gridArea und typen sollten am Prefab gesetzt sein.")]
    public GameObject foodPrefab;
    private Collider2D gridArea;

    [Header("Wellen Einstellungen")]
    [Tooltip("Zeit in Sekunden, wie lange eine Welle dauert, bevor gewechselt wird.")]
    public float zeitProWelle = 15f;

    [Header("Food Mengen")]
    public int anzahlKnapp = 1;
    public int anzahlNormal = 3;
    public int anzahlUeberfluss = 8;

    private int zielAnzahl;
    private List<GameObject> aktiveFoods = new List<GameObject>();

    private void Start()
    {
        // GridArea finden
        GameObject gridAreaObj = GameObject.FindGameObjectWithTag("GridArea");
        if (gridAreaObj != null)
        {
            gridArea = gridAreaObj.GetComponent<Collider2D>();
        }

        // Start mit normaler Welle
        zielAnzahl = anzahlNormal;
        StartCoroutine(WellenRoutine());
    }

    private void Update()
    {
        // 1. Bereinigt die Liste von Foods, die z.B. vom Spieler gegessen (zerstoert) wurden
        aktiveFoods.RemoveAll(f => f == null);

        // 2. Zu wenig Food auf dem Feld -> Neues Food spawnen
        while (aktiveFoods.Count < zielAnzahl)
        {
            SpawneFood();
        }

        // 3. Zu viel Food auf dem Feld (z.B. wenn Ueberfluss in Knappheit wechselt) -> Abbauen
        while (aktiveFoods.Count > zielAnzahl)
        {
            // Nimmt das zuletzt gespawnte Food und zerstoert es
            GameObject foodToRemove = aktiveFoods[aktiveFoods.Count - 1];
            aktiveFoods.RemoveAt(aktiveFoods.Count - 1);
            Destroy(foodToRemove);
        }
    }

    private IEnumerator WellenRoutine()
    {
        while (true)
        {
            // Wartet die eingestellte Zeit ab
            yield return new WaitForSeconds(zeitProWelle);

            // Waehlt zufaellig den naechsten Zustand
            int zufall = Random.Range(0, 3);

            if (zufall == 0)
            {
                zielAnzahl = anzahlKnapp;
            }
            else if (zufall == 1)
            {
                zielAnzahl = anzahlNormal;
            }
            else
            {
                zielAnzahl = anzahlUeberfluss;
            }
        }
    }

    private void SpawneFood()
    {
        if (foodPrefab == null) return;

        GameObject foodInstance = Instantiate(foodPrefab);

        if (gridArea != null)
        {
            Bounds bounds = gridArea.bounds;

            // Auf volle Grid Koordinaten runden (wie bei den Gegnern)
            float randomX = Mathf.Round(Random.Range(bounds.min.x, bounds.max.x));
            float randomY = Mathf.Round(Random.Range(bounds.min.y, bounds.max.y));

            foodInstance.transform.position = new Vector3(randomX, randomY, 0f);
        }

        aktiveFoods.Add(foodInstance);
    }
}