using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;
using System.Linq;


public class PongPadBuilder : PongBaseBuilder
{
    public Material cellHitMaterial, unstoppableBallMaterial;
    public ParticleSystem hitPS, starPS;
    public Ball shotBallPrefab;
    public MeshRenderer haloPrefab;
    public Points canvasPointsPrefab;
    public UnityEngine.UI.Text totalPointsText;
    public AudioClip backgroundMusic;
    public GameObject[] levelPrefabs;

    public static PongPadBuilder instance { get; private set; }

    GameObject track_cell;
    float? level_end_time;
    GameObject levelInstance;
    int current_level = 16;  // = 6;   /* set to non-zero to debug from a different level */
    public int _total_points { get; set; }
    AudioSource music_source;

    private void Awake()
    {
        instance = this;
        Ball.start_positions = new List<Vector3>();
        foreach (var ball in FindObjectsOfType<Ball>())
            Ball.start_positions.Add(ball.transform.position);
    }

    protected override void Start()
    {
        level_end_time = Time.time;

        base.Start();

        if (backgroundMusic != null)
        {
            var go = new GameObject("music");
            music_source = go.AddComponent<AudioSource>();
            music_source.clip = backgroundMusic;
            music_source.loop = true;
            music_source.priority = 0;
            music_source.volume = 0.65f;
            //DontDestroyOnLoad(go);
            //ChangedMusicVolume();
            music_source.Play();
        }
    }

    protected override void SetPaused(bool paused)
    {
        if (paused)
            totalPointsText.text = "READY";
        else
            Points.UpdateTotalPoints(0);
    }

    protected override void FrameByFrameUpdate()
    {
        if (track_cell == null)
        {
            var cell = FindObjectOfType<Cell>();
            if (cell != null)
            {
                track_cell = cell.gameObject;
            }
            else
            {
                if (level_end_time == null)
                {
                    Bonus.RemoveAllBonuses();

                    if (current_level >= levelPrefabs.Length)
                    {
                        Ball.RemoveAllBalls();
                        track_cell = gameObject;
                        StartCoroutine(EndGame());
                        return;
                    }
                    else
                        level_end_time = Time.time + 1.2f;
                }

                if (Time.time >= level_end_time.Value)
                {
                    level_end_time = null;
                    if (levelInstance != null)
                        Destroy((GameObject)levelInstance);

                    levelInstance = Instantiate(levelPrefabs[current_level++]);
                    Bonus.RemoveAllBonuses();
                }
            }
        }

        Physics.SyncTransforms();

        PongPad.UpdateAllBalls();
    }

    IEnumerator EndGame()
    {
        float f = 0f;
        float music_initial_volume = music_source.volume;
        float t0 = Time.time;
        int total_emit = 250;
        while (total_emit > 0)
        {
            yield return null;

            f += Time.deltaTime * 100f;
            while (f >= 1f)
            {
                var pos = new Vector3(0, 1.5f, 3f) + 1.5f * Random.insideUnitSphere;
                hitPS.Emit(pos, new Vector3(0, 1, 0) + (Random.onUnitSphere * 0.5f),
                           0.1f, Random.Range(0.2f, 0.5f), Color.white);
                total_emit--;
                f -= 1f;
            }

            music_source.volume = Mathf.Lerp(music_initial_volume, 0, Time.time - t0);
        }
        Baroque.FadeToColor(Color.black, 2f);
        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Intro");
    }
}
