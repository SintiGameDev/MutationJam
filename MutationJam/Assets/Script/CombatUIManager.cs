using UnityEngine;
using TMPro;

public class CombatUIManager : MonoBehaviour
{
    // Variablen für die Lebenspunkte
    private int enemyHealth = 50;
    private int playerHealth = 100; // Startwert für Spielerleben, kann angepasst werden

    // Referenzen auf die UI Texte
    private TextMeshProUGUI enemyHealthUI;
    private TextMeshProUGUI playerHealthUI;
    private TextMeshProUGUI playerDamageUI;

    void Start()
    {
        // UI Elemente über Tags finden
        GameObject enemyTextObj = GameObject.FindGameObjectWithTag("EnemyHealthText");
        if (enemyTextObj != null)
        {
            enemyHealthUI = enemyTextObj.GetComponent<TextMeshProUGUI>();
        }

        GameObject playerTextObj = GameObject.FindGameObjectWithTag("PlayerHealthText");
        if (playerTextObj != null)
        {
            playerHealthUI = playerTextObj.GetComponent<TextMeshProUGUI>();
        }

        GameObject damageTextObj = GameObject.FindGameObjectWithTag("PlayerDamageText");
        if (damageTextObj != null)
        {
            playerDamageUI = damageTextObj.GetComponent<TextMeshProUGUI>();
        }

        // Initiale Texte setzen
        UpdateEnemyHealthText();
        UpdatePlayerHealthText();

        if (playerDamageUI != null)
        {
            playerDamageUI.text = "Damage: 0";
        }
    }

    // Diese Methode wird vom Testbutton für den Spielerangriff aufgerufen
    public void PlayerAttackTest()
    {
        int testDamage = 5; // Fester Testwert
        ApplyDamageToEnemy(testDamage);
    }

    // Diese Methode rufst du später aus deinem Slot Machine Skript auf
    public void ApplyDamageToEnemy(int damageAmount)
    {
        enemyHealth -= damageAmount;

        // Verhindern, dass das Leben unter 0 fällt
        if (enemyHealth < 0)
        {
            enemyHealth = 0;
        }

        // Angezeigten Schaden aktualisieren
        if (playerDamageUI != null)
        {
            playerDamageUI.text = "Damage: " + damageAmount.ToString();
        }

        UpdateEnemyHealthText();
    }

    // Diese Methode wird vom Testbutton für den Gegnerangriff aufgerufen
    public void EnemyAttackTest()
    {
        // Zufälliger Schaden zwischen 1 und 10
        int randomDamage = Random.Range(1, 11);
        playerHealth -= randomDamage;

        if (playerHealth < 0)
        {
            playerHealth = 0;
        }

        UpdatePlayerHealthText();
    }

    // Hilfsmethoden zur Aktualisierung der Texte
    private void UpdateEnemyHealthText()
    {
        if (enemyHealthUI != null)
        {
            enemyHealthUI.text = "EnemyLife: " + enemyHealth.ToString();
        }
    }

    private void UpdatePlayerHealthText()
    {
        if (playerHealthUI != null)
        {
            playerHealthUI.text = "PlayerLife: " + playerHealth.ToString();
        }
    }
}