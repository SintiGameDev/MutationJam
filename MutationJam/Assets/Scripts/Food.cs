using System.Collections.Generic;
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

    // Registry ALLER aktiven Foods. Dadurch kann jedes Food beim Platzieren
    // pruefen, ob bereits ein anderes Food auf dem Feld liegt – so spawnen
    // mehrere Foods nicht aufeinander.
    private static readonly List<Food> alleFoods = new List<Food>();

    private Snake snake;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        snake = FindObjectOfType<Snake>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        gridArea = GameObject.FindGameObjectWithTag("GridArea").GetComponent<Collider2D>();
        AktuellerTyp = Random.value > 0.5f ? Nahrungstyp.Rot : Nahrungstyp.Blau; // Default-Typ, falls keine Typen definiert sind
    }

    private void OnEnable()
    {
        alleFoods.Add(this);
    }

    private void OnDisable()
    {
        alleFoods.Remove(this);
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

        // Feld weitersuchen, solange es von der Schlange ODER einem anderen Food belegt ist
        while (IstBelegt(x, y))
        {
            x++;
            if (x > bounds.max.x)
            {
                x = Mathf.RoundToInt(bounds.min.x);
                y++;
                if (y > bounds.max.y)
                {
                    y = Mathf.RoundToInt(bounds.min.y);
                }
            }
        }

        transform.position = new Vector2(x, y);
        WaehleZufaelligenTyp();
    }

    // Prueft Schlange UND alle anderen Foods (nicht sich selbst).
    private bool IstBelegt(int x, int y)
    {
        if (snake != null && snake.Occupies(x, y))
        {
            return true;
        }

        foreach (Food anderes in alleFoods)
        {
            if (anderes == this)
            {
                continue;
            }

            if (Mathf.RoundToInt(anderes.transform.position.x) == x &&
                Mathf.RoundToInt(anderes.transform.position.y) == y)
            {
                return true;
            }
        }

        return false;
    }

    private void WaehleZufaelligenTyp()
    {
        if (typen == null || typen.Length == 0)
        {
            return;
        }

        AktuellerTyp = typen[Random.Range(0, typen.Length)];

        if (spriteRenderer != null)
        {
            spriteRenderer.color = AktuellerTyp.farbe;
        }
    }

    // Das Einsammeln steuert Snake.OnTriggerEnter2D, damit der Typ
    // ausgelesen wird, BEVOR das Food neu platziert wird.
}