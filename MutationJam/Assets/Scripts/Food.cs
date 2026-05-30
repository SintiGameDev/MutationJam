using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Food : MonoBehaviour
{
    public Collider2D gridArea;

    [Tooltip("Im Inspector befuellen – z.B. drei Eintraege: Herz, Kirschen, Sterne. Alpha der Farbe auf 255 setzen!")]
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

        int x = Mathf.RoundToInt(Random.Range(bounds.min.x, bounds.max.x));
        int y = Mathf.RoundToInt(Random.Range(bounds.min.y, bounds.max.y));

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

    // Das Einsammeln steuert Snake.OnTriggerEnter2D, damit der Typ
    // ausgelesen wird, BEVOR das Food neu platziert wird.
}
