using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    [Tooltip("Optional: eigenes UIDocument-GameObject mit der DeathScreen.uxml. " +
             "Leer lassen, falls die Death-Elemente im selben Dokument wie das HUD liegen. " +
             "Bei eigenem Dokument: Sort Order hoeher als das HUD setzen.")]
    [SerializeField] private UIDocument deathScreenDocument;

    private Snake snake;

    private Label scoreValue;
    private VisualElement deathRoot;
    private Label deathScoreValue;

    void Start()
    {
        snake = FindObjectOfType<Snake>();

        var hudRoot = GetComponent<UIDocument>().rootVisualElement;

        // HUD
        scoreValue = hudRoot.Q<Label>("score-value");
        VerkabeleButton(hudRoot, "menu-button", OeffneMenue);
        VerkabeleButton(hudRoot, "restart-button", NeuStarten);

        // Death-Elemente: erst im (optionalen) Death-Dokument suchen, sonst im HUD-Root.
        // So funktioniert es, egal ob du ein zweites UIDocument nutzt oder alles in einem hast.
        VisualElement suchRoot = (deathScreenDocument != null)
            ? deathScreenDocument.rootVisualElement
            : hudRoot;

        deathRoot = suchRoot.Q<VisualElement>("death-root");
        deathScoreValue = suchRoot.Q<Label>("death-score-value");
        VerkabeleButton(suchRoot, "death-restart-button", NeuStarten);
        VerkabeleButton(suchRoot, "death-menu-button", OeffneMenue);

        if (deathRoot != null)
        {
            deathRoot.style.display = DisplayStyle.None;
        }
        else
        {
            Debug.LogWarning("UIManager: 'death-root' nicht gefunden. Liegt die DeathScreen.uxml " +
                "im zugewiesenen Death-Dokument oder im HUD-Dokument?");
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += AktualisiereScoreAnzeige;
            AktualisiereScoreAnzeige(ScoreManager.Instance.AktuellerScore);
        }

        if (snake != null)
        {
            snake.OnGestorben += ZeigeDeathScreen;
        }
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= AktualisiereScoreAnzeige;
        }
        if (snake != null)
        {
            snake.OnGestorben -= ZeigeDeathScreen;
        }
        Time.timeScale = 1f;
    }

    // Sucht den Button und haengt den Handler an – mit Warnung, falls nicht gefunden.
    private void VerkabeleButton(VisualElement root, string name, System.Action handler)
    {
        Button btn = root.Q<Button>(name);
        if (btn != null)
        {
            btn.clicked += handler;
        }
        else
        {
            Debug.LogWarning($"UIManager: Button '{name}' wurde nicht gefunden – Handler nicht verbunden.");
        }
    }

    private void AktualisiereScoreAnzeige(int score)
    {
        if (scoreValue != null)
        {
            scoreValue.text = score.ToString();
        }
    }

    private void ZeigeDeathScreen()
    {
        if (deathScoreValue != null && ScoreManager.Instance != null)
        {
            deathScoreValue.text = ScoreManager.Instance.AktuellerScore.ToString();
        }
        if (deathRoot != null)
        {
            deathRoot.style.display = DisplayStyle.Flex;
        }
        Time.timeScale = 0f;
    }

    private void NeuStarten()
    {
        Debug.Log("UIManager: 'Neu starten' geklickt – versuche Szene neu zu laden.");

        // WICHTIG: Zeit zuruecksetzen, sonst startet die neue Szene pausiert.
        Time.timeScale = 1f;

        Scene aktiv = SceneManager.GetActiveScene();

        // Haeufigste Fehlerquelle: Szene ist nicht in den Build Settings.
        if (aktiv.buildIndex < 0)
        {
            Debug.LogError("UIManager: Die aktive Szene ist NICHT in den Build Settings eingetragen! " +
                "File > Build Settings > 'Add Open Scenes'. Ohne Eintrag kann LoadScene sie nicht laden.");
            return;
        }

        SceneManager.LoadScene(aktiv.buildIndex);
    }

    private void OeffneMenue()
    {
        Time.timeScale = 1f;
        // TODO: Menü öffnen (z.B. SceneManager.LoadScene("MainMenu"))
    }

    void Update()
    {
    }
}