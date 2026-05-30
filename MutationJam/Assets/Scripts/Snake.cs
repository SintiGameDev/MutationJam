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

    [Header("Tower-Einstellungen")]
    [Tooltip("Wird an Segmente ohne eigene TurmKonfiguration uebergeben (z.B. Startsegmente)")]
    public GameObject standardTurmPrefab;

    [Header("Juice Einstellungen (LeanTween)")]
    public LeanTweenType moveEaseType = LeanTweenType.linear;
    public LeanTweenType segmentEaseType = LeanTweenType.easeOutQuad;
    public float squashAmount = 1.25f;
    public float RaupenFaktor = 0.05f;

    private readonly List<Transform> segments = new List<Transform>();
    private Vector2Int input;
    private float nextUpdate;

    public List<Transform> Segments => segments;
    // Alias fuer SnakeSegmentManager
    public IReadOnlyList<Transform> Segmente => segments;

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

        // 1. Segmente bewegen (mit strikter Grid-Rundung zur Drift-Vermeidung)
        for (int i = segments.Count - 1; i > 0; i--)
        {
            Transform currentSeg = segments[i];

            // FIX 1: Gnadenloses Runden der Zielkoordinaten, exakt wie in deinem Original.
            // Ohne das Runden kopiert der Schwanz die Float-Ungenauigkeiten der Tween-Animation.
            Vector3 targetPos = new Vector3(
                Mathf.RoundToInt(segments[i - 1].position.x),
                Mathf.RoundToInt(segments[i - 1].position.y),
                0f
            );

            LeanTween.cancel(currentSeg.gameObject);

            LeanTween.move(currentSeg.gameObject, targetPos, stepDuration)
                .setEase(segmentEaseType)
                .setUseEstimatedTime(true);

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

        // 2. Kopf bewegen
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

        // Neues Segment startet am Schwanzende
        segment.position = new Vector3(
            Mathf.RoundToInt(segments[segments.Count - 1].position.x),
            Mathf.RoundToInt(segments[segments.Count - 1].position.y),
            0f
        );
        segment.localScale = Vector3.zero;

        LeanTween.scale(segment.gameObject, Vector3.one, 0.2f)
            .setEase(LeanTweenType.easeOutBack)
            .setUseEstimatedTime(true);

        SnakeSegment snakeSegment = segment.gameObject.AddComponent<SnakeSegment>();
        snakeSegment.StandardTurmPrefab = standardTurmPrefab;
        snakeSegment.SetzeTyp(typ);

        segments.Add(segment);
    }

    // Entfernt eine zusammenhaengende Reihe gleicher Segmente.
    // Das Lückenschliessen uebernimmt FixedUpdate automatisch im naechsten Tick.
    public void EntferneSegmente(int startIndex, int anzahl)
    {
        for (int i = startIndex; i < startIndex + anzahl; i++)
        {
            if (segments[i] != null)
            {
                LeanTween.cancel(segments[i].gameObject);
                Destroy(segments[i].gameObject);
            }
        }
        segments.RemoveRange(startIndex, anzahl);
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
            Grow();
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
            // FIX 2: Verhindert Selbstzerstörung durch dicke Colliders beim Tweening
            int segmentIndex = segments.IndexOf(other.transform);

            // Wenn das getroffene Objekt Teil der Schlange ist UND es sich um den direkten Hals (Index 1 oder 2) handelt:
            // Ignoriere die Kollision. Der Kopf streift den Hals nur visuell durch den Squash-Effekt.
            if (segmentIndex > 0 && segmentIndex <= 2)
            {
                return;
            }

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