using UnityEngine;

// Zentrale Sound-Ausgabe (Singleton, wie ScoreManager).
// Andere Skripte rufen z.B. SoundManager.Instance?.SpieleMerge() auf.
// Alle Clips an einer Stelle im Inspector pflegbar.
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Clips")]
    [Tooltip("Schlange frisst ein Food.")]
    public AudioClip essenClip;
    [Tooltip("Ein neues Segment wird gespawnt (nur beim Food-Essen).")]
    public AudioClip spawnClip;
    [Tooltip("Drei Segmente mergen zu einer hoeheren Stufe.")]
    public AudioClip mergeClip;

    [Header("Lautstaerke")]
    [Range(0f, 1f)] public float essenLautstaerke = 1f;
    [Range(0f, 1f)] public float spawnLautstaerke = 1f;
    [Range(0f, 1f)] public float mergeLautstaerke = 1f;

    [Header("Variation")]
    [Tooltip("Zufaellige Pitch-Abweichung pro Sound (0 = aus). Macht wiederholte " +
             "Sounds weniger monoton – z.B. 0.1 = +/-10 %.")]
    [Range(0f, 0.5f)] public float pitchVariation = 0.08f;

    private AudioSource quelle;

    private void Awake()
    {
        // Doppelte Instanz vermeiden
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        quelle = GetComponent<AudioSource>();
        quelle.playOnAwake = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SpieleEssen() => Spiele(essenClip, essenLautstaerke);
    public void SpieleSpawn() => Spiele(spawnClip, spawnLautstaerke);
    public void SpieleMerge() => Spiele(mergeClip, mergeLautstaerke);

    // Spielt einen Clip ueber PlayOneShot, damit sich mehrere Sounds
    // ueberlappen koennen (z.B. Essen + Spawn quasi gleichzeitig).
    private void Spiele(AudioClip clip, float lautstaerke)
    {
        if (clip == null || quelle == null) return;

        // Pitch leicht variieren fuer Abwechslung
        if (pitchVariation > 0f)
            quelle.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        else
            quelle.pitch = 1f;

        quelle.PlayOneShot(clip, lautstaerke);
    }

    // Generisch: spielt einen beliebigen Clip (z.B. den Schuss-Sound aus der
    // TurmKonfiguration). Mit eigener Pitch-Variation pro Aufruf.
    public void SpieleClip(AudioClip clip, float lautstaerke = 1f)
    {
        Spiele(clip, lautstaerke);
    }
}
