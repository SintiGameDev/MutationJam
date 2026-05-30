using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Food : MonoBehaviour
{
    // Eine Unterkategorie von Food. Vorerst nur Bezeichnung + Sprite,
    // spaeter koennen hier z.B. Punktwerte oder Effekte dazukommen.
    [System.Serializable]
    public class Nahrungssymbol
    {
        public string bezeichnung;   // z.B. "Herz", "Kirschen", "Sterne"
        public Sprite sprite;
    }

    public Collider2D gridArea;

    [Tooltip("Im Inspector befuellen, z.B. drei Eintraege: Herz, Kirschen, Sterne")]
    public Nahrungssymbol[] symbole;

    // Welches Symbol das aktuell liegende Food gerade hat.
    // Wird von der Snake beim Einsammeln ausgelesen.
    public Nahrungssymbol AktuellesSymbol { get; private set; }

    private Snake snake;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        snake = FindObjectOfType<Snake>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        RandomizePosition();
    }

    public void RandomizePosition()
    {
        Bounds bounds = gridArea.bounds;

        // Pick a random position inside the bounds
        // Round the values to ensure it aligns with the grid
        int x = Mathf.RoundToInt(Random.Range(bounds.min.x, bounds.max.x));
        int y = Mathf.RoundToInt(Random.Range(bounds.min.y, bounds.max.y));

        // Prevent the food from spawning on the snake
        while (snake.Occupies(x, y))
        {
            x++;

            if (x > bounds.max.x)
            {
                x = Mathf.RoundToInt(bounds.min.x);
                y++;

                if (y > bounds.max.y) {
                    y = Mathf.RoundToInt(bounds.min.y);
                }
            }
        }

        transform.position = new Vector2(x, y);

        // Bei jedem Neuplatzieren ein neues Symbol auswuerfeln
        WaehleZufaelligesSymbol();
    }

    private void WaehleZufaelligesSymbol()
    {
        if (symbole == null || symbole.Length == 0) {
            return;
        }

        AktuellesSymbol = symbole[Random.Range(0, symbole.Length)];

        if (spriteRenderer != null && AktuellesSymbol.sprite != null) {
            spriteRenderer.sprite = AktuellesSymbol.sprite;
        }
    }

    // Hinweis: Das Einsammeln steuert jetzt die Snake (Snake.OnTriggerEnter2D),
    // damit das Symbol ausgelesen werden kann, BEVOR das Food neu platziert wird.
    // Deshalb gibt es hier kein OnTriggerEnter2D mehr.
}
