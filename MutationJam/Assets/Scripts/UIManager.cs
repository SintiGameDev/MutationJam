using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    [Tooltip("Optional: eigenes UIDocument-GameObject mit der DeathScreen.uxml. " +
             "Leer lassen, falls die Death-Elemente im selben Dokument wie das HUD liegen. " +
             "Bei eigenem Dokument: Sort Order hoeher als das HUD setzen.")]
    [SerializeField] private UIDocument deathScreenDocument;

    [Header("Score-Animation")]
    [Tooltip("Wie stark das Score-Label bei jeder Aenderung kurz aufpoppt (1 = aus).")]
    public float punchSkala = 1.4f;
    [Tooltip("Dauer des Aufpopp-Effekts in Sekunden.")]
    public float punchDauer = 0.18f;

    private Snake snake;

    private Label scoreValue;
    private VisualElement deathRoot;
    private Label deathScoreValue;

    private Coroutine punchCoroutine;
    private bool scoreInitialisiert = false;

    void Start()
    {
        snake = FindObjectOfType<Snake>();

        var hudRoot = GetComponent<UIDocument>().rootVisualElement;

        scoreValue = hudRoot.Q<Label>("score-value");
        VerkabeleButton(hudRoot, "menu-button", OeffneMenue);
        VerkabeleButton(hudRoot, "restart-button", NeuStarten);

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

        // Beim allerersten Setzen (Spielstart) NICHT poppen
        if (scoreInitialisiert)
        {
            StartePunch(scoreValue);
        }
        scoreInitialisiert = true;
    }

    private void ZeigeDeathScreen()
    {
        if (deathScoreValue != null && ScoreManager.Instance != null)
        {
            deathScoreValue.text = ScoreManager.Instance.AktuellerScore.ToString();
            StartePunch(deathScoreValue);
        }
        if (deathRoot != null)
        {
            deathRoot.style.display = DisplayStyle.Flex;
        }
        Time.timeScale = 0f;
    }

    // Startet den Aufpopp-Effekt fuer ein Label (vorherigen stoppen).
    private void StartePunch(Label label)
    {
        if (label == null || punchSkala <= 1f) return;

        if (punchCoroutine != null)
        {
            StopCoroutine(punchCoroutine);
        }
        punchCoroutine = StartCoroutine(PunchRoutine(label));
    }

    private IEnumerator PunchRoutine(Label label)
    {
        float t = 0f;
        float dauer = Mathf.Max(0.0001f, punchDauer);

        while (t < dauer)
        {
            // Ungescalete Zeit, damit es auch bei Time.timeScale = 0 (Death-Screen) laeuft
            t += Time.unscaledDeltaTime;
            float p = t / dauer;
            // Hoch und wieder zurueck (Sinus-Bogen)
            float s = 1f + (punchSkala - 1f) * Mathf.Sin(p * Mathf.PI);
            label.transform.scale = new Vector3(s, s, 1f);
            yield return null;
        }

        label.transform.scale = Vector3.one;
        punchCoroutine = null;
    }

    private void NeuStarten()
    {
        Debug.Log("UIManager: 'Neu starten' geklickt – versuche Szene neu zu laden.");
        Time.timeScale = 1f;

        Scene aktiv = SceneManager.GetActiveScene();
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