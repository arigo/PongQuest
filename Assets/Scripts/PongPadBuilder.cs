using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;
using System.Linq;


public class PongPadBuilder : PongBaseBuilder
{
    public Material cellHitMaterial, unstoppableBallMaterial, growingBallMaterial;
    public ParticleSystem hitPS, starPS;
    public Ball shotBallPrefab;
    public MeshRenderer haloPrefab;
    public Points canvasPointsPrefab;
    public UnityEngine.UI.Text totalPointsText;
    public Transform headsetPrefab;
    public Material padsAndHeadsetTransparencyMaterial;
    public AudioClip ballBounceSound, tileBreakSound;
    public AudioClip[] backgroundMusicParts;
    public int episodeNumber = 1;
    public GameObject[] levelPrefabs;

    public static PongPadBuilder instance { get; private set; }

    GameObject track_cell;
    float? level_end_time;
    GameObject levelInstance;
    int current_level;  // = 6;   /* set to non-zero to debug from a different level */
    public int _total_points { get; set; }
    AudioSource[] music_sources;
    int number_of_cells_in_this_level;
    float number_of_cells_killed;

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

        music_sources = new AudioSource[backgroundMusicParts.Length];
        var music_go = new GameObject("music");
        for (int i = 0; i < backgroundMusicParts.Length; i++)
        {
            var music_source = music_go.AddComponent<AudioSource>();
            music_source.clip = backgroundMusicParts[i];
            music_source.loop = true;
            music_source.priority = 0;
            music_source.volume = i == 0 ? MUSIC_VOLUME_MAX : 0f;
            music_sources[i] = music_source;
        }
        foreach (var asrc in music_sources)
            asrc.Play();
    }

    public void KilledOneCell(float cell_fraction)
    {
        number_of_cells_killed += cell_fraction;
        UpdateMusicVolumes();
    }

    float music_volumes_fraction = 0f, target_volumes_fraction = 0f;
    const float MUSIC_VOLUME_MAX = 0.65f;
    Coroutine coro_music_volumes;

    void UpdateMusicVolumes()
    {
        target_volumes_fraction = number_of_cells_killed;
        if (target_volumes_fraction > 0f && number_of_cells_in_this_level > 1)
            target_volumes_fraction /= number_of_cells_in_this_level - 1;

        if (coro_music_volumes == null)
            coro_music_volumes = StartCoroutine(_UpdateMusicVolumes());
    }

    IEnumerator _UpdateMusicVolumes()
    {
        while (music_volumes_fraction != target_volumes_fraction)
        {
            yield return null;

            float dt = Time.deltaTime;
            if (music_volumes_fraction < target_volumes_fraction)
                music_volumes_fraction = Mathf.Min(target_volumes_fraction, music_volumes_fraction + dt);
            else
                music_volumes_fraction = Mathf.Max(target_volumes_fraction, music_volumes_fraction - dt);

            float m = music_volumes_fraction * (music_sources.Length - 1) + 1;
            for (int i = 0; i < music_sources.Length; i++)
            {
                float vol1 = Mathf.Clamp01(m) * MUSIC_VOLUME_MAX;
                music_sources[i].volume = vol1;
                m -= 1f;
            }
        }
        coro_music_volumes = null;
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
                    {
                        if (number_of_cells_in_this_level > 0)
                        {
                            music_volumes_fraction = 1f;
                            for (int i = 0; i < music_sources.Length; i++)
                            {
                                int j = Random.Range(0, music_sources.Length);
                                var tmp = music_sources[i];
                                music_sources[i] = music_sources[j];
                                music_sources[j] = tmp;
                            }
                        }
                        number_of_cells_in_this_level = 0;
                        number_of_cells_killed = 0;
                        UpdateMusicVolumes();
                        level_end_time = Time.time + 1.2f;
                    }
                }

                if (Time.time >= level_end_time.Value)
                {
                    level_end_time = null;
                    if (levelInstance != null)
                        Destroy((GameObject)levelInstance);

                    Bonus.RemoveAllBonuses();
                    levelInstance = Instantiate(levelPrefabs[current_level++]);

                    number_of_cells_in_this_level = levelInstance.GetComponentsInChildren<Cell>().Length;
                    number_of_cells_killed = 0;
                }
            }
        }

        Physics.SyncTransforms();

        PongPad.UpdateAllBalls();
    }

    IEnumerator EndGame()
    {
        FadeOutSounds(0.8f);

        float f = 0f;
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
        }
        Baroque.FadeToColor(Color.black, 2f);
        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Intro");
    }
}
