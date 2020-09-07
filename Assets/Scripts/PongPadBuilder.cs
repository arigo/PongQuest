using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;
using System.Linq;


public class PongPadBuilder : PongBaseBuilder
{
    public Material cellHitMaterial, unstoppableBallMaterial, growingBallMaterial, cocoonMaterial;
    public ParticleSystem hitPS, starPS;
    public Ball shotBallPrefab;
    public MeshRenderer haloPrefab;
    public Points canvasPointsPrefab;
    public UnityEngine.UI.Text totalPointsText;
    public GameObject pausedCanvasGobj;
    public Transform headsetPrefab;
    public Material padsAndHeadsetTransparencyMaterial;
    public AudioClip ballBounceSound, tileBreakSound;
    //public AudioSource levelEndSound;
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

    Vector3? paused_canvas_position;

    protected override void PausedChange(bool paused)
    {
        if (paused)
        {
            if (!paused_explicit)
            {
                totalPointsText.text = "READY";
            }
            else
            {
                if (paused_canvas_position == null)
                    paused_canvas_position = pausedCanvasGobj.transform.position;

                pausedCanvasGobj.transform.SetPositionAndRotation(
                    OVRManager.instance.transform.TransformPoint(paused_canvas_position.Value),
                    OVRManager.instance.transform.rotation);
                pausedCanvasGobj.SetActive(true);
            }
        }
        else
        {
            Points.UpdateTotalPoints(0);
            pausedCanvasGobj.SetActive(false);
        }
    }

    protected override void FrameByFrameUpdate()
    {
        if (track_cell == null)
        {
            bool next_level_now = false;
            foreach (var cell in FindObjectsOfType<Cell>())
            {
                if (!cell.nextLevelIfOnlyMe)
                {
                    track_cell = cell.gameObject;
                    break;
                }
                next_level_now = true;
            }
            if (track_cell == null)
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
                        level_end_time = Time.time + (next_level_now ? 0f : 1.2f);
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
        //levelEndSound.Play();

        float f = 0f;
        float t0 = Time.time;
        int total_emit = 250;
        while (total_emit > 0)
        {
            yield return null;

            f += Time.deltaTime * 100f;
            while (f >= 1f)
            {
                var emit_params = new ParticleSystem.EmitParams
                {
                    position = new Vector3(0, 1.5f, 3f) + 1.5f * Random.insideUnitSphere,
                    velocity = new Vector3(0, 1, 0) + (Random.onUnitSphere * 0.5f),
                    startSize = 0.1f,
                    startLifetime = Random.Range(0.2f, 0.5f),
                    startColor = Color.white,
                };
                hitPS.Emit(emit_params, 1);
                total_emit--;
                f -= 1f;
            }
        }
        Baroque.FadeToColor(Color.black, 2f);
        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Intro");
    }


    class PointerOnPausedExplicit
    {
        public Transform cylinder_tr;
        //public int menu_press;
        public Collider clicking_button;
    }
    PointerOnPausedExplicit[] expls;

    protected override void AddPointersOnPausedExplicit()
    {
        foreach (var coll in pausedCanvasGobj.GetComponentsInChildren<BoxCollider>())
            coll.GetComponent<UnityEngine.UI.Text>().color = new Color(0.9f, 0.9f, 0.9f);

        foreach (var ctrl in Baroque.GetControllers())
            if (ctrl.isReady)
            {
                var expl = ctrl.GetAdditionalData(ref expls);
                if (expl.cylinder_tr == null)
                {
                    var gobj = Instantiate(pausedPointerPrefab, ctrl.transform);
                    expl.cylinder_tr = gobj.transform.GetChild(0);
                }

                const float Z_MAX = 0.75f;
                float z = Z_MAX;

                Collider click = null;
                if (Physics.Raycast(ctrl.transform.position, ctrl.transform.forward, out var hitInfo, 2 * Z_MAX,
                    layerMask: 1 << IntroPointer.LAYER_SELECTION))
                    click = hitInfo.collider;

                if (click != null && (expl.clicking_button == null || expl.clicking_button == click))
                {
                    var text = hitInfo.collider.GetComponent<UnityEngine.UI.Text>();
                    if (expl.clicking_button == null)
                        text.color = new Color(1f, 0.7f, 0.7f);
                    else
                        text.color = new Color(1f, 0.45f, 0.45f);

                    z = hitInfo.distance * 0.5f;
                }
                var cylinder_tr = expl.cylinder_tr;
                var v = cylinder_tr.localScale; v.y = z; cylinder_tr.localScale = v;
                v = cylinder_tr.localPosition; v.z = z; cylinder_tr.localPosition = v;

                if (ctrl.triggerPressed)
                {
                    if (expl.clicking_button == null)
                        expl.clicking_button = click;
                    //expl.menu_press = 0;
                }
                else if (expl.clicking_button != null && expl.clicking_button == click)
                {
                    SetPausedExplicit(false);
                    if (click.gameObject.name == "EXIT")
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Intro");
                    break;
                }
                else
                {
                    expl.clicking_button = null;
                    /*if (ctrl.touchpadPressed || ctrl.menuPressed)
                    {
                        if (expl.menu_press == 1)
                            expl.menu_press = 2;
                    }
                    else if (expl.menu_press == 2)
                    {
                        SetPausedExplicit(false);
                        break;
                    }
                    else
                        expl.menu_press = 1;*/
                }
            }
    }

    protected override void RemovePointersOnPausedExplicit()
    {
        var old_expls = expls;
        expls = null;
        foreach (var ctrl in Baroque.GetControllers())
        {
            var expl = ctrl.GetAdditionalData(ref old_expls);
            if (expl.cylinder_tr != null)
                Destroy((GameObject)expl.cylinder_tr.parent.gameObject);
        }
    }
}
