using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Diese Struktur definiert die Einträge für dein Array im Inspector
[System.Serializable]
public struct EnemyData
{
    public string spriteName; // Muss exakt dem Dateinamen entsprechen, z.B. "goblin_archer"
    public int health;
    public int damage;
}

public class RoundManager : MonoBehaviour
{
    [Header("UI Referenzen")]
    public Image enemyImageDisplay; // Hier dein "EnemyImage" aus der Hierarchie reinziehen

    [Header("Gegner Datenbank")]
    public EnemyData[] enemyDatabase; // Dein Array für Health und Dmg

    private Sprite[] availableSprites;
    private TextMeshProUGUI enemyNameTextUI;
    private CombatUIManager combatUI;

    void Start()
    {
        // Lädt alle Sprites aus dem Ordner Assets/Resources/Enemies
        availableSprites = Resources.LoadAll<Sprite>("Enemies");

        // Text für den Namen per Tag suchen
        GameObject nameTextObj = GameObject.FindGameObjectWithTag("EnemyNameText");
        if (nameTextObj != null)
        {
            enemyNameTextUI = nameTextObj.GetComponent<TextMeshProUGUI>();
        }

        // Referenz auf dein bestehendes Kampf Skript holen
        combatUI = GetComponent<CombatUIManager>();
    }

    // Diese Methode legst du auf das OnClick Event deines NewRoundButtons
    public void SpawnNewEnemy()
    {
        if (availableSprites.Length == 0)
        {
            Debug.LogError("Keine Sprites gefunden! Bitte prüfen, ob sie im Ordner Resources/Enemies liegen.");
            return;
        }

        // Zufälliges Sprite aus dem Ordner wählen
        int randomIndex = Random.Range(0, availableSprites.Length);
        Sprite chosenSprite = availableSprites[randomIndex];

        // UI Bild aktualisieren
        if (enemyImageDisplay != null)
        {
            enemyImageDisplay.sprite = chosenSprite;
        }

        // UI Text mit dem Dateinamen aktualisieren
        if (enemyNameTextUI != null)
        {
            enemyNameTextUI.text = chosenSprite.name;
        }

        // Werte aus dem Array suchen und anwenden
        ApplyStatsToEnemy(chosenSprite.name);
    }

    private void ApplyStatsToEnemy(string nameOfSprite)
    {
        bool matchFound = false;

        // Geht das Array durch und sucht nach dem passenden Namen
        foreach (EnemyData data in enemyDatabase)
        {
            if (data.spriteName == nameOfSprite)
            {
                // Übergibt die Werte an dein anderes Skript
                if (combatUI != null)
                {
                    combatUI.InitializeNewEnemy(data.health, data.damage);
                }
                matchFound = true;
                break;
            }
        }

        if (!matchFound)
        {
            Debug.LogWarning("Keine Daten für " + nameOfSprite + " im Array gefunden!");
        }
    }
}