using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Snake : MonoBehaviour
{
    public Transform segmentPrefab;
    public Vector2Int direction = Vector2Int.right;
    public float speed = 20f;
    public float speedMultiplier = 1f;
    public int initialSize = 4;
    public bool moveThroughWalls = false;

    [Header("Juice Einstellungen (LeanTween)")]
    public LeanTweenType moveEaseType = LeanTweenType.linear;
    public LeanTweenType segmentEaseType = LeanTweenType.easeOutQuad;
    public float squashAmount = 1.25f;
    public float RaupenFaktor = 0.05f;

    private readonly List<Transform> segments = new List<Transform>();
    private Vector2Int input;
    private float nextUpdate;

    public List<Transform> Segments => segments;

    private void Start()
    {
        ResetState();
    }

    private void Update()
    {
        if (direction.x != 0f)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                input = Vector2Int.up;
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                input = Vector2Int.down;
            }
        }
        else if (direction.y != 0f)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                input = Vector2Int.right;
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                input = Vector2Int.left;
            }
        }
    }

    private void FixedUpdate()
    {
        if (Time.fixedTime < nextUpdate)
        {
            return;
        }

        if (input != Vector2Int.zero)
        {
            direction = input;
        }

        float stepDuration = 1f / (speed * speedMultiplier);

        // 1. Segmente flüssig verschieben und skalieren
        for (int i = segments.Count - 1; i > 0; i--)
        {
            Transform currentSeg = segments[i];
            Vector3 targetPos = segments[i - 1].position;

            LeanTween.cancel(currentSeg.gameObject);

            LeanTween.move(currentSeg.gameObject, targetPos, stepDuration)
                .setEase(segmentEaseType)
                .setUseEstimatedTime(true);

            // Raupen-Effekt (Squash & Stretch)
            float delay = i * RaupenFaktor;

            LeanTween.scale(currentSeg.gameObject, new Vector3(squashAmount, 2f - squashAmount, 1f), stepDuration * 0.5f)
                .setEase(LeanTweenType.easeOutQuad)
                .setDelay(delay)
                .setUseEstimatedTime(true)
                .setOnComplete(() => {
                    if (currentSeg != null)
                    {
                        LeanTween.scale(currentSeg.gameObject, Vector3.one, stepDuration * 0.5f)
                            .setEase(LeanTweenType.easeInOutQuad)
                            .setUseEstimatedTime(true);
                    }
                });
        }

        // 2. Kopf bewegen (Index 0)
        LeanTween.cancel(gameObject);

        int x = Mathf.RoundToInt(transform.position.x) + direction.x;
        int y = Mathf.RoundToInt(transform.position.y) + direction.y;
        Vector3 headTargetPos = new Vector2(x, y);

        LeanTween.move(gameObject, headTargetPos, stepDuration)
            .setEase(moveEaseType)
            .setUseEstimatedTime(true);

        LeanTween.scale(gameObject, new Vector3(squashAmount, 2f - squashAmount, 1f), stepDuration * 0.3f)
            .setUseEstimatedTime(true)
            .setOnComplete(() => {
                LeanTween.scale(gameObject, Vector3.one, stepDuration * 0.7f).setUseEstimatedTime(true);
            });

        nextUpdate = Time.fixedTime + stepDuration;
    }

    public void Grow(Nahrungstyp typ = null)
    {
        Transform segment = Instantiate(segmentPrefab);

        // Wenn wir bereits Segmente haben, spawnen wir das neue an der Position des letzten
        segment.position = segments[segments.Count - 1].position;
        segment.localScale = Vector3.zero;

        LeanTween.scale(segment.gameObject, Vector3.one, 0.2f)
            .setEase(LeanTweenType.easeOutBack)
            .setUseEstimatedTime(true);

        SnakeSegment snakeSegment = segment.gameObject.AddComponent<SnakeSegment>();
        snakeSegment.SetzeTyp(typ);

        // Gefressene Stücke werden direkt hinter dem Kopf (Index 1) eingefügt,
        // Startsegmente (beim Reset) hängen wir einfach hinten an.
        if (typ != null && segments.Count > 1)
        {
            segments.Insert(1, segment);
            // Das neue Segment optisch auf die Position des Kopfes setzen, damit es "ausgestoßen" wird
            segment.position = transform.position;
        }
        else
        {
            segments.Add(segment);
        }
    }

    public void ResetState()
    {
        LeanTween.cancelAll();

        direction = Vector2Int.right;
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;

        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                Destroy(segments[i].gameObject);
            }
        }

        segments.Clear();
        segments.Add(transform);

        for (int i = 0; i < initialSize - 1; i++)
        {
            Grow(); // typ ist null -> bleibt grün und wird vom Matcher ignoriert
        }
    }

    public bool Occupies(int x, int y)
    {
        foreach (Transform segment in segments)
        {
            if (segment == null) continue;
            if (Mathf.RoundToInt(segment.position.x) == x &&
                Mathf.RoundToInt(segment.position.y) == y)
            {
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Food"))
        {
            Food food = other.GetComponent<Food>();
            Nahrungstyp typ = (food != null) ? food.AktuellerTyp : null;

            Grow(typ);

            SnakeSegmentManager segmentManager = GetComponent<SnakeSegmentManager>();
            if (segmentManager != null)
            {
                segmentManager.PruefeUndZerkleinereKette();
            }

            if (food != null)
            {
                food.RandomizePosition();
            }
        }
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            ResetState();
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            if (moveThroughWalls)
            {
                Traverse(other.transform);
            }
            else
            {
                ResetState();
            }
        }
    }

    private void Traverse(Transform wall)
    {
        LeanTween.cancel(gameObject);
        Vector3 position = transform.position;

        if (direction.x != 0f)
        {
            position.x = Mathf.RoundToInt(-wall.position.x + direction.x);
        }
        else if (direction.y != 0f)
        {
            position.y = Mathf.RoundToInt(-wall.position.y + direction.y);
        }

        transform.position = position;
    }
}