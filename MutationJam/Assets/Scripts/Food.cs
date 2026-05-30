using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Food : MonoBehaviour
{
    // Eine Unterkategorie von Food. Vorerst nur Bezeichnung + Farbe,
    // spaeter koennen hier z.B. Punktwerte oder Effekte dazukommen.
    [System.Serializable]
    public class Nahrungstyp
    {
        public string bezeichnung;          // z.B. "Herz", "Kirschen", "Sterne"
        public Color farbe = Color.white;   // Farbe des Food (und spaeter des Segments)
    }

    public Collider2D gridArea;

    [Tooltip("Im Inspector befuellen, z.B. drei Eintraege mit unterschiedlichen Farben")]
    public Nahrungstyp[] typen;

    // Welchen Typ das aktuell liegende Food gerade hat.
    // Wird von der Snake beim Einsammeln ausgelesen.
    public Nahrungstyp AktuellerTyp { get; private set; }

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

        // Bei jedem Neuplatzieren einen neuen Typ auswuerfeln
        WaehleZufaelligenTyp();
    }

    private void WaehleZufaelligenTyp()
    {
        if (typen == null || typen.Length == 0) {
            return;
        }

        AktuellerTyp = typen[Random.Range(0, typen.Length)];

        if (spriteRenderer != null) {
            spriteRenderer.color = AktuellerTyp.farbe;
        }
    }

    // Hinweis: Das Einsammeln steuert jetzt die Snake (Snake.OnTriggerEnter2D),
    // damit der Typ ausgelesen werden kann, BEVOR das Food neu platziert wird.
    // Deshalb gibt es hier kein OnTriggerEnter2D mehr.
}
