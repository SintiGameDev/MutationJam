using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    // Referenz auf das Snake-Objekt
    private Snake snake;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        snake = FindObjectOfType<Snake>();

        var root = GetComponent<UIDocument>().rootVisualElement;

        var menuButton = root.Q<Button>("menu-button");
        var restartButton = root.Q<Button>("restart-button");
        var scoreValue = root.Q<Label>("score-value");

        menuButton.clicked += () => { /* Menü öffnen */ };
        restartButton.clicked += () => { if (snake != null) snake.ResetState(); };

        // Score aktualisieren:
        //scoreValue.text = currentScore.ToString();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
