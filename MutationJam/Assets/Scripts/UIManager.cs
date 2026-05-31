using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    // Referenz auf das Snake-Objekt
    private Snake snake;

    // Label fuer den Score, damit der Event-Handler es erreichen kann
    private Label scoreValue;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        snake = FindObjectOfType<Snake>();

        var root = GetComponent<UIDocument>().rootVisualElement;
        var menuButton = root.Q<Button>("menu-button");
        var restartButton = root.Q<Button>("restart-button");
        scoreValue = root.Q<Label>("score-value");

        menuButton.clicked += () => { /* Menü öffnen */ };
        restartButton.clicked += () => {
            // Ermittelt den Index der aktuell aktiven Szene
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Lädt die Szene anhand des Index komplett neu
            SceneManager.LoadScene(currentSceneIndex);
        };

        // Score: auf Aenderungen reagieren und den aktuellen Wert direkt anzeigen
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += AktualisiereScoreAnzeige;
            AktualisiereScoreAnzeige(ScoreManager.Instance.AktuellerScore);
        }
    }

    private void OnDisable()
    {
        // Event sauber abmelden (z.B. bei Szenenwechsel)
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= AktualisiereScoreAnzeige;
        }
    }

    private void AktualisiereScoreAnzeige(int score)
    {
        if (scoreValue != null)
        {
            scoreValue.text = score.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}